using APSIM.Shared.Utilities;
using Models;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Soils;
using Models.Soils.Nutrients;
using Models.Surface;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnitTests.Soils;

namespace UnitTests
{
    public class IrrigationTests
    {
        [Test]
        public void IrrigationWorks()
        {
            Zone zone = CreateZoneWithSoil();

            var events = new Events(zone);
            var args = new object[] { this, new EventArgs() };
            events.Publish("Commencing", args);
            events.Publish("StartOfSimulation", args);
            events.Publish("DoDailyInitialisation", args);

            var soilWater = zone.Children[1].Children[7] as Models.WaterModel.WaterBalance;
            var swBeforeIrrigation = MathUtilities.Sum(soilWater.SWmm);
            var irrigation = zone.Children[0] as Irrigation;

            irrigation.Apply(10);
            events.Publish("DoSoilWaterMovement", args);
            var amountSWHasIncreased = MathUtilities.Sum(soilWater.SWmm) - swBeforeIrrigation;

            Assert.That(amountSWHasIncreased, Is.EqualTo(10));
        }

        [Test]
        public void IrrigationEfficiencyWorks()
        {
            Zone zone = CreateZoneWithSoil();

            var events = new Events(zone);
            var args = new object[] { this, new EventArgs() };
            events.Publish("Commencing", args);
            events.Publish("StartOfSimulation", args);
            events.Publish("DoDailyInitialisation", args);

            var soilWater = zone.Children[1].Children[7] as Models.WaterModel.WaterBalance;
            var swBeforeIrrigation = MathUtilities.Sum(soilWater.SWmm);
            var irrigation = zone.Children[0] as Irrigation;

            irrigation.Apply(10, efficiency:0.5);
            events.Publish("DoSoilWaterMovement", args);
            var amountSWHasIncreased = MathUtilities.Sum(soilWater.SWmm) - swBeforeIrrigation;

            Assert.That(amountSWHasIncreased, Is.EqualTo(5));
        }


        private static Zone CreateZoneWithSoil()
        {
            var zone = new Zone()
            {
                Area = 1,
                Children = new List<IModel>()
                {
                    new Irrigation(),
                    new Soil()
                    {
                        Children = new List<IModel>()
                        {
                            new Physical()
                            {
                                Thickness = new double[] { 100, 300, 300, 300, 300, 300 },
                                BD = new double[] { 1.36, 1.216, 1.24, 1.32, 1.372, 1.368 },
                                AirDry = new double[] { 0.135, 0.214, 0.253, 0.261, 0.261, 0.261 },
                                LL15 = new double[] { 0.27, 0.267, 0.261, 0.261, 0.261, 0.261 },
                                DUL = new double[] { 0.365, 0.461, 0.43, 0.412, 0.402, 0.404 },
                                SAT = new double[] { 0.400, 0.481, 0.45, 0.432, 0.422, 0.424 },
                                Children = new List<IModel>()
                                {
                                    new SoilCrop()
                                    {
                                        Name = "WheatSoil",
                                        KL = new double[] { 0.06, 0.060, 0.060, 0.060, 0.060, 0.060 },
                                        LL = new double[] { 0.27, 0.267, 0.261, 0.315, 0.402, 0.402 }
                                    }
                                }
                            },
                            new Organic()
                            {
                                Thickness = new double[] { 100, 300, 300, 300, 300, 300 },
                                Carbon = new double[] { 2, 1, 0.5, 0.4, 0.3, 0.2 }
                            },
                            new Solute
                            {
                                Name = "CL",
                                Thickness = new double[] { 100, 300, 300, 300, 300, 300 },
                                InitialValues = new double[] { 38, double.NaN, 500, 490, 500, 500 },
                                InitialValuesUnits = Solute.UnitsEnum.ppm
                            },
                            new Solute
                            {
                                Name = "NO3",
                                Thickness = new double[] { 100, 300, 300, 300, 300, 300 },
                                InitialValues = new double[] { 23, 7, 2, 1, 1, 1 },
                                InitialValuesUnits = Solute.UnitsEnum.kgha
                            },
                            new Solute
                            {
                                Name = "NH4",
                                Thickness = new double[] { 100, 300, 300, 300, 300, 300 },
                                InitialValues = new double[] { 23, 7, 2, 1, 1, 1 },
                                InitialValuesUnits = Solute.UnitsEnum.kgha
                            },
                            new Solute
                            {
                                Name = "Urea",
                                Thickness = new double[] { 100, 300, 300, 300, 300, 300 },
                                InitialValues = new double[] { 0, 0, 0, 0, 0, 0 },
                                InitialValuesUnits = Solute.UnitsEnum.kgha
                            },
                            new Water()
                            {
                                Thickness = new double[] { 100, 300, 300, 300, 300, 300  },
                                InitialValues = new double[] { 0.103, 0.238, 0.253, 0.261, 0.261, 0.261 },
                            },
                            new Models.WaterModel.WaterBalance()
                            {
                                Thickness = new double[] { 100, 300, 300, 300, 300, 300  },
                                SummerCona = 6,
                                WinterCona = 3,
                                SummerU = 6,
                                WinterU = 3,
                                CN2Bare = 60,
                                CNCov = 10,
                                CNRed = 10,
                                DiffusConst = 10,
                                DiffusSlope = 10,
                                Salb = 10,
                                ResourceName = "WaterBalance"
                            },
                            new Nutrient()
                            {
                                Children = new List<IModel>()
                                {
                                    new OrganicPool() { Name = "Inert" },
                                    new OrganicPool() { Name = "Microbial" },
                                    new OrganicPool() { Name = "Humic" },
                                    new OrganicPool() { Name = "FOMCellulose" },
                                    new OrganicPool() { Name = "FOMCarbohydrate" },
                                    new OrganicPool() { Name = "FOMLignin" },
                                    new OrganicPool() { Name = "SurfaceResidue" },
                                    new NFlow() { Name = "Hydrolysis" },
                                    new NFlow() { Name = "Denitrification" },
                                    new NFlow() { Name = "Nitrification" },
                                }
                            },
                            new MockSoilTemperature(),
                        }
                    },
                    new MockClock()
                    {
                        Today = new DateTime(2019, 1, 1),
                    },
                    new Weather.MockWeather()
                    {
                        Latitude = 10.2,
                        MaxT = 30,
                        MinT = 15,
                        Radn = 20
                    },
                    new MockSummary(),
                    new SurfaceOrganicMatter()
                    {
                        ResourceName = "SurfaceOrganicMatter",
                        InitialResidueName = "Wheat",
                        InitialResidueType = "Wheat",
                    }
                }
            };
            Resource.Instance.Replace(zone);
            FileFormat.InitialiseModel(zone, (e) => throw e);

            zone.ParentAllDescendants();
            foreach (IModel model in zone.FindAllDescendants())
                model.OnCreated();
            var links = new Links();
            links.Resolve(zone, true);
            var events = new Events(zone);
            events.ConnectEvents();

            var soil = zone.Children[1] as Soil;
            soil.Sanitise();

            return zone;
        }
    }
}
