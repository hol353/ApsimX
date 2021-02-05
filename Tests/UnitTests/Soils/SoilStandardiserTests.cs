namespace UnitTests.Soils
{
    using Models.Core;
    using Models.Soils;
    using Models.Soils.Standardiser;
    using NUnit.Framework;
    using System.Collections.Generic;
    using System.Linq;

    [TestFixture]
    public class StandardiserTests
    {
        /// <summary>Ensure layer mapping works.</summary>
        [Test]
        public void LayerMappingWorks()
        {
            var soil = new Soil
            {
                Children = new List<IModel>()
                {
                    new Physical()
                    {
                        Thickness = new double[] { 100, 300, 300 },
                        BD = new double[] { 1.36, 1.216, 1.24 },
                        AirDry = new double[] { 0.135, 0.214, 0.261 },
                        LL15 = new double[] { 0.27, 0.267, 0.261 },
                        DUL = new double[] { 0.365, 0.461, 0.43 },
                        SAT = new double[] { 0.400, 0.481, 0.45 },

                        Children = new List<IModel>()
                        {
                            new SoilCrop
                            {
                                Name = "Wheat",
                                KL = new double[] { 0.06, 0.060, 0.060 },
                                LL = new double[] { 0.27, 0.267, 0.261 }
                            }
                        }
                    },
                    new Models.WaterModel.WaterBalance(),
                    new CERESSoilTemperature(),
                    new Organic
                    {
                        Thickness = new double[] { 100, 300 },
                        Carbon = new double[] { 2, 1 }
                    },
                    new Chemical
                    {
                        Thickness = new double[] { 100, 200 },
                        NO3N = new double[] { 27, 10 },
                        CL = new double[] { 38, double.NaN }
                    },
                    new Sample
                    {
                        Thickness = new double[] { 500 },
                        SW = new double[] { 0.103 },
                        OC = new double[] { 1.35 },
                        SWUnits = Sample.SWUnitsEnum.Gravimetric
                    },
                    new Sample
                    {
                        Thickness = new double[] { 1000 },
                        OC = new double[] { 1.35 },
                        SWUnits = Sample.SWUnitsEnum.Volumetric
                    }
                }
            };
            Simulations.InitialiseModel(soil);

            SoilStandardiser.Standardise(soil);

            var water = soil.Children[0] as Physical;
            var soilOrganicMatter = soil.Children[3] as Organic;
            var sample = soil.Children[5] as Sample;

            // Make sure layer structures have been standardised.
            var targetThickness = new double[] { 100, 300, 300 };
            Assert.AreEqual(water.Thickness, targetThickness);
            Assert.AreEqual(soilOrganicMatter.Thickness, targetThickness);
            Assert.AreEqual(sample.Thickness, targetThickness);

            // Make sure sample units are volumetric.
            Assert.AreEqual(sample.SWUnits, Sample.SWUnitsEnum.Volumetric);
        }

        /// <summary>Ensure a LayerStructure is used for mapping.</summary>
        [Test]
        public void LayerStructureIsUsedForMapping()
        {
            var soil = new Soil
            {
                Children = new List<IModel>()
                {
                    new Physical()
                    {
                        Thickness = new double[] { 100, 300, 300 },
                        BD = new double[] { 1.36, 1.216, 1.24 },
                        AirDry = new double[] { 0.135, 0.214, 0.261 },
                        LL15 = new double[] { 0.27, 0.267, 0.261 },
                        DUL = new double[] { 0.365, 0.461, 0.43 },
                        SAT = new double[] { 0.400, 0.481, 0.45 },

                        Children = new List<IModel>()
                        {
                            new SoilCrop
                            {
                                Name = "WheatSoil",
                                KL = new double[] { 0.06, 0.060, 0.060 },
                                LL = new double[] { 0.27, 0.267, 0.261 }
                            }
                        }
                    },
                    new Models.WaterModel.WaterBalance(),
                    new CERESSoilTemperature(),
                    new Organic
                    {
                        Thickness = new double[] { 100, 300 },
                        Carbon = new double[] { 2, 1 }
                    },
                    new Chemical
                    {
                        Thickness = new double[] { 100, 200 },
                        NO3N = new double[] { 27, 6 },
                        CL = new double[] { 38, double.NaN }
                    },
                    new Sample
                    {
                        Thickness = new double[] { 500 },
                        SW = new double[] { 0.103 },
                        OC = new double[] { 1.35 },
                        SWUnits = Sample.SWUnitsEnum.Gravimetric
                    },
                    new Sample
                    {
                        Thickness = new double[] { 1000 },
                        OC = new double[] { 1.35 },
                        SWUnits = Sample.SWUnitsEnum.Volumetric
                    },
                    new LayerStructure
                    {
                        Thickness = new double[] { 100, 300 }
                    }
                }
            };
            Simulations.InitialiseModel(soil);

            SoilStandardiser.Standardise(soil);

            var water = soil.Children[0] as Physical;
            var soilOrganicMatter = soil.Children[3] as Organic;
            var sample = soil.Children[5] as Sample;

            // Make sure layer structures have been standardised.
            var targetThickness = new double[] { 100, 300 };
            Assert.AreEqual(water.Thickness, targetThickness);
            Assert.AreEqual(soilOrganicMatter.Thickness, targetThickness);
            Assert.AreEqual(sample.Thickness, targetThickness);

            // Make sure sample units are volumetric.
            Assert.AreEqual(sample.SWUnits, Sample.SWUnitsEnum.Volumetric);
        }

        /// <summary>Ensure a single initial conditions sample is created.</summary>
        [Test]
        public void InitialConditionsIsCreated()
        {
            var soil = new Soil
            {
                Children = new List<IModel>()
                {
                    new Physical()
                    {
                        Thickness = new double[] { 100, 200 },
                        BD = new double[] { 1.36, 1.216 },
                        AirDry = new double[] { 0.135, 0.214 },
                        LL15 = new double[] { 0.27, 0.267 },
                        DUL = new double[] { 0.365, 0.461 },
                        SAT = new double[] { 0.400, 0.481 },
                    },
                    new Models.WaterModel.WaterBalance(),
                    new CERESSoilTemperature(),
                    new Organic
                    {
                        Thickness = new double[] { 100, 200 },
                        Carbon = new double[] { 2, 1 },
                        FBiom = new double[] { 1, 2 }
                    },
                    new Chemical
                    {
                        Thickness = new double[] { 50, 50 },
                        NO3N = new double[] { 27, 16 },
                        NH4N = new double[] { 2, double.NaN },
                        PH = new double[] { 6.8, 6.9 },
                        EC = new double[] { 100, 200 }
                    },
                    new Sample
                    {
                        Thickness = new double[] { 100, 200 },
                        SW = new double[] { 0.1, 0.2 },
                        OC = new double[] { double.NaN, 0.9 },
                        SWUnits = Sample.SWUnitsEnum.Volumetric
                    },
                    new Sample
                    {
                        Thickness = new double[] { 100, 200 },
                        PH = new double[] { 6.4, double.NaN },
                    }
                }
            };
            Simulations.InitialiseModel(soil);

            SoilStandardiser.Standardise(soil);

            var initial = soil.Children[5] as Sample;
            var analysis = soil.Children[4] as Chemical;

            Assert.AreEqual(soil.FindAllChildren<Sample>().Count(), 1);
            Assert.AreEqual(initial.Name, "Initial");
            Assert.AreEqual(initial.SW, new double[] { 0.1, 0.2 } );
            Assert.AreEqual(initial.NO3, new double[] { 29.240000000000002, 2.432 });  // kg/ha
            Assert.AreEqual(initial.NH4, new double[] { 1.4960000000000002, 0.4864 }); // kg/ha
            Assert.AreEqual(initial.OC, new double[] { 2.0, 0.9 });
            Assert.AreEqual(initial.PH, new double[] { 6.4, 6.9 });
            Assert.AreEqual(initial.EC, new double[] { 150, 200 });

            var soilOrganicMatter = soil.Children[3] as Organic;
            Assert.IsNull(soilOrganicMatter.Carbon);

            Assert.NotNull(analysis);
        }
    }
}
