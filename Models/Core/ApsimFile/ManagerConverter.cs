namespace Models.Core.ApsimFile
{
    using APSIM.Shared.Utilities;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System;
    using Newtonsoft.Json.Linq;
    using System.Text;
    using Models.Core.Script;

    /// <summary>
    /// Provides helper methods to read and manipulate manager scripts.
    /// </summary>
    public class ManagerConverter
    {
        /// <summary>
        /// The Json token.
        /// </summary>
        public JObject Token { get; private set; }

        /// <summary>Default constructor.</summary>
        public ManagerConverter() { }

        /// <summary>The instance responsible for parsing the c# script.</summary>
        public ScriptParser Parser { get; private set; }

        /// <summary>
        /// Parameters (public properties with a display attribute) of the manager script.
        /// </summary>
        public Dictionary<string, string> Parameters
        {
            get
            {
                Dictionary<string, string> parameters = new Dictionary<string, string>();
                if (Token["Parameters"] == null)
                    return parameters;

                foreach (var parameter in Token["Parameters"])
                    parameters.Add(parameter["Key"].ToString(), parameter["Value"].ToString());
                return parameters;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="manager">The JSON manager object.</param>
        public ManagerConverter(JObject manager)
        {
            this.Token = manager;
            if (manager["Code"] != null)
                Read(manager["Code"].ToString());
            else
                Parser = new ScriptParser("");
        }

        /// <summary>Load script</summary>
        /// <param name="script">The manager script to work on</param>
        public void Read(string script)
        {
            Parser = new ScriptParser(script);
        }

        /// <summary>Load script</summary>
        /// <param name="node">The manager node to read from</param>
        public void Read(XmlNode node)
        {
            XmlCDataSection codeNode = XmlUtilities.Find(node, "Code").ChildNodes[0] as XmlCDataSection;
            Read(codeNode.InnerText);
        }

        /// <summary>Write script</summary>
        /// <param name="node">The manager node to write to</param>
        public void Write(XmlNode node)
        {
            XmlCDataSection codeNode = XmlUtilities.Find(node, "Code").ChildNodes[0] as XmlCDataSection;
            codeNode.InnerText = Parser.ToString();
        }

        /// <summary>Return all code</summary>
        public new string ToString()
        {
            return Parser?.ToString();
        }

        /// <summary>
        /// Save the manager object code back to the manager JSON object.
        /// </summary>
        public void Save()
        {
            Token["Code"] = ToString();
        }

        /// <summary>
        /// Changes the value of a parameter with a given key.
        /// </summary>
        /// <param name="key">Key of the paramter.</param>
        /// <param name="newParam">New value of the parameter.</param>
        public void UpdateParameter(string key, string newParam)
        {
            foreach (var parameter in Token["Parameters"].Children())
                if (parameter["Key"].ToString() == key)
                    parameter["Value"] = newParam;
                    //return;
        }

        /// <summary>
        /// Change manager to reflect moving of variables from one object to another e.g. from Soil to IPhysical.
        /// </summary>
        /// <param name="variablesToMove">The names of variables to move.</param>
        /// <returns>True if changes were made.</returns>
        public bool MoveVariables(ManagerReplacement[] variablesToMove)
        {
            var declarations = Parser.GetDeclarations();

            bool replacementMade = false;
            foreach (var variableToMove in variablesToMove)
            {
                var tokens = variableToMove.OldName.Split('.');
                if (tokens.Length != 2)
                    throw new Exception($"Invalid old variale name found {variableToMove.OldName}");
                var oldTypeName = tokens[0];
                var oldInstanceName = tokens[1];

                var pattern = $@"(\w+)\.{oldInstanceName}(\W+)";
                Parser.ReplaceRegex(pattern, match =>
                {
                    // Check the type of the variable to see if it is soil.
                    var soilInstanceName = match.Groups[1].Value;
                    var matchDeclaration = declarations.Find(decl => decl.InstanceName == soilInstanceName);
                    if (matchDeclaration == null || (matchDeclaration.TypeName != oldTypeName && !matchDeclaration.TypeName.EndsWith($".{oldTypeName}")))
                        return match.Groups[0].Value; // Don't change anything as the type isn't a match.

                    replacementMade = true;

                    tokens = variableToMove.NewName.Split('.');
                    string newInstanceName = null;
                    string newVariableName = null;
                    if (tokens.Length >= 1)
                        newInstanceName = tokens[0];
                    if (tokens.Length == 2)
                        newVariableName = tokens[1];

                    // Found a variable that needs renaming. 
                    // See if there is an instance varialbe of the correct type.If not add one.
                    var declaration = declarations.Find(decl => decl.TypeName == variableToMove.NewInstanceTypeName);
                    if (declaration == null)
                    {
                        declaration = new ScriptParser.Declaration()
                        {
                            TypeName = variableToMove.NewInstanceTypeName,
                            InstanceName = newInstanceName,
                            IsPrivate = true
                        };
                        declarations.Add(declaration);
                    }

                    if (!declaration.Attributes.Contains("[Link]"))
                        declaration.Attributes.Add("[Link]");

                    if (newVariableName == null)
                        return $"{declaration.InstanceName}{match.Groups[2].Value}";
                    else
                        return $"{declaration.InstanceName}.{newVariableName}{match.Groups[2].Value}";
                });
            }
            if (replacementMade)
                Parser.SetDeclarations(declarations);
            return replacementMade;
        }
    }

    /// <summary>
    /// Encapsulates a management replacement.
    /// </summary>
    public class ManagerReplacement
    {
        /// <summary>The old variable name.</summary>
        public string OldName { get; set; }

        /// <summary>The new variable name.</summary>
        public string NewName { get; set; }

        /// <summary>The type of the new instance variable..</summary>
        public string NewInstanceTypeName { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="searchFor"></param>
        /// <param name="replaceWith"></param>
        /// <param name="typeName"></param>
        public ManagerReplacement(string searchFor, string replaceWith, string typeName)
        {
            OldName = searchFor;
            NewName = replaceWith;
            NewInstanceTypeName = typeName;
        }
    }
}


