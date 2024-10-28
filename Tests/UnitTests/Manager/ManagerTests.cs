using APSIM.Shared.Utilities;
using Models;
using Models.Core;
using NUnit.Framework;
using System;
using APSIM.Shared.Documentation;
using Models.Core.ApsimFile;
using Models.Core.Run;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnitTests.Storage;
using Shared.Utilities;
using Models.Logging;
using APSIM.Documentation.Models;

namespace UnitTests.ManagerTests
{

    /// <summary>
    /// Unit Tests for manager scripts.
    /// </summary>
    class ManagerTests
    {
        /// <summary>Flags required for reflection that gets all public and private methods </summary>
        private const BindingFlags reflectionFlagsMethods = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod;
        /// <summary>Flags required for reflection to gets all public properties </summary>
        private const BindingFlags reflectionFlagsProperties = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod;

        /// <summary>
        /// Creates a Manager with different levels of setup for the unit tests to run with.
        /// </summary>
        private Manager createManager(bool withParent, bool withCompiler, bool withCode, bool withAlreadyCompiled)
        {
            Simulations sims = new Simulations();
            ScriptCompiler compiler = new ScriptCompiler();
            Manager testManager = new Manager();

            if (withParent)
                testManager.Parent = sims;

            if (withCompiler)
                testManager.SetCompiler(compiler);

            string basicCode = "";
            basicCode += "using System.Linq;\n";
            basicCode += "using System;\n";
            basicCode += "using Models.Core;\n";
            basicCode += "\n";
            basicCode += "namespace Models\n";
            basicCode += "{\n";
            basicCode += "\t[Serializable]\n";
            basicCode += "\tpublic class Script : Model\n";
            basicCode += "\t{\n";
            basicCode += "\t\t[Description(\"AProperty\")]\n";
            basicCode += "\t\tpublic string AProperty {get; set;} = \"Hello World\";\n";
            basicCode += "\t\tpublic int AMethod()\n";
            basicCode += "\t\t{\n";
            basicCode += "\t\t\treturn 0;\n";
            basicCode += "\t\t}\n";
            basicCode += "\t\t\n";
            basicCode += "\t\tpublic int BMethod(int arg1)\n";
            basicCode += "\t\t{\n";
            basicCode += "\t\t\treturn arg1;\n";
            basicCode += "\t\t}\n";
            basicCode += "\t\t\n";
            basicCode += "\t\tpublic int CMethod(int arg1, int arg2)\n";
            basicCode += "\t\t{\n";
            basicCode += "\t\t\treturn arg1;\n";
            basicCode += "\t\t}\n";
            basicCode += "\t\t\n";
            basicCode += "\t\tpublic int DMethod(int arg1, int arg2, int arg3)\n";
            basicCode += "\t\t{\n";
            basicCode += "\t\t\treturn arg1;\n";
            basicCode += "\t\t}\n";
            basicCode += "\t\t\n";
            basicCode += "\t\tpublic int EMethod(int arg1, int arg2, int arg3, int arg4)\n";
            basicCode += "\t\t{\n";
            basicCode += "\t\t\treturn arg1;\n";
            basicCode += "\t\t}\n";
            basicCode += "\t\t\n";
            basicCode += "\t\tpublic void Document()\n";
            basicCode += "\t\t{\n";
            basicCode += "\t\t\treturn;\n";
            basicCode += "\t\t}\n";
            basicCode += "\t}\n";
            basicCode += "}";

            if (withCode)
                testManager.Code = basicCode;

            if (withAlreadyCompiled)
            {
                if ((!withParent && !withCompiler) || !withCode)
                {
                    throw new Exception("Cannot create test Manager withAlreadyCompiled without a compiler and withGoodCode");
                }
                else
                {
                    testManager.OnCreated();
                    testManager.GetParametersFromScriptModel();
                    testManager.RebuildScriptModel();
                }
            } 
                

            return testManager;
        }


        /// <summary>
        /// This test reproduces a bug in which a simulation could run without
        /// error despite a manager script containing a syntax error.
        /// </summary>
        [Test]
        public void TestManagerWithError()
        {
            var simulations = new Simulations()
            { 
                Children = new List<IModel>()
                {
                    new Simulation()
                    {
                        Name = "Sim",
                        FileName = Path.GetTempFileName(),
                        Children = new List<IModel>()
                        {
                            new Clock()
                            {
                                StartDate = new DateTime(2019, 1, 1),
                                EndDate = new DateTime(2019, 1, 2)
                            },
                            new MockSummary(),
                            new Manager()
                            {
                                Code = "asdf"
                            }
                        }
                    }
                }
            };

            var runner = new Runner(simulations);
            Assert.That(runner.Run(), Is.Not.Null);
        }

        /// <summary>
        /// This test ensures that scripts aren't recompiled after events have
        /// been hooked up. Such behaviour would cause scripts to not receive
        /// any events, and the old/discarded scripts would receive events.
        /// </summary>
        [Test]
        public void TestScriptNotRebuilt()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.bork.apsimx");
            IModel file = FileFormat.ReadFromString<IModel>(json, e => throw e, false).NewModel as IModel;
            Simulation sim = file.FindInScope<Simulation>();
            Assert.DoesNotThrow(() => sim.Run());
        }

        /// <summary>
        /// Ensures that Manager Scripts are allowed to override the
        /// OnCreated() method.
        /// </summary>
        /// <remarks>
        /// OnCreatedError.apsimx contains a manager script which overrides
        /// the OnCreated() method and throws an exception from this method.
        /// 
        /// This test ensures that an exception is thrown and that it is the
        /// correct exception.
        /// 
        /// The manager in this file is disabled, but its OnCreated() method
        /// should still be called.
        /// </remarks>
        [Test]
        public void ManagerScriptOnCreated()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.OnCreatedError.apsimx");
            List<Exception> errors = new List<Exception>();
            IModel sims = FileFormat.ReadFromString<IModel>(json, e => errors.Add(e), false).NewModel;
            Manager manager = sims.FindDescendant<Manager>();
            Assert.Throws<Exception>(() => manager.RebuildScriptModel());
        }

        /// <summary>
        /// Reproduces issue #5202. This appears to be due to a bug where manager script parameters are not being 
        /// correctly overwritten by factors of an experiment (more precisely, they are overwritten, and then the 
        /// overwritten values are themselves being overwritten by the original values).
        /// </summary>
        [Test]
        public void TestManagerOverrides()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Manager.ManagerOverrides.apsimx");
            Simulations sims = FileFormat.ReadFromString<Simulations>(json, e => throw e, false).NewModel as Simulations;

            foreach (Runner.RunTypeEnum runType in Enum.GetValues(typeof(Runner.RunTypeEnum)))
            {
                Runner runner = new Runner(sims);
                List<Exception> errors = runner.Run();
                if (errors != null && errors.Count > 0)
                    throw errors[0];
            }
        }

        /// <summary>
        /// This test ensures one manager model can call another.
        /// </summary>
        [Test]
        public void TestOneManagerCallingAnother()
        {
            var simulations = new Simulations()
            { 
                Children = new List<IModel>()
                {
                    new Simulation()
                    {
                        Children = new List<IModel>()
                        {
                            new Clock() { StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2020, 1, 1)},
                            new MockSummary(),
                            new MockStorage(),
                            new Manager()
                            {
                                Name = "Manager1",
                                Code = "using Models.Core;" + Environment.NewLine +
                                       "using System;" + Environment.NewLine +
                                       "namespace Models" + Environment.NewLine +
                                       "{" + Environment.NewLine +
                                       "    [Serializable]" + Environment.NewLine +
                                       "    public class Script1 : Model" + Environment.NewLine +
                                       "    {" + Environment.NewLine +
                                       "        public int A = 1;" + Environment.NewLine +
                                       "    }" + Environment.NewLine +
                                       "}"
                            },
                            new Manager()
                            {
                                Name = "Manager2",
                                Code = "using Models.Core;" + Environment.NewLine +
                                       "using System;" + Environment.NewLine +
                                       "namespace Models" + Environment.NewLine +
                                       "{" + Environment.NewLine +
                                       "    [Serializable]" + Environment.NewLine +
                                       "    public class Script2 : Model" + Environment.NewLine +
                                       "    {" + Environment.NewLine +
                                       "        [Link] Script1 otherScript;" + Environment.NewLine +
                                       "        public int B { get { return otherScript.A + 1; } }" + Environment.NewLine +
                                       "    }" + Environment.NewLine +
                                       "}"
                            },
                            new Models.Report()
                            {
                                VariableNames = new string[] { "[Script2].B" },
                                EventNames = new string[] { "[Clock].EndOfDay" }
                            }
                        }
                    }
                }
            };
            //Apsim.InitialiseModel(simulations);

            var storage = simulations.Children[0].Children[2] as MockStorage;

            var runner = new Runner(simulations);
            runner.Run();

            double[] actual = storage.Get<double>("[Script2].B");
            double[] expected = new double[] { 2 };
            Assert.That(actual, Is.Not.EqualTo(expected));
        }

        /// <summary>
        /// Make sure the correct manager script is called and accessed when both script are just class 'Script'
        /// See issue #8624 where two zones with different scripts had reflection calling the wrong code.
        /// </summary>
        [Test]
        public void CorrectManagerCalledWhenBothHaveSameClassName()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Manager.ManagerClassNameConflict.apsimx");
            Simulations file = FileFormat.ReadFromString<Simulations>(json, e => throw e, false).NewModel as Simulations;

            // Run the file.
            var Runner = new Runner(file);
            Runner.Run();

            Summary sum = file.FindDescendant<Summary>();
            bool found = false;
            foreach (Message message in sum.GetMessages("Simulation"))
                if (message.Text.Contains("Correct Manager Called"))
                    found = true;

            Assert.That(found, Is.True);
        }

        /// <summary>
        /// The converter will check that it updates correctly, but this will run the file afterwards to make sure all the 
        /// connections still connect.
        /// </summary>
        [Test]
        public void TestMultipleScriptsWithSameClassNameConnectStill()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.CoverterTest172FileBefore.apsimx");
            ConverterReturnType ret = FileFormat.ReadFromString<Simulations>(json, e => {return;}, false);
            Simulations file = ret.NewModel as Simulations;

            var Runner = new Runner(file);
            Runner.Run();

            Assert.Pass();
        }

        /// <summary>
        /// Specific test for SetCompiler and Compiler
        /// These two work together, so should be tested together.
        /// Should not throw when a compiler is attached to a blank manager using these methods
        /// </summary>
        [Test]
        public void SetCompilerAndCompilerTests()
        {
            ScriptCompiler compiler = new ScriptCompiler();
            Manager testManager = new Manager();

            Assert.DoesNotThrow(() => testManager.SetCompiler(compiler));
            Assert.DoesNotThrow(() => typeof(Manager).InvokeMember("Compiler", reflectionFlagsMethods, null, testManager, null));
        }

        /// <summary>
        /// Specific test for TryGetCompiler
        /// Should return false on an empty Manager
        /// Should return true if has an ancestor of Simulations (since it loads a compiler)
        /// Should return true if it has the compiler directly attached
        /// </summary>
        [Test]
        public void TryGetCompilerTests()
        {
            Manager testManager;

            //Should be false if running without a compiler
            testManager = createManager(false, false, true, false);
            Assert.That((bool)typeof(Manager).InvokeMember("TryGetCompiler", reflectionFlagsMethods, null, testManager, null), Is.False);

            //should be found in sims
            testManager = createManager(true, false, true, false);
            Assert.That((bool)typeof(Manager).InvokeMember("TryGetCompiler", reflectionFlagsMethods, null, testManager, null), Is.True);

            //check if works assigning directly.
            testManager = createManager(false, true, true, false);
            testManager.SetCompiler(new ScriptCompiler());
            Assert.That((bool)typeof(Manager).InvokeMember("TryGetCompiler", reflectionFlagsMethods, null, testManager, null), Is.True);
        }

        /// <summary>
        /// Specific test for OnStartOfSimulation
        /// Should do nothing if using empty Manager that has a parent
        /// Should compile and have parameters if setup fully and compiled
        /// Should fail if after compiling, the code is changed to broken code and compiled again
        /// </summary>
        [Test]
        public void OnStartOfSimulationTests()
        {
            Manager testManager;

            //should work
            testManager = createManager(true, false, true, true);
            Assert.DoesNotThrow(() => typeof(Manager).InvokeMember("OnStartOfSimulation", reflectionFlagsMethods, null, testManager, new object[] { new object(), new EventArgs() }));
            Assert.That(testManager.Parameters.Count, Is.EqualTo(1));

            //Should fail, even though previously compiled with code.
            testManager = createManager(true, false, true, true);
            Assert.Throws<Exception>(() => testManager.Code = testManager.Code.Replace('{', 'i'));
            Assert.Throws<TargetInvocationException>(() => typeof(Manager).InvokeMember("OnStartOfSimulation", reflectionFlagsMethods, null, testManager, new object[] { new object(), new EventArgs() }));
        }

        /// <summary>
        /// Specific test for SetParametersInScriptModel
        /// Should not do anything or error on a blank manager
        /// Should make parameters if fully set up
        /// Should not have parameters if compiled but disabled
        /// </summary>
        [Test]
        public void SetParametersInScriptModelTests()
        {
            Manager testManager;

            //Should not throw, but not make parameters
            testManager = createManager(false, false, false, false);
            Assert.DoesNotThrow(() => typeof(Manager).InvokeMember("SetParametersInScriptModel", reflectionFlagsMethods, null, testManager, new object[] { }));
            Assert.That(testManager.Parameters, Is.Null);

            //Should make parameters
            testManager = createManager(false, true, true, true);
            Assert.DoesNotThrow(() => typeof(Manager).InvokeMember("SetParametersInScriptModel", reflectionFlagsMethods, null, testManager, new object[] { }));
            Assert.That(testManager.Parameters.Count, Is.EqualTo(1));

            //Should not make parameters
            testManager = createManager(false, true, true, false);
            testManager.Enabled = false;
            testManager.OnCreated();
            testManager.GetParametersFromScriptModel();
            Assert.DoesNotThrow(() => typeof(Manager).InvokeMember("SetParametersInScriptModel", reflectionFlagsMethods, null, testManager, new object[] { }));
            Assert.That(testManager.Parameters, Is.Null);
        }

        /// <summary>
        /// Specific test for GetParametersFromScriptModel
        /// Should not do anything or error on a blank manager
        /// Should make parameters if script compiled
        /// </summary>
        [Test]
        public void GetParametersFromScriptModelTests()
        {
            Manager testManager;

            //Should not throw, but not make parameters
            testManager = createManager(false, false, false, false);
            Assert.DoesNotThrow(() => testManager.GetParametersFromScriptModel());
            Assert.That(testManager.Parameters, Is.Null);

            //Should make parameters
            testManager = createManager(false, true, true, false);
            testManager.OnCreated();
            Assert.DoesNotThrow(() => testManager.GetParametersFromScriptModel());
            Assert.That(testManager.Parameters.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// Specific test for OnCreated
        /// Should not do anything or error on a blank manager
        /// Should not do anything or error on a manager with no script
        /// Should compile the script and allow parameteres to be made if has compiler and code
        /// </summary>
        [Test]
        public void OnCreatedTests()
        {
            Manager testManager;

            //shouldn't throw, but shouldn't load any script
            testManager = createManager(false, false, false, false);
            Assert.DoesNotThrow(() => testManager.OnCreated());
            Assert.That(testManager.Parameters, Is.Null);

            //shouldn't throw, but shouldn't load any script
            testManager = createManager(true, false, false, false);
            Assert.DoesNotThrow(() => testManager.OnCreated());
            Assert.DoesNotThrow(() => testManager.GetParametersFromScriptModel());
            Assert.That(testManager.Parameters.Count, Is.EqualTo(0));

            //should compile the script
            testManager = createManager(true, false, true, false);
            Assert.DoesNotThrow(() => testManager.OnCreated());
            Assert.DoesNotThrow(() => testManager.GetParametersFromScriptModel());
            Assert.That(testManager.Parameters.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// Specific test for RebuildScriptModel
        /// Tests a bunch of different inputs for scripts to make sure the compile under different setups
        /// </summary>
        [Test]
        public void RebuildScriptModelTests()
        {
            Manager testManager;

            //should compile and have parameters
            testManager = createManager(true, false, true, true);
            Assert.DoesNotThrow(() => testManager.RebuildScriptModel());
            Assert.That(testManager.Parameters.Count, Is.EqualTo(1));

            //should not compile if not enabled
            testManager = createManager(true, false, true, false);
            testManager.Enabled = false;
            testManager.OnCreated();
            Assert.DoesNotThrow(() => testManager.RebuildScriptModel());
            Assert.That(testManager.Parameters, Is.Null);

            //should not compile if not code, but with oncreated run.
            testManager = createManager(true, false, false, false);
            testManager.OnCreated();
            Assert.DoesNotThrow(() => testManager.RebuildScriptModel());
            Assert.That(testManager.Parameters.Count, Is.EqualTo(0));

            //should not compile if code is empty
            testManager = createManager(true, false, false, false);
            testManager.Code = "";
            testManager.OnCreated();
            Assert.DoesNotThrow(() => testManager.RebuildScriptModel());
            Assert.That(testManager.Parameters, Is.Null);

            //should throw error if broken code
            testManager = createManager(true, false, true, false);
            Assert.Throws<Exception>(() => testManager.Code = testManager.Code.Replace("{", ""));
            Assert.Throws<Exception>(() => testManager.OnCreated());
            Assert.Throws<Exception>(() => testManager.RebuildScriptModel());
            Assert.That(testManager.Parameters, Is.Null);
        }

        /// <summary>
        /// Specific test for Document
        /// Document should make a document object with the parameters listed
        /// It should fail if paramters have not been generated, even if code is there.
        /// </summary>
        [Test]
        public void DocumentTests()
        {
            Manager testManager;

            //should work
            testManager = createManager(true, false, true, true);
            List<ITag> tags = new List<ITag>();
            foreach (ITag tag in AutoDocumentation.Document(testManager))
                tags.Add(tag);
            Assert.That(tags.Count, Is.EqualTo(1));

        }

        /// <summary>
        /// Specific test for CodeArray
        /// Put code into the code array property, then pull it out and check that what you have
        /// is the same as what is stored
        /// </summary>
        [Test]
        public void CodeArrayTests()
        {
            Manager testManager;

            testManager = createManager(false, false, true, false);

            string[] array = testManager.CodeArray;
            testManager.CodeArray = array;

            string[] array2 = testManager.CodeArray;
            Assert.That(array2, Is.EqualTo(array));
        }

        /// <summary>
        /// Specific test for Code
        /// Check a range of inputs that could be stored in the code property
        /// Then pull those inputs out and make sure they are the same
        /// </summary>
        [Test]
        public void CodeTests()
        {
            Manager testManager;

            testManager = createManager(false, false, true, false);

            //empty
            testManager = new Manager();
            testManager.Code = "";
            Assert.That(testManager.Code, Is.EqualTo(""));

            //one space
            testManager = new Manager();
            testManager.Code = " ";
            Assert.That(testManager.Code, Is.EqualTo(" "));

            //two lines
            testManager = new Manager();
            testManager.Code = " \n ";
            Assert.That(testManager.Code, Is.EqualTo(" \n "));

            //null - should throw
            testManager = new Manager();
            Assert.Throws<Exception>(() => testManager.Code = null);

            //code in and out
            string code = createManager(false, false, true, false).Code;
            testManager = new Manager();
            testManager.Code = code;
            Assert.That(code, Is.EqualTo(testManager.Code));

            //should remove \r characters
            string codeWithR = code.Replace("\n", "\r\n");
            testManager = new Manager();
            testManager.Code = codeWithR;
            Assert.That(testManager.Code, Is.Not.EqualTo(codeWithR));

            //should compile
            testManager = createManager(true, false, true, false);
            testManager.OnCreated();
            testManager.GetParametersFromScriptModel();
            Assert.That(testManager.Parameters.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// Specific test for Parameters
        /// Check that inputing parameters are stored the same
        /// Check that null clears the parameters
        /// </summary>
        [Test]
        public void ParametersTests()
        {
            Manager testManager;

            List<KeyValuePair<string, string>> paras = new List<KeyValuePair<string, string>>();
            paras.Add(new KeyValuePair<string, string>("AProperty", "Hello World"));

            testManager = new Manager();
            testManager.Parameters = paras;
            Assert.That(testManager.Parameters, Is.EqualTo(paras));

            testManager = new Manager();
            testManager.Parameters = null;
            Assert.That(testManager.Parameters, Is.Null);
        }

        /// <summary>
        /// Specific test for Cursor
        /// Check that the cursor values are stored
        /// Check that it is set to null when nulled
        /// </summary>
        [Test]
        public void CursorTests()
        {
            Manager testManager;

            testManager = new Manager();
            ManagerCursorLocation loc = new ManagerCursorLocation();
            loc.TabIndex = 1;
            testManager.Cursor = loc;
            Assert.That(testManager.Cursor, Is.EqualTo(loc));

            testManager = new Manager();
            testManager.Cursor = null;
            Assert.That(testManager.Cursor, Is.Null);
        }

        /// <summary>
        /// Specific test for GetErrors
        /// Check that it stores errors from a bad script
        /// </summary>
        [Test]
        public void GetErrorsTests()
        {
            Manager testManager = createManager(true, true, true, true);
            Assert.Throws<Exception>(() => testManager.Code = "public class Script : Model {}");
            Assert.Throws<Exception>(() => testManager.OnCreated());
            Assert.Throws<Exception>(() => testManager.RebuildScriptModel());
            Assert.That(testManager.Errors.Split('\n').Length, Is.EqualTo(3));
        }

        /// <summary>
        /// Specific test for GetProperty
        /// Check that we can get values from the script of a manager
        /// </summary>
        [Test]
        public void GetPropertyTests()
        {
            Manager testManager = createManager(true, true, true, true);
            Assert.That(testManager.GetProperty("AProperty"), Is.EqualTo("Hello World"));
            Assert.That(testManager.GetProperty("BProperty"), Is.Null);
        }

        /// <summary>
        /// Specific test for SetProperty
        /// Check that we can change values of the script from a manager
        /// </summary>
        [Test]
        public void SetPropertyTests()
        {
            Manager testManager = createManager(true, true, true, true);
            Assert.DoesNotThrow(() => testManager.SetProperty("AProperty", "Another World"));
            Assert.That(testManager.GetProperty("AProperty"), Is.EqualTo("Another World"));
        }

        /// <summary>
        /// Specific test for RunMethod
        /// Check that we can call and run functions in a script from a manager
        /// </summary>
        [Test]
        public void RunMethodTests()
        {
            Manager testManager = createManager(true, true, true, true);
            Assert.DoesNotThrow(() => testManager.RunMethod("AMethod"));
            Assert.DoesNotThrow(() => testManager.RunMethod("BMethod", 1));
            Assert.DoesNotThrow(() => testManager.RunMethod("CMethod", 1, 1));
            Assert.DoesNotThrow(() => testManager.RunMethod("DMethod", 1, 1, 1));
            Assert.DoesNotThrow(() => testManager.RunMethod("EMethod", 1, 1, 1, 1));
            Assert.DoesNotThrow(() => testManager.RunMethod("CMethod", new object[] {1, 1}));
        }

        /// <summary>
        /// Specific test for RunMethod
        /// Check that we can call and run functions in a script from a manager
        /// </summary>
        [Test]
        public void RunReformatTests()
        {
            Manager testManager = createManager(true, true, true, true);
            string code = testManager.Code;
            testManager.Reformat();
            Assert.That(testManager.Code, Is.EqualTo(code));
        }

        /// <summary>
        /// A test to check that all functions in Manager have been tested by a unit test.
        /// </summary>
        [Test]
        public void MethodsHaveUnitTests()
        {
            //Get list of methods in this test file
            List<MethodInfo> testMethods = ReflectionUtilities.GetAllMethods(typeof(ManagerTests), reflectionFlagsMethods, false);
            string names = "";
            foreach (MethodInfo method in testMethods)
                names += method.Name + "\n";

            //Get lists of methods and properties from Manager
            List<MethodInfo> methods = ReflectionUtilities.GetAllMethodsWithoutProperties(typeof(Manager));
            List<PropertyInfo> properties = ReflectionUtilities.GetAllProperties(typeof(Manager), reflectionFlagsProperties, false);

            //Check that at least one of the methods is named for the method or property
            foreach (MethodInfo method in methods)
                if (names.Contains(method.Name) == false)
                    Assert.Fail($"{method.Name} is not tested by an individual unit test.");

            foreach (PropertyInfo prop in properties)
                if (names.Contains(prop.Name) == false)
                    Assert.Fail($"{prop.Name} is not tested by an individual unit test.");
        }
    }
}
