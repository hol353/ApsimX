using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using APSIM.Shared.Utilities;
using Models;
using Models.Core;
using Models.Core.ApsimFile;
using Models.PMF;
using Models.Soils;
using Models.Storage;
using NUnit.Framework;
using UnitTests.Storage;

namespace UnitTests.Core
{
    /// <summary>
    /// Tests to ensure that a simulation's state after a simulation
    /// is identical to its state before a simulation is run.
    /// </summary>
    [TestFixture]
    public class SimulationResetTests
    {
        private class Logger : Model
        {
            [Link] private IClock clock = null;
            [Link(Type = LinkType.Ancestor)] private Simulation sim = null;
            public string Json { get; set; }
            public bool ExitAfterLogging { get; set; }

            [EventSubscribe("EndOfDay")]
            private void Log(object sender, EventArgs args)
            {
                if (clock.Today == clock.StartDate)
                {
                    // Always set ExitAfterLogging and json to null before loggin, to ensure
                    // they don't get in the way.
                    bool exitEarly = ExitAfterLogging;
                    ExitAfterLogging = false;
                    Json = null;
                    Json = ReflectionUtilities.JsonSerialise(sim, true);
                    ExitAfterLogging = exitEarly;
                    if (ExitAfterLogging)
                        clock.EndDate = clock.Today;
                }
            }
        }

        [Parallelizable]
        //[TestCase("AgPasture.apsimx")]
        //[TestCase("Barley.apsimx")]
        //[TestCase("Chicory.apsimx")]
        //[TestCase("Eucalyptus.apsimx")]
        //[TestCase("FodderBeet.apsimx")]
        //[TestCase("Maize.apsimx")]
        //[TestCase("Oats.apsimx")]
        [TestCase("OilPalm.apsimx")]
        //[TestCase("PlantainForage.apsimx")]
        //[TestCase("Potato.apsimx")]
        //[TestCase("RedClover.apsimx")]
        //[TestCase("Rotation.apsimx")]
        //[TestCase("SCRUM.apsimx")]
        //[TestCase("SimpleGrazing.apsimx")]
        //[TestCase("Soybean.apsimx")]
        //[TestCase("Stock.apsimx")]
        //[TestCase("Sugarcane.apsimx")]
        //[TestCase("Wheat.apsimx")]
        //[TestCase("WhiteClover.apsimx")]
        public void TestSimulation(string fileName)
        {
            Simulation sim = CreateSimulation(Path.Combine("%root%", "Examples", fileName));
            Logger logger = new Logger();
            logger.Parent = sim;
            sim.Children.Add(logger);
            sim.Prepare();

            // 2. Run simulation.
            sim.Run();

            // 3. Compare simulation state now vs before running
            string pre = logger.Json;

            // Run a second time.
            logger.ExitAfterLogging = true;
            sim.Run();

            string post = logger.Json;

            // Easiest way to debug this test is to uncomment these two lines
            // and open the two json files in a diff tool.
            // File.WriteAllText(Path.Combine(Path.GetTempPath(), $"pre-{Guid.NewGuid().ToString()}.json"), pre);
            // File.WriteAllText(Path.Combine(Path.GetTempPath(), $"post-{Guid.NewGuid().ToString()}.json"), post);

            Assert.That(post, Is.EqualTo(pre), $"{Path.GetFileName(sim.FileName)} simulation failed to zero all variables");
        }

        private static Simulation CreateSimulation(string path)
        {
            path = PathUtilities.GetAbsolutePath(path, null);
            Simulations sims = FileFormat.ReadFromFile<Simulations>(path, e => throw e, false).NewModel as Simulations;
            foreach (Soil soil in sims.FindAllDescendants<Soil>())
                soil.Sanitise();
            DataStore storage = sims.FindDescendant<DataStore>();
            storage.UseInMemoryDB = true;
            IClock clock = sims.FindDescendant<Clock>();
            clock.EndDate = clock.StartDate.AddYears(1);
            return sims.FindDescendant<Simulation>();
        }
    }
}
