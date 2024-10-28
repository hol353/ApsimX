using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Models.Core;
using Models.Climate;
using Models.Functions;
using Models.Interfaces;
using Models.PMF.Interfaces;
using Models.PMF.Phen;
using Models.Surface;

namespace Models.PMF.SimplePlantModels
{
    /// <summary>
    /// Data structure that contains the information to set up a specific crop type of SCRUM
    /// </summary>
    /// <remarks>
    /// This includes parameters for the crop/cultivar as well as basic management (planting, harvesting, and residue management)
    /// </remarks>
    [ValidParent(ParentType = typeof(Zone))]
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class ScrumCropInstance : Model
    {
        /// <summary>Connection to the simulation clock.</summary>
        [Link(Type = LinkType.Scoped)]
        private Clock clock = null;

        /// <summary>Connection to the zone this crop is growing in.</summary>
        [Link(Type = LinkType.Ancestor)]
        private Zone myZone = null;

        /// <summary>Connection to the weather data model.</summary>
        [Link]
        Weather weather = null;

        /// <summary>Connection to the SCRUM plant model.</summary>
        [Link(Type = LinkType.Scoped, ByName = true)]
        public Plant scrum = null;

        /// <summary>Connection to the plant phenology model.</summary>
        [Link(Type = LinkType.Scoped, ByName = true)]
        private Phenology phenology = null;

        /// <summary>Connection to the sigmoid function.</summary>
        [Link] private SigmoidFunction sigmoid = null;

        /// <summary>Connection to the surface organic model.</summary>
        [Link(Type = LinkType.Scoped)]
        private SurfaceOrganicMatter surfaceOM = null;

        /// <summary>Connection to the summary model.</summary>
        [Link]
        private ISummary summary = null;

        /// <summary>Connection to the interface with the product 'organ'.</summary>
        [Link(ByName = true)]
        private IHasDamageableBiomass product = null;

        /// <summary>Connection to the interface with the stover 'organ'.</summary>
        [Link(ByName = true)]
        private IHasDamageableBiomass stover = null;

        //-------------------------------------------------------------------------------------------------------------
        // Parameters defining the crop/cultivar to be simulated
        //-------------------------------------------------------------------------------------------------------------

        /// <summary>Name of the crop being simulated.</summary>
        public string CropName { get { return Name; } }

        /// <summary>Harvest index for the crop (proportion of the plant biomass that is product, 0-1).</summary>
        [Separator("Setup to simulate an instance of a crop using SCRUM - Enter values defining the crop in the sections below\n" +
            " Parameters defining growth pattern and biomass partition")]
        [Description(" Harvest Index (0-1):")]
        public double HarvestIndex { get; set; }

        /// <summary>Moisture content of product at harvest (g/g).</summary>
        [Description(" Product moisture content (g/g):")]
        public double MoistureContent { get; set; }

        /// <summary>Proportion of biomass allocated to roots (0-1).</summary>
        [Description(" Root biomass proportion (0-1):")]
        public double Proot { get; set; }

        /// <summary>Root depth at maturity (mm).</summary>
        [Description(" Root depth at maturity (mm):")]
        public double MaxRD { get; set; }

        /// <summary>Crop height at maturity (mm).</summary>
        [Description(" Crop height at maturity (mm):")]
        public double MaxHeight { get; set; }

        /// <summary>Maximum crop green cover (limited between 0 and 0.97).</summary>
        [Description(" Maximum green cover (0-0.97):")]
        public double MaxCover { get; set; }

        /// <summary>Crop extinction coefficient (0-1).</summary>
        [Description(" Crop extinction coefficient (0-1):")]
        public double ExtinctCoeff { get; set; }

        /// <summary>Phenology stage at which the plant is typically harvested.</summary>
        /// <remarks>Used to define the place in the sigmoid curve at which the expected yield occurs.</remarks>
        [Description(" Choose the stage at which the crop is typically harvested:")]
        [Display(Type = DisplayType.ScrumHarvestStages)]
        public string TypicalHarvestStage { get; set; }

        /// <summary>Nitrogen concentration of plant at seedling stage (g/g/).</summary>
        [Separator(" Parameters defining crop nitrogen requirements")]
        [Description(" Nitrogen concentration of plant at seedling stage (g/g):")]
        public double SeedlingNConc { get; set; }

        /// <summary>Nitrogen concentration of product at maturity (g/g).</summary>
        [Description(" Nitrogen concentration of product at harvest (g/g)")]
        public double ProductHarvestNConc { get; set; }

        /// <summary>Nitrogen concentration of stover at maturity (g/g).</summary>
        [Description(" Nitrogen concentration of stover at harvest (g/g).")]
        public double StoverHarvestNConc { get; set; }

        /// <summary>Nitrogen concentration of roots (g/g).</summary>
        [Description(" Nitrogen concentration of roots (g/g):")]
        public double RootNConc { get; set; }

        /// <summary>Proportion of potential N fixation allowed for this crop (used for simulating legumes).</summary>
        [Description(" Proportion of potential N fixation for this crop, if a legume (0-1):")]
        public double LegumePropn { get; set; }

        /// <summary>Base temperature for the crop (oC).</summary>
        [Separator(" Parameters defining crop development as function of temperature")]
        [Description(" Crop base temperature (oC):")]
        public double BaseT { get; set; }

        /// <summary>Optimum temperature for the crop (oC).</summary>
        [Description(" Crop optimum temperature (oC):")]
        public double OptT { get; set; }

        /// <summary>Maximum temperature for the crop (oC).</summary>
        [Description(" Crop maximum temperature (oC):")]
        public double MaxT { get; set; }

        /// <summary>Thermal time required from sowing to emergence (oCd).</summary>
        [Description(" Thermal time required from sowing to emergence (oCd):")]
        public double Tt_SowtoEmerge { get; set; }

        /// <summary>Maximum canopy conductance (typically varies between 0.001 and 0.016 m/s).</summary>
        [Separator(" Parameters defining crop water requirements")]
        [Description(" Maximum canopy conductance (between 0.001 and 0.016 m/s):")]
        public double GSMax { get; set; }

        /// <summary>Net radiation at 50% of maximum conductance (typically varies between 50 and 200 W/m2).</summary>
        [Description(" Net radiation at 50% of maximum conductance (between 50 and 200 W/m^2):")]
        public double R50 { get; set; }

        /// <summary>Flag whether the crop responds to water stress.</summary>
        [Description(" Does the crop respond to water stress?")]
        public bool ConsiderWaterStress { get; set; }

        //-------------------------------------------------------------------------------------------------------------
        // Parameters defining the basic management for this crop, need to be set by a manager
        //-------------------------------------------------------------------------------------------------------------

        /// <summary>Date to establish the crop in the field.</summary>
        [JsonIgnore]
        public Nullable<DateTime> EstablishDate { get; set; }

        /// <summary>Stage at which the crop is established in the field.</summary>
        [JsonIgnore]
        public string EstablishStage { get; set; }

        /// <summary>Depth at which the crop/seeds are planted (mm).</summary>
        [JsonIgnore]
        public double PlantingDepth { get; set; }

        /// <summary>Expected yield for the crop, assumed to be fresh weight (t/ha).</summary>
        /// <remarks>The user is expected to consider genotype, location and growing conditions.</remarks>
        [JsonIgnore]
        public double ExpectedYield { get; set; }

        /// <summary>Date to harvest the crop.</summary>
        [JsonIgnore]
        public Nullable<DateTime> HarvestDate { get; set; }
        private DateTime nonNullHarvestDate;

        /// <summary>Thermal time required from establishment to reach harvest stage (oCd).</summary>
        [JsonIgnore]
        public double TtEstabToHarv { get; set; }

        /// <summary>Stage at which the crop is harvested from the field.</summary>
        [JsonIgnore]
        public string HarvestStage { get; set; }

        /// <summary>Proportion of expected yield that is left in the field at harvest.</summary>
        /// <remarks>This may be due to disease, poor quality, or lack of market.</remarks>
        [JsonIgnore]
        public double FieldLoss { get; set; }

        /// <summary>Proportion of stover that is removed from the field at harvest.</summary>
        /// <remarks>This may be used to mimic bailing or some other means of removal.</remarks>
        [JsonIgnore]
        public double ResidueRemoval { get; set; }

        /// <summary>Proportion of residues that are incorporated in the soil by cultivation at harvest.</summary>
        [JsonIgnore]
        public double ResidueIncorporation { get; set; }

        /// <summary>Depth down to which the residues are incorporated into the soil by cultivation.</summary>
        [JsonIgnore]
        public double ResidueIncorporationDepth { get; set; }

        //-------------------------------------------------------------------------------------------------------------
        // Outputs from this model
        //-------------------------------------------------------------------------------------------------------------

        /// <summary>Thermal time from emergence to maturity (oCd).</summary>
        [JsonIgnore]
        public double Tt_EmergtoMat { get; set; }

        /// <summary>Product biomass removed at harvested.</summary>
        [JsonIgnore]
        public Biomass ProductHarvested { get; private set; }

        /// <summary>Stover biomass removed at harvested.</summary>
        [JsonIgnore]
        public Biomass StoverRemoved { get; private set; }

        /// <summary>Publicises the Nitrogen demand for this crop instance. Occurs when a plant is sown.</summary>
        public event EventHandler<ScrumFertDemandData> SCRUMTotalNDemand;

        /// <summary>Calculates the amount of N required to grow the expected yield.</summary>
        /// <param name="yieldExpected">Fresh yield expected at harvest (t/ha)</param>
        /// <returns>The amount of N required by the crop</returns>
        private double calcTotalNDemand(double yieldExpected)
        {
            double dmc = 1.0 - MoistureContent;
            yieldExpected = yieldExpected * 100.0; // convert to g/m2
            double fDM = yieldExpected * dmc * (1.0 / HarvestIndex) * (1.0 / (1.0 - Proot));
            double productDM = fDM * (1.0 - Proot) * HarvestIndex;
            double stoverDM = fDM * (1.0 - Proot) * (1.0 - HarvestIndex);
            double rootDM = fDM * Proot;
            double productN = productDM * ProductHarvestNConc;
            double stoverN = stoverDM * StoverHarvestNConc;
            double rootN = rootDM * RootNConc;
            double demandKgPerHa = (productN + stoverN + rootN) * 10;
            return demandKgPerHa;
        }

        /// <summary>The cultivar object representing the current instance of the SCRUM crop.</summary>
        private Cultivar currentCrop = null;

        /// <summary>Thermal time from establishment to harvest (oCd).</summary>
        private double ttEstabToHarv { get; set; }

        /// <summary>Names and indices for each predefined crop phenology stage.</summary>
        [JsonIgnore]
        public static Dictionary<string, int> StageNumbers = new Dictionary<string, int>()
        {
            { "Seed", 1 },
            { "Emergence", 2 },
            { "Seedling", 3 },
            { "Vegetative", 4 },
            { "EarlyReproductive", 5 },
            { "MidReproductive", 6 },
            { "LateReproductive", 7 },
            { "Maturity", 8 },
            { "Ripe", 9 }
        };

        /// <summary>Proportion of maximum DM that occurs at each predefined phenology stage.</summary>
        /// <remarks>Computed based on a logistic function.</remarks>
        [JsonIgnore]
        public static Dictionary<string, double> PropnMaxDM = new Dictionary<string, double>()
        {
            { "Seed", 0.004 },
            { "Emergence", 0.0067 },
            { "Seedling", 0.011 },
            { "Vegetative", 0.5 },
            { "EarlyReproductive", 0.7 },
            { "MidReproductive", 0.86 },
            { "LateReproductive", 0.95 },
            {"Maturity", 0.99325 },
            {"Ripe", 0.99965 }
        };

        /// <summary>Proportion of thermal time accumulated at each phenology stage.</summary>
        /// <remarks>Derived from the proportion of DM using a logistic function re-arranged.</remarks>
        [JsonIgnore]
        public static Dictionary<string, double> PropnTt = new Dictionary<string, double>()
        {
            {"Seed", -0.0517 },
            { "Emergence", 0.0001 },
            { "Seedling", 0.0501 },
            { "Vegetative", 0.5 },
            { "EarlyReproductive", 0.5847 },
            { "MidReproductive", 0.6815 },
            { "LateReproductive", 0.7944 },
            { "Maturity", 0.9991 },
            { "Ripe", 1.2957 }
        };

        [JsonIgnore]
        private Dictionary<string, string> scrumParams = new Dictionary<string, string>()
        {
            {"InvertedRelativeDM","[SCRUM].Stover.Photosynthesis.UnStressedBiomass.Integral.Ymax.InvertedRelativeDMAtHarvest.FixedValue = " },
            {"ExpectedYield","[Product].ExpectedYield.FixedValue = "},
            {"HarvestIndex","[Product].HarvestIndex.FixedValue = "},
            {"DryMatterContent","[Product].DryMatterContent.FixedValue = "},
            {"RootProportion","[Root].RootProportion.FixedValue = "},
            {"ProductNConc","[Product].MaxNConcAtMaturity.FixedValue = "},
            {"StoverNConc","[Stover].MaxNConcAtMaturity.FixedValue = "},
            {"RootNConc","[Root].MaximumNConc.FixedValue = "},
            {"SeedlingNConc","[SCRUM].MaxNConcAtSeedling.FixedValue = " },
            {"LegumePropn","[SCRUM].LegumePropn.FixedValue = "},
            {"ExtinctCoeff","[Stover].ExtinctionCoefficient.FixedValue = "},
            {"XoCover","[SCRUM].Stover.Cover.Growth.Expansion.Delta.Integral.Xo.FixedValue = " },
            {"bCover","[SCRUM].Stover.Cover.Growth.Expansion.Delta.Integral.b.FixedValue = " },
            {"ACover","[SCRUM].Stover.Cover.Growth.Expansion.Delta.Integral.Ymax.FixedValue =" },
            {"XoBiomass","[Stover].Photosynthesis.UnStressedBiomass.Integral.Xo.FixedValue = "},
            {"bBiomass","[Stover].Photosynthesis.UnStressedBiomass.Integral.b.FixedValue = " },
            {"MaxHeight","[Stover].HeightFunction.Ymax.FixedValue = "},
            {"XoHig","[Stover].HeightFunction.Xo.FixedValue = " },
            {"bHig","[Stover].HeightFunction.b.FixedValue = " },
            {"MaxRootDepth","[Root].MaximumRootDepth.FixedValue = "},
            {"TtSeed","[Phenology].Seed.Target.FixedValue ="},
            {"TtSeedling","[Phenology].Seedling.Target.FixedValue ="},
            {"TtVegetative","[Phenology].Vegetative.Target.FixedValue ="},
            {"TtEarlyReproductive","[Phenology].EarlyReproductive.Target.FixedValue ="},
            {"TtMidReproductive","[Phenology].MidReproductive.Target.FixedValue ="},
            {"TtLateReproductive","[Phenology].LateReproductive.Target.FixedValue ="},
            {"TtMaturity","[Phenology].Maturity.Target.FixedValue ="},
            {"TtRipe","[Phenology].Ripe.Target.FixedValue ="},
            {"InitialStoverWt","[Stover].InitialWt.FixedValue = "},
            {"InitialProductWt","[Product].InitialWt.Structural.FixedValue = "},
            {"InitialRootWt", "[Root].InitialWt.Structural.FixedValue = " },
            {"InitialCover","[SCRUM].Stover.Cover.InitialCover.FixedValue = " },
            {"BaseT","[Phenology].ThermalTime.XYPairs.X[1] = "},
            {"OptT","[Phenology].ThermalTime.XYPairs.X[2] = " },
            {"MaxT","[Phenology].ThermalTime.XYPairs.X[3] = " },
            {"MaxTt","[Phenology].ThermalTime.XYPairs.Y[2] = "},
            {"GSMax","[SCRUM].Stover.Gsmax350 = " },
            {"R50","[SCRUM].Stover.R50 = " },
            {"WaterStressPhoto","[SCRUM].Stover.Photosynthesis.WaterStressFactor.XYPairs.Y[1] = "},
            {"WaterStressCover","[SCRUM].Stover.Cover.Growth.Expansion.WaterStressFactor.XYPairs.Y[1] = "},
            {"WaterStressNUptake","[SCRUM].Root.NUptakeSWFactor.XYPairs.Y[1] = "},
        };

        /// <summary>Flag whether this crop instance has been established.</summary>
        private bool cropEstablished = false;

        /// <summary>Flag whether this crop instance is being terminated.</summary>
        private bool cropTerminating = false;

        /// <summary>Sets the management for this crop instance.</summary>
        private ScrumManagementInstance setManagementInstance(ScrumManagementInstance management = null)
        {
            if (management != null)
            {
                EstablishDate = management.EstablishDate;
                EstablishStage = management.EstablishStage;
                PlantingDepth = management.PlantingDepth;
                HarvestStage = management.HarvestStage;
                ExpectedYield = management.ExpectedYield;
                HarvestDate = management.HarvestDate;
                TtEstabToHarv = management.TtEstabToHarv;
                FieldLoss = management.FieldLoss;
                ResidueRemoval = management.ResidueRemoval;
                ResidueIncorporation = management.ResidueIncorporation;
                ResidueIncorporationDepth = management.ResidueIncorporationDepth;
            }
            else
            {
                management = new ScrumManagementInstance(
                    cropName: CropName,
                    establishDate: (DateTime)EstablishDate,
                    establishStage: EstablishStage,
                    harvestStage: HarvestStage,
                    expectedYield: ExpectedYield,
                    harvestDate: HarvestDate,
                    ttEstabToHarv: TtEstabToHarv,
                    plantingDepth: PlantingDepth,
                    fieldLoss: FieldLoss,
                    residueRemoval: ResidueRemoval,
                    residueIncorporation: ResidueIncorporation,
                    residueIncorporationDepth: ResidueIncorporationDepth);
            }

            return management;
        }

        /// <summary>Establishes this crop instance (sets SCRUM running).</summary>
        public void Establish(ScrumManagementInstance management)
        {
            management = setManagementInstance(management);
            currentCrop = SetCropCoefficients(management);

            // check some parameters
            if (ExpectedYield == 0.0)
            {
                throw new Exception(Name + " must have an expected yield greater than zero or SCRUM can't simulate it.");
            }

            // compute N requirements
            ScrumFertDemandData fertDemandData = new ScrumFertDemandData(
                name: CropName,
                totalNDemand: management.IsFertilised ? calcTotalNDemand(ExpectedYield) : 0.0,
                establishDate: (DateTime)EstablishDate,
                firstFertdate: (DateTime)management.FirstFertDate,
                harvestDate: (DateTime)HarvestDate,
                tt_EmergtoMat: Tt_EmergtoMat);

            // invoke the SCRUM TotalNDemand event
            if (SCRUMTotalNDemand != null)
            {
                SCRUMTotalNDemand.Invoke(this, fertDemandData);
            }

            // initialise this crop instance in SCRUM
            scrum.Children.Add(currentCrop);
            double cropPopulation = 1.0;
            double rowWidth = 0.0;
            scrum.Sow(cultivar: CropName, population: cropPopulation, depth: PlantingDepth, rowSpacing: rowWidth, maxCover: MaxCover);
            if (management.EstablishStage.ToString() != "Seed")
            {
                phenology.SetToStage(StageNumbers[management.EstablishStage.ToString()]);
            }

            ProductHarvested = new Biomass();
            StoverRemoved = new Biomass();

            cropEstablished = true;
            summary.WriteMessage(this, "Some of the message above is not relevant as SCRUM has no notion of population, bud number or row spacing." +
                " Additional info that may be useful.  " + management.CropName + " is established as " + management.EstablishStage +
                " and will be harvested at " + management.HarvestStage + ". The potential yield is set to " + management.ExpectedYield.ToString() +
                " t/ha, with a moisture content of " + MoistureContent + " g/g and harvest index of " + HarvestIndex.ToString() +
                ". It will be harvested on " + nonNullHarvestDate.ToString("dd-MMM-yyyy") + ", requiring " + ttEstabToHarv.ToString() +
                " oCd from now on.", MessageType.Information);
        }

        /// <summary>Data structure that holds the parameters that define the crop/cultivar being simulated.</summary>
        /// <remarks>These will overwrite those already defined in SCRUM.</remarks>
        public Cultivar SetCropCoefficients(ScrumManagementInstance management)
        {
            Dictionary<string, string> cropParams = new Dictionary<string, string>(scrumParams);

            if (ConsiderWaterStress)
            {
                cropParams["WaterStressPhoto"] += "0.0";
                cropParams["WaterStressCover"] += "0.0";
                cropParams["WaterStressNUptake"] += "0.0";
            }
            else
            {
                cropParams["WaterStressPhoto"] += "1.0";
                cropParams["WaterStressCover"] += "1.0";
                cropParams["WaterStressNUptake"] += "1.0";
            }

            if (MoistureContent > 1.0)
            {
                throw new Exception("Moisture content of " + Name + " ScrumCropInstance has a moisture content > 1.0 g/g.  Value must be less than 1.0");
            }

            double dmc = 1.0 - MoistureContent;
            cropParams["DryMatterContent"] += dmc.ToString();
            double yieldExpected = management.ExpectedYield * 100.0; // convert to g/m2
            cropParams["ExpectedYield"] += yieldExpected.ToString();
            cropParams["HarvestIndex"] += HarvestIndex.ToString();
            cropParams["RootNConc"] += RootNConc.ToString();
            cropParams["SeedlingNConc"] += SeedlingNConc.ToString();
            cropParams["MaxRootDepth"] += MaxRD.ToString();
            cropParams["MaxHeight"] += MaxHeight.ToString();
            cropParams["RootProportion"] += Proot.ToString();
            cropParams["ACover"] += MaxCover.ToString();
            cropParams["ExtinctCoeff"] += ExtinctCoeff.ToString();
            cropParams["LegumePropn"] += LegumePropn.ToString();
            cropParams["GSMax"] += GSMax.ToString();
            cropParams["R50"] += R50.ToString();

            // Derive some crop parameters
            double typicalHarvestStageCode = StageNumbers[TypicalHarvestStage];
            double exponent = Math.Exp(-2.0 * (typicalHarvestStageCode - 3.0));
            cropParams["ProductNConc"] += ((ProductHarvestNConc - SeedlingNConc * exponent) / (1.0 - exponent)).ToString();
            cropParams["StoverNConc"] += ((StoverHarvestNConc - SeedlingNConc * exponent) / (1.0 - exponent)).ToString();

            ttEstabToHarv = 0.0;

            if (double.IsNaN(management.TtEstabToHarv) || (management.TtEstabToHarv == 0))
            {
                ttEstabToHarv = GetThermalTimeSum(management.EstablishDate, (DateTime)management.HarvestDate, BaseT, OptT, MaxT);
            }
            else
            {
                ttEstabToHarv = management.TtEstabToHarv;
            }

            if ((management.HarvestDate == DateTime.MinValue) || (management.HarvestDate == null))
            {
                HarvestDate = GetHarvestDate(management.EstablishDate, ttEstabToHarv, BaseT, OptT, MaxT);
                nonNullHarvestDate = (DateTime)HarvestDate;
            }
            else
            {
                HarvestDate = (DateTime)management.HarvestDate;
            }

            double tt_SowtoEmerg = 0;
            if (management.EstablishStage == "Seed")
            {
                tt_SowtoEmerg = Tt_SowtoEmerge;
            }

            double PropnTt_EstToHarv = PropnTt[management.HarvestStage] - Math.Max(PropnTt[management.EstablishStage], PropnTt["Emergence"]);
            Tt_EmergtoMat = (ttEstabToHarv - tt_SowtoEmerg) * 1 / PropnTt_EstToHarv;

            double Xo_Biomass = Tt_EmergtoMat * 0.5;
            double b_Biomass = Xo_Biomass * 0.2;
            double Xo_cov = Xo_Biomass * 0.4;
            double b_cov = Xo_cov * 0.2;
            double Xo_hig = Xo_Biomass * 0.7;
            double b_hig = Xo_hig * 0.2;

            cropParams["XoBiomass"] += Xo_Biomass.ToString();
            cropParams["bBiomass"] += b_Biomass.ToString();
            cropParams["XoCover"] += Xo_cov.ToString();
            cropParams["bCover"] += b_cov.ToString();
            cropParams["XoHig"] += Xo_hig.ToString();
            cropParams["bHig"] += b_hig.ToString();

            double ttPreEstab = Math.Max(PropnTt[management.EstablishStage], PropnTt["Emergence"]) * Tt_EmergtoMat;
            if (management.EstablishStage != "Seed")
            {
                ttPreEstab += tt_SowtoEmerg;
            }

            double irdm = 1.0 / sigmoid.Function(ttEstabToHarv + ttPreEstab - tt_SowtoEmerg, Xo_Biomass, b_Biomass);
            cropParams["InvertedRelativeDM"] += irdm.ToString();
            cropParams["TtSeed"] += tt_SowtoEmerg;
            cropParams["TtSeedling"] += (Tt_EmergtoMat * (PropnTt["Seedling"] - PropnTt["Emergence"])).ToString();
            cropParams["TtVegetative"] += (Tt_EmergtoMat * (PropnTt["Vegetative"] - PropnTt["Seedling"])).ToString();
            cropParams["TtEarlyReproductive"] += (Tt_EmergtoMat * (PropnTt["EarlyReproductive"] - PropnTt["Vegetative"])).ToString();
            cropParams["TtMidReproductive"] += (Tt_EmergtoMat * (PropnTt["MidReproductive"] - PropnTt["EarlyReproductive"])).ToString();
            cropParams["TtLateReproductive"] += (Tt_EmergtoMat * (PropnTt["LateReproductive"] - PropnTt["MidReproductive"])).ToString();
            cropParams["TtMaturity"] += (Tt_EmergtoMat * (PropnTt["Maturity"] - PropnTt["LateReproductive"])).ToString();
            cropParams["TtRipe"] += (Tt_EmergtoMat * (PropnTt["Ripe"] - PropnTt["Maturity"])).ToString();

            double agDM = yieldExpected * dmc * (1 / HarvestIndex) * irdm;
            double tDM = agDM + (agDM * Proot);
            double iDM = tDM * PropnMaxDM[management.EstablishStage];
            cropParams["InitialStoverWt"] += (iDM * (1 - Proot) * (1 - HarvestIndex)).ToString();
            cropParams["InitialProductWt"] += (iDM * (1 - Proot) * HarvestIndex).ToString();
            cropParams["InitialRootWt"] += Math.Max(0.01, iDM * Proot).ToString(); //Need to have some root mass at start or SCRUM throws an error
            cropParams["InitialCover"] += (MaxCover * 1 / (1 + Math.Exp(-(ttPreEstab - Xo_cov) / b_cov))).ToString();

            cropParams["BaseT"] += BaseT.ToString();
            cropParams["OptT"] += OptT.ToString();
            cropParams["MaxT"] += MaxT.ToString();
            cropParams["MaxTt"] += (OptT - BaseT).ToString();
            string[] commands = new string[cropParams.Count];
            cropParams.Values.CopyTo(commands, 0);

            Cultivar CropValues = new Cultivar(Name, commands);
            return CropValues;
        }

        [EventSubscribe("DoManagement")]
        private void OnDoManagement(object sender, EventArgs e)
        {
            if ((myZone != null) && (clock != null))
            {
                // check whether crop can be established
                if ((clock.Today == EstablishDate) && (cropEstablished == false))
                {
                    ScrumManagementInstance management = setManagementInstance();
                    currentCrop = SetCropCoefficients(management);
                    if (HarvestDate > clock.EndDate)
                    {
                        throw new Exception("Harvest date is beyond the end of the current met file");
                    }

                    Establish(management);
                }

                // check whether the crop was terminated yesterday (do some clean up)
                if (cropTerminating)
                {
                    ProductHarvested.Clear();
                    StoverRemoved.Clear();
                    cropTerminating = false;
                }

                // check whether the crop can be harvested/terminated
                if ((clock.Today == HarvestDate) && (cropEstablished == true))
                {
                    HarvestScrumCrop();
                    EndScrumCrop();

                    // incorporate some of the residue to the soil
                    ScrumManagementInstance management = setManagementInstance();
                    if (management.ResidueIncorporation > 0.0)
                    {
                        surfaceOM.Incorporate(management.ResidueIncorporation, management.ResidueIncorporationDepth);
                    }
                }
            }
        }

        /// <summary>Triggers the removal of biomass from various organs.</summary>
        public void HarvestScrumCrop()
        {
            Biomass initialCropBiomass = (Biomass)myZone.Get("[SCRUM].Product.Total");
            product.RemoveBiomass(liveToRemove: 1.0 - FieldLoss,
                                  deadToRemove: 1.0 - FieldLoss,
                                  liveToResidue: FieldLoss,
                                  deadToResidue: FieldLoss);
            Biomass finalCropBiomass = (Biomass)myZone.Get("[SCRUM].Product.Total");
            ProductHarvested = initialCropBiomass - finalCropBiomass;

            initialCropBiomass = (Biomass)myZone.Get("[SCRUM].Stover.Total");
            stover.RemoveBiomass(liveToRemove: ResidueRemoval,
                                 deadToRemove: ResidueRemoval,
                                 liveToResidue: 1.0 - ResidueRemoval,
                                 deadToResidue: 1.0 - ResidueRemoval);
            finalCropBiomass = (Biomass)myZone.Get("[SCRUM].Stover.Total");
            StoverRemoved = initialCropBiomass - finalCropBiomass;
        }

        /// <summary>Performs some tasks to end this instance of SCRUM.</summary>
        public void EndScrumCrop()
        {
            // end this crop instance
            scrum.EndCrop();

            // remove this crop instance from SCRUM and reset parameters
            scrum.Children.Remove(currentCrop);
            cropEstablished = false;
            cropTerminating = true;
        }

        /// <summary>Calculates the accumulated thermal time between two dates.</summary>
        /// <param name="start">Start Date</param>
        /// <param name="end">End Date</param>
        /// <param name="BaseT">Base temperature</param>
        /// <param name="OptT">Optimal temperature</param>
        /// <param name="MaxT">Maximum temperature</param>
        public double GetThermalTimeSum(DateTime start, DateTime end, double BaseT, double OptT, double MaxT)
        {
            double[] xs = new double[] { BaseT, OptT, MaxT };
            double[] ys = new double[] { 0, OptT - BaseT, 0 };
            XYPairs TtResponse = new XYPairs() { X = xs, Y = ys };

            double TtSum = 0;
            for (DateTime d = start; d <= end; d = d.AddDays(1))
            {
                DailyMetDataFromFile TodaysMetData = weather.GetMetData(d); // Read another line ahead to get tomorrows data
                TtSum += TtResponse.ValueIndexed((TodaysMetData.MinT + TodaysMetData.MaxT) / 2);
            }
            return TtSum;
        }

        /// <summary>Calculates the date at end of period that takes to accumulate a given thermal time.</summary>
        /// <param name="start">Start Date</param>
        /// <param name="HarvTt">Thermal time from establishment to Harvest</param>
        /// <param name="BaseT">Base Temperature</param>
        /// <param name="OptT">Optimum temperature</param>
        /// <param name="MaxT">Maximum Temperature</param>
        public DateTime GetHarvestDate(DateTime start, double HarvTt, double BaseT, double OptT, double MaxT)
        {
            double[] xs = new double[] { BaseT, OptT, MaxT };
            double[] ys = new double[] { 0, OptT - BaseT, 0 };
            XYPairs TtResponse = new XYPairs { X = xs, Y = ys };

            double TtSum = 0;
            DateTime d = start;
            while (TtSum < HarvTt)
            {
                DailyMetDataFromFile TodaysMetData = weather.GetMetData(d); // Read another line ahead to get tomorrows data
                TtSum += TtResponse.ValueIndexed((TodaysMetData.MinT + TodaysMetData.MaxT) / 2);
                d = d.AddDays(1);
            }
            return d;
        }
    }

    /// <summary>
    /// Data structure that contains information for calculating the N demand for a SCRUM instance.
    /// </summary>
    [Serializable]
    public class ScrumFertDemandData : EventArgs
    {
        /// <summary>The name of the crop being simulated.</summary>
        public string crop { get; set; }

        /// <summary>The amount of N required to grow to expected yield (g/m2).</summary>
        public double TotalNDemand { get; set; }

        /// <summary>The duration of the no-fertiliser application window (days).</summary>
        public int NonFertDuration { get; set; }

        /// <summary>The duration of the fertiliser application window (days).</summary>
        public int FertDuration { get; set; }

        /// <summary>The date the crop is to be harvested.</summary>
        public DateTime HarvestDate { get; set; }

        /// <summary>The thermal time required by crop from establishment to maturity (oCd).</summary>
        public double Tt_EmergtoMat { get; set; }

        /// <summary>The constructor</summary>
        public ScrumFertDemandData(string name, double totalNDemand, DateTime establishDate,
                                   DateTime firstFertdate, DateTime harvestDate, double tt_EmergtoMat)
        {
            crop = name;
            TotalNDemand = totalNDemand;
            NonFertDuration = (firstFertdate - establishDate).Days;
            FertDuration = (harvestDate - firstFertdate).Days;
            HarvestDate = harvestDate;
            Tt_EmergtoMat = tt_EmergtoMat;
        }
    }
}
