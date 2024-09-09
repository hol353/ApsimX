using APSIM.Shared.Utilities;
using Models;
using Models.Agroforestry;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Soils;
using Models.Soils.Arbitrator;
using Models.Storage;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

namespace UnitTests.Core
{
    [TestFixture]
    public class TreeProxyTests
    {
        /// <summary>
        /// Ensure that the TreeProxy model calculates its NUptake in SetActualNitrogenUptakes method
        /// rather than GetNitrogenUptakeEstimates. Issue #3566
        /// </summary>
        /// <param name="fileName"></param>
        [Test]
        public void TestTreeProxyDoesNUptakeInSetActualNitrogenUptakes()
        {
            // Open the wheat example.
            string path = Path.Combine("%root%", "Examples", "Agroforestry", "Single Tree Example.apsimx");
            path = PathUtilities.GetAbsolutePath(path, null);
            Simulations sims = FileFormat.ReadFromFile<Simulations>(path, e => throw e, false).NewModel as Simulations;
            foreach (Soil soil in sims.FindAllDescendants<Soil>())
                soil.Sanitise();
            DataStore storage = sims.FindDescendant<DataStore>();
            storage.UseInMemoryDB = true;
            Simulation sim = sims.FindDescendant<Simulation>();
            Utilities.ResolveLinks(sim);
            Zone topZone = sim.FindChild<Zone>();

            // Get the clockmodel instance and initialise it.
            var clock = sim.FindDescendant<Clock>();
            clock.StartDate = new System.DateTime(1900, 10, 1);
            Utilities.CallEvent(clock, "SimulationCommencing", null);


            TreeProxy treeProxy = sim.FindDescendant<TreeProxy>();

            // Check temporal data.
            Assert.That(treeProxy.Dates, Is.EqualTo(new DateTime[]
                                                    {
                                                        new(1900, 1, 1),
                                                        new(1900, 3, 1),
                                                        new(1900, 6, 1),
                                                        new(1900, 9, 1),
                                                        new(1900, 12, 31)
                                                    }));
            Assert.That(treeProxy.Heights, Is.EqualTo(new double[] { 1000, 2000, 3000, 4000, 5000 }));
            Assert.That(treeProxy.NDemands, Is.EqualTo(new double[] { 0.1, 0.1, 0.1, 0.1, 0.1 }));
            Assert.That(treeProxy.ShadeModifiers, Is.EqualTo(new double[] { 1, 1, 1, 1, 1 }));

            // Check spatial data.
            Assert.That(treeProxy.Spatial.Shade, Is.EqualTo(new double[] { 60, 50, 40, 30, 20, 0, 0, 0, 0, 0 }));
            Assert.That(treeProxy.Spatial.Rld(0), Is.EqualTo(new double[]   { 6, 5, 4, 2, 1.5, 1, 1 }));
            Assert.That(treeProxy.Spatial.Rld(0.5), Is.EqualTo(new double[] { 6, 5, 4, 2, 1.5, 1, 0  }));
            Assert.That(treeProxy.Spatial.Rld(1), Is.EqualTo(new double[]   { 5, 4, 3.5, 2, 1.5, 1, 0  }));
            Assert.That(treeProxy.Spatial.Rld(1.5), Is.EqualTo(new double[] { 4, 3, 3, 1.5, 1, 1, 0 }));
            Assert.That(treeProxy.Spatial.Rld(2), Is.EqualTo(new double[]   { 3, 2, 2, 1, 0, 0, 0 }));
            Assert.That(treeProxy.Spatial.Rld(2.5), Is.EqualTo(new double[] { 2, 1, 1, 0, 0, 0, 0 }));
            Assert.That(treeProxy.Spatial.Rld(3), Is.EqualTo(new double[]   { 1, 0.5, 0.2, 0, 0, 0, 0 }));
            Assert.That(treeProxy.Spatial.Rld(4), Is.EqualTo(new double[]   { 0, 0, 0, 0, 0, 0, 0 }));
            Assert.That(treeProxy.Spatial.Rld(5), Is.EqualTo(new double[]   { 0, 0, 0, 0, 0, 0, 0 }));
            Assert.That(treeProxy.Spatial.Rld(6), Is.EqualTo(new double[]   { 0, 0, 0, 0, 0, 0, 0 }));

            // Get the tree proxy model instance and initialise it.1
            Utilities.CallEvent(treeProxy, "SimulationCommencing", null);

            SoilState soilState = new(topZone.FindAllChildren<Zone>().Take(1));
            soilState.Zones[0].Water = new double[] { 0.3, 0.3, 0.3 };
            soilState.Zones[0].NO3N = new double[] { 1, 1, 1 };
            treeProxy.GetNitrogenUptakeEstimates(soilState);

            // Make sure NUptake wasn't set.
            Assert.That(treeProxy.NUptake, Is.Null);

            // Once SetActualNitrogenUptakes is called, NUptake should be set.
            treeProxy.SetActualNitrogenUptakes(soilState.Zones);
            Assert.That(treeProxy.NUptake, Is.Not.Null);
        }
    }
}