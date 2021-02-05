namespace UnitTests.Core.Script
{
    using Models.Core.Script;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;

    /// <summary>Test the script parser. </summary>
    [TestFixture]
    public class ScriptParserTests
    {
        /// <summary>Ensure we can get using statements from parser.</summary>
        [Test]
        public void GetUsingStatements()
        {
            string script =
                "// Comment 1" + Environment.NewLine +
                "// Comment 2" + Environment.NewLine +
                Environment.NewLine +
                "using System" + Environment.NewLine +
                Environment.NewLine +
                "using Models.Soils;" + Environment.NewLine +
                "using APSIM.Shared.Utilities;" + Environment.NewLine +
                Environment.NewLine +
                "namespace Models" + Environment.NewLine +
                "{" + Environment.NewLine +
                "}" + Environment.NewLine;

            var parser = new ScriptParser(script);
            Assert.AreEqual(parser.GetUsingStatements(), 
                            new string[] { "System", "Models.Soils", "APSIM.Shared.Utilities" });
        }

        /// <summary>Ensure we can set using statements.</summary>
        [Test]
        public void SetUsingStatements()
        {
            string script =
                "// Comment 1" + Environment.NewLine +
                "// Comment 2" + Environment.NewLine +
                Environment.NewLine +
                "using System" + Environment.NewLine +
                Environment.NewLine +
                "using Models.Soils;" + Environment.NewLine +
                "using APSIM.Shared.Utilities;" + Environment.NewLine +
                Environment.NewLine +
                "namespace Models" + Environment.NewLine +
                "{" + Environment.NewLine +
                "}" + Environment.NewLine;

            var parser = new ScriptParser(script);
            parser.SetUsingStatements(new string[] { "System" });
            Assert.AreEqual(parser.ToString(),
                "// Comment 1" + Environment.NewLine +
                "// Comment 2" + Environment.NewLine +
                Environment.NewLine +
                "using System;" + Environment.NewLine +
                Environment.NewLine +
                "namespace Models" + Environment.NewLine +
                "{" + Environment.NewLine +
                "}" + Environment.NewLine);

        }

        /// <summary>Ensure we can find declarations</summary>
        [Test]
        public void GetDeclarations()
        {
            string script =
                "using System" + Environment.NewLine +
                "namespace Models" + Environment.NewLine +
                "{" + Environment.NewLine +
                "    public class Script : Model" + Environment.NewLine +
                "    {" + Environment.NewLine +
                "        [Link] SoluteManager   mySolutes1;" + Environment.NewLine +
                "        [Link] " + Environment.NewLine +
                "        [Units(0-1)] " + Environment.NewLine +
                "        Fertiliser  fert;" + Environment.NewLine +
                "        [Link(Type = LinkType.Descendant, ByName = true)] " + Environment.NewLine +
                "        Soil mySoil;" + Environment.NewLine +
                "    }" + Environment.NewLine +
                "}" + Environment.NewLine;

            var parser = new ScriptParser(script);

            var declarations = parser.GetDeclarations();

            Assert.AreEqual(declarations[0].LineIndex, 5);
            Assert.AreEqual(declarations[0].InstanceName, "mySolutes1");
            Assert.AreEqual(declarations[0].TypeName, "SoluteManager");
            Assert.AreEqual(declarations[0].Attributes[0], "[Link]");

            Assert.AreEqual(declarations[1].LineIndex, 8);
            Assert.AreEqual(declarations[1].InstanceName, "fert");
            Assert.AreEqual(declarations[1].TypeName, "Fertiliser");
            Assert.IsTrue(declarations[1].Attributes.Contains("[Link]"));
            Assert.IsTrue(declarations[1].Attributes.Contains("[Units(0-1)]"));

            Assert.AreEqual(declarations[2].LineIndex, 10);
            Assert.AreEqual(declarations[2].InstanceName, "mySoil");
            Assert.AreEqual(declarations[2].TypeName, "Soil");
            Assert.AreEqual(declarations[2].Attributes[0], "[Link(Type = LinkType.Descendant, ByName = true)]");
        }

        /// <summary>Ensure we can find method calls</summary>
        [Test]
        public void FindMethodCalls()
        {
            string script =
                "using System" + Environment.NewLine +
                "namespace Models" + Environment.NewLine +
                "{" + Environment.NewLine +
                "    public class Script : Model" + Environment.NewLine +
                "    {" + Environment.NewLine +
                "        [Link] SoluteManager mySolutes1;" + Environment.NewLine +
                "        [Link] " +
                "        SoluteManager mySolutes2;" + Environment.NewLine +
                "        Fertiliser fert;" + Environment.NewLine +
                "        private void OnSimulationCommencing(object sender, EventArgs e)" + Environment.NewLine +
                "        {" + Environment.NewLine +
                "            mySolutes1.Add(arg1, arg2);" + Environment.NewLine +
                "            mySolutes2.Add (arg3,arg4);" + Environment.NewLine +
                "            fake.Add (arg3,arg4);" + Environment.NewLine +
                "            fert.Add (arg3,arg4);" + Environment.NewLine +
                "        }" + Environment.NewLine +
                "    }" + Environment.NewLine +
                "}" + Environment.NewLine;

            var parser = new ScriptParser(script);
            
            var methods = parser.FindMethodCalls("SoluteManager", "Add");
            Assert.AreEqual(methods.Count, 2);
            Assert.AreEqual(methods[0].InstanceName, "mySolutes1");
            Assert.AreEqual(methods[0].MethodName, "Add");
            Assert.AreEqual(methods[0].Arguments, new string[] { "arg1", "arg2" });
            Assert.AreEqual(methods[1].InstanceName, "mySolutes2");
            Assert.AreEqual(methods[1].MethodName, "Add");
            Assert.AreEqual(methods[1].Arguments, new string[] { "arg3", "arg4" });
        }

        /// <summary>Ensure we can set method call</summary>
        [Test]
        public void SetMethodCall()
        {
            string script =
                "using System" + Environment.NewLine +
                "namespace Models" + Environment.NewLine +
                "{" + Environment.NewLine +
                "    public class Script : Model" + Environment.NewLine +
                "    {" + Environment.NewLine +
                "        [Link] SoluteManager mySolutes1;" + Environment.NewLine +
                "        private void OnSimulationCommencing(object sender, EventArgs e)" + Environment.NewLine +
                "        {" + Environment.NewLine +
                "            mySolutes1.Add(arg1, arg2);" + Environment.NewLine +
                "        }" + Environment.NewLine +
                "    }" + Environment.NewLine +
                "}" + Environment.NewLine;

            var parser = new ScriptParser(script);

            var method = new ScriptParser.MethodCall
            {
                LineIndex = 8,
                InstanceName = "mySolutes1",
                MethodName = "Add2",
                Arguments = new List<string>()
            };
            method.Arguments.Add("10");
            parser.SetMethodCall(method);

            // Make sure we can't find old one.
            Assert.AreEqual(parser.FindMethodCalls("SoluteManager", "Add").Count, 0);

            // Make sure we find new one.
            var foundMethod = parser.FindMethodCalls("SoluteManager", "Add2")[0];
            Assert.AreEqual(foundMethod.LineIndex, 8);
            Assert.AreEqual(foundMethod.InstanceName, "mySolutes1");
            Assert.AreEqual(foundMethod.MethodName, "Add2");
            Assert.AreEqual(foundMethod.Arguments, new string[] { "10" });
        }

        /// <summary>
        /// Ensures the SearchReplaceManagerText method works correctly.
        /// </summary>
        [Test]
        public void ReplaceTextTests()
        {
            var parser = new ScriptParser("original text");

            string newText = "new text";
            parser.Replace("original text", newText);

            // Ensure the code was modified correctly.
            Assert.AreEqual(newText + Environment.NewLine, parser.ToString());

            // Ensure that passing in a null search string causes no changes.
            parser.Replace(null, "test");
            Assert.AreEqual(newText + Environment.NewLine, parser.ToString());

            // Attempt to replace code of a node which doesn't have a code
            // property. Ensure that no code property is created (and that
            // no exception is thrown).
            var parserWithNoCode = new ScriptParser("");
            parserWithNoCode.Replace("test1", "test2");
            Assert.Null(parserWithNoCode.ToString());
        }

        /// <summary>
        /// Ensures the ReplaceManagerCodeUsingRegex method works correctly.
        /// </summary>
        [Test]
        public void ReplaceCodeRegexTests()
        {
            var parser = new ScriptParser("original text");

            // This regular expression will effectively remove the first space.
            // There are simpler ways to achieve this but this method tests
            // backreferencing.
            string newText = "originaltext" + Environment.NewLine;
            parser.ReplaceRegex(@"([^\s]*)\s", @"$1");
            Assert.AreEqual(parser.ToString(), newText);

            // Ensure that passing in a null search string causes no changes.
            parser.ReplaceRegex(null, "test");
            Assert.AreEqual(parser.ToString(), newText);

            // Attempt to replace code of a node which doesn't have a code
            // property. Ensure that no code property is created (and that
            // no exception is thrown).
            var parserWithNoCode = new ScriptParser("");
            parserWithNoCode.ReplaceRegex("test1", "test2");
            Assert.Null(parserWithNoCode.ToString());
        }

        /// <summary>
        /// Ensures the AddDeclaration method works correctly when
        /// the manager object has an empty script.
        /// </summary>
        [Test]
        public void AddDeclarationToEmptyScript()
        {
            var parser = new ScriptParser("using System;");

            parser.AddDeclaration("NutrientPool", "Humic", new string[] { "[Link]" });

            // Ensure the link has been added below the using statement.
            Assert.AreEqual(parser.ToString(),
                "using System;" + Environment.NewLine +
                "namespace Models" + Environment.NewLine +
                "{" + Environment.NewLine +
                "    [Serializable]" + Environment.NewLine +
                "    public class Script : Model" + Environment.NewLine +
                "    {" + Environment.NewLine +
                "        [Link]" + Environment.NewLine +
                "        private NutrientPool Humic;" + Environment.NewLine +
                "    }" + Environment.NewLine +
                "}" + Environment.NewLine);
        }

        /// <summary>
        /// Ensures the AddDeclaration method works correctly when
        /// the manager object has no declarations.
        /// </summary>
        [Test]
        public void AddDeclarationToEmptyDeclarationSection()
        {
            var parser = new ScriptParser(
                "using System;" + Environment.NewLine +
                "namespace Models" + Environment.NewLine +
                "{" + Environment.NewLine +
                "    [Serializable]" + Environment.NewLine +
                "    public class Script : Model" + Environment.NewLine +
                "    {" + Environment.NewLine +
                "    }" + Environment.NewLine +
                "}" + Environment.NewLine);

            parser.AddDeclaration("NutrientPool", "Humic", new string[] { "[Link]" });

            // Ensure the link has been added below the using statement.
            Assert.AreEqual(parser.ToString(),
                "using System;" + Environment.NewLine +
                "namespace Models" + Environment.NewLine +
                "{" + Environment.NewLine +
                "    [Serializable]" + Environment.NewLine +
                "    public class Script : Model" + Environment.NewLine +
                "    {" + Environment.NewLine +
                "        [Link]" + Environment.NewLine +
                "        private NutrientPool Humic;" + Environment.NewLine +
                "    }" + Environment.NewLine +
                "}" + Environment.NewLine);
        }

        /// <summary>
        /// Ensures the AddDeclaration method works correctly when
        /// the parser object has no declarations.
        /// </summary>
        [Test]
        public void AddDeclarationToExistingDeclarationSection()
        {
            var parser = new ScriptParser(
                "using System;" + Environment.NewLine +
                "namespace Models" + Environment.NewLine +
                "{" + Environment.NewLine +
                "    [Serializable]" + Environment.NewLine +
                "    public class Script : Model" + Environment.NewLine +
                "    {" + Environment.NewLine +
                "        [Link]" + Environment.NewLine +
                "        A B;" + Environment.NewLine + 
                "    }" + Environment.NewLine +
                "}" + Environment.NewLine);

            parser.AddDeclaration("NutrientPool", "Humic", new string[] { "[Link]" });

            // Ensure the link has been added below the using statement.
            Assert.AreEqual(parser.ToString(),
                "using System;" + Environment.NewLine +
                "namespace Models" + Environment.NewLine +
                "{" + Environment.NewLine +
                "    [Serializable]" + Environment.NewLine +
                "    public class Script : Model" + Environment.NewLine +
                "    {" + Environment.NewLine +
                "        [Link]" + Environment.NewLine +
                "        private A B;" + Environment.NewLine +
                "        [Link]" + Environment.NewLine +
                "        private NutrientPool Humic;" + Environment.NewLine +
                "    }" + Environment.NewLine +
                "}" + Environment.NewLine);
        }

        /// <summary>
        /// Ensures the AddDeclaration method works correctly when
        /// the parser object has no declarations.
        /// </summary>
        [Test]
        public void AddDeclarationHandleProperties()
        {
            var parser = new ScriptParser(
                "using System;" + Environment.NewLine +
                "namespace Models" + Environment.NewLine +
                "{" + Environment.NewLine +
                "    [Serializable]" + Environment.NewLine +
                "    public class Script : Model" + Environment.NewLine +
                "    {" + Environment.NewLine +
                "        [Link] private A B = null;" + Environment.NewLine +
                "        [Link] " + Environment.NewLine +
                "        public C D;" + Environment.NewLine +
                "        [Link] E F;" + Environment.NewLine +
                "        [Description(\"Turn ferliser applications on? \")]" + Environment.NewLine +
                "        public yesnoType AllowFertiliser { get; set; }" + Environment.NewLine +
                "    }" + Environment.NewLine +
                "}" + Environment.NewLine);

            parser.AddDeclaration("NutrientPool", "Humic", new string[] { "[Link]" });

            // Ensure the link has been added below the using statement.
            Assert.AreEqual(parser.ToString(),
                "using System;" + Environment.NewLine +
                "namespace Models" + Environment.NewLine +
                "{" + Environment.NewLine +
                "    [Serializable]" + Environment.NewLine +
                "    public class Script : Model" + Environment.NewLine +
                "    {" + Environment.NewLine +
                "        [Link] private A B;" + Environment.NewLine +
                "        [Link]" + Environment.NewLine +
                "        public C D;" + Environment.NewLine +
                "        [Link] private E F;" + Environment.NewLine + 
                "        [Link]" + Environment.NewLine +
                "        private NutrientPool Humic;" + Environment.NewLine +
                "        [Description(\"Turn ferliser applications on? \")]" + Environment.NewLine +
                "        public yesnoType AllowFertiliser { get; set; }" + Environment.NewLine +
                "    }" + Environment.NewLine +
                "}" + Environment.NewLine);
        }
    }
}
