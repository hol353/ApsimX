
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Grazplan;
using Models.Interfaces;
using Models.Soils;
using Models.Soils.Arbitrator;
using Models.Soils.Nutrients;
using Models.Surface;
using Newtonsoft.Json;
using StdUnits;
using System;
using System.Collections.Generic;
using System.Linq;
using static Models.GrazPlan.GrazType;
using static Models.GrazPlan.PastureUtil;

namespace Models.GrazPlan
{
    /// <summary>
    /// The soil interface for the Pasture model
    /// </summary>
    [Serializable]
    public class TSoilInstance : Core.Model
    {
        /// <summary>Layer profile used by this component</summary>
        protected int FNoLayers;

        /// <summary></summary>
        protected double[] FLayerProfile;               // [1..  [0] is unused

        /// <summary>
        /// Set the layer count and thicknesses
        /// </summary>
        /// <param name="profile"></param>
        protected void SetLayerProfile(double[] profile)
        {
            FNoLayers = profile.Length;
            FLayerProfile = new double[FNoLayers + 1];
            Value2LayerArray(profile, ref FLayerProfile);
        }

        /// <summary>
        /// Fill a layer array where [0] is unused
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="LayerA"></param>
        protected void Value2LayerArray(double[] profile, ref double[] LayerA)
        {
            if (profile != null)
            {
                PastureUtil.FillArray(LayerA, 0.0);
                for (uint Ldx = 1; Ldx <= profile.Length; Ldx++)
                {
                    LayerA[Ldx] = profile[Ldx - 1];
                }
            }
        }
    }

    // ========================================================================

    /// <summary>
    /// # Pasture class that models temperate Australian pastures.
    /// Encapsulates the GRAZPLAN pasture model
    /// </summary>
    [Serializable]
    [ScopedModel]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Zone))]
    public class Pasture : TSoilInstance, IUptake, ICanopy
    {
        private TPasturePopulation PastureModel;
        private TWeatherHandler FWeather;
        private TPastureInputs FInputs;

        private double[] F_BulkDensity = new double[GrazType.MaxSoilLayers + 1];    // [1..
        private double[] F_SandPropn = new double[GrazType.MaxSoilLayers + 1];      // [1..
        private double[] F_DUL = new double[GrazType.MaxSoilLayers + 1];            // [1..
        private double[] F_LL15 = new double[GrazType.MaxSoilLayers + 1];           // [1..

        private double FFieldGreenDM;
        private double FFieldGAI;
        private double FFieldDAI;
        private double FFieldCoverSum;

        private double FFieldArea;
        private int FToday;             // StdDate.Date
        private double[][] FSoilPropn = new double[GrazType.stSENC + 1][];      // [stSEEDL..stSENC] - [1..3][1..]
        private double[][] FTranspiration = new double[GrazType.stSENC + 1][];  // [TOTAL..stSENC]   - [0..3][1..]

        private double FIntercepted = 0.0;  // Precipitation intercepted by herbage.  Default is 0.0

        /// <summary>kg/ha</summary>
        private double[] mySoilNH4Available;    // [0..
        /// <summary>kg/ha</summary>
        private double[] mySoilNO3Available;
        private double[] mySoilWaterAvailable;
        private double[] myTotalWater;

        #region Class links
        /// <summary>
        /// The simulation clock
        /// </summary>
        [Link]
        private Clock systemClock = null;

        /// <summary>
        /// The simulation weather component
        /// </summary>
        [Link]
        private IWeather locWtr = null;

        /// <summary>Link to the soil physical properties.</summary>
        [Link]
        private IPhysical soilPhysical = null;

        [Link]
        private Water water = null;

        [Link]
        private Chemical soilChemical = null;

        [Link(IsOptional = true)]
        private Stock stockModel = null;

        /// <summary>Soil-plant parameterisation.</summary>
        private Models.Soils.SoilCrop soilCropData;


        /// <summary>The surface organic matter model.</summary>
        [Link]
        private SurfaceOrganicMatter surfaceOrganicMatter = null;

        /// <summary>Link to the zone this pasture species resides in.</summary>
        [Link]
        private Zone zone = null;

        /// <summary>
        /// The supplement component
        /// </summary>
        [Link(IsOptional = true)]
        private Supplement suppFeed = null;

        /*/// <summary>Link to APSIM summary (logs the messages raised during model run).</summary>
        [Link]
        private ISummary outputSummary = null; */

        ///// <summary>Link to micro climate (aboveground resource arbitrator).</summary>
        //[Link]
        //private MicroClimate microClimate = null;

        /// <summary>Soil object where these roots are growing.</summary>
        private Models.Soils.Soil soil = null;

        /// <summary>Water balance model.</summary>
        private ISoilWater waterBalance = null;

        /// <summary>Soil nutrient model.</summary>
        private INutrient nutrient;

        /// <summary>NO3 solute in the soil.</summary>
        private ISolute no3 = null;

        /// <summary>NH4 solute in the soil.</summary>
        private ISolute nh4 = null;

        #endregion


        /// <summary>
        /// The Pasture class constructor
        /// </summary>
        public Pasture() : base()
        {
            // inputs to the pasture model
            FInputs = new TPastureInputs();
            FInputs.CO2_PPM = GrazEnv.REFERENCE_CO2;
            FInputs.Windspeed = 2.0;

            FFieldArea = 1.0;
        }

        #region Initialisation properties ====================================================

        /// <summary>
        /// Name of the pasture species for which parameters are to be used
        /// </summary>
        [Description("Species")]
        public string Species { get; set; } = "Perennial Ryegrass";

        /// <summary>
        /// Determines which of the plant nutrient submodels to activate.
        /// Feasible values are:
        ///
        /// '' - 'Simple' nutrient mode,
        /// 'N' - Nitrogen submodel only,
        /// 'NP' - Nitrogen and phosphorus submodels,
        /// 'NS' - Nitrogen and sulphur submodels,
        /// 'NPS' - All three plant nutrients.
        /// </summary>
        public string Nutrients { get; set; } = "N";

        /// <summary>
        /// Depth of each soil layer referenced in specifying root and seed pools.
        /// Must be given if soil profiles for root or seed pools are given,
        /// otherwise the profile depths will be requested (as layers) from the rest of the simulation.
        /// mm
        /// </summary>
        public double[] Layers { get; set; }

        /// <summary>
        /// Maximum rooting depth.
        /// The default value is calculated from soil bulk density and sand content
        /// mm
        /// </summary>
        [Description("Maximum rooting depth (mm)")]
        public double MaxRtDep { get; set; } = 650;

        /// <summary>
        /// Lagged daytime temperature.
        /// Default value is -999.9, which denotes that the value of daytime
        /// temperature in the first time step should be used
        /// oC
        /// </summary>
        public double LaggedDayT { get; set; } = -999.9;

        /// <summary>
        /// Value denoting the phenological stage of the species.
        /// (0-1) Vernalizing.
        /// (1-2) Vegetative.
        /// (2-3) Reproductive.
        /// (3-4) Summer-dormant perennials.
        /// (4.0) Senescent annuals.
        /// (5-6) Spray-topped.
        /// (6.0) Winter-dormant perennials. See Help for more info.
        /// </summary>
        [Description("Value denoting the phenological stage of the species")]
        public double Phenology { get; set; } = 1.0015;

        /// <summary>
        /// Current maximum length of the flowering period.
        /// Ignored if the species is modelled with no seed pools or the phenological stage is not reproductive.
        /// Default depends on the phenological stage;
        /// d
        /// </summary>
        public double FlowerLen { get; set; }

        /// <summary>
        /// Time since the start of flowering.
        /// Default depends on the phenological stage.
        /// d
        /// </summary>
        public double FlowerTime { get; set; }

        /// <summary>
        /// Number of preceding days of 'dry' conditions.
        /// Only meaningful if the pasture population is vulnerable to senescence. Default value is 0
        /// d
        /// </summary>
        public double SencIndex { get; set; } = 0;

        /// <summary>
        /// Number of preceding days of 'cool', moist conditions.
        /// Only meaningful if the pasture population is summer-dormant. Default value is 0
        /// d
        /// </summary>
        public int DormIndex { get; set; } = 0;

        /// <summary>
        /// Lagged mean temperature used in summer-dormancy calculations.
        /// Default value is -999.9, which denotes that the value of mean temperature in the first time step should be used.
        /// oC
        /// </summary>
        public double DormT { get; set; } = -999.9;

        /// <summary>
        /// Apparent extinction coefficients of seedlings, established plants and senescing plants.
        /// </summary>
        [Description("Apparent extinction coefficients of seedlings, established plants and senescing plants")]
        public double[] ExtinctCoeff { get; set; } = new double[] { 0.0, 0.0, 0.55 };

        /*
        These rules apply when providing values for GreenInit[].herbage or DryInit[].herbage:
	    The herbage field must have zero, one or two elements.
        If there is one element, it denotes the total (shoot) pool; if there are two elements, they denote leaf and stem.

        If the dmd sub-field has more than one element, then the weight sub-field must have one fewer elements.
        weight[1] denotes the mass of tissue with DMD in the range from dmd[1] to dmd[2], and so on.

        If a single value (i.e. an array of length 1) is provided for the dmd sub-field,
        it denotes the average DMD of all shoot/leaf/stem (depending on context).
        In this case, the corresponding weight sub-field must have a single element,
        which denotes the total weight of shoot/leaf/stem.

        The lengths of the n_conc, p_conc and/or s_conc sub-fields must be either the same as the weight sub-field, one or zero.
        If the length is same as the weight field, each value gives the nutrient concentration of the corresponding DMD class.
        If the length is one, the value denotes the average nutrient concentration.
        If the array is empty, a species-specific set of default nutrient concentrations is used.
        */

        /// <summary>
        /// Each element specifies the state of a cohort of green (living) herbage
        /// </summary>
        public GreenInit[] Green { get; set; }

        /// <summary>
        /// Each element specifies the state of a cohort of dry herbage (standing dead or litter)
        /// </summary>
        public DryInit[] Dry { get; set; }

        /// <summary>
        /// Mass of seeds in each soil layer [0..
        /// </summary>
        public SeedInit Seeds { get; set; }

        /// <summary>
        /// Get the seeds of the specified type over the layers
        /// </summary>
        /// <param name="soft">soft/hard</param>
        /// <param name="ripe">unripe/ripe</param>
        /// <returns>An array of seed amounts for each layer. kg/ha</returns>
        private double[] seedLayers(int soft, int ripe)
        {
            double[] result = null;
            if (PastureModel != null)
            {
                result = new double[FNoLayers];
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";

                // fills the zero based array
                for (int i = 0; i < FNoLayers; i++)
                {
                    result[i] = PastureModel.GetSeedMass(soft, ripe, i + 1);
                }

                PastureModel.MassUnit = sUnit;
            }

            return result;
        }

        /// <summary>
        /// Soft unripe seed
        /// </summary>
        [Units("kg/ha")]
        public double[] SeedSoftUnripe
        {
            get
            {
                return seedLayers(GrazType.SOFT, GrazType.UNRIPE);
            }
        }

        /// <summary>
        /// Hard unripe seed
        /// </summary>
        [Units("kg/ha")]
        public double[] SeedHardUnripe
        {
            get
            {
                return seedLayers(GrazType.HARD, GrazType.UNRIPE);
            }
        }

        /// <summary>
        /// Hard ripe seed
        /// </summary>
        [Units("kg/ha")]
        public double[] SeedHardRipe
        {
            get
            {
                return seedLayers(GrazType.HARD, GrazType.RIPE);
            }
        }

        /// <summary>
        /// Soft ripe seed
        /// </summary>
        [Units("kg/ha")]
        public double[] SeedSoftRipe
        {
            get
            {
                return seedLayers(GrazType.SOFT, GrazType.RIPE);
            }
        }

        /// <summary>
        /// Time since commencement of embryo dormancy.
        /// Only meaningful if unripe seeds are present. Default is 0.0
        /// d
        /// </summary>
        [Units("d")]
        public double SeedDormTime { get; set; } = 0;

        /// <summary>
        /// Germination index.
        /// Only meaningful if the species is modelled with seed pools. Default is 0.0
        /// d
        /// </summary>
        [Units("d")]
        public double GermIndex { get; set; } = 0;

        /// <summary>
        /// Rate parameter for the optional Monteith water uptake sub-model
        /// /d
        /// </summary>
        public double[] KL { get; set; } // [1..

        /// <summary>
        /// Minimum water content parameter for the optional Monteith water uptake sub-model
        /// mm/mm
        /// </summary>
        [Units("mm/mm")]
        public double[] LL { get; set; } // [1..

        #endregion


        #region ICanopy implementation

        /// <summary>Gets the canopy. Should return null if no canopy present.</summary>
        public string CanopyType { get { return "Pasture"; } }
        /// <summary>Canopy albedo, fraction of sun light reflected (0-1).</summary>
        [Units("0-1")]
        public double Albedo { get; set; } = GrazEnv.HERBAGE_ALBEDO;

        /// <summary>Maximum stomatal conductance (m/s).</summary>
        [Units("m/s")]
        public double Gsmax { get; set; } = 0.011;

        /// <summary>Solar radiation at which stomatal conductance decreases to 50% (W/m^2).</summary>
        [Units("W/m^2")]
        public double R50 { get; set; } = 200;

        /// <summary>Leaf Area Index of whole canopy, live + dead tissues (m^2/m^2).</summary>
        [Units("m^2/m^2")]
        public double LAITotal { get { return LAIGreen + LAIDead; } }

        /// <summary>Average canopy depth (mm)</summary>
        public double Depth { get { return Height; } }

        /// <summary>Average canopy width (mm)</summary>
        public double Width { get { return 0; } }

        /// <summary>Potential evapotranspiration, as calculated by MicroClimate (mm).</summary>
        [JsonIgnore]
        [Units("mm")]
        public double PotentialEP
        {
            get { return myWaterDemand; }
            set { myWaterDemand = value; }
        }

        /// <summary>Light profile, energy available for each canopy layer (W/m^2).</summary>
        private CanopyEnergyBalanceInterceptionlayerType[] myLightProfile;

        /// <summary>Light profile for this plant, interception calculated by MicroClimate (W/m^2).</summary>
        public CanopyEnergyBalanceInterceptionlayerType[] LightProfile
        {
            get { return myLightProfile; }
            set
            {
                if (value != null)
                {
                    InterceptedRadn = 0.0;
                    myLightProfile = value;
                    foreach (CanopyEnergyBalanceInterceptionlayerType canopyLayer in myLightProfile)
                    {
                        InterceptedRadn += canopyLayer.AmountOnGreen;
                    }

                    // to calculate photosynthesis
                    RadiationTopOfCanopy = locWtr.Radn;
                    if (InterceptedRadn > 0.0)
                    {
                        PastureModel.SetMonocultureLight();
                    }
                }
            }
        }
        #endregion

        /// <summary>Radiation intercepted by the plant's canopy (MJ/m^2/day).</summary>
        [JsonIgnore]
        [Units("MJ/m^2/day")]
        public double InterceptedRadn { get; set; }

        /// <summary>Radiance on top of the plant's canopy (MJ/m^2/day).</summary>
        [JsonIgnore]
        [Units("MJ/m^2/day")]
        public double RadiationTopOfCanopy { get; set; }

        // Water uptake process ===============================================

        /// <summary>Amount of soil water available to be taken up (mm).</summary>
        private double[] mySoilWaterUptakeAvail;

        /// <summary>Amount of soil water taken up (mm).</summary>
        public IReadOnlyList<double> WaterUptake => mySoilWaterUptakeAvail;


        /// <summary>Canopy characteristics of the plants. The array has one member per sub-canopy</summary>
        [Units("-")]
        public Canopy[] Canopy
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";

                Canopy[] result = MakeCanopy(PastureModel);

                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>
        /// Get the canopy description for a herbage component
        /// </summary>
        /// <param name="Model">Pasture model</param>
        /// <param name="iComp">The herbage component</param>
        /// <returns>A canopy object</returns>
        private Canopy MakeCanopyElement(TPasturePopulation Model, int iComp)
        {
            Canopy anElement = new Canopy();

            if (iComp == GrazType.sgLITTER)
                anElement.Name = sCOMPNAME[stLITT1];
            else
                anElement.Name = sCOMPNAME[iComp];
            anElement.PlantType = "pasture";
            anElement.Layer = new CanopyLayer[1];

            anElement.Layer[0].Thickness = 0.001 * Model.Height_MM();             // thickness (in metres)
            anElement.Layer[0].AreaIndex = Model.AreaIndex(iComp);
            if (iComp == stSEEDL || iComp == stESTAB || iComp == stSENC)
            {
                anElement.Layer[0].CoverGreen = Model.Cover(iComp);
                anElement.Layer[0].CoverTotal = anElement.Layer[0].CoverGreen;
            }
            else // stDEAD, sgLITTER
            {
                anElement.Layer[0].CoverGreen = 0.0;
                anElement.Layer[0].CoverTotal = Model.Cover(iComp);
            }

            return anElement;
        }

        /// <summary>
        /// Get the canopy details for each herbage component in the pasture
        /// </summary>
        /// <param name="Model">The pasture model</param>
        /// <returns>Array of herbage component canopy information</returns>
        private Canopy[] MakeCanopy(TPasturePopulation Model)
        {
            int iComp;
            int iCount = 0;

            for (iComp = stSEEDL; iComp <= stDEAD; iComp++)
            {
                if (Model.AreaIndex(iComp) > 0.0)
                {
                    iCount++;
                }
            }

            if (Model.AreaIndex(sgLITTER) > 0.0)
            {
                iCount++;
            }

            Canopy[] result = new Canopy[iCount];

            uint Jdx = 0;
            for (iComp = stSEEDL; iComp <= stDEAD; iComp++)
            {
                if (Model.AreaIndex(iComp) > 0.0)
                {
                    Jdx++;
                    result[Jdx - 1] = MakeCanopyElement(Model, iComp);
                }
            }

            if (Model.AreaIndex(sgLITTER) > 0.0)
            {
                Jdx++;
                result[Jdx - 1] = MakeCanopyElement(Model, sgLITTER);
            }

            return result;
        }

        #region Readable properties ====================================================

        /// <summary>
        /// Get the water supply information for this pasture
        /// </summary>
        /// <param name="Model"></param>
        /// <returns></returns>
        private WaterInfo[] MakeWaterInfo(TPasturePopulation Model)
        {
            int iCount;
            int iComp;
            double[] fRootLD;
            double[] fRootRad;
            double[] fSupply;
            int Jdx, Ldx;

            iCount = 0;
            for (iComp = stSEEDL; iComp <= stSENC; iComp++)
            {
                if (Model.WaterDemand(iComp, myWaterDemand) > 0.0)
                {
                    iCount++;
                }
            }

            WaterInfo[] result = new WaterInfo[iCount];

            iCount = Model.SoilLayerCount;
            Jdx = 0;
            for (iComp = stSEEDL; iComp <= stSENC; iComp++)
            {
                if (Model.WaterDemand(iComp,myWaterDemand) > 0.0)
                {
                    fSupply = Model.WaterMaxSupply(iComp, myWaterDemand);
                    fRootLD = Model.EffRootLengthD(iComp);
                    fRootRad = Model.RootRadii(iComp);

                    Jdx++;
                    result[Jdx - 1] = new WaterInfo();
                    result[Jdx - 1].Name = sCOMPNAME[iComp];
                    result[Jdx - 1].PlantType = "pasture";
                    result[Jdx - 1].Demand = Model.WaterDemand(iComp,myWaterDemand);
                    result[Jdx - 1].Layer = new WaterLayer[iCount];
                    for (Ldx = 1; Ldx <= iCount; Ldx++)
                    {
                        result[Jdx - 1].Layer[Ldx - 1] = new WaterLayer();
                        result[Jdx - 1].Layer[Ldx - 1].Thickness = Model.SoilLayer_MM[Ldx];
                        result[Jdx - 1].Layer[Ldx - 1].MaxSupply = fSupply[Ldx];
                        result[Jdx - 1].Layer[Ldx - 1].RLD = 1.0E-6 * fRootLD[Ldx]; // converted from m/m^3 to mm/mm^3
                        result[Jdx - 1].Layer[Ldx - 1].Radius = fRootRad[Ldx];
                    }
                }
            }

            return result;
        }

        /// <summary>Water demand and supply attributes (one member per sub-population)</summary>
        [Units("-")]
        public WaterInfo[] WaterDemandSupply
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";

                WaterInfo[] result = MakeWaterInfo(PastureModel);

                PastureModel.MassUnit = sUnit;
                return result;
            }
        }

        /// <summary>Green area index</summary>
        [Units("m^2/m^2")]
        public double GAI
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.AreaIndex(GrazType.sgGREEN);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Dead area index</summary>
        [Units("m^2/m^2")]
        public double DAI
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.AreaIndex(GrazType.sgDRY);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Green cover</summary>
        [Units("m^2/m^2")]
        public double CoverGreen
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.Cover(GrazType.sgGREEN);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Total cover</summary>
        [Units("m^2/m^2")]
        public double CoverTotal
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.Cover(GrazType.TOTAL);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Cover of standing dead and litter</summary>
        [Units("m^2/m^2")]
        public double CoverResidue
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.Cover(GrazType.sgDRY);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Height of the pasture (mm)</summary>
        [Units("mm")]
        public double Height
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.Height_MM();
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Ammonium-N uptake from each soil layer</summary>
        [Units("-")]
        public SoilLayer[] NH4Uptake { get { return NutrUptake(TPlantNutrient.pnNH4); } }

        /// <summary>Nitrate-N uptake from each soil layer</summary>
        [Units("-")]
        public SoilLayer[] NO3Uptake { get { return NutrUptake(TPlantNutrient.pnNO3); } }

        /// <summary>Phosphate-P uptake from each soil layer</summary>
        [Units("-")]
        public SoilLayer[] POXUptake { get { return NutrUptake(TPlantNutrient.pnPOx); } }

        /// <summary>Sulphate-S uptake from each soil layer</summary>
        [Units("-")]
        public SoilLayer[] SO4Uptake { get { return NutrUptake(TPlantNutrient.pnSO4); } }

        /// <summary>
        /// Nutrient uptake for the specified nutrient
        /// </summary>
        /// <param name="nutr">Nutrient. pnNO3...</param>
        /// <returns>Nutrient uptake for each soil layer in kg/ha</returns>
        private SoilLayer[] NutrUptake(TPlantNutrient nutr)
        {
            string sUnit = PastureModel.MassUnit;
            PastureModel.MassUnit = "kg/ha";

            double[][] Uptakes = PastureModel.GetNutrUptake(nutr);
            SoilLayer[] result = new SoilLayer[PastureModel.SoilLayerCount];
            double value;
            for (uint Ldx = 1; Ldx <= PastureModel.SoilLayerCount; Ldx++)
            {
                value = 0.0;
                for (int Kdx = 0; Kdx <= MAXNUTRAREAS - 1; Kdx++)
                    value = value + Uptakes[Kdx][Ldx];

                result[Ldx - 1].thickness = PastureModel.SoilLayer_MM[Ldx];
                result[Ldx - 1].amount = value;
            }

            PastureModel.MassUnit = sUnit;

            return result;
        }

        /// <summary>Parameters used by the Paddock component to determine water uptake</summary>
        [Units("")]
        public double[] WaterParams
        {
            get
            {
                double[] result;
                if ((PastureModel.fWater_KL[1] > 0.0) && (PastureModel.fPlant_LL[1] > 0.0))
                    result = new double[0];
                else if (PastureModel.Params.iWaterUptakeVersion == 1)
                    result = new double[2];
                else
                    result = new double[4];
                for (int Idx = 1; Idx <= result.Length; Idx++)
                    result[Idx - 1] = PastureModel.Params.WaterUseK[Idx];

                return result;
            }
        }

        /// <summary>
        /// Gets the total water demand from all components of the pasture
        /// </summary>
        [Units("mm")]
        public double WaterDemand
        {
            get
            {
                return myWaterDemand;
            }
            set
            {
                myWaterDemand = value;
            }
        }

        /// <summary>Total dry weight of all herbage</summary>
        [Units("kg/ha")]
        public double ShootDM { get { return GetDM(GrazType.TOTAL, GrazType.TOTAL); } }

        /// <summary>Dry weight of herbage of all herbage in each digestibility class</summary>
        [Units("kg/ha")]
        public double[] shootDMQ { get { return GetDMQ(GrazType.TOTAL, GrazType.TOTAL); } }

        /// <summary>Average DM digestibility of all herbage</summary>
        [Units("g/g")]
        public double ShootDMD { get { return GetDMD(GrazType.TOTAL, GrazType.TOTAL); } }

        /// <summary>Average crude protein content of all herbage</summary>
        [Units("g/g")]
        public double ShootCP { get { return GetPlantCP(GrazType.TOTAL, GrazType.TOTAL); } }

        /// <summary>Average nitrogen content of all herbage</summary>
        [Units("g/g")]
        public double ShootN { get { return GetPlantNutr(GrazType.TOTAL, GrazType.TOTAL, TPlantElement.N); } }

        /// <summary>Average phosphorous content of all herbage</summary>
        [Units("g/g")]
        public double ShootP { get { return GetPlantNutr(GrazType.TOTAL, GrazType.TOTAL, TPlantElement.P); } }

        /// <summary>Average sulphur content of all herbage</summary>
        [Units("g/g")]
        public double ShootS { get { return GetPlantNutr(GrazType.TOTAL, GrazType.TOTAL, TPlantElement.S); } }

        /// <summary>Total dry weight of seedlings</summary>
        [Units("kg/ha")]
        public double SeedlDM { get { return GetDM(GrazType.stSEEDL, GrazType.TOTAL); } }

        /// <summary>Dry weight of herbage of seedlings in each digestibility class</summary>
        [Units("kg/ha")]
        public double[] SeedlDMQ { get { return GetDMQ(GrazType.stSEEDL, GrazType.TOTAL); } }

        /// <summary>Average DM digestibility of seedlings</summary>
        [Units("g/g")]
        public double SeedlDMD { get { return GetDMD(GrazType.stSEEDL, GrazType.TOTAL); } }

        /// <summary>Average crude protein content of seedlings</summary>
        [Units("g/g")]
        public double SeedlCP { get { return GetPlantCP(GrazType.stSEEDL, GrazType.TOTAL); } }

        /// <summary>Average nitrogen content of seedlings</summary>
        [Units("g/g")]
        public double SeedlN { get { return GetPlantNutr(GrazType.stSEEDL, GrazType.TOTAL, TPlantElement.N); } }

        /// <summary>Average phosphorus content of seedlings</summary>
        [Units("g/g")]
        public double SeedlP { get { return GetPlantNutr(GrazType.stSEEDL, GrazType.TOTAL, TPlantElement.P); } }
        /// <summary>Average sulphur content of seedlings</summary>
        [Units("g/g")]
        public double SeedlS { get { return GetPlantNutr(GrazType.stSEEDL, GrazType.TOTAL, TPlantElement.S); } }

        /// <summary>Total dry weight of herbage of established plants</summary>
        [Units("kg/ha")]
        public double EstabDM { get { return GetDM(GrazType.stESTAB, GrazType.TOTAL); } }

        /// <summary>Dry weight of herbage of established plants in each digestibility class</summary>
        [Units("kg/ha")]
        public double[] EstabDMQ { get { return GetDMQ(GrazType.stESTAB, GrazType.TOTAL); } }

        /// <summary>Average DM digestibility of herbage of established plants</summary>
        [Units("g/g")]
        public double EstabDMD { get { return GetDMD(GrazType.stESTAB, GrazType.TOTAL); } }

        /// <summary>Average crude protein content of herbage of established plants</summary>
        [Units("g/g")]
        public double EstabCP { get { return GetPlantCP(GrazType.stESTAB, GrazType.TOTAL); } }

        /// <summary>Average nitrogen content of herbage of established plants</summary>
        [Units("g/g")]
        public double EstabN { get { return GetPlantNutr(GrazType.stESTAB, GrazType.TOTAL, TPlantElement.N); } }

        /// <summary>Average phosphorus content of herbage of established plants</summary>
        [Units("g/g")]
        public double EstabP { get { return GetPlantNutr(GrazType.stESTAB, GrazType.TOTAL, TPlantElement.P); } }

        /// <summary>Average sulphur content of herbage of established plants</summary>
        [Units("g/g")]
        public double EstabS { get { return GetPlantNutr(GrazType.stESTAB, GrazType.TOTAL, TPlantElement.S); } }

        /// <summary>Total dry weight of herbage of senescing plants</summary>
        [Units("kg/ha")]
        public double SencDM { get { return GetDM(GrazType.stSENC, GrazType.TOTAL); } }

        /// <summary>Dry weight of herbage of senescing plants in each digestibility clas</summary>
        [Units("kg/ha")]
        public double[] SencDMQ { get { return GetDMQ(GrazType.stSENC, GrazType.TOTAL); } }

        /// <summary>Average DM digestibility of herbage of senescing plants</summary>
        [Units("g/g")]
        public double SencDMD { get { return GetDMD(GrazType.stSENC, GrazType.TOTAL); } }

        /// <summary>Average crude protein content of herbage of senescing plants</summary>
        [Units("g/g")]
        public double SencCP { get { return GetPlantCP(GrazType.stSENC, GrazType.TOTAL); } }

        /// <summary>Average nitrogen content of senescing plants</summary>
        [Units("g/g")]
        public double SencN { get { return GetPlantNutr(GrazType.stSENC, GrazType.TOTAL, TPlantElement.N); } }

        /// <summary>Average phosphorus content of senescing plants</summary>
        [Units("g/g")]
        public double SencP { get { return GetPlantNutr(GrazType.stSENC, GrazType.TOTAL, TPlantElement.P); } }

        /// <summary>Average sulphur content of senescing plants</summary>
        [Units("g/g")]
        public double SencS { get { return GetPlantNutr(GrazType.stSENC, GrazType.TOTAL, TPlantElement.S); } }

        /// <summary>Total dry weight of standing dead herbage</summary>
        [Units("kg/ha")]
        public double DeadDM { get { return GetDM(GrazType.stDEAD, GrazType.TOTAL); } }

        /// <summary>Dry weight of standing dead herbage in each digestibility class</summary>
        [Units("kg/ha")]
        public double[] DeadDMQ { get { return GetDMQ(GrazType.stDEAD, GrazType.TOTAL); } }

        /// <summary>Average DM digestibility of standing dead herbage</summary>
        [Units("g/g")]
        public double DeadDMD { get { return GetDMD(GrazType.stDEAD, GrazType.TOTAL); } }

        /// <summary>Average crude protein content of standing dead herbage</summary>
        [Units("g/g")]
        public double DeadCP { get { return GetPlantCP(GrazType.stDEAD, GrazType.TOTAL); } }

        /// <summary>Average nitrogen content of standing dead herbage</summary>
        [Units("g/g")]
        public double DeadN { get { return GetPlantNutr(GrazType.stDEAD, GrazType.TOTAL, TPlantElement.N); } }

        /// <summary>Average phosphorus content of standing dead herbage</summary>
        [Units("g/g")]
        public double DeadP { get { return GetPlantNutr(GrazType.stDEAD, GrazType.TOTAL, TPlantElement.P); } }

        /// <summary>Average sulphur content of standing dead herbage</summary>
        [Units("g/g")]
        public double DeadS { get { return GetPlantNutr(GrazType.stDEAD, GrazType.TOTAL, TPlantElement.S); } }

        /// <summary>Total dry weight of litter</summary>
        [Units("kg/ha")]
        public double LitterDM { get { return GetDM(GrazType.sgLITTER, GrazType.TOTAL); } }

        /// <summary>Dry weight of herbage of litter in each digestibility class</summary>
        [Units("kg/ha")]
        public double[] LitterDMQ { get { return GetDMQ(GrazType.sgLITTER, GrazType.TOTAL); } }

        /// <summary>Average DM digestibility of litter</summary>
        [Units("g/g")]
        public double LitterDMD { get { return GetDMD(GrazType.sgLITTER, GrazType.TOTAL); } }

        /// <summary>Average crude protein content of litter</summary>
        [Units("g/g")]
        public double LitterCP { get { return GetPlantCP(GrazType.sgLITTER, GrazType.TOTAL); } }

        /// <summary>Average nitrogen content of litter</summary>
        [Units("g/g")]
        public double LitterN { get { return GetPlantNutr(GrazType.sgLITTER, GrazType.TOTAL, TPlantElement.N); } }

        /// <summary>Average phosphorus content of litter</summary>
        [Units("g/g")]
        public double LitterP { get { return GetPlantNutr(GrazType.sgLITTER, GrazType.TOTAL, TPlantElement.P); } }

        /// <summary>Average sulphur content of litter</summary>
        [Units("g/g")]
        public double LitterS { get { return GetPlantNutr(GrazType.sgLITTER, GrazType.TOTAL, TPlantElement.S); } }

        /// <summary>Total dry weight of green herbage</summary>
        [Units("kg/ha")]
        public double GreenDM { get { return GetDM(GrazType.sgGREEN, GrazType.TOTAL); } }

        /// <summary>Dry weight of green herbage in each digestibility class</summary>
        [Units("kg/ha")]
        public double[] GreenDMQ { get { return GetDMQ(GrazType.sgGREEN, GrazType.TOTAL); } }

        /// <summary>Average DM digestibility of green herbage</summary>
        [Units("g/g")]
        public double GreenDMD { get { return GetDMD(GrazType.sgGREEN, GrazType.TOTAL); } }

        /// <summary>Average crude protein content of green herbage (seedlings+established+senescing)</summary>
        [Units("g/g")]
        public double GreenCP { get { return GetPlantCP(GrazType.sgGREEN, GrazType.TOTAL); } }

        /// <summary>Average nitrogen content of green herbage</summary>
        [Units("g/g")]
        public double GreenN { get { return GetPlantNutr(GrazType.sgGREEN, GrazType.TOTAL, TPlantElement.N); } }

        /// <summary>Average phosphorus content of green herbage</summary>
        [Units("g/g")]
        public double GreenP { get { return GetPlantNutr(GrazType.sgGREEN, GrazType.TOTAL, TPlantElement.P); } }

        /// <summary>Average sulphur content of green herbage</summary>
        [Units("g/g")]
        public double GreenS { get { return GetPlantNutr(GrazType.sgGREEN, GrazType.TOTAL, TPlantElement.S); } }

        /// <summary>Total dry weight of dry herbage including litter</summary>
        [Units("kg/ha")]
        public double DryDM { get { return GetDM(GrazType.sgDRY, GrazType.TOTAL); } }

        /// <summary>Dry weight of dry herbage in each digestibility class</summary>
        [Units("kg/ha")]
        public double[] DryDMQ { get { return GetDMQ(GrazType.sgDRY, GrazType.TOTAL); } }

        /// <summary>Average DM digestibility of dry herbage</summary>
        [Units("g/g")]
        public double DryDMD { get { return GetDMD(GrazType.sgDRY, GrazType.TOTAL); } }

        /// <summary>Average crude protein content of dry herbage (standing dead+litter)</summary>
        [Units("g/g")]
        public double DryCP { get { return GetPlantCP(GrazType.sgDRY, GrazType.TOTAL); } }

        /// <summary>Average nitrogen content of dry herbage</summary>
        [Units("g/g")]
        public double DryN { get { return GetPlantNutr(GrazType.sgDRY, GrazType.TOTAL, TPlantElement.N); } }

        /// <summary>Average phosphorus content of dry herbage</summary>
        [Units("g/g")]
        public double DryP { get { return GetPlantNutr(GrazType.sgDRY, GrazType.TOTAL, TPlantElement.P); } }

        /// <summary>Average sulphur content of dry herbage</summary>
        [Units("g/g")]
        public double DryS { get { return GetPlantNutr(GrazType.sgDRY, GrazType.TOTAL, TPlantElement.S); } }

        /// <summary>Total dry weight of all leaves</summary>
        [Units("kg/ha")]
        public double LeafDM { get { return GetDM(GrazType.TOTAL, GrazType.ptLEAF); } }

        /// <summary>Dry weight of all leaves in each digestibility class</summary>
        [Units("kg/ha")]
        public double[] LeafDMQ { get { return GetDMQ(GrazType.TOTAL, GrazType.ptLEAF); } }

        /// <summary>Average DM digestibility of all leaves</summary>
        [Units("g/g")]
        public double LeafDMD { get { return GetDMD(GrazType.TOTAL, GrazType.ptLEAF); } }

        /// <summary>Average crude protein content of all leaves</summary>
        [Units("g/g")]
        public double LeafCP { get { return GetPlantCP(GrazType.TOTAL, GrazType.ptLEAF); } }

        /// <summary>Average nitrogen content of all leaves</summary>
        [Units("g/g")]
        public double LeafN { get { return GetPlantNutr(GrazType.TOTAL, GrazType.ptLEAF, TPlantElement.N); } }

        /// <summary>Average phosphorus content of all leaves</summary>
        [Units("g/g")]
        public double LeafP { get { return GetPlantNutr(GrazType.TOTAL, GrazType.ptLEAF, TPlantElement.P); } }

        /// <summary>Average sulphur content of all leaves</summary>
        [Units("g/g")]
        public double LeafS { get { return GetPlantNutr(GrazType.TOTAL, GrazType.ptLEAF, TPlantElement.S); } }

        /// <summary>Total dry weight of all stems</summary>
        [Units("kg/ha")]
        public double StemDM { get { return GetDM(GrazType.TOTAL, GrazType.ptSTEM); } }

        /// <summary>Dry weight of all stems in each digestibility class</summary>
        [Units("kg/ha")]
        public double[] StemDMQ { get { return GetDMQ(GrazType.TOTAL, GrazType.ptSTEM); } }

        /// <summary>Average DM digestibility of all stems</summary>
        [Units("g/g")]
        public double StemDMD { get { return GetDMD(GrazType.TOTAL, GrazType.ptSTEM); } }

        /// <summary>Average crude protein content of all stems</summary>
        [Units("g/g")]
        public double StemCP { get { return GetPlantCP(GrazType.TOTAL, GrazType.ptSTEM); } }

        /// <summary>Average nitrogen content of all stems</summary>
        [Units("g/g")]
        public double StemN { get { return GetPlantNutr(GrazType.TOTAL, GrazType.ptSTEM, TPlantElement.N); } }

        /// <summary>Average phosphorus content of all stems</summary>
        [Units("g/g")]
        public double StemP { get { return GetPlantNutr(GrazType.TOTAL, GrazType.ptSTEM, TPlantElement.P); } }

        /// <summary>Average sulphur content of all stems</summary>
        [Units("g/g")]
        public double StemS { get { return GetPlantNutr(GrazType.TOTAL, GrazType.ptSTEM, TPlantElement.S); } }

        /// <summary>Total dry weight of herbage available for grazing</summary>
        [Units("kg/ha")]
        public double AvailDM
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.AvailHerbage(GrazType.TOTAL, GrazType.TOTAL, GrazType.TOTAL);
                PastureModel.MassUnit = sUnit;
                return result;
            }
        }

        /// <summary>Dry weight of herbage available for grazing in each digestibility class</summary>
        [Units("kg/ha")]
        public double[] AvailDMQ
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";

                double[] result = new double[GrazType.HerbClassNo]; ;
                for (int i = 1; i <= GrazType.HerbClassNo; i++)
                    result[i - 1] = PastureModel.AvailHerbage(GrazType.TOTAL, GrazType.TOTAL, i);

                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Average DM digestibility of herbage available for grazing</summary>
        [Units("g/g")]
        public double AvailDMD
        {
            get
            {
                double result = WeightAverage(PastureModel.Digestibility(GrazType.sgGREEN, GrazType.TOTAL),
                                     PastureModel.AvailHerbage(GrazType.sgGREEN, GrazType.TOTAL, GrazType.TOTAL),
                                     PastureModel.Digestibility(GrazType.sgDRY, GrazType.TOTAL),
                                     PastureModel.AvailHerbage(GrazType.sgDRY, GrazType.TOTAL, GrazType.TOTAL));

                return result;
            }
        }

        /// <summary>Average crude protein content of herbage available for grazing</summary>
        [Units("g/g")]
        public double AvailCP
        {
            get
            {
                double result = WeightAverage(PastureModel.CrudeProtein(GrazType.sgGREEN, GrazType.TOTAL),
                                     PastureModel.AvailHerbage(GrazType.sgGREEN, GrazType.TOTAL, GrazType.TOTAL),
                                     PastureModel.CrudeProtein(GrazType.sgDRY, GrazType.TOTAL),
                                     PastureModel.AvailHerbage(GrazType.sgDRY, GrazType.TOTAL, GrazType.TOTAL));

                return result;
            }
        }

        /// <summary>Weight of green (seedling+established+senescing) herbage available for grazing</summary>
        [Units("kg/ha")]
        public double AvailGreen
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.AvailHerbage(GrazType.sgGREEN, GrazType.TOTAL, GrazType.TOTAL);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Weight of dry (standing dead+litter) herbage available for grazing</summary>
        [Units("kg/ha")]
        public double AvailDry
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.AvailHerbage(GrazType.sgDRY, GrazType.TOTAL, GrazType.TOTAL);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Height profile of green herbage (by plant parts)</summary>
        [Units("kg/ha")]
        public HerbageProfile GreenProfile
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                HerbageProfile result = makeHerbageProfile(PastureModel, GrazType.sgGREEN);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Height profile of all herbage (by plant parts)</summary>
        [Units("-")]
        public HerbageProfile ShootProfile
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                HerbageProfile result = makeHerbageProfile(PastureModel, GrazType.TOTAL);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        private HerbageProfile makeHerbageProfile(TPasturePopulation Model, int iComp)
        {
            double[] fPartMasses = new double[5];         // [ptLEAF..ptSEED]
            int iHeightProfileCount = 0;
            double[] fHeightProfile = new double[MaxSoilLayers + 1];
            double[][] fProfilePropns = new double[5][];     // [ptLEAF..ptSEED] of LayerArray;
            int iPart;
            int iLayer;


            for (int i = 0; i <= 5; i++)
            {
                fProfilePropns[i] = new double[MaxSoilLayers + 1];
            }

            // init fProfilePropns
            for (iPart = ptLEAF; iPart <= ptSTEM; iPart++)
            {
                for (iLayer = 1; iLayer <= MaxSoilLayers; iLayer++)
                    fProfilePropns[iPart][iLayer] = 0;
            }

            for (iPart = ptLEAF; iPart <= ptSTEM; iPart++)
            {
                fPartMasses[iPart] = Model.GetHerbageMass(iComp, iPart, TOTAL);
            }

            fPartMasses[ptSEED] = Model.GetSeedMass(TOTAL, TOTAL, 1);

            if (fPartMasses[ptLEAF] + fPartMasses[ptSTEM] + fPartMasses[ptSEED] <= 0.0)
            {
                iHeightProfileCount = 0;
            }
            else
            {
                Model.GetHeightProfile(iComp, ptSEED, ref iHeightProfileCount, ref fHeightProfile, ref fProfilePropns[ptSEED]);
                Model.GetHeightProfile(iComp, ptSTEM, ref iHeightProfileCount, ref fHeightProfile, ref fProfilePropns[ptSTEM]);
                Model.GetHeightProfile(iComp, ptLEAF, ref iHeightProfileCount, ref fHeightProfile, ref fProfilePropns[ptLEAF]);
            }

            HerbageProfile result = new HerbageProfile(iHeightProfileCount);

            for (iLayer = 1; iLayer <= iHeightProfileCount; iLayer++)
            {
                if (iLayer == 1)
                    result.bottom[iLayer - 1] = 0.0;
                else
                    result.bottom[iLayer - 1] = fHeightProfile[iLayer - 1];
                result.top[iLayer - 1] = fHeightProfile[iLayer];
                result.leaf[iLayer - 1] = fPartMasses[ptLEAF] * fProfilePropns[ptLEAF][iLayer];
                result.stem[iLayer - 1] = fPartMasses[ptSTEM] * fProfilePropns[ptSTEM][iLayer];
                result.head[iLayer - 1] = fPartMasses[ptSEED] * fProfilePropns[ptSEED][iLayer];
            }

            return result;
        }

        /// <summary>Total dry weight of all roots</summary>
        [Units("kg/ha")]
        public double RootDM
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.GetRootMass(GrazType.sgGREEN, GrazType.TOTAL, GrazType.TOTAL);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Average nitrogen content of all roots</summary>
        [Units("g/g")]
        public double RootN
        {
            get
            {
                return PastureModel.GetRootConc(GrazType.sgGREEN, GrazType.TOTAL, GrazType.TOTAL, TPlantElement.N);
            }
        }

        /// <summary>Average phosphorus content of all roots</summary>
        [Units("g/g")]
        public double RootP
        {
            get
            {
                return PastureModel.GetRootConc(GrazType.sgGREEN, GrazType.TOTAL, GrazType.TOTAL, TPlantElement.P);
            }
        }

        /// <summary>Average sulphur content of all roots</summary>
        [Units("g/g")]
        public double RootS
        {
            get
            {
                return PastureModel.GetRootConc(GrazType.sgGREEN, GrazType.TOTAL, GrazType.TOTAL, TPlantElement.S);
            }
        }

        /// <summary>Dry weight of all roots in each soil layer</summary>
        [Units("kg/ha")]
        public double[] RootDMProfile
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";

                double[] result = new double[PastureModel.SoilLayerCount];
                for (int i = 1; i <= PastureModel.SoilLayerCount; i++)
                    result[i - 1] = PastureModel.GetRootMass(GrazType.sgGREEN, GrazType.TOTAL, i);

                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Average nitrogen content of roots in each soil layer</summary>
        [Units("kg/ha")]
        public double[] RootProfileN
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";

                double[] result = new double[PastureModel.SoilLayerCount];
                for (int i = 1; i <= PastureModel.SoilLayerCount; i++)
                    result[i - 1] = PastureModel.GetRootNutr(GrazType.sgGREEN, GrazType.TOTAL, i, TPlantElement.N);

                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Average phosphorus content of roots in each soil layer</summary>
        [Units("kg/ha")]
        public double[] RootProfileP
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";

                double[] result = new double[PastureModel.SoilLayerCount];
                for (int i = 1; i <= PastureModel.SoilLayerCount; i++)
                    result[i - 1] = PastureModel.GetRootNutr(GrazType.sgGREEN, GrazType.TOTAL, i, TPlantElement.P);

                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Average sulphur content of roots in each soil layer</summary>
        [Units("kg/ha")]
        public double[] RootProfileS
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";

                double[] result = new double[PastureModel.SoilLayerCount];
                for (int i = 1; i <= PastureModel.SoilLayerCount; i++)
                    result[i - 1] = PastureModel.GetRootNutr(GrazType.sgGREEN, GrazType.TOTAL, i, TPlantElement.S);

                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Total dry weight of effective roots</summary>
        [Units("kg/ha")]
        public double RootEffDM
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.GetRootMass(GrazType.sgGREEN, GrazType.EFFR, GrazType.TOTAL);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Dry weight of effective roots in each soil layer</summary>
        [Units("kg/ha")]
        public double[] RootEffDMProfile
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";

                double[] result = new double[PastureModel.SoilLayerCount];
                for (int i = 1; i <= PastureModel.SoilLayerCount; i++)
                    result[i - 1] = PastureModel.GetRootMass(GrazType.sgGREEN, GrazType.EFFR, i);

                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Current depth of the rooting front</summary>
        [Units("mm")]
        public double RootDep
        {
            get { return PastureModel.GetRootDepth(GrazType.sgGREEN, TPasturePopulation.ALL_COHORTS); }
        }

        /// <summary>Average radius of all roots</summary>
        [Units("mm")]
        public double RootRadius
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.RootRadii(GrazType.sgGREEN)[1];
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Length density of effective roots in each soil layer</summary>
        [Units("mm/mm^3")]
        public double[] RootLVProfile
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";

                double[] result = new double[PastureModel.SoilLayerCount];
                for (int i = 1; i <= PastureModel.SoilLayerCount; i++)
                    result[i - 1] = PastureModel.EffRootLengthD(GrazType.sgGREEN)[i] * 1e-6;

                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Total dry weight of seeds in all soil layers</summary>
        [Units("kg/ha")]
        public double SeedDM
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.GetSeedMass(GrazType.TOTAL, GrazType.TOTAL, GrazType.TOTAL);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Average crude protein content of seeds</summary>
        [Units("g/g")]
        public double SeedCP
        {
            get
            {
                return PastureModel.SeedCrudeProtein(GrazType.TOTAL, GrazType.TOTAL, GrazType.TOTAL);
            }
        }

        /// <summary>Average nitrogen content of seeds</summary>
        [Units("g/g")]
        public double SeedN
        {
            get
            {
                return PastureModel.GetSeedNutr(GrazType.TOTAL, GrazType.TOTAL, GrazType.TOTAL, TPlantElement.N);
            }
        }

        /// <summary>Average phosphorus content of seeds</summary>
        [Units("g/g")]
        public double SeedP
        {
            get
            {
                return PastureModel.GetSeedNutr(GrazType.TOTAL, GrazType.TOTAL, GrazType.TOTAL, TPlantElement.P);
            }
        }

        /// <summary>Average sulphur content of seeds</summary>
        [Units("g/g")]
        public double SeedS
        {
            get
            {
                return PastureModel.GetSeedNutr(GrazType.TOTAL, GrazType.TOTAL, GrazType.TOTAL, TPlantElement.S);
            }
        }

        /// <summary>Weighted average value of the establishment index for seedlings</summary>
        [Units("-")]
        public double EstIndex
        {
            get
            {
                return PastureModel.EstablishIndex();
            }
        }

        /// <summary>Weighted average value of the seedling stress index</summary>
        [Units("-")]
        public double StressIndex
        {
            get
            {
                return PastureModel.SeedlingStress();
            }
        }

        /// <summary>Water uptake from each soil layer</summary>
        [Units("mm")]
        public double[] SWUptake
        {
            get
            {
                return LayerArray2Value(PastureModel.Transpiration(GrazType.sgGREEN), PastureModel.SoilLayerCount);
            }
        }

        private void DMPool2Value(DM_Pool Pool, ref OrganicMatter aValue)
        {
            aValue.Weight = Pool.DM;                    // kg / ha
            aValue.N = Pool.Nu[(int)TPlantElement.N];   // kg / ha
            aValue.P = Pool.Nu[(int)TPlantElement.P];   // kg / ha
            aValue.S = Pool.Nu[(int)TPlantElement.S];   // kg / ha
            aValue.AshAlk = Pool.AshAlk;                // mol / ha
        }

        private double[] LayerArray2Value(double[] fLayers, int iNoLayers)
        {
            double[] result = new double[iNoLayers];

            for (int Ldx = 1; Ldx <= iNoLayers; Ldx++)
                result[Ldx - 1] = fLayers[Ldx];

            return result;
        }

        /// <summary>Whole-plant net primary productivity</summary>
        [Units("kg/ha/d")]
        public double NPP
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.NetPP(GrazType.sgGREEN, GrazType.TOTAL);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Net primary productivity of shoots</summary>
        [Units("kg/ha/d")]
        public double ShootNPP
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.NetPP(GrazType.sgGREEN, GrazType.TOTAL) - PastureModel.NetPP(GrazType.sgGREEN, GrazType.ptROOT);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Gross whole-plant assimilation rate</summary>
        [Units("kg/ha/d")]
        public double Assimilation
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.Assimilation(GrazType.sgGREEN, GrazType.TOTAL);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Whole-plant respiration rate, in dry weight equivalent terms</summary>
        [Units("kg/ha/d")]
        public double Respiration
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.Respiration(GrazType.sgGREEN, GrazType.TOTAL);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Daily shoot growth rate</summary>
        [Units("kg/ha/d")]
        public double Growth
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.TopGrowth();
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Green leaf area index</summary>
        [Units("m^2/m^2")]
        public double LAI
        {
            get
            {
                double result = 0;
                if (PastureModel != null)
                {
                    string sUnit = PastureModel.MassUnit;
                    PastureModel.MassUnit = "kg/ha";
                    result = PastureModel.AreaIndex(GrazType.sgGREEN, GrazType.ptLEAF);
                    PastureModel.MassUnit = sUnit;
                }

                return result;
            }
            set
            {
                //throw new Exception("LAI.set not implemented");
            }
        }

        /// <summary>Green leaf area index</summary>
        [Units("m^2/m^2")]
        public double LAIGreen
        {
            get { return LAI; }
        }

        /// <summary>Dead area index</summary>
        [Units("m^2/m^2")]
        public double LAIDead
        {
            get
            {
                double result = 0;
                if (PastureModel != null)
                {
                    string sUnit = PastureModel.MassUnit;
                    PastureModel.MassUnit = "kg/ha";
                    result = PastureModel.AreaIndex(GrazType.sgDRY, GrazType.ptLEAF);
                    PastureModel.MassUnit = sUnit;
                }

                return result;
            }
        }

        /// <summary>Light interception growth-limiting factor</summary>
        [Units("-")]
        public double GLF_GAI { get { return GetGLF(TGrowthLimit.glGAI); } }

        /// <summary>VPD growth-limiting factor</summary>
        [Units("-")]
        public double GLF_VPD { get { return GetGLF(TGrowthLimit.glVPD); } }

        /// <summary>Soil moisture growth-limiting factor</summary>
        [Units("-")]
        public double GLF_SM { get { return GetGLF(TGrowthLimit.glSM); } }

        /// <summary>Temperature growth-limiting factor</summary>
        [Units("-")]
        public double GLF_Temp { get { return GetGLF(TGrowthLimit.glLowT); } }

        /// <summary>Waterlogging growth-limiting factor</summary>
        [Units("-")]
        public double GLF_WL { get { return GetGLF(TGrowthLimit.glWLog); } }

        /// <summary>Nitrogen growth-limiting factor</summary>
        [Units("-")]
        public double GLF_N { get { return GetGLF(TGrowthLimit.gl_N); } }

        /// <summary>Phosphorus growth-limiting factor</summary>
        [Units("-")]
        public double GLF_P { get { return GetGLF(TGrowthLimit.gl_P); } }

        /// <summary>Sulphur growth-limiting factor</summary>
        [Units("-")]
        public double GLF_S { get { return GetGLF(TGrowthLimit.gl_S); } }

        private double GetGLF(TGrowthLimit factor)
        {
            string sUnit = PastureModel.MassUnit;
            PastureModel.MassUnit = "kg/ha";
            double result = PastureModel.GrowthLimit(GrazType.sgGREEN, factor);   // glGAI, glVPD, glSM, glLowT, glWLog, gl_N, gl_P, gl_S
            PastureModel.MassUnit = sUnit;

            return result;
        }

        /// <summary>Nutrient growth-limiting factor</summary>
        [Units("-")]
        public double GLF_Nutr
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = Math.Min(PastureModel.GrowthLimit(GrazType.sgGREEN, TGrowthLimit.gl_N), Math.Min(PastureModel.GrowthLimit(GrazType.sgGREEN, TGrowthLimit.gl_P), PastureModel.GrowthLimit(GrazType.sgGREEN, TGrowthLimit.gl_S)));
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Nitrogen fixation rate</summary>
        [Units("kg/ha/d")]
        public double N_Fixed
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.N_Fixation(GrazType.sgGREEN);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Ammonium-N uptake from each soil layer</summary>
        [Units("kg/ha/d")]
        public double[] Uptake_NH4 { get { return UptakeByLayer(TPlantNutrient.pnNH4); } }

        /// <summary>Nitrate-N uptake from each soil layer</summary>
        [Units("kg/ha/d")]
        public double[] Uptake_NO3 { get { return UptakeByLayer(TPlantNutrient.pnNO3); } }

        /// <summary>Phosphate-P uptake from each soil layer</summary>
        [Units("kg/ha/d")]
        public double[] Uptake_POx { get { return UptakeByLayer(TPlantNutrient.pnPOx); } }

        /// <summary>Sulphate-S uptake from each soil layer</summary>
        [Units("kg/ha/d")]
        public double[] Uptake_SO4 { get { return UptakeByLayer(TPlantNutrient.pnSO4); } }

        /// <summary>
        /// Get the plant nutrient uptake from each soil layer
        /// </summary>
        /// <param name="nutr">Plant nutrient</param>
        /// <returns>Array of layer values for nutrient uptake kg/ha</returns>
        private double[] UptakeByLayer(TPlantNutrient nutr)
        {
            string sUnit = PastureModel.MassUnit;
            PastureModel.MassUnit = "kg/ha";

            double[][] Uptakes = PastureModel.GetNutrUptake(nutr);
            double[] result = new double[PastureModel.SoilLayerCount];

            for (int layer = 1; layer <= PastureModel.SoilLayerCount; layer++)
            {
                double value = 0.0;
                for (int area = 0; area <= MAXNUTRAREAS - 1; area++)
                    value = value + Uptakes[area][layer];

                result[layer - 1] = value;
            }

            PastureModel.MassUnit = sUnit;

            return result;
        }

        /// <summary>Allocation of assimilate to each plant part</summary>
        [Units("-")]
        public Allocation PlantAllocation
        {
            get
            {
                Allocation result = new Allocation();
                result.Leaf = PastureModel.Allocation(GrazType.sgGREEN, GrazType.ptLEAF);
                result.Stem = PastureModel.Allocation(GrazType.sgGREEN, GrazType.ptSTEM);
                result.Root = PastureModel.Allocation(GrazType.sgGREEN, GrazType.ptROOT);
                result.Seed = PastureModel.Allocation(GrazType.sgGREEN, GrazType.ptSEED);

                return result;
            }
        }

        /// <summary>Dry weight and quality of residues incorporated into the soil in this time step (one member per soil layer). [0] is surface</summary>
        [Units("-")]
        public Residue[] ResiduePlant
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";

                Residue[] result = new Residue[PastureModel.SoilLayerCount];

                DM_Pool surfPool = PastureModel.GetResidueFlux(GrazType.ptLEAF);
                AddDMPool(PastureModel.GetResidueFlux(GrazType.ptSTEM), surfPool);
                AddDMPool(PastureModel.GetResidueFlux(GrazType.ptROOT, 1), surfPool);
                result[0].CopyFrom(surfPool);
                for (int i = 2; i <= PastureModel.SoilLayerCount; i++)
                    result[i - 1].CopyFrom(PastureModel.GetResidueFlux(ptROOT, i));

                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Dry weight and quality of leaf residues incorporated into the soil in this time step</summary>
        [Units("-")]
        public Residue ResidueLeaf
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";

                Residue result = new Residue();
                result.CopyFrom(PastureModel.GetResidueFlux(GrazType.ptLEAF));

                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Dry weight and quality of stem residues incorporated into the soil in this time step</summary>
        [Units("-")]
        public Residue ResidueStem
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";

                Residue result = new Residue();
                result.CopyFrom(PastureModel.GetResidueFlux(GrazType.ptSTEM));

                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Dry weight and quality of root residues incorporated into the soil in this time step (one member per soil layer)</summary>
        [Units("-")]
        public Residue[] ResidueRoot
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";

                Residue[] result = new Residue[PastureModel.SoilLayerCount];
                for (int i = 1; i <= PastureModel.SoilLayerCount; i++)
                    result[i - 1].CopyFrom(PastureModel.GetResidueFlux(GrazType.ptROOT, i));

                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Mass of organic nutrients leached from dead pasture and litter by rainfall</summary>
        [Units("-")]
        public Leachate PlantLeachate
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";

                Leachate result = new Leachate();
                DM_Pool pool = PastureModel.GetLeachate();
                result.N = pool.Nu[(int)TPlantElement.N];
                result.P = pool.Nu[(int)TPlantElement.P];
                result.S = pool.Nu[(int)TPlantElement.S];

                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Rate of volatilization of tissue N into the atmosphere</summary>
        [Units("kg/ha/d")]
        public double GasNLoss
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = PastureModel.GetGaseousLoss(TPlantElement.N);
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Herbage bulk density of green shoots</summary>
        [Units("kg/m^3")]
        public double GreenBD
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double result = REF_HERBAGE_BD / PastureModel.Params.HeightRatio;
                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Relative height of two 'horizons' affecting the impact of defoliation on phenology</summary>
        [Units("-")]
        public double[] PhenHorizon
        {
            get
            {
                double[] result = new double[2];
                result[0] = PastureModel.fPhenoHorizon[0];
                result[1] = PastureModel.fPhenoHorizon[1];

                return result;
            }
        }

        private int[] DefoliationCompMap = { 0, sgGREEN, stDEAD, sgLITTER };    // 1..3
        private int[] DefoliationPartMap = { 0, ptLEAF, ptSTEM };               // 1..2

        /// <summary>Amount of herbage defoliated from each of green/standing dead/litter (1st index) x leaf/stem (2nd index) x DMD class (3rd index, 1=DMD 80-85pc, 12=DMD 35-40pc)</summary>
        [Units("kg/ha")]
        public double[,,] Defoliation
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";

                double[,,] result = new double[3, 2, 12];

                for (int Idx = 1; Idx <= 3; Idx++)
                {
                    for (int Jdx = 1; Jdx <= 2; Jdx++)
                    {
                        for (int Kdx = 1; Kdx <= 12; Kdx++)
                        {
                            result[Idx - 1, Jdx - 1, Kdx - 1] = PastureModel.Removal(DefoliationCompMap[Idx], DefoliationPartMap[Jdx], Kdx);
                        }
                    }
                }

                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>Rate of death of green herbage from each of leaf/stem (1st index) x DMD class (2nd index, 1=DMD 80-85pc, 12=DMD 35-40pc). Does not include defoliation or death due to kill, cultivate or cut events</summary>
        [Units("kg/ha/d")]
        public double[,] DeathRate { get { return Loss(GrazType.sgGREEN); } }

        /// <summary>Rate of fall of standing dead herbage from each of leaf/stem (1st index) x DMD class (2nd index, 1=DMD 80-85pc, 12=DMD 35-40pc)</summary>
        [Units("kg/ha/d")]
        public double[,] FallRate { get { return Loss(GrazType.stDEAD); } }

        /// <summary>Rate of incorporation of litter from each of leaf/stem (1st index) x DMD class (2nd index, 1=DMD 80-85pc, 12=DMD 35-40pc)</summary>
        [Units("kg/ha/d")]
        public double[,] IncorpRate { get { return Loss(GrazType.stLITT2); } }

        private double[,] Loss(int comp)
        {
            string sUnit = PastureModel.MassUnit;
            PastureModel.MassUnit = "kg/ha";
            double[,] result = new double[2, 12];

            for (int Jdx = 1; Jdx <= 2; Jdx++)
            {
                for (int Kdx = 1; Kdx <= 12; Kdx++)
                {
                    result[Jdx - 1, Kdx - 1] = PastureModel.BiomassExit(comp, DefoliationPartMap[Jdx], Kdx);
                }
            }

            PastureModel.MassUnit = sUnit;

            return result;
        }

        /// <summary>Amount of death of green herbage as a result of kill or cultivate events from each of leaf and stem</summary>
        [Units("kg/ha")]
        public double[] Killed
        {
            get
            {
                string sUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                double[] result = new double[2];
                result[0] = PastureModel.ShootKilled(GrazType.ptLEAF, GrazType.TOTAL);
                result[1] = PastureModel.ShootKilled(GrazType.ptSTEM, GrazType.TOTAL);

                PastureModel.MassUnit = sUnit;

                return result;
            }
        }

        /// <summary>
        /// Total dry weight of herbage of a plant
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="part">Plant part</param>
        /// <returns></returns>
        private double GetDM(int comp, int part)
        {
            string sUnit = PastureModel.MassUnit;
            PastureModel.MassUnit = "kg/ha";
            double result = PastureModel.GetHerbageMass(comp, part, GrazType.TOTAL);
            PastureModel.MassUnit = sUnit;

            return result;
        }

        /// <summary>
        /// Get the average digestibility of this herbage
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="part">Plant part</param>
        /// <returns></returns>
        private double GetDMD(int comp, int part)
        {
            string sUnit = PastureModel.MassUnit;
            PastureModel.MassUnit = "kg/ha";
            double result = PastureModel.Digestibility(comp, part);
            PastureModel.MassUnit = sUnit;

            return result;
        }

        /// <summary>
        /// Get the dry weight of a plant in each digestibility class
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="part">Plant part</param>
        /// <returns></returns>
        private double[] GetDMQ(int comp, int part)
        {
            string sUnit = PastureModel.MassUnit;
            PastureModel.MassUnit = "kg/ha";
            double[] result = new double[GrazType.HerbClassNo];
            for (int i = 1; i <= GrazType.HerbClassNo; i++)
                result[i - 1] = PastureModel.GetHerbageMass(comp, part, i);
            PastureModel.MassUnit = sUnit;

            return result;
        }

        /// <summary>
        /// Get average nutrient content of a plant (g/g)
        /// </summary>
        /// <param name="comp">Herbage</param>
        /// <param name="part">Plant part</param>
        /// <param name="elem">Nutrient element</param>
        /// <returns></returns>
        private double GetPlantNutr(int comp, int part, TPlantElement elem)
        {
            return PastureModel.GetHerbageConc(comp, part, GrazType.TOTAL, elem);
        }

        /// <summary>
        /// Get average crude protein of plant
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="part">Plant part</param>
        /// <returns></returns>
        private double GetPlantCP(int comp, int part)
        {
            string sUnit = PastureModel.MassUnit;
            PastureModel.MassUnit = "kg/ha";
            double result = PastureModel.CrudeProtein(comp, part, GrazType.TOTAL);
            PastureModel.MassUnit = sUnit;

            return result;
        }
        #endregion

        #region Subscribed events ====================================================

        /// <summary>
        /// At the start of the simulation.
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            // Initialise the pasture model with green and dry cohorts that are
            // found as children of this component.
            if (Children.Count == 0)
                throw new Exception("There must be at least one child pasture cohort initialisation model");
            int numDry = 0;
            int numGreen = 0;
            int idx = 0;

            GreenInit[] green = new GreenInit[0];
            DryInit[] dry = new DryInit[0];

            foreach (var initCohort in Children)
            {
                if (initCohort is GreenCohortInitialise greenInit)
                {
                    Array.Resize(ref green, green.Length + 1);
                    idx = numGreen;
                    green[idx] = new GreenInit();
                    green[idx].root_wt = new double[greenInit.RootWeight.Length][];
                    for (int i = 0; i < greenInit.RootWeight.Length; i++)
                        green[idx].root_wt[i] = new double[] { greenInit.RootWeight[i] };
                    green[idx].status = greenInit.Status;
                    green[idx].rt_dep = greenInit.RootDepth;
                    green[idx].estab_index = greenInit.EstIndex;
                    green[idx].stem_reloc = greenInit.StemReloc;
                    green[idx].stress_index = greenInit.StressIndex;
                    green[idx].frosts = greenInit.Frosts;
                    green[idx].herbage = new Herbage[2]; // leaf and stem

                    // leaf
                    green[idx].herbage[0] = new Herbage(1, new TPlantElement[] { TPlantElement.N });
                    green[idx].herbage[0].dmd = greenInit.LeafDMD;
                    green[idx].herbage[0].weight = greenInit.LeafWeight;
                    green[idx].herbage[0].n_conc = greenInit.LeafNConc;
                    green[idx].herbage[0].spec_area = greenInit.LeafSpecificArea;

                    // stem
                    green[idx].herbage[1] = new Herbage(1, new TPlantElement[] { TPlantElement.N });
                    green[idx].herbage[1].dmd = greenInit.StemDMD;
                    green[idx].herbage[1].weight = greenInit.StemWeight;
                    green[idx].herbage[1].n_conc = greenInit.StemNConc;
                    green[idx].herbage[1].spec_area = greenInit.StemSpecificArea;
                    numGreen += 1;
                    this.Green = green;
                }
                else if (initCohort is DryCohortInitialise dryInit)
                {
                    Array.Resize(ref dry, dry.Length + 1);
                    idx = numDry;
                    dry[idx] = new DryInit();
                    dry[idx].status = dryInit.Status;
                    dry[idx].herbage = new Herbage[2]; // leaf and stem

                    // leaf
                    dry[idx].herbage[0] = new Herbage(1, new TPlantElement[] { TPlantElement.N });
                    dry[idx].herbage[0].dmd = dryInit.LeafDMD;
                    dry[idx].herbage[0].weight = dryInit.LeafWeight;
                    dry[idx].herbage[0].n_conc = dryInit.LeafNConc;
                    dry[idx].herbage[0].spec_area = dryInit.LeafSpecificArea;

                    // stem
                    dry[idx].herbage[1] = new Herbage(1, new TPlantElement[] { TPlantElement.N });
                    dry[idx].herbage[1].dmd = dryInit.StemDMD;
                    dry[idx].herbage[1].weight = dryInit.StemWeight;
                    dry[idx].herbage[1].n_conc = dryInit.StemNConc;
                    dry[idx].herbage[1].spec_area = dryInit.StemSpecificArea;
                    numDry += 1;
                    this.Dry = dry;
                }
                else if (initCohort is SeedCohortInitialise seedInit)
                {
                    Seeds = new SeedInit();
                    Seeds.hard_ripe = seedInit.HardRipe;
                    Seeds.hard_unripe = seedInit.HardUnripe;
                    Seeds.soft_ripe = seedInit.SoftRipe;
                    Seeds.soft_unripe = seedInit.SoftUnripe;
                }
            }

            // =========================================================

            StartSimulation();

        }


        [EventSubscribe("StartOfDay")]
        private void OnStartOfDay(object sender, EventArgs e)
        {
            InitStep();
        }

        [EventSubscribe("DoPastureWater")]
        private void OnDoPastureWater(object sender, EventArgs e)
        {
            DoPastureWater();
        }

        /// <summary>
        /// Grow pasture
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("DoActualPlantGrowth")]
        private void OnDoActualPlantGrowth(object sender, EventArgs e)
        {
            DoPastureGrowth();
        }

        [EventSubscribe("DoEndPasture")]
        private void OnDoEndPasture(object sender, EventArgs e)
        {
            EndStep();
        }

        /// <summary>
        /// At the end of the simulation
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="e">The event arguments</param>
        [EventSubscribe("EndOfSimulation")]
        private void OnEndOfSimulation(object sender, EventArgs e)
        {

        }

        #endregion

        #region Private functions ============================================

        /// <summary>
        /// This responds to the OnStartOfSimulation event.
        /// Set up the model and initial values for the pasture.
        /// </summary>
        private void StartSimulation()
        {
            FWeather = new TWeatherHandler();
            PastureModel = new TPasturePopulation();
            PastureModel.MassUnit = "g/m^2";

            Initialise(zone);   // Get links to the support models like SoilCrop

            // required at initialisation
            FWeather.fLatDegrees = locWtr.Latitude;                             // Location. South is -ve
            SetLayerProfile(soilPhysical.Thickness);                            // Layers
            InitiliaseSoilArrays();
            Value2LayerArray(soilPhysical.BD, ref F_BulkDensity);               // Soil bulk density profile Mg/m^3
            Value2LayerArray(soilPhysical.DUL, ref F_DUL);                      // Profile of water content at drained upper limit
            Value2LayerArray(soilPhysical.LL15, ref F_LL15);                    // Profile of water content at (soil) lower limit
            Value2LayerArray(soilPhysical.ParticleSizeSand, ref F_SandPropn);   // Sand content profile //// TODO: check this

            PastureModel.ReadParamsFromValues(this.Nutrients, this.Species, this.MaxRtDep, this.KL, this.LL); // initialise the model with initial values

            FInputs.InitSoil(FNoLayers);

            double[] fCampbell = new double[GrazType.MaxSoilLayers + 1];
            for (int Ldx = 1; Ldx <= FNoLayers; Ldx++)
            {
                fCampbell[Ldx] = Math.Log(15.0 / (0.1)) / Math.Log(F_DUL[Ldx] / F_LL15[Ldx]);
            }

            if (PastureModel != null)
            {
                PastureModel.SetSoilParams(FLayerProfile, F_BulkDensity, F_SandPropn, fCampbell);
                if (PastureModel.fPlant_LL[1] == 0.0)
                {
                    PastureModel.fPlant_LL = F_LL15;
                }

                // inits are [0.. based arrays
                PastureModel.ReadStateFromValues(this.LaggedDayT, this.Phenology, this.FlowerLen, this.FlowerTime, this.SencIndex, this.DormIndex, this.DormT, this.ExtinctCoeff, this.Green, this.Dry, this.Seeds, this.SeedDormTime, this.GermIndex);

                FToday = systemClock.Today.Day + (systemClock.Today.Month * 0x100) + (systemClock.Today.Year * 0x10000);    //stddate
            }
        }

        /// <summary>Initialises arrays to same length as soil layers.</summary>
        private void InitiliaseSoilArrays()
        {
            mySoilWaterUptakeAvail = new double[FNoLayers];
            mySoilNH4UptakeAvail = new double[FNoLayers];
            mySoilNO3UptakeAvail = new double[FNoLayers];
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="zone"></param>
        /// <exception cref="Exception"></exception>
        private void Initialise(Zone zone)
        {
            #region Links to soil modules =========================
            // link to soil models parameters
            soil = zone.FindInScope<Models.Soils.Soil>();
            if (soil == null)
            {
                throw new Exception($"Cannot find soil in zone {zone.Name}");
            }

            soilChemical = soil.FindInScope<Models.Soils.Chemical>();
            if (soilChemical == null)
            {
                throw new Exception($"Cannot find soil chemical in soil {soil.Name}");
            }

            soilPhysical = soil.FindInScope<IPhysical>();
            if (soilPhysical == null)
            {
                throw new Exception($"Cannot find soil physical in soil {soil.Name}");
            }

            soilCropData = soil.FindDescendant<Models.Soils.SoilCrop>(Species + "Soil");
            if (soilCropData == null)
            {
                throw new Exception($"Cannot find a soil crop parameterisation called {Species + "Soil"}");
            }

            waterBalance = soil.FindInScope<ISoilWater>();
            if (waterBalance == null)
            {
                throw new Exception($"Cannot find a water balance model in soil {soil.Name}");
            }

            nutrient = zone.FindInScope<INutrient>();
            if (nutrient == null)
            {
                throw new Exception($"Cannot find SoilNitrogen in zone {zone.Name}");
            }

            no3 = zone.FindInScope("NO3") as ISolute;
            if (no3 == null)
            {
                throw new Exception($"Cannot find NO3 solute in zone {zone.Name}");
            }

            nh4 = zone.FindInScope("NH4") as ISolute;
            if (nh4 == null)
            {
                throw new Exception($"Cannot find NH4 solute in zone {zone.Name}");
            }

            #endregion

            // initialise soil related variables
            string zoneName = soil.Parent.Name;
            int nLayers = soilPhysical.Thickness.Length;
            mySoilNH4Available = new double[nLayers];
            mySoilNO3Available = new double[nLayers];
            mySoilWaterAvailable = new double[nLayers];
            myTotalWater = new double[nLayers];

            // set some initial values
            Layers = new double[nLayers];
            Array.Copy(soilPhysical.Thickness, Layers, nLayers);
            LL = new double[nLayers];
            KL = new double[nLayers];
            Array.Copy(soilCropData.KL, KL, nLayers);
            Array.Copy(soilCropData.LL, LL, nLayers);

        }

        /// <summary>
        /// Initial step = 100
        /// Acquires the green mass, green area index and cover of other plants and computes totals for the field.
        /// </summary>
        private void InitStep()
        {
            resetDrivers();

            FToday = systemClock.Today.Day + (systemClock.Today.Month * 0x100) + (systemClock.Today.Year * 0x10000);    //stddate

            GetWtrDrivers();
            storeWeather();
            PastureModel.Inputs = FInputs;

            FFieldGreenDM = PastureModel.GetHerbageMass(sgGREEN, TOTAL, TOTAL);
            FFieldGAI = PastureModel.AreaIndex(sgGREEN);
            FFieldDAI = PastureModel.AreaIndex(sgDRY);
            FFieldCoverSum = PastureModel.Cover(TOTAL);

            GetSiblingPlants();

            PastureModel.BeginTimeStep();
            storePastureCover();

            for (int Ldx = 1; Ldx <= FNoLayers; Ldx++)
            {
                // Soil water content profile is obtained BEFORE soil water dynamics calculations are made.
                FInputs.Theta[Ldx] = water.Volumetric[Ldx - 1];  // converting from 0-based to 1-based array
                FInputs.ASW[Ldx] = (FInputs.Theta[Ldx] - F_LL15[Ldx]) / (F_DUL[Ldx] - F_LL15[Ldx]);
                FInputs.ASW[Ldx] = Math.Max(0.0, Math.Min(FInputs.ASW[Ldx], 1.0));
                FInputs.WFPS[Ldx] = FInputs.Theta[Ldx] / (1.0 - F_BulkDensity[Ldx] / 2.65);
            }
        }

        /// <summary>
        /// Store the pasture cover for this Field
        /// </summary>
        private void storePastureCover()
        {
            PastureModel.PastureGreenDM = FFieldGreenDM;
            PastureModel.PastureGAI = FFieldGAI;
            PastureModel.PastureAreaIndex = FFieldGAI + FFieldDAI;
            PastureModel.PastureCoverSum = FFieldCoverSum;
        }

        /// <summary>
        /// Get values from sibling components
        /// </summary>
        private void GetSiblingPlants()
        {
            // get values from sibling components
            foreach (ICanopy amodel in zone.FindAllDescendants<ICanopy>())
            {
                if (amodel != this)
                {
                    FFieldCoverSum += amodel.CoverTotal;
                    FFieldGreenDM += this.GreenDM; // TODO: this needs to find values from AboveGroundLiveWt in pastures and crops
                    FFieldGAI += amodel.LAI;
                    FFieldDAI += amodel.LAITotal - amodel.LAI;
                }
            }
        }

        /// <summary>
        /// Store the weather driving values found in the met component
        /// </summary>
        private void GetWtrDrivers()
        {
            FInputs.CO2_PPM = locWtr.CO2;           // atmospheric CO2 ppm
            FInputs.MaxTemp = locWtr.MaxT;
            FInputs.Precipitation = locWtr.Rain;
            FInputs.MinTemp = locWtr.MinT;
            FInputs.Radiation = locWtr.Radn;
            FInputs.Windspeed = locWtr.Wind;
            FInputs.VP_Deficit = locWtr.VPD * 0.1;  // to kPa
            // TODO: FInputs.SurfaceEvap = ;    // Evaporation rate of free surface water (including water intercepted on herbage) mm
        }

        /// <summary>
        /// </summary>
        private void DoPastureWater()
        {

        }

        /// <summary>
        /// Do the growth = 6000
        /// Computes rates of development, growth and digestibility change of the species. Updates phenology state variables
        /// </summary>
        private void DoPastureGrowth()
        {
            FFieldGreenDM = PastureModel.GetHerbageMass(sgGREEN, TOTAL, TOTAL);
            FFieldGAI = PastureModel.AreaIndex(sgGREEN);
            FFieldDAI = PastureModel.AreaIndex(sgDRY);
            FFieldCoverSum = PastureModel.Cover(TOTAL);

            // Animal drivers - trampling is in kg/ha
            // Find the stock component and request the Trampling value for the paddock that this pasture is in.
            FInputs.TrampleRate = 0.0;
            if (stockModel != null)
                FInputs.TrampleRate = stockModel.TramplingMass(zone.Name) / 10000.0;   // ha -> m^2

            if (PastureModel.ElementSet.Length > 0)
            {
                // This pasture has nutrient drivers. NH4, NO3, P, S
                // Use the values calculated by the arbitrator
                LayerArrayMass2SoilNutrient(mySoilNH4UptakeAvail, ref FInputs.Nutrients[(int)TPlantNutrient.pnNH4]);   // Soil ammonium availability
                LayerArrayMass2SoilNutrient(mySoilNO3UptakeAvail, ref FInputs.Nutrients[(int)TPlantNutrient.pnNO3]);
                // P
                // S
            }

            //Soil pH profile. Default value is 7.0 in all layers
            Array.Copy(soilChemical.PH, 0, FInputs.pH, 1, soilChemical.PH.Length);

            storePastureCover();   // passes input values to the model

            // initialise the 3D array. This is the FSupply calculated by the arbitrator being sent for uptake.
            int x = Enum.GetNames(typeof(TPlantNutrient)).Length;
            double[][][] fSupply = new double[x][][];   // g/m^2
            for (int i = 0; i < x; i++)
            {
                fSupply[i] = new double[MAXNUTRAREAS][];
                for (int j = 0; j < MAXNUTRAREAS; j++)
                {
                    fSupply[i][j] = new double[this.FNoLayers + 1];
                }

                for (int layer = 1; layer <= FNoLayers; layer++)
                {
                    if (i == (int)TPlantNutrient.pnNO3)
                        fSupply[i][0][layer] = mySoilNO3UptakeAvail[layer - 1] * KGHA_GM2;
                    else if (i == (int)TPlantNutrient.pnNH4)
                        fSupply[i][0][layer] = mySoilNH4UptakeAvail[layer - 1] * KGHA_GM2;
                }
            }

            PastureModel.ComputeRates(fSupply, myWaterDemand);    // main growth update function
        }

        /// <summary>
        /// Publish biomass values = 9900
        /// Adds residue inputs from other models. Updates remaining state variables
        /// </summary>
        private void EndStep()
        {
            PastureModel.UpdateState();

            // soil residue destination
            BiomassToFOM(); // evtADD_FOM

            BiomassRemoved removed = TransferLitter();  // evtBIOMASS_OUT
            if (removed != null && surfaceOrganicMatter != null)
            {
                // leaf and stem
                for (int part = 0; part <= 1; part++)
                {
                    surfaceOrganicMatter.Add(removed.dltCropDM[part], removed.dltDM_N[part], removed.dltDM_P[part], /*removed.DMType[part]*/"pasture", removed.CropType);
                }
            }

            no3.AddKgHaDelta(SoluteSetterType.Plant, MathUtilities.Multiply_Value(mySoilNO3UptakeAvail, -1));
            nh4.AddKgHaDelta(SoluteSetterType.Plant, MathUtilities.Multiply_Value(mySoilNH4UptakeAvail, -1));
            //mySoilWaterAvailable = MathUtilities.Multiply_Value(mySoilWaterAvailable, -1.0);
            waterBalance.RemoveWater(mySoilWaterAvailable);

        }

        /// <summary>Average carbon content in plant dry matter (kg/kg).</summary>
        private const double carbonFractionInDM = 0.4;

        /// <summary>
        /// Transfer the residues to the soil
        /// </summary>
        private void BiomassToFOM()
        {
            string[] sFOMTypes = { "", "pasture_root", "pasture_leaf", "pasture_stem" };

            PastureFOMType removed = new PastureFOMType();
            removed.FOMTypes = new string[3];
            for (int Idx = 1; Idx <= 3; Idx++)
                removed.FOMTypes[Idx-1] = sFOMTypes[Idx];

            removed.Layers = new double[FNoLayers + 1];
            for (int Ldx = 1; Ldx <= FNoLayers; Ldx++)
                removed.Layers[Ldx] = FLayerProfile[Ldx]; // [1..

            removed.FOM = new OrganicMatter[FNoLayers+1][];
            removed.FOM[1] = new OrganicMatter[3];          // first layer has space for leaf and stem residue
            for (int i = 0; i < 3; i++)
                removed.FOM[1][i] = new OrganicMatter();

            for (int Ldx = 2; Ldx <= FNoLayers; Ldx++)
            {
                // further root layers
                removed.FOM[Ldx] = new OrganicMatter[1];
                removed.FOM[Ldx][0] = new OrganicMatter();
            }

            if (PastureModel != null)
            {
                string sPrevUnit = PastureModel.MassUnit;
                PastureModel.MassUnit = "kg/ha";
                for (int Ldx = 1; Ldx <= FNoLayers; Ldx++)                                                 // Root residues
                {
                    DMPool2Value(PastureModel.GetResidueFlux(ptROOT, Ldx), ref removed.FOM[Ldx][0]);
                }

                // Surface residues all go into the first soil layer
                DMPool2Value(PastureModel.GetResidueFlux(ptLEAF), ref removed.FOM[1][1]);       // leaf
                DMPool2Value(PastureModel.GetResidueFlux(ptSTEM), ref removed.FOM[1][2]);       // stem

                PastureModel.MassUnit = sPrevUnit;

                FOMLayerType FOMData = new FOMLayerType();
                FOMData.Type = this.Species;
                FOMData.Layer = new FOMLayerLayerType[FNoLayers];

                // Fill a FOMLayerType and do the Incorp

                // root layers
                for (int layer = 0; layer < FNoLayers; layer++)
                {
                    FOMData.Layer[layer] = new FOMLayerLayerType();
                    FOMData.Layer[layer].FOM = new FOMType();
                    FOMData.Layer[layer].FOM.amount = removed.FOM[layer + 1][0].Weight;
                    FOMData.Layer[layer].FOM.N = removed.FOM[layer + 1][0].N;
                    FOMData.Layer[layer].FOM.C = FOMData.Layer[layer].FOM.amount * carbonFractionInDM;
                    FOMData.Layer[layer].FOM.P = removed.FOM[layer + 1][0].P;
                    FOMData.Layer[layer].FOM.AshAlk = removed.FOM[layer + 1][0].AshAlk;

                    FOMData.Layer[layer].CNR = 0.0;        // not used here
                    FOMData.Layer[layer].LabileP = 0.0;    // not used here
                }

                // surface residues into first layer
                FOMData.Layer[0].FOM.amount += removed.FOM[1][1].Weight + removed.FOM[1][2].Weight;
                FOMData.Layer[0].FOM.N += removed.FOM[1][1].N + removed.FOM[1][2].N;
                FOMData.Layer[0].FOM.C = FOMData.Layer[0].FOM.amount * carbonFractionInDM;
                FOMData.Layer[0].FOM.P += removed.FOM[1][1].P + removed.FOM[1][2].P;
                FOMData.Layer[0].FOM.AshAlk += removed.FOM[1][1].AshAlk + removed.FOM[1][2].AshAlk; // ?

                nutrient.DoIncorpFOM(FOMData);
            }
        }

        /// <summary>
        /// Get the litter dry matter that is going to SOM
        /// </summary>
        /// <returns>Biomass removed in kg/ha</returns>
        private BiomassRemoved TransferLitter()
        {
            string[] sPartName = { "", "leaf", "stem" };

            BiomassRemoved removed = null;
            if (PastureModel != null)
            {
                if ((PastureModel.GetHerbageMass(stLITT1, TOTAL, TOTAL) + PastureModel.GetHerbageMass(stLITT2, TOTAL, TOTAL)) > 0.0)
                {
                    string sPrevUnit = PastureModel.MassUnit;
                    PastureModel.MassUnit = "kg/ha";

                    removed = new BiomassRemoved(2); // leaf and stem
                    removed.CropType = this.Species;

                    for (int iPart = ptLEAF; iPart <= ptSTEM; iPart++)
                    {
                        removed.DMType[iPart - 1] = sPartName[iPart];
                        removed.dltCropDM[iPart - 1] = PastureModel.GetHerbageMass(stLITT1, iPart, TOTAL) + PastureModel.GetHerbageMass(stLITT2, iPart, TOTAL);
                        removed.dltDM_N[iPart - 1] = PastureModel.GetHerbageNutr(stLITT1, iPart, TOTAL, TPlantElement.N) + PastureModel.GetHerbageNutr(stLITT2, iPart, TOTAL, TPlantElement.N);
                        removed.dltDM_P[iPart - 1] = PastureModel.GetHerbageNutr(stLITT1, iPart, TOTAL, TPlantElement.P) + PastureModel.GetHerbageNutr(stLITT2, iPart, TOTAL, TPlantElement.P);
                        removed.FractionToResidue[iPart - 1] = 1.0;

                        for (int Idx = 1; Idx <= HerbClassNo; Idx++)
                        {
                            PastureModel.SetHerbageMass(stLITT1, iPart, Idx, 0.0);
                            PastureModel.SetHerbageMass(stLITT2, iPart, Idx, 0.0);
                        }
                    }

                    PastureModel.MassUnit = sPrevUnit;
                }
            }

            return removed;
        }

        /// <summary>
        /// Store the nutrient from a layer array (kg/ha) in a SoilNutrientDistn
        /// </summary>
        /// <param name="LayerA_mass"></param>
        /// <param name="Nutrient">Dist of soil nutrients</param>
        private void LayerArrayMass2SoilNutrient(double[] LayerA_mass, ref TSoilNutrientDistn Nutrient)
        {
            Nutrient = new TSoilNutrientDistn();
            Nutrient.NoAreas = 1;
            for (int Ldx = 1; Ldx <= FNoLayers; Ldx++)
            {
                // [0] is the surface
                Nutrient.RelAreas[0] = 1.0;
                Nutrient.AvailKgHa[0][Ldx] = LayerA_mass[Ldx - 1];
                Nutrient.SolnPPM[0][Ldx] = LayerA_mass[Ldx - 1] * 100.0 / Layers[Ldx - 1] / FInputs.Theta[Ldx];
            }
        }

        /// <summary>
        /// Computes the derived values that go to make up the FInputs record for weather
        /// </summary>
        private void storeWeather()
        {
            if (FWeather != null)
            {
                FWeather.setToday(StdDate.DayOf(FToday), StdDate.MonthOf(FToday), StdDate.YearOf(FToday));  // also clears FWeather data list
                FWeather[TWeatherData.wdtRain] = FInputs.Precipitation;
                FWeather[TWeatherData.wdtMaxT] = FInputs.MaxTemp;
                FWeather[TWeatherData.wdtMinT] = FInputs.MinTemp;
                FWeather[TWeatherData.wdtRadn] = FInputs.Radiation;
                FWeather[TWeatherData.wdtWind] = FInputs.Windspeed;
                FWeather[TWeatherData.wdtEpan] = locWtr.PanEvap;
                FWeather[TWeatherData.wdtVP] = locWtr.VP;

                FInputs.MeanTemp = FWeather.MeanTemp();
                FInputs.MeanDayTemp = FWeather.MeanDayTemp();
                FInputs.DayLength = FWeather.Daylength(true);
                FInputs.DayLenIncreasing = FWeather.DaylengthIncreasing();
                FInputs.PotentialET = FWeather.PotentialET(GrazEnv.HERBAGE_ALBEDO);       // Reference evapotranspiration mm

                if (FInputs.Precipitation > 0.0)
                    FInputs.RainIntercept = Math.Min(1.0, FIntercepted / FInputs.Precipitation);
                else
                    FInputs.RainIntercept = 1.0;
            }
            else
                throw new Exception("FWeather is null in Pasture.storeWeather().");
        }

        /// <summary>
        /// Init the input values
        /// </summary>
        private void resetDrivers()
        {
            FSoilPropn = new double[GrazType.stSENC + 1][];
            FTranspiration = new double[GrazType.stSENC + 1][];
            for (int i = 0; i <= GrazType.stSENC; i++)
            {
                FSoilPropn[i] = new double[MaxSoilLayers + 1];
                FTranspiration[i] = new double[MaxSoilLayers + 1];
            }
        }

        /// <summary>Pasture herbage cohort component name</summary>
        public static string[] sCOMPNAME = { "", "seedling", "established", "senescing", "dead", "litter" };    // [0, stSEEDL..stLITT1]  - [0, 1..5]

        /// <summary>
        /// Called when a water arbitrator has calculated water uptakes
        /// It gives the total supply available for each cohort, for each soil layer.
        /// </summary>
        /// <param name="layerWater"></param>
        private void setCohortWaterSupply(double[] layerWater)
        {

            double[] fTotalRLD = PastureModel.EffRootLengthD(sgGREEN);

            for (int Idx = stSEEDL; Idx <= stSENC; Idx++)
            {
                double[] fCompRLD = PastureModel.EffRootLengthD(Idx);

                for (int Jdx = 1; Jdx <= layerWater.Length; Jdx++)
                {
                    double fRootPropn = PastureUtil.Div0(fCompRLD[Jdx], fTotalRLD[Jdx]);
                    FTranspiration[Idx][Jdx] = layerWater[Jdx - 1] * fRootPropn;
                }
            }
        }

        /// <summary></summary>
        protected const double EPS = 1.0E-5;

        /// <summary>Conversion g/m^2 to kg/ha. 1 g/m^2 = 10 kg/ha</summary>
        protected const double GM2_KGHA = 10.0;
        /// <summary>Conversion kg/ha to g/m^2</summary>
        protected const double KGHA_GM2 = 0.1;

        /// <summary>
        ///
        /// </summary>
        /// <param name="Layers1"></param>
        /// <param name="iNoLayers1"></param>
        /// <param name="Values1"></param>
        /// <param name="Layers2"></param>
        /// <param name="iNoLayers2"></param>
        /// <param name="Values2"></param>
        protected void MakeLayersAsAverage(double[] Layers1,
                               int iNoLayers1,
                               double[] Values1,
                               double[] Layers2,
                               int iNoLayers2,
                               ref double[] Values2)
        {
            int iSameLayers;
            double fTopDepth;
            double fBaseDepth1;
            double fBaseDepth2;
            int Ldx1, Ldx2;

            Values2 = new double[GrazType.MaxSoilLayers + 1];

            // Deal quickly with the case where layers are the same
            Ldx1 = 1;
            while ((Ldx1 <= iNoLayers1)
                  && (Ldx1 <= iNoLayers2)
                  && (Math.Abs(Layers1[Ldx1] - Layers2[Ldx1]) < 2.0 * EPS))
            {
                Values2[Ldx1] = Values1[Ldx1];
                Ldx1++;
            }
            iSameLayers = Ldx1 - 1;

            if (iSameLayers < iNoLayers2)
            {
                // We have found a layer mismatch.
                fTopDepth = 0.0;
                for (Ldx1 = 1; Ldx1 <= iSameLayers; Ldx1++)
                {
                    fTopDepth = fTopDepth + Layers1[Ldx1];
                }

                fBaseDepth1 = fTopDepth + Layers1[iSameLayers + 1];
                fBaseDepth2 = fTopDepth + Layers2[iSameLayers + 1];

                Ldx1 = iSameLayers + 1;
                Ldx2 = iSameLayers + 1;
                while ((Ldx1 <= iNoLayers1) && (Ldx2 <= iNoLayers2))
                {
                    if (fBaseDepth1 < fBaseDepth2 + EPS)
                    {
                        // Base of data layer shallower than or equal to base of soil budget layer
                        Values2[Ldx2] = Values2[Ldx2] + Values1[Ldx1] * (fBaseDepth1 - fTopDepth);

                        Ldx1++;
                        fTopDepth = fBaseDepth1;
                        fBaseDepth1 = fTopDepth + Values1[Ldx1];

                        if (Math.Abs(fBaseDepth1 - fBaseDepth2) < 2.0 * EPS)
                        {
                            // Layer bases at same depth - increment both
                            Ldx2++;
                            fBaseDepth2 = fBaseDepth2 + Layers2[Ldx2];
                        }
                    }
                    else
                    {
                        // Base of data layer deeper than base of soil budget layer
                        Values2[Ldx2] = Values2[Ldx2] + Values1[Ldx1] * (fBaseDepth2 - fTopDepth);
                        fTopDepth = fBaseDepth2;
                        Ldx2++;
                        fBaseDepth2 = fBaseDepth2 + Layers2[Ldx2];
                    }

                    // Complete the average
                    for (Ldx2 = iSameLayers + 1; Ldx2 <= iNoLayers2; Ldx2++)
                    {
                        Values2[Ldx2] = Values2[Ldx2] / Layers2[Ldx2];
                    }
                } // _ if (iSameLayers < Model.Params.LastLayer) _
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="Layers1"></param>
        /// <param name="iNoLayers1"></param>
        /// <param name="Values1"></param>
        /// <param name="Layers2"></param>
        /// <param name="iNoLayers2"></param>
        /// <param name="Values2"></param>
        protected void MakeLayersAsFlow(double[] Layers1, int iNoLayers1,
                               double[] Values1,
                               double[] Layers2, int iNoLayers2,
                               ref double[] Values2)
        {
            double fBaseDepth1;
            double fBaseDepth2;
            int Ldx1, Ldx2;

            Values2 = new double[GrazType.MaxSoilLayers + 1];

            Ldx1 = 0;
            fBaseDepth1 = 0.0;
            fBaseDepth2 = 0.0;
            for (Ldx2 = 1; Ldx2 <= iNoLayers2; Ldx2++)
            {
                fBaseDepth2 = fBaseDepth2 + Layers2[Ldx2];
                while ((Ldx1 <= iNoLayers1) && (fBaseDepth2 > fBaseDepth1 + EPS))
                {
                    Ldx1++;
                    fBaseDepth1 = fBaseDepth1 + Layers1[Ldx1];
                }
                Values2[Ldx2] = Values1[Ldx1];
            }
        }

        /// <summary>
        /// Store proportion of the soil volume occupied by roots for each of this pastures cohorts
        /// </summary>
        private void StoreSoilPropn()
        {
            for (int layer = 1; layer <= soilPhysical.Thickness.Length; layer++)
                if (mySoilWaterAvailable[layer - 1] > 0)
                    for (int iComp = stSEEDL; iComp <= stSENC; iComp++)
                        FSoilPropn[iComp][layer] = 1.0;
        }

        #endregion

        #region Arbitration functions ==============================================

        private double myWaterDemand; // Amount of water demanded by the plant(mm)

        /// <summary>Minimum significant difference between two values.</summary>
        internal const double Epsilon = 0.000000001;

        /// <summary>Amount of N demanded from the soil (kg/ha).</summary>
        private double mySoilNDemand;

        /// <summary>Amount of soil NH4-N available to be taken up by the plant (kg/ha).</summary>
        private double[] mySoilNH4UptakeAvail;

        /// <summary>Amount of soil NO3-N available to be taken up by the plant (kg/ha).</summary>
        private double[] mySoilNO3UptakeAvail;

        /// <summary>Finds out the amount of plant available water in the soil.</summary>
        /// <param name="myZone">The soil information</param>
        internal void EvaluateSoilWaterAvailability(ZoneWaterAndN myZone)
        {

            for (int layer = 0; layer < soilPhysical.Thickness.Length; layer++)
            {
                bool rootsInLayer = false;
                myTotalWater[layer] = Math.Max(0.0, myZone.Water[layer]);

                for (int iComp = stSEEDL; iComp <= stSENC; iComp++)
                {
                    if (PastureModel.WaterDemand(iComp,myWaterDemand) > 0.0)
                    {
                        double[] fRootLD = PastureModel.EffRootLengthD(iComp);
                        if (fRootLD[layer + 1] > 0)
                            rootsInLayer = true;
                    }
                }


                if (rootsInLayer)
                    mySoilWaterAvailable[layer] = Math.Max(0.0, myZone.Water[layer] - soilCropData.LLmm[layer]) * soilCropData.KL[layer];
                else
                    mySoilWaterAvailable[layer] = 0;
                //mySoilWaterAvailable[layer] *= FractionLayerWithRoots(layer) * soilCropData.KL[layer] * KLModiferDueToDamage(layer); */
            }
        }

        /// <summary>Finds out the amount of plant available nitrogen (NH4 and NO3) in the soil.</summary>
        /// <param name="myZone">The soil information from the zone that contains the roots.</param>
        internal void EvaluateSoilNitrogenAvailability(ZoneWaterAndN myZone)
        {
            Array.Clear(this.mySoilNH4Available);
            Array.Clear(this.mySoilNO3Available);

            for (int iComp = stSEEDL; iComp <= stSENC; iComp++)
            {
                double[][][] fSupply = PastureModel.ComputeNutrientUptake2(iComp, TPlantElement.N, myZone);
                for (int iLayer = 0; iLayer < FNoLayers; iLayer++)
                {
                    this.mySoilNH4Available[iLayer] += fSupply[(int)TPlantNutrient.pnNH4][0][iLayer + 1] * GM2_KGHA;
                    this.mySoilNO3Available[iLayer] += fSupply[(int)TPlantNutrient.pnNO3][0][iLayer + 1] * GM2_KGHA;
                }
            }
        }

        /// <summary>Gets the potential plant N uptake for each layer (mm).</summary>
        /// <param name="soilstate">The soil state (current N contents).</param>
        /// <returns>The potential N uptake (kg/ha).</returns>
        public List<ZoneWaterAndN> GetNitrogenUptakeEstimates(SoilState soilstate)
        {
            bool IsAlive = true;
            if (IsAlive)
            {
                // Calculate the demand
                double maxDemand = 0;   // g/m^2
                double critDemand = 0;  // g/m^2
                PastureModel.ComputeNutrientRatesEstimate(TPlantElement.N, ref maxDemand, ref critDemand, myWaterDemand);

                double NSupply = 0.0;  //NOTE: This is in kg, not kg/ha, to arbitrate N demands for spatial simulations.
                List<ZoneWaterAndN> zones = new List<ZoneWaterAndN>();

                foreach (ZoneWaterAndN zone in soilstate.Zones)
                {

                    ZoneWaterAndN UptakeDemands = new ZoneWaterAndN(zone.Zone);
                    zones.Add(UptakeDemands);

                    EvaluateSoilNitrogenAvailability(zone); // get the N amount available in the soil

                    UptakeDemands.NO3N = this.mySoilNO3Available;    // kg/ha
                    UptakeDemands.NH4N = this.mySoilNH4Available;
                    UptakeDemands.Water = new double[zone.NO3N.Length];

                    NSupply += (this.mySoilNH4Available.Sum() + this.mySoilNO3Available.Sum()) * zone.Zone.Area; //NOTE: This is in kg, not kg/ha

                }

                // kg/ha
                mySoilNDemand = maxDemand * GM2_KGHA;

                // get the amount of soil N demanded
                double NDemand = mySoilNDemand * zone.Area; //NOTE: This is in kg, not kg/ha, to arbitrate N demands for spatial simulations.

                // estimate fraction of N used up
                double fractionUsed = 0.0;
                if (NSupply > Epsilon)
                {
                    fractionUsed = Math.Min(1.0, NDemand / NSupply);
                }

                this.mySoilNH4UptakeAvail = MathUtilities.Multiply_Value(mySoilNH4Available, fractionUsed);
                this.mySoilNO3UptakeAvail = MathUtilities.Multiply_Value(mySoilNO3Available, fractionUsed);

                // reduce the PotentialUptakes that we pass to the soil arbitrator
                foreach (ZoneWaterAndN UptakeDemands in zones)
                {
                    UptakeDemands.NO3N = MathUtilities.Multiply_Value(UptakeDemands.NO3N, fractionUsed);
                    UptakeDemands.NH4N = MathUtilities.Multiply_Value(UptakeDemands.NH4N, fractionUsed);
                }

                return zones;
            }
            else
                return null;
        }

        /// <summary>Sets the amount of water taken up by this plant (mm).</summary>
        /// <param name="zones">The water uptake from each layer (mm), by zone.</param>
        public void SetActualWaterUptake(List<ZoneWaterAndN> zones)
        {
            Array.Clear(mySoilWaterUptakeAvail, 0, WaterUptake.Count);

            foreach (ZoneWaterAndN zone in zones)
            {
                if (zone.Zone.Name == this.zone.Name)
                {
                    // Note: The uptake is done during computeRates()
                    mySoilWaterUptakeAvail = MathUtilities.Add(mySoilWaterUptakeAvail, zone.Water);
                }
            }

            // Proportion of the soil volume occupied by roots of this plant population
            StoreSoilPropn();
            setCohortWaterSupply(mySoilWaterUptakeAvail);   // set the water available for each cohort via FTranspiration
            for (int iComp = stSEEDL; iComp <= stSENC; iComp++)
            {
                PastureModel.SetSoilPropn(iComp, FSoilPropn[iComp]);
                PastureModel.SetTranspiration(iComp, FTranspiration[iComp]);
            }
        }

        /// <summary>Sets the amount of N taken up by this plant (kg/ha).</summary>
        /// <param name="zones">The N uptake from each layer (kg/ha), by zone.</param>
        public void SetActualNitrogenUptakes(List<ZoneWaterAndN> zones)
        {
            Array.Clear(mySoilNH4UptakeAvail, 0, mySoilNH4UptakeAvail.Length);
            Array.Clear(mySoilNO3UptakeAvail, 0, mySoilNO3UptakeAvail.Length);

            foreach (ZoneWaterAndN zone in zones)
            {
                if (zone.Zone.Name == this.zone.Name)
                {
                    // Note: The uptake is done during computeRates() !
                    mySoilNH4UptakeAvail = MathUtilities.Add(mySoilNH4UptakeAvail, zone.NH4N);
                    mySoilNO3UptakeAvail = MathUtilities.Add(mySoilNO3UptakeAvail, zone.NO3N);
                }
            }
        }

        /// <summary>Gets the potential plant water uptake for each layer (mm). Used by the Soil Arbitrator</summary>
        /// <remarks>The model can only handle one root zone at present.</remarks>
        /// <param name="soilstate">The soil state (current water content).</param>
        /// <returns>The potential water uptake (mm).</returns>
        public List<ZoneWaterAndN> GetWaterUptakeEstimates(SoilState soilstate)
        {
            bool IsAlive = true;
            if (IsAlive)
            {
                // 1. get all water supplies.
                double waterSupply = 0;  //NOTE: This is in L, not mm, to arbitrate water demands for spatial simulations.

                List<double[]> supplies = new List<double[]>();
                List<Zone> zones = new List<Zone>();
                foreach (ZoneWaterAndN zone in soilstate.Zones)
                {
                    if (zone.Zone.Name == this.zone.Name)
                    {
                        // get the amount of water available in the soil
                        EvaluateSoilWaterAvailability(zone);

                        supplies.Add(mySoilWaterAvailable);
                        zones.Add(zone.Zone);
                        waterSupply += mySoilWaterAvailable.Sum() * zone.Zone.Area;
                    }
                }

                // 2. get the amount of soil water demanded NOTE: This is in L, not mm,
                Zone parentZone = FindAncestor<Zone>();
                double waterDemand = WaterDemand * parentZone.Area;

                // 3. estimate fraction of water used up
                double fractionUsed = 0.0;
                if (waterSupply > Epsilon)
                {
                    fractionUsed = Math.Min(1.0, waterDemand / waterSupply);
                }

                // 4. apply demand supply ratio to each zone and create a ZoneWaterAndN structure to return to caller.
                List<ZoneWaterAndN> ZWNs = new List<ZoneWaterAndN>();
                for (int i = 0; i < supplies.Count; i++)
                {
                    // just send uptake from my zone
                    ZoneWaterAndN uptake = new ZoneWaterAndN(zones[i]);
                    uptake.Water = MathUtilities.Multiply_Value(supplies[i], fractionUsed);
                    uptake.NO3N = new double[uptake.Water.Length];
                    uptake.NH4N = new double[uptake.Water.Length];
                    ZWNs.Add(uptake);
                }

                return ZWNs;
            }
            else
            {
                return null;
            }
        }
        #endregion

        #region Management methods ============================================


        /// <summary>
        /// Adds a given amount of seed of the species.
        /// The new seed is assumed to be immediately germinable.
        /// In species that are modelled as not having seed pools, the sown material is added as established plants
        /// </summary>
        /// <param name="rate">Amount of seed sown. kg/ha</param>
        public void Sow(double rate)
        {
            PastureModel.Sow(rate * KGHA_GM2);      // Convert kg/ha to g/m^2
        }

        /// <summary>
        /// This event causes the pasture to mimic the effects of spraying with glyphosate
        /// </summary>
        public void SprayTop()
        {
            PastureModel.SprayTop();
        }

        /// <summary>
        /// Kills the nominated proportion of the sward and incorporates the newly-dead material,
        /// along with the nominated proportion of dead, litter and seeds, into the soil.
        /// </summary>
        /// <param name="depth">Depth of cultivation. mm</param>
        /// <param name="propnIncorp">Proportion of surface herbage incorporated into the soil</param>
        public void Cultivate(double depth, double propnIncorp)
        {
            PastureModel.Cultivate(propnIncorp, depth);
        }

        /// <summary>
        /// Conserve fodder
        /// </summary>
        public event Supplement.ConserveSuppDelegate Conserve;

        /// <summary>
        /// Removes all herbage down to a nominated threshold and makes it available for storage as hay.
        /// </summary>
        /// <param name="cutHeight">Height of cutting. mm</param>
        /// <param name="storeName">Name of the store (supplement component) that will contain the cut forage. Will move it directly to the storage if a name is supplied.</param>
        /// <param name="gathered">Proportion of cut forage gathered in (the remainder becomes litter).</param>
        /// <param name="dmdLoss">Loss of DMD during cutting, drying and storage</param>
        /// <param name="dmContent">Dry matter content when stored. kg/kg</param>
        public void Cut(double cutHeight, string storeName = "fodder", double gathered = 1.0, double dmdLoss = 0.02, double dmContent = 0.90)
        {
            double hayFW = 0;

            if (dmContent == 0.0)
                dmContent = 0.90;

            double leftPropn;
            if (cutHeight >= PastureModel.Height_MM())
                leftPropn = 1.0;
            else
                leftPropn = cutHeight / PastureModel.Height_MM();
            FoodSupplement HayComposition = new FoodSupplement();
            PastureModel.Conserve(leftPropn * PastureModel.GetHerbageMass(sgSTANDING, TOTAL, TOTAL),
                                  leftPropn * PastureModel.GetHerbageMass(stLITT1, TOTAL, TOTAL),
                                  gathered, dmdLoss, dmContent, ref hayFW, ref HayComposition);

            // if a Supplement component is found and the storeName is valid then conserve this fodder in the store.
            if ((suppFeed != null) && (gathered > 0))
            {
                ConserveType conserve = new ConserveType();
                conserve.Name = storeName;
                conserve.FreshWt = GM2_KGHA * hayFW * FFieldArea;
                conserve.DMContent = HayComposition.dmPropn;
                conserve.DMD = HayComposition.dmDigestibility;
                conserve.NConc = HayComposition.CrudeProt / N2Protein;
                conserve.PConc = HayComposition.Phosphorus;
                conserve.SConc = HayComposition.Sulphur;
                conserve.AshAlk = HayComposition.AshAlkalinity;

                if (Conserve != null)
                    Conserve.Invoke(conserve);
            }
        }

        /// <summary>
        /// Kills the nominated proportions of herbage (including roots) and seeds.
        /// When killed, green herbage becomes standing dead and roots become residues
        /// </summary>
        /// <param name="propnHerbage">Proportion of herbage to be killed</param>
        /// <param name="propnSeed">Proportion of seeds to be killed</param>
        public void Kill(double propnHerbage = 1, double propnSeed = 1)
        {
            PastureModel.Kill(propnHerbage, propnSeed);
        }

        /// <summary>
        /// Simulates the effect of a fire; equivalent to a kill event followed by removal of a proportion of the herbage.
        /// Surviving, killed and already-dead herbage are removed in equal proportions
        /// </summary>
        /// <param name="killPlants">Proportion of herbage killed by the fire.</param>
        /// <param name="killSeed">Proportion of seeds killed by the fire.</param>
        /// <param name="propnUnburnt">Proportion of herbage (green and dead) that remains after the fire has passed. </param>
        public void Burn(double killPlants, double killSeed, double propnUnburnt)
        {
            double hayFW = 0;

            PastureModel.Kill(killPlants, killSeed);

            FoodSupplement HayComposition = new FoodSupplement();
            PastureModel.Conserve(propnUnburnt * PastureModel.GetHerbageMass(sgSTANDING, TOTAL, TOTAL),
                                  propnUnburnt * PastureModel.GetHerbageMass(stLITT1, TOTAL, TOTAL),
                                  1.0, 0.0, 1.0, ref hayFW, ref HayComposition);
        }

        /// <summary>
        /// Remove herbage and seed in each pool.
        /// </summary>
        /// <param name="removing">Each class of herbage and seed to remove. [GrazType.DigClassNo]</param>
        public void RemoveHerbage(PastureRemoval removing)
        {
            double[] HerbageRemoved = new double[GrazType.DigClassNo + 1];
            double[] SeedRemoved = new double[GrazType.RIPE + 1];

            for (int Idx = 1; Idx <= GrazType.DigClassNo; Idx++)
                HerbageRemoved[Idx] = removing.herbage[Idx - 1] * KGHA_GM2;
            for (int Idx = UNRIPE; Idx <= RIPE; Idx++)
                SeedRemoved[Idx] = removing.seed[Idx - 1] * KGHA_GM2;

            PastureModel.PassRemoval(HerbageRemoved, SeedRemoved);
        }

        #endregion
    }
}
