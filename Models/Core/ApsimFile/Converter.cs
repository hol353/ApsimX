namespace Models.Core.ApsimFile
{
    using APSIM.Shared.Utilities;
    using Models.Climate;
    using Models.Factorial;
    using Models.Functions;
    using Models.LifeCycle;
    using Models.PMF;
    using Models.Soils;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Xml;

    /// <summary>
    /// Converts the .apsim file from one version to the next
    /// </summary>
    public class Converter
    {
        /// <summary>Gets the latest .apsimx file format version.</summary>
        public static int LatestVersion { get { return 126; } }

        /// <summary>Converts a .apsimx string to the latest version.</summary>
        /// <param name="st">XML or JSON string to convert.</param>
        /// <param name="toVersion">The optional version to convert to.</param>
        /// <param name="fileName">The optional filename where the string came from.</param>
        /// <returns>Returns true if something was changed.</returns>
        public static ConverterReturnType DoConvert(string st, int toVersion = -1, string fileName = null)
        {
            ConverterReturnType returnData = new ConverterReturnType();

            if (toVersion == -1)
                toVersion = LatestVersion;

            int offset = st.TakeWhile(c => char.IsWhiteSpace(c)).Count();
            char firstNonBlankChar = st[offset];

            if (firstNonBlankChar == '<')
            {
                bool changed = XmlConverters.DoConvert(ref st, Math.Min(toVersion, XmlConverters.LastVersion), fileName);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(st);
                int fileVersion = Convert.ToInt32(XmlUtilities.Attribute(doc.DocumentElement, "Version"), CultureInfo.InvariantCulture);
                if (fileVersion == toVersion)
                    return new ConverterReturnType()
                    { DidConvert = changed, RootXml = doc };

                st = ConvertToJSON(st, fileName);
                returnData.Root = JObject.Parse(st);
            }
            else if (firstNonBlankChar == '{')
            {
                // json
                returnData.Root = JObject.Parse(st);
            }
            else
            {
                throw new Exception("Unknown string encountered. Not JSON or XML. String: " + st);
            }

            if (returnData.Root.ContainsKey("Version"))
            {
                int fileVersion = (int)returnData.Root["Version"];

                if (fileVersion > LatestVersion)
                    throw new Exception(string.Format("Unable to open file '{0}'. File version is greater than the latest file version. Has this file been opened in a more recent version of Apsim?", fileName));

                // Run converters if not at the latest version.
                while (fileVersion < toVersion)
                {
                    returnData.DidConvert = true;

                    // Find the method to call to upgrade the file by one version.
                    int versionFunction = fileVersion + 1;
                    MethodInfo method = typeof(Converter).GetMethod("UpgradeToVersion" + versionFunction, BindingFlags.NonPublic | BindingFlags.Static);
                    if (method == null)
                        throw new Exception("Cannot find converter to go to version " + versionFunction);

                    // Found converter method so call it.
                    method.Invoke(null, new object[] { returnData.Root, fileName });

                    fileVersion++;
                }

                if (returnData.DidConvert)
                {
                    returnData.Root["Version"] = fileVersion;
                    st = returnData.Root.ToString();
                }
            }
            returnData.DidConvert = EnsureSoilHasInitWaterAndSample(returnData.Root) || returnData.DidConvert;

            return returnData;
        }

        /// <summary>
        /// If root is a soil then make sure it has a sample or init water.
        /// </summary>
        /// <param name="root">The root node of the JSON to look at.</param>
        /// <returns>True if model was changed.</returns>
        private static bool EnsureSoilHasInitWaterAndSample(JObject root)
        {
            string rootType = JsonUtilities.Type(root, true);

            if (rootType != null && rootType == "Models.Soils.Soil")
            {
                JArray soilChildren = root["Children"] as JArray;
                if (soilChildren != null && soilChildren.Count > 0)
                {
                    var initWater = soilChildren.FirstOrDefault(c => c["$type"].Value<string>().Contains(".InitWater"));
                    if (initWater == null)
                        initWater = soilChildren.FirstOrDefault(c => c["$type"].Value<string>().Contains(".InitialWater"));
                    var sample = soilChildren.FirstOrDefault(c => c["$type"].Value<string>().Contains(".Sample"));

                    bool res = false;
                    if (initWater == null)
                    {
                        // Add in an initial water and initial conditions models.
                        initWater = new JObject();
                        initWater["$type"] = "Models.Soils.InitialWater, Models";
                        JsonUtilities.RenameModel(initWater as JObject, "Initial water");
                        initWater["PercentMethod"] = "FilledFromTop";
                        initWater["FractionFull"] = 1;
                        initWater["DepthWetSoil"] = "NaN";
                        soilChildren.Add(initWater);
                        res = true;
                    }
                    if (sample == null)
                    {
                        sample = new JObject();
                        sample["$type"] = "Models.Soils.Sample, Models";
                        JsonUtilities.RenameModel(sample as JObject, "Initial conditions");
                        sample["Thickness"] = new JArray(new double[] { 1800 });
                        sample["NO3N"] = new JArray(new double[] { 3 });
                        sample["NH4"] = new JArray(new double[] { 1 });
                        sample["SWUnits"] = "Volumetric";
                        soilChildren.Add(sample);
                        res = true;
                    }
                    return res;
                }
            }

            return false;
        }

        /// <summary>Upgrades to version 47 - the first JSON version.</summary>
        private static string ConvertToJSON(string st, string fileName)
        {
            string json = XmlToJson.Convert(st);
            JObject j = JObject.Parse(json);
            j["Version"] = 47;
            return j.ToString();
        }

        private static void UpgradeToVersion47(JObject root, string fileName)
        {
            // Nothing to do as conversion to JSON has already happened.
        }

        /// <summary>
        /// Upgrades to version 48. Iterates through all manager scripts, and replaces
        /// all instances of the text "DisplayTypeEnum" with "DisplayType".
        /// </summary>
        /// <param name="root"></param>
        /// <param name="fileName"></param>
        private static void UpgradeToVersion48(JObject root, string fileName)
        {
            foreach (JObject manager in JsonUtilities.ChildrenRecursively(root, "Manager"))
                JsonUtilities.ReplaceManagerCode(manager, "DisplayTypeEnum", "DisplayType");
        }


        /// <summary>
        /// Upgrades to version 49. Renames Models.Morris+Parameter to Models.Sensitivity.Parameter.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="fileName"></param>
        private static void UpgradeToVersion49(JObject root, string fileName)
        {
            foreach (JObject morris in JsonUtilities.ChildrenRecursively(root, "Models.Morris"))
                foreach (var parameter in morris["Parameters"])
                    parameter["$type"] = parameter["$type"].ToString().Replace("Models.Morris+Parameter", "Models.Sensitivity.Parameter");
        }

        ///<summary>
        /// Upgrades to version 50. Fixes the RelativeTo property of 
        /// InitialWater components of soils copied from Apsim Classic.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="fileName"></param>
        /// <remarks>
        /// ll15 must be renamed to LL15.
        /// Wheat must be renamed to WheatSoil.
        /// Maize must be renamed to MaizeSoil.
        /// </remarks>
        private static void UpgradeToVersion50(JObject root, string fileName)
        {
            foreach (JObject initialWater in JsonUtilities.ChildrenRecursively(root, "InitialWater"))
            {
                if (initialWater["RelativeTo"] != null)
                {
                    if (initialWater["RelativeTo"].ToString().ToUpper().Contains("LL15"))
                        initialWater["RelativeTo"] = initialWater["RelativeTo"].ToString().Replace("ll15", "LL15");
                    else if (!string.IsNullOrEmpty(initialWater["RelativeTo"].ToString()) && !initialWater["RelativeTo"].ToString().EndsWith("Soil"))
                        initialWater["RelativeTo"] = initialWater["RelativeTo"].ToString() + "Soil";
                }
            }
        }
        /// <summary>
        /// Changes GsMax to Gsmax350 in all models that implement ICanopy.
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion51(JObject root, string fileName)
        {
            // Create a list of models that might have gsmax.
            // Might need to add in other models that implement ICanopy 
            // e.g. OilPalm, AgPastureSpecies, SimpleTree, Sugarcane

            var models = new List<JObject>();
            models.AddRange(JsonUtilities.ChildrenOfType(root, "Leaf"));
            models.AddRange(JsonUtilities.ChildrenOfType(root, "SimpleLeaf"));
            models.AddRange(JsonUtilities.ChildrenOfType(root, "PerennialLeaf"));
            models.AddRange(JsonUtilities.ChildrenOfType(root, "SorghumLeaf"));

            // Loop through all models and rename Gsmax to Gsmax350.
            foreach (var model in models)
            {
                JsonUtilities.RenameProperty(model, "Gsmax", "Gsmax350");
                JsonUtilities.AddConstantFunctionIfNotExists(model, "StomatalConductanceCO2Modifier", "1.0");
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion52(JObject root, string fileName)
        {
            foreach (var SOM in JsonUtilities.ChildrenOfType(root, "SoilOrganicMatter"))
            {
                double rootWt;
                if (SOM["RootWt"] is JArray)
                    rootWt = Convert.ToDouble(SOM["RootWt"][0], CultureInfo.InvariantCulture); // This can happen when importing old APSIM file.
                else
                    rootWt = Convert.ToDouble(SOM["RootWt"], CultureInfo.InvariantCulture);
                SOM.Remove("RootWt");
                double[] thickness = MathUtilities.StringsToDoubles(JsonUtilities.Values(SOM, "Thickness"));

                double profileDepth = MathUtilities.Sum(thickness);
                double cumDepth = 0;
                double[] rootWtFraction = new double[thickness.Length];

                for (int layer = 0; layer < thickness.Length; layer++)
                {
                    double fracLayer = Math.Min(1.0, MathUtilities.Divide(profileDepth - cumDepth, thickness[layer], 0.0));
                    cumDepth += thickness[layer];
                    rootWtFraction[layer] = fracLayer * Math.Exp(-3.0 * Math.Min(1.0, MathUtilities.Divide(cumDepth, profileDepth, 0.0)));
                }
                // get the actuall FOM distribution through layers (adds up to one)
                double totFOMfraction = MathUtilities.Sum(rootWtFraction);
                for (int layer = 0; layer < thickness.Length; layer++)
                    rootWtFraction[layer] /= totFOMfraction;
                double[] rootWtVector = MathUtilities.Multiply_Value(rootWtFraction, rootWt);

                JsonUtilities.SetValues(SOM, "RootWt", rootWtVector);
            }

        }

        /// <summary>
        /// Adds solutes under SoilNitrogen.
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion53(JObject root, string fileName)
        {
            foreach (var soilNitrogen in JsonUtilities.ChildrenOfType(root, "SoilNitrogen"))
            {
                JsonUtilities.CreateNewChildModel(soilNitrogen, "NO3", "Models.Soils.SoilNitrogenNO3");
                JsonUtilities.CreateNewChildModel(soilNitrogen, "NH4", "Models.Soils.SoilNitrogenNH4");
                JsonUtilities.CreateNewChildModel(soilNitrogen, "Urea", "Models.Soils.SoilNitrogenUrea");
                JsonUtilities.CreateNewChildModel(soilNitrogen, "PlantAvailableNO3", "Models.Soils.SoilNitrogenPlantAvailableNO3");
                JsonUtilities.CreateNewChildModel(soilNitrogen, "PlantAvailableNH4", "Models.Soils.SoilNitrogenPlantAvailableNH4");
            }

            foreach (var report in JsonUtilities.ChildrenOfType(root, "Report"))
            {
                JsonUtilities.SearchReplaceReportVariableNames(report, "SoilNitrogen.NO3", "SoilNitrogen.NO3.kgha");
                JsonUtilities.SearchReplaceReportVariableNames(report, "SoilNitrogen.NH4", "SoilNitrogen.NH4.kgha");
                JsonUtilities.SearchReplaceReportVariableNames(report, "SoilNitrogen.urea", "SoilNitrogen.Urea.kgha");
                JsonUtilities.SearchReplaceReportVariableNames(report, "SoilNitrogen.PlantAvailableNO3", "SoilNitrogen.PlantAvailableNO3.kgha");
                JsonUtilities.SearchReplaceReportVariableNames(report, "SoilNitrogen.PlantAvailableNH4", "SoilNitrogen.PlantAvailableNH4.kgha");
                JsonUtilities.SearchReplaceReportVariableNames(report, "[SoilNitrogen].no3", "[SoilNitrogen].NO3.kgha");
                JsonUtilities.SearchReplaceReportVariableNames(report, "[SoilNitrogen].nh4", "[SoilNitrogen].NH4.kgha");
                JsonUtilities.SearchReplaceReportVariableNames(report, "[SoilNitrogen].urea", "[SoilNitrogen].Urea.kgha");
            }
            foreach (var manager in JsonUtilities.ChildManagers(root))
            {
                var originalCode = manager.ToString();
                if (originalCode != null)
                {
                    if (originalCode.Contains("SoilNitrogen.NO3"))
                    {
                        manager.Parser.Replace("Soil.SoilNitrogen.NO3", "NO3.kgha");
                        manager.Parser.Replace("SoilNitrogen.NO3", "NO3.kgha");
                        manager.Parser.AddDeclaration("ISolute", "NO3", new string[] { "[ScopedLinkByName]" });
                    }
                    if (originalCode.Contains("SoilNitrogen.NH4"))
                    {
                        manager.Parser.Replace("Soil.SoilNitrogen.NH4", "NH4.kgha");
                        manager.Parser.Replace("SoilNitrogen.NH4", "NH4.kgha");
                        manager.Parser.AddDeclaration("ISolute", "NH4", new string[] { "[ScopedLinkByName]" });
                    }
                    if (originalCode.Contains("SoilNitrogen.urea"))
                    {
                        manager.Parser.Replace("Soil.SoilNitrogen.urea", "Urea.kgha");
                        manager.Parser.Replace("SoilNitrogen.urea", "Urea.kgha");
                        manager.Parser.AddDeclaration("ISolute", "Urea", new string[] { "[ScopedLinkByName]" });
                    }
                    if (originalCode.Contains("SoilNitrogen.PlantAvailableNO3"))
                    {
                        manager.Parser.Replace("Soil.SoilNitrogen.PlantAvailableNO3", "PlantAvailableNO3.kgha");
                        manager.Parser.Replace("SoilNitrogen.PlantAvailableNO3", "PlantAvailableNO3.kgha");
                        manager.Parser.AddDeclaration("ISolute", "PlantAvailableNO3", new string[] { "[ScopedLinkByName]" });
                    }
                    if (originalCode.Contains("SoilNitrogen.PlantAvailableNH4"))
                    {
                        manager.Parser.Replace("Soil.SoilNitrogen.PlantAvailableNH4", "PlantAvailableNH4.kgha");
                        manager.Parser.Replace("SoilNitrogen.PlantAvailableNH4", "PlantAvailableNH4.kgha");
                        manager.Parser.AddDeclaration("ISolute", "PlantAvailableNH4", new string[] { "[ScopedLinkByName]" });
                    }
                    if (originalCode != manager.ToString())
                    {
                        var usingLines = manager.Parser.GetUsingStatements().ToList();
                        usingLines.Add("Models.Interfaces");
                        manager.Parser.SetUsingStatements(usingLines);
                        manager.Save();
                    }
                }
            }

            foreach (var series in JsonUtilities.ChildrenOfType(root, "Series"))
            {
                if (series["XFieldName"] != null)
                {
                    series["XFieldName"] = series["XFieldName"].ToString().Replace("SoilNitrogen.NO3", "SoilNitrogen.NO3.kgha");
                    series["XFieldName"] = series["XFieldName"].ToString().Replace("SoilNitrogen.NH4", "SoilNitrogen.NH4.kgha");
                    series["XFieldName"] = series["XFieldName"].ToString().Replace("SoilNitrogen.urea", "SoilNitrogen.Urea.kgha");
                    series["XFieldName"] = series["XFieldName"].ToString().Replace("SoilNitrogen.PlantAvailableNO3", "SoilNitrogen.PlantAvailableNO3.kgha");
                    series["XFieldName"] = series["XFieldName"].ToString().Replace("SoilNitrogen.PlantAvailableNH4", "SoilNitrogen.PlantAvailableNH4.kgha");
                }
                if (series["YFieldName"] != null)
                {
                    series["YFieldName"] = series["YFieldName"].ToString().Replace("SoilNitrogen.NO3", "SoilNitrogen.NO3.kgha");
                    series["YFieldName"] = series["YFieldName"].ToString().Replace("SoilNitrogen.NH4", "SoilNitrogen.NH4.kgha");
                    series["YFieldName"] = series["YFieldName"].ToString().Replace("SoilNitrogen.urea", "SoilNitrogen.Urea.kgha");
                    series["YFieldName"] = series["YFieldName"].ToString().Replace("SoilNitrogen.PlantAvailableNO3", "SoilNitrogen.PlantAvailableNO3.kgha");
                    series["YFieldName"] = series["YFieldName"].ToString().Replace("SoilNitrogen.PlantAvailableNH4", "SoilNitrogen.PlantAvailableNH4.kgha");
                }
            }
        }

        /// <summary>
        /// Remove SoluteManager.
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion54(JObject root, string fileName)
        {
            foreach (var soluteManager in JsonUtilities.ChildrenOfType(root, "SoluteManager"))
                soluteManager.Remove();

            foreach (var report in JsonUtilities.ChildrenOfType(root, "Report"))
            {
                JsonUtilities.SearchReplaceReportVariableNames(report, "[Soil].NO3N", "[Soil].SoilNitrogen.NO3.kgha");
                JsonUtilities.SearchReplaceReportVariableNames(report, "[Soil].NH4N", "[Soil].SoilNitrogen.NH4.kgha");
                JsonUtilities.SearchReplaceReportVariableNames(report, "[Soil].UreaN", "[Soil].SoilNitrogen.Urea.kgha");
            }

            foreach (var manager in JsonUtilities.ChildManagers(root))
            {
                bool managerChanged = false;
                if (manager.Parser.Replace("mySoil.NO3N", "NO3.kgha"))
                {
                    manager.Parser.AddDeclaration("ISolute", "NO3", new string[] { "[ScopedLinkByName]" });
                    managerChanged = true;
                }
                if (manager.Parser.Replace("mySoil.NH4N", "NH4.kgha"))
                {
                    manager.Parser.AddDeclaration("ISolute", "NH4", new string[] { "[ScopedLinkByName]" });
                    managerChanged = true;
                }
                if (manager.Parser.Replace("mySoil.UreaN", "Urea.kgha"))
                {
                    manager.Parser.AddDeclaration("ISolute", "Urea", new string[] { "[ScopedLinkByName]" });
                    managerChanged = true;
                }
                if (manager.Parser.Replace("Soil.NO3N", "NO3.kgha"))
                {
                    manager.Parser.AddDeclaration("ISolute", "NO3", new string[] { "[ScopedLinkByName]" });
                    managerChanged = true;
                }
                if (manager.Parser.Replace("Soil.NH4N", "NH4.kgha"))
                {
                    manager.Parser.AddDeclaration("ISolute", "NH4", new string[] { "[ScopedLinkByName]" });
                    managerChanged = true;
                }
                if (manager.Parser.Replace("Soil.UreaN", "Urea.kgha"))
                {
                    manager.Parser.AddDeclaration("ISolute", "Urea", new string[] { "[ScopedLinkByName]" });
                    managerChanged = true;
                }
                if (manager.Parser.Replace("mySoil.SoilNitrogen.", "SoilNitrogen."))
                {
                    manager.Parser.AddDeclaration("SoilNitrogen", "SoilNitrogen", new string[] { "[ScopedLinkByName]" });
                    managerChanged = true;
                }
                if (manager.Parser.Replace("Soil.SoilNitrogen.", "SoilNitrogen."))
                {
                    manager.Parser.AddDeclaration("SoilNitrogen", "SoilNitrogen", new string[] { "[ScopedLinkByName]" });
                    managerChanged = true;
                }
                if (manager.Parser.Replace("soil.SoilNitrogen.", "SoilNitrogen."))
                {
                    manager.Parser.AddDeclaration("SoilNitrogen", "SoilNitrogen", new string[] { "[ScopedLinkByName]" });
                    managerChanged = true;
                }
                if (manager.Parser.Replace("soil1.SoilNitrogen.", "SoilNitrogen."))
                {
                    manager.Parser.AddDeclaration("SoilNitrogen", "SoilNitrogen", new string[] { "[ScopedLinkByName]" });
                    managerChanged = true;
                }
                var declarations = manager.Parser.GetDeclarations();
                if (declarations.RemoveAll(declaration => declaration.TypeName == "SoluteManager") > 0)
                {
                    manager.Parser.SetDeclarations(declarations);
                    managerChanged = true;
                }

                if (managerChanged)
                {
                    var usingLines = manager.Parser.GetUsingStatements().ToList();
                    usingLines.Add("Models.Interfaces");
                    manager.Parser.SetUsingStatements(usingLines);
                    manager.Save();
                }
            }
        }


        /// <summary>
        /// Changes initial Root Wt to an array.
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion55(JObject root, string fileName)
        {
            foreach (var SOM in JsonUtilities.ChildrenOfType(root, "SoilOrganicMatter"))
            {
                double soilcnr;
                if (SOM["SoilCN"] is JArray)
                    soilcnr = Convert.ToDouble(SOM["SoilCN"][0], CultureInfo.InvariantCulture); // This can happen when importing old APSIM file.
                else
                    soilcnr = Convert.ToDouble(SOM["SoilCN"], CultureInfo.InvariantCulture);
                SOM.Remove("SoilCN");
                double[] thickness = MathUtilities.StringsToDoubles(JsonUtilities.Values(SOM, "Thickness"));

                double[] SoilCNVector = new double[thickness.Length];

                for (int layer = 0; layer < thickness.Length; layer++)
                    SoilCNVector[layer] = soilcnr;

                JsonUtilities.SetValues(SOM, "SoilCN", SoilCNVector);
            }

        }

        /// <summary>
        /// Change Factor.Specifications to Factor.Specification. Also FactorValue
        /// becomes CompositeFactor.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="fileName"></param>
        private static void UpgradeToVersion56(JToken root, string fileName)
        {
            foreach (var factor in JsonUtilities.ChildrenRecursively(root as JObject, "Factor"))
            {
                var parent = JsonUtilities.Parent(factor);

                string parentModelType = JsonUtilities.Type(parent);
                if (parentModelType == "Factors")
                {
                    var specifications = factor["Specifications"] as JArray;
                    if (specifications != null)
                    {
                        if (specifications.Count > 1)
                        {
                            // must be a compound factor. 

                            // Change our Factor to a CompositeFactor
                            factor["$type"] = "Models.Factorial.CompositeFactor, Models";

                            // Remove the Factor from it's parent.
                            var parentChildren = parent["Children"] as JArray;
                            parentChildren.Remove(factor);

                            // Create a new site factor and add our CompositeFactor to the children list.
                            var siteFactor = JsonUtilities.ChildWithName(parent as JObject, "Site") as JObject;
                            if (siteFactor == null)
                            {
                                // Create a site factor 
                                siteFactor = new JObject();
                                siteFactor["$type"] = "Models.Factorial.Factor, Models";
                                JsonUtilities.RenameModel(siteFactor, "Site");
                                JArray siteFactorChildren = new JArray();
                                siteFactor["Children"] = siteFactorChildren;

                                // Add our new site factor to our models parent.
                                parentChildren.Add(siteFactor);
                            }
                            (siteFactor["Children"] as JArray).Add(factor);

                        }
                        else
                        {
                            // Convert array to string.
                            if (specifications.Count > 0)
                                factor["Specification"] = specifications[0].ToString();
                        }
                    }
                }
                else if (parentModelType == "Factor")
                {
                    factor["$type"] = "Models.Factorial.CompositeFactor, Models";
                }
            }

            foreach (var series in JsonUtilities.ChildrenRecursively(root as JObject, "Series"))
            {
                var factorToVaryColours = series["FactorToVaryColours"];
                if (factorToVaryColours != null && factorToVaryColours.Value<string>() == "Simulation")
                    series["FactorToVaryColours"] = "SimulationName";
                var factorToVaryMarkers = series["FactorToVaryMarkers"];
                if (factorToVaryMarkers != null && factorToVaryMarkers.Value<string>() == "Simulation")
                    series["FactorToVaryMarkers"] = "SimulationName";
                var factorToVaryLines = series["FactorToVaryLines"];
                if (factorToVaryLines != null && factorToVaryLines.Value<string>() == "Simulation")
                    series["FactorToVaryLines"] = "SimulationName";
            }
        }

        /// <summary>
        /// Upgrades to version 57. Adds a RetranslocateNonStructural node to
        /// all GenericOrgans which do not have a child called
        /// RetranslocateNitrogen.
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion57(JObject root, string fileName)
        {
            foreach (JObject organ in JsonUtilities.ChildrenRecursively(root, "GenericOrgan"))
                if (JsonUtilities.ChildWithName(organ, "RetranslocateNitrogen") == null)
                    JsonUtilities.AddModel(organ, typeof(RetranslocateNonStructural), "RetranslocateNitrogen");
        }

        /// <summary>
        /// Upgrades to version 58. Renames 'ParamThickness' to 'Thickness' in Weirdo.
        /// Also change calls to property soil.SWAtWaterThickness to soil.Thickness.
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion58(JObject root, string fileName)
        {
            foreach (JObject weirdo in JsonUtilities.ChildrenRecursively(root, "WEIRDO"))
            {
                var paramThicknessNode = weirdo["ParamThickness"];
                if (paramThicknessNode != null)
                {
                    weirdo["Thickness"] = paramThicknessNode;
                    weirdo.Remove("ParamThickness");
                }
            }

            foreach (var manager in JsonUtilities.ChildManagers(root))
            {
                if (manager.Parser.Replace(".SWAtWaterThickness", ".Thickness"))
                    manager.Save();
            }
        }

        /// <summary>
        /// Upgrades to version 59. Renames 'SoilCropOilPalm' to 'SoilCrop'.
        /// Renames Soil.SoilOrganicMatter.OC to Soil.Initial.OC
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion59(JObject root, string fileName)
        {
            foreach (var sample in JsonUtilities.ChildrenRecursively(root, "Sample"))
            {
                var array = sample["NO3"] as JArray;
                if (array != null)
                {
                    var nitrogenValue = new JObject();
                    nitrogenValue["$type"] = "Models.Soils.NitrogenValue, Models";

                    var storedAsPPM = sample["NO3Units"]?.ToString() == "0" ||
                                      sample["NO3Units"]?.ToString() == "ppm" ||
                                      sample["NO3Units"] == null;

                    nitrogenValue["Values"] = array;
                    nitrogenValue["StoredAsPPM"] = storedAsPPM;
                    sample.Remove("NO3");
                    sample["NO3N"] = nitrogenValue;
                }

                array = sample["NH4"] as JArray;
                if (array != null)
                {
                    var nitrogenValue = new JObject();
                    nitrogenValue["$type"] = "Models.Soils.NitrogenValue, Models";

                    var storedAsPPM = sample["NH4Units"]?.ToString() == "0" ||
                                      sample["NH4Units"]?.ToString() == "ppm" ||
                                      sample["NH4Units"] == null;

                    nitrogenValue["Values"] = array;
                    nitrogenValue["StoredAsPPM"] = storedAsPPM;
                    sample.Remove("NH4");
                    sample["NH4N"] = nitrogenValue;
                }
            }
            foreach (var soilCropOilPalmNode in JsonUtilities.ChildrenRecursively(root, "SoilCropOilPalm"))
                soilCropOilPalmNode["$type"] = "Models.Soils.SoilCrop, Models";

            foreach (var report in JsonUtilities.ChildrenRecursively(root, "Report"))
            {
                JsonUtilities.SearchReplaceReportVariableNames(report, ".SoilOrganicMatter.OC", ".Initial.OC");
                JsonUtilities.SearchReplaceReportVariableNames(report, "[Soil].PH", "[Soil].Initial.PH");
                JsonUtilities.SearchReplaceReportVariableNames(report, "[Soil].EC", "[Soil].Initial.EC");
                JsonUtilities.SearchReplaceReportVariableNames(report, "[Soil].ESP", "[Soil].Initial.ESP");
                JsonUtilities.SearchReplaceReportVariableNames(report, "[Soil].Cl", "[Soil].Initial.CL");
                JsonUtilities.SearchReplaceReportVariableNames(report, "[Soil].OC", "[Soil].Initial.OC");
                JsonUtilities.SearchReplaceReportVariableNames(report, "[Soil].InitialNO3N", "[Soil].Initial.NO3N.PPM");
                JsonUtilities.SearchReplaceReportVariableNames(report, "[Soil].InitialNH4N", "[Soil].Initial.NH4N.PPM");
            }

            foreach (var series in JsonUtilities.ChildrenRecursively(root, "Series"))
            {
                if (series["XFieldName"] != null)
                    series["XFieldName"] = series["XFieldName"].ToString().Replace(".SoilOrganicMatter.OC", ".Initial.OC");
                if (series["YFieldName"] != null)
                    series["YFieldName"] = series["YFieldName"].ToString().Replace(".SoilOrganicMatter.OC", ".Initial.OC");
            }

            foreach (var expressionFunction in JsonUtilities.ChildrenRecursively(root, "ExpressionFunction"))
            {
                var expression = expressionFunction["Expression"].ToString();
                expression = expression.Replace(".SoilOrganicMatter.OC", ".Initial.OC");
                expressionFunction["Expression"] = expression;
            }

            foreach (var manager in JsonUtilities.ChildManagers(root))
            {
                var changeMade = manager.Parser.Replace("Soil.ToCumThickness(soil.Thickness)", "soil.ThicknessCumulative");

                if (manager.Parser.Replace("mySoil.Depth.Length", "mySoil.Thickness.Length"))
                    changeMade = true;

                if (manager.Parser.Replace("soil.Depth.Length", "soil.Thickness.Length"))
                    changeMade = true;

                if (changeMade)
                    manager.Save();
            }
        }

        /// <summary>
        /// Convert no3 and nh4 parameters from ppm to kg/ha.
        /// </summary>
        /// <param name="values"></param>
        private static void ConvertToPPM(JArray values)
        {
            var sample = JsonUtilities.Parent(JsonUtilities.Parent(values));
            var soil = JsonUtilities.Parent(sample) as JObject;
            var water = JsonUtilities.Children(soil).Find(child => JsonUtilities.Type(child) == "Water");
            if (water == null)
                water = JsonUtilities.Children(soil).Find(child => JsonUtilities.Type(child) == "WEIRDO");

            // Get soil thickness and bulk density.
            var soilThickness = water["Thickness"].Values<double>().ToArray();
            var soilBD = water["BD"].Values<double>().ToArray();

            // Get sample thickness and bulk density.
            var sampleThickness = sample["Thickness"].Values<double>().ToArray();
            var sampleBD = Soils.Standardiser.Layers.MapConcentration(soilBD, soilThickness, sampleThickness, soilBD.Last());

            for (int i = 0; i < values.Count; i++)
                values[i] = values[i].Value<double>() * 100 / (sampleBD[i] * sampleThickness[i]);
        }

        /// <summary>
        /// Does the specified array have non NaN values?
        /// </summary>
        /// <param name="no3Values">The array to remove them from.</param>
        private static bool HasValues(JArray no3Values)
        {
            foreach (var value in no3Values)
                if (value.ToString() != "NaN")
                    return true;
            return false;
        }

        /// <summary>
        /// Upgrades to version 60. Move NO3 and NH4 from sample to Analaysis node
        /// and always store as ppm.
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion60(JObject root, string fileName)
        {
            foreach (var sample in JsonUtilities.ChildrenRecursively(root, "Sample"))
            {
                var soil = JsonUtilities.Parent(sample) as JObject;
                var analysis = JsonUtilities.Children(soil).Find(child => JsonUtilities.Type(child) == "Analysis");
                var water = JsonUtilities.Children(soil).Find(child => JsonUtilities.Type(child) == "Water");
                if (water == null)
                    water = JsonUtilities.Children(soil).Find(child => JsonUtilities.Type(child) == "WEIRDO");

                var no3Node = sample["NO3N"];
                if (no3Node != null && no3Node.HasValues)
                {
                    if (analysis == null)
                        throw new Exception("Cannot find an analysis node while converting a soil sample.");

                    // Convert units to ppm if necessary.
                    var no3Values = no3Node["Values"] as JArray;

                    // Only overlay values if they are not NaN values.
                    if (HasValues(no3Values))
                    {
                        if (!no3Node["StoredAsPPM"].Value<bool>())
                            ConvertToPPM(no3Values);

                        // Make sure layers match analysis layers.
                        var analysisThickness = analysis["Thickness"].Values<double>().ToArray();
                        var sampleThickness = sample["Thickness"].Values<double>().ToArray();
                        var values = no3Values.Values<double>().ToArray();
                        var mappedValues = Soils.Standardiser.Layers.MapConcentration(values, sampleThickness, analysisThickness, 1.0);
                        no3Values = new JArray(mappedValues);

                        // Move from sample to analysis
                        analysis["NO3N"] = no3Values;
                    }
                }
                sample["NO3N"] = null;
                var nh4Node = sample["NH4N"];
                if (nh4Node != null && nh4Node.HasValues)
                {
                    if (analysis == null)
                        throw new Exception("Cannot find an analysis node while converting a soil sample.");

                    // Convert units to ppm if necessary.
                    var nh4Values = nh4Node["Values"] as JArray;

                    // Only overlay values if they are not NaN values.
                    if (HasValues(nh4Values))
                    {
                        if (!nh4Node["StoredAsPPM"].Value<bool>())
                            ConvertToPPM(nh4Values);

                        // Make sure layers match analysis layers.
                        var analysisThickness = analysis["Thickness"].Values<double>().ToArray();
                        var sampleThickness = sample["Thickness"].Values<double>().ToArray();
                        var values = nh4Values.Values<double>().ToArray();
                        var mappedValues = Soils.Standardiser.Layers.MapConcentration(values, sampleThickness, analysisThickness, 0.2);
                        nh4Values = new JArray(mappedValues);

                        // Move from sample to analysis
                        analysis["NH4N"] = nh4Values;
                    }
                }
                sample["NH4N"] = null;
            }
        }

        /// <summary>
        /// Upgrade to version 60. Ensures that a micromet model is within every simulation.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="fileName"></param>
        private static void UpgradeToVersion61(JObject root, string fileName)
        {
            foreach (JObject Sim in JsonUtilities.ChildrenRecursively(root, "Simulation"))
            {
                List<JObject> MicroClimates = JsonUtilities.ChildrenRecursively(root, "MicroClimate");
                if (MicroClimates.Count == 0)
                    AddMicroClimate(Sim);
            }

        }

        /// <summary>
        /// Add a MicroClimate model to the specified JSON model token.
        /// </summary>
        /// <param name="simulation">An APSIM Simulation</param>
        public static void AddMicroClimate(JObject simulation)
        {
            JArray children = simulation["Children"] as JArray;
            if (children == null)
            {
                children = new JArray();
                simulation["Children"] = children;
            }

            JObject microClimateModel = new JObject();
            microClimateModel["$type"] = "Models.MicroClimate, Models";
            JsonUtilities.RenameModel(microClimateModel, "MicroClimate");
            microClimateModel["a_interception"] = "0.0";
            microClimateModel["b_interception"] = "1.0";
            microClimateModel["c_interception"] = "0.0";
            microClimateModel["d_interception"] = "0.0";
            microClimateModel["soil_albedo"] = "0.13";
            microClimateModel["SoilHeatFluxFraction"] = "0.4";
            microClimateModel["NightInterceptionFraction"] = "0.5";
            microClimateModel["ReferenceHeight"] = "2.0";
            microClimateModel["IncludeInDocumentation"] = "true";
            microClimateModel["Enabled"] = "true";
            microClimateModel["ReadOnly"] = "false";
            var weathers = JsonUtilities.ChildrenOfType(simulation, "Weather");

            // Don't bother with microclimate if no weather component
            if (weathers.Count != 0)
            {
                var weather = weathers.First();
                int index = children.IndexOf(weather);
                children.Insert(index + 1, microClimateModel);
            }
        }

        /// <summary>
        /// Upgrades to version 62. Fixes SimpleLeaf variable names
        /// following a refactor of this class.
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion62(JObject root, string fileName)
        {
            // We renamed a lot of IFunctions and removed the 'Function' suffix.
            // ie HeightFunction -> Height.
            Dictionary<string, string> changedProperties = new Dictionary<string, string>();
            changedProperties.Add("Tallness", "HeightFunction");
            changedProperties.Add("Area", "LAIFunction");
            changedProperties.Add("LaiDead", "LaiDeadFunction");
            changedProperties.Add("WaterDemand", "WaterDemandFunction");
            changedProperties.Add("Cover", "CoverFunction");
            changedProperties.Add("ExtinctionCoefficient", "ExtinctionCoefficientFunction");
            changedProperties.Add("BaseHeight", "BaseHeightFunction");
            changedProperties.Add("Wideness", "WidthFunction");
            changedProperties.Add("DetachmentRate", "DetachmentRateFunction");
            changedProperties.Add("InitialWt", "InitialWtFunction");
            changedProperties.Add("MaintenanceRespiration", "MaintenanceRespirationFunction");
            changedProperties.Add("FRGR", "FRGRFunction");

            // Names of nodes which are probably simple leaf. The problem is that
            // in released models, the model is stored in a separate file to the
            // simulations. Therefore when we parse/convert the simulation file,
            // we don't know the names of the simple leaf models, so we are forced
            // take a guess.
            List<string> modelNames = new List<string>() { "Leaf", "Stover" };

            // Names of nodes which are definitely simple leaf.
            List<string> definiteSimpleLeaves = new List<string>();

            // Go through all SimpleLeafs and rename the appropriate children.
            foreach (JObject leaf in JsonUtilities.ChildrenRecursively(root, "SimpleLeaf"))
            {
                modelNames.Add(leaf["Name"].ToString());
                definiteSimpleLeaves.Add(leaf["Name"].ToString());
                // We removed the Leaf.AppearedCohortNo property.
                JObject relativeArea = JsonUtilities.FindFromPath(leaf, "DeltaLAI.Vegetative.Delta.RelativeArea");
                if (relativeArea != null && relativeArea["XProperty"].ToString() == "[Leaf].AppearedCohortNo")
                    relativeArea["XProperty"] = "[Leaf].NodeNumber";

                foreach (var change in changedProperties)
                {
                    string newName = change.Key;
                    string old = change.Value;
                    JsonUtilities.RenameChildModel(leaf, old, newName);
                }
            }

            foreach (JObject reference in JsonUtilities.ChildrenRecursively(root, "VariableReference"))
            {
                foreach (string leafName in definiteSimpleLeaves)
                {
                    foreach (KeyValuePair<string, string> property in changedProperties)
                    {
                        string oldName = property.Value;
                        string newName = property.Key;

                        string toReplace = $"{leafName}.{oldName}";
                        string replaceWith = $"{leafName}.{newName}";
                        reference["VariableName"] = reference["VariableName"].ToString().Replace(toReplace, replaceWith);

                        toReplace = $"[{leafName}].{oldName}";
                        replaceWith = $"[{leafName}].{newName}";
                        reference["VariableName"] = reference["VariableName"].ToString().Replace(toReplace, replaceWith);
                    }
                }
            }

            // Attempt some basic find/replace in manager scripts.
            foreach (ManagerConverter manager in JsonUtilities.ChildManagers(root))
            {
                foreach (var change in changedProperties)
                {
                    string newName = change.Key;
                    string old = change.Value;

                    bool changed = false;
                    foreach (string modelName in modelNames)
                    {
                        string toReplace = $"{modelName}.{old}";
                        string replaceWith = $"{modelName}.{newName}";
                        changed |= manager.Parser.Replace(toReplace, replaceWith, true);

                        foreach (KeyValuePair<string, string> parameter in manager.Parameters)
                        {
                            string newParam = parameter.Value.Replace(toReplace, replaceWith);
                            manager.UpdateParameter(parameter.Key, newParam);
                        }

                        toReplace = $"[{modelName}].{old}";
                        replaceWith = $"[{modelName}].{newName}";
                        changed |= manager.Parser.Replace(toReplace, replaceWith, true);

                        foreach (KeyValuePair<string, string> parameter in manager.Parameters)
                        {
                            string newParam = parameter.Value.Replace(toReplace, replaceWith);
                            manager.UpdateParameter(parameter.Key, newParam);
                        }
                    }
                    if (changed)
                        manager.Save();
                }
            }

            // Fix some cultivar commands.
            foreach (JObject cultivar in JsonUtilities.ChildrenRecursively(root, "Cultivar"))
            {
                if (!cultivar["Command"].HasValues)
                    continue;

                foreach (JValue command in cultivar["Command"].Children())
                {
                    foreach (var change in changedProperties)
                    {
                        string newName = change.Key;
                        string old = change.Value;
                        foreach (string modelName in modelNames)
                        {
                            command.Value = command.Value.ToString().Replace($"{modelName}.{old}", $"{modelName}.{newName}");
                            command.Value = command.Value.ToString().Replace($"[{modelName}].{old}", $"[{modelName}].{newName}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Upgrades to version 63. Rename the 'Water' node under soil to 'Physical'
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion63(JObject root, string fileName)
        {
            foreach (var water in JsonUtilities.ChildrenRecursively(root, "Water"))
            {
                water["$type"] = "Models.Soils.Physical, Models";
                JsonUtilities.RenameModel(water, "Physical");
            }

            foreach (var report in JsonUtilities.ChildrenOfType(root, "Report"))
            {
                JsonUtilities.SearchReplaceReportVariableNames(report, ".Water.", ".Physical.");
            }

            foreach (var factor in JsonUtilities.ChildrenOfType(root, "Factor"))
            {
                var specification = factor["Specification"];
                if (specification != null)
                {
                    var specificationString = specification.ToString();
                    specificationString = specificationString.Replace(".Water.", ".Physical.");
                    specificationString = specificationString.Replace("[Water]", "[Physical]");
                    factor["Specification"] = specificationString;
                }
            }

            foreach (var factor in JsonUtilities.ChildrenOfType(root, "CompositeFactor"))
            {
                var specifications = factor["Specifications"];
                if (specifications != null)
                {
                    for (int i = 0; i < specifications.Count(); i++)
                    {
                        var specificationString = specifications[i].ToString();
                        specificationString = specificationString.Replace(".Water.", ".Physical.");
                        specificationString = specificationString.Replace("[Water]", "[Physical]");
                        specifications[i] = specificationString;
                    }
                }
            }
        }


        /// <summary>
        /// Upgrades to version 64. Rename the 'SoilOrganicMatter' node under soil to 'Organic'
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion64(JObject root, string fileName)
        {
            foreach (var organic in JsonUtilities.ChildrenRecursively(root, "SoilOrganicMatter"))
            {
                organic["$type"] = "Models.Soils.Organic, Models";
                JsonUtilities.RenameModel(organic, "Organic");
                organic["FOMCNRatio"] = organic["RootCN"];
                organic["FOM"] = organic["RootWt"];
                organic["SoilCNRatio"] = organic["SoilCN"];
                organic["Carbon"] = organic["OC"];
                var ocUnits = organic["OCUnits"];
                if (ocUnits != null)
                {
                    string ocUnitsString = ocUnits.ToString();
                    if (ocUnitsString == "1" || ocUnitsString == "WalkleyBlack")
                    {
                        var oc = organic["Carbon"].Values<double>().ToArray();
                        oc = MathUtilities.Multiply_Value(oc, 1.3);
                        organic["Carbon"] = new JArray(oc);
                    }
                }
            }

            foreach (var report in JsonUtilities.ChildrenOfType(root, "Report"))
            {
                JsonUtilities.SearchReplaceReportVariableNames(report, ".SoilOrganicMatter.", ".Organic.");
                JsonUtilities.SearchReplaceReportVariableNames(report, ".RootCN", ".FOMCNRatio");
                JsonUtilities.SearchReplaceReportVariableNames(report, ".RootWt", ".FOM");
                JsonUtilities.SearchReplaceReportVariableNames(report, ".SoilCN", ".SoilCNRatio");
                JsonUtilities.SearchReplaceReportVariableNames(report, ".Organic.OC", ".Organic.Carbon");
            }

            foreach (var factor in JsonUtilities.ChildrenOfType(root, "Factor"))
            {
                var specification = factor["Specification"];
                if (specification != null)
                {
                    var specificationString = specification.ToString();
                    specificationString = specificationString.Replace(".SoilOrganicMatter.", ".Organic.");
                    specificationString = specificationString.Replace("[SoilOrganicMatter]", "[Organic]");
                    specificationString = specificationString.Replace(".Organic.OC", ".Organic.Carbon");
                    specificationString = specificationString.Replace(".RootCN", ".FOMCNRatio");
                    specificationString = specificationString.Replace(".RootWt", ".FOM");
                    specificationString = specificationString.Replace(".SoilCN", ".SoilCNRatio");
                    factor["Specification"] = specificationString;
                }
            }

            foreach (var factor in JsonUtilities.ChildrenOfType(root, "CompositeFactor"))
            {
                var specifications = factor["Specifications"];
                if (specifications != null)
                {
                    for (int i = 0; i < specifications.Count(); i++)
                    {
                        var specificationString = specifications[i].ToString();
                        specificationString = specificationString.Replace(".SoilOrganicMatter.", ".Organic.");
                        specificationString = specificationString.Replace("[SoilOrganicMatter]", "[Organic]");
                        specificationString = specificationString.Replace(".OC", ".Carbon");
                        specificationString = specificationString.Replace(".RootCN", ".FOMCNRatio");
                        specificationString = specificationString.Replace(".RootWt", ".FOM");
                        specificationString = specificationString.Replace(".SoilCN", ".SoilCNRatio");
                        specifications[i] = specificationString;
                    }
                }
            }

            foreach (var series in JsonUtilities.ChildrenOfType(root, "Series"))
            {
                if (series["XFieldName"] != null)
                {
                    series["XFieldName"] = series["XFieldName"].ToString().Replace("SoilOrganicMatter", "Organic");
                    series["XFieldName"] = series["XFieldName"].ToString().Replace(".Organic.OC", ".Organic.Carbon");
                }
                if (series["YFieldName"] != null)
                {
                    series["YFieldName"] = series["YFieldName"].ToString().Replace("SoilOrganicMatter", "Organic");
                    series["YFieldName"] = series["YFieldName"].ToString().Replace(".Organic.OC", ".Organic.Carbon");
                }
            }

            foreach (var child in JsonUtilities.ChildrenRecursively(root))
            {
                if (JsonUtilities.Type(child) == "Morris" || JsonUtilities.Type(child) == "Sobol")
                {
                    var parameters = child["Parameters"];
                    for (int i = 0; i < parameters.Count(); i++)
                    {
                        var parameterString = parameters[i]["Path"].ToString();
                        parameterString = parameterString.Replace(".SoilOrganicMatter.", ".Organic.");
                        parameterString = parameterString.Replace("[SoilOrganicMatter]", "[Organic]");
                        parameterString = parameterString.Replace(".OC", ".Carbon");
                        parameters[i]["Path"] = parameterString;
                    }
                }
            }
        }

        /// <summary>
        /// Upgrades to version 65. Rename the 'Analysis' node under soil to 'Chemical'
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion65(JObject root, string fileName)
        {
            foreach (var chemical in JsonUtilities.ChildrenRecursively(root, "Analysis"))
            {
                var soil = JsonUtilities.Parent(chemical);
                var physical = JsonUtilities.ChildWithName(soil as JObject, "Physical");

                chemical["$type"] = "Models.Soils.Chemical, Models";
                JsonUtilities.RenameModel(chemical, "Chemical");
                if (physical != null && physical["Thickness"] != null)
                {
                    // Move particle size numbers from chemical to physical and make sure layers are mapped.
                    var physicalThickness = physical["Thickness"].Values<double>().ToArray();
                    var chemicalThickness = chemical["Thickness"].Values<double>().ToArray();

                    if (chemical["ParticleSizeSand"] != null && chemical["ParticleSizeSand"].HasValues)
                    {
                        var values = chemical["ParticleSizeSand"].Values<double>().ToArray();
                        if (values.Length < physicalThickness.Length)
                            Array.Resize(ref values, chemicalThickness.Length);
                        var mappedValues = Soils.Standardiser.Layers.MapConcentration(values, chemicalThickness, physicalThickness, values.Last());
                        physical["ParticleSizeSand"] = new JArray(mappedValues);
                    }

                    if (chemical["ParticleSizeSilt"] != null && chemical["ParticleSizeSilt"].HasValues)
                    {
                        var values = chemical["ParticleSizeSilt"].Values<double>().ToArray();
                        if (values.Length < physicalThickness.Length)
                            Array.Resize(ref values, chemicalThickness.Length);
                        var mappedValues = Soils.Standardiser.Layers.MapConcentration(values, chemicalThickness, physicalThickness, values.Last());
                        physical["ParticleSizeSilt"] = new JArray(mappedValues);
                    }

                    if (chemical["ParticleSizeClay"] != null && chemical["ParticleSizeClay"].HasValues)
                    {
                        var values = chemical["ParticleSizeClay"].Values<double>().ToArray();
                        if (values.Length < physicalThickness.Length)
                            Array.Resize(ref values, chemicalThickness.Length);
                        var mappedValues = Soils.Standardiser.Layers.MapConcentration(values, chemicalThickness, physicalThickness, values.Last());
                        physical["ParticleSizeClay"] = new JArray(mappedValues);
                    }

                    // convert ph units
                    var phUnits = physical["PHUnits"];
                    if (phUnits != null)
                    {
                        string phUnitsString = phUnits.ToString();
                        if (phUnitsString == "1")
                        {
                            // pH in water = (pH in CaCl X 1.1045) - 0.1375
                            var ph = physical["PH"].Values<double>().ToArray();
                            ph = MathUtilities.Subtract_Value(MathUtilities.Multiply_Value(ph, 1.1045), 0.1375);
                            chemical["PH"] = new JArray(ph);
                        }
                    }
                }
            }

            foreach (var report in JsonUtilities.ChildrenOfType(root, "Report"))
            {
                JsonUtilities.SearchReplaceReportVariableNames(report, ".Analysis.", ".Chemical.");
            }

            foreach (var factor in JsonUtilities.ChildrenOfType(root, "Factor"))
            {
                var specification = factor["Specification"];
                if (specification != null)
                {
                    var specificationString = specification.ToString();
                    specificationString = specificationString.Replace(".Analysis.", ".Chemical.");
                    specificationString = specificationString.Replace("[Analysis]", "[Chemical]");
                    factor["Specification"] = specificationString;
                }
            }

            foreach (var factor in JsonUtilities.ChildrenOfType(root, "CompositeFactor"))
            {
                var specifications = factor["Specifications"];
                if (specifications != null)
                {
                    for (int i = 0; i < specifications.Count(); i++)
                    {
                        var specificationString = specifications[i].ToString();
                        specificationString = specificationString.Replace(".Analysis.", ".Chemical.");
                        specificationString = specificationString.Replace("[Analysis]", "[Chemical]");
                        specifications[i] = specificationString;
                    }
                }
            }

            foreach (var series in JsonUtilities.ChildrenOfType(root, "Series"))
            {
                if (series["XFieldName"] != null)
                {
                    series["XFieldName"] = series["XFieldName"].ToString().Replace("Analysis", "Chemical");
                }
                if (series["YFieldName"] != null)
                {
                    series["YFieldName"] = series["YFieldName"].ToString().Replace("Analysis", "Chemical");
                }
            }

            foreach (var child in JsonUtilities.ChildrenRecursively(root))
            {
                if (JsonUtilities.Type(child) == "Morris" || JsonUtilities.Type(child) == "Sobol")
                {
                    var parameters = child["Parameters"];
                    for (int i = 0; i < parameters.Count(); i++)
                    {
                        var parameterString = parameters[i]["Path"].ToString();
                        parameterString = parameterString.Replace(".Analysis.", ".Chemical.");
                        parameterString = parameterString.Replace("[Analysis]", "[Chemical]");
                        parameters[i]["Path"] = parameterString;
                    }
                }
            }
        }

        /// <summary>
        /// When a factor is under a factors model, insert a permutation model.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="fileName"></param>
        private static void UpgradeToVersion66(JToken root, string fileName)
        {
            foreach (var factors in JsonUtilities.ChildrenRecursively(root as JObject, "Factors"))
            {
                if (JsonUtilities.Children(factors).Count > 1)
                {
                    var permutationsNode = new JObject();
                    permutationsNode["$type"] = "Models.Factorial.Permutation, Models";
                    JsonUtilities.RenameModel(permutationsNode, "Permutation");
                    permutationsNode["Children"] = factors["Children"];
                    var children = new JArray(permutationsNode);
                    factors["Children"] = children;
                }
            }
        }

        /// <summary>
        /// Upgrades to version 67. Sets the Start and End properties
        /// in clock to the values previously stored in StartDate and EndDate.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="fileName"></param>
        private static void UpgradeToVersion67(JObject root, string fileName)
        {
            foreach (JObject clock in JsonUtilities.ChildrenRecursively(root, "Clock"))
            {
                clock["Start"] = clock["StartDate"];
                clock["End"] = clock["EndDate"];
            }
        }

        /// <summary>
        /// Upgrades to version 68. Removes AgPasture.Sward
        /// </summary>
        /// <param name="root"></param>
        /// <param name="fileName"></param>
        private static void UpgradeToVersion68(JObject root, string fileName)
        {
            foreach (JObject sward in JsonUtilities.ChildrenRecursively(root, "Sward"))
            {
                foreach (JObject pastureSpecies in JsonUtilities.Children(sward))
                {
                    if (pastureSpecies["Name"].ToString().Equals("Ryegrass", StringComparison.InvariantCultureIgnoreCase))
                        JsonUtilities.RenameModel(pastureSpecies, "AGPRyegrass");
                    if (pastureSpecies["Name"].ToString().Equals("WhiteClover", StringComparison.InvariantCultureIgnoreCase))
                        JsonUtilities.RenameModel(pastureSpecies, "AGPWhiteClover");
                    pastureSpecies["ResourceName"] = pastureSpecies["Name"];

                    var swardParentChildren = JsonUtilities.Parent(sward)["Children"] as JArray;
                    swardParentChildren.Add(pastureSpecies);
                }
                sward.Remove();
            }

            bool foundAgPastureWhiteClover = false; // as opposed to a PMF whiteclover
            foreach (JObject pastureSpecies in JsonUtilities.ChildrenRecursively(root, "PastureSpecies"))
            {
                if (pastureSpecies["Name"].ToString().Equals("Ryegrass", StringComparison.InvariantCultureIgnoreCase))
                    JsonUtilities.RenameModel(pastureSpecies, "AGPRyegrass");
                if (pastureSpecies["Name"].ToString().Equals("WhiteClover", StringComparison.InvariantCultureIgnoreCase))
                {
                    JsonUtilities.RenameModel(pastureSpecies, "AGPWhiteClover");
                    foundAgPastureWhiteClover = true;
                }

                pastureSpecies["ResourceName"] = pastureSpecies["Name"];
            }

            foreach (JObject soilCrop in JsonUtilities.ChildrenRecursively(root, "SoilCrop"))
            {
                if (soilCrop["Name"].ToString().Equals("SwardSoil", StringComparison.InvariantCultureIgnoreCase))
                    soilCrop.Remove();
                if (soilCrop["Name"].ToString().Equals("RyegrassSoil", StringComparison.InvariantCultureIgnoreCase))
                    JsonUtilities.RenameModel(soilCrop, "AGPRyegrassSoil");
                if (foundAgPastureWhiteClover && soilCrop["Name"].ToString().Equals("WhiteCloverSoil", StringComparison.InvariantCultureIgnoreCase))
                    JsonUtilities.RenameModel(soilCrop, "AGPWhiteCloverSoil");
            }
        }

        /// <summary>
        /// Upgrades to version 69. Fixes link attributes in manager scripts after
        /// link refactoring.
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the .apsimx file.</param>
        private static void UpgradeToVersion69(JObject root, string fileName)
        {
            foreach (ManagerConverter manager in JsonUtilities.ChildManagers(root))
            {
                // The linking code previously had a hack which automatically enabled link by name if the target
                // model type is IFunction or Biomass (or any inherited class thereof). I've removed this hack 
                // from the link resolution code, which means that all such links must be adjusted accordingly.

                // [Link(...)] [Units] [...] Biomass -> [Link(ByName = true, ...)] [Units] [...] Biomass
                manager.Parser.ReplaceRegex(@"\[Link\(([^\)]+)\)\]((\s*\[[^\]]+\])*\s*(public|private|protected|internal|static|readonly| )*\s*(IFunction|Biomass|CNReductionForCover|CNReductionForTillage|RunoffModel|WaterTableModel|HeightFunction|DecumulateFunction|EndOfDayFunction|LiveOnEventFunction|AccumulateAtEvent|DailyMeanVPD|CERESDenitrificationWaterFactor|CERESDenitrificationTemperatureFactor|CERESMineralisationFOMCNRFactor|DayCentN2OFractionModel|CERESNitrificationpHFactor|CERESNitrificationWaterFactor|CERESUreaHydrolysisModel|CERESMineralisationWaterFactor|CERESMineralisationTemperatureFactor|CERESNitrificationModel|StringComparisonFunction|AccumulateByDate|AccumulateByNumericPhase|TrackerFunction|ArrayFunction|WangEngelTempFunction|BoundFunction|LinearAfterThresholdFunction|SoilWaterScale|MovingAverageFunction|HoldFunction|DeltaFunction|MovingSumFunction|QualitativePPEffect|AccumulateFunction|AddFunction|AgeCalculatorFunction|AirTemperatureFunction|BellCurveFunction|Constant|DivideFunction|ExponentialFunction|ExpressionFunction|ExternalVariable|LessThanFunction|LinearInterpolationFunction|MaximumFunction|MinimumFunction|MultiplyFunction|OnEventFunction|PhaseBasedSwitch|PhaseLookup|PhaseLookupValue|PhotoperiodDeltaFunction|PhotoperiodFunction|PowerFunction|SigmoidFunction|SoilTemperatureDepthFunction|SoilTemperatureFunction|SoilTemperatureWeightedFunction|SplineInterpolationFunction|StageBasedInterpolation|SubtractFunction|VariableReference|WeightedTemperatureFunction|XYPairs|CanopyPhotosynthesis|RUECO2Function|RUEModel|StorageDMDemandFunction|StorageNDemandFunction|InternodeCohortDemandFunction|BerryFillingRateFunction|TEWaterDemandFunction|FillingRateFunction|AllometricDemandFunction|InternodeDemandFunction|PartitionFractionDemandFunction|PopulationBasedDemandFunction|PotentialSizeDemandFunction|RelativeGrowthRateDemandFunction))", @"[Link(Type = LinkType.Child, ByName = true, $1)]$2");

                // [Link] IFunction -> [Link(ByName = true)] IFunction
                manager.Parser.ReplaceRegex(@"\[Link\]((\s*\[[^\]]+\])*\s*(public|private|protected|internal|static|readonly| )*\s*(IFunction|Biomass|CNReductionForCover|CNReductionForTillage|RunoffModel|WaterTableModel|HeightFunction|DecumulateFunction|EndOfDayFunction|LiveOnEventFunction|AccumulateAtEvent|DailyMeanVPD|CERESDenitrificationWaterFactor|CERESDenitrificationTemperatureFactor|CERESMineralisationFOMCNRFactor|DayCentN2OFractionModel|CERESNitrificationpHFactor|CERESNitrificationWaterFactor|CERESUreaHydrolysisModel|CERESMineralisationWaterFactor|CERESMineralisationTemperatureFactor|CERESNitrificationModel|StringComparisonFunction|AccumulateByDate|AccumulateByNumericPhase|TrackerFunction|ArrayFunction|WangEngelTempFunction|BoundFunction|LinearAfterThresholdFunction|SoilWaterScale|MovingAverageFunction|HoldFunction|DeltaFunction|MovingSumFunction|QualitativePPEffect|AccumulateFunction|AddFunction|AgeCalculatorFunction|AirTemperatureFunction|BellCurveFunction|Constant|DivideFunction|ExponentialFunction|ExpressionFunction|ExternalVariable|LessThanFunction|LinearInterpolationFunction|MaximumFunction|MinimumFunction|MultiplyFunction|OnEventFunction|PhaseBasedSwitch|PhaseLookup|PhaseLookupValue|PhotoperiodDeltaFunction|PhotoperiodFunction|PowerFunction|SigmoidFunction|SoilTemperatureDepthFunction|SoilTemperatureFunction|SoilTemperatureWeightedFunction|SplineInterpolationFunction|StageBasedInterpolation|SubtractFunction|VariableReference|WeightedTemperatureFunction|XYPairs|CanopyPhotosynthesis|RUECO2Function|RUEModel|StorageDMDemandFunction|StorageNDemandFunction|InternodeCohortDemandFunction|BerryFillingRateFunction|TEWaterDemandFunction|FillingRateFunction|AllometricDemandFunction|InternodeDemandFunction|PartitionFractionDemandFunction|PopulationBasedDemandFunction|PotentialSizeDemandFunction|RelativeGrowthRateDemandFunction))", @"[Link(Type = LinkType.Child, ByName = true)]$1");

                // Here I assume that all [LinkByPath] links will have a path argument supplied.
                // [LinkByPath(...)] -> [Link(Type = LinkType.Path, ...)]
                manager.Parser.ReplaceRegex(@"\[LinkByPath\(([^\)]+)\)", @"[Link(Type = LinkType.Path, $1)");

                // [ParentLink(...)] -> [Link(Type = LinkType.Ancestor, ...)]
                manager.Parser.ReplaceRegex(@"\[ParentLink\(([^\)]+)\)", @"[Link(Type = LinkType.Ancestor, $1)");

                // [ParentLink] -> [Link(Type = LinkType.Ancestor, ByName = false)]
                manager.Parser.Replace("[ParentLink]", "[Link(Type = LinkType.Ancestor)]", caseSensitive: true);

                // [ScopedLinkByName(...)] -> [Link(ByName = true, ...)]
                manager.Parser.ReplaceRegex(@"\[ScopedLinkByName\(([^\)]+)\)", @"[Link(ByName = true, $1)");

                // [ScopedLinkByName] -> [Link(ByName = true)]
                manager.Parser.Replace("[ScopedLinkByName]", "[Link(ByName = true)]", caseSensitive: true);

                // [ScopedLink(...)] -> [Link(...)]
                manager.Parser.ReplaceRegex(@"\[ScopedLink\(([^\)]+)\)", @"[Link($1)");

                // [ScopedLink] -> [Link]
                manager.Parser.Replace("[ScopedLink]", "[Link]", caseSensitive: true);

                // [ChildLinkByName(...)] -> [Link(Type = LinkType.Child, ByName = true, ...)]
                manager.Parser.ReplaceRegex(@"\[ChildLinkByName\(([^\)]+)\)", @"[Link(Type = LinkType.Child, ByName = true, $1)");

                // [ChildLinkByName] -> [Link(Type = LinkType.Child, ByName = true)]
                manager.Parser.Replace("[ChildLinkByName]", "[Link(Type = LinkType.Child, ByName = true)]", caseSensitive: true);

                // [ChildLink(...)] -> [Link(Type = LinkType.Child, ...)]
                manager.Parser.ReplaceRegex(@"\[ChildLink\(([^\)]+)\)", @"[Link(Type = LinkType.Child, $1)");

                // [ChildLink] -> [Link(Type = LinkType.Child)]
                manager.Parser.Replace("[ChildLink]", "[Link(Type = LinkType.Child)]", caseSensitive: true);

                manager.Save();
            }
        }

        /// <summary>
        /// Changes the type of the Stock component inital values genotypes array
        /// from StockGeno to SingleGenotypeInits.
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion70(JObject root, string fileName)
        {
            foreach (var stockNode in JsonUtilities.ChildrenOfType(root, "Stock"))
            {
                // for each element of GenoTypes[]
                var genotypes = stockNode["GenoTypes"];
                for (int i = 0; i < genotypes.Count(); i++)
                {
                    genotypes[i]["$type"] = "Models.GrazPlan.SingleGenotypeInits, Models";
                    double dr = Convert.ToDouble(genotypes[i]["DeathRate"]);
                    double drw = Convert.ToDouble(genotypes[i]["WnrDeathRate"]);
                    genotypes[i]["DeathRate"] = new JArray(new double[] { dr, drw });
                    genotypes[i]["PotFleeceWt"] = genotypes[i]["RefFleeceWt"];
                    genotypes[i]["Conceptions"] = genotypes[i]["Conception"];
                    genotypes[i]["GenotypeName"] = genotypes[i]["Name"];
                }
            }
        }

        /// <summary>
        /// Alters all existing linint functions to have a child variable reference IFunction called XValue instead of a
        /// string property called XProperty that IFunction then had to locate
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion71(JObject root, string fileName)
        {
            foreach (JObject linint in JsonUtilities.ChildrenRecursively(root, "LinearInterpolationFunction"))
            {
                VariableReference varRef = new VariableReference();
                varRef.Name = "XValue";
                varRef.VariableName = linint["XProperty"].ToString();
                JsonUtilities.AddModel(linint, varRef);
                linint.Remove("XProperty");
            }
        }

        /// <summary>
        /// Remove .Value() from all variable references because it is redundant
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion72(JObject root, string fileName)
        {
            foreach (var varRef in JsonUtilities.ChildrenRecursively(root, "VariableReference"))
                varRef["VariableName"] = varRef["VariableName"].ToString().Replace(".Value()", "");

            foreach (var report in JsonUtilities.ChildrenOfType(root, "Report"))
                JsonUtilities.SearchReplaceReportVariableNames(report, ".Value()", "");

            foreach (var graph in JsonUtilities.ChildrenOfType(root, "Series"))
            {
                if (graph["XFieldName"] != null)
                    graph["XFieldName"] = graph["XFieldName"].ToString().Replace(".Value()", "");
                if (graph["X2FieldName"] != null)
                    graph["X2FieldName"] = graph["X2FieldName"].ToString().Replace(".Value()", "");
                if (graph["YFieldName"] != null)
                    graph["YFieldName"] = graph["YFieldName"].ToString().Replace(".Value()", "");
                if (graph["Y2FieldName"] != null)
                    graph["Y2FieldName"] = graph["Y2FieldName"].ToString().Replace(".Value()", "");
            }
        }

        /// <summary>
        /// Alters all existing AllometricDemand functions to have a child variable reference IFunction called XValue and YValue instead of 
        /// string property called XProperty and YProperty that it then had to locate.  Aiming to get all things using get for properties
        /// to be doing it via Variable reference so we can stream line scoping rules
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion73(JObject root, string fileName)
        {
            foreach (JObject Alomet in JsonUtilities.ChildrenRecursively(root, "AllometricDemandFunction"))
            {
                VariableReference XvarRef = new VariableReference();
                XvarRef.Name = "XValue";
                XvarRef.VariableName = Alomet["XProperty"].ToString();
                JsonUtilities.AddModel(Alomet, XvarRef);
                Alomet.Remove("XProperty");
                VariableReference YvarRef = new VariableReference();
                YvarRef.Name = "YValue";
                YvarRef.VariableName = Alomet["YProperty"].ToString();
                JsonUtilities.AddModel(Alomet, YvarRef);
                Alomet.Remove("YProperty");

            }
        }

        /// <summary>
        /// Changes the Surface Organic Matter property FractionFaecesAdded to 1.0
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion74(JObject root, string fileName)
        {
            foreach (JObject som in JsonUtilities.ChildrenRecursively(root, "SurfaceOrganicMatter"))
                som["FractionFaecesAdded"] = "1.0";
        }

        /// <summary>
        /// Change TreeLeafAreas to ShadeModiers in Tree Proxy
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion75(JObject root, string fileName)
        {
            foreach (JObject TreeProxy in JsonUtilities.ChildrenRecursively(root, "TreeProxy"))
            {
                TreeProxy["ShadeModifiers"] = TreeProxy["TreeLeafAreas"];
                // ShadeModifiers is sometimes null (not sure why) so fill it with 1s using Heights to get array length
                var SM = TreeProxy["Heights"].Values<double>().ToArray();
                for (int i = 0; i < SM.Count(); i++)
                    SM[i] = 1.0;
                TreeProxy["ShadeModifiers"] = new JArray(SM);
            }
        }

        /// <summary>
        /// Change flow_urea to FlowUrea in soil water
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion76(JObject root, string fileName)
        {
            foreach (var manager in JsonUtilities.ChildManagers(root))
            {
                if (manager.Parser.Replace(".flow_urea", ".FlowUrea"))
                    manager.Save();
            }
            foreach (var report in JsonUtilities.ChildrenOfType(root, "Report"))
            {
                JsonUtilities.SearchReplaceReportVariableNames(report, ".flow_urea", ".FlowUrea");
            }
        }

        /// <summary>
        /// Change the property in Stock to Genotypes
        /// </summary>
        /// <param name="root"></param>
        /// <param name="fileName"></param>
        private static void UpgradeToVersion77(JObject root, string fileName)
        {
            foreach (var manager in JsonUtilities.ChildManagers(root))
            {
                if (manager.Parser.Replace(".GenoTypes", ".Genotypes"))
                    manager.Save();
            }
            foreach (var stock in JsonUtilities.ChildrenOfType(root, "Stock"))
            {
                JsonUtilities.SearchReplaceReportVariableNames(stock, ".GenoTypes", ".Genotypes");
            }
        }

        /// <summary>
        /// Change the namespace for SimpleGrazing
        /// </summary>
        /// <param name="root"></param>
        /// <param name="fileName"></param>
        private static void UpgradeToVersion78(JObject root, string fileName)
        {
            foreach (var simpleGrazing in JsonUtilities.ChildrenOfType(root, "SimpleGrazing"))
            {
                simpleGrazing["$type"] = "Models.AgPasture.SimpleGrazing, Models";
            }
        }

        /// <summary>
        /// Change manager method and AgPasture variable names.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="fileName"></param>
        private static void UpgradeToVersion79(JObject root, string fileName)
        {
            Tuple<string, string>[] changes =
            {
                new Tuple<string, string>(".Graze(", ".RemoveBiomass("),
                new Tuple<string, string>(".EmergingTissuesWt",   ".EmergingTissue.Wt"),
                new Tuple<string, string>(".EmergingTissuesN",    ".EmergingTissue.N"),
                new Tuple<string, string>(".DevelopingTissuesWt", ".DevelopingTissue.Wt"),
                new Tuple<string, string>(".DevelopingTissuesN",  ".DevelopingTissue.N"),
                new Tuple<string, string>(".MatureTissuesWt", ".MatureTissue.Wt"),
                new Tuple<string, string>(".MatureTissuesN",  ".MatureTissue.N"),
                new Tuple<string, string>(".DeadTissuesWt", ".DeadTissue.Wt"),
                new Tuple<string, string>(".DeadTissuesN",  ".DeadTissue.N")
            };

            JsonUtilities.RenameVariables(root, changes);
        }

        /// <summary>
        /// Replace ExcelMultiInput with ExcelInput.
        /// Change ExcelInput.FileName from a string into a string[].
        /// </summary>
        /// <param name="root"></param>
        /// <param name="fileName"></param>
        private static void UpgradeToVersion80(JObject root, string fileName)
        {
            // Rename ExcelInput.FileName to FileNames and make it an array.
            foreach (JObject excelInput in JsonUtilities.ChildrenRecursively(root, "ExcelInput"))
            {
                if (excelInput["FileName"] != null)
                    excelInput["FileNames"] = new JArray(excelInput["FileName"].Value<string>());
            }

            // Replace ExcelMultiInput with an ExcelInput.
            foreach (JObject excelMultiInput in JsonUtilities.ChildrenRecursively(root, "ExcelMultiInput"))
            {
                excelMultiInput["$type"] = "Models.PostSimulationTools.ExcelInput, Models";
            }
        }


        /// <summary>
        /// Seperate life cycle process class into Growth, Transfer and Mortality classes.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="fileName"></param>
        private static void UpgradeToVersion81(JObject root, string fileName)
        {
            // Rename ExcelInput.FileName to FileNames and make it an array.
            foreach (JObject LSP in JsonUtilities.ChildrenRecursively(root, "LifeStageProcess"))
            {
                if (int.Parse(LSP["ProcessAction"].ToString()) == 0) //Process is Transfer
                {
                    LSP["$type"] = "Models.LifeCycle.LifeStageTransfer, Models";
                }
                else if (int.Parse(LSP["ProcessAction"].ToString()) == 1) //Process is PhysiologicalGrowth
                {
                    LSP["$type"] = "Models.LifeCycle.LifeStageGrowth, Models";
                }
                else if (int.Parse(LSP["ProcessAction"].ToString()) == 2) //Process is Mortality
                {
                    LSP["$type"] = "Models.LifeCycle.LifeStageMortality, Models";
                }

            }

            foreach (JObject LSRP in JsonUtilities.ChildrenRecursively(root, "LifeStageReproductionProcess"))
            {
                LSRP["$type"] = "Models.LifeCycle.LifeStageReproduction, Models";
            }

            foreach (JObject LSIP in JsonUtilities.ChildrenRecursively(root, "LifeStageImmigrationProcess"))
            {
                LSIP["$type"] = "Models.LifeCycle.LifeStageImmigration, Models";
            }
        }

        /// <summary>
        /// Add Critical N Conc (if not existing) to all Root Objects by copying the maximum N conc
        /// </summary>
        /// <param name="root"></param>
        /// <param name="fileName"></param>
        private static void UpgradeToVersion82(JObject root, string fileName)
        {
            foreach (JObject r in JsonUtilities.ChildrenRecursively(root, "Root"))
            {
                if (JsonUtilities.ChildWithName(r, "CriticalNConc") == null)
                {
                    JObject minNConc = JsonUtilities.ChildWithName(r, "MinimumNConc");
                    if (minNConc == null)
                        throw new Exception("Root has no CriticalNConc or MaximumNConc");

                    VariableReference varRef = new VariableReference();
                    varRef.Name = "CriticalNConc";
                    varRef.VariableName = "[Root].MinimumNConc";
                    JsonUtilities.AddModel(r, varRef);
                }
            }
        }

        /// <summary>
        /// Remove .Value() from everywhere possible.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="fileName"></param>
        private static void UpgradeToVersion83(JObject root, string fileName)
        {
            // 1. Report
            foreach (JObject report in JsonUtilities.ChildrenRecursively(root, "Report"))
            {
                JArray variables = report["VariableNames"] as JArray;
                if (variables == null)
                    continue;

                for (int i = 0; i < variables.Count; i++)
                    variables[i] = variables[i].ToString().Replace(".Value()", "");
            }

            // 2. ExpressionFunction
            foreach (JObject function in JsonUtilities.ChildrenRecursively(root, "ExpressionFunction"))
                function["Expression"] = function["Expression"].ToString().Replace(".Value()", "");
        }

        /// <summary>
        /// Renames the Input.FileName property to FileNames and makes it an array.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="fileName"></param>
        private static void UpgradeToVersion84(JObject root, string fileName)
        {
            foreach (JObject input in JsonUtilities.ChildrenRecursively(root, "Input"))
                if (input["FileName"] != null)
                    input["FileNames"] = new JArray(input["FileName"]);
        }

        /// <summary>
        /// Add a field to the Checkpoints table.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="fileName"></param>
        private static void UpgradeToVersion85(JObject root, string fileName)
        {
            SQLite db = new SQLite();
            var dbFileName = Path.ChangeExtension(fileName, ".db");
            if (File.Exists(dbFileName))
            {
                try
                {
                    db.OpenDatabase(dbFileName, false);
                    if (db.TableExists("_Checkpoints"))
                    {
                        if (!db.GetTableColumns("_Checkpoints").Contains("OnGraphs"))
                        {
                            db.AddColumn("_Checkpoints", "OnGraphs", "integer");
                        }
                    }
                }
                finally
                {
                    db.CloseDatabase();
                }
            }
        }

        /// <summary>
        /// Add new methods structure to OrganArbitrator.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="fileName"></param>
        private static void UpgradeToVersion86(JObject root, string fileName)
        {
            foreach (var arbitrator in JsonUtilities.ChildrenOfType(root, "OrganArbitrator"))
            {
                //remove DMArbitrator, and NArbitrator
                var children = JsonUtilities.Children(arbitrator);
                var exdm = children.Find(c => JsonUtilities.Name(c).Equals("dmArbitrator", StringComparison.OrdinalIgnoreCase));
                JsonUtilities.RemoveChild(arbitrator, JsonUtilities.Name(exdm));

                var exn = children.Find(c => JsonUtilities.Name(c).Equals("nArbitrator", StringComparison.OrdinalIgnoreCase));
                JsonUtilities.RemoveChild(arbitrator, JsonUtilities.Name(exn));

                JsonUtilities.RenameModel(exdm, "ArbitrationMethod");
                JsonUtilities.RenameModel(exn, "ArbitrationMethod");

                //Add DMArbitration
                var dm = JsonUtilities.CreateNewChildModel(arbitrator, "DMArbitration", "Models.PMF.BiomassTypeArbitrator");
                var folder = JsonUtilities.CreateNewChildModel(dm, "PotentialPartitioningMethods", "Models.Core.Folder");
                JsonUtilities.CreateNewChildModel(folder, "ReallocationMethod", "Models.PMF.Arbitrator.ReallocationMethod");
                JsonUtilities.CreateNewChildModel(folder, "AllocateFixationMethod", "Models.PMF.Arbitrator.AllocateFixationMethod");
                JsonUtilities.CreateNewChildModel(folder, "RetranslocationMethod", "Models.PMF.Arbitrator.RetranslocationMethod");
                JsonUtilities.CreateNewChildModel(folder, "SendPotentialDMAllocationsMethod", "Models.PMF.Arbitrator.SendPotentialDMAllocationsMethod");

                folder = JsonUtilities.CreateNewChildModel(dm, "AllocationMethods", "Models.Core.Folder");
                JsonUtilities.CreateNewChildModel(folder, "NutrientConstrainedAllocationMethod", "Models.PMF.Arbitrator.NutrientConstrainedAllocationMethod");
                JsonUtilities.CreateNewChildModel(folder, "DryMatterAllocationsMethod", "Models.PMF.Arbitrator.DryMatterAllocationsMethod");

                JArray dmChildren = dm["Children"] as JArray;
                dmChildren.Add(exdm);

                //Add N Arbitration
                var n = JsonUtilities.CreateNewChildModel(arbitrator, "NArbitration", "Models.PMF.BiomassTypeArbitrator");
                folder = JsonUtilities.CreateNewChildModel(n, "PotentialPartitioningMethods", "Models.Core.Folder");
                JsonUtilities.CreateNewChildModel(folder, "ReallocationMethod", "Models.PMF.Arbitrator.ReallocationMethod");

                folder = JsonUtilities.CreateNewChildModel(n, "ActualPartitioningMethods", "Models.Core.Folder");
                JsonUtilities.CreateNewChildModel(folder, "AllocateFixationMethod", "Models.PMF.Arbitrator.AllocateFixationMethod");
                JsonUtilities.CreateNewChildModel(folder, "RetranslocationMethod", "Models.PMF.Arbitrator.RetranslocationMethod");

                folder = JsonUtilities.CreateNewChildModel(n, "AllocationMethods", "Models.Core.Folder");
                JsonUtilities.CreateNewChildModel(folder, "NitrogenAllocationsMethod", "Models.PMF.Arbitrator.NitrogenAllocationsMethod");

                JArray nChildren = n["Children"] as JArray;
                nChildren.Add(exn);
                var allocatesMethod = JsonUtilities.CreateNewChildModel(n, "AllocateUptakesMethod", "Models.PMF.Arbitrator.AllocateUptakesMethod");

                var water = JsonUtilities.CreateNewChildModel(arbitrator, "WaterUptakeMethod", "Models.PMF.Arbitrator.WaterUptakeMethod");
                var nitrogen = JsonUtilities.CreateNewChildModel(arbitrator, "NitrogenUptakeMethod", "Models.PMF.Arbitrator.NitrogenUptakeMethod");
            }
        }

        /// <summary>
        /// Remove Models.Report namespace.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="fileName"></param>
        private static void UpgradeToVersion87(JObject root, string fileName)
        {
            // Fix type of Report nodes
            foreach (JObject report in JsonUtilities.ChildrenRecursively(root, "Report"))
                report["$type"] = report["$type"].ToString().Replace("Report.Report", "Report");

            // Fix type of all models in the now-removed Models.Graph namespace
            foreach (JObject model in JsonUtilities.ChildrenRecursively(root))
                model["$type"] = model["$type"].ToString().Replace("Models.Graph.", "Models.");

            // Fix graph axes - these are a property of graphs, not a model themselves
            foreach (JObject graph in JsonUtilities.ChildrenRecursively(root, "Graph"))
            {
                JArray axes = graph["Axis"] as JArray;
                if (axes != null)
                    foreach (JObject axis in axes)
                        axis["$type"] = axis["$type"].ToString().Replace("Models.Graph", "Models");
            }

            // Fix nutrient directed graphs - the nodes/arcs are not children, but
            // need to have their types fixed.
            foreach (JObject nutrient in JsonUtilities.ChildrenRecursively(root, "Nutrient"))
            {
                JObject directedGraph = nutrient["DirectedGraphInfo"] as JObject;
                if (directedGraph != null)
                {
                    directedGraph["$type"] = directedGraph["$type"].ToString().Replace("Models.Graph.", "Models.");
                    JArray nodes = directedGraph["Nodes"] as JArray;
                    if (nodes != null)
                        foreach (JObject node in nodes)
                            node["$type"] = node["$type"].ToString().Replace("Models.Graph.", "Models.");
                    JArray arcs = directedGraph["Arcs"] as JArray;
                    if (arcs != null)
                        foreach (JObject arc in arcs)
                            arc["$type"] = arc["$type"].ToString().Replace("Models.Graph.", "Models.");
                }
            }

            // Replace ExcelMultiInput with an ExcelInput.
            foreach (ManagerConverter manager in JsonUtilities.ChildManagers(root))
            {
                manager.Parser.Replace("Models.Report", "Models");
                manager.Parser.Replace("using Report", "using Models");
                //manager.ReplaceRegex("(using Models.+)using Models", "$1");
                manager.Parser.Replace("Report.Report", "Report");

                manager.Parser.Replace("Models.Graph", "Models");
                manager.Parser.Replace("Graph.Graph", "Graph");

                List<string> usingStatements = manager.Parser.GetUsingStatements().ToList();
                usingStatements.Remove("Models.Graph");
                usingStatements.Remove("Graph");

                manager.Parser.SetUsingStatements(usingStatements.Distinct());

                manager.Save();
            }
        }

        /// <summary>
        /// Replace SoilWater model with WaterBalance model.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="fileName"></param>
        private static void UpgradeToVersion88(JObject root, string fileName)
        {
            foreach (JObject soilWater in JsonUtilities.ChildrenRecursively(root, "SoilWater"))
            {
                soilWater["$type"] = "Models.WaterModel.WaterBalance, Models";
                soilWater["ResourceName"] = "WaterBalance";
                if (soilWater["discharge_width"] != null)
                    soilWater["DischargeWidth"] = soilWater["discharge_width"];
                if (soilWater["catchment_area"] != null)
                    soilWater["CatchmentArea"] = soilWater["catchment_area"];
            }

            foreach (var manager in JsonUtilities.ChildManagers(root))
            {
                bool managerChanged = false;

                var declarations = manager.Parser.GetDeclarations();
                foreach (var declaration in declarations)
                {
                    if (declaration.TypeName == "SoilWater")
                    {
                        declaration.TypeName = "ISoilWater";
                        managerChanged = true;
                    }
                }

                if (managerChanged)
                {
                    manager.Parser.SetDeclarations(declarations);

                    var usings = manager.Parser.GetUsingStatements().ToList();
                    if (!usings.Contains("Models.Interfaces"))
                    {
                        usings.Add("Models.Interfaces");
                        manager.Parser.SetUsingStatements(usings);
                    }
                }

                if (manager.Parser.Replace(" as SoilWater", ""))
                    managerChanged = true;
                if (manager.Parser.Replace("solute_flow_eff", "SoluteFlowEfficiency"))
                    managerChanged = true;
                if (manager.Parser.Replace("solute_flux_eff", "SoluteFluxEfficiency"))
                    managerChanged = true;
                if (manager.Parser.Replace("[EventSubscribe(\"Commencing\")", "[EventSubscribe(\"StartOfSimulation\")"))
                    managerChanged = true;

                if (managerChanged)
                    manager.Save();
            }

            foreach (var report in JsonUtilities.ChildrenOfType(root, "Report"))
            {
                JsonUtilities.SearchReplaceReportVariableNames(report, "solute_flow_eff", "SoluteFlowEfficiency");
                JsonUtilities.SearchReplaceReportVariableNames(report, "solute_flux_eff", "SoluteFluxEfficiency");
            }

        }

        /// <summary>
        /// Replace 'avg' with 'mean' in report variables.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="fileName"></param>
        private static void UpgradeToVersion89(JObject root, string fileName)
        {
            foreach (var report in JsonUtilities.ChildrenOfType(root, "Report"))
                JsonUtilities.SearchReplaceReportVariableNames(report, "avg of ", "mean of ");
        }

        /// <summary>
        /// Fixes a bug where a manager script's children were being serialized.
        /// When attempting to reopen the file, the script's type cannot be resolved.
        /// This converter will strip out all script children of managers under replacements.
        /// </summary>
        /// <param name="root">Root node.</param>
        /// <param name="fileName">Path to the .apsimx file.</param>
        private static void UpgradeToVersion90(JObject root, string fileName)
        {
            foreach (JObject replacements in JsonUtilities.ChildrenRecursively(root, "Replacements"))
                foreach (JObject manager in JsonUtilities.ChildrenRecursively(root, "Manager"))
                    JsonUtilities.RemoveChild(manager, "Script");
        }

        /// <summary>
        /// </summary>
        /// <param name="root"></param>
        /// <param name="fileName"></param>
        private static void UpgradeToVersion91(JObject root, string fileName)
        {
            Tuple<string, string>[] changes =
            {
                new Tuple<string, string>(".HarvestableWt",          ".Harvestable.Wt"),
                new Tuple<string, string>(".HarvestableN",           ".Harvestable.N"),
                new Tuple<string, string>(".StandingHerbageWt",      ".Standing.Wt"),
                new Tuple<string, string>(".StandingHerbageN",       ".Standing.N"),
                new Tuple<string, string>(".StandingHerbageNConc",   ".Standing.NConc"),
                new Tuple<string, string>(".StandingLiveHerbageWt",  ".StandingLive.Wt"),
                new Tuple<string, string>(".StandingLiveHerbageN",   ".StandingLive.N"),
                new Tuple<string, string>(".StandingDeadHerbageWt",  ".StandingDead.Wt"),
                new Tuple<string, string>(".StandingDeadHerbageN",   ".StandingDead.N"),
                new Tuple<string, string>(".HerbageDigestibility",   ".Standing.Digestibility"),
                new Tuple<string, string>(".RootDepthMaximum",       ".Root.RootDepthMaximum"),
                new Tuple<string, string>("[AGPRyeGrass].RootLengthDensity", "[AGPRyeGrass].Root.RootLengthDensity"),
                new Tuple<string, string>("[AGPWhiteClover].RootLengthDensity", "[AGPWhiteClover].Root.RootLengthDensity"),
                new Tuple<string, string>("[AGPLucerne].RootLengthDensity", "[AGPLucerne].Root.RootLengthDensity")
            };
            JsonUtilities.RenameVariables(root, changes);
        }

        /// <summary>
        /// Change names of a couple of parameters in SimpleGrazing.
        /// </summary>
        /// <param name="root">Root node.</param>
        /// <param name="fileName">Path to the .apsimx file.</param>
        private static void UpgradeToVersion92(JObject root, string fileName)
        {
            foreach (JObject simpleGrazing in JsonUtilities.ChildrenRecursively(root, "SimpleGrazing"))
            {
                simpleGrazing["FractionExcretedNToDung"] = simpleGrazing["FractionOfBiomassToDung"];
                if (simpleGrazing["FractionNExportedInAnimal"] == null)
                    simpleGrazing["FractionNExportedInAnimal"] = 0.75;
            }

            Tuple<string, string>[] changes =
            {
                new Tuple<string, string>(".AmountDungCReturned",  ".AmountDungWtReturned")
            };
            JsonUtilities.RenameVariables(root, changes);
        }

        /// <summary>
        /// In SimpleGrazin, Turn "Fraction of defoliated N leaving the system" into a fraction of defoliated N going to soil.
        /// </summary>
        /// <param name="root">Root node.</param>
        /// <param name="fileName">Path to the .apsimx file.</param>
        private static void UpgradeToVersion93(JObject root, string fileName)
        {
            foreach (JObject simpleGrazing in JsonUtilities.ChildrenRecursively(root, "SimpleGrazing"))
            {
                if (simpleGrazing["FractionNExportedInAnimal"] != null)
                {
                    var fractionNExportedInAnimal = Convert.ToDouble(simpleGrazing["FractionNExportedInAnimal"].Value<double>());
                    simpleGrazing["FractionDefoliatedNToSoil"] = 1 - fractionNExportedInAnimal;

                }
                if (simpleGrazing["FractionExcretedNToDung"] != null)
                {
                    var fractionExcretedNToDung = simpleGrazing["FractionExcretedNToDung"] as JArray;
                    if (fractionExcretedNToDung.Count > 0)
                        simpleGrazing["CNRatioDung"] = "NaN";
                }
            }
        }

        /// <summary>
        /// Convert stock genotypes array into GenotypeCross child models.
        /// </summary>
        /// <param name="root">Root node.</param>
        /// <param name="fileName">Path to the .apsimx file.</param>
        private static void UpgradeToVersion94(JObject root, string fileName)
        {
            foreach (JObject stock in JsonUtilities.ChildrenRecursively(root, "Stock"))
            {
                var oldGenotypes = stock["Genotypes"] as JArray;
                if (oldGenotypes != null)
                {
                    var newGenotypes = new JArray();
                    foreach (var oldgenotype in oldGenotypes)
                    {
                        var genotypeCross = new JObject();
                        genotypeCross["$type"] = "Models.GrazPlan.GenotypeCross, Models";
                        genotypeCross["Name"] = oldgenotype["GenotypeName"];
                        var oldgenotypeDeathRates = oldgenotype["DeathRate"] as JArray;
                        if (oldgenotypeDeathRates != null && oldgenotypeDeathRates.Count == 2)
                        {
                            genotypeCross["MatureDeathRate"] = oldgenotypeDeathRates[0];
                            genotypeCross["WeanerDeathRate"] = oldgenotypeDeathRates[1];
                        }
                        genotypeCross["Conception"] = oldgenotype["Conceptions"];

                        if (string.IsNullOrEmpty(oldgenotype["DamBreed"].ToString()))
                            genotypeCross["PureBredBreed"] = oldgenotype["GenotypeName"];
                        else if (string.IsNullOrEmpty(oldgenotype["SireBreed"].ToString()))
                            genotypeCross["PureBredBreed"] = oldgenotype["DamBreed"];
                        else
                            genotypeCross["DamBreed"] = oldgenotype["DamBreed"];
                        genotypeCross["SireBreed"] = oldgenotype["SireBreed"];
                        genotypeCross["Generation"] = oldgenotype["Generation"];
                        genotypeCross["SRW"] = oldgenotype["SRW"];
                        genotypeCross["PotFleeceWt"] = oldgenotype["PotFleeceWt"];
                        genotypeCross["MaxFibreDiam"] = oldgenotype["MaxFibreDiam"];
                        genotypeCross["FleeceYield"] = oldgenotype["FleeceYield"];
                        genotypeCross["PeakMilk"] = oldgenotype["PeakMilk"];
                        newGenotypes.Add(genotypeCross);
                    }
                    stock.Remove("Genotypes");
                    if (newGenotypes.Count > 0)
                        stock["Children"] = newGenotypes;
                }
            }
            Tuple<string, string>[] changes =
            {
                new Tuple<string, string>(".GenotypeNamesAll()",  ".Genotypes.Names.ToArray()")
            };
            if (JsonUtilities.RenameVariables(root, changes))
            {
                // The replacement is in a manager. Need to make sure that LINQ is added as a using
                // because the .ToArray() depends on it.
                foreach (var manager in JsonUtilities.ChildManagers(root))
                {
                    var usings = manager.Parser.GetUsingStatements().ToList();
                    if (usings.Find(u => u.Contains("System.Linq")) == null)
                    {
                        usings.Add("System.Linq");
                        manager.Parser.SetUsingStatements(usings);
                        manager.Save();
                    }
                }
            }
        }

        /// <summary>
        /// Change initialDM on Generic organ and root from a single value to a BiomassPoolType so each type can be sepcified
        /// </summary>
        /// <param name="root">Root node.</param>
        /// <param name="fileName">Path to the .apsimx file.</param>
        private static void UpgradeToVersion95(JObject root, string fileName)
        {
            // Remove initalDM model and replace with initialBiomass object
            foreach (string org in new List<string>() { "GenericOrgan", "Root" })
            {
                foreach (JObject O in JsonUtilities.ChildrenRecursively(root, org))
                {
                    if (O["Enabled"].ToString() == "True")
                    {
                        string initName = org == "GenericOrgan" ? "InitialWtFunction" : "InitialDM";
                        JObject InitialWt = JsonUtilities.CreateNewChildModel(O, "InitialWt", "Models.PMF.BiomassDemand");
                        JObject Structural = JsonUtilities.ChildWithName(O, initName).DeepClone() as JObject;
                        Structural["Name"] = "Structural";
                        JArray ChildFunctions = new JArray();
                        ChildFunctions.Add(Structural);
                        InitialWt["Children"] = ChildFunctions;
                        JsonUtilities.AddConstantFunctionIfNotExists(InitialWt, "Metabolic", "0.0");
                        JsonUtilities.AddConstantFunctionIfNotExists(InitialWt, "Storage", "0.0");
                        JsonUtilities.RemoveChild(O, initName);
                    }
                }
            }
            // Altermanager code where initial root wt is being set.
            foreach (var manager in JsonUtilities.ChildrenOfType(root, "Manager"))
            {
                string code = manager["Code"].ToString();
                string[] lines = code.Split('\n');
                bool ContainsZoneInitalDM = false;
                for (int i = 0; i < lines.Count(); i++)
                {
                    if (lines[i].Contains("Root.ZoneInitialDM.Add(") && (ContainsZoneInitalDM == false))
                    {
                        ContainsZoneInitalDM = true;
                        string InitialDM = lines[i].Split('(')[1].Replace(";", "").Replace(")", "").Replace("\r", "").Replace("\n", "");
                        string NewCode = "BiomassDemand InitialDM = new BiomassDemand();\r\n" +
                                         "Constant InitStruct = new Constant();\r\n" +
                                         "InitStruct.FixedValue = " + InitialDM + ";\r\n" +
                                         "InitialDM.Structural = InitStruct;\r\n" +
                                         "Constant InitMetab = new Constant();\r\n" +
                                         "InitMetab.FixedValue = 0;\r\n" +
                                         "InitialDM.Metabolic = InitMetab;\r\n" +
                                         "Constant InitStor = new Constant();\r\n" +
                                         "InitStor.FixedValue = 0;\r\n" +
                                         "InitialDM.Storage = InitStor;\r\n" +
                                         lines[i].Split('(')[0] + "(InitialDM);\r\n";
                        lines[i] = NewCode;
                    }
                    else if (lines[i].Contains("Root.ZoneInitialDM.Add("))
                    {
                        string InitialDM = lines[i].Split('(')[1].Replace(";", "").Replace(")", "").Replace("\r", "").Replace("\n", "");
                        lines[i] = "InitStruct.FixedValue = " + InitialDM + ";\r\n" +
                                    lines[i].Split('(')[0] + "(InitialDM);\r\n";
                    }
                }

                if (ContainsZoneInitalDM)
                {
                    string newCode = "using Models.Functions;\r\n";
                    foreach (string line in lines)
                    {
                        newCode += line + "\n";
                    }
                    manager["Code"] = newCode;
                }
            }
        }

        /// <summary>
        /// Add RootShape to all simulations.
        /// </summary>
        /// <param name="root">Root node.</param>
        /// <param name="fileName">Path to the .apsimx file.</param>
        private static void UpgradeToVersion96(JObject root, string fileName)
        {
            foreach (JObject thisRoot in JsonUtilities.ChildrenRecursively(root, "Root"))
            {
                if (JsonUtilities.ChildrenRecursively(thisRoot, "RootShapeCylindre").Count == 0 &&
                    JsonUtilities.ChildrenRecursively(thisRoot, "RootShapeSemiCircle").Count == 0 &&
                    JsonUtilities.ChildrenRecursively(thisRoot, "RootShapeSemiCircleSorghum").Count == 0 &&
                    JsonUtilities.ChildrenRecursively(thisRoot, "RootShapeSemiEllipse").Count == 0)
                {
                    JArray rootChildren = thisRoot["Children"] as JArray;
                    if (rootChildren != null && rootChildren.Count > 0)
                    {
                        JToken thisPlant = JsonUtilities.Parent(thisRoot);

                        JArray rootShapeChildren = new JArray();
                        string type;

                        if (thisPlant["CropType"].ToString() == "Sorghum")
                        {
                            type = "Models.Functions.RootShape.RootShapeSemiCircleSorghum, Models";
                        }
                        else if (thisPlant["CropType"].ToString() == "C4Maize")
                        {
                            type = "Models.Functions.RootShape.RootShapeSemiCircle, Models";
                        }
                        else
                        {
                            type = "Models.Functions.RootShape.RootShapeCylindre, Models";
                        }

                        JObject rootShape = new JObject
                        {
                            ["$type"] = type,
                            ["Name"] = "RootShape",
                            ["Children"] = rootShapeChildren
                        };
                        rootChildren.AddFirst(rootShape);
                    }
                }
            }
        }

        /// <summary>
        /// Add RootShape to all simulations.
        /// </summary>
        /// <param name="root">Root node.</param>
        /// <param name="fileName">Path to the .apsimx file.</param>
        private static void UpgradeToVersion97(JObject root, string fileName)
        {
            foreach (JObject AirTempFunc in JsonUtilities.ChildrenOfType(root, "AirTemperatureFunction"))
            {
                JObject tempResponse = JsonUtilities.ChildWithName(AirTempFunc, "XYPairs");
                tempResponse["Name"] = "TemperatureResponse";
            }
        }

        /// <summary>
        /// Convert stock animalparamset
        /// </summary>
        /// <param name="root">Root node.</param>
        /// <param name="fileName">Path to the .apsimx file.</param>
        private static void UpgradeToVersion98(JObject root, string fileName)
        {
            foreach (JObject paramSet in JsonUtilities.ChildrenRecursively(root, "AnimalParamSet"))
            {
                paramSet["$type"] = "Models.GrazPlan.Genotype, Models";
            }
        }

        /// <summary>
        /// Add InterpolationMethod to AirTemperature Function.
        /// </summary>
        /// <param name="root">Root node.</param>
        /// <param name="fileName">Path to the .apsimx file.</param>
        private static void UpgradeToVersion99(JObject root, string fileName)
        {
            foreach (JObject AirTempFunc in JsonUtilities.ChildrenOfType(root, "AirTemperatureFunction"))
            {
                AirTempFunc["agregationMethod"] = "0";
                JsonUtilities.AddModel(AirTempFunc, typeof(ThreeHourSin), "InterpolationMethod");
            }
        }

        /// <summary>
        /// Change SimpleGrazing.FractionDefoliatedNToSoil from a scalar to an array.
        /// </summary>
        /// <param name="root">Root node.</param>
        /// <param name="fileName">Path to the .apsimx file.</param>
        private static void UpgradeToVersion100(JObject root, string fileName)
        {
            foreach (var simpleGrazing in JsonUtilities.ChildrenOfType(root, "SimpleGrazing"))
            {
                if (simpleGrazing["FractionDefoliatedNToSoil"] != null)
                {
                    var arr = new JArray();
                    arr.Add(simpleGrazing["FractionDefoliatedNToSoil"].Value<double>());
                    simpleGrazing["FractionDefoliatedNToSoil"] = arr;
                }
            }
        }

        /// <summary>
        /// Add canopy width Function.
        /// </summary>
        /// <param name="root">Root node.</param>
        /// <param name="fileName">Path to the .apsimx file.</param>
        private static void UpgradeToVersion101(JObject root, string fileName)
        {
            foreach (JObject Leaf in JsonUtilities.ChildrenOfType(root, "Leaf"))
            {
                JsonUtilities.AddConstantFunctionIfNotExists(Leaf, "WidthFunction", "0");

                VariableReference varRef = new VariableReference();
                varRef.Name = "DepthFunction";
                varRef.VariableName = "[Leaf].Height";

                JsonUtilities.AddModel(Leaf, varRef);
            }
        }

        /// <summary>
        /// Rename Models.Sensitivity.CroptimizR to Models.Optimisation.CroptimizR.
        /// </summary>
        /// <param name="root">Root node.</param>
        /// <param name="fileName">Path to the .apsimx file.</param>
        private static void UpgradeToVersion102(JObject root, string fileName)
        {
            foreach (JObject croptimizR in JsonUtilities.ChildrenRecursively(root, "CroptimizR"))
                croptimizR["$type"] = croptimizR["$type"].ToString().Replace("Sensitivity", "Optimisation");
        }

        /// <summary>
        /// Rename TemperatureResponse to Response on Interpolate functions.
        /// </summary>
        /// <param name="root">Root node.</param>
        /// <param name="fileName">Path to the .apsimx file.</param>
        private static void UpgradeToVersion103(JObject root, string fileName)
        {
            foreach (JObject atf in JsonUtilities.ChildrenRecursively(root, "AirTemperatureFunction"))
            {
                atf["$type"] = "Models.Functions.HourlyInterpolation, Models";
                foreach (JObject c in atf["Children"])
                {
                    if (c["Name"].ToString() == "TemperatureResponse")
                    {
                        c["Name"] = "Response";
                    }
                }
                foreach (JObject cultivar in JsonUtilities.ChildrenRecursively(root, "Cultivar"))
                {
                    if (!cultivar["Command"].HasValues)
                        continue;

                    foreach (JValue command in cultivar["Command"].Children())
                    {
                        command.Value = command.Value.ToString().Replace(".TemperatureResponse", ".Response");
                    }
                }
            }
        }


        /// <summary>
        /// Add expression function to replace direct call to structure in nodenumberphase
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion104(JObject root, string fileName)
        {
            foreach (JObject NNP in JsonUtilities.ChildrenRecursively(root, "NodeNumberPhase"))
            {
                VariableReference varRef = new VariableReference();
                varRef.Name = "LeafTipNumber";
                varRef.VariableName = "[Structure].LeafTipsAppeared";
                JsonUtilities.AddModel(NNP, varRef);
            }
        }


        /// <summary>
        /// Add expression function to replace direct call to structure in nodenumberphase
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion105(JObject root, string fileName)
        {
            foreach (JObject LAP in JsonUtilities.ChildrenRecursively(root, "LeafAppearancePhase"))
            {
                VariableReference varRef = new VariableReference();
                varRef.Name = "FinalLeafNumber";
                varRef.VariableName = "[Structure].FinalLeafNumber";
                JsonUtilities.AddModel(LAP, varRef);
            }
        }

        /// <summary>
        /// Change Nutrient.FOMC and Nutrient.FOMN to Nutrient.FOM.C and Nutrient.FOM.N
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion106(JObject root, string fileName)
        {
            Tuple<string, string>[] changes =
            {
                new Tuple<string, string>("utrient.FOMC",  "utrient.FOM.C"),
                new Tuple<string, string>("utrient.FOMN",  "utrient.FOM.N")
            };

            JsonUtilities.RenameVariables(root, changes);

            // Add Models.Soils.Nutrients namespace to all manager files that
            // reference Nutrient or Solute.

            foreach (var manager in JsonUtilities.ChildManagers(root))
            {
                var code = manager.ToString();
                if (code != null && (code.Contains("Nutrient") || code.Contains("Solute")))
                {
                    var usingLines = manager.Parser.GetUsingStatements().ToList();
                    usingLines.Add("Models.Soils.Nutrients");
                    manager.Parser.SetUsingStatements(usingLines);
                    manager.Save();
                }
            }
        }

        /// <summary>
        /// Add expression function to replace direct call to structure in LeafAppearancePhase
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion107(JObject root, string fileName)
        {
            foreach (JObject LAP in JsonUtilities.ChildrenRecursively(root, "LeafAppearancePhase"))
            {
                ExpressionFunction expFunction = new ExpressionFunction();
                expFunction.Name = "LeafNumber";
                expFunction.Expression = "[Leaf].ExpandedCohortNo + [Leaf].NextExpandingLeafProportion";
                JsonUtilities.AddModel(LAP, expFunction);

                VariableReference varRef1 = new VariableReference();
                varRef1.Name = "FullyExpandedLeafNo";
                varRef1.VariableName = "[Leaf].ExpandedCohortNo";
                JsonUtilities.AddModel(LAP, varRef1);

                VariableReference varRef2 = new VariableReference();
                varRef2.Name = "InitialisedLeafNumber";
                varRef2.VariableName = "[Leaf].InitialisedCohortNo";
                JsonUtilities.AddModel(LAP, varRef2);

            }
        }

        /// <summary>
        /// Upgrade to version 108.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="fileName"></param>
        private static void UpgradeToVersion108(JObject root, string fileName)
        {
            Tuple<string, string>[] changes =
            {
                new Tuple<string, string>("Wheat.Structure.HaunStage",          "Wheat.Phenology.HaunStage"),
                new Tuple<string, string>("[Wheat].Structure.HaunStage",          "[Wheat].Phenology.HaunStage"),
                new Tuple<string, string>("Wheat.Structure.PTQ",          "Wheat.Phenology.PTQ"),
                new Tuple<string, string>("[Wheat].Structure.PTQ",          "[Wheat].Phenology.PTQ")
            };
            JsonUtilities.RenameVariables(root, changes);
        }

        /// <summary>
        /// Create Models.Climate namespace.
        /// The following types will be moved into Models.Climate:
        /// - ControlledEnvironment
        /// - SlopeEffectsOnWeather
        /// - Weather
        /// - WeatherSampler
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion109(JObject root, string fileName)
        {
            Type[] typesToMove = new Type[] { typeof(ControlledEnvironment), typeof(SlopeEffectsOnWeather), typeof(Weather), typeof(WeatherSampler) };

            foreach (Type type in typesToMove)
                foreach (JObject instance in JsonUtilities.ChildrenRecursively(root, type.Name))
                    if (!instance["$type"].ToString().Contains("Models.Climate"))
                        instance["$type"] = instance["$type"].ToString().Replace("Models.", "Models.Climate.");

            foreach (ManagerConverter manager in JsonUtilities.ChildManagers(root))
            {
                string code = manager.ToString();
                if (code == null)
                    continue;
                foreach (Type type in typesToMove)
                {
                    if (code.Contains(type.Name))
                    {
                        List<string> usings = manager.Parser.GetUsingStatements().ToList();
                        usings.Add("Models.Climate");
                        manager.Parser.SetUsingStatements(usings);
                        manager.Save();
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Add canopy width Function.
        /// </summary>
        /// <param name="root">Root node.</param>
        /// <param name="fileName">Path to the .apsimx file.</param>
        private static void UpgradeToVersion110(JObject root, string fileName)
        {
            foreach (JObject Root in JsonUtilities.ChildrenOfType(root, "Root"))
                JsonUtilities.AddConstantFunctionIfNotExists(Root, "RootDepthStressFactor", "1");
        }

        /// <summary>
        /// Modify manager scripts to use the new generic model locator API.
        /// </summary>
        /// <param name="root">Root node.</param>
        /// <param name="fileName">Path to the .apsimx file.</param>
        private static void UpgradeToVersion111(JObject root, string fileName)
        {
            foreach (ManagerConverter manager in JsonUtilities.ChildManagers(root))
            {
                // Apsim.Get(model, path) -> model.FindByPath(path)?.Value
                FixApsimGet(manager);

                // Apsim.FullPath(model) -> model.FullPath
                FixFullPath(manager);

                // Apsim.GetVariableObject(model, path) -> model.FindByPath(path)
                FixGetVariableObject(manager);

                // Apsim.Ancestor<T>(model) -> model.FindAncestor<T>()
                FixAncestor(manager);

                // Apsim.Siblings(model) -> model.FindAllSiblings()
                FixSiblings(manager);

                // Apsim.ParentAllChildren(model) -> model.ParentAllDescendants()
                FixParentAllChildren(manager);

                // This one will fail if using a runtime type.
                // Apsim.Parent(model, type) -> model.FindAncestor<Type>()
                FixParent(manager);

                // Apsim.Set(model, path, value) -> model.FindByPath(path).Value = value
                FixSet(manager);

                // Apsim.Find(model, x) -> model.FindInScope(x)
                // Apsim.Find(model, typeof(Y)) -> model.FindInScope<Y>()
                // This one will fail if the second argument is a runtime type which doesn't
                // use the typeof() syntax. E.g. if it's a variable of type Type.
                // Will probably fail in other cases too. It's really not ideal.
                FixFind(manager);

                // Apsim.FindAll(model) -> model.FindAllInScope().ToList()
                // Apsim.FindAll(model, typeof(X)) -> model.FindAllInScope<X>().ToList()
                FixFindAll(manager);

                // Apsim.Child(model, "Wheat") -> model.FindChild("Wheat")
                // Apsim.Child(model, typeof(IOrgan)) -> model.FindChild<IOrgan>()
                FixChild(manager);

                // Apsim.Children(model, typeof(IFunction)) -> model.FindAllChildren<IFunction>().ToList<IModel>()
                // Apsim.Children(model, obj.GetType()) -> model.FindAllChildren().Where(c => obj.GetType().IsAssignableFrom(c.GetType())).ToList<IModel>()
                // This will add "using System.Linq;" if necessary.
                FixChildren(manager);

                // Apsim.ChildrenRecursively(model) -> model.FindAllDescendants().ToList()
                // Apsim.ChildrenRecursively(model, typeof(IOrgan)) -> model.FindAllDescendants<IOrgan>().OfType<IModel>().ToList()
                // Apsim.ChildrenRecursively(model, GetType()) -> model.FindAllDescendants().Where(d => GetType().IsAssignableFrom(d.GetType())).ToList()
                FixChildrenRecursively(manager);

                // Apsim.ChildrenRecursivelyVisible(model) -> model.FindAllDescendants().Where(m => !m.IsHidden).ToList()
                FixChildrenRecursivelyVisible(manager);

                manager.Save();
            }

            void FixApsimGet(ManagerConverter manager)
            {
                string pattern = @"Apsim\.Get\(([^,]+),\s*((?>\((?<c>)|[^()]+|\)(?<-c>))*(?(c)(?!)))\)";
                manager.Parser.ReplaceRegex(pattern, match =>
                {
                    string replace = @"$1.FindByPath($2).Value";
                    if (match.Groups[1].Value.Contains(" "))
                        replace = replace.Replace("$1", "($1)");

                    return Regex.Replace(match.Value, pattern, replace);
                });
            }

            void FixFullPath(ManagerConverter manager)
            {
                string pattern = @"Apsim\.FullPath\(((?>\((?<c>)|[^()]+|\)(?<-c>))*(?(c)(?!)))\)";
                string replacement = "$1.FullPath";
                manager.Parser.ReplaceRegex(pattern, match =>
                {
                    if (match.Groups[1].Value.Contains(" "))
                        replacement = replacement.Replace("$1", "($1)");

                    return Regex.Replace(match.Value, pattern, replacement);
                });
            }

            void FixGetVariableObject(ManagerConverter manager)
            {
                string pattern = @"Apsim\.GetVariableObject\(([^,]+),\s*((?>\((?<c>)|[^()]+|\)(?<-c>))*(?(c)(?!)))\)";
                manager.Parser.ReplaceRegex(pattern, match =>
                {
                    string replace = @"$1.FindByPath($2)";
                    if (match.Groups[1].Value.Contains(" "))
                        replace = replace.Replace("$1", "($1)");

                    return Regex.Replace(match.Value, pattern, replace);
                });
            }


            void FixAncestor(ManagerConverter manager)
            {
                string pattern = @"Apsim\.Ancestor<([^>]+)>\(((?>\((?<c>)|[^()]+|\)(?<-c>))*(?(c)(?!)))\)";
                manager.Parser.ReplaceRegex(pattern, match =>
                {
                    string replace = @"$2.FindAncestor<$1>($3)";
                    if (match.Groups[2].Value.Contains(" "))
                        replace = replace.Replace("$2", "($2)");

                    return Regex.Replace(match.Value, pattern, replace);
                });
            }

            void FixSiblings(ManagerConverter manager)
            {
                bool replaced = false;
                string pattern = @"Apsim\.Siblings\(((?>\((?<c>)|[^()]+|\)(?<-c>))*(?(c)(?!)))\)";
                manager.Parser.ReplaceRegex(pattern, match =>
                {
                    replaced = true;

                    string replace = @"$1.FindAllSiblings().ToList<IModel>()";
                    if (match.Groups[1].Value.Contains(" "))
                        replace = replace.Replace("$1", "($1)");

                    return Regex.Replace(match.Value, pattern, replace);
                });

                if (replaced)
                {
                    List<string> usings = manager.Parser.GetUsingStatements().ToList();
                    if (!usings.Contains("System.Linq"))
                        usings.Add("System.Linq");
                    manager.Parser.SetUsingStatements(usings);
                }
            }

            void FixParentAllChildren(ManagerConverter manager)
            {
                string pattern = @"Apsim\.ParentAllChildren\(((?>\((?<c>)|[^()]+|\)(?<-c>))*(?(c)(?!)))\)";
                manager.Parser.ReplaceRegex(pattern, match =>
                {
                    string replace = @"$1.ParentAllDescendants()";
                    if (match.Groups[1].Value.Contains(" "))
                        replace = replace.Replace("$1", "($1)");

                    return Regex.Replace(match.Value, pattern, replace);
                });
            }

            void FixParent(ManagerConverter manager)
            {
                string pattern = @"Apsim\.Parent\(((?>\((?<c>)|[^()]+|\)(?<-c>))*(?(c)(?!)))\)";
                manager.Parser.ReplaceRegex(pattern, match =>
                {
                    string argsRegex = @"(?:[^,()]+((?:\((?>[^()]+|\((?<c>)|\)(?<-c>))*\)))*)+";
                    var args = Regex.Matches(match.Groups[1].Value, argsRegex);

                    if (args.Count != 2)
                        throw new Exception($"Incorrect number of arguments passed to Apsim.Parent()");

                    string modelName = args[0].Value;
                    if (modelName.Contains(" "))
                        modelName = $"({modelName})";

                    string type = args[1].Value.Replace("typeof(", "").TrimEnd(')').Trim();

                    return $"{modelName}.FindAncestor<{type}>()";
                });
            }

            void FixSet(ManagerConverter manager)
            {
                string pattern = @"Apsim\.Set\(((?>\((?<c>)|[^()]+|\)(?<-c>))*(?(c)(?!)))\)";
                manager.Parser.ReplaceRegex(pattern, match =>
                {
                    string argsRegex = @"(?:[^,()]+((?:\((?>[^()]+|\((?<c>)|\)(?<-c>))*\)))*)+";
                    var args = Regex.Matches(match.Groups[1].Value, argsRegex);

                    if (args.Count != 3)
                        throw new Exception($"Incorrect number of arguments passed to Apsim.Set()");

                    string model = args[0].Value.Trim();
                    if (model.Contains(" "))
                        model = $"({model})";

                    string path = args[1].Value.Trim();
                    string value = args[2].Value.Trim();

                    return $"{model}.FindByPath({path}).Value = {value}";
                });
            }

            void FixFind(ManagerConverter manager)
            {
                string pattern = @"Apsim\.Find\(((?>\((?<c>)|[^()]+|\)(?<-c>))*(?(c)(?!)))\)";
                manager.Parser.ReplaceRegex(pattern, match =>
                {
                    string argsRegex = @"(?:[^,()]+((?:\((?>[^()]+|\((?<c>)|\)(?<-c>))*\)))*)+";
                    var args = Regex.Matches(match.Groups[1].Value, argsRegex);

                    if (args.Count != 2)
                        throw new Exception($"Incorrect number of arguments passed to Apsim.Find()");

                    string model = args[0].Value.Trim();
                    if (model.Contains(" "))
                        model = $"({model})";

                    string pathOrType = args[1].Value.Trim();
                    if (pathOrType.Contains("typeof("))
                    {
                        string type = pathOrType.Replace("typeof(", "").TrimEnd(')');
                        return $"{model}.FindInScope<{type}>()";
                    }
                    else
                        return $"{model}.FindInScope({pathOrType})";
                });
            }

            void FixFindAll(ManagerConverter manager)
            {
                string pattern = @"Apsim\.FindAll\(((?>\((?<c>)|[^()]+|\)(?<-c>))*(?(c)(?!)))\)";
                bool replaced = manager.Parser.ReplaceRegex(pattern, match =>
                {
                    string argsRegex = @"(?:[^,()]+((?:\((?>[^()]+|\((?<c>)|\)(?<-c>))*\)))*)+";
                    var args = Regex.Matches(match.Groups[1].Value, argsRegex);

                    if (args.Count == 1)
                    {
                        string model = args[0].Value.Trim();
                        if (model.Contains(" "))
                            model = $"({model})";

                        return $"{model}.FindAllInScope().ToList()";
                    }
                    else if (args.Count == 2)
                    {
                        string model = args[0].Value.Trim();
                        if (model.Contains(" "))
                            model = $"({model})";

                        string type = args[1].Value.Trim().Replace("typeof(", "").TrimEnd(')');

                        // See comment in the FixChildren() method. This really isn't ideal.
                        return $"{model}.FindAllInScope<{type}>().OfType<IModel>().ToList()";
                    }
                    else
                        throw new Exception($"Incorrect number of arguments passed to Apsim.FindAll()");
                });

                if (replaced)
                    AddLinqIfNotExist(manager);
            }

            void FixChild(ManagerConverter manager)
            {
                string pattern = @"Apsim\.Child\(((?>\((?<c>)|[^()]+|\)(?<-c>))*(?(c)(?!)))\)";
                manager.Parser.ReplaceRegex(pattern, match =>
                {
                    string argsRegex = @"(?:[^,()]+((?:\((?>[^()]+|\((?<c>)|\)(?<-c>))*\)))*)+";
                    var args = Regex.Matches(match.Groups[1].Value, argsRegex);

                    if (args.Count != 2)
                        throw new Exception($"Incorrect number of arguments passed to Apsim.Find()");

                    string model = args[0].Value.Trim();
                    if (model.Contains(" "))
                        model = $"({model})";

                    string pathOrType = args[1].Value.Trim();
                    if (pathOrType.Contains("typeof("))
                    {
                        string type = pathOrType.Replace("typeof(", "").TrimEnd(')');
                        return $"{model}.FindChild<{type}>()";
                    }
                    else
                        return $"{model}.FindChild({pathOrType})";
                });

                pattern = @"(FindChild<([^>]+)>\(\)) as \2";
                manager.Parser.ReplaceRegex(pattern, "$1");
            }

            void FixChildren(ManagerConverter manager)
            {

                string pattern = @"Apsim\.Children\(((?>\((?<c>)|[^()]+|\)(?<-c>))*(?(c)(?!)))\)";
                bool replaced = manager.Parser.ReplaceRegex(pattern, match =>
                {
                    string argsRegex = @"(?:[^,()]+((?:\((?>[^()]+|\((?<c>)|\)(?<-c>))*\)))*)+";
                    var args = Regex.Matches(match.Groups[1].Value, argsRegex);

                    if (args.Count != 2)
                        throw new Exception($"Incorrect number of arguments passed to Apsim.Children()");

                    string model = args[0].Value.Trim();
                    if (model.Contains(" "))
                        model = $"({model})";

                    string type = args[1].Value.Trim();

                    Match simplify = Regex.Match(type, @"typeof\(([^\)]+)\)");
                    if (simplify.Groups.Count == 2)
                    {
                        type = simplify.Groups[1].Value;

                        // Unfortunately we need some sort of cast here, in case the type
                        // being searched for is an interface such as IPlant which doesn't
                        // implement IModel, even though realistically the only results
                        // which FindAllChildren() will return are guaranteed to be IModels.
                        // In an ideal world, the user wouldn't access any members of IModel,
                        // and we could just happily return an IEnumerable<IPlant> or
                        // List<IPlant>, but that's obviously not a guarantee we can make.
                        return $"{model}.FindAllChildren<{type}>().OfType<IModel>().ToList()";
                    }
                    else
                    {
                        // Need to ensure that we're using System.Linq;
                        return $"{model}.FindAllChildren().Where(c => {type}.IsAssignableFrom(c.GetType())).ToList()";
                    }
                });

                if (replaced)
                    AddLinqIfNotExist(manager);
            }

            void FixChildrenRecursively(ManagerConverter manager)
            {
                string pattern = @"Apsim\.ChildrenRecursively\(((?>\((?<c>)|[^()]+|\)(?<-c>))*(?(c)(?!)))\)";
                bool replaced = manager.Parser.ReplaceRegex(pattern, match =>
                {
                    string argsRegex = @"(?:[^,()]+((?:\((?>[^()]+|\((?<c>)|\)(?<-c>))*\)))*)+";
                    var args = Regex.Matches(match.Groups[1].Value, argsRegex);

                    if (args.Count == 1)
                    {

                        string model = args[0].Value.Trim();
                        if (model.Contains(" "))
                            model = $"({model})";

                        return $"{model}.FindAllDescendants().ToList()";
                    }
                    else if (args.Count == 2)
                    {
                        string model = args[0].Value.Trim();
                        if (model.Contains(" "))
                            model = $"({model})";

                        string type = args[1].Value.Trim();

                        Match simplify = Regex.Match(type, @"typeof\(([^\)]+)\)");
                        if (simplify.Groups.Count == 2)
                        {
                            // Code uses a simple typeof(X) to reference the type. This can be converted to the generic usage.
                            type = simplify.Groups[1].Value;
                            return $"{model}.FindAllDescendants<{type}>().OfType<IModel>().ToList()";
                        }
                        else
                            // This is a bit uglier and we need to use a qnd linq query to fix it up.
                            return $"{model}.FindAllDescendants().Where(d => {type}.IsAssignableFrom(d.GetType())).ToList()";
                    }
                    else
                        throw new Exception($"Incorrect number of arguments passed to Apsim.ChildrenRecursively()");
                });

                if (replaced)
                    AddLinqIfNotExist(manager);
            }

            void AddLinqIfNotExist(ManagerConverter manager)
            {
                List<string> usings = manager.Parser.GetUsingStatements().ToList();
                if (!usings.Contains("System.Linq"))
                    usings.Add("System.Linq");
                manager.Parser.SetUsingStatements(usings);
            }

            void FixChildrenRecursivelyVisible(ManagerConverter manager)
            {
                string pattern = @"Apsim\.ChildrenRecursivelyVisible\(((?>\((?<c>)|[^()]+|\)(?<-c>))*(?(c)(?!)))\)";
                bool replaced = manager.Parser.ReplaceRegex(pattern, match =>
                {
                    string argsRegex = @"(?:[^,()]+((?:\((?>[^()]+|\((?<c>)|\)(?<-c>))*\)))*)+";
                    var args = Regex.Matches(match.Groups[1].Value, argsRegex);

                    if (args.Count == 1)
                    {
                        string model = args[0].Value.Trim();
                        if (model.Contains(" "))
                            model = $"({model})";

                        return $"{model}.FindAllDescendants().Where(m => !m.IsHidden).ToList()";
                    }
                    else
                        throw new Exception($"Incorrect number of arguments passed to Apsim.ChildrenRecursivelyVisible()");
                });

                if (replaced)
                    AddLinqIfNotExist(manager);
            }
        }

        /// <summary>
        /// Upgrade to version 112. Lots of breaking changes to class Plant.
        /// </summary>
        /// <param name="root">The root json token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion112(JObject root, string fileName)
        {
            var changes = new Tuple<string, string>[]
            {
                new Tuple<string, string>("Plant.Canopy", "Plant.Leaf"),
                new Tuple<string, string>("[Plant].Canopy", "[Plant].Leaf"),
                new Tuple<string, string>("plant.Canopy", "plant.Leaf"),
                new Tuple<string, string>("[plant].Canopy", "[plant].Leaf"),
                new Tuple<string, string>("Wheat.Canopy", "Wheat.Leaf"),
                new Tuple<string, string>("[Wheat].Canopy", "[Wheat].Leaf"),
                new Tuple<string, string>("wheat.Canopy", "wheat.Leaf"),
                new Tuple<string, string>("[wheat].Canopy", "[wheat].Leaf"),
                new Tuple<string, string>("[Phenology].CurrentPhaseName", "[Phenology].CurrentPhase.Name"),
                new Tuple<string, string>("[phenology].CurrentPhaseName", "[phenology].CurrentPhase.Name"),
                new Tuple<string, string>("Phenology.CurrentPhaseName", "Phenology.CurrentPhase.Name"),
                new Tuple<string, string>("phenology.CurrentPhaseName", "phenology.CurrentPhase.Name"),
                new Tuple<string, string>("[Plant].Phenology.DaysAfterSowing", "[Plant].DaysAfterSowing"),
                new Tuple<string, string>("Plant.Phenology.DaysAfterSowing", "Plant.DaysAfterSowing"),
                new Tuple<string, string>("Phenology.DaysAfterSowing", "DaysAfterSowing"),
                new Tuple<string, string>("[Phenology].DaysAfterSowing", "[Plant].DaysAfterSowing"),
            };
            JsonUtilities.RenameVariables(root, changes);

            // Some more complicated changes to manager code.
            foreach (ManagerConverter manager in JsonUtilities.ChildManagers(root))
            {
                ConvertPlantPropertyToDirectLink(manager, "Root", "Models.PMF.Organs");
                ConvertPlantPropertyToDirectLink(manager, "Structure", "Models.PMF.Struct");
                ConvertPlantPropertyToDirectLink(manager, "Phenology", "Models.PMF.Phen");
            }

            void ConvertPlantPropertyToDirectLink(ManagerConverter manager, string property, string nameSpace)
            {
                string code = manager.ToString();
                if (code == null)
                    return;
                if (code.Contains($".{property}."))
                {
                    string plantName = Regex.Match(code, $@"(\w+)\.{property}\.").Groups[1].Value;
                    JObject zone = JsonUtilities.Ancestor(manager.Token, typeof(Zone));
                    if (zone == null)
                    {
                        JObject replacements = JsonUtilities.Ancestor(manager.Token, typeof(Replacements));
                        if (replacements != null)
                        {
                            JObject replacement = JsonUtilities.ChildrenRecursively(root).Where(j => j != manager.Token && j["Name"].ToString() == manager.Token["Name"].ToString()).FirstOrDefault();
                            if (replacement != null)
                                zone = JsonUtilities.Ancestor(replacement, typeof(Zone));
                            else
                                // This manager script is under replacements, but is not replacing any models.
                                // It is also likely to contain compilation errors due to API changes. Therefore
                                // we will disable it to suppress these errors.
                                manager.Token["Enabled"] = false;
                        }
                    }

                    int numPlantsInZone = JsonUtilities.ChildrenRecursively(zone, "Plant").Count;
                    if (numPlantsInZone > 0)
                    {
                        manager.Parser.AddUsingStatement(nameSpace);

                        bool isOptional = false;
                        var plantLink = manager.Parser.GetDeclarations().Find(d => d.InstanceName == plantName);
                        if (plantLink != null)
                        {
                            string linkAttribute = plantLink.Attributes.Find(a => a.Contains("[Link"));
                            if (linkAttribute != null && linkAttribute.Contains("IsOptional = true"))
                                isOptional = true;
                        }

                        string link;
                        int numPlantsWithCorrectName = JsonUtilities.ChildrenRecursively(zone, "Plant").Count(p => p["Name"].ToString() == plantName);
                        if (string.IsNullOrEmpty(plantName) || numPlantsWithCorrectName == 0)
                            link = $"[Link{(isOptional ? "(IsOptional = true)" : "")}]";
                        else
                            link = $"[Link(Type = LinkType.Path, Path = \"[{plantName}].{property}\"{(isOptional ? ", IsOptional = true" : "")})]";

                        string memberName = property[0].ToString().ToLower() + property.Substring(1);
                        manager.Parser.AddDeclaration(property, memberName, new string[1] { link });

                        if (!string.IsNullOrEmpty(plantName))
                            manager.Parser.ReplaceRegex($"([^\"]){plantName}\\.{property}", $"$1{memberName}");
                        manager.Save();
                    }
                }
            }
        }

        /// <summary>
        /// Upgrade to version 113. Rename SowPlant2Type to SowingParameters.
        /// </summary>
        /// <param name="root">The root json token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion113(JObject root, string fileName)
        {
            foreach (ManagerConverter manager in JsonUtilities.ChildManagers(root))
                if (manager.Parser.Replace("SowPlant2Type", "SowingParameters"))
                    manager.Save();
        }

        /// <summary>
        /// Upgrade to version 114. Remove references to Plant.IsC4.
        /// </summary>
        /// <param name="root">The root json token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion114(JObject root, string fileName)
        {
            foreach (ManagerConverter manager in JsonUtilities.ChildManagers(root))
                if (manager.Parser.ReplaceRegex(@"(\w+)\.IsC4", "$1.FindByPath(\"Leaf.Photosynthesis.FCO2.PhotosyntheticPathway\")?.Value?.ToString() == \"C4\""))
                    manager.Save();
        }

        /// <summary>
        /// Upgrade to version 115. Add mortality rate constant of 0 to any plants
        /// which do not already have a mortality rate.
        /// </summary>
        /// <param name="root">The root json token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        /// <remarks>
        /// This is part of the change to make mortality rate non-optional.
        /// </remarks>
        private static void UpgradeToVersion115(JObject root, string fileName)
        {
            foreach (JObject plant in JsonUtilities.ChildrenRecursively(root, nameof(Plant)))
            {
                if ((plant["ResourceName"] == null || JsonUtilities.Ancestor(plant, typeof(Replacements)) != null) && JsonUtilities.ChildWithName(plant, "MortalityRate", ignoreCase: true) == null)
                {
                    Constant mortalityRate = new Constant();
                    mortalityRate.Name = "MortalityRate";
                    mortalityRate.FixedValue = 0;
                    JsonUtilities.AddModel(plant, mortalityRate);
                }
            }
        }

        /// <summary>
        /// Upgrade to version 115. Add PlantType to IPlants.
        /// </summary>
        /// <param name="root">The root json token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion116(JObject root, string fileName)
        {
            foreach (JObject pasture in JsonUtilities.ChildrenRecursively(root, "PastureSpecies"))
            {
                string plantType = pasture["ResourceName"]?.ToString();//?.Substring("AGP".Length);
                if (string.IsNullOrEmpty(plantType))
                    plantType = pasture["Name"]?.ToString();
                pasture["PlantType"] = plantType;
            }
            
            foreach (JObject plant in JsonUtilities.ChildrenRecursively(root, "Plant"))
                plant["PlantType"] = plant["CropType"]?.ToString();

            JsonUtilities.RenameVariables(root, new Tuple<string, string>[] { new Tuple<string, string>("CropType", "PlantType")});
        }

        /// <summary>
        /// Upgrade to version 115. Add PlantType to IPlants.
        /// </summary>
        /// <param name="root">The root json token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion117(JObject root, string fileName)
        {
            foreach (JObject croptimizr in JsonUtilities.ChildrenRecursively(root, "CroptimizR"))
            {
                string variableName = croptimizr["VariableName"]?.ToString();
                JArray variableNames = new JArray(new object[1] { variableName });
                croptimizr.Remove("VariableName");
                croptimizr["VariableNames"] = variableNames;
            }
        }


        /// <summary>
        /// Renames Initial NO3N and NH4N to NO3 and NH4
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion118(JObject root, string fileName)
        {
            foreach (var sample in JsonUtilities.ChildrenRecursively(root, "Sample"))
            {
                JsonUtilities.RenameChildModel(sample, "NO3N", "NO3");
                JsonUtilities.RenameChildModel(sample, "NH4N", "NH4");
            }

            foreach (var chloride in JsonUtilities.ChildrenRecursively(root, "Chloride"))
            {
                chloride["$type"] = "Models.Soils.Nutrients.Solute, Models";
                chloride["Name"] = "CL";
            }
        }

        /// <summary>
        /// Change report and manager scripts for soil variables that have been out of soil class. 
        /// </summary>
        /// <param name="root">The root json token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion119(JObject root, string fileName)
        {
            var changes = new Tuple<string, string>[]
            {
                new Tuple<string, string>("[ISoil]", "[Soil]"),
                new Tuple<string, string>("[Soil].Temperature", "[Soil].Temperature.Value"),
                new Tuple<string, string>("Soil.Temperature", "Soil.Temperature.Value"),
                new Tuple<string, string>("[Soil].FBiom", "[Soil].Organic.FBiom"),
                new Tuple<string, string>("[Soil].FInert", "[Soil].Organic.FInert"),
                new Tuple<string, string>("[Soil].InitialRootWt", "[Soil].Organic.FOM"),
                new Tuple<string, string>("[Soil].DepthMidPoints", "[Soil].Physical.DepthMidPoints"),
                new Tuple<string, string>("[Soil].Thickness", "[Soil].Physical.Thickness"),
                new Tuple<string, string>("[Soil].BD", "[Soil].Physical.BD"),
                new Tuple<string, string>("[Soil].AirDry", "[Soil].Physical.AirDry"),
                new Tuple<string, string>("[Soil].LL15", "[Soil].Physical.LL15"),   // will also convert LL15mm
                new Tuple<string, string>("[Soil].DUL", "[Soil].Physical.DUL"),     // will also convert DULmm
                new Tuple<string, string>("[Soil].SAT", "[Soil].Physical.SAT"),     // will also convert SATmm
                new Tuple<string, string>("[Soil].PAWC", "[Soil].Physical.PAWC"),   // will also convert PAWCmm
                new Tuple<string, string>("[Soil].PAW", "[Soil].SoilWater.PAW"),    // will also convert PAWmm
                new Tuple<string, string>("[Soil].Water", "[Soil].SoilWater.SWmm"),
                new Tuple<string, string>("[Soil].KS", "[Soil].Physical.KS"),
            };
            JsonUtilities.RenameVariables(root, changes);

            // Look in manager scripts and move some soil properties to the soil physical instance.
            var variablesToMove = new ManagerReplacement[]
            {
                new ManagerReplacement("Soil.ThicknessCumulative", "soilPhysical.ThicknessCumulative", "IPhysical"),
                new ManagerReplacement("Soil.Thickness", "soilPhysical.Thickness", "IPhysical"),
                new ManagerReplacement("Soil.BD", "soilPhysical.BD", "IPhysical"),
                new ManagerReplacement("Soil.AirDry", "soilPhysical.AirDry", "IPhysical"),
                new ManagerReplacement("Soil.LL15", "soilPhysical.LL15", "IPhysical"),
                new ManagerReplacement("Soil.LL15mm", "soilPhysical.LL15mm", "IPhysical"),
                new ManagerReplacement("Soil.DUL", "soilPhysical.DUL", "IPhysical"),
                new ManagerReplacement("Soil.DULmm", "soilPhysical.DULmm", "IPhysical"),
                new ManagerReplacement("Soil.SAT", "soilPhysical.SAT", "IPhysical"),
                new ManagerReplacement("Soil.KS", "soilPhysical.KS", "IPhysical"),
                new ManagerReplacement("Soil.PAWC", "soilPhysical.PAWC", "IPhysical"),
                new ManagerReplacement("Soil.PAWCmm", "soilPhysical.PAWCmm", "IPhysical"),
                new ManagerReplacement("Soil.SoilWater", "waterBalance", "ISoilWater"),
                new ManagerReplacement("Soil.Water", "waterBalance.SWmm", "ISoilWater"),
            };
            foreach (var manager in JsonUtilities.ChildManagers(root))
            {
                bool changesMade = manager.MoveVariables(variablesToMove);

                if (changesMade)
                {
                    manager.Parser.AddUsingStatement("Models.Interfaces");
                    manager.Save();
                }
            }

            // Rename the CERESSoilTemperature model to SoilTemperature
            foreach (var soil in JsonUtilities.ChildrenRecursively(root, "Soil"))
                JsonUtilities.RenameChildModel(soil, "CERESSoilTemperature", "Temperature");
        }

        /// <summary>
        /// Remove empty samples from soils.
        /// </summary>
        /// <param name="root">The root json token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion120(JObject root, string fileName)
        {
            foreach (JObject sample in JsonUtilities.ChildrenRecursively(root, "Sample"))
            {
                if ( (sample["NO3N"] == null || !sample["NO3N"].HasValues)
                 &&  (sample["NH4N"] == null || !sample["NH4N"].HasValues)
                 &&  (sample["SW"]   == null || !sample["SW"].HasValues)
                 &&  (sample["OC"]   == null || !sample["OC"].HasValues)
                 &&  (sample["EC"]   == null || !sample["EC"].HasValues)
                 &&  (sample["CL"]   == null || !sample["CL"].HasValues)
                 &&  (sample["ESP"]  == null || !sample["ESP"].HasValues)
                 &&  (sample["PH"]   == null || !sample["PH"].HasValues) )
                {
                    // The sample is empty. If it is not being overridden by a factor
                    // or replacements, get rid of it.
                    JObject expt = JsonUtilities.Ancestor(sample, typeof(Experiment));
                    if (expt != null)
                    {
                        // The sample is in an experiment. If it's being overriden by a factor,
                        // ignore it.
                        JObject factors = JsonUtilities.ChildWithName(expt, "Factors");
                        if (factors != null && JsonUtilities.DescendantOfType(factors, typeof(Sample)) != null)
                            continue;
                    }

                    JObject replacements = JsonUtilities.DescendantOfType(root, typeof(Replacements));
                    if (replacements != null && JsonUtilities.DescendantOfType(replacements, typeof(Sample)) != null)
                        continue;

                    JObject parent = JsonUtilities.Parent(sample) as JObject;
                    string name = sample["Name"]?.ToString();
                    if (parent != null && !string.IsNullOrEmpty(name))
                        JsonUtilities.RemoveChild(parent, name);
                }
            }
        }

        /// <summary>
        /// Replace all instances of PhaseBasedSwitch with PhaseLookupValues.
        /// </summary>
        /// <param name="root">The root json token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion121(JObject root, string fileName)
        {
            foreach (JObject phaseSwitch in JsonUtilities.ChildrenRecursively(root, "PhaseBasedSwitch"))
            {
                phaseSwitch["$type"] = "Models.Functions.PhaseLookupValue, Models";
                Constant value = new Constant();
                value.FixedValue = 1;
                JsonUtilities.AddModel(phaseSwitch, value);
            }
        }

        /// <summary>
        /// Set maps' default zoom level to 360.
        /// </summary>
        /// <param name="root">The root json token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion122(JObject root, string fileName)
        {
            foreach (JObject map in JsonUtilities.ChildrenRecursively(root, nameof(Map)))
            {
                map["Zoom"] = 360;
                map["Center"]["Latitude"] = 0;
                map["Center"]["Longitude"] = 0;
            }
        }

        /// <summary>
        /// Remove all references to Arbitrator.WDemand, Arbitrator.WSupply, and Arbitrator.WAllocated.
        /// </summary>
        /// <param name="root">The root json token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion123(JObject root, string fileName)
        {
            string[] patterns = new[]
            {
                "Arbitrator.WSupply",
                "[Arbitrator].WSupply",
                "Arbitrator.WDemand",
                "[Arbitrator].WDemand",
                "Arbitrator.WAllocated",
                "[Arbitrator].WAllocated",
            };
            foreach (JObject report in JsonUtilities.ChildrenRecursively(root, typeof(Report).Name))
            {
                if (report["VariableNames"] is JArray variables)
                {
                    for (int i = variables.Count - 1; i >= 0; i--)
                        if (patterns.Any(p => variables[i].ToString().Contains(p)))
                            variables.RemoveAt(i);
                    report["VariableNames"] = variables;
                }
            }
        }

        /// <summary>
        /// Rename RadIntTot to RadiationIntercepted.
        /// </summary>
        /// <param name="root">The root json token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion124(JObject root, string fileName)
        {
            Tuple<string, string>[] changes = new Tuple<string, string>[1]
            {
                new Tuple<string, string>("RadIntTot", "RadiationIntercepted")
            };
            JsonUtilities.RenameVariables(root, changes);
            foreach (ManagerConverter manager in JsonUtilities.ChildManagers(root))
            {
                bool changed = false;
                foreach (Tuple<string, string> change in changes)
                    changed |= manager.Parser.Replace(change.Item1, change.Item2, true);
                if (changed)
                    manager.Save();
            }
        }

        /// <summary>
        /// Add a default value for Sobol's variable to aggregate.
        /// This was previously assumed to be Clock.Today.Year but
        /// has been extracted to a variable.
        /// </summary>
        /// <param name="root">The root json token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion125(JObject root, string fileName)
        {
            foreach (JObject sobol in JsonUtilities.ChildrenRecursively(root, "Sobol"))
            {
                sobol["TableName"] = "Report";
                sobol["AggregationVariableName"] = "Clock.Today.Year";
            }
        }

        /// <summary>
        /// Add progeny destination phase and mortality function.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="fileName"></param>
        private static void UpgradeToVersion900(JObject root, string fileName)
        {
            foreach (JObject LC in JsonUtilities.ChildrenRecursively(root, "LifeCycle"))
            {
                foreach (JObject LP in JsonUtilities.ChildrenRecursively(LC, "LifeCyclePhase"))
                {
                    JsonUtilities.AddConstantFunctionIfNotExists(LP, "Migration", "0.0");
                    JObject ProgDest = JsonUtilities.CreateNewChildModel(LP, "ProgenyDestination", "Models.LifeCycle.ProgenyDestinationPhase");
                    ProgDest["NameOfLifeCycleForProgeny"] = LC["Name"].ToString();
                    ProgDest["NameOfPhaseForProgeny"] = LP["NameOfPhaseForProgeny"].ToString();
                }
            }
        }

        /// <summary>
        /// Move physical properties off Weirdo class and use Physical class instead.
        /// </summary>
        /// <param name="root">The root json token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion126(JObject root, string fileName)
        {

            foreach (var soil in JsonUtilities.ChildrenRecursively(root, "Soil"))
            {
                var weirdo = JsonUtilities.Children(soil).Find(child => JsonUtilities.Type(child) == "WEIRDO");
                if (weirdo != null)
                {
                    Physical physical = new Physical();
                    physical.Name = "Physical";
                    if (weirdo["BD"].ToArray().Length > 0)
                        physical.BD = weirdo["BD"].Values<double>().ToArray();
                    if (weirdo["DUL"].ToArray().Length > 0)
                        physical.DUL = weirdo["DUL"].Values<double>().ToArray();
                    if (weirdo["LL15"].ToArray().Length > 0)
                        physical.LL15 = weirdo["LL15"].Values<double>().ToArray();
                    if (weirdo["SAT"].ToArray().Length > 0)
                        physical.SAT = weirdo["SAT"].Values<double>().ToArray();
                    if (weirdo["Thickness"].ToArray().Length > 0)
                        physical.Thickness = weirdo["Thickness"].Values<double>().ToArray();
                    JsonUtilities.AddModel(soil, physical);
                }
            }
        }

        /// <summary>
        /// Refactor LifeCycle model
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion890(JObject root, string fileName)
        {
            //Method to convert LifeCycleProcesses to functions
            void ChangeToFunction(JObject LifePhase, string OldType, string NewName, string FunctType, string SubFunctType = "")
            {
                JArray children = LifePhase["Children"] as JArray;
                var Process = JsonUtilities.ChildrenOfType(LifePhase, OldType);
                if (Process.Count >= 1)
                {
                    bool FirstProc = true;
                    JObject funct = new JObject();
                    JArray ChildFunctions = new JArray();
                    foreach (var proc in Process)
                    {
                        if (FunctType == "Add")
                        {
                            if (FirstProc)
                            {
                                funct["$type"] = "Models.Functions.AddFunction, Models";
                                funct["Name"] = NewName;
                                FirstProc = false;
                            }
                            foreach (var c in proc["Children"])
                            {
                                if ((SubFunctType == "All") && (c["$type"].ToString() != "Models.Memo, Models"))
                                {
                                    JObject cld = new JObject();
                                    cld["$type"] = "Models.Functions.MultiplyFunction, Models";
                                    cld["Name"] = c["Name"].ToString();
                                    JObject Popn = new JObject();
                                    Popn["$type"] = "Models.Functions.VariableReference, Models";
                                    Popn["Name"] = "CohortPopulation";
                                    Popn["VariableName"] = "[" + LifePhase["Name"].ToString() + "].CurrentCohort.Population";
                                    JArray kids = new JArray();
                                    kids.Add(Popn);
                                    kids.Add(c);
                                    cld["Children"] = kids;
                                    ChildFunctions.Add(cld);
                                }
                                else
                                    ChildFunctions.Add(c);
                            }
                        }
                        else if (FunctType == "Multiply")
                        {
                            if (FirstProc)
                            {
                                funct["$type"] = "Models.Functions.MultiplyFunction, Models";
                                funct["Name"] = NewName;
                                JObject Popn = new JObject();
                                Popn["$type"] = "Models.Functions.VariableReference, Models";
                                Popn["Name"] = "CohortPopulation";
                                Popn["VariableName"] = "[" + LifePhase["Name"].ToString() + "].CurrentCohort.Population";
                                ChildFunctions.Add(Popn);
                                FirstProc = false;
                            }
                            if (SubFunctType == "All")
                            {
                                foreach (JObject kid in proc["Children"])
                                    ChildFunctions.Add(kid);
                            }
                            else if (SubFunctType == "Min")
                            {
                                JObject Min = new JObject();
                                Min["$type"] = "Models.Functions.MinimumFunction, Models";
                                Min["Name"] = "ProgenyRate";
                                Min["Children"] = proc["Children"];
                                ChildFunctions.Add(Min);
                            }
                        }
                        else
                            throw new Exception("Something got Funct up");

                        JsonUtilities.RemoveChild(LifePhase, proc["Name"].ToString());
                    }
                    funct["Children"] = ChildFunctions;
                    children.Add(funct);
                }
                else
                    JsonUtilities.AddConstantFunctionIfNotExists(LifePhase, NewName, "0.0");
            }

            // Method to add infestation object
            void AddInfestObject(string Name, JToken zone, string Org, string Phase, JArray ChildFunction, int typeIndex)
            {
                JObject Infest = JsonUtilities.CreateNewChildModel(zone, Name, "Models.LifeCycle.Infestation");
                Infest["TypeOfInfestation"] = typeIndex;
                Infest["InfestingOrganisumName"] = Org;
                Infest["InfestingPhaseName"] = Phase;
                Infest["ChronoAgeOfImmigrants"] = 0;
                Infest["PhysAgeOfImmigrants"] = 0.2;
                Infest["Children"] = ChildFunction;
            }

            foreach (JObject LC in JsonUtilities.ChildrenRecursively(root, "LifeCycle"))
            {
                List<string> ChildPhases = new List<string>();
                JToken zone = JsonUtilities.Parent(LC);

                foreach (JObject LP in JsonUtilities.ChildrenRecursively(LC, "LifeStage"))
                {
                    LP["$type"] = "Models.LifeCycle.LifeCyclePhase, Models";
                    var ReproductiveProcess = JsonUtilities.ChildrenOfType(LP, "LifeStageReproduction");
                    //Convert LifeCycleProcesses to functions
                    ChangeToFunction(LP, "LifeStageImmigration", "Immigration", "Add");
                    ChangeToFunction(LP, "LifeStageGrowth", "Development", "Add");
                    ChangeToFunction(LP, "LifeStageMortality", "Mortality", "Add", "All");
                    ChangeToFunction(LP, "LifeStageReproduction", "Reproduction", "Multiply", "Min");
                    ChangeToFunction(LP, "LifeStageTransfer", "Graduation", "Multiply", "All");

                    //If functionality present in Graduation, move to development and delete graduation
                    var Dev = JsonUtilities.ChildWithName(LP, "Development");
                    var Grad = JsonUtilities.ChildWithName(LP, "Graduation");
                    if (Dev["$type"].ToString() == "Models.Functions.Constant, Models") //There was no functionality in Development
                    {
                        JsonUtilities.RemoveChild(LP, "Development");
                        Grad["Name"] = "Development"; //Set Graduation function to be Development
                        JsonUtilities.RemoveChild(Grad, "CohortPopulation");
                    }
                    else
                        JsonUtilities.RemoveChild(LP, "Graduation");

                    //Fix variable references
                    foreach (JObject xv in JsonUtilities.ChildrenOfType(LP, "VariableReference"))
                    {
                        if (xv["VariableName"].ToString() == "[LifeCycle].CurrentLifeStage.CurrentCohort.PhenoAge")
                            xv["VariableName"] = "[" + LP["Name"] + "].CurrentCohort.ChronologicalAge";
                    }
                    foreach (JObject xv in JsonUtilities.ChildrenOfType(LP, "LinearAfterThresholdFunction"))
                    {
                        if (xv["XProperty"].ToString() == "[LifeCycle].CurrentLifeStage.CurrentCohort.PhenoAge")
                            xv["XProperty"] = "[" + LP["Name"] + "].CurrentCohort.ChronologicalAge";
                    }

                    if (ReproductiveProcess.Count >= 1)
                    {
                        LP["NameOfPhaseForProgeny"] = ReproductiveProcess[0]["TransferTo"].ToString();
                    }
                    ChildPhases.Add(LP["Name"].ToString());
                }

                //Move immigration function from life phase to infestation event
                foreach (JObject LP in JsonUtilities.ChildrenRecursively(LC, "LifeCyclePhase"))
                {
                    JObject NumberFunction = JsonUtilities.ChildWithName(LP, "Immigration");
                    NumberFunction["Name"] = "NumberOfImmigrants";
                    JArray ChildFunction = new JArray();
                    ChildFunction.Add(NumberFunction);
                    if ((NumberFunction["$type"].ToString() == "Models.Functions.Constant, Models") &&
                        (double.Parse(NumberFunction["FixedValue"].ToString()) == 0.0))
                    {
                        //Don't add immigration event, not needed
                    }
                    else
                    {
                        AddInfestObject("Ongoing Infestation " + LP["Name"], zone, LC["Name"].ToString(), LP["Name"].ToString(), ChildFunction, 3);
                    }
                    JsonUtilities.RemoveChild(LP, "NumberOfImmigrants");
                }

                // Add infestation phase for initial populations
                int cou = 0;
                foreach (double init in LC["InitialPopulation"])
                {
                    if (init > 0)
                    {
                        string Name = "Initial Infestation " + ChildPhases[cou];
                        JObject NumberFunction = new JObject();
                        NumberFunction["$type"] = "Models.Functions.Constant, Models";
                        NumberFunction["Name"] = "NumberOfImmigrants";
                        NumberFunction["FixedValue"] = init;
                        JArray ChildFunction = new JArray();
                        ChildFunction.Add(NumberFunction);
                        AddInfestObject(Name, zone, LC["Name"].ToString(), ChildPhases[cou], ChildFunction, 0);
                        cou += 1;
                    }
                }
            }
            foreach (var report in JsonUtilities.ChildrenOfType(root, "Report"))
            {
                JsonUtilities.SearchReplaceReportVariableNames(report, "Migrants", "Graduates");
                JsonUtilities.SearchReplaceReportVariableNames(report, "Mortality", "Mortalities");
                JsonUtilities.SearchReplaceReportVariableNames(report, "PhenologicalAge", "PhysiologicalAge");
            }

            foreach (var manager in JsonUtilities.ChildManagers(root))
            {
                manager.Parser.Replace("LifeCycle.LifeStage", "LifeCycle.LifeCyclePhase");
                manager.Save();
            }
        }

        /// <summary>
        /// Changes initial Root Wt to an array.
        /// </summary>
        /// <param name="root">The root JSON token.</param>
        /// <param name="fileName">The name of the apsimx file.</param>
        private static void UpgradeToVersion999(JObject root, string fileName)
        {
            // Delete all alias children.
            foreach (var soilNitrogen in JsonUtilities.ChildrenOfType(root, "SoilNitrogen"))
            {
                var parent = JsonUtilities.Parent(soilNitrogen);
                var nutrient = JsonUtilities.CreateNewChildModel(parent, "Nutrient", "Models.Soils.Nutrients.Nutrient");
                nutrient["ResourceName"] = "Nutrient";
                soilNitrogen.Remove();
            }

            foreach (var manager in JsonUtilities.ChildManagers(root))
            {
                manager.Parser.Replace("using Models.Soils;", "using Models.Soils;\r\nusing Models.Soils.Nutrients;");

                manager.Parser.Replace("SoilNitrogen.FOMN", ".Nutrient.FOMN");
                manager.Parser.Replace("SoilNitrogen.FOMC", ".Nutrient.FOMC");

                if (manager.Parser.Replace("Soil.SoilNitrogen.HumicN", "Humic.N"))
                    manager.Parser.AddDeclaration("NutrientPool", "Humic", new string[] { "[ScopedLinkByName]" });
                if (manager.Parser.Replace("Soil.SoilNitrogen.HumicC", "Humic.C"))
                    manager.Parser.AddDeclaration("NutrientPool", "Humic", new string[] { "[ScopedLinkByName]" });

                if (manager.Parser.Replace("Soil.SoilNitrogen.MicrobialN", "Microbial.N"))
                    manager.Parser.AddDeclaration("NutrientPool", "Microbial", new string[] { "[ScopedLinkByName]" });
                if (manager.Parser.Replace("Soil.SoilNitrogen.MicrobialC", "Microbial.C"))
                    manager.Parser.AddDeclaration("NutrientPool", "Microbial", new string[] { "[ScopedLinkByName]" });

                if (manager.Parser.Replace("Soil.SoilNitrogen.dlt_n_min_res", "SurfaceResidueDecomposition.MineralisedN"))
                    manager.Parser.AddDeclaration("CarbonFlow", "SurfaceResidueDecomposition", new string[] { "[LinkByPath(Path=\"[Nutrient].SurfaceResidue.Decomposition\")]" });

                manager.Parser.Replace("SoilNitrogen.MineralisedN", "Nutrient.MineralisedN");

                manager.Parser.Replace("SoilNitrogen.TotalN", "Nutrient.TotalN");
                if (manager.Parser.Replace("SoilNitrogen.TotalN", "Nutrient.TotalN"))
                {
                    manager.Parser.RemoveDeclaration("SoilNitrogen");
                    manager.Parser.AddDeclaration("INutrient", "Nutrient", new string[] { "[ScopedLinkByName]" });
                }

                manager.Parser.Replace("SoilNitrogen.TotalC", "Nutrient.TotalC");
                if (manager.Parser.Replace("SoilNitrogen.TotalC", "Nutrient.TotalC"))
                {
                    manager.Parser.RemoveDeclaration("SoilNitrogen");
                    manager.Parser.AddDeclaration("INutrient", "Nutrient", new string[] { "[ScopedLinkByName]" });
                }

                manager.Parser.Replace("SoilNitrogen.mineral_n", "Nutrient.MineralN");
                manager.Parser.Replace("SoilNitrogen.Denitrification", "Nutrient.Natm");
                manager.Parser.Replace("SoilNitrogen.n2o_atm", "Nutrient.N2Oatm");
                manager.Save();
            }

            foreach (var report in JsonUtilities.ChildrenOfType(root, "Report"))
            {
                JsonUtilities.SearchReplaceReportVariableNames(report, "SoilNitrogen.NO3.kgha", "Nutrient.NO3.kgha");
                JsonUtilities.SearchReplaceReportVariableNames(report, "SoilNitrogen.NH4.kgha", "Nutrient.NH4.kgha");
                JsonUtilities.SearchReplaceReportVariableNames(report, "SoilNitrogen.Urea.kgha", "Nutrient.Urea.kgha");
                JsonUtilities.SearchReplaceReportVariableNames(report, "SoilNitrogen.PlantAvailableNO3.kgha", "Nutrient.PlantAvailableNO3.kgha");
                JsonUtilities.SearchReplaceReportVariableNames(report, "SoilNitrogen.PlantAvailableNH4.kgha", "Nutrient.PlantAvailableNH4.kgha");
                JsonUtilities.SearchReplaceReportVariableNames(report, "[SoilNitrogen].NO3.kgha", "[Nutrient].NO3.kgha");
                JsonUtilities.SearchReplaceReportVariableNames(report, "[SoilNitrogen].NH4.kgha", "[Nutrient].NH4.kgha");
                JsonUtilities.SearchReplaceReportVariableNames(report, "[SoilNitrogen].Urea.kgha", "[Nutrient].Urea.kgha");

                JsonUtilities.SearchReplaceReportVariableNames(report, ".SoilNitrogen.FOMN", ".Nutrient.FOMN");
                JsonUtilities.SearchReplaceReportVariableNames(report, ".SoilNitrogen.FOMC", ".Nutrient.FOMC");
                JsonUtilities.SearchReplaceReportVariableNames(report, ".SoilNitrogen.HumicN", ".Nutrient.Humic.N");
                JsonUtilities.SearchReplaceReportVariableNames(report, ".SoilNitrogen.HumicC", ".Nutrient.Humic.C");
                JsonUtilities.SearchReplaceReportVariableNames(report, ".SoilNitrogen.MicrobialN", ".Nutrient.Microbial.N");
                JsonUtilities.SearchReplaceReportVariableNames(report, ".SoilNitrogen.MicrobialC", ".Nutrient.Microbial.C");
                JsonUtilities.SearchReplaceReportVariableNames(report, ".SoilNitrogen.urea", ".Nutrient.Urea.kgha");
                JsonUtilities.SearchReplaceReportVariableNames(report, ".SoilNitrogen.dlt_n_min_res", ".Nutrient.SurfaceResidue.Decomposition.MineralisedN");
                JsonUtilities.SearchReplaceReportVariableNames(report, ".SoilNitrogen.MineralisedN", ".Nutrient.MineralisedN");
                JsonUtilities.SearchReplaceReportVariableNames(report, ".SoilNitrogen.Denitrification", ".Nutrient.Natm");
                JsonUtilities.SearchReplaceReportVariableNames(report, ".SoilNitrogen.n2o_atm", ".Nutrient.N2Oatm");
                JsonUtilities.SearchReplaceReportVariableNames(report, ".SoilNitrogen.TotalC", ".Nutrient.TotalC");
                JsonUtilities.SearchReplaceReportVariableNames(report, ".SoilNitrogen.TotalN", ".Nutrient.TotalN");
                JsonUtilities.SearchReplaceReportVariableNames(report, ".SoilNitrogen.mineral_n", ".Nutrient.MineralN");
                JsonUtilities.SearchReplaceReportVariableNames(report, ".SoilNitrogen.Nitrification", ".Nutrient.NH4.Nitrification");
            }

            foreach (var series in JsonUtilities.ChildrenOfType(root, "Series"))
            {
                if (series["XFieldName"] != null)
                {
                    series["XFieldName"] = series["XFieldName"].ToString().Replace("SoilNitrogen.NO3.kgha", "Nutrient.NO3.kgha");
                    series["XFieldName"] = series["XFieldName"].ToString().Replace("SoilNitrogen.NH4.kgha", "Nutrient.NH4.kgha");
                    series["XFieldName"] = series["XFieldName"].ToString().Replace("SoilNitrogen.Urea.kgha", "Nutrient.Urea.kgha");
                    series["XFieldName"] = series["XFieldName"].ToString().Replace("SoilNitrogen.PlantAvailableNO3.kgha", "Nutrient.PlantAvailableNO3.kgha");
                    series["XFieldName"] = series["XFieldName"].ToString().Replace("SoilNitrogen.PlantAvailableNH4.kgha", "Nutrient.PlantAvailableNH4.kgha");
                }
                if (series["YFieldName"] != null)
                {
                    series["YFieldName"] = series["YFieldName"].ToString().Replace("SoilNitrogen.NO3.kgha", "Nutrient.NO3.kgha");
                    series["YFieldName"] = series["YFieldName"].ToString().Replace("SoilNitrogen.NH4.kgha", "Nutrient.NH4.kgha");
                    series["YFieldName"] = series["YFieldName"].ToString().Replace("SoilNitrogen.Urea.kgha", "Nutrient.Urea.kgha");
                    series["YFieldName"] = series["YFieldName"].ToString().Replace("SoilNitrogen.PlantAvailableNO3.kgha", "Nutrient.PlantAvailableNO3.kgha");
                    series["YFieldName"] = series["YFieldName"].ToString().Replace("SoilNitrogen.PlantAvailableNH4.kgha", "Nutrient.PlantAvailableNH4.kgha");
                }
            }

        }
    }
}

