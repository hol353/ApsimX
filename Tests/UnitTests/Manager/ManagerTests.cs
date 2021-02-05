using APSIM.Shared.Utilities;
using Models;
using Models.Core;
using NUnit.Framework;
using System;
namespace UnitTests.ManagerTests
{
    using Models.Core.ApsimFile;
    using Models.Core.Run;
    using Models.Core.Script;
    using System.Collections.Generic;
    using System.IO;
    using UnitTests.Storage;

    /// <summary>
    /// Unit Tests for manager scripts.
    /// </summary>
    class ManagerTests
    {
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
            Assert.IsNotNull(runner.Run());
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
            IModel file = FileFormat.ReadFromString<IModel>(json, out List<Exception> errors);
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
            FileFormat.ReadFromString<IModel>(json, out List<Exception> errors);
            Assert.NotNull(errors);
            Assert.AreEqual(1, errors.Count, "Encountered the wrong number of errors when opening OnCreatedError.apsimx.");
            Assert.That(errors[0].ToString().Contains("Error thrown from manager script's OnCreated()"), "Encountered an error while opening OnCreatedError.apsimx, but it appears to be the wrong error: {0}.", errors[0].ToString());
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
            Simulations sims = FileFormat.ReadFromString<Simulations>(json, out List<Exception> errors);
            if (errors != null && errors.Count > 0)
                throw errors[0];

            Runner runner = new Runner(sims);
            errors = runner.Run();
            if (errors != null && errors.Count > 0)
                throw errors[0];
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
            Assert.AreNotEqual(expected, actual);
        }
    }
}
