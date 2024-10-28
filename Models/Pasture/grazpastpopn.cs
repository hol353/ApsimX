using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.PostSimulationTools;
using Models.Soils.Arbitrator;
using StdUnits;
using static Models.GrazPlan.GrazType;
using static Models.GrazPlan.PastureUtil;

namespace Models.GrazPlan
{
    /// <summary>
    /// This record is fed into TPasturePopulation objects before calling the
    /// WaterDemand and computeRates methods.  It contains all the inputs
    /// required by these two methods.
    /// </summary>
    [Serializable]
    public class TPastureInputs
    {
        /// <summary>Gets or sets the Maximum air temperature (deg C)</summary>
        public double MaxTemp { get; set; }

        /// <summary>Gets or sets the Minimum air temperature (deg C)</summary>
        public double MinTemp { get; set; }

        /// <summary>Gets or sets the Mean of MaxTemp and MinTemp</summary>
        public double MeanTemp { get; set; }

        /// <summary>Gets or sets the Mean temperature during daylight hours</summary>
        public double MeanDayTemp { get; set; }

        /// <summary>Gets or sets the Rainfall</summary>
        public double Precipitation { get; set; }

        /// <summary>Gets or sets the Potential evapotranspiration (mm H2O)</summary>
        public double PotentialET { get; set; }

        /// <summary>Gets or sets the Vapour pressure deficit (kPa)</summary>
        public double VP_Deficit { get; set; }

        /// <summary>Gets or sets the Daily solar radiation (MJ/m2)</summary>
        public double Radiation { get; set; }

        /// <summary>Gets or sets the Wind speed at 2m (m/s)</summary>
        public double Windspeed { get; set; }

        /// <summary>Gets or sets the Daylength including civil twilight (hr)</summary>
        public double DayLength { get; set; }

        /// <summary>Gets or sets the increasing daylength. TRUE if day length is increasing</summary>
        public bool DayLenIncreasing { get; set; }

        /// <summary>Gets or sets the CO2 parts/million</summary>
        public double CO2_PPM { get; set; }

        /// <summary>Soil water content (mm/mm)</summary>
        public double[] Theta;

        /// <summary>(SW-WP)/(DUL-WP)  (-)</summary>
        public double[] ASW;

        /// <summary>SW/SAT  (-)</summary>
        public double[] WFPS;

        /// <summary>Evaporation rate of free surface water (including water intercepted on herbage). Default value is 0.0. (mm)</summary>
        public double SurfaceEvap { get; set; } = 0;

        /// <summary>Soil pH</summary>
        public double[] pH;

        /// <summary>Gets or sets the Proportion of precip intercepted (0-1)</summary>
        public double RainIntercept { get; set; }

        /// <summary>Gets or sets the Trampling rate.
        /// Stocking rate factor used to determine fall of standing dead. Read as mass of grazers per unit area. kg/m^2. Default is 0.0</summary>
        public double TrampleRate { get; set; } = 0.0;

        /// <summary>
        /// Available nutrient in soil
        /// </summary>
        public TSoilNutrientDistn[] Nutrients = new TSoilNutrientDistn[Enum.GetNames(typeof(TPlantNutrient)).Length];  // [TPlantNutrient] of TSoilNutrientDistn;

        /// <summary>
        /// Initializes a new instance of the TPastureInputs class
        /// </summary>
        public TPastureInputs()
        {
            int len = Enum.GetNames(typeof(TPlantNutrient)).Length;
            for (int i = 0; i < len; i++)
            {
                this.Nutrients[i] = new TSoilNutrientDistn();
            }
        }

        /// <summary>
        /// Initialise the soil arrays
        /// </summary>
        /// <param name="layercount"></param>
        public void InitSoil(int layercount)
        {
            // [0] is surface
            pH = new double[layercount + 1];
            for (int Ldx = 1; Ldx <= layercount; Ldx++)
            {
                pH[Ldx] = 7.0;  // Default value for pH
            }
            WFPS = new double[layercount + 1];
            ASW = new double[layercount + 1];
            Theta = new double[layercount + 1];
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class TNutrientInfo
    {
        /// <summary></summary>
        public double[,] fMaxShootConc = new double[ptSTEM + 1, HerbClassNo + 1];                   // [ptLEAF..ptSTEM, 1..HerbClassNo]

        /// <summary></summary>
        public double[,] fMinShootConc = new double[ptSTEM + 1, HerbClassNo + 1];                   // [ptLEAF..ptSTEM,1..HerbClassNo]

        /// <summary></summary>
        public double[] fMaxDemand = new double[GrazType.ptSEED + 1];

        /// <summary></summary>
        public double[] fCritDemand = new double[GrazType.ptSEED + 1];

        /// <summary></summary>
        public double fSupplied;

        /// <summary></summary>
        public double fRootTranslocSupply;

        /// <summary></summary>
        public double fStemTranslocSupply;

        /// <summary></summary>
        public double[,] fRecycled = new double[GrazType.ptSTEM + 1, GrazType.HerbClassNo + 1];     // [ptLEAF..ptSTEM,1..HerbClassNo]

        /// <summary></summary>
        public double fRecycledSum;

        /// <summary></summary>
        public double fFixed;

        /// <summary></summary>
        public double[][][] fUptake;                                                                // [TPlantNutrient(0..3)][0..MAXNUTRAREAS - 1][1..MaxSoilLayers]    == TElemUptakeDistn

        /// <summary></summary>
        public double fUptakeSum;

        /// <summary></summary>
        public double[,] fRelocated = new double[GrazType.ptSTEM + 1, GrazType.HerbClassNo + 1];    // [ptLEAF..ptSTEM,1..HerbClassNo]

        /// <summary></summary>
        public double fRelocatedSum;

        /// <summary></summary>
        public double[] fRelocatedRoot = new double[GrazType.MaxSoilLayers + 1];                    // [TOTAL..MaxSoilLayers]

        /// <summary></summary>
        public double[,] fLeached = new double[GrazType.ptSTEM + 1, GrazType.HerbClassNo + 1];      // [ptLEAF..ptSTEM,1..HerbClassNo]

        /// <summary></summary>
        public double fGaseousLoss;

        /// <summary>
        /// Initializes a new instance of the TNutrientInfo class
        /// </summary>
        public TNutrientInfo()
        {
            int x = Enum.GetNames(typeof(TPlantNutrient)).Length;
            this.fUptake = new double[x][][];
            for (int i = 0; i < x; i++)
            {
                this.fUptake[i] = new double[MAXNUTRAREAS][];
                for (int j = 0; j < MAXNUTRAREAS; j++)
                {
                    this.fUptake[i][j] = new double[MaxSoilLayers + 1];
                }
            }
        }
    }

    /// <summary>
    /// The pasture population
    /// Population of a single pasture species
    /// </summary>
    [Serializable]
    public class TPasturePopulation
    {
        private bool FRecomputeRoots;
        private string FMassUnit = "";
        private double FMassScalar;

        // Parameters --------------------------------------------------------------
        private TPastureParamSet FParams;
        private string FParamFile = "";

        /// <summary></summary>
        public double FMaxRootDepth;

        /// <summary></summary>
        public int FSoilLayerCount;

        /// <summary>Depth of each soil layer</summary>
        public double[] FSoilLayers;

        /// <summary>Cumulative depth of each soil layer</summary>
        public double[] FSoilDepths;

        /// <summary></summary>
        private double[] FBulkDensity;

        /// <summary></summary>
        private double[] FSandContent;

        /// <summary></summary>
        private double[] FCampbellParam;

        /// <summary></summary>
        public double[] FRootRestriction;

        /// <summary></summary>
        public double FFertScalar = 1.0;

        // Driving variables -------------------------------------------------------
        TPastureInputs FInputs = new TPastureInputs();
        private readonly double[] FLightFract = new double[GrazType.stSENC + 1];         // [stSEEDL..stSENC]
        private readonly double[][] FSoilFract = new double[GrazType.stSENC + 1][];      // [stSEEDL..stSENC] of LayerArray;
        private readonly double[][] FTranspireRate = new double[GrazType.stSENC + 1][];  // [stSEEDL..stSENC] of LayerArray;
        private double FPastureDM;
        private double FPastureGAI;
        private double FPastureTotalAI;
        private double FPastureCoverSum;

        // Temporary variables -----------------------------------------------------

        /// <summary></summary>
        private readonly double[] FExtinctChange = new double[GrazType.stLITT2 + 1];             // [stSEEDL..stLITT2]

        /// <summary>Hardening of unripe seeds       g/g/d</summary>
        private double FHardenRate;

        /// <summary>Ripening of unripe seeds        g/g/d</summary>
        private double FRipenRate;

        /// <summary>Accounting for non-seed part of diaspores</summary>
        private double FDiscardRate;

        /// <summary>Softening of ripe seeds         g/g/d</summary>
        private readonly double[] FSoftenRate = new double[GrazType.MaxSoilLayers + 1];

        /// <summary>Death rates of seed             g/g/d</summary>
        private readonly double[,] FSeedDeathRate = new double[3, GrazType.MaxSoilLayers + 1];   // [SOFT..HARD,1..MaxSoilLayers]

        /// <summary>Germination rates               g/g/d</summary>
        private readonly double[] FGermnRate = new double[GrazType.MaxSoilLayers + 1];

        /// <summary></summary>
        private readonly double[] FHerbageGrazed = new double[GrazType.HerbClassNo + 1];

        /// <summary></summary>
        private readonly double[] FSeedGrazed = new double[GrazType.RIPE + 1];                   // [UNRIPE..RIPE]

        /// <summary>Proportion of herbage mass grazed</summary>
        public double[,,] FGrazedPropn = new double[GrazType.stLITT2 + 1, GrazType.ptSTEM + 1, GrazType.HerbClassNo + 1];      // [stSEEDL..stLITT2, ptLEAF..ptSTEM, 1..HerbClassNo]

        // Reporting variables -----------------------------------------------------

        /// <summary>Shoot DM x component at start of time step g/m^2</summary>
        private readonly double[] FShootLossDenom = new double[GrazType.stLITT2 + 1];            // [stSEEDL..stLITT2]

        /// <summary>Live root DM at start of time step         g/m^2 </summary>
        private double FRootLossDenom;

        /// <summary>Flows out of component during time step    g/m^2/d</summary>
        private readonly double[] FShootFluxLoss = new double[GrazType.stLITT2 + 1];             // [stSEEDL..stLITT2]

        /// <summary>Respiratory loss from component            g/m^2/d</summary>
        private readonly double[] FShootRespireLoss = new double[GrazType.stLITT2 + 1];          // [stSEEDL..stLITT2]

        /// <summary></summary>
        private double FRootDeathLoss;

        /// <summary></summary>
        private double FRootRespireLoss;

        /// <summary>Rate of decline in digestibility           %/d</summary>
        private readonly double[] FDigDecline = new double[GrazType.stLITT2 + 1];                // [stSEEDL..stLITT2]

        /// <summary>Cutting of shoots             g/m^2/d</summary>
        private readonly bool[] FPhenologyEvent = new bool[(int)PastureUtil.TDevelopEvent.endDormantW + 1];

        /// <summary></summary>
        private readonly double[,,] FShootsCut = new double[GrazType.stLITT2 + 1, GrazType.ptSTEM + 1, GrazType.HerbClassNo + 1];   // [TOTAL..stLITT2, TOTAL..ptSTEM, TOTAL..HerbClassNo]

        /// <summary>Grazing of shoots             g/m^2/d</summary>
        private readonly double[,,] FShootsGrazed = new double[GrazType.stLITT2 + 1, GrazType.ptSTEM + 1, GrazType.HerbClassNo + 1];  // [TOTAL..stLITT2, TOTAL..ptSTEM, TOTAL..HerbClassNo]

        /// <summary>Killing of shoots             g/m^2/d </summary>
        private readonly double[,] FShootsKilled = new double[GrazType.ptSTEM + 1, GrazType.HerbClassNo + 1];    // [TOTAL..ptSTEM,   TOTAL..HerbClassNo] of Single;

        /// <summary></summary>
        private readonly GrazType.DM_Pool[] FTopResidue = new GrazType.DM_Pool[GrazType.ptSTEM + 1];             // [ptLEAF..ptSTEM]

        /// <summary></summary>
        private readonly GrazType.DM_Pool[] FRootResidue = new GrazType.DM_Pool[GrazType.MaxSoilLayers + 1];

        /// <summary></summary>
        public GrazType.DM_Pool FLeachate = new DM_Pool();

        /// <summary>Phenological stage and variables</summary>
        public PastureUtil.TDevelopType Phenology;

        /// <summary></summary>
        public double LaggedMeanT;

        /// <summary></summary>
        public double VernIndex;

        /// <summary></summary>
        public double DegDays;

        /// <summary></summary>
        public double FloweringLength;

        /// <summary></summary>
        public double FloweringTime;

        /// <summary></summary>
        public double fSencDays;

        /// <summary></summary>
        public int DormDays;

        /// <summary></summary>
        public int DormIndex;

        /// <summary></summary>
        public double DormMeanTemp;

        /// <summary></summary>
        public double WDormMeanTemp;

        /// <summary></summary>
        public int Days_EDormant;

        /// <summary></summary>
        public double GermnIndex;

        /// <summary></summary>
        public double[] fPhenoHorizon = new double[2];

        /// <summary></summary>
        public double[] fWater_KL = null; // [1..

        /// <summary></summary>
        public double[] fPlant_LL = null; // [1..

        /// <summary>Gets the pasture parameter set</summary>
        public TPastureParamSet Params { get { return this.FParams; } }

        /// <summary>Gets the pasture parameter set optional filename</summary>
        public string ParamFileName { get { return this.FParamFile; } }

        /// <summary>Gets the soil layer count</summary>
        public int SoilLayerCount { get { return this.FSoilLayerCount; } }

        /// <summary>Gets the soil layer depths</summary>
        public double[] SoilLayer_MM { get { return this.FSoilLayers; } }

        /// <summary></summary>
        public const int ALL_COHORTS = -1;

        /// <summary></summary>
        public bool RecomputeRoots { get { return this.FRecomputeRoots; } set { this.FRecomputeRoots = value; } }

        /// <summary>Gets or sets the inputs</summary>
        public TPastureInputs Inputs { get { return this.FInputs; } set { this.FInputs = value; } }

        /// <summary>Gets or sets the pasture green dry matter</summary>
        public double PastureGreenDM { get { return this.FPastureDM; } set { this.FPastureDM = value; } }

        /// <summary>Gets or sets the pasture green area index</summary>
        public double PastureGAI { get { return this.FPastureGAI; } set { this.FPastureGAI = value; } }

        /// <summary>Gets or sets the pasture area index</summary>
        public double PastureAreaIndex { get { return this.FPastureTotalAI; } set { this.FPastureTotalAI = value; } }

        /// <summary>Gets or sets the pasture cover sum</summary>
        public double PastureCoverSum { get { return this.FPastureCoverSum; } set { this.FPastureCoverSum = value; } }

        /// <summary>
        /// Gets or sets the phenocode
        /// </summary>
        public double PhenoCode { get { return this.GetPhenoCode(); } set { this.SetPhenoCode(value); } }

        /// <summary>
        /// Gets or sets the mass unit
        /// </summary>
        public string MassUnit { get { return this.FMassUnit; } set { this.SetMassUnit(value); } }

        /// <summary>
        /// Gets the plant elements
        /// </summary>
        public TPlantElement[] ElementSet { get { return this.FElements; } }

        /// <summary>
        /// Gets the max rooting depth
        /// </summary>
        public double MaxRootingDepth { get { return this.FMaxRootDepth; } }

        /// <summary>
        /// Initializes a new instance of the TPasturePopulation class.
        /// This constructor leaves Params = NIL.It will usually be used prior to
        /// populating the population from file
        /// </summary>
        public TPasturePopulation() : base()
        {
            this.FCohorts = new TPastureCohort[0];
            this.Initialise("", new TPlantElement[0]);
        }

        /// <summary>
        /// Initializes a new instance of the TPasturePopulation class.
        /// Constructor for a pasture sward with no nutrient limitations(all the
        /// NutrModels objects are NIL).
        /// </summary>
        /// <param name="speciesName">Name of a pasture species.  Must match one of the members of ParamsGlb</param>
        public TPasturePopulation(string speciesName) : base()
        {
            this.FCohorts = new TPastureCohort[0];
            this.Initialise(speciesName, new TPlantElement[0]);
        }

        // TElementSet = set of TPlantElement;

        /// <summary>
        /// Initializes a new instance of the TPasturePopulation class.
        /// </summary>
        /// <param name="speciesName">Name of a pasture species.  Must match one of the members of ParamsGlb</param>
        /// <param name="elements">Set of plant nutrients to be simulated</param>
        public TPasturePopulation(string speciesName, TPlantElement[] elements /*= []*/) : base()
        {
            this.FCohorts = new TPastureCohort[0];
            this.Initialise(speciesName, elements);
        }

        /// <summary>
        /// Initializes a new instance of the TPasturePopulation class.
        /// Copy constructor
        /// </summary>
        /// <param name="Source"></param>
        public TPasturePopulation(TPasturePopulation Source) : base()
        {
            this.FCohorts = new TPastureCohort[0];

            this.Initialise("", new TPlantElement[0]);

            this.SetSoilParams(Source.FSoilLayers, Source.FBulkDensity, Source.FSandContent, Source.FCampbellParam);
        }

        // delegates used for setting the functions when certain nutrients are present.
        // Configured in Initialise().
        /// <summary></summary>
        public delegate void AddPool_(DM_Pool PartPool, ref DM_Pool TotPool, bool bAllowLoss = false);

        /// <summary></summary>
        public AddPool_ AddPool = null;

        /// <summary></summary>
        public delegate void MovePool_(double KgHaDM, ref DM_Pool SrcPool, ref DM_Pool DstPool);

        /// <summary></summary>
        public MovePool_ MovePool = null;

        /// <summary></summary>
        public delegate void ResizePool_(ref DM_Pool aPool, double fNewDM);

        /// <summary></summary>
        public ResizePool_ ResizePool = null;

        /// <summary>
        /// Common initialisation logic at construction time.
        /// </summary>
        /// <param name="speciesName">Name of a pasture species. Must match one of the members of ParamsGlb</param>
        /// <param name="elements">Set of plant nutrients to be simulated</param>
        public void Initialise(string speciesName, TPlantElement[] elements /*= []*/)
        {
            // create the arrays required by this instance
            for (int i = 0; i <= stSENC; i++)
            {
                this.FSoilFract[i] = new double[MaxSoilLayers + 1];         // [stSEEDL..stSENC] of LayerArray;
                this.FTranspireRate[i] = new double[MaxSoilLayers + 1];     // [stSEEDL..stSENC] of LayerArray;
            }

            // init seed pool storage
            for (int h = 0; h <= HARD; h++)
            {
                for (int r = 0; r <= RIPE; r++)
                {
                    for (int iLayer = 0; iLayer <= MaxSoilLayers; iLayer++)
                    {
                        this.FSeeds[h, r, iLayer] = new DM_Pool();
                    }
                }
            }

            for (int i = 0; i <= MaxSoilLayers; i++)
            {
                this.FRootResidue[i] = new DM_Pool();
            }

            for (int i = 0; i <= ptSTEM; i++)
            {
                this.FTopResidue[i] = new DM_Pool();
            }

            if (speciesName != "")
            {
                this.SetParameters(speciesName, TGPastureParams.PastureParamsGlb(), "");
            }

            // Connect the correct DM pool routines to this population
            this.FElements = elements;
            if (elements.Length == 0)
            {
                this.AddPool = PastureUtil.AddPool0;
                this.MovePool = PastureUtil.MovePool0;
                this.ResizePool = PastureUtil.ResizePool0;
            }
            else if ((elements.Length == 1) && (elements[0] ==TPlantElement.N))
            {
                this.AddPool = PastureUtil.AddPool1;
                this.MovePool = PastureUtil.MovePool1;
                this.ResizePool = PastureUtil.ResizePool1;
            }
            else
            {
                this.AddPool = PastureUtil.AddPool2;
                this.MovePool = PastureUtil.MovePool2;
                this.ResizePool = PastureUtil.ResizePool2;
            }

            this.FMassScalar = 1.0;
            this.ClearState();
        }

        // Morphology (height profile) calculations

        /// <summary>
        /// Find the relative heights (0=soil surface, 1=top of canopy):
        /// - below which all herbage is stem (fHeight0)
        /// - above which all herbage is leaf (fHeight1)
        /// The equations assume that
        /// 1. herbage bulk density is constant over height
        /// 2. the proportion of leaf at a given relative height is a ramp function.
        ///
        /// Parameters of the ramp function are computed from the y-intercept of the linear
        /// part (MorphK[1]) and the current proportion of leaf in the whole population.
        /// <![CDATA[ * Note that fHeight0 < 0 and fHeight1 > 1 are permitted; this just means ]]>
        ///   that a mixture of leaf and stem extends to the bottom or top of the profile,
        ///   respectively
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="leafPropn"></param>
        /// <param name="height0"></param>
        /// <param name="height1"></param>
        /// <param name="intcpt"></param>
        /// <param name="slope"></param>
        public void GetRelHeightThresholds(int comp,
                                           ref double leafPropn,
                                           ref double height0,
                                           ref double height1,
                                           ref double intcpt,
                                           ref double slope)
        {
            leafPropn = PastureUtil.Div0(this.HerbageMassGM2(comp, ptLEAF, 0),                            // Proportion of leaf over all heights
                                         this.HerbageMassGM2(comp, TOTAL, 0));

            if ((leafPropn <= this.Params.MorphK[1]) || (leafPropn >= 1.0))                               // Case where the logic won't work; use
            {                                                                                             // f(z) = P(leaf). MorphK[1]=1.0 therefore gives the old model.
                intcpt = Math.Min(1.0, leafPropn);
                slope = 0.0;
            }
            else
            {
                intcpt = this.Params.MorphK[1];
                if ((intcpt >= 0.0) && (leafPropn <= 0.5 * (intcpt + 1.0)))
                {
                    slope = 2.0 * (leafPropn - intcpt);
                }
                else if (intcpt >= 0.0)
                {
                    slope = 0.5 * StdMath.Sqr(1.0 - intcpt) / (1.0 - leafPropn);
                }
                else if (leafPropn < 0.5 / (1.0 - intcpt))
                {
                    slope = leafPropn - intcpt + Math.Sqrt(Math.Pow(leafPropn - intcpt, 2) - Math.Pow(intcpt, 2));
                }
                else
                {
                    slope = (0.5 - intcpt) / (1.0 - leafPropn);
                }
            }

            if (slope > 0.0)
            {
                height0 = (0.0 - intcpt) / slope;
                height1 = (1.0 - intcpt) / slope;
            }
            else
            {
                height0 = 0.0;
                height1 = 1.0;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="relHeight"></param>
        /// <param name="leafPropn"></param>
        /// <param name="height0"></param>
        /// <param name="height1"></param>
        /// <param name="intcpt"></param>
        /// <param name="slope"></param>
        /// <param name="leafPropnAbove"></param>
        /// <param name="stemPropnAbove"></param>
        public void GetLeafStemPropnAbove(double relHeight, double leafPropn, double height0, double height1, double intcpt, double slope, ref double leafPropnAbove, ref double stemPropnAbove)
        {
            double fLeafBelow;            // Proportion of *total shoot* that is leaf below the given relative height

            if (relHeight <= height0)
            {
                fLeafBelow = 0.0;
            }
            else if (relHeight >= height1)
            {
                fLeafBelow = leafPropn - (1.0 - relHeight);
            }
            else
            {
                fLeafBelow = intcpt * (relHeight - height0) + 0.5 * slope * (StdMath.Sqr(relHeight) - StdMath.Sqr(height0));
            }

            if (leafPropn <= 0.0)
            {
                leafPropnAbove = 0.0;
            }
            else if (leafPropn < 1.0)
            {
                leafPropnAbove = 1.0 - fLeafBelow / leafPropn;
            }
            else
            {
                leafPropnAbove = 1.0;
            }

            if (leafPropn >= 1.0)
            {
                stemPropnAbove = 0.0;
            }
            else if (leafPropn > 0.0)
            {
                stemPropnAbove = 1.0 - (relHeight - fLeafBelow) / (1.0 - leafPropn);
            }
            else
            {
                stemPropnAbove = 1.0;
            }
        }

        /// <summary>
        /// Given a relative height(0 = soil surface, 1 = top of canopy), returns the
        /// proportions of the leaf mass and the stem mass that are above that height
        /// i.e. [leaf mass above height]:[total leaf mass] and
        ///      [stem mass above height]:[total stem mass]
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="relHeight"></param>
        /// <param name="leafPropnAbove"></param>
        /// <param name="stemPropnAbove"></param>
        public void GetPropnAboveHeight(int comp, double relHeight, ref double leafPropnAbove, ref double stemPropnAbove)
        {
            double fLeafPropn = 0;
            double fIntcpt = 0;
            double fSlope = 0;
            double fHeight0 = 0;
            double fHeight1 = 0;

            relHeight = Math.Max(0.0, Math.Min(relHeight, 1.0));
            this.GetRelHeightThresholds(comp, ref fLeafPropn, ref fHeight0, ref fHeight1, ref fIntcpt, ref fSlope);
            this.GetLeafStemPropnAbove(relHeight, fLeafPropn, fHeight0, fHeight1, fIntcpt, fSlope, ref leafPropnAbove, ref stemPropnAbove);
        }

        // Assimilation and respiration

        /// <summary>
        /// "Potential" (no nutrient or meristem limitation) assimilation rate for each
        /// component and then cohort, i.e. delta-ASS* in the specification document
        /// </summary>
        public void ComputePotAssimilation(double pastureWaterDemand)
        {
            double[] fLimitF = new double[(int)TGrowthLimit.gl_S + 1];        // TGLFArray;
            double fPotTR;
            double fActTR;
            double fRUE_Scale;
            double fRUEffic;
            double fRUE_Assim;
            double fTranspEffic;
            double fTE_Assim;
            double fPropnFreeEvap;
            double fCompPotAssim;
            double fCompFPA;
            double fCohortPropn;
            int iCohort;
            int iLayer;

            for (int iComp = stSEEDL; iComp <= stSENC; iComp++)
            {
                var values = Enum.GetValues(typeof(TGrowthLimit)).Cast<TGrowthLimit>().ToArray();
                foreach (var Lim in values)                                                         // Some are computed later
                {
                    fLimitF[(int)Lim] = 1.0;
                }

                fLimitF[(int)TGrowthLimit.glLowT] = PastureUtil.SIG(this.LaggedMeanT, this.Params.LowT_K[1], this.Params.LowT_K[2]); // Temperature limit to assimilation

                fPotTR = this.WaterDemand(iComp, pastureWaterDemand);                                                   // Soil moisture limit to assimilation
                fActTR = 0.0;
                for (iLayer = 1; iLayer <= this.FSoilLayerCount; iLayer++)
                {
                    PastureUtil.XInc(ref fActTR, this.FTranspireRate[iComp][iLayer]);
                }

                if (fPotTR > 0.0)
                {
                    fLimitF[(int)TGrowthLimit.glSM] = Math.Min(1.0, (fActTR / fPotTR) / this.Params.WaterK[1]);
                }

                fLimitF[(int)TGrowthLimit.glWLog] = 0.0;                                            // Waterlogging limit to assimilation
                for (iLayer = 1; iLayer <= this.FSoilLayerCount; iLayer++)
                {
                    PastureUtil.XInc(ref fLimitF[(int)TGrowthLimit.glWLog],
                                     this.PropnEffRootsInLayer(iComp, iLayer)
                                           * Math.Exp(-this.Params.WaterLogK[2]
                                           * Math.Max(0.0, this.Inputs.WFPS[iLayer] - this.Params.WaterLogK[1])));
                }

                fRUE_Scale = (this.Params.RadnUseK[2] + REF_RADNFLUX)                               // Radiation-limited assimilation rate
                              / (this.Params.RadnUseK[2] + this.Inputs.Radiation / this.Inputs.DayLength)
                              * (1.0 - (1.0 - this.Params.RadnUseK[3]) * Div0(this.ProjArea(ptSTEM), this.ProjArea(TOTAL)));

                fRUEffic = this.Params.RadnUseK[1] * fRUE_Scale * this.CO2_RadnUseEff()
                              * Math.Min(fLimitF[(int)TGrowthLimit.glLowT], Math.Min(fLimitF[(int)TGrowthLimit.glSM], fLimitF[(int)TGrowthLimit.glWLog]));
                fRUE_Assim = fRUEffic * (this.LightPropn(iComp) * this.Inputs.Radiation);

                if (this.Inputs.VP_Deficit > 0.0)
                {                                                                                   // Transpiration-limited assimilation rate
                    fTranspEffic = this.Params.TranspEffK[1] / this.Inputs.VP_Deficit               // This transpiration efficiency is
                                    * fRUE_Scale * this.CO2_TranspEff()                             // expressed in terms of NPP
                                    * Math.Min(fLimitF[(int)TGrowthLimit.glLowT], fLimitF[(int)TGrowthLimit.glWLog]);
                    fTE_Assim = (fTranspEffic * fActTR) / (1.0 - this.GrowthRespirationRate()) + this.MaintRespirationGM2(iComp);
                }
                else
                {
                    fTE_Assim = VERYLARGE;
                }


                // This Proportion of day when evap not from free water surfaces - get from MicroClimate
                if (this.Inputs.PotentialET > 0.0)
                {
                    fPropnFreeEvap = Math.Min(this.Inputs.SurfaceEvap / this.Inputs.PotentialET, 1.0);      // Proportion of the day with evaporation of free water
                }
                else
                {
                    fPropnFreeEvap = 0.0;
                }

                fCompPotAssim = fPropnFreeEvap * fRUE_Assim
                                 + (1.0 - fPropnFreeEvap) * Math.Min(fRUE_Assim, fTE_Assim);

                if (fRUE_Assim > 0.0)
                {
                    fLimitF[(int)TGrowthLimit.glVPD] = fCompPotAssim / fRUE_Assim;
                }

                fCompFPA = this.ProjArea(iComp);                                                // Distribute potential assimilation
                for (iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)                 // between the cohorts of this component
                {
                    if (this.BelongsIn(iCohort, iComp))
                    {
                        fCohortPropn = PastureUtil.Div0(this.FCohorts[iCohort].ProjArea(), fCompFPA);
                        fLimitF[(int)TGrowthLimit.glGAI] = fCohortPropn * this.LightPropn(iComp);
                        this.FCohorts[iCohort].SetPotAssimilation(fCohortPropn * fCompPotAssim, fLimitF);
                    }
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="part">Plant part - leaf, stem, root, seed or total</param>
        /// <returns></returns>
        public double MaintRespirationGM2(int comp = sgGREEN, int part = TOTAL)
        {
            double result = 0.0;
            for (int iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
            {
                if (this.BelongsIn(iCohort, comp))
                {
                    result += this.FCohorts[iCohort].fMaintRespiration[part];
                }
            }

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public double GrowthRespirationRate()
        {
            return this.Params.RespireK[4];
        }

        /// <summary>
        /// Total removal of herbage by cutting and grazing
        /// * Assumes that the computeRemoval() method has been called
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="part">Plant part - leaf, stem, root, seed or total</param>
        /// <param name="DMD"></param>
        /// <returns></returns>
        private double RemovalGM2(int comp, int part = TOTAL, int DMD = TOTAL)
        {
            double result;
            switch (comp)
            {
                case int i when i >= stSEEDL && i <= stLITT2:
                    result = this.FShootsGrazed[comp, part, DMD] + this.FShootsCut[comp, part, DMD];
                    break;
                case sgGREEN:
                    result = this.FShootsGrazed[stSEEDL, part, DMD] + this.FShootsCut[stSEEDL, part, DMD]
                                     + this.FShootsGrazed[stESTAB, part, DMD] + this.FShootsCut[stESTAB, part, DMD]
                                     + this.FShootsGrazed[stSENC, part, DMD] + this.FShootsCut[stSENC, part, DMD];
                    break;
                case sgEST_SENC:
                    result = this.FShootsGrazed[stESTAB, part, DMD] + this.FShootsCut[stESTAB, part, DMD]
                                     + this.FShootsGrazed[stSENC, part, DMD] + this.FShootsCut[stSENC, part, DMD];
                    break;
                case sgDRY:
                    result = this.FShootsGrazed[stDEAD, part, DMD] + this.FShootsCut[stDEAD, part, DMD]
                                     + this.FShootsGrazed[stLITT1, part, DMD] + this.FShootsCut[stLITT1, part, DMD]
                                     + this.FShootsGrazed[stLITT2, part, DMD] + this.FShootsCut[stLITT2, part, DMD];
                    break;
                case sgAV_DRY:
                    result = this.FShootsGrazed[stDEAD, part, DMD] + this.FShootsCut[stDEAD, part, DMD]
                                     + this.FShootsGrazed[stLITT1, part, DMD] + this.FShootsCut[stLITT1, part, DMD];
                    break;
                case sgSTANDING:
                    result = this.FShootsGrazed[stSEEDL, part, DMD] + this.FShootsCut[stSEEDL, part, DMD]
                                     + this.FShootsGrazed[stESTAB, part, DMD] + this.FShootsCut[stESTAB, part, DMD]
                                     + this.FShootsGrazed[stSENC, part, DMD] + this.FShootsCut[stSENC, part, DMD]
                                     + this.FShootsGrazed[stDEAD, part, DMD] + this.FShootsCut[stDEAD, part, DMD];
                    break;
                case sgLITTER:
                    result = this.FShootsGrazed[stLITT1, part, DMD] + this.FShootsCut[stLITT1, part, DMD]
                                     + this.FShootsGrazed[stLITT2, part, DMD] + this.FShootsCut[stLITT2, part, DMD];
                    break;
                case TOTAL:
                    result = this.FShootsGrazed[TOTAL, part, DMD] + this.FShootsCut[TOTAL, part, DMD];
                    break;
                default:
                    result = 0.0;
                    break;
            }

            return result;
        }

        /// <summary>
        /// Computes the rate of change in the extinction coefficient of each component
        /// </summary>
        public void ComputeExtinction()
        {
            double fShootDM;
            double fShootRGR;
            double fRemovalFract;
            int iComp;

            for (iComp = stSEEDL; iComp <= stSENC; iComp++)
            {
                fShootDM = this.HerbageMassGM2(iComp, TOTAL, TOTAL);

                if (fShootDM > 0.0)
                {
                    fShootRGR = (this.NetGrowth(iComp, ptLEAF) + this.NetGrowth(iComp, ptSTEM)) / fShootDM;
                    fRemovalFract = (this.Removal(iComp, ptLEAF) + this.Removal(iComp, ptSTEM)) / fShootDM;

                    this.FExtinctChange[iComp] = fRemovalFract * (this.Params.LightK[8] - this.FExtinctionK[iComp])
                                             - fShootRGR * (this.FExtinctionK[iComp] - this.Params.LightK[7]);
                    this.FExtinctChange[iComp] = Math.Max(this.Params.LightK[7] - this.FExtinctionK[iComp],
                                                 Math.Min(this.FExtinctChange[iComp], this.Params.LightK[8] - this.FExtinctionK[iComp]));
                }
                else
                {
                    this.FExtinctChange[iComp] = this.Params.LightK[7] - this.FExtinctionK[iComp];
                }
            }
        }

        /// <summary>
        /// Main phenology routine.  See the phenology section of the model MS for
        /// the logic used here
        /// </summary>
        public void ComputePhenology()
        {
            const double SprayTopSlow = 0.6;
            double fWaterLimit;
            double fDDToday;
            bool bMainTrigger;
            bool bDaylenChangeTrigger;
            bool bColdReset;
            double fRootASW,
            fThreshold;
            int iLayer;

            double fTTerm;
            double fDLTerm;

            if (this.FindCohort(stESTAB) >= 0)
            {
                fWaterLimit = this.GrowthLimit(stESTAB, TGrowthLimit.glSM);
            }
            else
            {
                fWaterLimit = this.GrowthLimit(sgGREEN, TGrowthLimit.glSM);
            }

            fDDToday = Math.Max(0.0, this.Inputs.MeanTemp - this.Params.DevelopK[3]);                 // Degree days (base Kv3)
            switch (this.Phenology)
            {
                case TDevelopType.Vernalizing:
                case TDevelopType.DormantW:
                    this.VernIndex += this.Params.DevelopK[1] * Math.Exp(-this.Params.DevelopK[2] * this.Inputs.MinTemp);
                    break;
                case TDevelopType.Vegetative:
                    this.DegDays += fDDToday;
                    break;
                case TDevelopType.Reproductive:
                    {
                        if (this.DegDays < this.Params.DevelopK[6])
                        {
                            this.DegDays += fDDToday * Math.Max(0.0, 1.0 - this.Params.DevelopK[15] * (1.0 - fWaterLimit));
                        }
                        else
                        {
                            this.DegDays += fDDToday;
                        }
                    }

                    break;
                case TDevelopType.SprayTopped:
                    this.DegDays += fDDToday * SprayTopSlow;
                    break;
                case TDevelopType.Dormant:
                    {
                        this.DormDays++;
                        if (this.DormDays == 1)
                        {
                            this.DormMeanTemp = this.Inputs.MeanTemp;
                        }
                        else
                        {
                            this.DormMeanTemp = 0.2 * this.Inputs.MeanTemp + 0.8 * this.DormMeanTemp;
                        }

                        fRootASW = 0.0;
                        for (iLayer = 1; iLayer <= this.FSoilLayerCount; iLayer++)
                        {
                            PastureUtil.XInc(ref fRootASW, this.PropnEffRootsInLayer(stSENC, iLayer) * this.Inputs.ASW[iLayer]);
                        }

                        if (this.DormDays < this.Params.DevelopK[14])
                        {
                            fThreshold = this.Params.DevelopK[12];                                    // After K(V,14) days, lower the moisture threshold for breaking dormancy
                        }
                        else
                        {
                            fThreshold = this.Params.DevelopK[19] * this.Params.DevelopK[12];
                        }

                        if ((this.DormMeanTemp <= this.Params.DevelopK[11]) && (fRootASW >= fThreshold))
                        {
                            this.DormIndex++;
                        }
                        else
                        {
                            this.DormIndex = 0;
                        }
                    }

                    break;
                case TDevelopType.Senescent:
                    this.fSencDays += 1.0;
                    break;
            } // case Phenostage

            if ((this.Phenology == PastureUtil.TDevelopType.Reproductive || this.Phenology == PastureUtil.TDevelopType.SprayTopped)
              && (this.DegDays >= this.Params.DevelopK[6]))
            {
                // Update flowering state variables
                if (this.FloweringLength < 0.0)                                                         // Start of flowering: set pot. length
                {
                    this.FloweringTime = 0.0;
                    this.FloweringLength = this.Params.DevelopK[7];
                    this.FPhenologyEvent[(int)TDevelopEvent.startFlowering] = true;
                }
                else if (this.FloweringTime < this.FloweringLength)                                     // Flowering stage
                {
                    this.FloweringTime += 1.0;
                    this.FloweringLength = Math.Max(0.0, this.FloweringLength - this.Params.DevelopK[8] * (1.0 - fWaterLimit));
                }
                else if (this.FloweringTime >= this.FloweringLength)
                {
                    this.FloweringTime += 1.0;
                }
            }

            if (!this.Params.bWinterDormant)
            {
                bColdReset = false;
            }
            else
            {
                fTTerm = RAMP(this.WDormMeanTemp, this.Params.DevelopK[27], this.Params.DevelopK[26]);
                fDLTerm = RAMP(this.Inputs.DayLength, this.Params.DevelopK[29], this.Params.DevelopK[28]);
                bColdReset = (fTTerm + fDLTerm) >= 1.0;
            }

            if (bColdReset)
            {
                if (!this.Params.bAnnual)
                {
                    this.Phenology = PastureUtil.TDevelopType.DormantW;
                }
                else
                {
                    this.Phenology = PastureUtil.TDevelopType.Senescent;
                    this.Senesce();
                }
            }
            else if ((this.Phenology == PastureUtil.TDevelopType.Vernalizing) && (this.VernIndex >= 1.0))           // End of vernalization
            {
                this.FPhenologyEvent[(int)PastureUtil.TDevelopEvent.endVernalizing] = true;

                this.Phenology = PastureUtil.TDevelopType.Vegetative;
                this.DegDays = 0.0;
            }
            else if (this.Phenology == PastureUtil.TDevelopType.Vegetative)                                         // End of vegetative growth
            {
                bMainTrigger = (((this.Params.ReproTrigger == ReproTriggerEnum.trigLongDayLength) && (this.Inputs.DayLength >= Math.Abs(this.Params.DevelopK[4])))
                             || ((this.Params.ReproTrigger == ReproTriggerEnum.trigShortDayLength) && (this.Inputs.DayLength <= Math.Abs(this.Params.DevelopK[4])))
                             || ((this.Params.ReproTrigger == ReproTriggerEnum.trigDegDay) && (this.DegDays >= this.Params.DevelopK[5])));

                if (this.Params.bLongDay)
                {
                    bDaylenChangeTrigger = this.Inputs.DayLenIncreasing;
                }
                else if (this.Params.bShortDay)
                {
                    bDaylenChangeTrigger = !this.Inputs.DayLenIncreasing;
                }
                else
                {
                    bDaylenChangeTrigger = true;
                }

                if (bMainTrigger && bDaylenChangeTrigger)
                {
                    this.FPhenologyEvent[(int)PastureUtil.TDevelopEvent.endVegetative] = true;

                    this.Phenology = PastureUtil.TDevelopType.Reproductive;
                    this.DegDays = 0.0;
                    this.FloweringTime = 0;                                                      // First day of reproductive growth indicate the pre-flowering phase
                    this.FloweringLength = -1.0;
                }
            }
            else if ((this.Phenology == PastureUtil.TDevelopType.Reproductive || this.Phenology == PastureUtil.TDevelopType.SprayTopped)
              && (this.DegDays >= this.Params.DevelopK[9]))
            {
                // End of reproduction
                this.FPhenologyEvent[(int)PastureUtil.TDevelopEvent.endReproductive] = true;

                if ((!this.Params.bHasSeeds || (this.FloweringTime >= this.FloweringLength))
                   && (fWaterLimit <= this.Params.DevelopK[10]))
                {
                    this.fSencDays += 1.0;
                }
                else
                {
                    this.fSencDays = 0;
                }

                fThreshold = this.Params.DevelopK[20] * PastureUtil.RAMP(this.DegDays, this.Params.DevelopK[21], this.Params.DevelopK[9]);

                if (this.fSencDays >= fThreshold)
                {
                    this.Senesce();
                    if (this.Params.bSummerDormant)
                    {
                        this.Phenology = PastureUtil.TDevelopType.Dormant;
                        this.DormIndex = 0;
                        this.DormDays = 0;
                        this.DegDays = 0.0;
                    }
                    else if (this.Params.bAnnual)
                    {
                        this.Phenology = PastureUtil.TDevelopType.Senescent;
                        this.DegDays = 0.0;
                    }
                    else
                    {
                        this.StartNewCycle(true, true);
                    }

                    if (this.Params.bHasSeeds)
                    {
                        this.Days_EDormant = 0;
                    }
                }
            }
            else if (this.Phenology == PastureUtil.TDevelopType.Dormant)
            {
                // End of summer-dormancy
                fThreshold = this.Params.DevelopK[13] * StdMath.Sqr(Math.Max(0.0, 1.0 - this.DormDays / this.Params.DevelopK[14]));
                if (this.DormIndex > fThreshold)
                {
                    this.FPhenologyEvent[(int)PastureUtil.TDevelopEvent.endDormant] = true;
                    this.StartNewCycle(true, true);
                }
            }
            else if (this.Phenology == PastureUtil.TDevelopType.DormantW)
            {
                // End of winter dormancy
                if (!bColdReset)
                {
                    this.FPhenologyEvent[(int)PastureUtil.TDevelopEvent.endDormantW] = true;
                    this.StartNewCycle(true, false);
                }
            }

            if (this.HerbageMassGM2(sgGREEN, TOTAL, TOTAL) < VERYSMALL)
            {
                if (this.Params.bHasSeeds && !(this.Phenology == PastureUtil.TDevelopType.Dormant || this.Phenology == PastureUtil.TDevelopType.DormantW))
                {
                    this.Phenology = PastureUtil.TDevelopType.Senescent;
                    this.DegDays = 0.0;
                }
                else if (this.RootMassGM2(sgGREEN, TOTAL, TOTAL) < GrazType.VERYSMALL)
                {
                    this.StartNewCycle(true, true);
                }
            }
        }

        /// <summary>
        /// Re-commence the phenological cycle
        /// </summary>
        /// <param name="deHarden"></param>
        /// <param name="resetVern"></param>
        public void StartNewCycle(bool deHarden, bool resetVern)
        {
            int iNewCohort;

            if (!this.Params.bVernReqd || (!resetVern && (this.VernIndex >= 1.0)))
            {
                // Go to the first phenological stage
                this.Phenology = PastureUtil.TDevelopType.Vegetative;
                this.VernIndex = 0.0;
            }
            else if (resetVern)
            {
                this.Phenology = PastureUtil.TDevelopType.Vernalizing;
                this.VernIndex = 0.0;
            }
            else
            {
                this.Phenology = PastureUtil.TDevelopType.Vernalizing;
            }

            this.DegDays = 0.0;
            this.FloweringLength = -1;

            if (!this.Params.bAnnual)
            {
                // In perennials, place the root mass in a new cohort with stESTAB status, so ending any dormancy
                iNewCohort = this.FindCohort(stESTAB);
                if (iNewCohort < 0)
                {
                    iNewCohort = this.MakeNewCohort(stESTAB);
                    this.FCohorts[iNewCohort].RootDepth = 0.0;
                }

                for (int iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
                {
                    if (this.BelongsIn(iCohort, stSENC))
                    {
                        this.TransferCohortRoots(iCohort, iNewCohort);
                    }
                }

                this.ClearEmptyCohorts(sgEST_SENC);
            }

            this.ComputeTotals();

            this.SetPhenologyHorizon();         // Initialise the horizon which, if removed, will affect this.Phenology

            if (deHarden)
            {
                // Frost-hardening set back for new growth
                for (int iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
                {
                    this.FCohorts[iCohort].FrostFactor = 0.0;
                }
            }

            this.FPhenologyEvent[(int)PastureUtil.TDevelopEvent.startCycle] = true;
        }

        /// <summary>
        /// Movement of biomass at start of senescence
        /// </summary>
        public void Senesce()
        {
            int iCohort;

            if (this.Params.DeathK[3] > 0.0)
            {
                for (iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
                {
                    if (this.FCohorts[iCohort].Status == stESTAB)
                    {
                        this.FCohorts[iCohort].Status = stSENC;
                    }
                }
            }

            this.fSencDays = 0.0;

            this.FPhenologyEvent[(int)PastureUtil.TDevelopEvent.startSenescing] = true;
        }

        /// <summary>
        /// Changes in phenological state as a result of herbage removal
        /// Assumes that computeRemoval() has been called
        /// </summary>
        public void ComputePhenologySetback()
        {
            double fShootDM;
            double fRemovedDM;
            double fRemovalFract;
            double[] fHorizonMin = new double[2];  // [0..1]
            double[] fHorizonMax = new double[2];  // [0..1]
            double fDDToday;
            double fRemovalTerm;
            double fDevelopTerm;
            double fResetHorizon;
            double fResetPropn;
            int Idx;

            fShootDM = this.HerbageMassGM2(sgGREEN, TOTAL, TOTAL);

            if (this.Params.bHasSetback
               && (fShootDM > 0.0)
               && (this.Phenology == PastureUtil.TDevelopType.Vernalizing || this.Phenology == PastureUtil.TDevelopType.Vegetative || this.Phenology == PastureUtil.TDevelopType.Reproductive || this.Phenology == PastureUtil.TDevelopType.SprayTopped))
            {
                fRemovedDM = this.RemovalGM2(sgGREEN, ptLEAF) + this.RemovalGM2(sgGREEN, ptSTEM);
                fRemovalFract = Math.Max(0.0, Math.Min(1.0, fRemovedDM / (fShootDM + fRemovedDM)));

                fDDToday = Math.Max(0.0, this.Inputs.MeanTemp - this.Params.DevelopK[3]);         // Degree days (base Kv3)
                fHorizonMin[0] = 0.0;
                fHorizonMin[1] = this.Params.DevelopK[22];
                fHorizonMax[0] = this.Params.DevelopK[24];
                fHorizonMax[1] = 1.0;

                if (fRemovalFract < 1.0)
                {
                    fRemovalTerm = fRemovalFract / (1.0 - fRemovalFract);
                }
                else
                {
                    fRemovalTerm = VERYLARGE;
                }

                for (Idx = 0; Idx <= 1; Idx++)
                {
                    if (this.Phenology == PastureUtil.TDevelopType.Vernalizing || this.Phenology == PastureUtil.TDevelopType.Vegetative)
                    {
                        fDevelopTerm = 0.0;
                    }
                    else if (fDDToday < this.Params.DevelopK[23] * this.Params.DevelopK[6])
                    {
                        fDevelopTerm = (fHorizonMax[Idx] - fHorizonMin[Idx]) * fDDToday / (this.Params.DevelopK[23] * this.Params.DevelopK[6]);
                    }
                    else
                    {
                        fDevelopTerm = fHorizonMax[Idx] - fHorizonMin[Idx];
                    }

                    this.fPhenoHorizon[Idx] = Math.Min(1.0, Math.Min(fHorizonMax[Idx], this.fPhenoHorizon[Idx] + fDevelopTerm) + this.fPhenoHorizon[Idx] * fRemovalTerm);
                }

                fResetHorizon = this.Params.DevelopK[25] * this.fPhenoHorizon[0];

                if (fRemovalFract > 1.0 - fResetHorizon)
                {
                    this.StartNewCycle(true, true);                                          // All meristems grazed out - reset the phenological cycle
                }
                else if (fRemovalFract > 1.0 - this.fPhenoHorizon[1])
                {
                    // Removal into the sensitive horizon - set phenology back
                    fResetPropn = StdMath.Sqr(PastureUtil.RAMP(fRemovalFract, 1.0 - this.fPhenoHorizon[1], 1.0 - this.fPhenoHorizon[0]));

                    switch (this.Phenology)
                    {
                        case PastureUtil.TDevelopType.Vernalizing:
                            this.VernIndex = (1.0 - fResetPropn) * this.VernIndex;
                            break;
                        case PastureUtil.TDevelopType.Vegetative:
                            this.DegDays = (1.0 - fResetPropn) * this.DegDays;
                            break;
                        case PastureUtil.TDevelopType.Reproductive:
                        case PastureUtil.TDevelopType.SprayTopped:
                            {
                                if (fResetPropn == 1.0)
                                {
                                    // Roughly speaking, all the flowers removed
                                    this.DegDays = 0.0;
                                }
                                else if (this.DegDays < this.Params.DevelopK[6])
                                {
                                    this.DegDays = (1.0 - fResetPropn) * this.DegDays;
                                }
                            }

                            break;
                    }

                    if (fResetPropn == 1.0)
                    {
                        for (Idx = 0; Idx <= 1; Idx++)
                        {
                            this.fPhenoHorizon[Idx] = fHorizonMin[Idx];
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Initialization code for fPhenoHorizon[]
        /// </summary>
        public void SetPhenologyHorizon()
        {
            double fDevelopFract;

            if (this.Phenology == PastureUtil.TDevelopType.Reproductive || this.Phenology == PastureUtil.TDevelopType.SprayTopped)
            {
                if (this.DegDays < this.Params.DevelopK[23] * this.Params.DevelopK[6])
                {
                    fDevelopFract = this.DegDays / (this.Params.DevelopK[23] * this.Params.DevelopK[6]);
                }
                else
                {
                    fDevelopFract = 1.0;
                }
            }
            else
            {
                fDevelopFract = 0.0;
            }

            this.fPhenoHorizon[0] = 0.0 + fDevelopFract * this.Params.DevelopK[24];
            this.fPhenoHorizon[1] = this.Params.DevelopK[22] + fDevelopFract * (1.0 - this.Params.DevelopK[22]);
        }

        /// <summary>
        /// Compute seed flows
        /// </summary>
        public void ComputeSeedFlows()
        {
            double fDelta_GI;
            int iLayer, iSoft;

            this.FHardenRate = this.Params.SeedK[1];

            if (this.Days_EDormant >= 0)
            {
                // Innate dormancy
                this.Days_EDormant++;
            }

            if (this.Days_EDormant >= this.Params.SeedK[2])
            {
                // End of innate dormancy
                this.Days_EDormant = -1;
                this.FRipenRate = this.Params.GermnK[8];
                this.FDiscardRate = 1.0 - this.Params.GermnK[8];
            }
            else
            {
                this.FRipenRate = 0.0;
                this.FDiscardRate = 0.0;
            }

            if (this.Params.SeedK[3] > 0.0)
            {
                // Breaking of induced dormancy
                this.FSoftenRate[1] = this.Params.SeedK[3] * Math.Max(0.0, this.Inputs.MaxTemp - this.Params.SeedK[4]);
            }
            else
            {
                this.FSoftenRate[1] = Math.Abs(this.Params.SeedK[3]) * Math.Max(0.0, this.Params.SeedK[4] - this.Inputs.MeanTemp);
            }

            for (iLayer = 2; iLayer <= this.FSeedLayers; iLayer++)
            {
                this.FSoftenRate[iLayer] = 0.0;
            }

            // Death of seed, including buried seeds
            for (iSoft = SOFT; iSoft <= HARD; iSoft++)
            {
                for (iLayer = 1; iLayer <= this.FSeedLayers; iLayer++)
                {
                    if (this.Inputs.ASW[iLayer] > 0.5)
                    {
                        this.FSeedDeathRate[iSoft, iLayer] = this.Params.SeedDeathK[iSoft];
                    }
                    else
                    {
                        this.FSeedDeathRate[iSoft, iLayer] = 0.0;
                    }
                }
            }

            // Germination
            if ((this.SeedMassGM2(SOFT, RIPE, 1) == 0.0) || (this.Inputs.ASW[1] < this.Params.GermnK[1]))
            {
                this.GermnIndex = 0.0;
                this.FGermnRate[1] = 0.0;
            }
            else
            {
                fDelta_GI = Math.Min(RAMP(this.Inputs.MeanTemp, this.Params.GermnK[2], this.Params.GermnK[3]),
                                     PastureUtil.RAMP(this.Inputs.MeanTemp, this.Params.GermnK[5], this.Params.GermnK[4]));
                this.GermnIndex += fDelta_GI;
                this.FGermnRate[1] = PastureUtil.RAMP(this.GermnIndex, this.Params.GermnK[6], this.Params.GermnK[7]);
            }

            for (iLayer = 2; iLayer <= this.FSeedLayers; iLayer++)
            {
                this.FGermnRate[iLayer] = 0.0;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public double SeedSetPropn()
        {
            if (this.FloweringLength > 0.0)
            {
                return this.FloweringLength / this.Params.DevelopK[7];
            }
            else
            {
                return 0.0;
            }
        }

        /// <summary>
        /// * This method is used in the Sow() method, when a species without seed pools
        ///   is sown. In this case the "seedlings" are added as established plants;
        ///   hence the iSeedlComp variable.
        /// </summary>
        /// <param name="seedlPool"></param>
        public void AddSeedlings(DM_Pool seedlPool)
        {
            int iSeedlComp;
            double[] fPartFract = new double[ptROOT + 1]; // [ptLEAF..ptROOT]
            double fNewSeedlDM;
            double fOldSeedlDM;
            double fOldGreenDM;
            DM_Pool add_Pool = new DM_Pool();
            double add_SpecArea;
            double fCohortDM;
            int iCohort;
            int iPart, iDMD;

            if (seedlPool.DM > 0.0)
            {
                if (this.Params.bHasSeeds)
                {
                    iSeedlComp = stSEEDL;
                }
                else
                {
                    iSeedlComp = stESTAB;
                }

                iCohort = this.FindCohort(iSeedlComp);                                      // Find the youngest existing cohort
                if ((iCohort < 0)                                                           // Start of a new seedling cohort
                   || ((iSeedlComp == stSEEDL) && (this.FCohorts[iCohort].RootDepth > COHORT_ROOT_DIFF)))
                {
                    iCohort = this.MakeNewCohort(iSeedlComp);

                    this.FCohorts[iCohort].RootDepth = 0.0;

                    this.FCohorts[iCohort].ComputeRootExtension(this.FInputs.MeanTemp, this.FInputs.ASW);
                    this.FCohorts[iCohort].RootDepth = Math.Max(this.FCohorts[iCohort].FRootExtension[1], 1.0);
                }

                fPartFract[ptROOT] = this.Params.AllocK[1] / (1.0 + this.Params.AllocK[1]);           // Mass allocation at germination
                fPartFract[ptLEAF] = this.Params.AllocK[4] * (1.0 - fPartFract[ptROOT]);
                fPartFract[ptSTEM] = 1.0 - fPartFract[ptROOT] - fPartFract[ptLEAF];

                fNewSeedlDM = seedlPool.DM * (1.0 - fPartFract[ptROOT]);
                fOldSeedlDM = this.HerbageMassGM2(stSEEDL, TOTAL, TOTAL);
                fOldGreenDM = this.HerbageMassGM2(sgGREEN, TOTAL, TOTAL);
                fCohortDM = this.FCohorts[iCohort].Herbage[TOTAL, TOTAL].DM;

                this.FCohorts[iCohort].FrostFactor = WeightAverage(this.FCohorts[iCohort].FrostFactor, fCohortDM, 0.0, fNewSeedlDM);
                this.FCohorts[iCohort].SeedlStress = WeightAverage(this.FCohorts[iCohort].SeedlStress, fCohortDM, 0.0, fNewSeedlDM);
                this.FCohorts[iCohort].EstabIndex = WeightAverage(this.FCohorts[iCohort].EstabIndex, fCohortDM, 1.0, fNewSeedlDM);

                for (iPart = ptLEAF; iPart <= ptSTEM; iPart++)                              // Shift DM into shoot pools
                {
                    for (iDMD = 1; iDMD <= HerbClassNo; iDMD++)
                    {
                        if (this.FCohorts[iCohort].FNewShootDistn[iPart, iDMD] > 0.0)
                        {
                            add_Pool = PoolFraction(seedlPool,
                                                      fPartFract[iPart] * this.FCohorts[iCohort].FNewShootDistn[iPart, iDMD]);
                            add_SpecArea = this.FCohorts[iCohort].ComputeNewSpecificArea(iPart, this.Inputs.MeanTemp, this.Inputs.Radiation);
                            this.FCohorts[iCohort].AddHerbage(iPart, iDMD, ref add_Pool, add_SpecArea);
                        }
                    }
                }

                this.AddPool(PoolFraction(seedlPool, fPartFract[ptROOT]), ref this.FCohorts[iCohort].Roots[EFFR, 1]);       // Shift DM into root pool
                this.FCohorts[iCohort].ComputeTotals();

                if (this.Phenology == TDevelopType.Vernalizing)
                {
                    // Adjust phenological state to account
                    this.VernIndex = WeightAverage(this.VernIndex, fOldGreenDM, 0.0, fNewSeedlDM);
                }
                else if (!this.Params.bVernReqd && (this.Phenology == TDevelopType.Vegetative))
                {
                    this.DegDays = WeightAverage(this.DegDays, fOldGreenDM, 0.0, fNewSeedlDM);
                }
                else if ((this.Phenology == TDevelopType.Senescent || this.Phenology == TDevelopType.Dormant || this.Phenology == TDevelopType.DormantW)
                        || (fNewSeedlDM > fOldGreenDM)
                        || (this.Params.bVernReqd
                            && (this.Phenology > TDevelopType.Vernalizing)
                            && (fOldSeedlDM + fNewSeedlDM > fOldGreenDM - fOldSeedlDM)))
                {
                    // Restart phenological cycle if necessary
                    this.StartNewCycle(true, true);
                }
            }
        }

        /// <summary>
        /// Computes the values of FRootRestriction (RBD) and, optionally, re-sets the
        /// maximum rooting depth
        /// * Assumes that FSandContent and FBulkDensity are known
        /// </summary>
        /// <param name="setRDMax"></param>
        public void ComputeRootingParams(bool setRDMax)
        {
            double fBD_Threshold;
            int iLayer;

            for (iLayer = 1; iLayer <= this.FSoilLayerCount; iLayer++)
            {
                fBD_Threshold = this.Params.RootK[6] + (this.Params.RootK[5] - this.Params.RootK[6]) * this.FSandContent[iLayer];
                if (this.FBulkDensity[iLayer] <= fBD_Threshold)
                {
                    this.FRootRestriction[iLayer] = 1.0;
                }
                else
                {
                    this.FRootRestriction[iLayer] = Math.Max(this.Params.RootK[8],
                                                       1.0 - this.Params.RootK[7] * (this.FBulkDensity[iLayer] - fBD_Threshold));
                }
            }

            if (setRDMax)
            {
                this.SetMaxRootDepth(this.DefaultMaxRootDepth());
            }
        }

        /// <summary>
        ///  Returns the proportion of the total mass of effective roots that is
        ///  present in the nominated layer.
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="layer"></param>
        /// <returns></returns>
        public double PropnEffRootsInLayer(int comp, int layer)
        {
            double denom;

            double result = 0.0;
            denom = 0.0;
            for (int iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
            {
                if (this.BelongsIn(iCohort, comp))
                {
                    result += this.FCohorts[iCohort].Roots[EFFR, layer].DM;
                    denom += this.FCohorts[iCohort].Roots[EFFR, TOTAL].DM;
                }
            }

            if (denom > 0.0)
            {
                result /= denom;
            }
            else
            {
                result = 0.0;
            }

            return result;
        }

        /// <summary>
        /// Water uptake logic (for use in monoculture only)
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="demand"></param>
        /// <param name="uptake"></param>
        private void ComputeWaterUptake_ASW(int comp, double demand, ref double[] uptake)
        {
            double[] fRLD; // LayerArray
            double fTotRoots;
            double[] fRootPropn = new double[this.FSoilLayerCount + 1];
            double fTranspDeficit;
            double fTranspRatio;
            double[] fDistribute = new double[this.FSoilLayerCount + 1];
            double fTotDistr;
            double fExtraUptake;
            int iLayer;

            fRLD = this.EffRootLengthD(comp);
            fTotRoots = 0.0;

            // Compute the proportion of root length in each soil layer
            for (iLayer = 1; iLayer <= this.FSoilLayerCount; iLayer++)
            {
                fRootPropn[iLayer] = fRLD[iLayer] * this.FSoilLayers[iLayer];
                fTotRoots += fRootPropn[iLayer];
            }

            if (fTotRoots > 0.0)
            {
                for (iLayer = 1; iLayer <= this.FSoilLayerCount; iLayer++)
                {
                    fRootPropn[iLayer] = fRootPropn[iLayer] / fTotRoots;
                }
            }

            fTranspDeficit = demand;                                                       // Model equations start here
            for (iLayer = 1; iLayer <= this.FSoilLayerCount; iLayer++)
            {
                fTranspRatio = Math.Max(0.0, Math.Min(this.FInputs.ASW[iLayer] / this.Params.WaterUseK[1], 1.0));
                if (fTranspRatio < 1.0)
                {
                    fDistribute[iLayer] = 0.0;
                }
                else
                {
                    fDistribute[iLayer] = fRootPropn[iLayer];
                }

                uptake[iLayer] = demand * fRootPropn[iLayer] * fTranspRatio;
                fTranspDeficit -= uptake[iLayer];
            }

            fExtraUptake = this.Params.WaterUseK[2] * fTranspDeficit;
            if (fExtraUptake > GrazType.VERYSMALL)
            {
                fTotDistr = 0.0;
                for (iLayer = 1; iLayer <= this.FSoilLayerCount; iLayer++)
                {
                    fTotDistr += fDistribute[iLayer];
                }

                if (fTotDistr > 0.0)
                {
                    for (iLayer = 1; iLayer <= this.FSoilLayerCount; iLayer++)
                    {
                        uptake[iLayer] = uptake[iLayer] + fExtraUptake * (fDistribute[iLayer] / fTotDistr);
                    }
                }
            }
        }

        /// <summary>
        /// Water uptake logic (for use in monoculture only)
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="demand"></param>
        /// <param name="uptake"></param>
        private void ComputeWaterUptake_KL(int comp, double demand, ref double[] uptake)
        {
            double fRootPropn;
            double fSupplySum;
            int iLayer;

            GrazType.InitLayerArray(ref uptake, 0.0);

            double[] fCompRLD = this.EffRootLengthD(comp);
            double[] fTotalRLD = this.EffRootLengthD(sgGREEN);

            fSupplySum = 0.0;
            for (iLayer = 1; iLayer <= Math.Min(this.fWater_KL.Length, this.FSoilLayerCount); iLayer++)
            {
                fRootPropn = PastureUtil.Div0(fCompRLD[iLayer], fTotalRLD[iLayer]);
                uptake[iLayer] = fRootPropn * this.FSoilLayers[iLayer] * this.fWater_KL[iLayer]
                                   * Math.Max(0.0, this.FInputs.Theta[iLayer] - this.fPlant_LL[iLayer]);
                fSupplySum += uptake[iLayer];
            }

            if (fSupplySum > demand)
            {
                for (iLayer = 1; iLayer <= Math.Min(this.fWater_KL.Length, this.FSoilLayerCount); iLayer++)
                {
                    uptake[iLayer] = uptake[iLayer] * (demand / fSupplySum);
                }
            }
        }

        /// <summary>
        /// Do the calculations to determine the maximum and critical demand
        /// </summary>
        /// <param name="elem">N, P, S</param>
        /// <param name="maxDemand">The maximum demand is the maximum amount of nutrient that can be acquired through uptake or fixation</param>
        /// <param name="critDemand">The critical demand is the amount below which the plants' net primary production will be restricted</param>
        /// <param name="pastureWaterDemand">Pasture Water Demand</param>
        public void ComputeNutrientRatesEstimate(TPlantElement elem, ref double maxDemand, ref double critDemand, double pastureWaterDemand)
        {
            maxDemand = 0;
            critDemand = 0;

            double fMoistureChange;
            double fDormTempFract;
            bool bDormant;
            int iCohort;

            bDormant = (this.Phenology == TDevelopType.Dormant || this.Phenology == TDevelopType.DormantW);

            if (this.LaggedMeanT >= -99.9)
            {
                this.LaggedMeanT = 0.1 * this.Inputs.MeanDayTemp + 0.9 * this.LaggedMeanT;
                if (this.Params.DevelopK[30] < 1.0)
                {
                    fDormTempFract = 1.0;
                }
                else
                {
                    fDormTempFract = 1.0 / this.Params.DevelopK[30];
                }

                this.WDormMeanTemp = fDormTempFract * this.Inputs.MeanTemp + (1.0 - fDormTempFract) * this.WDormMeanTemp;
            }
            else
            {
                this.LaggedMeanT = this.Inputs.MeanTemp;
                this.WDormMeanTemp = this.Inputs.MeanTemp;
            }

            if (DormMeanTemp < -99.9)
            {
                DormMeanTemp = Inputs.MeanTemp;
            }

            // Need extension rates in computing allocation within root pools
            for (iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
            {
                if (this.BelongsIn(iCohort, sgGREEN))
                {
                    this.FCohorts[iCohort].ComputeRootExtension(this.Inputs.MeanTemp, this.Inputs.ASW);
                    this.FCohorts[iCohort].ComputeNewSpecificArea(TOTAL, this.Inputs.MeanTemp, this.Inputs.Radiation);
                }
            }

            for (iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
            {
                if (this.BelongsIn(iCohort, sgGREEN))
                {
                    this.FCohorts[iCohort].ComputeRespiration(this.Inputs.MeanTemp, bDormant);
                    this.FCohorts[iCohort].ComputeAllocation();
                }
            }

            this.ComputePotAssimilation(pastureWaterDemand);

            for (iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
            {
                if (this.BelongsIn(iCohort, sgGREEN))
                {
                    this.FCohorts[iCohort].ComputePotTranslocation(this.Inputs.MeanTemp);            // Need target R:S ratio to compute this
                    this.FCohorts[iCohort].ComputePotNetGrowth();
                }
            }

            fMoistureChange = this.Inputs.RainIntercept * this.Inputs.Precipitation - this.Inputs.SurfaceEvap;
            for (iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
            {
                if (this.BelongsIn(iCohort, stDEAD))
                {
                    this.FCohorts[iCohort].DeadMoistureBalance(fMoistureChange);
                }

                this.FCohorts[iCohort].ComputeFlowRates(this.Inputs.MinTemp, this.Inputs.MeanTemp,
                                                    this.LaggedMeanT, this.Inputs.Precipitation,
                                                    this.Inputs.TrampleRate, this.Inputs.ASW[1]);
                this.FCohorts[iCohort].ComputeFrostHardening(this.Inputs.MinTemp);
            }




            for (int iComp = stSEEDL; iComp <= stSENC; iComp++)
            {
                for (iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
                {
                    if (this.FCohorts[iCohort].Status == iComp)
                    {
                        this.FCohorts[iCohort].ComputeNutrientDemand(elem);
                        maxDemand += this.FCohorts[iCohort].FNutrientInfo[(int)elem].fMaxDemand[TOTAL];     // accumulate the max demand
                        critDemand += this.FCohorts[iCohort].FNutrientInfo[(int)elem].fCritDemand[TOTAL];   // accumulate the critical demand
                    }
                }
            }
        }

        /// <summary>
        /// Compute nutrient rates
        /// </summary>
        /// <param name="elem">N, P, S</param>
        /// <param name="fSupply">Nutrient supply in g/m^2</param>
        private void ComputeNutrientRates(TPlantElement elem, double[][][] fSupply)
        {
            int iComp;
            int iCohort;

            for (iComp = stSEEDL; iComp <= stSENC; iComp++)
            {
                for (iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
                {
                    if (this.FCohorts[iCohort].Status == iComp)
                    {
                        this.FCohorts[iCohort].ComputeNutrientDemand(elem);
                        this.FCohorts[iCohort].ResetNutrientSupply(elem);
                        this.FCohorts[iCohort].TranslocateNutrients(elem);
                        if (elem == TPlantElement.N)
                        {
                            this.FCohorts[iCohort].RecycleNutrients(elem);
                        }

                        if ((elem == TPlantElement.N) && this.Params.bLegume)
                        {
                            this.FCohorts[iCohort].FixNitrogen();
                        }
                    }
                }

                this.ComputeNutrientUptake3(iComp, elem, fSupply);

                for (iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
                {
                    if (this.FCohorts[iCohort].Status == iComp)
                    {
                        this.FCohorts[iCohort].RelocateNutrients(elem);
                    }
                }
            }

            for (iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
            {
                if (this.FCohorts[iCohort].Status == stDEAD)
                {
                    this.FCohorts[iCohort].ResetNutrientSupply(elem);
                }
            }

            for (iComp = stLITT1; iComp <= stLITT2; iComp++)
            {
                for (iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
                {
                    if (this.FCohorts[iCohort].Status == iComp)
                    {
                        this.FCohorts[iCohort].ResetNutrientSupply(elem);
                        this.FCohorts[iCohort].LeachNutrients(elem);
                    }
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="Rho"></param>
        /// <param name="Tau"></param>
        /// <returns></returns>
        private double G(double Rho, double Tau)
        {
            double Rho2, Rho2T, X;
            double result;

            Rho2 = StdMath.Sqr(Rho);
            Rho2T = Math.Pow(Rho, 2 * Tau);
            if (Math.Abs(Tau) < 1.0E-7)
            {
                X = 2.0 * Math.Log(Rho);
            }
            else
            {
                X = (Rho2T - 1.0) / Tau;
            }

            result = (1 - Rho2 + Rho2 * X * (1.0 + (Tau + 1) / (Rho2 * Rho2T - 1.0))
                         + (1 - Rho2T * StdMath.Sqr(Rho2)) * (Tau + 1) / ((Tau + 2.0) * (Rho2 * Rho2T - 1.0)))
                       / (4.0 * (Tau + 1.0));

            return result;
        }

        /// <summary>
        /// Compute the nutrient uptake
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="elem"></param>
        private void ComputeNutrientUptake(int comp, TPlantElement elem)
        {
            double fDemandSum;
            int iLastLayer;
            double[] fTransp_M;
            double[] fRadii;
            double[] fRLD;
            double[][][] fSupply;     // TElemUptakeDistn
            double fDepth_M;
            double fRootDistance;
            double fEffRadius;
            double fRootLength;
            double fTortuosity;
            double fDe;
            double fRho;
            double fTau;
            double fSlope;
            double fAvailGM2;
            int iCohort;
            int iLayer, iArea;

            // initialise the 3D array
            int x = Enum.GetNames(typeof(TPlantNutrient)).Length;
            fSupply = new double[x][][];
            for (int i = 0; i < x; i++)
            {
                fSupply[i] = new double[MAXNUTRAREAS][];
                for (int j = 0; j < MAXNUTRAREAS; j++)
                {
                    fSupply[i][j] = new double[this.FSoilLayerCount + 1];
                }
            }

            fDemandSum = 0.0;
            iLastLayer = 0;
            for (iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
            {
                if (this.BelongsIn(iCohort, comp))
                {
                    fDemandSum += Math.Max(0.0, this.FCohorts[iCohort].FNutrientInfo[(int)elem].fMaxDemand[TOTAL] - this.FCohorts[iCohort].FNutrientInfo[(int)elem].fSupplied);
                    iLastLayer = Math.Max(iLastLayer, this.FCohorts[iCohort].FMaxRootLayer);
                }
            }

            if (fDemandSum > VERYSMALL)
            {
                fRLD = this.EffRootLengthD(comp);                                              // Length density of eff. roots  (m/m^3)
                fRadii = this.RootRadii(comp);                                                 // Average root radius (m)
                fTransp_M = this.Transpiration(comp);                                          // Transpiration (m^3/m^2/d)
                for (iLayer = 1; iLayer <= iLastLayer; iLayer++)
                {
                    fTransp_M[iLayer] = 0.001 * fTransp_M[iLayer];
                }

                PastureUtil.Fill3DArray(fSupply, 0.0);
                for (iLayer = 1; iLayer <= iLastLayer; iLayer++)
                {
                    if ((fRLD[iLayer] > VERYSMALL)
                       && (this.Inputs.Theta[iLayer] > VERYSMALL)
                       && (this.FSoilFract[comp][iLayer] > VERYSMALL))
                    {
                        fDepth_M = 0.001 * this.SoilLayer_MM[iLayer];                           // Soil layer depth in metres
                        fTortuosity = 0.45 * Math.Pow(this.Inputs.WFPS[iLayer], 0.3 * this.FCampbellParam[iLayer]);
                        var values = Enum.GetValues(typeof(TPlantNutrient)).Cast<TPlantNutrient>().ToArray();
                        foreach (var Nutr in values)
                        {
                            if (Nutr2Elem[(int)Nutr] == elem)                                   // Quantities that depend on the nutrient
                            {
                                fDe = DiffuseAq[(int)Nutr] * this.Inputs.Theta[iLayer] * fTortuosity;       // Effective diffusivity (m^2/d)
                                fRootDistance = Math.Sqrt(this.FSoilFract[comp][iLayer] / (Math.PI * fRLD[iLayer])); // Half-distance between roots (m)
                                fEffRadius = this.Params.NutrEffK[(int)Nutr] * fRadii[iLayer];
                                fRootLength = fRLD[iLayer] * fDepth_M;                          // Root length in the layer (m/m^2)
                                fRho = fRootDistance / fEffRadius;                              // Dimensionless distance between roots
                                if (fRho > 1.0)
                                {
                                    fTau = -Math.Min(0.4999999,                                 // Dimensionless transpiration
                                                          fTransp_M[iLayer] / (2.0 * Math.PI * fDe * fRootLength));
                                    fSlope = 0.5 * Math.PI * fRootLength * fDe * (StdMath.Sqr(fRho) - 1.0) / this.G(fRho, fTau);
                                }
                                else
                                {
                                    fSlope = VERYLARGE;
                                }

                                TSoilNutrientDistn nutrDist = this.Inputs.Nutrients[(int)Nutr];
                                for (iArea = 0; iArea <= nutrDist.NoAreas - 1; iArea++)
                                {
                                    // fSupply is in units of g/m^2/d of uptake per (m/m^3) of root length
                                    fAvailGM2 = 0.99999 * (nutrDist.AvailKgHa[iArea][iLayer] * KGHA_GM2) * this.FSoilFract[comp][iLayer];
                                    fSupply[(int)Nutr][iArea][iLayer] = Math.Min(nutrDist.RelAreas[iArea] * fSlope * nutrDist.SolnPPM[iArea][iLayer],
                                                                       fAvailGM2) / fRLD[iLayer];
                                }
                            }
                        }
                    }
                }

                for (iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
                {
                    if (this.BelongsIn(iCohort, comp))
                    {
                        this.FCohorts[iCohort].UptakeNutrients(elem, fSupply);
                    }
                }
            } // if fDemandSum > VERYSMALL
        }

        /// <summary>
        /// Compute the nutrient uptake
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="elem">N,P,S</param>
        /// <param name="myZone"></param>
        /// <returns>The supply for [nutrient][area][layer] in g/m^2</returns>
        public double[][][] ComputeNutrientUptake2(int comp, TPlantElement elem, ZoneWaterAndN myZone)
        {
            int iLastLayer;
            double[] fTransp_M;
            double[] fRadii;
            double[] fRLD;
            double[][][] fSupply;     // TElemUptakeDistn
            double fDepth_M;
            double fRootDistance;
            double fEffRadius;
            double fRootLength;
            double fTortuosity;
            double fDe;
            double fRho;
            double fTau;
            double fSlope;
            double fAvailGM2;
            int iCohort;
            int iLayer;

            // initialise the 3D array
            int x = Enum.GetNames(typeof(TPlantNutrient)).Length;
            fSupply = new double[x][][];
            for (int i = 0; i < x; i++)
            {
                fSupply[i] = new double[MAXNUTRAREAS][];
                for (int j = 0; j < MAXNUTRAREAS; j++)
                {
                    fSupply[i][j] = new double[this.FSoilLayerCount + 1];
                }
            }

            iLastLayer = 0;
            for (iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
            {
                if (this.BelongsIn(iCohort, comp))
                {
                    iLastLayer = Math.Max(iLastLayer, this.FCohorts[iCohort].FMaxRootLayer);
                }
            }

            fRLD = this.EffRootLengthD(comp);                                              // Length density of eff. roots  (m/m^3)
            fRadii = this.RootRadii(comp);                                                 // Average root radius (m)
            fTransp_M = this.Transpiration(comp);                                          // Transpiration (m^3/m^2/d)
            for (iLayer = 1; iLayer <= iLastLayer; iLayer++)
            {
                fTransp_M[iLayer] = 0.001 * fTransp_M[iLayer];
            }

            PastureUtil.Fill3DArray(fSupply, 0.0);
            for (iLayer = 1; iLayer <= iLastLayer; iLayer++)
            {
                if ((fRLD[iLayer] > VERYSMALL)
                    && (this.Inputs.Theta[iLayer] > VERYSMALL)
                    && (this.FSoilFract[comp][iLayer] > VERYSMALL))
                {
                    fDepth_M = 0.001 * this.SoilLayer_MM[iLayer];                           // Soil layer depth in metres
                    fTortuosity = 0.45 * Math.Pow(this.Inputs.WFPS[iLayer], 0.3 * this.FCampbellParam[iLayer]);
                    var values = Enum.GetValues(typeof(TPlantNutrient)).Cast<TPlantNutrient>().ToArray();
                    foreach (var Nutr in values)
                    {
                        if (Nutr2Elem[(int)Nutr] == elem)                                   // Quantities that depend on the nutrient
                        {
                            fDe = DiffuseAq[(int)Nutr] * this.Inputs.Theta[iLayer] * fTortuosity;       // Effective diffusivity (m^2/d)
                            fRootDistance = Math.Sqrt(this.FSoilFract[comp][iLayer] / (Math.PI * fRLD[iLayer])); // Half-distance between roots (m)
                            fEffRadius = this.Params.NutrEffK[(int)Nutr] * fRadii[iLayer];
                            fRootLength = fRLD[iLayer] * fDepth_M;                          // Root length in the layer (m/m^2)
                            fRho = fRootDistance / fEffRadius;                              // Dimensionless distance between roots
                            if (fRho > 1.0)
                            {
                                fTau = -Math.Min(0.4999999,                                 // Dimensionless transpiration
                                                        fTransp_M[iLayer] / (2.0 * Math.PI * fDe * fRootLength));
                                fSlope = 0.5 * Math.PI * fRootLength * fDe * (StdMath.Sqr(fRho) - 1.0) / this.G(fRho, fTau);
                            }
                            else
                            {
                                fSlope = VERYLARGE;
                            }

                            // fSupply is in units of g/m^2/d of uptake per (m/m^3) of root length
                            double amountKgHa;
                            if (Nutr == TPlantNutrient.pnNO3)
                                amountKgHa = myZone.NO3N[iLayer - 1];
                            else if (Nutr == TPlantNutrient.pnNH4)
                                amountKgHa = myZone.NH4N[iLayer - 1];
                            else
                                throw new Exception("Invalid element");
                            double amountSolN = amountKgHa * 100.0 / this.FSoilLayers[iLayer] / this.Inputs.Theta[iLayer];

                            fAvailGM2 = 0.99999 * (amountKgHa * KGHA_GM2) * this.FSoilFract[comp][iLayer];
                            double relArea = 1;
                            fSupply[(int)Nutr][0][iLayer] = Math.Min(relArea * fSlope * amountSolN, fAvailGM2);
                        }
                    }
                }
            }
            return fSupply;
        }

        /// <summary>
        /// Compute the nutrient uptake
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="elem">N,P,S</param>
        /// <param name="fSupply">Nutrient supply g/m^2</param>
        private void ComputeNutrientUptake3(int comp, TPlantElement elem, double[][][] fSupply)
        {
            double totalMaxDemandAllCohorts = 0;
            for (int iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
            {
                totalMaxDemandAllCohorts += this.FCohorts[iCohort].FNutrientInfo[(int)elem].fMaxDemand[TOTAL];
            }

            for (int iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
            {
                if (this.BelongsIn(iCohort, comp))
                {
                    double maxDemandOfCohort = this.FCohorts[iCohort].FNutrientInfo[(int)elem].fMaxDemand[TOTAL];

                    fSupply[0][0] = MathUtilities.Multiply_Value(fSupply[0][0], maxDemandOfCohort / totalMaxDemandAllCohorts);
                    fSupply[1][0] = MathUtilities.Multiply_Value(fSupply[1][0], maxDemandOfCohort / totalMaxDemandAllCohorts);
                    this.FCohorts[iCohort].UptakeNutrients(elem, fSupply);
                }
            }
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <returns></returns>
        private double CO2_WaterDemand(int comp)
        {
            double GAMMA = 0.0674;                  // kPa/oC
            //// double R_ATM_REF = 208.0 / 120.0;   // s/m per (mm of pasture height) * (m/s wind)

            double fRelCO2Change;
            double fDelta;
            double fHeight_M;
            double fActiveGAI;
            double fAerodynamicResistance;
            double fStomatalResistance_Ref;
            double fStomatalResistance_CO2;

            double result = 1.0;

            if (this.Inputs.CO2_PPM != GrazEnv.REFERENCE_CO2)
            {
                // "active" (sunlight) green area index
                fActiveGAI = 0.0;
                if (comp == stSEEDL || comp == sgGREEN)
                {
                    fActiveGAI += PastureUtil.Div0(this.LightPropn(stSEEDL), this.FExtinctionK[stSEEDL]);
                }

                if (comp == stESTAB || comp == sgEST_SENC || comp == sgGREEN)
                {
                    fActiveGAI += PastureUtil.Div0(this.LightPropn(stESTAB), this.FExtinctionK[stESTAB]);
                }

                if (comp == stSENC || comp == sgEST_SENC || comp == sgGREEN)
                {
                    fActiveGAI += PastureUtil.Div0(this.LightPropn(stSENC), this.FExtinctionK[stSENC]);
                }

                if (fActiveGAI > 0.0)
                {
                    // slope of the saturated vapour pressure curve
                    fDelta = 4098.0 * 0.6108 * Math.Exp(17.27 * this.Inputs.MeanTemp / (this.Inputs.MeanTemp + 237.3))
                                                     / Math.Pow(this.Inputs.MeanTemp + 237.3, 2);

                    // aerodynamic resistance (s/m)
                    fHeight_M = Math.Min(1.99, 0.001 * this.Height_MM());

                    fAerodynamicResistance = Math.Log((2.0 - 2 / 3 * fHeight_M) / (0.123 * fHeight_M))
                                               * Math.Log((2.0 - 2 / 3 * fHeight_M) / (0.0123 * fHeight_M))
                                               / (Math.Pow(0.41, 2) * Math.Max(0.01, this.Inputs.Windspeed));

                    // stomatal resistance at reference and current [CO2] (s/m)
                    fRelCO2Change = this.Inputs.CO2_PPM / GrazEnv.REFERENCE_CO2 - 1.0;
                    fStomatalResistance_Ref = this.Params.WaterUseK[5];
                    fStomatalResistance_CO2 = this.Params.WaterUseK[5] * (1.0 + this.Params.WaterUseK[6] * fRelCO2Change);

                    result = (fAerodynamicResistance * (1.0 + fDelta / GAMMA) + fStomatalResistance_Ref / fActiveGAI)
                            / (fAerodynamicResistance * (1.0 + fDelta / GAMMA) + fStomatalResistance_CO2 / fActiveGAI);
                }
            }

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        private double CO2_RadnUseEff()
        {
            double fCO2_CompPt;
            double result;

            if (this.Inputs.CO2_PPM == GrazEnv.REFERENCE_CO2)
            {
                result = 1.0;
            }
            else if (this.Inputs.CO2_PPM <= 0.0)
            {
                result = 0.0;
            }
            else
            {
                fCO2_CompPt = this.Params.RadnUseK[4]
                               + (this.Params.RadnUseK[5] - this.Params.RadnUseK[4])
                                 * this.Inputs.MeanDayTemp / REF_CO2_TEMP
                                 * (this.Params.RadnUseK[6] - REF_CO2_TEMP) / (this.Params.RadnUseK[6] - this.Inputs.MeanDayTemp);
                result = ((this.Inputs.CO2_PPM - fCO2_CompPt) * (GrazEnv.REFERENCE_CO2 + 2.0 * fCO2_CompPt))
                             / ((this.Inputs.CO2_PPM + 2.0 * fCO2_CompPt) * (GrazEnv.REFERENCE_CO2 - fCO2_CompPt));
            }

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        private double CO2_TranspEff()
        {
            double fRelCO2Change;
            double result;

            if (this.Inputs.CO2_PPM > 0.0)
            {
                fRelCO2Change = this.Inputs.CO2_PPM / GrazEnv.REFERENCE_CO2 - 1.0;
                result = (1.0 + fRelCO2Change) / (1.0 + this.Params.WaterUseK[6] * fRelCO2Change);
            }
            else
            {
                result = 0.0;
            }

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="part">Plant part - leaf, stem, root, seed or total</param>
        /// <returns></returns>
        private double CO2_CrudeProtein(int part)
        {
            if (this.FElements.Contains(TPlantElement.N))
            {
                throw new Exception("TPasturePopulation.fCO2_CrudeProtein should not be called in N-aware execution");
            }

            if (this.CohortCount() > 0)
            {
                return this.FCohorts[0].CO2_NutrConc(part, TPlantElement.N);
            }
            else
            {
                return 1.0;
            }
        }

        /// <summary>
        /// Populates the two arrays
        ///    FGrazingPropn      proportion of herbage mass grazed
        ///    FShootsGrazed      total grazing amounts (for reporting)
        /// </summary>
        private void ComputeRemoval()
        {
            double[] fAvailDM = new double[HerbClassNo + 1];
            double[] fAvailGrazePropn = new double[HerbClassNo + 1];
            double[] fAvailPropn = new double[stLITT2 + 1];    // [stSEEDL..stLITT2]
            int iComp, iPart, iDMD;

            for (iDMD = 1; iDMD <= HerbClassNo; iDMD++)
            {
                fAvailDM[iDMD] = this.AvailHerbageGM2(TOTAL, TOTAL, iDMD);
            }

            for (iDMD = 1; iDMD <= HerbClassNo; iDMD++)
            {
                if ((fAvailDM[iDMD] == 0) && (this.FHerbageGrazed[iDMD] > 0))
                {
                    fAvailGrazePropn[iDMD] = 0.0;
                }
                else
                {
                    fAvailGrazePropn[iDMD] = PastureUtil.Div0(this.FHerbageGrazed[iDMD], fAvailDM[iDMD]);
                }
            }

            for (iComp = stSEEDL; iComp <= stLITT2; iComp++)
            {
                fAvailPropn[iComp] = PastureUtil.Div0(this.AvailHerbageGM2(iComp, TOTAL, TOTAL), this.HerbageMassGM2(iComp, TOTAL, TOTAL));
            }

            for (iComp = stSEEDL; iComp <= stLITT2; iComp++)
            {
                for (iPart = ptLEAF; iPart <= ptSTEM; iPart++)
                {
                    for (iDMD = 1; iDMD <= HerbClassNo; iDMD++)
                    {
                        this.FGrazedPropn[iComp, iPart, iDMD] = fAvailPropn[iComp] * fAvailGrazePropn[iDMD];
                        if (this.FGrazedPropn[iComp, iPart, iDMD] != 0)
                        {
                            this.FShootsGrazed[iComp, iPart, iDMD] = this.HerbageMassGM2(iComp, iPart, iDMD)
                                                         * this.FGrazedPropn[iComp, iPart, iDMD];
                        }
                        else
                        {
                            this.FShootsGrazed[iComp, iPart, iDMD] = 0;
                        }
                    }
                }
            }

            // Compute marginal totals in FShootsGrazed
            for (iComp = stSEEDL; iComp <= stLITT2; iComp++)
            {
                for (iPart = ptLEAF; iPart <= ptSTEM; iPart++)
                {
                    this.FShootsGrazed[iComp, iPart, TOTAL] = 0.0;
                    for (iDMD = 1; iDMD <= HerbClassNo; iDMD++)
                    {
                        this.FShootsGrazed[iComp, iPart, TOTAL] = this.FShootsGrazed[iComp, iPart, TOTAL] + this.FShootsGrazed[iComp, iPart, iDMD];
                    }
                }

                for (iDMD = TOTAL; iDMD <= HerbClassNo; iDMD++)
                {
                    this.FShootsGrazed[iComp, TOTAL, iDMD] = 0.0;
                    for (iPart = ptLEAF; iPart <= ptSTEM; iPart++)
                    {
                        this.FShootsGrazed[iComp, TOTAL, iDMD] = this.FShootsGrazed[iComp, TOTAL, iDMD] + this.FShootsGrazed[iComp, iPart, iDMD];
                    }
                }
            }

            for (iPart = TOTAL; iPart <= ptSTEM; iPart++)
            {
                for (iDMD = TOTAL; iDMD <= HerbClassNo; iDMD++)
                {
                    this.FShootsGrazed[TOTAL, iPart, iDMD] = 0.0;
                    for (iComp = stSEEDL; iComp <= stLITT2; iComp++)
                    {
                        this.FShootsGrazed[TOTAL, iPart, iDMD] = this.FShootsGrazed[TOTAL, iPart, iDMD] + this.FShootsGrazed[iComp, iPart, iDMD];
                    }
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="source"></param>
        /// <param name="flux"></param>
        /// <param name="priorFluxes"></param>
        /// <returns></returns>
        public double LimitedFlux(DM_Pool source, double flux, ref double priorFluxes)
        {
            double result = Math.Max(0.0, Math.Min(flux, source.DM - priorFluxes));
            priorFluxes += result;

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="fSource"></param>
        /// <param name="fPropn"></param>
        /// <param name="fPriorFluxes"></param>
        /// <returns></returns>
        public double LimitedPropn(DM_Pool fSource, double fPropn, ref double fPriorFluxes)
        {
            double result = Math.Max(0.0, Math.Min(fPropn * fSource.DM, fSource.DM - fPriorFluxes));
            fPriorFluxes += result;

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="newSeeds"></param>
        /// <param name="germinSeeds"></param>
        private void UpdateSeeds(DM_Pool newSeeds, ref DM_Pool germinSeeds)
        {
            double[,] fPriorFluxes = new double[HARD + 1, RIPE + 1];    // [SOFT..HARD,UNRIPE..RIPE]
            double[,] fGrazed = new double[HARD + 1, RIPE + 1];         // [SOFT..HARD,UNRIPE..RIPE]
            double[,] fDying = new double[HARD + 1, RIPE + 1];          // [SOFT..HARD,UNRIPE..RIPE]
            double[] fRipened = new double[HARD + 1];                   // [SOFT..HARD]
            double[] fDiscarded = new double[HARD + 1];                 // [SOFT..HARD]
            double fHardened;
            double fSoftened;
            double fGerminated;
            DM_Pool Eaten = new DM_Pool();
            double fPropn;
            int iSoft;
            int iRipe;
            int iLayer;

            PastureUtil.ZeroPool(ref germinSeeds);

            for (iLayer = 1; iLayer <= this.FSeedLayers; iLayer++)
            {                                                                                       // All flows occur within a seed layer...
                PastureUtil.FillArray(fPriorFluxes, 0.0);                                           // Convert all rates to g/m^2/d. The order of calculation is important
                for (iSoft = SOFT; iSoft <= HARD; iSoft++)
                {
                    for (iRipe = UNRIPE; iRipe <= RIPE; iRipe++)                                    // if the flux rates exceed the pool size
                    {
                        if (iLayer == 1)
                        {
                            fPropn = Div0(this.FSeeds[iSoft, iRipe, iLayer].DM, this.FSeeds[iSoft, TOTAL, iLayer].DM);
                            fGrazed[iSoft, iRipe] = this.LimitedFlux(this.FSeeds[iSoft, iRipe, iLayer],
                                                                  this.FSeedGrazed[iRipe] * fPropn,
                                                                  ref fPriorFluxes[iSoft, iRipe]);
                        }
                        else
                        {
                            fGrazed[iSoft, iRipe] = 0.0;
                        }

                        fDying[iSoft, iRipe] = this.LimitedPropn(this.FSeeds[iSoft, iRipe, iLayer],
                                                              this.FSeedDeathRate[iSoft, iLayer],
                                                              ref fPriorFluxes[iSoft, iRipe]);
                    }
                }

                fGerminated = this.LimitedPropn(this.FSeeds[SOFT, RIPE, iLayer], this.FGermnRate[iLayer], ref fPriorFluxes[SOFT, RIPE]);
                fSoftened = this.LimitedPropn(this.FSeeds[HARD, RIPE, iLayer], this.FSoftenRate[iLayer], ref fPriorFluxes[HARD, RIPE]);
                for (iSoft = SOFT; iSoft <= HARD; iSoft++)
                {
                    fRipened[iSoft] = this.LimitedPropn(this.FSeeds[iSoft, UNRIPE, iLayer], this.FRipenRate, ref fPriorFluxes[iSoft, UNRIPE]);
                    fDiscarded[iSoft] = this.LimitedPropn(this.FSeeds[iSoft, UNRIPE, iLayer], this.FDiscardRate, ref fPriorFluxes[iSoft, UNRIPE]);
                }

                fHardened = this.LimitedPropn(this.FSeeds[SOFT, UNRIPE, iLayer], this.FHardenRate, ref fPriorFluxes[SOFT, UNRIPE]);

                if ((this.GermnIndex > 0.0) && (fRipened[SOFT] > 0.0))
                {
                    this.GermnIndex = (this.FSeeds[SOFT, RIPE, 1].DM - fGerminated)
                                    / (this.FSeeds[SOFT, RIPE, 1].DM - fGerminated + fRipened[SOFT]);
                }

                // Now execute the mass flows
                for (iSoft = SOFT; iSoft <= HARD; iSoft++)
                {
                    for (iRipe = UNRIPE; iRipe <= RIPE; iRipe++)
                    {
                        this.MovePool(fGrazed[iSoft, iRipe], ref this.FSeeds[iSoft, iRipe, iLayer], ref Eaten);
                        this.MoveToResidue(fDying[iSoft, iRipe],
                                       ref this.FSeeds[iSoft, iRipe, iLayer],
                                       stESTAB, ptSEED, TOTAL, iLayer);
                    }
                }

                this.MovePool(fHardened, ref this.FSeeds[SOFT, UNRIPE, iLayer], ref this.FSeeds[HARD, UNRIPE, iLayer]);
                for (iSoft = SOFT; iSoft <= HARD; iSoft++)
                {
                    this.MovePool(fRipened[iSoft], ref this.FSeeds[iSoft, UNRIPE, iLayer], ref this.FSeeds[iSoft, RIPE, iLayer]);
                    this.MoveToResidue(fDiscarded[iSoft], ref this.FSeeds[iSoft, UNRIPE, iLayer], ptSEED, TOTAL, iLayer);
                }

                this.MovePool(fSoftened, ref this.FSeeds[HARD, RIPE, iLayer], ref this.FSeeds[SOFT, RIPE, iLayer]);
                this.MovePool(fGerminated, ref this.FSeeds[SOFT, RIPE, iLayer], ref germinSeeds);
            }

            this.AddPool(newSeeds, ref this.FSeeds[SOFT, UNRIPE, 1]);

            for (iSoft = SOFT; iSoft <= HARD; iSoft++)
            {
                for (iRipe = UNRIPE; iRipe <= RIPE; iRipe++)
                {
                    for (iLayer = 1; iLayer <= this.FSeedLayers; iLayer++)
                    {
                        PastureUtil.ZeroRoundOff(ref this.FSeeds[iSoft, iRipe, iLayer]);
                    }
                }
            }
        }

        /// <summary>
        /// Compute marginal totals in various arrays
        /// </summary>
        private void ComputeTotals()
        {
            int iCohort;
            int iSoft;
            int iRipe;
            int iLayer;
            int iPart;
            int iDMD;

            for (iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
            {
                this.FCohorts[iCohort].ComputeTotals();
            }

            if (this.Params.bHasSeeds)
            {
                for (iLayer = 1; iLayer <= this.FSeedLayers; iLayer++)
                {
                    for (iRipe = GrazType.UNRIPE; iRipe <= GrazType.RIPE; iRipe++)
                    {
                        PastureUtil.ZeroPool(ref this.FSeeds[GrazType.TOTAL, iRipe, iLayer]);
                        for (iSoft = GrazType.SOFT; iSoft <= GrazType.HARD; iSoft++)
                        {
                            this.AddPool(this.FSeeds[iSoft, iRipe, iLayer], ref this.FSeeds[TOTAL, iRipe, iLayer]);
                        }
                    }

                    for (iSoft = GrazType.TOTAL; iSoft <= GrazType.HARD; iSoft++)
                    {
                        PastureUtil.ZeroPool(ref this.FSeeds[iSoft, GrazType.TOTAL, iLayer]);
                        for (iRipe = GrazType.UNRIPE; iRipe <= GrazType.RIPE; iRipe++)
                        {
                            this.AddPool(this.FSeeds[iSoft, iRipe, iLayer], ref this.FSeeds[iSoft, TOTAL, iLayer]);
                        }
                    }
                }

                for (iSoft = GrazType.TOTAL; iSoft <= GrazType.HARD; iSoft++)
                {
                    for (iRipe = GrazType.TOTAL; iRipe <= GrazType.RIPE; iRipe++)
                    {
                        PastureUtil.ZeroPool(ref this.FSeeds[iSoft, iRipe, GrazType.TOTAL]);
                        for (iLayer = 1; iLayer <= this.FSeedLayers; iLayer++)
                        {
                            this.AddPool(this.FSeeds[iSoft, iRipe, iLayer], ref this.FSeeds[iSoft, iRipe, GrazType.TOTAL]);
                        }
                    }
                }
            }

            for (iDMD = 1; iDMD <= HerbClassNo; iDMD++)
            {
                this.FShootsKilled[GrazType.TOTAL, iDMD] = 0.0;
                for (iPart = GrazType.ptLEAF; iPart <= GrazType.ptSTEM; iPart++)
                {
                    this.FShootsKilled[GrazType.TOTAL, iDMD] = this.FShootsKilled[GrazType.TOTAL, iDMD] + this.FShootsKilled[iPart, iDMD];
                }
            }

            for (iPart = GrazType.TOTAL; iPart <= GrazType.ptSTEM; iPart++)
            {
                this.FShootsKilled[iPart, GrazType.TOTAL] = 0.0;
                for (iDMD = 1; iDMD <= HerbClassNo; iDMD++)
                {
                    this.FShootsKilled[iPart, GrazType.TOTAL] = this.FShootsKilled[iPart, GrazType.TOTAL] + this.FShootsKilled[iPart, iDMD];
                }
            }
        }

        /// <summary>
        /// Computes the fShootLosses[] and DigDecline[] arrays
        /// </summary>
        private void StoreFlowDenominators()
        {
            int iCohort;
            int iComp;

            PastureUtil.FillArray(this.FShootLossDenom, 0.0);
            this.FRootLossDenom = 0.0;

            for (iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
            {
                iComp = this.FCohorts[iCohort].Status;
                this.FShootLossDenom[iComp] = this.FShootLossDenom[iComp] + this.FCohorts[iCohort].Herbage[TOTAL, TOTAL].DM;

                if (iComp == stSEEDL || iComp == stESTAB || iComp == stSENC)
                {
                    this.FRootLossDenom += this.FCohorts[iCohort].Roots[TOTAL, TOTAL].DM;
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        private void StoreFlowRates()
        {
            int iCohort;
            int iComp;
            int iPart;
            int iDMD;
            int iLayer;
            int iAge;

            PastureUtil.FillArray(this.FShootFluxLoss, 0.0);
            PastureUtil.FillArray(this.FShootRespireLoss, 0.0);
            PastureUtil.FillArray(this.FDigDecline, 0.0);
            this.FRootRespireLoss = 0.0;

            for (iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
            {
                iComp = this.FCohorts[iCohort].Status;
                for (iPart = ptLEAF; iPart <= ptSTEM; iPart++)
                {
                    for (iDMD = 1; iDMD <= HerbClassNo; iDMD++)
                    {
                        this.FShootFluxLoss[iComp] = this.FShootFluxLoss[iComp] + this.FCohorts[iCohort].FBiomassExitGM2[iPart, iDMD];
                        this.FShootRespireLoss[iComp] = this.FShootRespireLoss[iComp] + Math.Max(0.0, -this.FCohorts[iCohort].FShootNetGrowth[iPart, iDMD].DM);
                        this.FDigDecline[iComp] = this.FDigDecline[iComp] + this.FCohorts[iCohort].FDMDDecline[iPart, iDMD] * this.FCohorts[iCohort].Herbage[iPart, iDMD].DM;
                        if (iPart == ptSTEM)
                        {
                            this.FDigDecline[iComp] = this.FDigDecline[iComp] + this.FCohorts[iCohort].FStemTransloc[iDMD];
                        }
                    }
                }

                if (this.BelongsIn(iCohort, sgGREEN))
                {
                    for (iLayer = 1; iLayer <= this.FSoilLayerCount; iLayer++)
                    {
                        for (iAge = EFFR; iAge <= OLDR; iAge++)
                        {
                            this.FRootRespireLoss += Math.Max(0.0, -this.FCohorts[iCohort].FRootNetGrowth[iAge, iLayer].DM);
                        }
                    }
                }
            }

            this.FRootDeathLoss = 0.0;
            for (iLayer = 1; iLayer <= this.FSoilLayerCount; iLayer++)
            {
                this.FRootDeathLoss += this.FRootResidue[iLayer].DM;
            }

            for (iComp = stSEEDL; iComp <= stLITT2; iComp++)
            {
                if (this.FShootLossDenom[iComp] > 0.0)
                {
                    this.FDigDecline[iComp] = (this.FDigDecline[iComp] + this.FShootRespireLoss[iComp]) / this.FShootLossDenom[iComp];
                }
                else
                {
                    this.FDigDecline[iComp] = 0.0;
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        private double GetPhenoCode()
        {
            double fIndex;

            switch (this.Phenology)
            {
                case PastureUtil.TDevelopType.Vernalizing:
                case PastureUtil.TDevelopType.DormantW:
                    fIndex = this.VernIndex;
                    break;
                case PastureUtil.TDevelopType.Vegetative:
                case PastureUtil.TDevelopType.Reproductive:
                case PastureUtil.TDevelopType.SprayTopped:
                    fIndex = this.DegDays;
                    break;
                case PastureUtil.TDevelopType.Dormant:
                    fIndex = this.DormDays;
                    break;
                default:
                    fIndex = 0.0;
                    break;
            }

            return this.EncodePhenology(this.Phenology, fIndex);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private double EncodePhenology(PastureUtil.TDevelopType stage, double index)
        {
            double result;
            switch (stage)
            {
                case PastureUtil.TDevelopType.Vernalizing:
                    result = (int)stage + index;
                    break;
                case PastureUtil.TDevelopType.Vegetative:
                case PastureUtil.TDevelopType.Reproductive:
                case PastureUtil.TDevelopType.SprayTopped:
                    result = (int)stage + index / 10000.0;
                    break;
                case PastureUtil.TDevelopType.Dormant:
                    result = (int)stage + index / 1000.0;
                    break;
                case PastureUtil.TDevelopType.DormantW:
                    result = (int)stage + Math.Min(index, 1.0);
                    break;
                default:
                    result = (int)stage;
                    break;
            }

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="code"></param>
        /// <param name="stage"></param>
        /// <param name="index"></param>
        private void DecodePhenology(double code, ref PastureUtil.TDevelopType stage, ref double index)
        {
            if ((code >= 0) && (code < 7.0))
            {
                // convert the code to a stage enum value
                stage = (TDevelopType)Math.Truncate(code);
                switch (stage)
                {
                    case PastureUtil.TDevelopType.Vernalizing:
                    case PastureUtil.TDevelopType.DormantW:
                        index = PastureUtil.Frac(code);
                        break;
                    case PastureUtil.TDevelopType.Vegetative:
                    case PastureUtil.TDevelopType.Reproductive:
                    case PastureUtil.TDevelopType.SprayTopped:
                        index = PastureUtil.Frac(code) * 10000.0;
                        break;
                    case PastureUtil.TDevelopType.Dormant:
                        index = PastureUtil.Frac(code) * 1000.0;
                        break;
                    default:
                        index = 0.0;
                        break;
                }
            }
            else if (code == 7.0)
            {
                stage = PastureUtil.TDevelopType.DormantW;
                index = 1.0;
            }
            else
            {
                stage = PastureUtil.TDevelopType.Senescent;
                index = 0.0;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        private void SetPhenoCode(double value)
        {
            double fIndex = 0;

            this.DecodePhenology(value, ref this.Phenology, ref fIndex);
            switch (this.Phenology)
            {
                case PastureUtil.TDevelopType.Vernalizing:
                case PastureUtil.TDevelopType.DormantW:
                    this.VernIndex = fIndex;
                    break;
                case PastureUtil.TDevelopType.Vegetative:
                case PastureUtil.TDevelopType.Reproductive:
                case PastureUtil.TDevelopType.SprayTopped:
                    this.DegDays = fIndex;
                    break;
                case PastureUtil.TDevelopType.Dormant:
                    this.DormDays = (int)Math.Round(fIndex);
                    break;
            }

            if ((this.Phenology == PastureUtil.TDevelopType.Senescent)
                    && (this.FSeeds[SOFT, UNRIPE, 1].DM + this.FSeeds[HARD, UNRIPE, 1].DM > 0.0)
                    && (this.Days_EDormant == -1))
            {
                this.Days_EDormant = 0;
            }
        }

        /// <summary>
        /// Set the conversion factor. Will scale results.
        /// </summary>
        /// <param name="value"></param>
        private void SetMassUnit(string value)
        {
            this.FMassUnit = value.ToLower();
            if ((this.FMassUnit == "") || (this.FMassUnit == "g/m^2"))
            {
                this.FMassScalar = 1.0;
            }
            else if (this.FMassUnit == "kg/ha")
            {
                this.FMassScalar = PastureUtil.GM2_KGHA;
            }
            else
            {
                throw new Exception("Invalid unit (" + value + ") for herbage masses");
            }
        }

        /// <summary>
        /// Herbage mass, in g/m^2
        /// Assumes that marginal totals in FCohorts[] are up-to-date
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="part">Plant part - leaf, stem, root, seed or total</param>
        /// <param name="DMD"></param>
        /// <returns></returns>
        public double HerbageMassGM2(int comp, int part, int DMD)
        {
            double result = 0.0;
            for (int iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
            {
                if ((comp == TOTAL) || (this.BelongsIn(iCohort, comp)))
                {
                    result += this.FCohorts[iCohort].Herbage[part, DMD].DM;
                }
            }

            return result;
        }

        /// <summary>
        /// Herbage mass, in user units
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="part">Plant part - leaf, stem, root, seed or total</param>
        /// <param name="DMD"></param>
        /// <returns></returns>
        public double GetHerbageMass(int comp, int part, int DMD)
        {
            return this.FMassScalar * this.HerbageMassGM2(comp, part, DMD);
        }

        /// <summary>
        /// Assign herbage mass, in user units
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="part">Plant part - leaf, stem, root, seed or total</param>
        /// <param name="DMD"></param>
        /// <param name="value"></param>
        public void SetHerbageMass(int comp, int part, int DMD, double value)
        {
            int iCohort;

            value /= this.FMassScalar;
            if ((comp >= stSEEDL) && (comp <= stLITT2))
            {
                iCohort = this.MakeOneCohort(comp);
                this.FCohorts[iCohort].SetHerbageDM(part, DMD, value);
            }
        }

        /// <summary>
        ///  Mass of nutrients in herbage (user units)
        /// * Assumes that marginal totals in FCohorts[] are up-to-date
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="part">Plant part - leaf, stem, root, seed or total</param>
        /// <param name="DMD"></param>
        /// <param name="elem"></param>
        /// <returns></returns>
        public double GetHerbageNutr(int comp, int part, int DMD, TPlantElement elem)
        {
            double result = 0.0;
            for (int iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
            {
                if (this.BelongsIn(iCohort, comp))
                {
                    result += this.FCohorts[iCohort].GetHerbageNutr(part, DMD, elem);
                }
            }

            result *= this.FMassScalar;

            return result;
        }

        /// <summary>
        /// Assign mass of nutrients in herbage(user units)
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="part">Plant part - leaf, stem, root, seed or total</param>
        /// <param name="DMD"></param>
        /// <param name="elem"></param>
        /// <param name="value"></param>
        public void SetHerbageNutr(int comp, int part, int DMD, TPlantElement elem, double value)
        {
            int iCohort;
            {
                value /= this.FMassScalar;
                if ((comp >= stSEEDL) && (comp <= stLITT2))
                {
                    iCohort = this.MakeOneCohort(comp);
                    this.FCohorts[iCohort].SetHerbageNutr(part, DMD, elem, value);
                }
            }
        }

        /// <summary>
        /// Assumes that marginal totals in FCohorts[] are up-to-date
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="part">Plant part - leaf, stem, root, seed or total</param>
        /// <param name="iDMD"></param>
        /// <param name="elem"></param>
        /// <returns></returns>
        public double GetHerbageConc(int comp, int part, int iDMD, TPlantElement elem)
        {
            return PastureUtil.Div0(this.GetHerbageNutr(comp, part, iDMD, elem), this.GetHerbageMass(comp, part, iDMD));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="part">Plant part - leaf, stem, root, seed or total</param>
        /// <param name="DMD"></param>
        /// <param name="elem"></param>
        /// <param name="value"></param>
        public void SetHerbageConc(int comp, int part, int DMD, TPlantElement elem, double value)
        {
            this.SetHerbageNutr(comp, part, DMD, elem, value * this.GetHerbageMass(comp, part, DMD));
        }

        /// <summary>
        /// Assumes that marginal totals in FCohorts[] are up-to-date
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="age"></param>
        /// <param name="layer"></param>
        /// <returns>Root mass, in g/m^2</returns>
        private double RootMassGM2(int comp, int age, int layer)
        {
            double result = 0.0;
            for (int iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
            {
                if (this.BelongsIn(iCohort, comp))
                {
                    result += this.FCohorts[iCohort].Roots[age, layer].DM;
                }
            }

            return result;
        }

        /// <summary>
        /// Root mass in user units
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="age">Root age</param>
        /// <param name="layer">Soil layer</param>
        /// <returns></returns>
        public double GetRootMass(int comp, int age, int layer)
        {
            return this.FMassScalar * this.RootMassGM2(comp, age, layer);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="age"></param>
        /// <param name="layer">1-n</param>
        /// <param name="elem">N,P,S</param>
        /// <returns></returns>
        public double GetRootNutr(int comp, int age, int layer, TPlantElement elem)
        {
            int iCohort;

            double result = 0.0;
            for (iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
            {
                if (this.BelongsIn(iCohort, comp))
                {
                    result += this.FCohorts[iCohort].GetRootNutr(age, layer, elem);
                }
            }

            result *= this.FMassScalar;

            return result;
        }

        /// <summary>
        /// Set the nutrient value for this root cohort
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="age"></param>
        /// <param name="layer">1-n</param>
        /// <param name="elem">N,P,S</param>
        /// <param name="value">New value</param>
        public void SetRootNutr(int comp, int age, int layer, TPlantElement elem, double value)
        {
            int iCohort;

            if ((comp >= stSEEDL) && (comp <= stSENC))
            {
                value /= this.FMassScalar;
                iCohort = this.MakeOneCohort(comp);
                this.FCohorts[iCohort].SetRootNutr(age, layer, elem, value);
            }
        }

        /// <summary>
        /// Get root nutrient concentration
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="age"></param>
        /// <param name="layer">1-n</param>
        /// <param name="elem">N,P,S</param>
        /// <returns></returns>
        public double GetRootConc(int comp, int age, int layer, TPlantElement elem)
        {
            return PastureUtil.Div0(this.GetRootNutr(comp, age, layer, elem), this.GetRootMass(comp, age, layer));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="age"></param>
        /// <param name="layer"></param>
        /// <param name="elem"></param>
        /// <param name="value"></param>
        public void SetRootConc(int comp, int age, int layer, TPlantElement elem, double value)
        {
            if ((comp >= stSEEDL) && (comp <= stSENC))
            {
                this.SetRootNutr(comp, age, layer, elem, value * this.GetRootMass(comp, age, layer));
            }
        }

        /// <summary>
        /// Seed mass, in g/m^2
        /// *Assumes that marginal totals in FSeeds[] are up - to - date
        /// </summary>
        /// <param name="soft"></param>
        /// <param name="ripe"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        private double SeedMassGM2(int soft, int ripe, int layer)
        {
            return this.FSeeds[soft, ripe, layer].DM;
        }

        /// <summary>
        /// Seed mass, in user units
        /// </summary>
        /// <param name="soft"></param>
        /// <param name="ripe"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        public double GetSeedMass(int soft, int ripe, int layer)
        {
            return this.FMassScalar * this.SeedMassGM2(soft, ripe, layer);
        }

        /// <summary>
        /// Assign seed mass (user units)
        /// </summary>
        /// <param name="soft"></param>
        /// <param name="ripe"></param>
        /// <param name="layer">1-n</param>
        /// <param name="value"></param>
        public void SetSeedMass(int soft, int ripe, int layer, double value)
        {
            value /= this.FMassScalar;

            if ((soft != TOTAL) && (ripe != TOTAL) && (layer != TOTAL))
            {
                if (this.FSeeds[soft, ripe, layer].DM > 0.0)
                {
                    this.ResizePool(ref this.FSeeds[soft, ripe, layer], value);
                }
                else
                {
                    this.FSeeds[soft, ripe, layer] = this.MakeNewPool(ptSEED, value);
                }

                if (value > 0.0)
                {
                    this.FSeedLayers = Math.Max(this.FSeedLayers, layer);
                }
            }
            else if (layer == TOTAL)
            {
                this.SetSeedMass(soft, ripe, 1, value);
                for (int Ldx = 2; Ldx <= this.FSeedLayers; Ldx++)
                {
                    this.SetSeedMass(soft, ripe, Ldx, 0.0);
                }
            }

            this.ComputeTotals();

            if (this.FSeeds[SOFT, UNRIPE, 1].DM + this.FSeeds[HARD, UNRIPE, 1].DM == 0.0)
            {
                this.Days_EDormant = -1;
            }
            else if ((this.Phenology == PastureUtil.TDevelopType.Senescent) && (this.Days_EDormant == -1))
            {
                this.Days_EDormant = 0;
            }
        }

        /// <summary>
        /// * Assumes that marginal totals in FCohorts[] are up-to-date
        /// </summary>
        /// <param name="soft"></param>
        /// <param name="ripe"></param>
        /// <param name="layer">Soil layer 1-n</param>
        /// <param name="elem">N,P,S</param>
        /// <returns></returns>
        public double GetSeedNutr(int soft, int ripe, int layer, TPlantElement elem)
        {
            if (this.FElements.Contains(elem))
            {
                return this.FMassScalar * this.FSeeds[soft, ripe, layer].Nu[(int)elem];
            }
            else
            {
                return this.GetSeedMass(soft, ripe, layer) * this.GetSeedConc(soft, ripe, layer, elem);
            }
        }

        /// <summary>
        /// Assumes that marginal totals in FCohorts[] are up-to-date
        /// </summary>
        /// <param name="soft"></param>
        /// <param name="ripe"></param>
        /// <param name="layer"></param>
        /// <param name="elem"></param>
        /// <returns></returns>
        public double GetSeedConc(int soft, int ripe, int layer, TPlantElement elem)
        {
            double protConc;
            double result;
            if (this.FElements.Contains(elem))
            {
                result = PastureUtil.Div0(this.FSeeds[soft, ripe, layer].Nu[(int)elem], this.FSeeds[soft, ripe, layer].DM);
            }
            else
            {
                protConc = this.Params.Seed_Prot;
                switch (elem)
                {
                    case TPlantElement.N:
                        result = protConc / N2Protein;
                        break;
                    case TPlantElement.P:
                        result = protConc / N2Protein * PastureUtil.DEF_P2N;
                        break;
                    case TPlantElement.S:
                        result = protConc / N2Protein * PastureUtil.DEF_S2N;
                        break;
                    default:
                        result = 0.0;
                        break;
                }
            }

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="cohIdx"></param>
        /// <returns></returns>
        public double GetRootDepth(int comp, int cohIdx)
        {
            int iCohort;
            double result;
            if (cohIdx != ALL_COHORTS)
            {
                iCohort = this.FindCohort(comp, cohIdx);
                result = this.FCohorts[iCohort].RootDepth;
            }
            else
            {
                result = 0.0;
                for (iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
                {
                    if (this.BelongsIn(iCohort, comp))
                    {
                        result = Math.Max(result, this.FCohorts[iCohort].RootDepth);
                    }
                }
            }

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="cohIdx"></param>
        /// <param name="value"></param>
        public void SetRootDepth(int comp, int cohIdx, double value)
        {
            int iCohort;

            if (cohIdx != ALL_COHORTS)
            {
                iCohort = this.FindCohort(comp, cohIdx);
                this.FCohorts[iCohort].RootDepth = value;
                this.FCohorts[iCohort].SetDefaultRoots(EFFR, this.FCohorts[iCohort].Roots[EFFR, TOTAL].DM, value);
                this.FCohorts[iCohort].SetDefaultRoots(OLDR, this.FCohorts[iCohort].Roots[OLDR, TOTAL].DM, value);
            }
            else
            {
                for (iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
                {
                    if (this.BelongsIn(iCohort, comp))
                    {
                        this.FCohorts[iCohort].RootDepth = value;
                        this.FCohorts[iCohort].SetDefaultRoots(EFFR, this.FCohorts[iCohort].Roots[EFFR, TOTAL].DM, value);
                        this.FCohorts[iCohort].SetDefaultRoots(OLDR, this.FCohorts[iCohort].Roots[OLDR, TOTAL].DM, value);
                    }
                }
            }
        }

        /*
         function  getFrostCount(   iComp, iCohIdx : Integer ) : Single;
         procedure setFrostCount(   iComp, iCohIdx : Integer; fValue : Single );
        */

        /// <summary>
        /// Herbage available for grazing, in g/m^2
        /// Relies on the value of FPastureDM(total live+senc pasture of all species)
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="part">Plant part - leaf, stem, root, seed or total</param>
        /// <param name="DMD"></param>
        /// <returns></returns>
        public double AvailHerbageGM2(int comp, int part, int DMD)
        {
            double result;
            double greenPropn;

            if ((comp == stDEAD) || (comp == stLITT1))
            {
                result = this.HerbageMassGM2(comp, part, DMD);
            }
            else if (comp == stLITT2)
            {
                result = 0.0;
            }
            else if ((comp == sgDRY) || (comp == sgAV_DRY))
            {
                result = this.HerbageMassGM2(sgAV_DRY, part, DMD);
            }
            else
            {
                if (this.PastureGreenDM > PastureUtil.UNGRAZEABLE / this.Params.HeightRatio)
                {
                    greenPropn = 1.0 - (PastureUtil.UNGRAZEABLE / this.Params.HeightRatio) / this.PastureGreenDM;
                }
                else
                {
                    greenPropn = 0.0;
                }

                if (comp == TOTAL)
                {
                    result = greenPropn * this.HerbageMassGM2(sgGREEN, part, DMD) + this.HerbageMassGM2(sgAV_DRY, part, DMD);
                }
                else if (comp == sgSTANDING)
                {
                    result = greenPropn * this.HerbageMassGM2(sgGREEN, part, DMD) + this.HerbageMassGM2(stDEAD, part, DMD);
                }
                else
                {
                    result = greenPropn * this.HerbageMassGM2(comp, part, DMD);
                }
            }

            return result;
        }

        /// <summary>
        /// Initialise the model
        /// </summary>
        public void ReadParamsFromValues(string nutrients, string species, double maxroot, double[] kl, double[] ll)
        {
            string nutr = nutrients.ToUpper();

            List<TPlantElement> ElemList = new List<TPlantElement>();

            if (nutr.IndexOf("N") > -1)
            {
                ElemList.Add(TPlantElement.N);
            }

            if (nutr.IndexOf("P") > -1)
            {
                ElemList.Add(TPlantElement.P);
            }

            if (nutr.IndexOf("S") > -1)
            {
                ElemList.Add(TPlantElement.S);
            }

            this.Initialise("", ElemList.ToArray());
            this.SetParameters(species, null, "");
            this.ClearState();

            bool RecomputeRoots = maxroot < 0.0;
            if (!RecomputeRoots)
            {
                this.SetMaxRootDepth(maxroot);
            }

            if (kl != null && kl.Length > 0)
            {
                this.fWater_KL = new double[kl.Length + 1];
                kl.CopyTo(this.fWater_KL, 1);
            }

            if (ll != null && ll.Length > 0)
            {
                this.fPlant_LL = new double[ll.Length + 1];
                ll.CopyTo(this.fPlant_LL, 1);
            }
        }

        /// <summary></summary>
        public TPlantElement[] FElements = new TPlantElement[0];

        // State variables ---------------------------------------------------------

        /// <summary></summary>
        public double[] FExtinctionK = new double[GrazType.stLITT2 + 1]; // [stSEEDL..stLITT2]

        /// <summary>List of pasture cohorts</summary>
        public TPastureCohort[] FCohorts;

        /// <summary></summary>
        protected int FSeedLayers;

        /// <summary>
        /// Seed weights g/m^2
        /// B(,seed,,)
        /// </summary>
        protected GrazType.DM_Pool[,,] FSeeds = new GrazType.DM_Pool[GrazType.HARD + 1, GrazType.RIPE + 1, GrazType.MaxSoilLayers + 1];

        /// <summary>
        /// * Assumes that Params is known
        /// </summary>
        protected void ClearState()
        {
            int iComp;

            this.FFertScalar = 1.0;
            this.LaggedMeanT = -999.9;

            if (this.Params != null)
            {
                for (iComp = GrazType.stSEEDL; iComp <= GrazType.stSENC; iComp++)
                {
                    this.FExtinctionK[iComp] = this.Params.LightK[7];
                }

                this.FExtinctionK[GrazType.stDEAD] = this.Params.LightK[9];
                this.FExtinctionK[GrazType.stLITT1] = this.Params.LightK[10];
                this.FExtinctionK[GrazType.stLITT2] = this.Params.LightK[10];
            }

            this.Days_EDormant = -1;
            this.GermnIndex = 0.0;
            this.VernIndex = 0.0;
            this.DegDays = 0.0;
            this.FloweringLength = -1.0;
            this.FloweringTime = 0.0;
            this.fSencDays = 0.0;
            this.DormDays = 0;
            this.DormIndex = 0;
            this.DormMeanTemp = -999.9;

            while (this.CohortCount() > 0)
            {                                                                                   // Clear existing state data
                this.DeleteCohort(this.CohortCount() - 1);
            }

            this.MakeNewCohort(GrazType.stDEAD);                                                // Always have cohorts of standing dead & litter
            this.MakeNewCohort(GrazType.stLITT1);
            this.MakeNewCohort(GrazType.stLITT2);

            this.InitSeedPools();
            this.FSeedLayers = 1;

            if (this.Params != null)
            {
                this.StartNewCycle(true, true);
            }
        }

        /// <summary>
        /// Zero the seed pools
        /// </summary>
        private void InitSeedPools()
        {
            for (int h = 0; h <= HARD; h++)
            {
                for (int r = 0; r <= RIPE; r++)
                {
                    for (int iLayer = 0; iLayer <= MaxSoilLayers; iLayer++)
                    {
                        ZeroPool(ref this.FSeeds[h, r, iLayer]);
                    }
                }
            }
        }

        /// <summary>
        /// Move a proportion of the DM in SrcPool into DstPool
        /// </summary>
        /// <param name="propn"></param>
        /// <param name="srcPool"></param>
        /// <param name="dstPool"></param>
        public void MovePoolPropn(double propn, ref DM_Pool srcPool, ref DM_Pool dstPool)
        {
            this.MovePool(srcPool.DM * propn, ref srcPool, ref dstPool);
        }

        /// <summary>
        /// Move a mass out of a DM pool to
        /// </summary>
        /// <param name="mass"></param>
        /// <param name="srcPool"></param>
        /// <param name="comp">Herbage component</param>
        /// <param name="part">Plant part - leaf, stem, root, seed or total</param>
        /// <param name="DMD"></param>
        /// <param name="layer"></param>
        public void MoveToResidue(double mass, ref DM_Pool srcPool, int comp, int part, int DMD, int layer = 1)
        {
            double fPropn;
            DM_Pool Delta = new DM_Pool();

            if (srcPool.DM > 0.0)
            {
                var values = Enum.GetValues(typeof(TPlantElement)).Cast<TPlantElement>().ToArray();
                Delta.DM = Math.Min(mass, srcPool.DM);
                fPropn = Delta.DM / srcPool.DM;
                foreach (var Elem in values)
                {
                    Delta.Nu[(int)Elem] = fPropn * srcPool.Nu[(int)Elem];
                }

                Delta.AshAlk = fPropn * srcPool.AshAlk;

                srcPool.DM -= Delta.DM;
                foreach (var Elem in values)
                {
                    srcPool.Nu[(int)Elem] = srcPool.Nu[(int)Elem] - Delta.Nu[(int)Elem];
                }

                srcPool.AshAlk -= Delta.AshAlk;

                // Provide default values for nutrients
                foreach (var Elem in values)
                {
                    if (!this.ElementSet.Contains(Elem))
                    {
                        switch (part)
                        {
                            case ptLEAF:
                            case ptSTEM:
                                Delta.Nu[(int)Elem] = Delta.DM * this.GetHerbageConc(comp, part, DMD, Elem);
                                break;
                            case ptROOT:
                                Delta.Nu[(int)Elem] = Delta.DM * this.GetRootConc(comp, TOTAL, layer, Elem);
                                break;
                            case ptSEED:
                                Delta.Nu[(int)Elem] = Delta.DM * this.GetSeedConc(TOTAL, TOTAL, layer, Elem);
                                break;
                        }
                    }
                }

                if (!this.ElementSet.Contains(TPlantElement.N))
                {
                    switch (part)
                    {
                        case ptLEAF:
                        case ptSTEM:
                            Delta.AshAlk = Delta.DM * this.HerbageAshAlkalinity(comp, part, DMD);
                            break;
                        case ptROOT:
                            Delta.AshAlk = Delta.DM * this.RootAshAlkalinity(comp, TOTAL, layer);
                            break;
                        case ptSEED:
                            Delta.AshAlk = Delta.DM * this.SeedAshAlkalinity(TOTAL, TOTAL, layer);
                            break;
                    }
                }

                if ((part == ptLEAF) || (part == ptSTEM))
                {
                    AddPool2(Delta, ref this.FTopResidue[part]);
                }
                else
                {
                    AddPool2(Delta, ref this.FRootResidue[layer]);
                }
            }
        }

        /// <summary>
        /// Determines whether a given cohort (iCohort) is part of a nominated herbage
        /// component(iComp), i.e.it has a given status
        /// </summary>
        /// <param name="cohort">The cohort index</param>
        /// <param name="comp">The herbage component</param>
        /// <returns></returns>
        protected bool BelongsIn(int cohort, int comp)
        {
            bool result = false;

            switch (comp)
            {
                case GrazType.TOTAL:
                    result = true;
                    break;
                case GrazType.stSEEDL:
                case GrazType.stESTAB:
                case GrazType.stSENC:
                case GrazType.stDEAD:
                case GrazType.stLITT1:
                case GrazType.stLITT2:
                    result = (this.FCohorts[cohort].Status == comp);
                    break;
                case GrazType.sgGREEN:
                    result = (this.FCohorts[cohort].Status >= GrazType.stSEEDL) && (this.FCohorts[cohort].Status <= GrazType.stSENC);
                    break;
                case GrazType.sgEST_SENC:
                    result = (this.FCohorts[cohort].Status >= GrazType.stESTAB) && (this.FCohorts[cohort].Status <= GrazType.stSENC);
                    break;
                case GrazType.sgDRY:
                    result = (this.FCohorts[cohort].Status >= GrazType.stDEAD) && (this.FCohorts[cohort].Status <= GrazType.stLITT2);
                    break;
                case GrazType.sgAV_DRY:
                    result = (this.FCohorts[cohort].Status >= GrazType.stDEAD) && (this.FCohorts[cohort].Status <= GrazType.stLITT1);
                    break;
                case GrazType.sgSTANDING:
                    result = (this.FCohorts[cohort].Status >= GrazType.stSEEDL) && (this.FCohorts[cohort].Status <= GrazType.stDEAD);
                    break;
                case GrazType.sgLITTER:
                    result = (this.FCohorts[cohort].Status >= GrazType.stLITT1) && (this.FCohorts[cohort].Status <= GrazType.stLITT2);
                    break;
            }

            return result;
        }

        /// <summary>
        /// Find a cohort with a given status. By default, finds the first such cohort.
        /// </summary>
        /// <param name="status"></param>
        /// <param name="cohIdx">Cohort index</param>
        /// <returns></returns>
        public int FindCohort(int status, int cohIdx = 0)
        {
            int result;
            int iCount;

            iCount = this.CohortCount();
            result = 0;
            while ((result < iCount)
                  && !((this.FCohorts[result].Status == status) && (cohIdx == 0)))
            {
                if (this.FCohorts[result].Status == status)
                {
                    cohIdx--;
                }

                result++;
            }

            if (result == iCount)
            {
                result = -1;
            }

            return result;
        }

        /// <summary>
        /// Creates a new cohort (sub-population) with the nominated status.
        /// * This method is responsible for ensuring that the cohorts remain sorted
        ///   in order:
        ///   <![CDATA[seedlings < established, live < senescing < standing dead < litter ]]>
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        protected int MakeNewCohort(int status)
        {
            int result;
            int iCount;
            int iNewCohort;
            int Idx;

            iCount = this.CohortCount();

            iNewCohort = 0;
            while ((iNewCohort < iCount) && (this.FCohorts[iNewCohort].Status < status))
            {
                iNewCohort++;
            }

            Array.Resize(ref this.FCohorts, iCount + 1);
            for (Idx = iCount; Idx >= iNewCohort + 1; Idx--)
            {
                this.FCohorts[Idx] = this.FCohorts[Idx - 1];
            }

            this.FCohorts[iNewCohort] = new TPastureCohort(this, status);

            if (status == stSEEDL)
            {
                this.FCohorts[iNewCohort].EstabIndex = 1.0;
            }

            if ((status == stESTAB) || (status == stSENC))
            {
                this.FCohorts[iNewCohort].RootDepth = this.FMaxRootDepth;
            }

            result = iNewCohort;

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="cohort"></param>
        protected void DeleteCohort(int cohort)
        {
            for (int Idx = cohort + 1; Idx <= this.FCohorts.Length - 1; Idx++)
            {
                this.FCohorts[Idx - 1] = this.FCohorts[Idx];
            }

            Array.Resize(ref this.FCohorts, this.FCohorts.Length - 1);
        }

        /// <summary>
        /// Combines two herbage cohorts, leaving the result in iCohort1
        /// </summary>
        /// <param name="cohort1"></param>
        /// <param name="cohort2"></param>
        protected void MergeCohorts(int cohort1, int cohort2)
        {
            double fShoot1, fShoot2;
            int iPart, iDMD;

            // PastureUtil.TGrowthLimit Limit;
            int limit;

            if (this.FCohorts[cohort1].Status != this.FCohorts[cohort2].Status)
            {
                throw new Exception("Cannot merge herbage cohorts");
            }

            fShoot1 = this.FCohorts[cohort1].Herbage[TOTAL, TOTAL].DM;                                      // Weighting factors
            fShoot2 = this.FCohorts[cohort2].Herbage[TOTAL, TOTAL].DM;

            // Merge specific herbage areas
            for (iPart = ptLEAF; iPart <= ptSTEM; iPart++)
            {
                for (iDMD = TOTAL; iDMD <= HerbClassNo; iDMD++)
                {
                    this.FCohorts[cohort1].SpecificArea[iPart, iDMD] = WeightAverage(this.FCohorts[cohort1].SpecificArea[iPart, iDMD],
                                                                                  this.FCohorts[cohort1].Herbage[iPart, iDMD].DM,
                                                                                  this.FCohorts[cohort2].SpecificArea[iPart, iDMD],
                                                                                  this.FCohorts[cohort2].Herbage[iPart, iDMD].DM);
                }
            }

            // Merge biomass pools
            for (iPart = TOTAL; iPart <= ptSTEM; iPart++)
            {
                for (iDMD = TOTAL; iDMD <= HerbClassNo; iDMD++)
                {
                    this.AddPool(this.FCohorts[cohort2].Herbage[iPart, iDMD], ref this.FCohorts[cohort1].Herbage[iPart, iDMD]);
                }
            }

            this.FCohorts[cohort1].AddRoots(this.FCohorts[cohort2]);

            this.FCohorts[cohort1].FrostFactor = WeightAverage(this.FCohorts[cohort1].FrostFactor, fShoot1,
                                                              this.FCohorts[cohort2].FrostFactor, fShoot2);

            if (this.BelongsIn(cohort1, stSEEDL))
            {
                this.FCohorts[cohort1].EstabIndex = WeightAverage(this.FCohorts[cohort1].EstabIndex, fShoot1,
                                                                 this.FCohorts[cohort2].EstabIndex, fShoot2);
                this.FCohorts[cohort1].EstabIndex = WeightAverage(this.FCohorts[cohort1].SeedlStress, fShoot1,
                                                                 this.FCohorts[cohort2].SeedlStress, fShoot2);
            }
            else if (this.BelongsIn(cohort1, sgEST_SENC))
            {
                this.FCohorts[cohort1].StemReserve = this.FCohorts[cohort1].StemReserve + this.FCohorts[cohort2].StemReserve;
            }

            // Merge the reporting variables
            if (this.BelongsIn(cohort1, sgGREEN))
            {
                this.FCohorts[cohort1].RootTranslocSum = this.FCohorts[cohort1].RootTranslocSum + this.FCohorts[cohort2].RootTranslocSum;
                this.FCohorts[cohort1].StemTranslocSum = this.FCohorts[cohort1].StemTranslocSum + this.FCohorts[cohort2].StemTranslocSum;
                this.FCohorts[cohort1].fR2S_Target = this.FCohorts[cohort1].fR2S_Target + this.FCohorts[cohort2].fR2S_Target;
                for (iPart = TOTAL; iPart <= ptSEED; iPart++)
                {
                    this.FCohorts[cohort1].fMaintRespiration[iPart] = this.FCohorts[cohort1].fMaintRespiration[iPart] + this.FCohorts[cohort2].fMaintRespiration[iPart];
                    this.FCohorts[cohort1].fGrowthRespiration[iPart] = this.FCohorts[cohort1].fGrowthRespiration[iPart] + this.FCohorts[cohort2].fGrowthRespiration[iPart];
                }

                for (iPart = ptLEAF; iPart <= ptSEED; iPart++)
                {
                    this.FCohorts[cohort1].Allocation[iPart] = WeightAverage(this.FCohorts[cohort1].Allocation[iPart], fShoot1,
                                                                         this.FCohorts[cohort2].Allocation[iPart], fShoot2);
                }

                this.FCohorts[cohort1].LimitFactors[(int)PastureUtil.TGrowthLimit.glGAI] = this.FCohorts[cohort1].LimitFactors[(int)PastureUtil.TGrowthLimit.glGAI]
                                                                                     + this.FCohorts[cohort2].LimitFactors[(int)PastureUtil.TGrowthLimit.glGAI];
                for (limit = (int)PastureUtil.TGrowthLimit.glVPD; limit <= (int)PastureUtil.TGrowthLimit.gl_S; limit++)
                {
                    this.FCohorts[cohort1].LimitFactors[limit] = WeightAverage(this.FCohorts[cohort1].LimitFactors[limit], fShoot1,
                                                                        this.FCohorts[cohort2].LimitFactors[limit], fShoot2);
                }
            }

            this.DeleteCohort(cohort2);
        }

        /// <summary>
        ///  Combine all cohorts with a given status into a single cohort
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        protected int MakeOneCohort(int status)
        {
            int iCount, Idx;
            int result;

            iCount = this.CohortCount(status);
            if (iCount == 0)
            {
                result = this.MakeNewCohort(status);
            }
            else
            {
                result = this.FindCohort(status, 0);
                for (Idx = iCount - 1; Idx >= 1; Idx--)
                {
                    this.MergeCohorts(result, this.FindCohort(status, Idx));
                }
            }

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="cohortSet"></param>
        protected void ClearEmptyCohorts(int cohortSet)
        {
            int iCohort;

            for (iCohort = this.CohortCount() - 1; iCohort >= 0; iCohort--)
            {
                if (this.BelongsIn(iCohort, cohortSet) && (this.FCohorts[iCohort].Herbage[TOTAL, TOTAL].DM < 0.1 * VERYSMALL))
                {
                    if ((iCohort == stDEAD) || (iCohort == stLITT1) || (iCohort == stLITT2)
                       || (this.FCohorts[iCohort].Roots[TOTAL, TOTAL].DM < VERYSMALL))
                    {
                        this.DeleteCohort(iCohort);
                    }
                    else
                    {
                        // This case is a summer - dormant popn
                        for (int i = 0; i <= ptSTEM; i++)
                        {
                            for (int j = 0; j <= HerbClassNo; j++)
                            {
                                ZeroDMPool(ref this.FCohorts[iCohort].Herbage[i, j]);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Moves the roots from one herbage cohort into another
        /// </summary>
        /// <param name="srcCohort"></param>
        /// <param name="destCohort"></param>
        protected void TransferCohortRoots(int srcCohort, int destCohort)
        {
            this.FCohorts[destCohort].AddRoots(this.FCohorts[srcCohort]);
            this.FCohorts[srcCohort].ClearRoots();
        }

        /// <summary>
        /// Read the state (at init time)
        /// </summary>
        /// <param name="laggedT"></param>
        /// <param name="phen"></param>
        /// <param name="flowerlen"></param>
        /// <param name="flowertime"></param>
        /// <param name="sencidx"></param>
        /// <param name="dormidx"></param>
        /// <param name="dormt"></param>
        /// <param name="extintc"></param>
        /// <param name="green"></param>
        /// <param name="dry"></param>
        /// <param name="seed">Array of seeds in layers [0..</param>
        /// <param name="seeddormtime"></param>
        /// <param name="germidx"></param>
        public void ReadStateFromValues(double laggedT, double phen, double flowerlen, double flowertime, double sencidx,
                                        double dormidx, double dormt, double[] extintc,
                                        GreenInit[] green, DryInit[] dry, SeedInit seed, double seeddormtime, double germidx)
        {
            string sPrevUnit;
            double fPhenology;
            double fFlowerLen;
            double fFlowerTime;
            double fSencIndex;
            int iDormIndex;
            double fDormT;
            string sStatus;
            int iCohort;
            int idx, jdx;
            int iLayer;

            sPrevUnit = this.MassUnit;
            this.MassUnit = "g/m^2";

            this.LaggedMeanT = laggedT;

            fPhenology = phen;
            fFlowerLen = flowerlen; // or this.Params.DevelopK[7]);
            fFlowerTime = flowertime;
            fSencIndex = sencidx;
            iDormIndex = Convert.ToInt32(dormidx);
            fDormT = dormt; // or this.LaggedMeanT);

            if (fPhenology != 0.0)
            {
                this.PhenoCode = fPhenology;
            }

            if (this.Params.bHasSeeds && (this.Phenology == PastureUtil.TDevelopType.Reproductive || this.Phenology == PastureUtil.TDevelopType.SprayTopped))
            {
                this.FloweringLength = Math.Max(0.0, Math.Min(fFlowerLen, this.Params.DevelopK[7]));      // Length of flowering period

                // Days since the start of flowering
                if (this.DegDays <= this.Params.DevelopK[6])
                {
                    this.FloweringTime = 0.0;
                }
                else if (fFlowerTime >= 0.0)
                {
                    this.FloweringTime = Math.Min(fFlowerTime, this.FloweringLength);
                }
                else
                {
                    // Assume 10dd per day accrues to get a default value for the flowering time
                    fFlowerTime = (this.DegDays - this.Params.DevelopK[6]) / 10.0;
                    this.FloweringTime = Math.Min(fFlowerTime, this.FloweringLength);
                }
            }

            if (this.Phenology == PastureUtil.TDevelopType.Reproductive || this.Phenology == PastureUtil.TDevelopType.SprayTopped)
            {
                this.fSencDays = fSencIndex;
            }
            else if (this.Phenology == PastureUtil.TDevelopType.Dormant)
            {
                this.DormIndex = iDormIndex;
                if (this.DormIndex > 0)
                {
                    this.DormMeanTemp = fDormT;
                }
            }

            if (extintc != null)
            {
                if ((extintc.Length >= 1) && this.Params.bHasSeeds)
                {
                    this.SetExtinctionCoeff(GrazType.stSEEDL, extintc[0]);
                }

                if (extintc.Length >= 2)
                {
                    this.SetExtinctionCoeff(GrazType.stESTAB, extintc[1]);
                }

                if (extintc.Length >= 3)
                {
                    this.SetExtinctionCoeff(GrazType.stSENC, extintc[2]);
                }
            }

            if ((green != null) && green.Length > 0)
            {
                for (idx = 0; idx < green.Length; idx++)
                {
                    sStatus = green[idx].status; // or PastureUtil.StatusName[GrazType.stESTAB]
                    if (sStatus == PastureUtil.StatusName[GrazType.stSEEDL])
                    {
                        iCohort = this.MakeNewCohort(GrazType.stSEEDL);
                    }
                    else if (sStatus == PastureUtil.StatusName[GrazType.stSENC])
                    {
                        iCohort = this.MakeNewCohort(GrazType.stSENC);
                    }
                    else
                    {
                        iCohort = this.MakeNewCohort(GrazType.stESTAB);
                    }
                    this.FCohorts[iCohort].ReadFromValue(green[idx], null, true);
                }
            }

            if ((dry != null) && dry.Length > 0)
            {
                for (idx = 0; idx < dry.Length; idx++)
                {
                    sStatus = dry[idx].status; // or  PastureUtil.StatusName[GrazType.stDEAD]
                    if (sStatus == PastureUtil.StatusName[GrazType.stLITT1])
                    {
                        iCohort = this.MakeNewCohort(GrazType.stLITT1);
                    }
                    else if (sStatus == PastureUtil.StatusName[GrazType.stLITT2])
                    {
                        iCohort = this.MakeNewCohort(GrazType.stLITT2);
                    }
                    else
                    {
                        iCohort = this.MakeNewCohort(GrazType.stDEAD);
                    }
                    this.FCohorts[iCohort].ReadFromValue(null, dry[idx], false);
                }
            }

            // Enforce a single cohort of dead and of litter
            for (idx = this.CohortCount() - 1; idx >= 0; idx--)
            {
                if (this.BelongsIn(idx, sgDRY))
                {
                    for (jdx = this.CohortCount() - 1; jdx >= idx + 1; jdx--)
                    {
                        if (this.FCohorts[jdx].Status == this.FCohorts[idx].Status)
                        {
                            this.MergeCohorts(idx, jdx);
                        }
                    }
                }
            }

            if (this.Params.bHasSeeds)                                                               // Seeds -------------------------------
            {
                if (seed == null)
                    throw new Exception(this.Params.sName + " needs to include a seed component.");

                this.SetSeedMass(GrazType.TOTAL, GrazType.TOTAL, GrazType.TOTAL, 0.0);

                if (seed.soft_unripe.Length > 0)
                {
                    for (iLayer = 0; iLayer < seed.soft_unripe.Length; iLayer++)
                    {
                        this.SetSeedMass(GrazType.SOFT, GrazType.UNRIPE, iLayer+1, PastureUtil.ReadMass(seed.soft_unripe[iLayer], "kg/ha"));
                    }
                }

                if (seed.soft_ripe.Length > 0)
                {
                    for (iLayer = 0; iLayer < seed.soft_ripe.Length; iLayer++)
                    {
                        this.SetSeedMass(GrazType.SOFT, GrazType.RIPE, iLayer+1, PastureUtil.ReadMass(seed.soft_ripe[iLayer], "kg/ha"));
                    }
                }

                if (seed.hard_unripe.Length > 0)
                {
                    for (iLayer = 0; iLayer < seed.hard_unripe.Length; iLayer++)
                    {
                        this.SetSeedMass(GrazType.HARD, GrazType.UNRIPE, iLayer+1, PastureUtil.ReadMass(seed.hard_unripe[iLayer], "kg/ha"));
                    }
                }

                if (seed.hard_ripe.Length > 0)
                {
                    for (iLayer = 0; iLayer < seed.hard_ripe.Length; iLayer++)
                    {
                        this.SetSeedMass(GrazType.HARD, GrazType.RIPE, iLayer+1, PastureUtil.ReadMass(seed.hard_ripe[iLayer], "kg/ha"));
                    }
                }

                this.Days_EDormant = Convert.ToInt32(seeddormtime);
                this.GermnIndex = germidx;
            }

            this.MassUnit = sPrevUnit;
        }

        /// <summary>
        /// *N.B.the order of search for a parameter set containing sSpeciesName is:
        /// 1.the TPastureParamSet given by mainParamSet
        /// 2.the pasture parameter file given by sParamFile
        /// 3.the default parameter set
        /// </summary>
        /// <param name="speciesName"></param>
        /// <param name="mainParamSet"></param>
        /// <param name="paramFile"></param>
        /// <exception cref="Exception"></exception>
        public void SetParameters(string speciesName, TPastureParamSet mainParamSet, string paramFile)
        {
            TParameterSet FullSet;
            TPastureParamSet SpeciesSet;
            int iCohort;

            try
            {
                if (mainParamSet != null)
                {
                    FullSet = mainParamSet;
                }
                else
                {
                    FullSet = new TPastureParamSet();
                    if (paramFile != "")
                    {
                        TGParamFactory.ParamXMLFactory().readFromFile(paramFile, FullSet, false);
                    }
                    else
                    {
                        TGParamFactory.ParamXMLFactory().readDefaults("PASTURE_PARAM_GLB", ref FullSet);
                    }

                    SpeciesSet = ((TPastureParamSet)FullSet).Match(speciesName);
                    if (SpeciesSet == null)
                    {
                        throw new Exception("Pasture species name \n(" + speciesName + ") not found");
                    }

                    TParameterSet empty = null;
                    this.FParams = new TPastureParamSet(empty, SpeciesSet);
                    for (iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
                    {
                        this.FCohorts[iCohort].SetParameters();
                    }

                    this.FParamFile = paramFile;
                }
            }
            catch (Exception e)
            {
                throw new Exception("Exception in setParamters(): " + e.Message);
            }
            finally
            {
                if (mainParamSet == null)
                {
                    FullSet = null;
                }
            }
        }

        /// <summary>
        /// Number of cohorts matching a particular status
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <returns></returns>
        public int CohortCount(int comp = GrazType.TOTAL)
        {
            int iCohort;
            int result;
            if (comp == GrazType.TOTAL)
            {
                result = this.FCohorts.Length;
            }
            else
            {
                result = 0;
                for (iCohort = 0; iCohort <= this.FCohorts.Length - 1; iCohort++)
                {
                    if (this.BelongsIn(iCohort, comp))
                    {
                        result++;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Get the cohort by index
        /// </summary>
        /// <param name="cohort">Cohort index</param>
        /// <returns>A pasture cohort</returns>
        public TPastureCohort Cohort(int cohort)
        {
            return this.FCohorts[cohort];
        }

        /// <summary>
        /// New dry matter pool
        /// </summary>
        /// <param name="part">Plant part - leaf, stem, root, seed or total</param>
        /// <param name="DM"></param>
        /// <returns></returns>
        public DM_Pool MakeNewPool(int part, double DM)
        {
            DM_Pool result = new DM_Pool();

            result.DM = DM;
            var values = Enum.GetValues(typeof(TPlantElement)).Cast<TPlantElement>();
            foreach (var Elem in values)
            {
                if (this.FElements.Contains(Elem))
                {
                    result.Nu[(int)Elem] = DM * this.Params.NutrConcK[1, (int)Elem, part];
                }
                else
                {
                    result.Nu[(int)Elem] = 0.0;
                }
            }

            if (this.FElements.Contains(TPlantElement.N))
            {
                result.AshAlk = DM * this.Params.AshAlkK[part];
            }
            else
            {
                result.AshAlk = 0.0;
            }

            return result;
        }

        /// <summary>
        /// Set the soil parameters, layer count
        /// </summary>
        /// <param name="soilLayers">The layer profile</param>
        /// <param name="bulkDensity">Bulk densities</param>
        /// <param name="sandContent">Sand contents</param>
        /// <param name="campbellParam"></param>
        public void SetSoilParams(double[] soilLayers, double[] bulkDensity, double[] sandContent, double[] campbellParam)
        {
            // count the soil layers that have depths
            int i = 0;
            while ((i < GrazType.MaxSoilLayers) && (i < soilLayers.Length - 1) && (soilLayers[i+1] > 0))
            {
                i++;
            }

            this.FSoilLayerCount = i;

            this.FRootRestriction = new double[this.FSoilLayerCount + 1];
            this.FSoilDepths = new double[this.FSoilLayerCount + 1];
            this.FSoilDepths[0] = 0.0;
            for (int iLayer = 1; iLayer <= this.FSoilLayerCount; iLayer++)
            {
                this.FSoilDepths[iLayer] = this.FSoilDepths[iLayer - 1] + soilLayers[iLayer];
            }

            this.FSoilLayers = soilLayers;
            this.FBulkDensity = bulkDensity;
            this.FSandContent = sandContent;
            this.FCampbellParam = campbellParam;

            this.ComputeRootingParams(this.RecomputeRoots);
        }

        /// <summary>
        /// Set the maximum root depth
        /// </summary>
        /// <param name="depthValue"></param>
        public void SetMaxRootDepth(double depthValue)
        {
            this.FMaxRootDepth = depthValue;
            this.SetRootDepth(GrazType.sgEST_SENC, ALL_COHORTS, depthValue);
        }

        /// <summary>
        /// Light interception for a monoculture
        /// </summary>
        public void SetMonocultureLight()
        {
            double[] fCompFPA = new double[GrazType.stSENC + 1]; // [stSEEDL..stSENC]
            double fHorzFPA;
            double fHorzLight;
            int iComp;

            for (iComp = GrazType.stSEEDL; iComp <= GrazType.stSENC; iComp++)
            {
                fCompFPA[iComp] = this.ProjArea(iComp);
            }

            fHorzFPA = fCompFPA[stESTAB] + fCompFPA[stSENC];
            fHorzLight = 1.0 - Math.Exp(-fHorzFPA);
            this.SetLightPropn(GrazType.stESTAB, fHorzLight * PastureUtil.Div0(fCompFPA[GrazType.stESTAB], fHorzFPA));
            this.SetLightPropn(GrazType.stSENC, fHorzLight * PastureUtil.Div0(fCompFPA[GrazType.stSENC], fHorzFPA));

            fHorzFPA = fCompFPA[GrazType.stSEEDL];
            fHorzLight = (1.0 - fHorzLight) * (1.0 - Math.Exp(-fHorzFPA));
            this.SetLightPropn(GrazType.stSEEDL, fHorzLight);
        }

        /// <summary>
        /// Set to a monoculture soil
        /// </summary>
        public void SetMonocultureSoil()
        {
            double[] fVolume = new double[this.FSoilLayerCount + 1];
            int iLayer;

            for (iLayer = 1; iLayer <= this.FSoilLayerCount; iLayer++)
            {
                fVolume[iLayer] = 1.0;
            }

            this.SetSoilPropn(GrazType.TOTAL, fVolume);
        }

        /// <summary>
        /// Set the light proportion for this herbage component.
        /// </summary>
        /// <param name="comp">Herbage component. seedling...senescent</param>
        /// <param name="value"></param>
        public void SetLightPropn(int comp, double value)
        {
            if ((comp >= stSEEDL) && (comp <= stSENC))
            {
                this.FLightFract[comp] = value;
            }
            else
            {
                throw new Exception("Cannot assign light proportion for pasture component = " + comp.ToString());
            }
        }

        /// <summary>
        /// Set the transpiration rate for this herbage component.
        /// </summary>
        /// <param name="comp">Herbage component. seedling...senescent</param>
        /// <param name="values"></param>
        public void SetTranspiration(int comp, double[] values)
        {
            if ((comp >= stSEEDL) && (comp <= stSENC))
            {
                Array.Copy(values, 0, this.FTranspireRate[comp], 0, values.Length);
            }
        }

        /// <summary>
        /// Assign soil proportion for the component of this pasture
        /// </summary>
        /// <param name="comp">Herbage component. seedling...senescent, total</param>
        /// <param name="values"></param>
        public void SetSoilPropn(int comp, double[] values)
        {
            double[][] fRLD = new double[GrazType.stSENC + 1][];     // [stSEEDL..stSENC] of LayerArray
            double fRLDTotal;
            int iLayer;
            int Idx;

            if ((comp >= stSEEDL) && (comp <= GrazType.stSENC))
            {
                this.FSoilFract[comp] = values;
            }
            else if (comp == GrazType.TOTAL)
            {
                for (Idx = GrazType.stSEEDL; Idx <= GrazType.stSENC; Idx++)
                {
                    fRLD[Idx] = this.EffRootLengthD(Idx);
                }

                for (iLayer = 1; iLayer <= this.FSoilLayerCount; iLayer++)
                {
                    fRLDTotal = fRLD[GrazType.stSEEDL][iLayer] + fRLD[GrazType.stESTAB][iLayer] + fRLD[GrazType.stSENC][iLayer];
                    for (Idx = GrazType.stSEEDL; Idx <= GrazType.stSENC; Idx++)
                    {
                        this.FSoilFract[Idx][iLayer] = values[iLayer] * PastureUtil.Div0(fRLD[Idx][iLayer], fRLDTotal);
                    }
                }
            }
            else
            {
                throw new Exception("Cannot assign soil proportion for pasture component = " + comp.ToString());
            }
        }

        /// <summary>
        /// Get the light fraction for this herbage component.
        /// </summary>
        /// <param name="comp">Herbage component. seedling...senescent</param>
        /// <returns></returns>
        public double LightPropn(int comp)
        {
            if ((comp >= stSEEDL) && (comp <= stSENC))
            {
                return this.FLightFract[comp];
            }
            else if (comp == sgEST_SENC)
            {
                return this.FLightFract[stESTAB] + this.FLightFract[stSENC];
            }
            else
            {
                return 0.0;
            }
        }

        /// <summary>
        /// Cover function
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <returns></returns>
        public double Cover(int comp)
        {
            return 1.0 - Math.Exp(-this.ProjArea(comp));
        }

        /// <summary>
        /// Area index of a component or components
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="part">Plant part - leaf, stem, root, seed or total</param>
        /// <returns></returns>
        public double AreaIndex(int comp, int part = TOTAL)
        {
            int iCohort;

            double result = 0.0;
            for (iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
            {
                if (this.BelongsIn(iCohort, comp))
                {
                    result += this.FCohorts[iCohort].AreaIndex(part);
                }
            }

            return result;
        }

        /// <summary>
        /// "Ground cover" function - takes extinction coefficient into account
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <returns></returns>
        public double ProjArea(int comp)
        {
            double result;
            if (comp >= stSEEDL && comp <= stLITT2)
            {
                result = this.FExtinctionK[comp] * this.AreaIndex(comp);
            }
            else
            {
                switch (comp)
                {
                    case sgEST_SENC:
                        result = this.ProjArea(stESTAB) + this.ProjArea(stSENC);
                        break;
                    case sgGREEN:
                        result = this.ProjArea(stSEEDL) + this.ProjArea(stESTAB) + this.ProjArea(stSENC);
                        break;
                    case sgDRY:
                        result = this.ProjArea(stDEAD) + this.ProjArea(stLITT1) + this.ProjArea(stLITT2);
                        break;
                    case sgAV_DRY:
                        result = this.ProjArea(stDEAD) + this.ProjArea(stLITT1);
                        break;
                    case sgSTANDING:
                        result = this.ProjArea(stSEEDL) + this.ProjArea(stESTAB) + this.ProjArea(stSENC) + this.ProjArea(stDEAD);
                        break;
                    case sgLITTER:
                        result = this.ProjArea(stLITT1) + this.ProjArea(stLITT2);
                        break;
                    case TOTAL:
                        result = this.ProjArea(sgGREEN) + this.ProjArea(sgDRY);
                        break;
                    default:
                        result = 0.0;
                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// Compute water demand
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="pastureWaterDemand">Sward water demand</param>
        /// <returns></returns>
        public double WaterDemand(int comp, double pastureWaterDemand)
        {
            // Should be the myWaterDemand x proportion for this component
            //return this.LightPropn(comp) * (this.Inputs.PotentialET - this.Inputs.SurfaceEvap) * this.CO2_WaterDemand(comp);
            double totalLightFraction = 0;
            for (int iComp = stSEEDL; iComp <= stSENC; iComp++)
                totalLightFraction += this.LightPropn(iComp);

            return MathUtilities.Divide(this.LightPropn(comp),totalLightFraction,0.0) * pastureWaterDemand;
        }

        /// <summary>
        /// Compute max water uptake
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="pastureWaterDemand">Pasture water demand</param>
        /// <returns></returns>
        public double[] WaterMaxSupply(int comp, double pastureWaterDemand)
        {
            double[] result = new double[this.FSoilLayerCount + 1];
            double fRootDepth_;
            double fExtractDepth;
            int iLayer;

            if (this.fWater_KL[1] > 0.0)
            {
                // Monteith uptake model
                fRootDepth_ = this.GetRootDepth(comp, ALL_COHORTS);

                for (iLayer = 1; iLayer <= this.FSoilLayerCount; iLayer++)
                {
                    if (fRootDepth_ >= this.FSoilDepths[iLayer])
                    {
                        fExtractDepth = this.FSoilLayers[iLayer];
                    }
                    else
                    {
                        fExtractDepth = Math.Max(0.0, fRootDepth_ - this.FSoilDepths[iLayer - 1]);
                    }

                    result[iLayer] = fExtractDepth * this.fWater_KL[iLayer]
                                      * Math.Max(0.0, this.FInputs.Theta[iLayer] - this.fPlant_LL[iLayer]);
                }
            }
            else
            {
                // ASW-based uptake model
                this.ComputeWaterUptake_ASW(comp, this.WaterDemand(comp, pastureWaterDemand), ref result);
            }

            return result;
        }

        /// <summary>
        /// Get the transpiration of this herbage component.
        /// </summary>
        /// <param name="comp">Herbage component. seedling...senescent</param>
        /// <returns></returns>
        public double[] Transpiration(int comp)
        {
            int iLayer;
            double[] result;

            if ((comp >= stSEEDL) && (comp <= stSENC))
            {
                result = (double[]) this.FTranspireRate[comp].Clone();
            }
            else
            {
                result = new double[this.FSoilLayerCount + 1];
                for (iLayer = 1; iLayer <= this.FSoilLayerCount; iLayer++)
                {
                    if (comp == sgEST_SENC)
                    {
                        result[iLayer] = this.FTranspireRate[stESTAB][iLayer]
                                    + this.FTranspireRate[stSENC][iLayer];
                    }
                    else if ((comp == sgGREEN) || (comp == TOTAL))
                    {
                        result[iLayer] = this.FTranspireRate[stSEEDL][iLayer]
                                        + this.FTranspireRate[stESTAB][iLayer]
                                        + this.FTranspireRate[stSENC][iLayer];
                    }
                }
            }

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="comp">Herbage component. seedling...senescent</param>
        /// <returns></returns>
        public double[] RootRadii(int comp)
        {
            int iLayer;

            double[] result = new double[this.FSoilLayerCount + 1];
            for (iLayer = 1; iLayer <= this.FSoilLayerCount; iLayer++)
            {
                if (this.RootMassGM2(comp, EFFR, iLayer) > 0.0)
                {
                    result[iLayer] = this.Params.RootK[10];
                }
                else
                {
                    result[iLayer] = 0.0;
                }
            }

            return result;
        }

        /// <summary>
        /// Effective root length density in m/m^3
        /// </summary>
        /// <param name="comp">Herbage component. seedling...senescent</param>
        /// <returns></returns>
        public double[] EffRootLengthD(int comp)
        {
            int iLayer;
            double[] result = new double[this.FSoilLayerCount + 1];

            for (iLayer = 1; iLayer <= this.FSoilLayerCount; iLayer++)
            {
                result[iLayer] = PastureUtil.Div0(this.Params.RootK[9] * this.RootMassGM2(comp, EFFR, iLayer), 0.001 * this.FSoilLayers[iLayer]);
            }

            return result;
        }

        /// <summary>
        /// Compute cation uptake
        /// </summary>
        /// <returns>Value for each layer 1-n</returns>
        public double[] CationUptake()
        {
            double[] result = new double[this.FSoilLayerCount + 1];
            double[] fCohortUptake;
            int iCohort;
            int iLayer;

            for (iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
            {
                if (this.BelongsIn(iCohort, sgGREEN))
                {
                    fCohortUptake = this.FCohorts[iCohort].CationUptake();
                    for (iLayer = 1; iLayer <= this.FSoilLayerCount; iLayer++)
                    {
                        result[iLayer] = result[iLayer] + fCohortUptake[iLayer];
                    }
                }
            }

            for (iLayer = 1; iLayer <= this.FSoilLayerCount; iLayer++)
            {
                result[iLayer] = result[iLayer] * this.FMassScalar;
            }

            return result;
        }

        /// <summary>
        /// Get the height profile
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="part">Plant part - leaf, stem, root, seed or total</param>
        /// <param name="levelCount"></param>
        /// <param name="heights_MM"></param>
        /// <param name="massPropn"></param>
        public void GetHeightProfile(int comp, int part,
                                      ref int levelCount,
                                      ref double[] heights_MM,
                                      ref double[] massPropn)
        {
            int VARYINGLEVELS = 5;

            double fSwardHeight;
            double[] fRelHeights = new double[this.FSoilLayerCount + 1];    // The SoilArray type has a zero layer...
            double[] fMassPropnAbove = new double[this.FSoilLayerCount + 1];
            double fLeafPropn = 0;
            double fIntcpt = 0;
            double fSlope = 0;
            double fHeight0 = 0;
            double fHeight1 = 0;
            double fLevelDelta;
            double fDummy = 0;
            int iLevel;

            fSwardHeight = this.Height_MM();

            if ((part == ptLEAF) || (part == ptSTEM) || (part == ptSEED))
            {
                this.GetRelHeightThresholds(comp, ref fLeafPropn, ref fHeight0, ref fHeight1, ref fIntcpt, ref fSlope);

                if ((fSlope == 0.0) || (fSwardHeight == 0.0))
                {
                    // Uniform herbage profile
                    levelCount = 1;
                    fRelHeights[1] = 1.0;
                }
                else
                {
                    // Profile with higher proportions of leaf near the top
                    levelCount = 0;
                    fRelHeights[0] = 0.0;

                    // Stem-only portion at the base?
                    if (fHeight0 > 0.0)
                    {
                        levelCount++;
                        fRelHeights[levelCount] = Math.Min(fHeight0, 1.0);
                    }

                    fLevelDelta = (Math.Min(fHeight1, 1.0) - Math.Max(fHeight0, 0.0)) / VARYINGLEVELS;
                    for (iLevel = 1; iLevel <= VARYINGLEVELS; iLevel++)
                    {
                        levelCount++;
                        fRelHeights[levelCount] = fRelHeights[levelCount - 1] + fLevelDelta;
                    }

                    // Leaf-only portion at the top?
                    if (fHeight1 < 1.0)
                    {
                        levelCount++;
                        fRelHeights[levelCount] = 1.0;
                    }
                }

                // Cumulative distribution of mass over the height profile
                fMassPropnAbove[0] = 1.0;
                for (iLevel = 1; iLevel <= levelCount - 1; iLevel++)
                {
                    if (part == ptLEAF)
                    {
                        this.GetLeafStemPropnAbove(fRelHeights[iLevel], fLeafPropn, fHeight0, fHeight1, fIntcpt, fSlope, ref fMassPropnAbove[iLevel], ref fDummy);
                    }
                    else if (part == ptSTEM)
                    {
                        this.GetLeafStemPropnAbove(fRelHeights[iLevel], fLeafPropn, fHeight0, fHeight1, fIntcpt, fSlope, ref fDummy, ref fMassPropnAbove[iLevel]);
                    }
                    else // seed
                    {
                        fMassPropnAbove[iLevel] = 1.0;
                    }
                }

                fMassPropnAbove[levelCount] = 0.0;

                // Convert cumulative profile to within-layer profile
                for (iLevel = 1; iLevel <= levelCount; iLevel++)
                {
                    heights_MM[iLevel] = fSwardHeight * fRelHeights[iLevel];
                    massPropn[iLevel] = fMassPropnAbove[iLevel - 1] - fMassPropnAbove[iLevel];
                }
            }
            else
            {
                levelCount = 0;
            }
        }

        /// <summary>
        /// Construct a record of information required by the animal intake model
        /// * Herbage and seed mass values are given in the nominated unit
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        public GrazingInputs GetGrazingInputs(string unit)
        {
            int[] iDMDMap = { 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6 };

            string sOldUnit;
            double fGreenDM, fDryDM;

            double fDM, fDMD, fCP, fDG, fPh, fSu, fAA;
            int iDMD, iClss, iRipe;

            GrazingInputs result = new GrazingInputs();
            sOldUnit = this.MassUnit;
            this.MassUnit = unit;
            zeroGrazingInputs(ref result);

            for (iDMD = 1; iDMD <= HerbClassNo; iDMD++)
            {
                fGreenDM = this.AvailHerbage(sgGREEN, TOTAL, iDMD);
                fDryDM = this.AvailHerbage(sgAV_DRY, TOTAL, iDMD);
                fDM = fGreenDM + fDryDM;

                if (fDM > VERYSMALL)
                {
                    fDMD = PastureUtil.HerbageDMD[iDMD];
                    fCP = WeightAverage(this.CrudeProtein(sgGREEN, TOTAL, iDMD), fGreenDM,
                                         this.CrudeProtein(sgAV_DRY, TOTAL, iDMD), fDryDM);
                    fDG = WeightAverage(this.DegradableProt(sgGREEN, TOTAL, iDMD),
                                               fGreenDM * this.CrudeProtein(sgGREEN, TOTAL, iDMD),
                                               this.DegradableProt(sgAV_DRY, TOTAL, iDMD),
                                               fDryDM * this.CrudeProtein(sgAV_DRY, TOTAL, iDMD));
                    fPh = WeightAverage(this.GetHerbageConc(sgGREEN, TOTAL, iDMD, TPlantElement.P), fGreenDM,
                                               this.GetHerbageConc(sgAV_DRY, TOTAL, iDMD, TPlantElement.P), fDryDM);
                    fSu = WeightAverage(this.GetHerbageConc(sgGREEN, TOTAL, iDMD, TPlantElement.S), fGreenDM,
                                               this.GetHerbageConc(sgAV_DRY, TOTAL, iDMD, TPlantElement.S), fDryDM);
                    fAA = WeightAverage(this.HerbageAshAlkalinity(sgGREEN, TOTAL, iDMD), fGreenDM,
                                               this.HerbageAshAlkalinity(sgAV_DRY, TOTAL, iDMD), fDryDM);
                    iClss = iDMDMap[iDMD];
                    result.Herbage[iClss].HeightRatio = WeightAverage(result.Herbage[iClss].HeightRatio, result.Herbage[iClss].Biomass, this.Params.HeightRatio, fDM);
                    result.Herbage[iClss].Degradability = WeightAverage(result.Herbage[iClss].Degradability, result.Herbage[iClss].Biomass * result.Herbage[iClss].CrudeProtein, fDG, fDM * fCP);
                    result.Herbage[iClss].Digestibility = WeightAverage(result.Herbage[iClss].Digestibility, result.Herbage[iClss].Biomass, fDMD, fDM);
                    result.Herbage[iClss].CrudeProtein = WeightAverage(result.Herbage[iClss].CrudeProtein, result.Herbage[iClss].Biomass, fCP, fDM);
                    result.Herbage[iClss].PhosContent = WeightAverage(result.Herbage[iClss].PhosContent, result.Herbage[iClss].Biomass, fPh, fDM);
                    result.Herbage[iClss].SulfContent = WeightAverage(result.Herbage[iClss].SulfContent, result.Herbage[iClss].Biomass, fSu, fDM);
                    result.Herbage[iClss].AshAlkalinity = WeightAverage(result.Herbage[iClss].AshAlkalinity, result.Herbage[iClss].Biomass, fAA, fDM);
                    result.Herbage[iClss].Biomass = result.Herbage[iClss].Biomass + fDM;

                    result.TotalGreen += fGreenDM;                       // Total available green and dry DM
                    result.TotalDead += fDryDM;
                }
            } // _ FOR iDMD = 1 TO HerbClassNo

            // Seed info
            for (iRipe = UNRIPE; iRipe <= RIPE; iRipe++)
            {
                if (this.Params.bHasSeeds && (this.Params.Seed_Class[iRipe] != 0))
                {
                    result.SeedClass[1, iRipe] = this.Params.Seed_Class[iRipe];

                    result.Seeds[1, iRipe].Biomass = this.GetSeedMass(TOTAL, iRipe, 1);
                    result.Seeds[1, iRipe].Digestibility = this.SeedDigestibility(iRipe);
                    result.Seeds[1, iRipe].CrudeProtein = this.SeedCrudeProtein(TOTAL, iRipe, 1);
                    result.Seeds[1, iRipe].Degradability = this.SeedDegradableProt(iRipe);
                    result.Seeds[1, iRipe].PhosContent = this.GetSeedConc(TOTAL, iRipe, 1, TPlantElement.P);
                    result.Seeds[1, iRipe].SulfContent = this.GetSeedConc(TOTAL, iRipe, 1, TPlantElement.S);
                    result.Seeds[1, iRipe].AshAlkalinity = this.SeedAshAlkalinity(TOTAL, iRipe, 1);
                    result.Seeds[1, iRipe].HeightRatio = 1.0;
                }
            }

            result.LegumePropn = Convert.ToDouble(this.Params.bLegume);  // true == 1
            result.SelectFactor = this.Params.SelectFactor;

            this.MassUnit = sOldUnit;

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="part">Plant part - leaf, stem, root, seed or total</param>
        /// <param name="layer"></param>
        /// <returns></returns>
        public DM_Pool GetResidueFlux(int part, int layer = 1)
        {
            DM_Pool result = new DM_Pool();

            if (part == ptROOT)
            {
                result = this.FRootResidue[layer];
            }
            else if (((part == ptLEAF) || (part == ptSTEM)) && ((layer == 0) || (layer == 1)))
            {
                result = this.FTopResidue[part];
            }
            else
            {
                PastureUtil.ZeroPool(ref result);
            }

            if (this.FMassScalar != 1.0)
            {
                result = GrazType.MultiplyDMPool(result, this.FMassScalar);
            }

            return result;
        }

        /// <summary>
        /// Get the nutrient uptake in each layer for each area
        /// </summary>
        /// <param name="nutr">Plant nutrient</param>
        /// <returns>Nutrient uptake in [area][layer]</returns>
        public double[][] GetNutrUptake(TPlantNutrient nutr)
        {
            double[][] result = new double[MAXNUTRAREAS][]; // array[0..MAXNUTRAREAS - 1] of LayerArray -> TSoilUptakeDistn
            double[][] cohortUptake;
            int iCohort, iArea, iLayer;

            for (int i = 0; i <= MAXNUTRAREAS - 1; i++)
            {
                result[i] = new double[this.FSoilLayerCount + 1];
            }

            for (iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
            {
                if (this.BelongsIn(iCohort, sgGREEN))
                {
                    cohortUptake = this.FCohorts[iCohort].NutrUptake(nutr);
                    for (iArea = 0; iArea <= this.FInputs.Nutrients[(int)nutr].NoAreas - 1; iArea++)
                    {
                        for (iLayer = 1; iLayer <= this.FSoilLayerCount; iLayer++)
                        {
                            result[iArea][iLayer] = result[iArea][iLayer] + cohortUptake[iArea][iLayer];
                        }
                    }
                }
            }

            if (this.FMassScalar != 1.0)
            {
                for (iArea = 0; iArea <= this.FInputs.Nutrients[(int)nutr].NoAreas - 1; iArea++)
                {
                    for (iLayer = 1; iLayer <= this.FSoilLayerCount; iLayer++)
                    {
                        result[iArea][iLayer] = this.FMassScalar * result[iArea][iLayer];
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Get the leachate amount
        /// </summary>
        /// <returns>The leachate value</returns>
        public DM_Pool GetLeachate()
        {
            DM_Pool pool = this.FLeachate;
            if (this.FMassScalar != 1.0)
            {
                pool = GrazType.MultiplyDMPool(pool, this.FMassScalar);
            }

            return pool;
        }

        /// <summary>
        /// Get the gaseous losses for this element
        /// </summary>
        /// <param name="Elem"></param>
        /// <returns></returns>
        public double GetGaseousLoss(TPlantElement Elem)
        {
            int iCohort;

            double result = 0.0;
            for (iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
            {
                result += this.FCohorts[iCohort].GaseousLoss(Elem);
            }

            result = this.FMassScalar * result;

            return result;
        }

        // Model logic .............................................................}

        /// <summary>
        /// Compute a default value for the MaxRootDepth property
        /// </summary>
        /// <returns>Max root depth</returns>
        public double DefaultMaxRootDepth()
        {
            double fRootsLeft;
            double fMaxDepth;
            int iLayer;
            double result;

            fRootsLeft = this.Params.RootK[1];
            iLayer = 1;
            fMaxDepth = 0.0;
            while ((iLayer <= this.FSoilLayerCount) && (fRootsLeft > 0.0))
            {
                if (this.FRootRestriction[iLayer] * fRootsLeft <= this.FSoilLayers[iLayer])
                {
                    fMaxDepth = this.FSoilDepths[iLayer - 1] + this.FRootRestriction[iLayer] * fRootsLeft;
                    fRootsLeft = 0.0;
                }
                else
                {
                    fRootsLeft -= this.FSoilLayers[iLayer] / this.FRootRestriction[iLayer];
                }

                iLayer++;
            }

            if (fRootsLeft > 0.0)
            {
                result = this.FSoilDepths[this.FSoilLayerCount];
            }
            else
            {
                result = 10.0 * Math.Round(fMaxDepth / 10.0);                   // Round off to nearest 10 mm
            }

            return result;
        }

        /// <summary>
        /// Zeros fields that denote the magnitudes of various DM and element flows
        /// </summary>
        private void ZeroFlows()
        {
            for (int i = 0; i <= GrazType.stSENC; i++)
            {
                PastureUtil.FillArray(this.FTranspireRate[i], 0.0);
            }

            PastureUtil.FillArray(this.FHerbageGrazed, 0);
            PastureUtil.FillArray(this.FSeedGrazed, 0.0);
            PastureUtil.FillArray(this.FShootsCut, 0.0);
            PastureUtil.FillArray(this.FShootsGrazed, 0.0);
            PastureUtil.FillArray(this.FShootsKilled, 0.0);

            PastureUtil.FillArray(this.FShootLossDenom, 0.0);
            this.FRootLossDenom = 0.0;
            PastureUtil.FillArray(this.FShootFluxLoss, 0.0);
            PastureUtil.FillArray(this.FShootRespireLoss, 0.0);
            this.FRootDeathLoss = 0.0;
            this.FRootRespireLoss = 0.0;
            PastureUtil.FillArray(this.FDigDecline, 0.0);
            for (int i = 0; i < ptSTEM; i++)
            {
                ZeroDMPool(ref this.FTopResidue[i]);
            }

            for (int i = 0; i < this.FRootResidue.Length; i++)
            {
                ZeroDMPool(ref this.FRootResidue[i]);
            }

            // FLeachate = new DM_Pool();
            ZeroDMPool(ref this.FLeachate);
        }

        /// <summary>
        /// Start of the timestep
        /// </summary>
        public void BeginTimeStep()
        {
            this.ZeroFlows();

            for (int Event = (int)TDevelopEvent.startCycle; Event <= (int)TDevelopEvent.endDormantW - 1; Event++)
            {
                this.FPhenologyEvent[Event] = false;
            }
        }

        /// <summary>
        /// Water uptake logic (for use in monoculture only)
        /// </summary>
        /// <param name="pastureWaterDemand"></param>
        public void ComputeWaterUptake(double pastureWaterDemand)
        {
            double fDemand;
            double[] fUptake = new double[this.FSoilLayerCount + 1];
            int iComp;

            for (iComp = stSEEDL; iComp <= stSENC; iComp++)
            {
                fDemand = this.WaterDemand(iComp, pastureWaterDemand);
                if (fDemand <= 0.0)
                {
                    fUptake = new double[this.FSoilLayerCount + 1];
                }
                else if (this.fWater_KL[1] == 0)
                {
                    this.ComputeWaterUptake_ASW(iComp, fDemand, ref fUptake);
                }
                else
                {
                    this.ComputeWaterUptake_KL(iComp, fDemand, ref fUptake);
                }

                this.SetTranspiration(iComp, fUptake);
            }
        }

        /// <summary>
        /// Essentially a control routine. The vast majority of equations appear in
        /// the methods which it calls.
        /// </summary>
        /// <param name="fSupply">Nutrient supply in g/m^2</param>
        /// <param name="pastureWaterDemand"></param>
        public void ComputeRates(double[][][] fSupply, double pastureWaterDemand)
        {
            double fMoistureChange;
            double fDormTempFract;
            bool bDormant;
            int iCohort;

            bDormant = (this.Phenology == TDevelopType.Dormant || this.Phenology == TDevelopType.DormantW);

            if (this.LaggedMeanT >= -99.9)
            {
                this.LaggedMeanT = 0.1 * this.Inputs.MeanDayTemp + 0.9 * this.LaggedMeanT;
                if (this.Params.DevelopK[30] < 1.0)
                {
                    fDormTempFract = 1.0;
                }
                else
                {
                    fDormTempFract = 1.0 / this.Params.DevelopK[30];
                }

                this.WDormMeanTemp = fDormTempFract * this.Inputs.MeanTemp + (1.0 - fDormTempFract) * this.WDormMeanTemp;
            }
            else
            {
                this.LaggedMeanT = this.Inputs.MeanTemp;
                this.WDormMeanTemp = this.Inputs.MeanTemp;
            }

            if (DormMeanTemp < -99.9)
            {
                DormMeanTemp = Inputs.MeanTemp;
            }

            // Need extension rates in computing allocation within root pools
            for (iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
            {
                if (this.BelongsIn(iCohort, sgGREEN))
                {
                    this.FCohorts[iCohort].ComputeRootExtension(this.Inputs.MeanTemp, this.Inputs.ASW);
                    this.FCohorts[iCohort].ComputeNewSpecificArea(TOTAL, this.Inputs.MeanTemp, this.Inputs.Radiation);
                }
            }

            for (iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
            {
                if (this.BelongsIn(iCohort, sgGREEN))
                {
                    this.FCohorts[iCohort].ComputeRespiration(this.Inputs.MeanTemp, bDormant);
                    this.FCohorts[iCohort].ComputeAllocation();
                }
            }

            this.ComputePotAssimilation(pastureWaterDemand);

            for (iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
            {
                if (this.BelongsIn(iCohort, sgGREEN))
                {
                    this.FCohorts[iCohort].ComputePotTranslocation(this.Inputs.MeanTemp);            // Need target R:S ratio to compute this
                    this.FCohorts[iCohort].ComputePotNetGrowth();
                }
            }

            fMoistureChange = this.Inputs.RainIntercept * this.Inputs.Precipitation - this.Inputs.SurfaceEvap;
            for (iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
            {
                if (this.BelongsIn(iCohort, stDEAD))
                {
                    this.FCohorts[iCohort].DeadMoistureBalance(fMoistureChange);
                }

                this.FCohorts[iCohort].ComputeFlowRates(this.Inputs.MinTemp, this.Inputs.MeanTemp,
                                                    this.LaggedMeanT, this.Inputs.Precipitation,
                                                    this.Inputs.TrampleRate, this.Inputs.ASW[1]);
                this.FCohorts[iCohort].ComputeFrostHardening(this.Inputs.MinTemp);
            }

            var values = Enum.GetValues(typeof(TPlantElement)).Cast<TPlantElement>().ToArray();

            foreach (var Elem in values)
            {
                if (this.FElements.Contains(Elem))
                {
                    this.ComputeNutrientRates(Elem, fSupply);
                }
            }

            for (iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
            {
                if (this.BelongsIn(iCohort, sgGREEN))
                {
                    this.FCohorts[iCohort].ComputeNetGrowth(bDormant);

                    foreach (var Elem in values)
                    {
                        if (this.FElements.Contains(Elem))
                        {
                            this.FCohorts[iCohort].RescaleNutrientRates(Elem);
                        }
                    }
                }
            }

            this.ComputePhenology();

            if (this.Params.bHasSeeds)
            {
                if ((this.FloweringLength >= 0.0) && (this.FloweringTime == 0.0))
                {
                    for (iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
                    {
                        if (this.BelongsIn(iCohort, sgGREEN))
                        {
                            this.FCohorts[iCohort].SetStemReserve();
                        }
                    }
                }

                this.ComputeSeedFlows();
            }
        }

        /// <summary>
        /// Stores removal amounts for later processing
        /// * This is complicated by the fact that the pasture model has two DMD classes
        ///   for every class in the DigClassArray.
        /// </summary>
        /// <param name="class_GM2"></param>
        /// <param name="seed_GM2"></param>
        public void PassRemoval(double[] class_GM2, double[] seed_GM2)
        {
            double fDM0;
            double fDM1;
            int iClass;
            int iDMD;
            int iRipe;

            for (iClass = 1; iClass <= DigClassNo; iClass++)
            {
                iDMD = 2 * iClass - 1;
                fDM0 = this.AvailHerbageGM2(TOTAL, TOTAL, iDMD);
                fDM1 = this.AvailHerbageGM2(TOTAL, TOTAL, iDMD + 1);
                this.FHerbageGrazed[iDMD] = this.FHerbageGrazed[iDMD] + class_GM2[iClass] * PastureUtil.Div0(fDM0, fDM0 + fDM1);
                this.FHerbageGrazed[iDMD + 1] = this.FHerbageGrazed[iDMD + 1] + class_GM2[iClass] * PastureUtil.Div0(fDM1, fDM0 + fDM1);
            }

            for (iRipe = UNRIPE; iRipe <= RIPE; iRipe++)
            {
                this.FSeedGrazed[iRipe] = this.FSeedGrazed[iRipe] + seed_GM2[iRipe];
            }
        }

        /// <summary>
        ///
        /// Update the value of most of the state variables.
        /// * Exceptions are:  FDays_EDormant   updated in computeSeedFlows()
        ///                    GermnIndex       ditto)
        /// </summary>
        public void UpdateState()
        {
            DM_Pool NewSeeds = new DM_Pool();
            DM_Pool GerminSeeds = new DM_Pool();
            int iComp;
            int iCohort;

            this.ComputeRemoval();                                                               // Distribute removal rates across cohorts & components

            this.ComputePhenologySetback();                                                      // Must be done after removal rates are distributed across components
            this.ComputeExtinction();
            for (iComp = stSEEDL; iComp <= stSENC; iComp++)
            {
                this.SetExtinctionCoeff(iComp, this.FExtinctionK[iComp] + this.FExtinctChange[iComp]);
            }

            this.StoreFlowDenominators();

            ZeroPool(ref NewSeeds);

            // Mass balance equation for each cohort
            // Work from litter back to seedlings so that death & fall can be added directly to the destination pools
            for (iCohort = this.CohortCount() - 1; iCohort >= 0; iCohort--)
            {
                this.FCohorts[iCohort].UpdateState(ref NewSeeds);
            }

            this.StoreFlowRates();

            // Mass balance equation for each seed pool
            if (this.Params.bHasSeeds)
            {
                this.UpdateSeeds(NewSeeds, ref GerminSeeds);
                if (GerminSeeds.DM > 0.0)
                {
                    this.AddSeedlings(GerminSeeds);
                }
            }

            for (iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
            {
                var values = Enum.GetValues(typeof(TPlantElement)).Cast<TPlantElement>().ToArray();
                foreach (var Elem in values)
                {
                    if (this.FElements.Contains(Elem))
                    {
                        this.FCohorts[iCohort].LoseExcessNutrients(Elem);
                        this.FCohorts[iCohort].TransferSenescedNutrients(Elem);
                    }
                }
            }

            // Change in status of seedling cohorts (senescence of established cohorts carried out in the Senesce method)
            for (iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
            {
                if (this.FCohorts[iCohort].EstablishesToday())
                {
                    this.FCohorts[iCohort].Status = stESTAB;
                }
            }

            this.ComputeTotals();
            this.ClearEmptyCohorts(sgGREEN);          // Remove any empty cohorts of green herbage

            // This cohort-merging rule gives a single cohort of live & senescing plants
            for (iCohort = this.CohortCount() - 1; iCohort >= 1; iCohort--)
            {
                if ((this.FCohorts[iCohort - 1].Status == this.FCohorts[iCohort].Status)
                     && this.BelongsIn(iCohort, sgEST_SENC))
                {
                    this.MergeCohorts(iCohort - 1, iCohort);
                }
            }
        }

        // Management events .......................................................

        /// <summary>
        /// Sow seeds into a sward.  In perennial plants, they  are assumed to emerge
        /// instantly as there is no seed pool.  In annual plants, seeds are assumed
        /// to be ready to germinate
        /// </summary>
        /// <param name="seed_GM2"></param>
        public void Sow(double seed_GM2)
        {
            DM_Pool SownPool;

            if (seed_GM2 > 0.0)
            {
                SownPool = this.MakeNewPool(ptSEED, seed_GM2);  // creates a new DM_Pool
                if (this.Params.bHasSeeds)
                {
                    this.AddPool(SownPool, ref this.FSeeds[SOFT, RIPE, 1]);
                }
                else
                {
                    this.AddSeedlings(SownPool);
                }

                this.ComputeTotals();
            }
        }

        /// <summary>
        /// Spray-top a sward.  Spray-topping only affects grasses
        /// </summary>
        public void SprayTop()
        {
            if (this.Params.bGrass && (this.Phenology == TDevelopType.Vernalizing || this.Phenology == TDevelopType.Vegetative || this.Phenology == TDevelopType.Reproductive))
            {
                if (this.Phenology != TDevelopType.Reproductive)
                {
                    this.DegDays = 0.0;
                }

                this.Phenology = TDevelopType.SprayTopped;
            }
        }

        /// <summary>
        /// Kill a proportion of live biomass, e.g. by herbicide application
        /// </summary>
        /// <param name="propnHerbage"></param>
        /// <param name="propnSeeds"></param>
        public void Kill(double propnHerbage, double propnSeeds)
        {
            double fCohortStem;
            int iDead;
            int iCohort;
            int iPart, iDMD;
            int iAge, iLayer;
            int iSoft, iRipe;

            iDead = this.FindCohort(stDEAD);
            if (iDead < 0)
            {
                iDead = this.MakeNewCohort(stDEAD);
            }

            for (iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
            {
                if (this.BelongsIn(iCohort, sgGREEN))
                {
                    fCohortStem = this.FCohorts[iCohort].Herbage[ptSTEM, TOTAL].DM;
                    this.FCohorts[iCohort].RemoveStemReserve(propnHerbage * fCohortStem);
                    for (iPart = ptLEAF; iPart <= ptSTEM; iPart++)
                    {
                        for (iDMD = 1; iDMD <= HerbClassNo; iDMD++)
                        {
                            this.FShootsKilled[iPart, iDMD] = this.FShootsKilled[iPart, iDMD] + propnHerbage * this.FCohorts[iCohort].Herbage[iPart, iDMD].DM;
                            this.MovePoolPropn(propnHerbage,
                                          ref this.FCohorts[iCohort].Herbage[iPart, iDMD],
                                          ref this.FCohorts[iDead].Herbage[iPart, iDMD]);
                        }
                    }

                    for (iAge = EFFR; iAge <= OLDR; iAge++)
                    {
                        for (iLayer = 1; iLayer <= this.FSoilLayerCount; iLayer++)
                        {
                            this.MoveToResidue(propnHerbage * this.FCohorts[iCohort].Roots[iAge, iLayer].DM,
                                               ref this.FCohorts[iCohort].Roots[iAge, iLayer],
                                               this.FCohorts[iCohort].Status, ptROOT, TOTAL, iLayer);
                        }
                    }
                }
            }

            // Only seed in the "first soil layer" is killed
            for (iSoft = SOFT; iSoft <= HARD; iSoft++)
            {
                for (iRipe = UNRIPE; iRipe <= RIPE; iRipe++)
                {
                    this.MoveToResidue(propnSeeds * this.FSeeds[iSoft, iRipe, 1].DM,
                                       ref this.FSeeds[iSoft, iRipe, 1],
                                       stESTAB, ptSEED, TOTAL, 1);
                }
            }

            this.ComputeTotals();                           // Re-compute marginal totals
        }

        /// <summary>
        /// Proportion of a layer in the cultivated zone
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="ploughDepth"></param>
        /// <returns></returns>
        private double PloughPropn(int layer, double ploughDepth)
        {
            if (layer <= this.FSoilLayerCount)
            {
                return PastureUtil.RAMP(ploughDepth, this.FSoilDepths[layer - 1], this.FSoilDepths[layer]);
            }
            else
            {
                return 0.0;
            }
        }

        /// <summary>
        /// Mix the appropriate fraction of Input into the Output pool
        /// </summary>
        /// <param name="input">Input.DM is not returned altered!</param>
        /// <param name="layer"></param>
        /// <param name="output"></param>
        /// <param name="ploughDepth"></param>
        private void MixPool(DM_Pool input, int layer, ref DM_Pool output, double ploughDepth)
        {
            double ploughMM;

            if (layer <= this.FSoilLayerCount)
            {
                ploughMM = Math.Min(this.FSoilDepths[layer], ploughDepth) - Math.Min(this.FSoilDepths[layer - 1], ploughDepth);
            }
            else
            {
                ploughMM = 0.0;
            }

            this.MovePoolPropn(ploughMM / ploughDepth, ref input, ref output);
        }

        /// <summary>
        /// Cultivation method.  Cultivation kills all live and senescing biomass and
        /// incorporates a given fraction of the dead and litter biomass (including the
        /// newly killed material) into the soil, uniformly to a given depth.
        /// Seed populations are assumed to be mixed to the cultivation depth.
        /// </summary>
        /// <param name="propnIncorp"></param>
        /// <param name="ploughDepth"></param>
        public void Cultivate(double propnIncorp, double ploughDepth)
        {
            DM_Pool PloughPool = new DM_Pool();
            int cohort;
            int part, iDMD,
            layer,
            ripe, soft;

            this.Kill(1.0, 0.0);

            PastureUtil.ZeroPool(ref PloughPool);

            // Move dead & litter material to PloughPool, awaiting incorporation
            for (cohort = 0; cohort <= this.CohortCount() - 1; cohort++)
            {
                for (part = ptLEAF; part <= ptSTEM; part++)
                {
                    for (iDMD = 1; iDMD <= HerbClassNo; iDMD++)
                    {
                        if (this.BelongsIn(cohort, sgGREEN))
                        {
                            this.FShootsKilled[part, iDMD] = this.FShootsKilled[part, iDMD] + propnIncorp * this.FCohorts[cohort].Herbage[part, iDMD].DM;
                        }

                        this.MovePoolPropn(propnIncorp, ref this.FCohorts[cohort].Herbage[part, iDMD], ref PloughPool);
                    }
                }
            }

            // Now move killed root material within the plough layer into PloughPool
            for (layer = 1; layer <= this.FSoilLayerCount; layer++)
            {
                this.MovePoolPropn(this.PloughPropn(layer, ploughDepth), ref this.FRootResidue[layer], ref PloughPool);
            }

            // Incorporate into the soil & mix
            for (layer = 1; layer <= this.FSoilLayerCount; layer++)
            {
                this.MixPool(PloughPool, layer, ref this.FRootResidue[layer], ploughDepth);
            }

            if (this.Params.bHasSeeds)
            {
                for (soft = SOFT; soft <= HARD; soft++)
                {
                    for (ripe = UNRIPE; ripe <= RIPE; ripe++)
                    {
                        PastureUtil.ZeroPool(ref PloughPool);

                        // Gather the seeds to be mixed
                        for (layer = 1; layer <= this.FSoilLayerCount; layer++)
                        {
                            this.MovePoolPropn(this.PloughPropn(layer, ploughDepth), ref this.FSeeds[soft, ripe, layer], ref PloughPool);
                        }

                        // Mix the seeds through the soil
                        for (layer = 1; layer <= this.FSoilLayerCount; layer++)
                        {
                            this.MixPool(PloughPool, layer, ref this.FSeeds[soft, ripe, layer], ploughDepth);
                        }
                    }
                }

                // Keep track of the deepest layer that may contain seeds
                for (layer = 1; layer <= this.FSoilLayerCount; layer++)
                {
                    if (this.PloughPropn(layer, ploughDepth) > 0.0)
                    {
                        this.FSeedLayers = Math.Max(this.FSeedLayers, layer);
                    }
                }
            }

            this.ComputeTotals();                                           // Re-compute marginal totals
        }

        /// <summary>
        /// Remove part of the unripe, soft seed pool
        /// </summary>
        /// <param name="soft"></param>
        /// <param name="ripe"></param>
        /// <param name="layer"></param>
        /// <param name="cutPropn"></param>
        /// <returns></returns>
        private DM_Pool CutSeeds(int soft, int ripe, int layer, double cutPropn)
        {
            DM_Pool result = new DM_Pool();

            ZeroPool(ref result);
            this.MovePoolPropn(cutPropn, ref this.FSeeds[soft, ripe, layer], ref result);

            var values = Enum.GetValues(typeof(TPlantElement)).Cast<TPlantElement>().ToArray();
            foreach (var Elem in values)
            {
                if (!this.FElements.Contains(Elem))
                {
                    result.Nu[(int)Elem] = result.DM * this.GetSeedConc(soft, ripe, layer, Elem);
                }
            }

            if (!this.FElements.Contains(TPlantElement.N))
            {
                result.AshAlk = result.DM * this.SeedAshAlkalinity(soft, ripe, layer);
            }

            return result;
        }

        /// <summary>
        /// Combine removed forage into a TSupplement
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="DMD"></param>
        /// <param name="DG"></param>
        /// <param name="DMDLoss"></param>
        /// <param name="DMContent"></param>
        /// <param name="hayFW"></param>
        /// <param name="poolHay"></param>
        /// <param name="composition"></param>
        private void Pool2Hay(DM_Pool pool, double DMD, double DG, double DMDLoss, double DMContent,
                              ref double hayFW, ref FoodSupplement poolHay, ref FoodSupplement composition)
        {
            double poolFW;

            if (pool.DM > 0.0)
            {
                poolFW = pool.DM * (1.0 - DMD) / (1.0 - DMD + DMDLoss);         // Respiration of digestible DM
                poolFW /= DMContent;                                            // Convert to a fresh weight basis

                poolHay.IsRoughage = true;
                poolHay.DMPropn = DMContent;
                poolHay.dmDigestibility = Math.Max(0.0, DMD - DMDLoss);
                poolHay.ME2DM = FoodSupplement.ConvertDMDToME2DM(poolHay.dmDigestibility, true, 0.0);
                poolHay.CrudeProt = N2Protein * pool.Nu[(int)TPlantElement.N] / pool.DM;
                poolHay.degProt = DG;
                poolHay.Phosphorus = pool.Nu[(int)TPlantElement.P] / pool.DM;
                poolHay.Sulphur = pool.Nu[(int)TPlantElement.S] / pool.DM;
                poolHay.AshAlkalinity = pool.AshAlk / pool.DM;
                poolHay.MaxPassage = 0.0;

                composition.Mix(composition, poolHay, hayFW / (hayFW + poolFW));
                hayFW += poolFW;
            }
        }

        /// <summary>
        /// Cuts herbage to specified residual levels
        /// </summary>
        /// <param name="standingLeft">Standing aftermath (kg/ha, total basis)</param>
        /// <param name="litterLeft">Litter aftermath (kg/ha, total basis)</param>
        /// <param name="propnGathered">Proportion of cut material conserved</param>
        /// <param name="DMDLoss">DMD loss on cutting and storage</param>
        /// <param name="DMContent">DM content of conserved forage</param>
        /// <param name="hayFW">Fresh weight of conserved forage (kg/ha)</param>
        /// <param name="composition">Composition of conserved forage</param>
        public void Conserve(double standingLeft,
                             double litterLeft,
                             double propnGathered,
                             double DMDLoss,
                             double DMContent,
                             ref double hayFW,
                             ref FoodSupplement composition)
        {
            double totalStanding,
              totalLitter,
              standingPropn,
              litterPropn;
            double[] partPropn = new double[ptSTEM + 1]; // [ptLEAF..ptSTEM]
            double cohortStemDM;
            double propn;
            DM_Pool cutPool = new DM_Pool();

            int litter;
            int cohort;
            int comp;
            int part, iDMD, soft;

            FoodSupplement PoolHay = new FoodSupplement();
            hayFW = 0.0;

            totalLitter = this.HerbageMassGM2(stLITT1, TOTAL, TOTAL);                   // Compute the proportions of standing and litter forage that are to be cut
            totalStanding = this.HerbageMassGM2(sgSTANDING, TOTAL, TOTAL);
            standingPropn = PastureUtil.Div0(Math.Max(0.0, totalStanding - standingLeft), totalStanding);
            litterPropn = PastureUtil.Div0(Math.Max(0.0, totalLitter - litterLeft), totalLitter);
            this.GetPropnAboveHeight(sgSTANDING, 1.0 - standingPropn, ref partPropn[ptLEAF], ref partPropn[ptSTEM]);

            litter = this.FindCohort(stLITT1);
            if (litter < 0)
            {
                litter = this.MakeNewCohort(stLITT1);
            }

            // This relies on the litter cohort being last in FCohorts[]
            for (cohort = this.CohortCount() - 1; cohort >= 0; cohort--)
            {
                comp = this.FCohorts[cohort].Status;

                if (this.BelongsIn(cohort, sgGREEN))
                {
                    cohortStemDM = this.FCohorts[cohort].Herbage[ptSTEM, TOTAL].DM;
                    this.FCohorts[cohort].RemoveStemReserve(partPropn[ptSTEM] * cohortStemDM);
                }

                for (part = ptLEAF; part <= ptSTEM; part++)
                {
                    if (comp == stLITT2)
                    {
                        // Comminuted litter is not cut
                        propn = 0.0;
                    }
                    else if (comp == stLITT1)
                    {
                        propn = litterPropn;
                    }
                    else
                    {
                        propn = partPropn[part];
                    }

                    for (iDMD = 1; iDMD <= HerbClassNo; iDMD++)
                    {
                        cutPool = this.FCohorts[cohort].CutHerbage(part, iDMD, propn);            // Transfer the cut herbage to CutPool
                        this.FShootsCut[comp, part, iDMD] = this.FShootsCut[comp, part, iDMD] + cutPool.DM;

                        // Harvesting is not completely efficient harvest losses enter the litter pool
                        this.MovePoolPropn(Math.Max(0.0, 1.0 - propnGathered),
                                           ref cutPool,
                                           ref this.FCohorts[litter].Herbage[part, iDMD]);

                        // Remainder goes into conserved forage
                        this.Pool2Hay(cutPool,
                                      HerbageDMD[iDMD],
                                      this.DegradableProt(this.FCohorts[cohort].Status, part, iDMD),
                                      DMDLoss, DMContent, ref hayFW,
                                      ref PoolHay,
                                      ref composition);
                    }
                }
            }

            if (this.Params.bHasSeeds)
            {
                // Assumes that ripe seed escapes & that unripe seed has the same vertical distribution as standing herbage
                for (soft = SOFT; soft <= HARD; soft++)
                {
                    cutPool = this.CutSeeds(soft, UNRIPE, 1, standingPropn * propnGathered);

                    // Harvest % of seeds in line with herbage
                    // Harvest of seed is taken to be 100% efficient for now
                    this.Pool2Hay(cutPool,
                                  this.SeedDigestibility(UNRIPE),
                                  this.SeedDegradableProt(UNRIPE),
                                  DMDLoss, DMContent, ref hayFW,
                                  ref PoolHay,
                                  ref composition);
                }
            }

            this.ComputeTotals();                                       // Re-compute marginal totals
            PoolHay = null;

            composition.Name = "hay";

            // Compute marginal totals in FShootsCut
            for (comp = stSEEDL; comp <= stLITT1; comp++)
            {
                for (part = ptLEAF; part <= ptSTEM; part++)
                {
                    this.FShootsCut[comp, part, TOTAL] = 0.0;
                    for (iDMD = 1; iDMD <= HerbClassNo; iDMD++)
                    {
                        this.FShootsCut[comp, part, TOTAL] = this.FShootsCut[comp, part, TOTAL] + this.FShootsCut[comp, part, iDMD];
                    }
                }

                for (iDMD = TOTAL; iDMD <= HerbClassNo; iDMD++)
                {
                    this.FShootsCut[comp, TOTAL, iDMD] = 0.0;
                    for (part = ptLEAF; part <= ptSTEM; part++)
                    {
                        this.FShootsCut[comp, TOTAL, iDMD] = this.FShootsCut[comp, TOTAL, iDMD] + this.FShootsCut[comp, part, iDMD];
                    }
                }
            }

            for (part = TOTAL; part <= ptSTEM; part++)
            {
                for (iDMD = TOTAL; iDMD <= HerbClassNo; iDMD++)
                {
                    this.FShootsCut[TOTAL, part, iDMD] = 0.0;
                    for (comp = stSEEDL; comp <= stLITT1; comp++)
                    {
                        this.FShootsCut[TOTAL, part, iDMD] = this.FShootsCut[TOTAL, part, iDMD] + this.FShootsCut[comp, part, iDMD];
                    }
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="standing"></param>
        /// <param name="part">Plant part - leaf, stem, root, seed or total</param>
        /// <param name="herbage"></param>
        /// <param name="meanDMD"></param>
        public void AddDryPasture(bool standing, int part, DM_Pool herbage, double meanDMD = 0.0)
        {
            int[] DRY_COMP = { stLITT1, stDEAD };  // [boolean]

            int cohort;
            int highDMD;
            int lowDMD;
            double crudeProt;
            double[] defPropns = new double[HerbClassNo + 1]; // HerbageArray;
            double fract;
            int iDMD;

            if (herbage.DM > 1.0E-9)
            {
                cohort = this.FindCohort(DRY_COMP[Convert.ToInt32(standing)]);
                if (cohort < 0)
                {
                    cohort = this.MakeNewCohort(DRY_COMP[Convert.ToInt32(standing)]);
                }

                highDMD = this.FCohorts[cohort].FHighestDMDClass[part];
                lowDMD = this.FCohorts[cohort].FLowestDMDClass[part];

                if (meanDMD == 0.0)
                {
                    crudeProt = N2Protein * herbage.Nu[(int)TPlantElement.N] / herbage.DM;
                    if (crudeProt <= this.Params.Protein[lowDMD])
                    {
                        this.AddPool(herbage, ref this.FCohorts[cohort].Herbage[part, lowDMD]);
                    }
                    else if (crudeProt >= this.Params.Protein[highDMD])
                    {
                        this.AddPool(herbage, ref this.FCohorts[cohort].Herbage[part, highDMD]);
                    }
                    else
                    {
                        iDMD = lowDMD;
                        while (crudeProt > this.Params.Protein[iDMD - 1])
                        {
                            iDMD--;
                        }

                        fract = (crudeProt - this.Params.Protein[iDMD]) / (this.Params.Protein[iDMD - 1] - this.Params.Protein[iDMD]);
                        this.AddPool(PoolFraction(herbage, fract), ref this.FCohorts[cohort].Herbage[part, iDMD - 1]);
                        this.AddPool(PoolFraction(herbage, 1.0 - fract), ref this.FCohorts[cohort].Herbage[part, iDMD]);
                    }
                }
                else // fMeanDMD provided
                {
                    if (meanDMD <= HerbageDMD[lowDMD])
                    {
                        this.AddPool(herbage, ref this.FCohorts[cohort].Herbage[part, lowDMD]);
                    }
                    else if (meanDMD >= HerbageDMD[highDMD])
                    {
                        this.AddPool(herbage, ref this.FCohorts[cohort].Herbage[part, highDMD]);
                    }
                    else
                    {
                        defPropns = this.DefaultDigClassPropns(false, TDevelopType.Senescent, part, meanDMD);
                        for (iDMD = highDMD; iDMD <= lowDMD; iDMD++)     /////////////check this iteration
                        {
                            this.AddPool(PoolFraction(herbage, defPropns[iDMD]),
                                     ref this.FCohorts[cohort].Herbage[part, iDMD + 1]);
                        }
                    }
                }

                this.ComputeTotals();                                   // Re-compute marginal totals
            }
        }

        /// <summary>
        /// Returns the default distribution across DMD classes of a herbage component
        /// of given average DMD
        /// * Implemented as a procedure, not a method, so that it can be used in dialogs
        /// </summary>
        /// <param name="green"></param>
        /// <param name="phenoStage"></param>
        /// <param name="part">Plant part - leaf, stem, root, seed or total</param>
        /// <param name="DMDValue"></param>
        /// <returns></returns>
        private double[] DefaultDigClassPropns(bool green, TDevelopType phenoStage, int part, double DMDValue)
        {
            double maxDMD,
            minDMD,
            relDMD;
            double threshold;
            double[] relLimits = new double[HerbClassNo + 1];
            double[] cumValues = new double[HerbClassNo + 1];
            double _A, _B;
            double delta;
            double testDMD;
            int iDMD;
            double[] result = new double[HerbClassNo + 1];

            maxDMD = this.Params.MatureK[1, part];
            if (!green)
            {
                minDMD = HerbageDMD[12];
            }
            else if (!(phenoStage == TDevelopType.Reproductive || phenoStage == TDevelopType.SprayTopped || phenoStage == TDevelopType.Senescent || phenoStage == TDevelopType.Dormant))
            {
                minDMD = this.Params.MatureK[2, part];
            }
            else
            {
                minDMD = this.Params.MatureK[3, part];
            }

            minDMD = HerbageDMD[DMDToClass(minDMD, true)];
            relDMD = (DMDValue - minDMD) / (maxDMD - minDMD);
            threshold = (HerbageDMD[DMDToClass(maxDMD, false)] - minDMD) / (maxDMD - minDMD) + 1.0E-7;
            threshold = Math.Min(threshold, 1.0);

            if ((relDMD > 0.0) && (relDMD < threshold))
            {
                for (iDMD = 0; iDMD <= HerbClassNo; iDMD++)
                {
                    relLimits[iDMD] = Math.Max(0.0, Math.Min((DMDLimits[iDMD] - minDMD) / (maxDMD - minDMD), 1.0));
                }

                _A = 60.0 * relDMD * (StdMath.Sqr(relDMD) - relDMD + 1 / 3);
                _B = (1.0 - relDMD) / relDMD * _A;
                delta = 0.1;
                bool complete; // = false;
                do
                {
                    for (iDMD = 0; iDMD <= HerbClassNo; iDMD++)
                    {
                        cumValues[iDMD] = StdMath.CumBeta(relLimits[iDMD], _A, _B);
                    }

                    for (iDMD = 1; iDMD <= HerbClassNo; iDMD++)
                    {
                        result[iDMD] = cumValues[iDMD - 1] - cumValues[iDMD];
                    }

                    testDMD = 0.0;
                    for (iDMD = 1; iDMD <= HerbClassNo; iDMD++)
                    {
                        testDMD += HerbageDMD[iDMD] * result[iDMD];
                    }

                    if (((testDMD < DMDValue) && (delta > 0.0))
                       || ((testDMD > DMDValue) && (delta < 0.0)))
                    {
                        delta = -0.5 * delta;
                    }

                    _B *= (1.0 + delta);
                    complete = (Math.Abs(testDMD - DMDValue) < 1.0E-7) || (Math.Abs(delta) < 1.0E-6);
                } while (!complete);
            }
            else if (relDMD >= threshold)
            {
                PastureUtil.FillArray(result, 0.0);

                iDMD = DMDToClass(maxDMD, false);
                result[iDMD] = Math.Max(0.0, Math.Min(1.0 - (HerbageDMD[iDMD] - DMDValue) / CLASSWIDTH, 1.0));
                result[iDMD + 1] = 1.0 - result[iDMD];
            }
            else
            {
                PastureUtil.FillArray(result, 0.0);

                iDMD = DMDToClass(minDMD, true);
                result[iDMD] = Math.Max(0.0, Math.Min(1.0 - (DMDValue - HerbageDMD[iDMD]) / CLASSWIDTH, 1.0));
                result[iDMD - 1] = 1.0 - result[iDMD];
            }

            return result;
        }

        // Initialisation properties ...............................................}

        /// <summary>
        ///
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="value"></param>
        public void SetExtinctionCoeff(int comp, double value)
        {
            this.FExtinctionK[comp] = Math.Max(this.Params.LightK[7], Math.Min(value, this.Params.LightK[8]));
        }

        // Reporting variables .....................................................}

        /// <summary>
        /// Herbage available for grazing, in user units
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="part">Plant part - leaf, stem, root, seed or total</param>
        /// <param name="DMD"></param>
        /// <returns></returns>
        public double AvailHerbage(int comp, int part, int DMD)
        {
            return this.FMassScalar * this.AvailHerbageGM2(comp, part, DMD);
        }

        /// <summary>
        /// Average digestibility of shoot
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="part">Plant part - leaf, stem, root, seed or total</param>
        /// <param name="DMD"></param>
        /// <returns></returns>
        public double Digestibility(int comp, int part, int DMD = TOTAL)
        {
            double poolWt;
            double denom;
            double result;

            if (DMD != TOTAL)
            {
                result = PastureUtil.HerbageDMD[DMD];
            }
            else
            {
                result = 0.0;
                denom = 0.0;
                for (DMD = 1; DMD <= HerbClassNo; DMD++)
                {
                    poolWt = this.HerbageMassGM2(comp, part, DMD);
                    result += poolWt * PastureUtil.HerbageDMD[DMD];
                    denom += poolWt;
                }

                result = PastureUtil.Div0(result, denom);
            }

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="part">Plant part - leaf, stem, root, seed or total</param>
        /// <param name="DMD"></param>
        /// <returns></returns>
        public double CrudeProtein(int comp, int part, int DMD = 0)
        {
            double poolWt;
            double denom;
            double result;

            if (this.FElements.Contains(TPlantElement.N))
            {
                result = N2Protein * this.GetHerbageConc(comp, part, DMD, TPlantElement.N);
            }
            else if ((part != TOTAL) && (DMD != TOTAL))
            {
                result = this.Params.Protein[DMD] * this.CO2_CrudeProtein(part);
            }
            else if ((part == TOTAL) && (DMD != TOTAL))
            {
                result = 0.0;
                denom = 0.0;
                for (part = ptLEAF; part <= ptSTEM; part++)
                {
                    poolWt = this.HerbageMassGM2(comp, part, DMD);
                    result += poolWt * this.CrudeProtein(comp, part, DMD);
                    denom += poolWt;
                }

                result = PastureUtil.Div0(result, denom);
            }
            else
            {
                result = 0.0;
                denom = 0.0;
                for (DMD = 1; DMD <= HerbClassNo; DMD++)
                {
                    poolWt = this.HerbageMassGM2(comp, part, DMD);
                    result += poolWt * this.CrudeProtein(comp, part, DMD);
                    denom += poolWt;
                }

                result = PastureUtil.Div0(result, denom);
            }

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="part">Plant part - leaf, stem, root, seed or total</param>
        /// <param name="DMD"></param>
        /// <returns></returns>
        public double DegradableProt(int comp, int part, int DMD)
        {
            double poolWt;
            double denom;
            double result;

            if (DMD != TOTAL)
            {
                result = this.Params.DgProtein[DMD];
            }
            else
            {
                result = 0.0;
                denom = 0.0;
                for (DMD = 1; DMD <= HerbClassNo; DMD++)
                {
                    poolWt = this.HerbageMassGM2(comp, part, DMD) * this.CrudeProtein(comp, part, DMD);
                    result += poolWt * this.Params.DgProtein[DMD];
                    denom += poolWt;
                }

                result = PastureUtil.Div0(result, denom);
            }

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="part">Plant part - leaf, stem, root, seed or total</param>
        /// <param name="DMD"></param>
        /// <returns></returns>
        public double HerbageAshAlkalinity(int comp, int part, int DMD)
        {
            double poolWt;
            double denom;
            int cohort;
            double result;

            if (this.FElements.Length == 0)
            {
                if (part == TOTAL)
                {
                    result = WeightAverage(this.Params.AshAlkK[ptLEAF], this.HerbageMassGM2(comp, ptLEAF, DMD),
                                            this.Params.AshAlkK[ptSTEM], this.HerbageMassGM2(comp, ptSTEM, DMD));
                }
                else
                {
                    result = this.Params.AshAlkK[part];
                }
            }
            else
            {
                result = 0.0;
                denom = 0.0;
                for (cohort = 0; cohort <= this.CohortCount() - 1; cohort++)
                {
                    if (this.BelongsIn(cohort, comp))
                    {
                        poolWt = this.FCohorts[cohort].Herbage[part, DMD].DM;
                        result += poolWt * this.FCohorts[cohort].Herbage[part, DMD].AshAlk;
                        denom += poolWt;
                    }
                }

                result = PastureUtil.Div0(result, denom);
            }

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="age"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        public double RootAshAlkalinity(int comp, int age, int layer)
        {
            double poolWt;
            double denom;
            int cohort;
            double result;

            if (this.FElements.Length == 0)
            {
                result = this.Params.AshAlkK[ptROOT];
            }
            else
            {
                result = 0.0;
                denom = 0.0;
                for (cohort = 0; cohort <= this.CohortCount() - 1; cohort++)
                {
                    if (this.BelongsIn(cohort, comp))
                    {
                        poolWt = this.FCohorts[cohort].Roots[age, layer].DM;
                        result += poolWt * this.FCohorts[cohort].Roots[age, layer].AshAlk;
                        denom += poolWt;
                    }
                }

                result = PastureUtil.Div0(result, denom);
            }

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="ripeness"></param>
        /// <returns></returns>
        public double SeedDigestibility(int ripeness)
        {
            return this.Params.Seed_Dig[ripeness];
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="soft"></param>
        /// <param name="ripe"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        public double SeedCrudeProtein(int soft, int ripe, int layer)
        {
            if (this.FElements.Contains(TPlantElement.N))
            {
                return N2Protein * this.GetSeedConc(soft, ripe, layer, TPlantElement.N);
            }
            else
            {
                return this.Params.Seed_Prot;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="ripeness"></param>
        /// <returns></returns>
        public double SeedDegradableProt(int ripeness)
        {
            return Math.Min(1.0, 0.1 + this.Params.Seed_Dig[ripeness]);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="soft"></param>
        /// <param name="ripe"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        public double SeedAshAlkalinity(int soft, int ripe, int layer)
        {
            if (this.FElements.Length == 0)
            {
                return this.Params.AshAlkK[ptSEED];
            }
            else
            {
                return PastureUtil.Div0(this.FSeeds[soft, ripe, layer].AshAlk, this.FSeeds[soft, ripe, layer].DM);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public double Height_MM()
        {
            double result = 100.0 * GrazType.DM2Height * this.Params.HeightRatio * this.HerbageMassGM2(TOTAL, TOTAL, TOTAL);
            if ((this.FPastureCoverSum > 0.0) && (this.Cover(TOTAL) > 0.0))
            {
                result *= (this.FPastureCoverSum / this.Cover(TOTAL));
            }

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <returns></returns>
        public double ExtinctionCoeff(int comp)
        {
            return this.FExtinctionK[comp];
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="part">Plant part - leaf, stem, root, seed or total</param>
        /// <returns></returns>
        public double NetPP(int comp = sgGREEN, int part = TOTAL)
        {
            return this.Assimilation(comp, part) - this.Respiration(comp, part);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="part">Plant part - leaf, stem, root, seed or total</param>
        /// <returns></returns>
        public double Assimilation(int comp = sgGREEN, int part = TOTAL)
        {
            double result = 0.0;
            for (int iCohort = 0; iCohort <= this.CohortCount() - 1; iCohort++)
            {
                if (this.BelongsIn(iCohort, comp))
                {
                    if (part == TOTAL)
                    {
                        result += this.FCohorts[iCohort].Assimilation;
                    }
                    else
                    {
                        result += this.FCohorts[iCohort].Assimilation * this.FCohorts[iCohort].Allocation[part];
                    }
                }
            }

            result = this.FMassScalar * result;

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="part">Plant part - leaf, stem, root, seed or total</param>
        /// <returns></returns>
        public double Respiration(int comp = sgGREEN, int part = TOTAL)
        {
            int cohort;
            double result = 0.0;

            for (cohort = 0; cohort <= this.CohortCount() - 1; cohort++)
            {
                if (this.BelongsIn(cohort, comp))
                {
                    result += this.FCohorts[cohort].fMaintRespiration[part]
                             + this.FCohorts[cohort].fGrowthRespiration[part];
                }
            }

            result = this.FMassScalar * result;

            return result;
        }

        /// <summary>
        /// Net growth including translocation
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="part">Plant part - leaf, stem, root, seed or total</param>
        /// <returns>Net growth</returns>
        private double NetGrowth(int comp = sgGREEN, int part = TOTAL)
        {
            int cohort, idx;

            double result = 0.0;
            for (cohort = 0; cohort <= this.CohortCount() - 1; cohort++)
            {
                if (this.BelongsIn(cohort, comp))
                {
                    if (part != TOTAL)
                    {
                        result += this.FCohorts[cohort].FPartNetGrowth[part];
                    }
                    else
                    {
                        for (idx = ptLEAF; idx <= ptSEED; idx++)
                        {
                            result += this.FCohorts[cohort].FPartNetGrowth[idx];
                        }
                    }
                }
            }

            result = this.FMassScalar * result;

            return result;
        }

        /// <summary>
        /// Total removal of herbage by cutting and grazing, in user units
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="part">Plant part - leaf, stem, root, seed or total</param>
        /// <param name="DMD"></param>
        /// <returns></returns>
        public double Removal(int comp,
                              int part = TOTAL,
                              int DMD = TOTAL)
        {
            return this.FMassScalar * this.RemovalGM2(comp, part, DMD);
        }

        /// <summary>
        /// Amount of shoot killed in Kill or Cultivate events
        /// mass / unit area units
        /// </summary>
        /// <param name="part">Plant part - leaf, stem, root, seed or total</param>
        /// <param name="DMD"></param>
        /// <returns></returns>
        public double ShootKilled(int part = TOTAL, int DMD = TOTAL)
        {
            return this.FShootsKilled[part, DMD] * this.FMassScalar;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public double TopGrowth()
        {
            int cohort;
            double result = 0.0;

            for (cohort = 0; cohort <= this.CohortCount() - 1; cohort++)
            {
                if (this.BelongsIn(cohort, sgGREEN))
                {
                    result += this.FCohorts[cohort].FPartNetGrowth[ptLEAF]
                                 + this.FCohorts[cohort].FPartNetGrowth[ptSTEM]
                                 + this.FCohorts[cohort].FPartNetGrowth[ptSEED]
                                 - this.FCohorts[cohort].StemTranslocSum;
                }
            }

            result = this.FMassScalar * Math.Max(0.0, result);

            return result;
        }

        /// <summary>
        /// Get the growth limiting factor value
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="factor">The limiting factor type</param>
        /// <returns>Value from 0-1</returns>
        public double GrowthLimit(int comp, PastureUtil.TGrowthLimit factor)
        {
            double denom;
            int cohort;
            int member;

            double result = 0.0;
            denom = 0.0;
            member = -1;
            for (cohort = 0; cohort <= this.CohortCount() - 1; cohort++)
            {
                if (this.BelongsIn(cohort, comp) && this.BelongsIn(cohort, sgGREEN))
                {
                    // Only green herbage has growth limits
                    if (factor == PastureUtil.TGrowthLimit.glGAI)
                    {
                        result += this.FCohorts[cohort].LimitFactors[(int)factor];
                    }
                    else
                    {
                        result += this.FCohorts[cohort].Herbage[TOTAL, TOTAL].DM * this.FCohorts[cohort].LimitFactors[(int)factor];
                        denom += this.FCohorts[cohort].Herbage[TOTAL, TOTAL].DM;
                    }

                    if (member == -1)
                    {
                        member = cohort;
                    }
                }
            }

            if (factor != PastureUtil.TGrowthLimit.glGAI)
            {
                if (denom > 0.0)
                {
                    result /= denom;
                }
                else if (member >= 0)
                {
                    result = this.FCohorts[member].LimitFactors[(int)factor];
                }
                else
                {
                    result = 0.0;
                }
            }

            return result;
        }

        /// <summary>
        /// Weighted average of the assimilate allocations of the nominated cohorts
        /// * It is possible for there to be an allocation pattern even when total
        /// assimilation is zero
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="part">Plant part - leaf, stem, root, seed or total</param>
        /// <returns></returns>
        public double Allocation(int comp, int part)
        {
            double denom;
            double alloc;
            int cohort;

            double result = 0.0;
            denom = 0.0;
            alloc = -1.0;
            for (cohort = 0; cohort <= this.CohortCount() - 1; cohort++)
            {
                if (this.BelongsIn(cohort, comp) && this.BelongsIn(cohort, sgGREEN))
                {
                    result += this.FCohorts[cohort].Assimilation * this.FCohorts[cohort].Allocation[part];
                    denom += this.FCohorts[cohort].Assimilation;
                    if (alloc < 0.0)
                    {
                        // Store a value in case of zero assimilation
                        alloc = this.FCohorts[cohort].Allocation[part];
                    }
                }
            }

            if (denom > 0.0)
            {
                result = PastureUtil.Div0(result, denom);
            }
            else if (alloc >= 0.0)
            {
                result = alloc;
            }

            return result;
        }

        /// <summary>
        /// Death of green, fall of dead, comminution or loss of litter in
        /// mass/unit area units
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <param name="part">Plant part - leaf, stem, root, seed or total</param>
        /// <param name="DMD"></param>
        /// <returns></returns>
        public double BiomassExit(int comp,
                                  int part = TOTAL,
                                  int DMD = TOTAL)
        {
            int cohort;
            int sumPart;
            int sumDMD;

            double result = 0.0;
            for (cohort = 0; cohort <= this.CohortCount() - 1; cohort++)
            {
                if (this.BelongsIn(cohort, comp))
                {
                    if ((part != TOTAL) && (DMD != TOTAL))
                    {
                        result += this.FCohorts[cohort].FBiomassExitGM2[part, DMD];
                    }
                    else if (DMD != TOTAL)
                    {
                        for (sumPart = ptLEAF; sumPart <= ptSTEM; sumPart++)
                        {
                            result += this.FCohorts[cohort].FBiomassExitGM2[sumPart, DMD];
                        }
                    }
                    else if (part != TOTAL)
                    {
                        for (sumDMD = 1; sumDMD <= HerbClassNo; sumDMD++)
                        {
                            result += this.FCohorts[cohort].FBiomassExitGM2[part, sumDMD];
                        }
                    }
                    else
                    {
                        for (sumPart = ptLEAF; sumPart <= ptSTEM; sumPart++)
                        {
                            for (sumDMD = 1; sumDMD <= HerbClassNo; sumDMD++)
                            {
                                result += this.FCohorts[cohort].FBiomassExitGM2[sumPart, sumDMD];
                            }
                        }
                    }
                }
            }

            result *= this.FMassScalar;

            return result;
        }

        /// <summary>
        /// Get the N fixation over all cohorts
        /// </summary>
        /// <param name="comp">Herbage component</param>
        /// <returns></returns>
        public double N_Fixation(int comp = sgGREEN)
        {
            int cohort;

            double result = 0.0;
            for (cohort = 0; cohort <= this.CohortCount() - 1; cohort++)
            {
                if (this.BelongsIn(cohort, comp))
                {
                    result += this.FCohorts[cohort].FNutrientInfo[(int)TPlantElement.N].fFixed;
                }
            }

            result *= this.FMassScalar;

            return result;
        }

        /// <summary>
        /// Weighted average of the establishment indices of the various seedling
        /// cohorts.
        /// </summary>
        /// <param name="cohIdx"></param>
        /// <returns></returns>
        public double EstablishIndex(int cohIdx = ALL_COHORTS)
        {
            double denom;
            int cohort;
            double result;

            if (cohIdx != ALL_COHORTS)
            {
                cohort = this.FindCohort(stSEEDL, cohIdx);
                result = this.FCohorts[cohort].EstabIndex;
            }
            else
            {
                result = 0.0;
                denom = 0.0;
                for (cohort = 0; cohort <= this.CohortCount() - 1; cohort++)
                {
                    if (this.BelongsIn(cohort, stSEEDL))
                    {
                        result += this.FCohorts[cohort].Herbage[TOTAL, TOTAL].DM * this.FCohorts[cohort].EstabIndex;
                        denom += this.FCohorts[cohort].Herbage[TOTAL, TOTAL].DM;
                    }
                }

                result = PastureUtil.Div0(result, denom);
            }

            return result;
        }

        /// <summary>
        /// Weighted average of the seedling stress indices of the various seedling
        /// cohorts.
        /// </summary>
        /// <param name="cohIdx"></param>
        /// <returns></returns>
        public double SeedlingStress(int cohIdx = ALL_COHORTS)
        {
            double denom;
            int cohort;
            double result;

            if (cohIdx != ALL_COHORTS)
            {
                cohort = this.FindCohort(stSEEDL, cohIdx);
                result = this.FCohorts[cohort].SeedlStress;
            }
            else
            {
                result = 0.0;
                denom = 0.0;
                for (cohort = 0; cohort <= this.CohortCount() - 1; cohort++)
                {
                    if (this.BelongsIn(cohort, stSEEDL))
                    {
                        result += this.FCohorts[cohort].Herbage[TOTAL, TOTAL].DM * this.FCohorts[cohort].SeedlStress;
                        denom += this.FCohorts[cohort].Herbage[TOTAL, TOTAL].DM;
                    }
                }

                result = PastureUtil.Div0(result, denom);
            }

            return result;
        }
    }
}
