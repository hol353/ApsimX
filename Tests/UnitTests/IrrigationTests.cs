namespace UnitTests
{
    using APSIM.Shared.Utilities;
    using Models;
    using Models.Core;
    using Models.Soils;
    using Models.Soils.Nutrients;
    using Models.Surface;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using UnitTests.Soils;

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

            var soilWater = zone.Children[1].Children[4] as Models.WaterModel.WaterBalance;
            var swBeforeIrrigation = MathUtilities.Sum(soilWater.SWmm);
            var irrigation = zone.Children[0] as Irrigation;

            irrigation.Apply(10);
            events.Publish("DoSoilWaterMovement", args);
            var amountSWHasIncreased = MathUtilities.Sum(soilWater.SWmm) - swBeforeIrrigation;

            Assert.AreEqual(amountSWHasIncreased, 10);
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

            var soilWater = zone.Children[1].Children[4] as Models.WaterModel.WaterBalance;
            var swBeforeIrrigation = MathUtilities.Sum(soilWater.SWmm);
            var irrigation = zone.Children[0] as Irrigation;

            irrigation.Apply(10, efficiency:0.5);
            events.Publish("DoSoilWaterMovement", args);
            var amountSWHasIncreased = MathUtilities.Sum(soilWater.SWmm) - swBeforeIrrigation;

            Assert.AreEqual(amountSWHasIncreased, 5);
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
                            new Chemical()
                            {
                                Thickness = new double[] { 100, 300, 300, 300, 300, 300 },
                                CL = new double[] { 38, double.NaN, 500, 490, 500, 500 }
                            },
                            new Sample()
                            {
                                Thickness = new double[] { 100, 300, 300, 300, 300, 300  },
                                SW = new double[] { 0.103, 0.238, 0.253, 0.261, 0.261, 0.261 },
                                NO3 = new double[] { 23, 7, 2, 1, 1, 1 },
                                OC = new double[] { 1.35, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN },
                                SWUnits = Sample.SWUnitsEnum.Gravimetric
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
                                    new MockNutrientPool() { Name = "Inert" },
                                    new MockNutrientPool() { Name = "Microbial" },
                                    new MockNutrientPool() { Name = "Humic" },
                                    new MockNutrientPool() { Name = "FOMCellulose" },
                                    new MockNutrientPool() { Name = "FOMCarbohydrate" },
                                    new MockNutrientPool() { Name = "FOMLignin" },
                                    new MockNutrientPool() { Name = "SurfaceResidue" },
                                    new Solute() { Name = "NO3" },
                                    new Solute() { Name = "NH4" },
                                    new Solute() { Name = "Urea"}
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

            Simulations.InitialiseModel(zone);

            var links = new Links();
            links.Resolve(zone, true);
            var events = new Events(zone);
            events.ConnectEvents();
            return zone;
        }
    }
}
