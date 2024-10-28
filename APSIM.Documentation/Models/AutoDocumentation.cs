using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using APSIM.Shared.Utilities;
using APSIM.Shared.Documentation;
using Models;
using Models.Core;
using Models.Functions;
using APSIM.Documentation.Models.Types;
using Models.PMF;
using Models.PMF.Phen;
using Models.Core.Run;
using Models.Storage;
using Models.PMF.Organs;
using Models.PMF.Struct;
using Models.Functions.DemandFunctions;
using Models.Factorial;
using Models.Functions.SupplyFunctions;
using Models.Functions.RootShape;
using Models.PMF.OilPalm;
using M = Models;
using Models.AgPasture;
using Models.Soils.Nutrients;

namespace APSIM.Documentation.Models
{

    /// <summary>
    /// A class of auto-documentation methods and HTML building widgets.
    /// </summary>
    public class AutoDocumentation
    {
        private static object lockObject = new object();

        /// <summary>Table relating member and summary xml comments.</summary>
        private static readonly Dictionary<string, string> summaries = new();

        /// <summary>Table relating member and remarks xml comments.</summary>
        private static readonly Dictionary<string, string> remarks = new();

        /// <summary>Flag for whether or not documentation has been loaded.</summary>
        private static bool initialized = false;

        /// <summary>Returns a dictionary to match model classes to document class.</summary>
        private static Dictionary<Type, Type> DefineFunctions()
        {
            Dictionary<Type, Type> documentMap = new()
            {
                {typeof(Plant), typeof(DocPlant)},
                {typeof(PastureSpecies), typeof(DocPlant)},
                {typeof(Sugarcane), typeof(DocPlant)},
                {typeof(Clock), typeof(DocClock)},
                {typeof(Simulation), typeof(DocGenericWithChildren)},
                {typeof(CalculateCarbonFractionFromNConc), typeof(DocBiomassArbitrationFunction)},
                {typeof(DeficitDemandFunction), typeof(DocBiomassArbitrationFunction)},
                {typeof(MobilisationSupplyFunction), typeof(DocBiomassArbitrationFunction)},
                {typeof(PlantPartitionFractions), typeof(DocBiomassArbitrationFunction)},
                {typeof(NutrientDemandFunctions), typeof(DocGenericWithChildren)},
                {typeof(NutrientPoolFunctions), typeof(DocGenericWithChildren)},
                {typeof(NutrientProportionFunctions), typeof(DocGenericWithChildren)},
                {typeof(NutrientSupplyFunctions), typeof(DocGenericWithChildren)},
                {typeof(Phenology), typeof(DocPhenology)},
                {typeof(Root), typeof(DocRoot)},
                {typeof(Cultivar), typeof(DocCultivar)},
                {typeof(Map), typeof(DocMap)},
                {typeof(BoundFunction), typeof(DocBoundFunction)},
                {typeof(Memo), typeof(DocMemo)},
                {typeof(Structure), typeof(DocStructure)},
                {typeof(Folder),typeof(DocFolder)},
                {typeof(LinearInterpolationFunction), typeof(DocLinearInterpolationFunction)},
                {typeof(HeightFunction), typeof(DocFunction)},
                {typeof(BudNumberFunction), typeof(DocFunction)},
                {typeof(ZadokPMFWheat), typeof(DocZadokPMFWheat)},
                {typeof(XYPairs), typeof(DocXYPairs)},
                {typeof(VernalisationPhase), typeof(DocPhase)},
                {typeof(SubDailyInterpolation), typeof(DocSubDailyInterpolation)},
                {typeof(StorageNDemandFunction), typeof(DocStorageNDemandFunction)},
                {typeof(StartPhase), typeof(DocPhase)},
                {typeof(PhotoperiodPhase), typeof(DocPhase)},
                {typeof(NodeNumberPhase), typeof(DocPhase)},
                {typeof(LeafDeathPhase), typeof(DocPhase)},
                {typeof(LeafAppearancePhase), typeof(DocPhase)},
                {typeof(GrazeAndRewind), typeof(DocPhase)},
                {typeof(GotoPhase), typeof(DocPhase)},
                {typeof(GerminatingPhase), typeof(DocPhase)},
                {typeof(GenericPhase), typeof(DocPhase)},
                {typeof(EndPhase), typeof(DocPhase)},
                {typeof(EmergingPhase), typeof(DocPhase)},
                {typeof(SorghumLeaf), typeof(DocSorghumLeaf)},
                {typeof(ReproductiveOrgan), typeof(DocReproductiveOrgan) },
                {typeof(PerennialLeaf), typeof(DocPerennialLeaf)},
                {typeof(Nodule), typeof(DocNodule)},
                {typeof(Leaf), typeof(DocLeaf)},
                {typeof(Manager), typeof(DocManager)},
                {typeof(Experiment), typeof(DocExperiment)},
                {typeof(FrostSenescenceFunction), typeof(DocFrostSenescenceFunction)},
                {typeof(RUEModel), typeof(DocGenericWithChildren)},
                {typeof(LeafCohortParameters), typeof(DocLeafCohortParameters)},
                {typeof(RUECO2Function), typeof(DocGenericWithChildren)},
                {typeof(RootShapeSemiCircle), typeof(DocGenericWithChildren)},
                {typeof(RootShapeCylinder), typeof(DocGenericWithChildren)},
                {typeof(RootShapeSemiEllipse), typeof(DocGenericWithChildren)},
                {typeof(RootShapeSemiCircleSorghum), typeof(DocGenericWithChildren)},
                {typeof(HIReproductiveOrgan), typeof(DocGenericWithChildren)},
                {typeof(BasialBuds), typeof(DocGenericWithChildren)},
                {typeof(OilPalm), typeof(DocPlant)},
                {typeof(GenericOrgan), typeof(DocGenericOrgan)},
                {typeof(WaterSenescenceFunction), typeof(DocWaterSenescenceFunction)},
                {typeof(CanopyGrossPhotosynthesisHourly), typeof(DocGenericWithChildren)},
                {typeof(CanopyPhotosynthesis), typeof(DocGenericWithChildren)},
                {typeof(LeafLightUseEfficiency), typeof(DocGenericWithChildren)},
                {typeof(LeafMaxGrossPhotosynthesis), typeof(DocGenericWithChildren)},
                {typeof(LimitedTranspirationRate), typeof(DocGenericWithChildren)},
                {typeof(StomatalConductanceCO2Modifier), typeof(DocGenericWithChildren)},
                {typeof(BiomassDemand), typeof(DocGenericWithChildren)},
                {typeof(BiomassDemandAndPriority), typeof(DocGenericWithChildren)},
                {typeof(EnergyBalance), typeof(DocGenericWithChildren)},
                {typeof(Alias), typeof(DocAlias)},
                {typeof(Simulations), typeof(DocSimulations)},
                {typeof(M.Graph), typeof(DocGraph)},
                {typeof(Nutrient), typeof(DocNutrient)},
            };
            return documentMap;
        }

        /// <summary>Writes the description of a class to the tags.</summary>
        /// <param name="model">The model to get documentation for.</param>
        public static List<ITag> Document(IModel model)
        {
            List<ITag> newTags;
            newTags = AutoDocumentation.DocumentModel(model);
            newTags = DocumentationUtilities.CleanEmptySections(newTags);
            newTags = DocumentationUtilities.AddHeader(model.Name, newTags);
            return newTags;
        }

        /// <summary>Writes the description of a class to the tags.</summary>
        /// <param name="model">The model to get documentation for.</param>
        public static List<ITag> DocumentModel(IModel model)
        {
            List<ITag> newTags;

            DefineFunctions().TryGetValue(model.GetType(), out Type docType);

            if (docType != null) 
            {
                object documentClass = Activator.CreateInstance(docType, new object[]{model});
                newTags = (documentClass as DocGeneric).Document(0);
            }
            else if (docType == null && model as IFunction != null)
            {
                newTags = new DocFunction(model).Document(0);
            }
            else
            {
                newTags = new DocGeneric(model).Document(0);
            }
            return newTags;
        }

        /// <summary>Documents the specified model.</summary>
        /// <param name="rootModel">The model this model is held in.</param>
        /// <param name="modelNameToDocument">The model name to document.</param>
        /// <param name="tags">The auto doc tags.</param>
        /// <param name="headingLevel">The starting heading level.</param>
        public void DocumentModel2(IModel rootModel, string modelNameToDocument, List<ITag> tags, int headingLevel)
        {
            //This was in my stash, no idea where the function is that was using this in models.
            Simulation simulation = rootModel.FindInScope<Simulation>();
            if (simulation != null)
            {
                // Find the model of the right name.
                IModel modelToDocument = simulation.FindInScope(modelNameToDocument);

                // If not found then find a model of the specified type.
                if (modelToDocument == null)
                    modelToDocument = simulation.FindByPath("[" + modelNameToDocument + "]")?.Value as IModel;

                // If the simulation has the same name as the model we want to document, dig a bit deeper
                if (modelToDocument == simulation)
                    modelToDocument = simulation.FindAllDescendants().Where(m => !m.IsHidden).ToList().FirstOrDefault(m => m.Name.Equals(modelNameToDocument, StringComparison.OrdinalIgnoreCase));

                // If still not found throw an error.
                if (modelToDocument != null)
                {
                    // Get the path of the model (relative to parentSimulation) to document so that 
                    // when replacements happen below we will point to the replacement model not the 
                    // one passed into this method.
                    string pathOfSimulation = simulation.FullPath + ".";
                    string pathOfModelToDocument = modelToDocument.FullPath.Replace(pathOfSimulation, "");

                    // Clone the simulation
                    SimulationDescription simDescription = new SimulationDescription(simulation);

                    Simulation clonedSimulation = simDescription.ToSimulation();

                    // Prepare the simulation for running - this perform misc cleanup tasks such
                    // as removing disabled models, standardising the soil, resolving links, etc.
                    clonedSimulation.Prepare();
                    rootModel.FindInScope<IDataStore>().Writer.Stop();
                    // Now use the path to get the model we want to document.
                    modelToDocument = clonedSimulation.FindByPath(pathOfModelToDocument)?.Value as IModel;

                    if (modelToDocument == null)
                        throw new Exception("Cannot find model to document: " + modelNameToDocument);

                    //Get the simulations to do linking with
                    Simulations sims = simulation.FindAncestor<Simulations>();

                    // resolve all links in cloned simulation.
                    sims.Links.Resolve(clonedSimulation, true);

                    // Document the model.
                    AutoDocumentation.DocumentModel(modelToDocument);

                    // Unresolve links.
                    sims.Links.Unresolve(clonedSimulation, true);
                }
            }
        }

        /// <summary>Gets the units from a declaraion.</summary>
        /// <param name="model">The model containing the declaration field.</param>
        /// <param name="fieldName">The declaration field name.</param>
        /// <returns>The units (no brackets) or any empty string.</returns>
        public static string GetUnits(IModel model, string fieldName)
        {
            if (model == null || string.IsNullOrEmpty(fieldName))
                return string.Empty;
            FieldInfo field = model.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (field != null)
            {
                UnitsAttribute unitsAttribute = ReflectionUtilities.GetAttribute(field, typeof(UnitsAttribute), false) as UnitsAttribute;
                if (unitsAttribute != null)
                    return unitsAttribute.ToString();
            }

            PropertyInfo property = model.GetType().GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property != null)
            {
                UnitsAttribute unitsAttribute = ReflectionUtilities.GetAttribute(property, typeof(UnitsAttribute), false) as UnitsAttribute;
                if (unitsAttribute != null)
                    return unitsAttribute.ToString();
            }
            // Didn't find untis - try parent.
            if (model.Parent != null)
                return GetUnits(model.Parent, model.Name);
            else
                return string.Empty;
        }

        /// <summary>Gets the description from a declaraion.</summary>
        /// <param name="model">The model containing the declaration field.</param>
        /// <param name="fieldName">The declaration field name.</param>
        /// <returns>The description or any empty string.</returns>
        public static string GetDescription(IModel model, string fieldName)
        {
            if (model == null || string.IsNullOrEmpty(fieldName))
                return string.Empty;
            FieldInfo field = model.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                DescriptionAttribute descriptionAttribute = ReflectionUtilities.GetAttribute(field, typeof(DescriptionAttribute), false) as DescriptionAttribute;
                if (descriptionAttribute != null)
                    return descriptionAttribute.ToString();
            }

            return string.Empty;
        }
        /*
        /// <summary>
        /// Document the summary description of a model.
        /// </summary>
        /// <param name="model">The model to get documentation for.</param>
        /// <param name="tags">The tags to add to.</param>
        /// <param name="headingLevel">The heading level to use.</param>
        /// <param name="indent">The indentation level.</param>
        /// <param name="documentAllChildren">Document all children?</param>
        public static void DocumentModelSummary(IModel model, List<ITag> tags, int headingLevel, int indent, bool documentAllChildren)
        {
            if (model == null)
                return;

            if (!initialized)
                InitialiseDoc();

            var summaryText = GetSummaryRaw(model.GetType().FullName.Replace("+", "."), 'T');
            if (summaryText != null)
                ParseTextForTags(summaryText, model, tags, headingLevel, indent, documentAllChildren);
        }
        */
        /// <summary>
        /// Get the summary of a member (field, property)
        /// </summary>
        /// <param name="member">The member to get the summary for.</param>
        public static string GetSummary(MemberInfo member)
        {
            var fullName = member.ReflectedType + "." + member.Name;
            if (member is PropertyInfo)
                return GetSummary(fullName, 'P');
            else if (member is FieldInfo)
                return GetSummary(fullName, 'F');
            else if (member is EventInfo)
                return GetSummary(fullName, 'E');
            else if (member is MethodInfo method)
            {
                string args = string.Join(",", method.GetParameters().Select(p => p.ParameterType.FullName));
                args = args.Replace("+", ".");
                return GetSummary($"{fullName}({args})", 'M');
            }
            else
                throw new ArgumentException($"Unknown argument type {member.GetType().Name}");
        }

        /// <summary>
        /// Get the summary of a type removing CRLF.
        /// </summary>
        /// <param name="t">The type to get the summary for.</param>
        public static string GetSummary(Type t)
        {
            return GetSummary(t.FullName, 'T');
        }

        /// <summary>
        /// Get the summary of a type without removing CRLF.
        /// </summary>
        /// <param name="t">The type to get the summary for.</param>
        public static string GetSummaryRaw(Type t)
        {
            return GetSummaryRaw(t.FullName, 'T');
        }

        /// <summary>
        /// Get the remarks tag of a type (if it exists).
        /// </summary>
        /// <param name="t">The type.</param>
        public static string GetRemarks(Type t)
        {
            return GetRemarks(t.FullName, 'T');
        }

        /// <summary>
        /// Get the remarks of a member (field, property) if it exists.
        /// </summary>
        /// <param name="member">The member.</param>
        public static string GetRemarks(MemberInfo member)
        {
            var fullName = member.ReflectedType + "." + member.Name;
            if (member is PropertyInfo)
                return GetRemarks(fullName, 'P');
            else if (member is FieldInfo)
                return GetRemarks(fullName, 'F');
            else if (member is EventInfo)
                return GetRemarks(fullName, 'E');
            else if (member is MethodInfo method)
            {
                string args = string.Join(",", method.GetParameters().Select(p => p.ParameterType.FullName));
                args = args.Replace("+", ".");
                return GetRemarks($"{fullName}({args})", 'M');
            }
            else
                throw new ArgumentException($"Unknown argument type {member.GetType().Name}");
        }

        /// <summary>
        /// Initialise the doc instance.
        /// </summary>
        private static void InitialiseDoc()
        {
            lock (lockObject)
            {
                if (!initialized)
                {
                    string fileName = Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".xml");
                    var doc = XDocument.Load(fileName);
                    foreach (var elt in doc.Element("doc").Element("members").Elements())
                    {
                        var name = elt.Attribute("name")?.Value;
                        if (name != null)
                        {
                            var summary = elt.Element("summary")?.Value.Trim();
                            if (summary != null)
                                summaries[name] = summary;
                            var remark = elt.Element("remarks")?.Value.Trim();
                            if (remark != null)
                                remarks[name] = remark;
                        }
                    }
                    initialized = true;
                }
            }
        }

        /// <summary>
        /// Get the summary of a member (class, field, property)
        /// </summary>
        /// <param name="path">The path to the member.</param>
        /// <param name="typeLetter">Type type letter: 'T' for type, 'F' for field, 'P' for property.</param>
        private static string GetSummary(string path, char typeLetter)
        {
            var rawSummary = GetSummaryRaw(path, typeLetter);
            if (rawSummary != null)
            {
                // Need to fix multiline comments - remove newlines and consecutive spaces.
                return Regex.Replace(rawSummary, @"\n[ \t]+", "\n");
            }
            return null;
        }

        /// <summary>
        /// Get the summary of a member (class, field, property)
        /// </summary>
        /// <param name="path">The path to the member.</param>
        /// <param name="typeLetter">Type type letter: 'T' for type, 'F' for field, 'P' for property.</param>
        private static string GetSummaryRaw(string path, char typeLetter)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            if (!initialized)
                InitialiseDoc();

            path = path.Replace("+", ".");

            if (summaries.TryGetValue($"{typeLetter}:{path}", out var summary))
                return summary;
            return null;
        }

        /// <summary>
        /// Get the remarks of a member (class, field, property).
        /// </summary>
        /// <param name="path">The path to the member.</param>
        /// <param name="typeLetter">Type letter: 'T' for type, 'F' for field, 'P' for property.</param>
        /// <returns></returns>
        private static string GetRemarks(string path, char typeLetter)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            if (!initialized)
                InitialiseDoc();

            path = path.Replace("+", ".");

            if (remarks.TryGetValue($"{typeLetter}:{path}", out var remark))
            {
                // Need to fix multiline remarks - trim newlines and consecutive spaces.
                return Regex.Replace(remark, @"\n\s+", "\n");
            }
            return null;
        }
        /*
        /// <summary>
        /// Parse a string into documentation tags
        /// </summary>
        /// <param name="stringToParse">The string to parse</param>
        /// <param name="model">The associated model where the string came from</param>
        /// <param name="tags">The list of tags to add to</param>
        /// <param name="headingLevel">The current heading level</param>
        /// <param name="indent">The current indent level</param>
        /// <param name="doNotTrim">If true, don't trim the lines</param>
        /// <param name="documentAllChildren">Ensure all children are documented?</param>
        public static void ParseTextForTags(string stringToParse, IModel model, List<ITag> tags, int headingLevel, int indent, bool documentAllChildren, bool doNotTrim = false)
        {
            if (string.IsNullOrEmpty(stringToParse) || model == null)
                return;
            List<IModel> childrenDocumented = new List<IModel>();
            int numSpacesStartOfLine = -1;
            string paragraphSoFar = string.Empty;
            if (stringToParse.StartsWith("\r\n"))
                stringToParse = stringToParse.Remove(0, 2);
            StringReader reader = new StringReader(stringToParse);
            string line = reader.ReadLine();
            int targetHeadingLevel = headingLevel;
            while (line != null)
            {
                if (!doNotTrim)
                    line = line.Trim();

                // Adjust heading levels.
                if (line.StartsWith("#"))
                {
                    int currentHeadingLevel = line.Count(c => c == '#');
                    targetHeadingLevel = headingLevel + currentHeadingLevel - 1; // assumes models start numbering headings at 1 '#' character
                    string hashString = new string('#', targetHeadingLevel);
                    line = hashString + line.Replace("#", "") + hashString;
                }

                if (line != string.Empty && !doNotTrim)
                {
                    {
                        if (numSpacesStartOfLine == -1)
                        {
                            int preLineLength = line.Length;
                            line = line.TrimStart();
                            numSpacesStartOfLine = preLineLength - line.Length - 1;
                        }
                        else
                            line = line.Remove(0, numSpacesStartOfLine);
                    }
                }

                if (line.StartsWith("[DocumentMathFunction"))
                {
                    StoreParagraphSoFarIntoTags(tags, indent, ref paragraphSoFar);
                    var operatorChar = line["[DocumentMathFunction".Length + 1];
                    childrenDocumented.AddRange(DocumentMathFunction(model, operatorChar, tags, headingLevel, indent));
                }
                else
                {
                    // Remove expression macros and replace with values.
                    line = RemoveMacros(model, line);

                    string heading;
                    int thisHeadingLevel;
                    if (GetHeadingFromLine(line, out heading, out thisHeadingLevel))
                    {
                        StoreParagraphSoFarIntoTags(tags, indent, ref paragraphSoFar);
                        tags.Add(new Heading(heading, thisHeadingLevel));
                    }
                    else if (line.StartsWith("[Document "))
                    {
                        StoreParagraphSoFarIntoTags(tags, indent, ref paragraphSoFar);

                        // Find child
                        string childName = line.Replace("[Document ", "").Replace("]", "");
                        IModel child = model.FindByPath(childName)?.Value as IModel;
                        if (child == null)
                            paragraphSoFar += "<b>Unknown child name: " + childName + " </b>\r\n";
                        else
                        {
                            Document(child, targetHeadingLevel + 1);
                            childrenDocumented.Add(child);
                        }
                    }
                    else if (line.StartsWith("[DocumentType "))
                    {
                        StoreParagraphSoFarIntoTags(tags, indent, ref paragraphSoFar);

                        // Find children
                        string childTypeName = line.Replace("[DocumentType ", "").Replace("]", "");
                        Type childType = ReflectionUtilities.GetTypeFromUnqualifiedName(childTypeName);
                        foreach (IModel child in model.FindAllChildren().Where(c => childType.IsAssignableFrom(c.GetType())))
                        {
                            Document(child, targetHeadingLevel + 1);
                            childrenDocumented.Add(child);
                        }
                    }
                    else if (line == "[DocumentView]")
                        tags.Add(new ModelView(model));
                    else if (line.StartsWith("[DocumentChart "))
                    {
                        StoreParagraphSoFarIntoTags(tags, indent, ref paragraphSoFar);
                        var words = line.Replace("[DocumentChart ", "").Split(',');
                        if (words.Length == 4)
                        {
                            var xypairs = model.FindByPath(words[0])?.Value as XYPairs;
                            if (xypairs != null)
                            {
                                childrenDocumented.Add(xypairs);
                                var xName = words[2];
                                var yName = words[3].Replace("]", "");
                                //tags.Add(new GraphAndTable(xypairs, words[1], xName, yName, indent));
                            }
                        }
                    }
                    else if (line.StartsWith("[DontDocument"))
                    {
                        string childName = line.Replace("[DontDocument ", "").Replace("]", "");
                        IModel child = model.FindByPath(childName)?.Value as IModel;
                        if (childName != null)
                            childrenDocumented.Add(child);
                    }
                    else
                        paragraphSoFar += line + "\r\n";
                }
                line = reader.ReadLine();
            }

            StoreParagraphSoFarIntoTags(tags, indent, ref paragraphSoFar);

            if (documentAllChildren)
            {
                // write children.
                foreach (IModel child in model.FindAllChildren<IModel>())
                {
                    if (!childrenDocumented.Contains(child))
                        Document(child, headingLevel + 1);
                }
            }
        }
        
        private static string RemoveMacros(IModel model, string line)
        {
            if (model == null || string.IsNullOrEmpty(line))
                return string.Empty;
            int posMacro = line.IndexOf('[');
            while (posMacro != -1)
            {
                int posEndMacro = line.IndexOf(']', posMacro);
                if (posEndMacro != -1)
                {
                    string macro = line.Substring(posMacro + 1, posEndMacro - posMacro - 1);
                    try
                    {
                        object value = EvaluateModelPath(model, macro);
                        if (value != null)
                        {
                            if (value is Array)
                                value = StringUtilities.Build(value as Array, $"{Environment.NewLine}{Environment.NewLine}");

                            line = line.Remove(posMacro, posEndMacro - posMacro + 1);
                            line = line.Insert(posMacro, value.ToString());
                        }
                    }
                    catch (Exception)
                    {
                    }
                }

                if (line == "")
                    posMacro = -1;
                else if (posMacro < line.Length)
                    posMacro = line.IndexOf('[', posMacro + 1);

                if (string.IsNullOrEmpty(line))
                    break;
            }

            return line;
        }
        
        /// <summary>
        /// Evaluate a path that can include child models, properties or method calls.
        /// </summary>
        /// <param name="model">The reference model.</param>
        /// <param name="path">The path to locate</param>
        private static object EvaluateModelPath(IModel model, string path)
        {
            object obj = model;
            foreach (var word in path.Split('.'))
            {
                if (obj == null)
                    return null;
                if (word.EndsWith("()"))
                {
                    // Process a method (with no arguments) call.
                    // e.g. GetType()
                    var methodName = word.Replace("()", "");
                    var method = obj.GetType().GetMethod(methodName);
                    if (method != null)
                        obj = method.Invoke(obj, null);
                }
                else if (obj is IModel && word == "Units")
                    obj = GetUnits(model, model.Name);
                else if (obj is IModel)
                {
                    // Process a child or property of a model.
                    obj = (obj as IModel).FindByPath(word, LocatorFlags.None)?.Value;
                }
                else
                {
                    // Process properties / fields of an object (not an IModel)
                    obj = ReflectionUtilities.GetValueOfFieldOrProperty(word, obj);
                }
            }

            return obj;
        }
        
        private static void StoreParagraphSoFarIntoTags(List<ITag> tags, int indent, ref string paragraphSoFar)
        {
            if (paragraphSoFar.Trim() != string.Empty)
                tags.Add(new Paragraph(paragraphSoFar, indent));
            paragraphSoFar = string.Empty;
        }

        /// <summary>Look at a string and return true if it is a heading.</summary>
        /// <param name="st">The string to look at.</param>
        /// <param name="heading">The returned heading.</param>
        /// <param name="headingLevel">The returned heading level.</param>
        /// <returns></returns>
        private static bool GetHeadingFromLine(string st, out string heading, out int headingLevel)
        {
            if (string.IsNullOrEmpty(st))
            {
                heading = string.Empty;
                headingLevel = 0;
                return false;
            }

            heading = st.Replace("#", string.Empty);
            headingLevel = 0;
            if (st.StartsWith("####"))
            {
                headingLevel = 4;
                return true;
            }
            if (st.StartsWith("###"))
            {
                headingLevel = 3;
                return true;
            }
            if (st.StartsWith("##"))
            {
                headingLevel = 2;
                return true;
            }
            if (st.StartsWith("#"))
            {
                headingLevel = 1;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Document all child members of the specified model.
        /// </summary>
        /// <param name="model">The parent model</param>
        /// <param name="tags">Documentation elements</param>
        /// <param name="headingLevel">Heading level</param>
        /// <param name="indent">Indent level</param>
        /// <param name="childTypesToExclude">An optional list of Types to exclude from documentation.</param>
        public static void DocumentChildren(IModel model, List<ITag> tags, int headingLevel, int indent, Type[] childTypesToExclude = null)
        {
            if (model == null)
                return;
            foreach (IModel child in model.Children)
                if (//child.IncludeInDocumentation &&
                    (childTypesToExclude == null || Array.IndexOf(childTypesToExclude, child.GetType()) == -1))
                    Document(child, headingLevel + 1);
        }

        /// <summary>
        /// Document the mathematical function.
        /// </summary>
        /// <param name="function">The IModel function.</param>
        /// <param name="op">The operator</param>
        /// <param name="tags">The tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        private static List<IModel> DocumentMathFunction(IModel function, char op,
                                                         List<ITag> tags, int headingLevel, int indent)
        {
            // create a string to display 'child1 - child2 - child3...'
            string msg = string.Empty;
            List<IModel> childrenToDocument = new List<IModel>();
            foreach (IModel child in function.FindAllChildren<IFunction>())
            {
                if (msg != string.Empty)
                    msg += " " + op + " ";

                if (!AddChildToMsg(child, ref msg))
                    childrenToDocument.Add(child);
            }

            tags.Add(new Paragraph("<i>" + function.Name + " = " + msg + "</i>", indent));

            // write children
            if (childrenToDocument.Count > 0)
            {
                tags.Add(new Paragraph("Where:", indent));

                foreach (IModel child in childrenToDocument)
                    Document(child, headingLevel + 1);
            }

            return childrenToDocument;
        }

        /// <summary>
        /// Return the name of the child or it's value if the name of the child is equal to
        /// the written value of the child. i.e. if the value is 1 and the name is 'one' then
        /// return the value, instead of the name.
        /// </summary>
        /// <param name="child">The child model.</param>
        /// <param name="msg">The message to add to.</param>
        /// <returns>True if child's value was added to msg.</returns>
        private static bool AddChildToMsg(IModel child, ref string msg)
        {
            if (child is Constant)
            {
                double doubleValue = (child as Constant).FixedValue;
                if (Math.IEEERemainder(doubleValue, doubleValue) == 0)
                {
                    int intValue = Convert.ToInt32(doubleValue, CultureInfo.InvariantCulture);
                    string writtenInteger = Integer.ToWritten(intValue);
                    writtenInteger = writtenInteger.Replace(" ", "");  // don't want spaces.
                    if (writtenInteger.Equals(child.Name, StringComparison.CurrentCultureIgnoreCase))
                    {
                        msg += intValue.ToString();
                        return true;
                    }
                }
            }
            else if (child is VariableReference)
            {
                msg += StringUtilities.RemoveTrailingString((child as VariableReference).VariableName, ".Value()");
                return true;
            }

            msg += child.Name;
            return false;
        }
        */
    }
    
}
