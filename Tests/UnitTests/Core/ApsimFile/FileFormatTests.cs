namespace UnitTests.Core.ApsimFile
{
    using APSIM.Shared.Utilities;
    using Models;
    using Models.Core;
    using Models.Core.Interfaces;
    using Models.Core.ApsimFile;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Linq;

    /// <summary>
    /// Test the writer's load/save .apsimx capability 
    /// </summary>
    [TestFixture]
    public class FileFormatTests
    {
        /// <summary>Test that a simulation can be written to a string.</summary>
        [Test]
        public void FileFormat_WriteToString()
        {
            // Create some models.
            Simulation sim = new Simulation();
            sim.Children.Add(new Clock()
            {
                Name = "Clock",
                StartDate = new DateTime(2015, 1, 1),
                EndDate = new DateTime(2015, 12, 31)
            });
            sim.Children.Add(new Summary()
            {
                Name = "SummaryFile"
            });
            sim.Children.Add(new Manager()
            {
                Name = "Manager",
                Code = ""
            });

            Simulations simulations = new Simulations();
            simulations.Children.Add(sim);

            string json = FileFormat.WriteToString(simulations);

            string expectedJson = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.FileFormatTestsReadFromString.json");
            Assert.IsTrue(json.Contains("\"$type\": \"Models.Clock, Models\""));
            Assert.IsTrue(json.Contains("\"Start\": \"2015-01-01T00:00:00\""));
            Assert.IsTrue(json.Contains("\"End\": \"2015-12-31T00:00:00\""));
            Assert.IsTrue(json.Contains("\"$type\": \"Models.Summary, Models\""));
            Assert.IsTrue(json.Contains("\"$type\": \"Models.Manager, Models\""));
        }

        /// <summary>Test that a single model can be written to a string. e.g. copy to clipboard.</summary>
        [Test]
        public void FileFormat_WriteSingleModel()
        {
            // Create some models.
            Clock c = new Clock()
            {
                Name = "Clock",
                StartDate = new DateTime(2015, 1, 1),
                EndDate = new DateTime(2015, 12, 31)
            };

            string json = FileFormat.WriteToString(c);

            string expectedJson = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.FileFormatTestsWriteSingleModel.json");
            Assert.AreEqual(json, expectedJson);
        }

        /// <summary>Test that a simulation can be created from a json string.</summary>
        [Test]
        public void FileFormat_ReadFromString()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.FileFormatTestsReadFromString.json");
            List<Exception> creationExceptions;
            var simulations = FileFormat.ReadFromString<Simulations>(json, out creationExceptions);
            Assert.AreEqual(creationExceptions.Count, 0);

            Assert.IsNotNull(simulations);
            Assert.AreEqual(simulations.Children.Count, 1);
            var simulation = simulations.Children[0];
            Assert.AreEqual(simulation.Parent, simulations);
            Assert.AreEqual(simulation.Children.Count, 3);
            Assert.AreEqual(simulation.Children[0].Name, "Clock");
            Assert.AreEqual(simulation.Children[0].Parent, simulation);
            Assert.AreEqual((simulation.Children[0] as Clock).StartDate, new DateTime(2015, 1, 1));
            Assert.AreEqual(simulation.Children[1].Name, "SummaryFile");
            Assert.AreEqual(simulation.Children[1].Parent, simulation);
            Assert.AreEqual(simulation.Children[2].Name, "Manager");
            Assert.AreEqual(simulation.Children[2].Parent, simulation);
        }

        /// <summary>Test that a model can throw during creation and that it is captured.</summary>
        [Test]
        public void FileFormat_CheckThatModelsCanThrowExceptionsDuringCreation()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.FileFormatTestsCheckThatModelsCanThrowExceptionsDuringCreation.json");
            List<Exception> creationExceptions;
            var simulations = FileFormat.ReadFromString<Simulations>(json, out creationExceptions);
            Assert.AreEqual(creationExceptions.Count, 1);
            Assert.IsTrue(creationExceptions[0].Message.Contains("A namespace cannot directly contain members"));

            // Even though the manager model threw an exception we should still have
            // a valid simulation.
            Assert.IsNotNull(simulations);
            Assert.AreEqual(simulations.Children.Count, 1);
            var simulation = simulations.Children[0];
            Assert.AreEqual(simulation.Parent, simulations);
            Assert.AreEqual(simulation.Children.Count, 2);
            Assert.AreEqual(simulation.Children[0].Name, "Clock");
            Assert.AreEqual(simulation.Children[0].Parent, simulation);
            Assert.AreEqual((simulation.Children[0] as Clock).StartDate, new DateTime(2015, 1, 1));
            Assert.AreEqual(simulation.Children[1].Name, "Manager");
            Assert.AreEqual(simulation.Children[1].Parent, simulation);
        }

        /// <summary>Test that models that implement IDontSerialiseChildren don't have the children serialised.</summary>
        [Test]
        public void FileFormat_CheckIOptionallySerialiseChildrenWorks()
        {
            // Create some models.
            Simulation sim = new Simulation();
            sim.Children.Add(new ModelWithIDontSerialiseChildren()
            {
                Name = "ModelImplementingIDontSerialiseChildren",
                Children = new List<IModel>() { new Clock() }
            });

            Simulations simulations = new Simulations();
            simulations.Children.Add(sim);

            string json = FileFormat.WriteToString(simulations);
            Assert.IsFalse(json.Contains("Models.Clock"));
        }

        /// <summary>
        /// This test ensures that exceptions thrown while opening a file cause
        /// the run to be flagged as failed.
        /// </summary>
        [Test]
        public void OnCreatedShouldFailRun()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.OnCreatedError.apsimx");
            string fileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".apsimx");
            File.WriteAllText(fileName, json);

            Assembly models = typeof(IModel).Assembly;
            string modelsExe = AppDomain.CurrentDomain.GetAssemblies().First(a => string.Equals(a.FullName, models.FullName, StringComparison.Ordinal)).Location;

            var proc = new ProcessUtilities.ProcessWithRedirectedOutput();
            proc.Start(modelsExe, fileName, Path.GetTempPath(), true);
            proc.WaitForExit();

            Assert.AreNotEqual(0, proc.ExitCode, "A file ran without error when an exception should have been thrown while opening the file.");
        }

        /// <summary>A class that implements IDontSerialiseChildren.</summary>
        private class ModelWithIDontSerialiseChildren : Model, IOptionallySerialiseChildren
        {
            /// <summary>Allow children to be serialised?</summary>
            public bool DoSerialiseChildren { get { return false; } }
        }

    }
}
