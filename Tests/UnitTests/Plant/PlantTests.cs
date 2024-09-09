using APSIM.Shared.Utilities;
using Models;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Soils;
using Models.Storage;
using NUnit.Framework;
using System.Globalization;
using System.IO;
using System.Linq;

namespace UnitTests.Core
{
    [TestFixture]
    public class PlantTests
    {
        /// <summary>
        /// Test that the plant model leaf detachment variable has values during the crop, rather than just when the
        /// crop gets harvested. Issue #3559
        /// </summary>
        /// <param name="fileName"></param>
        [Test]
        public void TestPlantDetached()
        {
            // Open the wheat example.
            string path = Path.Combine("%root%", "Examples", "Wheat.apsimx");
            path = PathUtilities.GetAbsolutePath(path, null);
            Simulations sims = FileFormat.ReadFromFile<Simulations>(path, e => throw e, false).NewModel as Simulations;
            foreach (Soil soil in sims.FindAllDescendants<Soil>())
                soil.Sanitise();
            DataStore storage = sims.FindDescendant<DataStore>();
            storage.UseInMemoryDB = true;
            Simulation sim = sims.FindDescendant<Simulation>();

            // Modify the clock end date so only 1 year of simulation.
            IClock clock = sim.FindDescendant<Clock>();
            clock.EndDate = clock.StartDate.AddYears(1);

            // Add detached variable to report.
            var report = sim.FindDescendant<Models.Report>();
            report.VariableNames = new[]
            {
                "[Clock].Today",
                "[Wheat].Leaf.Detached.Wt"
            };
            report.EventNames = new[]
            {
                "[Clock].EndOfDay"
            };

            // Modify wheat leaf cohort parameters to induce some daily detachment.
            sim.Set("[Field].Wheat.Leaf.CohortParameters.DetachmentLagDuration.FixedValue", 1);
            sim.Set("[Field].Wheat.Leaf.CohortParameters.DetachmentDuration.FixedValue", 1);

            // Run simulation.
            sim.Prepare();
            sim.Run();
            storage.Writer.Stop();
            storage.Reader.Refresh();

            var dataTable = storage.Reader.GetData("Report");

            var data = DataTableUtilities.GetColumnAsDoubles(dataTable, "Wheat.Leaf.Detached.Wt", CultureInfo.InvariantCulture);

            Assert.That(data.Sum(), Is.GreaterThan(0));
        }
    }
}
