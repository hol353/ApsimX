using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using CMPServices;
using APSIM.Shared.Utilities;

//Classes for I/O of the generic parameter set class, TParameterSet.           
namespace Models.GrazPlan
{
    

    /// <summary>
    /// Class that wraps the XML param reader
    /// </summary>
    static public class TGParamFactory
    {
        static private TParameterXMLFactory _GParamFactory = null;

        /// <summary>
        /// Returns a ptr to the _GParamFactory. ParamFactory is loaded on demand only.
        /// </summary>
        static public TParameterXMLFactory ParamXMLFactory()
        {
            if (_GParamFactory == null)
                _GParamFactory = new TParameterXMLFactory();
            return _GParamFactory;
        }
    }

    /// <summary>
    /// The DTD for the XML parameter format is:                                     
    /// <![CDATA[
    /// <!ELEMENT parameters (par? set?) >                                           
    /// <!ATTLIST parameters name    CDATA "" >                                    
    /// <!ATTLIST parameters version CDATA "" >                                    
    /// <!ELEMENT set (par? set? translate?) >                                       
    /// <!ATTLIST set name CDATA #REQUIRED >                                       
    /// <!ATTLIST set locales CDATA "" >                                           
    /// <!ELEMENT translate #PCDATA >                                                
    /// <!ATTLIST translate lang CDATA #REQUIRED >                                 
    /// <!ELEMENT par #PCDATA >                                                      
    /// <!ATTLIST par name CDATA #REQUIRED >                                       
    /// ]]>                                                                           
    /// * Parameter value data are contained within the &lt;par&gt; elements.              
    /// * The name attribute of a &lt;par&gt; element is parsed to determine which         
    /// parameter value(s) it holds.                                               
    /// * Lists of values within an element are separated by commas (this means that 
    /// the comma is not permitted in text parameter values).                      
    /// * A blank parameter value (including in a list) denotes "leave at the value  
    /// in the parent parameter set", which may be undefined.                      
    /// </summary>
    public class TParameterXMLFactory
    {
        /// <summary>
        /// Type real-single
        /// </summary>
        protected const TTypedValue.TBaseType ptyReal = TTypedValue.TBaseType.ITYPE_SINGLE;
        /// <summary>
        /// Type integer
        /// </summary>
        protected const TTypedValue.TBaseType ptyInt = TTypedValue.TBaseType.ITYPE_INT4;
        /// <summary>
        /// Type Boolean
        /// </summary>
        protected const TTypedValue.TBaseType ptyBool = TTypedValue.TBaseType.ITYPE_BOOL;
        /// <summary>
        /// Type string
        /// </summary>
        protected const TTypedValue.TBaseType ptyText = TTypedValue.TBaseType.ITYPE_STR;

        /// <summary>
        /// Parses a &lt;parameters&gt; or &lt;set&gt; element in an XML parameter document          
        /// </summary>
        /// <param name="Parser"></param>
        /// <param name="aNode"></param>
        /// <param name="Params"></param>
        /// <param name="bModify"></param>
        private void readParamNode(TXMLParser Parser, XmlNode aNode, ref TParameterSet Params, bool bModify)
        {
            XmlNode childNode;
            TParameterSet newParams;
            string sTag,
            sValues,
            sChildName,
            sLang,
            sDummy;

            try
            {
                Params.sName = Parser.getAttrValue(aNode, "name");                        // Name and version information. The     
                Params.sEnglishName = Params.sName;
                if (Params.bRootNode())                                                     //   version is passed to child sets   
                    Params.sVersion = Parser.getAttrValue(aNode, "version");               //   during creation                   

                childNode = Parser.firstElementChild(aNode, "translate");                 // See if tbere's a translation of the name matching our current language setting 
                while (childNode != null)
                {
                    sLang = Parser.getAttrValue(childNode, "lang");
                    sDummy = Parser.getText(childNode);
                    Params.addTranslation(sLang, sDummy);
                    childNode = Parser.nextElementSibling(childNode, "translate");
                }

                if (!bModify)                                                           // If we are not modifying an existing   
                    while (Params.iChildCount() > 0)                                           //   parameter set, then clear any old   
                        Params.deleteChild(Params.iChildCount() - 1);                              //   child parameter sets               

                sValues = Parser.getAttrValue(aNode, "locales").Trim();                     // Populate the locale list              
                Params.setLocaleText(sValues);

                childNode = Parser.firstElementChild(aNode, "par");                       // Parse the <par> elements              
                while (childNode != null)
                {
                    sTag = Parser.getAttrValue(childNode, "name");
                    sValues = Parser.getText(childNode);
                    readParamValues(ref Params, sTag, sValues, bModify);
                    childNode = Parser.nextElementSibling(childNode, "par");
                }
                Params.deriveParams();

                childNode = Parser.firstElementChild(aNode, "set");                       // Create child parameter sets from the  
                while (childNode != null)                                                   //   <set> elements                     
                {
                    if (!bModify)
                        newParams = Params.addChild();
                    else
                    {                                                                      // If we are modifying an existing      
                        sChildName = Parser.getAttrValue(childNode, "name");                  //  parameter set, then locate the child 
                        newParams = Params.getChild(sChildName);                              //   set that we are about to parse      
                        if (newParams == null)
                            newParams = Params.addChild();
                    }
                    readParamNode(Parser, childNode, ref newParams, bModify);
                    childNode = Parser.nextElementSibling(childNode, "set");
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        /// <summary>
        /// Parses the contents of a &lt;par&gt; element in an XML parameter document.         
        /// </summary>
        /// <param name="Params"></param>
        /// <param name="sTag"></param>
        /// <param name="sValues"></param>
        /// <param name="bPropagate"></param>
        private void readParamValues(ref TParameterSet Params, string sTag, string sValues, bool bPropagate)
        {
            TParameterDefinition Definition;
            string sValue;
            int Idx;

            sValues = sValues.Trim();

            if ((sTag != "") && (sValues != ""))
            {
                Definition = Params.getDefinition(sTag);
                if ((Definition == null) || (Definition.iDimension() > 1))
                    throw new Exception("Invalid tag when reading parameters: " + sTag);

                if (Definition.bIsScalar())                                                // Reference to a single value           
                    assignParameter(ref Params, sTag, sValues, bPropagate);
                else
                {                                                                      // Reference to a list of values         
                    for (Idx = 0; Idx <= Definition.iCount - 1; Idx++)
                    {
                        sValue = stripValue(ref sValues);
                        if (sValue != "")                                                  // Null string denotes "leave value at   
                            assignParameter(ref Params, sTag + Definition.item(Idx).sPartName,             //   default"                    
                                             sValue, bPropagate);
                    }
                }
            }
        }

        /// <summary>
        /// Sets the value of a parameter in a set, and optionally propagates the value  
        /// to descendant parameter sets                                                 
        /// </summary>
        /// <param name="Params"></param>
        /// <param name="sTag"></param>
        /// <param name="sValue"></param>
        /// <param name="bPropagate"></param>
        private void assignParameter(ref TParameterSet Params, string sTag, string sValue, bool bPropagate)
        {
            int Idx;

            Params.setParam(sTag, sValue);
            if (bPropagate)
            {
                for (Idx = 0; Idx <= Params.iChildCount() - 1; Idx++)
                {
                    TParameterSet child = Params.getChild(Idx);
                    assignParameter(ref child, sTag, sValue, true);
                }
            }
        }

        /// <summary>
        /// Reads a string from a comma-separated list                                   
        /// </summary>
        /// <param name="sValues"></param>
        /// <returns></returns>
        private string stripValue(ref string sValues)
        {
            int iPosn;
            string result = "";

            iPosn = sValues.IndexOf(',');
            if (iPosn < 0)
            {
                result = sValues.Trim();
                sValues = "";
            }
            else
            {
                result = sValues.Substring(0, iPosn).Trim();
                sValues = sValues.Remove(0, iPosn + 1);
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sText"></param>
        /// <param name="Params"></param>
        /// <param name="bModify"></param>
        public void readFromXML(string sText, ref TParameterSet Params, bool bModify)
        {
            TXMLParser Parser;

            Parser = new TXMLParser(sText);
            readParamNode(Parser, Parser.rootNode(), ref Params, bModify);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="subSet"></param>
        /// <param name="Definition"></param>
        /// <returns></returns>
        private bool bDiffers(TParameterSet subSet, TParameterDefinition Definition)
        {
            bool result;

            if (!Definition.bValueDefined())
                result = false;
            else if ((subSet.Parent == null) || (!subSet.Parent.bIsDefined(Definition.sFullName)))
                result = true;
            else
            {
                switch (Definition.paramType)
                {
                    case ptyReal: result = (subSet.fParam(Definition.sFullName) != subSet.Parent.fParam(Definition.sFullName));
                        break;
                    case ptyInt: result = (subSet.iParam(Definition.sFullName) != subSet.Parent.iParam(Definition.sFullName));
                        break;
                    case ptyBool: result = (subSet.bParam(Definition.sFullName) != subSet.Parent.bParam(Definition.sFullName));
                        break;
                    case ptyText: result = (subSet.sParam(Definition.sFullName) != subSet.Parent.sParam(Definition.sFullName));
                        break;
                    default: result = false;
                        break;
                }
            }
            return result;
        }

        private void writeParameters(TParameterSet subSet, TParameterDefinition Definition, List<string> Strings, int iIndent)
        {
            int iDiffCount;
            string sLine;
            int Idx;

            if (Definition.iDimension() > 1)                                         // Multi-dimensional array of            
                for (Idx = 0; Idx <= Definition.iCount - 1; Idx++)                                  //   parameters - recurse                
                    writeParameters(subSet, Definition.item(Idx), Strings, iIndent);

            else if (Definition.bIsScalar() && bDiffers(subSet, Definition))      // Single parameter value                
            {
                sLine = new string(' ', iIndent)
                       + "<par name=\"" + Definition.sFullName + "\">"
                       + subSet.sParam(Definition.sFullName)
                       + "</par>";
                Strings.Add(sLine);
            }
            else                                                                     // List of parameter values (one-        
            {                                                                    //   dimensional)                        
                iDiffCount = 0;
                for (Idx = 0; Idx <= Definition.iCount - 1; Idx++)
                    if (bDiffers(subSet, Definition.item(Idx)))
                        iDiffCount++;

                if (iDiffCount > 1)                                                // More than one difference - write      
                {                                                                  //   the differing values in a list      
                    sLine = new string(' ', iIndent)
                             + "<par name=\"" + Definition.sFullName + "\">";
                    for (Idx = 0; Idx <= Definition.iCount - 1; Idx++)
                    {
                        if (Idx > 0)
                            sLine += ',';
                        if (bDiffers(subSet, Definition.item(Idx)))
                            sLine += subSet.sParam(Definition.item(Idx).sFullName);
                    }
                    sLine += "</par>";
                    Strings.Add(sLine);
                }
                else if (iDiffCount == 1)                                           // Only one parameter is different -     
                    for (Idx = 0; Idx <= Definition.iCount - 1; Idx++)                               //  write it as a scalar                
                        if (bDiffers(subSet, Definition.item(Idx)))
                            writeParameters(subSet, Definition.item(Idx), Strings, iIndent);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="subSet"></param>
        /// <param name="Strings"></param>
        /// <param name="sElem"></param>
        /// <param name="iIndent"></param>
        private void writeParamSet(TParameterSet subSet,
                                   List<string> Strings,
                                   string sElem,
                                   int iIndent)
        {
            string sLine;
            int Idx;

            if (!subSet.bRootNode())
                Strings.Add("");

            sLine = new string(' ', iIndent) + "<" + sElem + " name=\"" + subSet.sEnglishName + "\"";
            if (subSet.bRootNode())
                sLine += " version=\"" + subSet.sVersion + "\">";
            else
            {
                if (subSet.iLocaleCount() > 0)
                {
                    sLine += " locales=\"" + subSet.getLocale(0);
                    for (Idx = 1; Idx <= subSet.iLocaleCount() - 1; Idx++)
                        sLine += ";" + subSet.getLocale(Idx);
                    sLine += "\"";
                }
                sLine += ">";
            }
            Strings.Add(sLine);

            if (subSet.iTranslationCount() > 0)
                for (Idx = 0; Idx <= subSet.iTranslationCount() - 1; Idx++)
                {
                    sLine = new string(' ', iIndent + 2) + "<translate lang=\"" +
                             subSet.getTranslation(Idx).sLang + "\">" +
                             TTypedValue.escapeText(subSet.getTranslation(Idx).sText) + "</translate>";
                    Strings.Add(sLine);
                }

            for (Idx = 0; Idx <= subSet.iDefinitionCount() - 1; Idx++)
                writeParameters(subSet, subSet.getDefinition(Idx), Strings, iIndent + 2);
            for (Idx = 0; Idx <= subSet.iChildCount() - 1; Idx++)
                writeParamSet(subSet.getChild(Idx), Strings, "set", iIndent + 2);

            sLine = new string(' ', iIndent) + "</" + sElem + ">";
            if (!subSet.bRootNode() && (subSet.iChildCount() > 0))
                sLine += "<!-- " + subSet.sEnglishName + " -->";

            Strings.Add(sLine);
        }

        /// <summary>
        /// The strategy for obtaining default parameters is:                            
        /// 1. Attempt to read a base parameter set from a resource called sPrmID in the 
        ///    current module.                                                           
        /// </summary>
        /// <param name="sPrmID"></param>
        /// <param name="Params"></param>
        public void readDefaults(string sPrmID, ref TParameterSet Params)
        {
            readFromResource(sPrmID, ref Params, false);
            Params.sCurrLocale = GrazLocale.sDefaultLocale();
        }

        /// <summary>
        /// Read from internal resource
        /// </summary>
        /// <param name="sResID"></param>
        /// <param name="Params"></param>
        /// <param name="bModify"></param>
        public void readFromResource(string sResID, ref TParameterSet Params, bool bModify)
        {
            string paramStr = ReflectionUtilities.GetResourceAsString("Models.Resources.GrazPlan." + sResID);
            readFromXML(paramStr, ref Params, bModify);
        }

        private void readFromStream(StreamReader Stream, TParameterSet Params, bool bModify)
        {
            string sParamStr;

            sParamStr = Stream.ReadToEnd();
            readFromXML(sParamStr, ref Params, bModify);
        }

        /// <summary>
        /// Read the parameters from a file
        /// </summary>
        /// <param name="sFileName"></param>
        /// <param name="Params"></param>
        /// <param name="bModify"></param>
        public void readFromFile(string sFileName, TParameterSet Params, bool bModify)
        {
            StreamReader fStream = null;
            try
            {
                fStream = new StreamReader(sFileName);
                readFromStream(fStream, Params, bModify);
            }
            catch (Exception e)
            {
                if (fStream != null)
                    fStream = null;
                throw new Exception("Cannot load parameter data from \"" + sFileName + "\" \n\n" + e.Message);
            }
        }
        /// <summary>
        /// Parameter set as XML
        /// </summary>
        /// <param name="Params"></param>
        /// <returns></returns>
        public string sParamXML(TParameterSet Params)
        {
            List<string> Strings = new List<string>();

            writeParamSet(Params, Strings, "parameters", 0);
            return string.Join("\n", Strings.ToArray());
        }
    }
}