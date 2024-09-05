using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using APSIM.Shared.Utilities;
using Models.CLEM.Reporting;
using Models.Core;

namespace Crop2ML.Interop;

/// <summary>
///
/// </summary>
public class Crop2MLInterop
{
    /// <summary>
    /// Write an xml file.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="sourcFileName"></param>
    /// <param name="fileName"></param>
    public static void WriteXmlFile(Type type, string sourcFileName, string fileName)
    {
        var modelUnit = GetModelDescription(type, sourcFileName);
        var serializer = new XmlSerializer(typeof(ModelUnit));

        XmlWriterSettings settings = new();
        settings.Indent = true;

        XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
        ns.Add(string.Empty, string.Empty);

        using StreamWriter writer = new(fileName);
        using XmlWriter xmlWriter = XmlWriter.Create(writer, settings);

        serializer.Serialize(xmlWriter, modelUnit, ns);
    }

    /// <summary>Get a model description.</summary>
    /// <param name="type">The type to document.</param>
    public static ModelUnit GetModelDescription(Type type, string sourcFileName)
    {
        List<(string oldName, string newName)> replacements = new();  // map of oldname, newname
        ModelUnit modelUnit = new();
        modelUnit.Inputs = new();
        modelUnit.Inputs.Input = new();
        modelUnit.Outputs = new();
        modelUnit.Outputs.Output = new();

        string sourceCode = File.ReadAllText(sourcFileName);

        // Iterate though all [Link] fields.
        foreach (FieldInfo field in type.GetFields(BindingFlags.Public |
                                                   BindingFlags.NonPublic |
                                                   BindingFlags.Instance |
                                                   BindingFlags.FlattenHierarchy))
        {
            LinkAttribute linkAttribute = field.GetCustomAttribute<LinkAttribute>();
            if (linkAttribute != null)
            {
                string instanceName = field.Name;

                // Now iterate through all instances where this instance is used in the source code.
                foreach (string usedPropertyName in EnumerateFieldUsage(field.Name, sourceCode))
                {
                    // Find the declaration of the property that was used.
                    PropertyInfo property = field.FieldType.GetProperty(usedPropertyName, BindingFlags.Public |
                                                                                          BindingFlags.NonPublic |
                                                                                          BindingFlags.Instance |
                                                                                          BindingFlags.FlattenHierarchy);
                    if (property == null)
                        throw new Exception($"Cannot find property {usedPropertyName} in type {field.FieldType}");

                    replacements.Add(($"{field.Name}.{usedPropertyName}",$"i{usedPropertyName}"));
                    VariableProperty variableProperty = new(null, property);

                    // Add an exogenous variable to model unit.
                    string description = variableProperty.Description ?? usedPropertyName;
                    modelUnit.Inputs.Input.Add(new()
                    {
                        Name = usedPropertyName,
                        Description = description,
                        Inputtype = "variable",
                        Variablecategory = "exogenous",
                        Datatype = property.PropertyType.Name.ToUpper(),
                        Unit = variableProperty.Units
                    });
                }
            }
        }

        // Iterate though all properties. They can be either outputs or parameters if they have a [Display] attribute.
        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public |
                                                             BindingFlags.Instance |
                                                             BindingFlags.DeclaredOnly))
        {
            VariableProperty variableProperty = new(null, property);

            // Add an exogenous variable to model unit.
            string description = variableProperty.Description ?? property.Name;

            if (variableProperty.Display == null)
                modelUnit.Outputs.Output.Add(new()
                {
                    Name = property.Name,
                    Description = description,
                    Variablecategory = "state",
                    Datatype = property.PropertyType.Name.ToUpper(),
                    Unit = variableProperty.Units
                });
            else
                modelUnit.Inputs.Input.Add(new()
                {
                    Name = property.Name,
                    Description = description,
                    Inputtype = "parameter",
                    Parametercategory = "constant",
                    Datatype = property.PropertyType.Name.ToUpper(),
                    Unit = variableProperty.Units
                });

        }

        string modifiedSourceCode = ConverSourceCodeToCrop2MLFriendly(replacements, modelUnit, sourceCode);

        File.WriteAllText(Path.ChangeExtension(sourcFileName, ".modified.cs"), modifiedSourceCode);
        return modelUnit;
    }

    /// <summary>
    /// Clean up source code to make it more friendly to Crop2ML.
    /// </summary>
    /// <param name="replacements"></param>
    /// <param name="modelUnit"></param>
    /// <param name="sourceCode"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private static string ConverSourceCodeToCrop2MLFriendly(List<(string oldName, string newName)> replacements, ModelUnit modelUnit, string sourceCode)
    {
        // Remove old variable names.
        foreach (var replacement in replacements)
            sourceCode = sourceCode.Replace(replacement.oldName, replacement.newName);

        // Create an input declaration section that will be inserted into source code.
        StringBuilder builder = new();
        foreach (var input in modelUnit.Inputs.Input)
        {
            if (input.Inputtype == "variable")
            {
                string dataType = input.Datatype;
                if (dataType == "DATETIME")
                    dataType = "DateTime";
                else
                    dataType = dataType.ToLower();
                builder.AppendLine($"private {dataType} i{input.Name};");
            }
        }

        // Add new inputs to code at top of class.
        var match = Regex.Match(sourceCode, @"public class .+\n\s*({)");
        if (!match.Success)
            throw new Exception("Cannot find start of class in sourcecode");
        int posOpenBrace = match.Groups[1].Index;

        // Determine the indent number of characters.
        int indent = posOpenBrace - sourceCode.LastIndexOf('\n', posOpenBrace) + 4;
        string textToInsert = StringUtilities.IndentText(builder.ToString(), indent);
        textToInsert += Environment.NewLine;

        // Insert input declarations into source code
        sourceCode = sourceCode.Insert(posOpenBrace + 3, textToInsert);
        return sourceCode;
    }

    /// <summary>
    /// Enumerate all usages of a object instance.
    /// </summary>
    /// <param name="name">The instance name of the object.</param>
    /// <param name="sourceCode">The source code to search.</param>
    /// <returns>A collection of field names of the instance that were referenced.</returns>
    private static IEnumerable<string> EnumerateFieldUsage(string name, string sourceCode)
    {
        return Regex.Matches(sourceCode, @$"{name}\.(\w+)")
                    .Where(m => !IsCommentedOut(m, sourceCode))
                    .Select(m => m.Groups[1].ToString())
                    .Distinct();
    }

    /// <summary>
    /// Determine if a match is commented out.
    /// </summary>
    /// <param name="m"></param>
    /// <returns></returns>
    private static bool IsCommentedOut(Match m, string sourceCode)
    {
        // Scan backwards for a '//' or '\n'. If // is found first then the match is commented out.
        int i = Math.Max(sourceCode.LastIndexOf("//", m.Index),
                         sourceCode.LastIndexOf("\n", m.Index));
        return i >= 0 && sourceCode.Substring(i, 2) == "//";
    }

    /// <summary>Return a datatable of methods for the specified type.</summary>
    /// <param name="type">The type to document.</param>
    private static void GetMethods(Type type)
    {
        /*DataTable methods = new DataTable("Methods");
        methods.Columns.Add("Name", typeof(string));
        methods.Columns.Add("Description", typeof(string));

        foreach (MethodInfo method in type.GetMethods(System.Reflection.BindingFlags.Public |
                                                System.Reflection.BindingFlags.Instance |
                                                System.Reflection.BindingFlags.DeclaredOnly))
        {
            if (!method.IsSpecialName)
            {
                DataRow row = methods.NewRow();
                string parameters = null;
                foreach (ParameterInfo argument in method.GetParameters())
                {
                    if (parameters != null)
                        parameters += ", ";
                    parameters += GetTypeName(argument.ParameterType) + " " + argument.Name;
                }
                string description = CodeDocumentation.GetSummary(method);
                string remarks = CodeDocumentation.GetRemarks(method);
                if (!string.IsNullOrEmpty(remarks))
                    description += Environment.NewLine + Environment.NewLine + remarks;
                string methodName = method.Name;
                // Italicise the method description.
                if (!string.IsNullOrEmpty(description))
                    description = $"*{description}*";
                StringBuilder st = new StringBuilder();
                string returnType = GetTypeName(method.ReturnType);
                st.Append(returnType);
                st.Append(" ");
                st.Append(method.Name);
                st.Append("(");
                st.Append(parameters);
                st.AppendLine(")");
                st.AppendLine();
                st.Append(description);

                row["Name"] = method.Name;
                row["Description"] = st.ToString();//.Replace("\r\n", " ");

                methods.Rows.Add(row);
            }
        }

        if (methods.Rows.Count > 0)
            return methods;
        else
            return null;*/
    }
}