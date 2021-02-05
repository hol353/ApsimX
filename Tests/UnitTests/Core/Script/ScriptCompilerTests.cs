namespace UnitTests.Core.Script
{
    using Models.Core.Script;
    using NUnit.Framework;
    using System;

    /// <summary>Test the script compiler.</summary>
    [TestFixture]
    public class ScriptCompilerTests
    {
        /// <summary>Compile two scripts successfully.</summary>
        [Test]
        public void CompileTwoScripts()
        {
            object s1 = null;
            ScriptCompiler.Instance.AddScript(
                "using Models.Core;" + Environment.NewLine +
                "using System;" + Environment.NewLine +
                "namespace Models" + Environment.NewLine +
                "{" + Environment.NewLine +
                "    class S1 : Model" + Environment.NewLine +
                "    {" + Environment.NewLine +
                "    }" + Environment.NewLine +
                "}" + Environment.NewLine,
                "TestS1",
                (instance) => s1 = instance);

            object s2 = null;
            ScriptCompiler.Instance.AddScript(
                "using Models.Core;" + Environment.NewLine +
                "using System;" + Environment.NewLine +
                "namespace Models" + Environment.NewLine +
                "{" + Environment.NewLine +
                "    class S2 : Model" + Environment.NewLine +
                "    {" + Environment.NewLine +
                "    }" + Environment.NewLine +
                "}" + Environment.NewLine,
                "TestS1",
                (instance) => s2 = instance);
            var errorMessges = ScriptCompiler.Instance.Compile();

            Assert.IsNull(errorMessges);
            Assert.IsNotNull(s1);
            Assert.IsNotNull(s2);
        }

        /// <summary>Ensure that only one compile happens even through 'Compile' is called twice.</summary>
        [Test]
        public void EnsureOneCompileHappensOnSuccessiveCalls()
        {
            object s1 = null;
            ScriptCompiler.Instance.AddScript(
                "using Models.Core;" + Environment.NewLine +
                "using System;" + Environment.NewLine +
                "namespace Models" + Environment.NewLine +
                "{" + Environment.NewLine +
                "    class S1 : Model" + Environment.NewLine +
                "    {" + Environment.NewLine +
                "    }" + Environment.NewLine +
                "}" + Environment.NewLine,
                "TestS1",
                (instance) => s1 = instance);
            var errorMessges = ScriptCompiler.Instance.Compile();

            Assert.IsTrue(ScriptCompiler.Instance.DidCompile);
            Assert.IsNull(errorMessges);
            Assert.IsNotNull(s1);

            ScriptCompiler.Instance.Compile();
            Assert.IsFalse(ScriptCompiler.Instance.DidCompile);
        }

        /// <summary>Compile two scripts that have errors. Ensure lines numbers are correct.</summary>
        [Test]
        public void CompileThreeScriptsWithErrors()
        {
            string badScript1 =
                "namespace Models" + Environment.NewLine +
                "{" + Environment.NewLine +
                "    class S2 : Model" + Environment.NewLine +
                "    {" + Environment.NewLine +
                "    }" + Environment.NewLine +
                "}" + Environment.NewLine;
            string badScript2 =
                "namespace Models" + Environment.NewLine +
                "{" + Environment.NewLine +
                "    class S2 : Model" + Environment.NewLine +
                "    {" + Environment.NewLine +
                "    }" + Environment.NewLine +
                "}" + Environment.NewLine;


            object s1 = null;
            ScriptCompiler.Instance.AddScript(badScript1, "TestS1", (instance) => s1 = instance);

            object s2 = null;
            ScriptCompiler.Instance.AddScript(badScript2, "TestS2", (instance) => s2 = instance);

            object s3 = null;
            ScriptCompiler.Instance.AddScript(badScript2, "TestS3", (instance) => s3 = instance);

            var errorMessges = ScriptCompiler.Instance.Compile();

            Assert.IsNotNull(errorMessges);
            Assert.IsNull(s1);
            Assert.IsNull(s2);
            Assert.IsNull(s3);
            Assert.AreEqual("TestS1, Line 3: The type or namespace name 'Model' could not be found (are you missing a using directive or an assembly reference?)" + Environment.NewLine +
                            "TestS2, Line 3: The type or namespace name 'Model' could not be found (are you missing a using directive or an assembly reference?)" + Environment.NewLine +
                            "TestS3, Line 3: The type or namespace name 'Model' could not be found (are you missing a using directive or an assembly reference?)" + Environment.NewLine,
                            errorMessges);
        }
    }
}
