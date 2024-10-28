using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Models.Functions;
using Newtonsoft.Json.Linq;

namespace Models.Core.ApsimFile
{
    /// <summary>
    /// A collection of json utilities.
    /// </summary>
    /// <remarks>
    /// If you write a new utility for this class, please write a unit test
    /// for it. See JsonUtilitiesTests.cs in the UnitTests project.
    /// </remarks>
    public static class JsonUtilities
    {
        /// <summary>
        /// Returns the name of a node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <remarks>
        /// This actually fetches the 'Apsim' name of the node.
        /// e.g. For a Report called HarvestReport this will return 
        /// HarvestReport.
        /// </remarks>
        public static string Name(JToken node)
        {
            if (node == null)
                return null;

            if (node is JObject)
            {
                JProperty nameProperty = (node as JObject).Property("Name");
                if (nameProperty == null)
                    throw new Exception(string.Format("Attempted to fetch the name property of json node {0}.", node.ToString()));
                return (string)nameProperty.Value;
            }

            if (node is JProperty)
                return (node as JProperty).Name;

            return string.Empty;
        }

        /// <summary>
        /// Returns the type of an apsim model node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="withNamespace">
        /// If true, the namespace will be included in the returned type name.
        /// e.g. Models.Core.Simulations
        /// </param>
        public static string Type(JToken node, bool withNamespace = false)
        {
            // If the node is not a JObject, it is not an apsim model.
            if (!(node is JObject))
                return null;

            JProperty typeProperty = (node as JObject).Property("$type");

            if (typeProperty == null)
                return null;

            string typeName = (string)typeProperty.Value;

            // Type is written as "Namespace.TypeName, Assembly"
            // e.g. Models.Core.Simulations, Models
            int indexOfComma = typeName.IndexOf(',');
            if (indexOfComma >= 0)
                typeName = typeName.Substring(0, indexOfComma);

            if (!withNamespace)
            {
                int indexOfLastPeriod = typeName.LastIndexOf('.');
                if (indexOfLastPeriod >= 0)
                    typeName = typeName.Substring(indexOfLastPeriod + 1);
            }

            return typeName;
        }

        /// <summary>
        /// Returns the name space of an apsim model node.
        /// </summary>
        /// <param name="node">The node.</param>
        public static string NameSpace(JToken node)
        {
            // If the node is not a JObject, it is not an apsim model.
            if (!(node is JObject))
                return null;

            JProperty typeProperty = (node as JObject).Property("$type");

            if (typeProperty == null)
                return null;

            string typeName = (string)typeProperty.Value;

            string[] allBits = typeName.Split(',');
            string lessAssembly = allBits[0];
            string[] pathBits = lessAssembly.Split('.');
            string nameSpace = "";
            for (int i = 0; i < pathBits.Length - 1; i++)
            {
                nameSpace += pathBits[i] + '.';
            }
            return nameSpace.Remove(nameSpace.Length - 1, 1);
            // Should return the name space of the type.  Eg for a class of type GenericOrgan should return "Models.PMF.Organs"
        }

        /// <summary>
        /// Returns the child models of a given node.
        /// Will never return null.
        /// </summary>
        /// <param name="node">The node.</param>
        public static List<JObject> Children(JObject node)
        {
            if (node == null)
                return new List<JObject>();

            JProperty childrenProperty = node.Property("Children");

            if (childrenProperty == null)
                return new List<JObject>();

            IEnumerable<JToken> children = childrenProperty.Values();

            if (children == null)
                return new List<JObject>();

            return children.Cast<JObject>().ToList();
        }

        /// <summary>
        /// Returns the child models of a given node that have the specified type.
        /// Will never return null.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="typeName">The type of children to return.</param>
        public static List<JObject> ChildrenOfType(JObject node, string typeName)
        {
            return ChildrenRecursively(node).Where(child => Type(child) == typeName).ToList();
        }

        /// <summary>
        /// Returns the child models in a given name space .
        /// Will never return null.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="nameSpaceName">The name space containing of children to return.</param>
        public static List<JObject> ChildrenInNameSpace(JObject node, string nameSpaceName)
        {
            return ChildrenRecursively(node).Where(child => NameSpace(child) == nameSpaceName).ToList();
        }

        /// <summary>
        /// Returns the descendant of a given node of the specified type.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="typeName">The type name of model to search for (e.g. Physical).</param>
        public static JObject DescendantOfType(JObject node, string typeName)
        {
            return ChildrenRecursively(node).FirstOrDefault(child => Type(child) == typeName);
        }

        /// <summary>
        /// Returns the first child model of a given node that has the specified name.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="name">The type of children to return.</param>
        /// <param name="ignoreCase">Perform a case-insensitive search?</param>
        /// <returns>The found child or null if not found.</returns>
        public static JObject ChildWithName(JObject node, string name, bool ignoreCase = false)
        {
            StringComparison comparisonType = ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;
            return Children(node).Find(child => string.Equals(Name(child), name, comparisonType));
        }

        /// <summary>
        /// Returns the first descendant model of a given node that has the specified name.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="name">The type of children to return.</param>
        /// <param name="ignoreCase">Perform a case-insensitive search?</param>
        /// <returns>The found child or null if not found.</returns>
        public static JObject DescendantWithName(JObject node, string name, bool ignoreCase = false)
        {
            StringComparison comparisonType = ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;
            return ChildrenRecursively(node).Find(child => string.Equals(Name(child), name, comparisonType));
        }

        /// <summary>
        /// Returns all descendants of a given node.
        /// Will never return null.
        /// </summary>
        /// <param name="node">The node.</param>
        public static List<JObject> ChildrenRecursively(JObject node)
        {
            List<JObject> descendants = new List<JObject>();
            Descendants(node, ref descendants);
            return descendants;
        }

        /// <summary>
        /// Returns a all descendants of a node, which are of a given type.
        /// Will never return null;
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="typeFilter">Type type name, with or without the namespace.</param>
        /// <returns></returns>
        public static List<JObject> ChildrenRecursively(JObject node, string typeFilter)
        {
            List<JObject> descendants = new List<JObject>();
            Descendants(node, ref descendants, typeFilter);
            return descendants;
        }

        /// <summary>
        /// Perform a search and replace in manager script.
        /// </summary>
        /// <param name="manager">The manager model.</param>
        /// <param name="searchPattern">The string to search for.</param>
        /// <param name="replacePattern">The string to replace.</param>
        public static void ReplaceManagerCode(JObject manager, string searchPattern, string replacePattern)
        {
            string code = manager["Code"]?.ToString();
            if (code == null || searchPattern == null)
                return;
            manager["Code"] = code.Replace(searchPattern, replacePattern);
        }

        /// <summary>
        /// Perform a search and replace in manager script.
        /// </summary>
        /// <param name="manager">The manager model.</param>
        /// <param name="searchPattern">The pattern to search for.</param>
        /// <param name="replacePattern">The string to replace.</param>
        /// <param name="options">Regular expression options to use. Default value is none.</param>
        public static void ReplaceManagerCodeUsingRegex(JObject manager, string searchPattern, string replacePattern, RegexOptions options = RegexOptions.None)
        {
            string code = manager["Code"]?.ToString();
            if (code == null || searchPattern == null)
                return;
            manager["Code"] = Regex.Replace(code, searchPattern, replacePattern, options);
        }

        /// <summary>
        /// Returns a list of child managers recursively.
        /// </summary>
        /// <param name="node">The root node.</param>
        /// <returns>Returns a list of manager models.</returns>
        public static List<ManagerConverter> ChildManagers(JObject node)
        {
            var managers = new List<ManagerConverter>();

            foreach (var manager in JsonUtilities.ChildrenOfType(node, "Manager"))
                managers.Add(new ManagerConverter(manager));
            return managers;
        }

        /// <summary>
        /// Returns a node of a given path. The path should be period-delimited
        /// names of subsequent child models. The first name in the path should be
        /// the name of a child model of `node`.
        /// model of `node`.
        /// </summary>
        /// <param name="node">The node to start searching from.</param>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public static JObject FindFromPath(JObject node, string path)
        {
            foreach (string name in path.Split('.'))
                node = ChildWithName(node, name);

            return node;
        }

        /// <summary>
        /// Return the parent APSIM model token for the specified model token.
        /// </summary>
        /// <param name="modelToken">The model token to find the parent for.</param>
        /// <returns>The parent or null if not found.</returns>
        public static JToken Parent(JToken modelToken)
        {
            var obj = modelToken.Parent;
            while (obj != null)
            {
                if (Type(obj) != null)
                    return obj;

                obj = obj.Parent;
            }

            return null;
        }

        /// <summary>Return all sibling models.</summary>
        /// <param name="model">The model whose siblings will be returned.</param>
        public static JObject[] Siblings(JObject model)
        {
            JObject parent = Parent(model) as JObject;
            return Children(parent).Where(c => c != model).ToArray();
        }

        /// <summary>Find a sibling with the specified name.</summary>
        /// <param name="model"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static JObject Sibling(JObject model, string name)
        {
            return Siblings(model).FirstOrDefault(s => s["Name"]?.ToString() == name);
        }

        /// <summary>Rename a child property if it exists.</summary>
        /// <param name="modelToken">The APSIM model token.</param>
        /// <param name="propertyName">The name of the property to rename.</param>
        /// <param name="newPropertyName">The new name of the property.</param>
        public static void RenameProperty(JToken modelToken, string propertyName, string newPropertyName)
        {
            var valueToken = modelToken[propertyName];
            if (valueToken != null && valueToken.Parent is JProperty)
            {
                var propertyToken = valueToken.Parent as JProperty;
                propertyToken.Remove(); // remove from parent.
                modelToken[newPropertyName] = valueToken;
            }
        }

        /// <summary>
        /// Renames a model. If a sibling model already exists with the same name,
        /// will try appending numbers to the name.
        /// </summary>
        /// <param name="model">The model to be renamed.</param>
        /// <param name="name">The new name for the model.</param>
        public static void RenameModel(JObject model, string name)
        {
            model["Name"] = name;
            for (int i = 0; i < 1000 && Sibling(model, name) != null; i++)
            {
                name = $"{name}{i}";
                model["Name"] = name;
            }
        }

        /// <summary>
        /// Renames a child node if it exists.
        /// </summary>
        /// <param name="node">Parent node.</param>
        /// <param name="childName">Name of the child to be renamed.</param>
        /// <param name="newName">New name of the child.</param>
        public static void RenameChildModel(JObject node, string childName, string newName)
        {
            JObject child = ChildWithName(node, childName);
            if (child != null)
                RenameModel(child, newName);
        }

        /// <summary>
        /// Replaces the type a child node if it exists.
        /// </summary>
        /// <param name="node">Parent node.</param>
        /// <param name="currentType">Type of child model to replace, including full namespace</param>
        /// <param name="newType">New type of child model, including full namespace</param>
        public static void ReplaceChildModelType(JObject node, string currentType, string newType)
        {
            foreach (JObject child in ChildrenRecursively(node, currentType))
                child["$type"] = child["$type"].ToString().Replace(currentType, newType);
        }

        /// <summary>
        /// Gets a list of property values.
        /// </summary>
        /// <param name="node">The model node to look under.</param>
        /// <param name="propertyName">The property name to return.</param>
        /// <returns>The values or null if not found.</returns>
        public static List<string> Values(JObject node, string propertyName)
        {
            var variableNamesObject = node[propertyName];
            if (variableNamesObject is JArray)
            {
                var array = variableNamesObject as JArray;
                return array.Values<string>().ToList();
            }
            return null;
        }

        /// <summary>
        /// Sets a list of property values.
        /// </summary>
        /// <param name="node">The model node to look under.</param>
        /// <param name="propertyName">The property name to return.</param>
        /// <param name="values">New values</param>
        /// <returns>The values or null if not found.</returns>
        public static void SetValues<T>(JObject node, string propertyName, IEnumerable<T> values)
        {
            var variableNamesObject = node[propertyName];
            if (variableNamesObject == null)
            {
                variableNamesObject = new JArray();
                node[propertyName] = variableNamesObject;
            }
            if (variableNamesObject is JArray)
            {
                var array = variableNamesObject as JArray;
                array.Clear();
                foreach (var value in values)
                    array.Add(value);
            }
        }

        /// <summary>
        /// Perform a search and replace in report variables.
        /// </summary>
        /// <param name="report">The report model.</param>
        /// <param name="searchPattern">The pattern to search for.</param>
        /// <param name="replacePattern">The string to replace.</param>
        public static bool SearchReplaceReportVariableNames(JObject report, string searchPattern, string replacePattern)
        {
            var variableNames = Values(report, "VariableNames");

            bool replacementMade = false;
            if (variableNames != null)
            {
                for (int i = 0; i < variableNames.Count; i++)
                    if (variableNames[i].Contains(searchPattern))
                    {
                        variableNames[i] = variableNames[i].Replace(searchPattern, replacePattern);
                        replacementMade = true;
                    }
                if (replacementMade)
                    SetValues(report, "VariableNames", variableNames);
            }
            return replacementMade;
        }

        /// <summary>
        /// Perform a search and replace in graph variables.
        /// </summary>
        /// <param name="graph">The graph model.</param>
        /// <param name="searchPattern">The pattern to search for.</param>
        /// <param name="replacePattern">The string to replace.</param>
        public static bool SearchReplaceGraphVariableNames(JObject graph, string searchPattern, string replacePattern)
        {

            bool replacementMade = false;

            if (graph["XFieldName"] != null)
            {
                if (graph["XFieldName"].ToString().Contains(searchPattern))
                {
                    graph["XFieldName"] = graph["XFieldName"].ToString().Replace(searchPattern, replacePattern);
                    replacementMade = true;
                }
            }
            if (graph["X2FieldName"] != null)
            {
                if (graph["X2FieldName"].ToString().Contains(searchPattern))
                {
                    graph["X2FieldName"] = graph["X2FieldName"].ToString().Replace(searchPattern, replacePattern);
                    replacementMade = true;
                }
            }
            if (graph["YFieldName"] != null)
            {
                if (graph["YFieldName"].ToString().Contains(searchPattern))
                {
                    graph["YFieldName"] = graph["YFieldName"].ToString().Replace(searchPattern, replacePattern);
                    replacementMade = true;
                }
            }
            if (graph["Y2FieldName"] != null)
            {
                if (graph["Y2FieldName"].ToString().Contains(searchPattern))
                {
                    graph["y2FieldName"] = graph["Y2FieldName"].ToString().Replace(searchPattern, replacePattern);
                    replacementMade = true;
                }
            }
          
            return replacementMade;
        }

        /// <summary>
        /// Add a constant function to the specified JSON model token.
        /// </summary>
        /// <param name="modelToken">The APSIM model token.</param>
        /// <param name="name">The name of the constant function</param>
        /// <param name="fixedValue">The fixed value of the constant function</param>
        public static void AddConstantFunctionIfNotExists(JObject modelToken, string name, double fixedValue)
        {
            string stringValue = fixedValue.ToString(CultureInfo.InvariantCulture);
            AddConstantFunctionIfNotExists(modelToken, name, stringValue);
        }

        /// <summary>
        /// Add a constant function to the specified JSON model token.
        /// </summary>
        /// <param name="modelToken">The APSIM model token.</param>
        /// <param name="name">The name of the constant function</param>
        /// <param name="fixedValue">The fixed value of the constant function</param>
        public static void AddConstantFunctionIfNotExists(JObject modelToken, string name, string fixedValue)
        {
            if (ChildWithName(modelToken, name, true) == null)
            {
                JArray children = modelToken["Children"] as JArray;
                if (children == null)
                {
                    children = new JArray();
                    modelToken["Children"] = children;
                }

                JObject constantModel = new JObject();
                constantModel["$type"] = "Models.Functions.Constant, Models";
                constantModel["Name"] = name;
                constantModel["FixedValue"] = fixedValue;
                children.Add(constantModel);
            }
        }

        /// <summary>
        /// Add a variable reference function to the specified JSON model token,
        /// if a variable referenec with the given name doesn't already exist.
        /// </summary>
        /// <param name="modelToken">The JSON node to which the variable reference will be added.</param>
        /// <param name="name">Name of the variabel reference to add.</param>
        /// <param name="variable">The variable to be referenced by the variable reference function.</param>
        public static void AddVariableReferenceIfNotExists(JObject modelToken, string name, string variable)
        {
            if (ChildWithName(modelToken, name, true) == null)
            {
                JArray children = modelToken["Children"] as JArray;
                if (children == null)
                {
                    children = new JArray();
                    modelToken["Children"] = children;
                }

                JObject variableReference = new JObject();
                variableReference["$type"] = "Models.Functions.VariableReference, Models";
                variableReference["Name"] = name;
                variableReference["VariableName"] = variable;
                children.Add(variableReference);
            }
        }

        /// <summary>
        /// Adds the given model as a child of node.
        /// </summary>
        /// <param name="node">Node to which the model will be added.</param>
        /// <param name="model">Child model to be added to node.</param>
        /// <remarks>
        /// If we ever rename the Children property of IModel, this (along with
        /// many other things) will break horribly.
        /// </remarks>
        public static void AddModel(JObject node, IModel model)
        {
            var children = node["Children"] as JArray;
            if (children == null)
            {
                children = new JArray();
                node["Children"] = children;
            }
            string json = FileFormat.WriteToString(model);
            JObject child = JObject.Parse(json);
            children.Add(child);
        }

        /// <summary>
        /// Adds a model of a given type as a child of node.
        /// </summary>
        /// <param name="node">Node to which the model will be added.</param>
        /// <param name="t">Type of the child model to be added to node.</param>
        public static void AddModel(JObject node, Type t)
        {
            AddModel(node, t, t.Name);
        }

        /// <summary>
        /// Adds a model of a given type as a child of node.
        /// </summary>
        /// <param name="node">Node to which the model will be added.</param>
        /// <param name="t">Type of the child model to be added to node.</param>
        /// <param name="name">Name of the model to be added.</param>
        public static void AddModel(JObject node, Type t, string name)
        {
            if (!(typeof(IModel).IsAssignableFrom(t)))
                throw new Exception(string.Format("Unable to add model of type {0} as a child node - it is not an IModel.", t.FullName));
            if (name == null)
                throw new Exception(string.Format("Unable to add model of type {0} to node of type {1}: Provided name is null.", t.FullName, node["$type"]));
            IModel model = (IModel)t.Assembly.CreateInstance(t.FullName);
            model.Name = name;
            AddModel(node, model);
        }

        /// <summary>
        /// Create and add a new child model node.
        /// </summary>
        /// <param name="parent">The parent model node.</param>
        /// <param name="name">The model name.</param>
        /// <param name="fullTypeName">The typespace name + model class name eg. Models.Clock</param>
        public static JObject CreateNewChildModel(JToken parent, string name, string fullTypeName)
        {
            var children = parent["Children"] as JArray;
            if (children == null)
            {
                children = new JArray();
                parent["Children"] = children;
            }

            var newChild = new JObject();
            newChild["$type"] = fullTypeName + ", Models";
            newChild["Name"] = name;
            children.Add(newChild);
            return newChild;
        }

        /// <summary>
        /// Renames a child node if it exists.
        /// </summary>
        /// <param name="node">Parent node.</param>
        /// <param name="childName">Name of the child to be removed.</param>
        public static void RemoveChild(JObject node, string childName)
        {
            var child = ChildWithName(node, childName);
            if (child == null)
                return;

            child.Remove();
        }

        /// <summary>
        /// Renames a child node if it exists.
        /// </summary>
        /// <param name="node">Parent node.</param>
        public static void RemoveChildren(JObject node)
        {
            var children = node["Children"] as JArray;
            children.RemoveAll();
        }

        /// <summary>
        /// Helper method for <see cref="ChildrenRecursively(JObject)"/>.
        /// Will never return null.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="descendants">List of descendants.</param>
        /// <param name="typeFilter">Type name by which to filter.</param>
        private static void Descendants(JObject node, ref List<JObject> descendants, string typeFilter = null)
        {
            if (node == null)
                return;

            List<JObject> children = Children(node);
            if (children == null)
                return;

            if (descendants == null)
                descendants = new List<JObject>();

            foreach (JObject child in children)
            {
                if (string.IsNullOrEmpty(typeFilter) || Type(child, typeFilter.Contains('.')) == typeFilter)
                    descendants.Add(child);
                Descendants(child, ref descendants, typeFilter);
            }
        }

        /// <summary>
        /// Helper method for renaming variables in report and manager.
        /// </summary>
        /// <param name="node">The JSON root node.</param>
        /// <param name="changes">List of old and new name tuples.</param>
        public static bool RenameVariables(JObject node, Tuple<string, string>[] changes)
        {
            bool replacementMade = false;
            foreach (var manager in JsonUtilities.ChildManagers(node))
            {

                foreach (var replacement in changes)
                {
                    if (manager.Replace(replacement.Item1, replacement.Item2))
                        replacementMade = true;
                }
                if (replacementMade)
                    manager.Save();
            }
            foreach (var report in JsonUtilities.ChildrenOfType(node, "Report"))
            {
                foreach (var replacement in changes)
                {
                    if (JsonUtilities.SearchReplaceReportVariableNames(report, replacement.Item1, replacement.Item2))
                        replacementMade = true;
                }
            }

            foreach (var simpleGrazing in JsonUtilities.ChildrenOfType(node, "SimpleGrazing"))
            {
                var expression = simpleGrazing["FlexibleExpressionForTimingOfGrazing"]?.ToString();
                if (!string.IsNullOrEmpty(expression))
                {
                    foreach (var replacement in changes)
                    {
                        if (expression.Contains(replacement.Item1))
                        {
                            expression = expression.Replace(replacement.Item1, replacement.Item2);
                            replacementMade = true;
                        }
                    }
                    simpleGrazing["FlexibleExpressionForTimingOfGrazing"] = expression;
                }
            }
            foreach (var factor in JsonUtilities.ChildrenOfType(node, "Factor"))
            {
                var specification = factor["Specification"]?.ToString();
                if (specification != null)
                {
                    bool replacementFound = false;
                    foreach (var replacement in changes)
                    {
                        replacementFound = specification.ToString().Contains(replacement.Item1) || replacementFound;
                        specification = specification.ToString().Replace(replacement.Item1, replacement.Item2);
                    }
                    if (replacementFound)
                    {
                        replacementMade = true;
                        factor["Specification"] = specification;
                    }
                }
            }

            foreach (var compositeFactor in JsonUtilities.ChildrenOfType(node, "CompositeFactor"))
            {
                var specifications = compositeFactor["Specifications"] as JArray;
                if (specifications != null)
                {
                    bool replacementFound = false;
                    foreach (var replacement in changes)
                        for (int i = 0; i < specifications.Count; i++)
                        {
                            replacementFound = replacementFound || specifications[i].ToString().Contains(replacement.Item1);
                            specifications[i] = specifications[i].ToString().Replace(replacement.Item1, replacement.Item2);
                        }
                    if (replacementFound)
                    {
                        replacementMade = true;
                        compositeFactor["Specifications"] = specifications;
                    }
                }
            }

            foreach (JObject variableRef in ChildrenOfType(node, typeof(VariableReference).Name))
            {
                foreach (var replacement in changes)
                {
                    string variableName = variableRef["VariableName"]?.ToString();
                    if (variableName.Contains(replacement.Item1))
                    {
                        replacementMade = true;
                        variableRef["VariableName"] = variableName?.Replace(replacement.Item1, replacement.Item2);
                    }
                }
            }

            foreach (JObject series in ChildrenOfType(node, nameof(Series)))
            {
                foreach (var change in changes)
                {
                    var from = change.Item1.Replace("[", "").Replace("]", "");
                    var to = change.Item2.Replace("[", "").Replace("]", "");
                    if (series["XFieldName"]?.ToString() != null && series["XFieldName"].ToString().Contains(from))
                    {
                        replacementMade = true;
                        series["XFieldName"] = series["XFieldName"].ToString().Replace(from, to);
                    }

                    if (series["YFieldName"]?.ToString() != null && series["YFieldName"].ToString().Contains(from))
                    {
                        replacementMade = true;
                        series["YFieldName"] = series["YFieldName"].ToString().Replace(from, to);
                    }

                    if (series["X2FieldName"]?.ToString() != null && series["X2FieldName"].ToString().Contains(from))
                    {
                        replacementMade = true;
                        series["X2FieldName"] = series["X2FieldName"].ToString().Replace(from, to);
                    }

                    if (series["Y2FieldName"]?.ToString() != null && series["Y2FieldName"].ToString().Contains(from))
                    {
                        replacementMade = true;
                        series["Y2FieldName"] = series["Y2FieldName"].ToString().Replace(from, to);
                    }

                    if (series["Filter"]?.ToString() != null && series["Filter"].ToString().Contains(from))
                    {
                        replacementMade = true;
                        series["Filter"] = series["Filter"].ToString().Replace(from, to);
                    }
                }
            }

            foreach (var croptimizr in JsonUtilities.ChildrenOfType(node, "CroptimizR"))
            {
                foreach (var replacement in changes)
                {
                    foreach (var parameter in croptimizr["Parameters"] as JArray)
                    {
                        var path = parameter["Path"].ToString();
                        if (path.Contains(replacement.Item1))
                        {
                            parameter["Path"] = path.Replace(replacement.Item1, replacement.Item2);
                            replacementMade = true;
                        }
                    }
                }
            }


            return replacementMade;
        }

        /// <summary>
        /// Find an ancestor model of the given type.
        /// </summary>
        /// <param name="token">Model whose ancestor we want to find.</param>
        /// <param name="type">Type of ancestor to search for.</param>
        public static JObject Ancestor(JObject token, Type type)
        {
            JToken parent = Parent(token);
            while (parent != null)
            {
                Type parentType = System.Type.GetType(Type(parent, true));
                if (type.IsAssignableFrom(parentType))
                    return parent as JObject;

                parent = Parent(parent);
            }

            return null;
        }

        /// <summary>
        /// Find an ancestor model of the given type.
        /// </summary>
        /// <param name="token">Model whose ancestor we want to find.</param>
        /// <param name="typeName">Type name of ancestor to search for.</param>
        public static JObject Ancestor(JObject token, string typeName)
        {
            JToken parent = Parent(token);
            while (parent != null)
            {
                var typeTokens = Type(parent, true).Split(".");
                if (typeTokens != null && typeTokens.Last() == typeName)
                    return parent as JObject;

                parent = Parent(parent);
            }

            return null;
        }

        /// <summary>
        /// Adds a JObject child to another JObject.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="child"></param>
        public static void AddChild(JObject node, JObject child)
        {
            var children = node["Children"] as JArray;
            children.Add(child);
        }

    }
}
