using APSIM.Shared.Utilities;
using Models;
using Models.Climate;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Core.Run;
using Models.Storage;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Crop2ML.Interop;
using Models.Toy;

namespace UnitTests.Core
{
    /// <summary>This is a test class for the Crop2MLInterop class</summary>
    [TestFixture]
    public class Crop2MLInteropTests
    {
        /// <summary>
        /// Test that Crop2MLInterop can get a Crop2ML model description.
        /// </summary>
        [Test]
        public void TestGetModelDescription()
        {
            string binPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string apsimRootPath = Path.Combine(binPath, "..", "..", "..");
            string toyFileName = Path.Combine(apsimRootPath, "Tests", "UnitTests", "Crop2ML.Interop", "Toy1.cs");
            var modelUnit = Crop2MLInterop.GetModelDescription(typeof(Toy1), toyFileName);

            Assert.That(modelUnit.Inputs.Input.Count, Is.EqualTo(3));
            Assert.That(modelUnit.Inputs.Input[0].Name, Is.EqualTo("MinT"));
            Assert.That(modelUnit.Inputs.Input[1].Name, Is.EqualTo("VP"));
            Assert.That(modelUnit.Inputs.Input[2].Name, Is.EqualTo("MaxT"));
            Assert.That(modelUnit.Outputs.Output.Count, Is.EqualTo(1));
            Assert.That(modelUnit.Outputs.Output[0].Name, Is.EqualTo("VPD"));
        }

        /// <summary>
        /// Test that Crop2MLInterop can write a Crop2ML file.
        /// </summary>
        [Test]
        public void TestWriteXml()
        {
            string binPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string apsimRootPath = Path.Combine(binPath, "..", "..", "..");
            string toyFileName = Path.Combine(apsimRootPath, "Tests", "UnitTests", "Crop2ML.Interop", "Toy1.cs");
            string fileName = Path.GetTempFileName();
            Crop2MLInterop.WriteXmlFile(typeof(Toy1), toyFileName, fileName);

            Assert.That(File.Exists(fileName));
            File.Delete(fileName);
         }

        /// <summary>
        /// Test that Crop2MLInterop can write a Crop2ML file.
        /// </summary>
        [Test]
        public void TestWriteXmlForSoilTemperature()
        {
            string binPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string apsimRootPath = Path.Combine(binPath, "..", "..", "..");
            string fileName = Path.Combine(apsimRootPath, "Models", "Soils", "SoilTemp", "SoilTemperature.cs");
            string outputFilePath = Path.GetTempFileName();
            Crop2MLInterop.WriteXmlFile(typeof(Models.Soils.SoilTemp.SoilTemperature), fileName, outputFilePath);
            File.Copy(outputFilePath, Path.ChangeExtension(fileName, "xml"), overwrite: true);

            Assert.That(File.Exists(outputFilePath));
            File.Delete(outputFilePath);
         }
    }
}