namespace Models.AgPasture
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Functions;
    using Models.Interfaces;
    using Models.PMF;
    using Models.PMF.Interfaces;
    using Models.Soils;
    using Models.Soils.Nutrients;
    using Models.Surface;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Zone))]
    public class SimpleGrazing : Model
    {
        [Link] Clock clock = null;
        [Link] ISummary summary = null;
        [Link] List<IPlantDamage> forages = null;
        [Link(ByName = true)] ISolute Urea = null;
        [Link] IPhysical soilPhysical = null;
        [Link] SurfaceOrganicMatter surfaceOrganicMatter = null;

        private double residualBiomass;
        private CSharpExpressionFunction expressionFunction;
        private int simpleGrazingFrequency;

        /// <summary>Average potential ME concentration in herbage material (MJ/kg)</summary>
        private const double PotentialMEOfHerbage = 16.0;

        /// <summary>Grazing rotation type enum for drop down.</summary>
        public enum GrazingRotationTypeEnum
        {
            /// <summary>A simple rotation.</summary>
            SimpleRotation,

            /// <summary>A rotation based on a target mass.</summary>
            TargetMass,

            /// <summary>Timing of grazing is controlled elsewhere.</summary>
            TimingControlledElsewhere,

            /// <summary>Flexible grazing using an expression.</summary>
            Flexible
        }

        /// <summary>class for encapsulating a urine return.</summary>
        public class UrineReturnType : EventArgs
        {
            /// <summary>Amount of urine to return (kg)</summary>
            public double Amount { get; set;  }

            /// <summary>Depth (mm) of soil to return urine into.</summary>
            public double Depth { get; set;  }

            /// <summary>Grazed dry matter.</summary>
            public double GrazedDM { get; set; }
        }

        /// <summary>Invoked when a grazing occurs.</summary>
        public event EventHandler Grazed;

        /// <summary>Invoked when biomass is removed.</summary>
        public event BiomassRemovedDelegate BiomassRemoved;

        /// <summary>Invoked when urine is to be returned to soil.</summary>
        /// <remarks>
        /// This event provides a mechanism for another model to perform a
        /// urine return to the soil. If no other model subscribes to this 
        /// event then SimpleGrazing will do the urine return. This mechanism
        /// allows a urine patch model to work.
        /// </remarks>
        public event EventHandler<UrineReturnType> DoUrineReturn;

        ////////////// GUI parameters shown to user //////////////

        /// <summary>Use a strict rotation, a target pasture mass, or both?</summary>
        [Separator("Grazing parameters")]
        [Description("Use a simple rotation, a target pasture mass, or both?")]
        [Units("-")]
        public GrazingRotationTypeEnum GrazingRotationType { get; set; }

        /// <summary></summary>
        [Separator("Settings for the 'Simple Rotation'")]
        [Description("Frequency of grazing (days) or \"end of month\"")]
        [Units("days")]
        [Display(EnabledCallback = "IsSimpleGrazingTurnedOn")]
        public string SimpleGrazingFrequencyString { get; set; }

        /// <summary></summary>
        [Description("Minimum grazeable dry matter to trigger grazing (kgDM/ha). Set to zero to turn off.")]
        [Units("kgDM/ha")]
        [Display(EnabledCallback = "IsSimpleGrazingTurnedOn")]
        public double SimpleMinGrazable { get; set; }

        /// <summary></summary>
        [Description("Residual pasture mass after grazing (kgDM/ha)")]
        [Units("kgDM/ha")]
        [Display(EnabledCallback = "IsSimpleGrazingTurnedOn")]
        public double SimpleGrazingResidual { get; set; }

        /// <summary></summary>
        [Separator("Settings for the 'Target Mass' - all values by month from January")]
        [Description("Target mass of pasture to trigger grazing event, monthly values (kgDM/ha)")]
        [Units("kgDM/ha")]
        [Display(EnabledCallback = "IsTargetMassTurnedOn")]
        public double[] PreGrazeDMArray { get; set; }

        /// <summary></summary>
        [Description("Residual mass of pasture post grazing, monthly values (kgDM/ha)")]
        [Units("kgDM/ha")]
        [Display(EnabledCallback = "IsTargetMassTurnedOn")]
        public double[] PostGrazeDMArray { get; set; }

        /// <summary></summary>
        [Separator("Settings for flexible grazing")]
        [Description("Expression for timing of grazing (e.g. AGPRyegrass.CoverTotal > 0.95)")]
        [Display(EnabledCallback = "IsFlexibleGrazingTurnedOn")]
        public string FlexibleExpressionForTimingOfGrazing { get; set; }

        /// <summary></summary>
        [Description("Residual pasture mass after grazing (kgDM/ha)")]
        [Units("kgDM/ha")]
        [Display(EnabledCallback = "IsFlexibleGrazingTurnedOn")]
        public double FlexibleGrazePostDM { get; set; }

        /// <summary></summary>
        [Separator("Optional rules for rotation length")]
        [Description("Monthly maximum rotation length (days)")]
        [Units("days")]
        [Display(EnabledCallback = "IsTargetMassTurnedOn,IsFlexibleGrazingTurnedOn")]
        public double[] MaximumRotationLengthArray { get; set; }

        /// <summary></summary>
        [Description("Monthly minimum rotation length (days)")]
        [Units("days")]
        [Display(EnabledCallback = "IsTargetMassTurnedOn,IsFlexibleGrazingTurnedOn")]
        public double[] MinimumRotationLengthArray { get; set; }

        /// <summary></summary>
        [Separator("Optional no-grazing window")]
        [Description("Start of the no-grazing window (dd-mmm)")]
        [Display(EnabledCallback = "IsNotTimingControlledElsewhere")]
        public string NoGrazingStartString { get; set; }

        /// <summary></summary>
        [Description("End of the no-grazing window (dd-mmm)")]
        [Display(EnabledCallback = "IsNotTimingControlledElsewhere")]
        public string NoGrazingEndString { get; set; }

        /// <summary></summary>
        [Separator("Urine and Dung.")]

        [Description("Fraction of defoliated N going to soil. Remainder is exported as animal product or to lanes/camps (0-1).")]
        public double[] FractionDefoliatedNToSoil { get; set; }   

        /// <summary></summary>
        [Description("Proportion of excreted N going to dung (0-1). Yearly or 12 monthly values. Blank means use C:N ratio of dung.")]
        [Display(EnabledCallback = "IsFractionExcretedNToDungEnabled")]
        public double[] FractionExcretedNToDung { get; set; }

        /// <summary></summary>
        [Description("C:N ratio of biomass for dung. If set to zero it will calculate the C:N using digestibility. ")]
        [Display(EnabledCallback = "IsCNRatioDungEnabled")]
        public double CNRatioDung { get; set; }

        /// <summary></summary>
        [Description("Depth that urine is added (mm)")]
        [Units("mm")]
        public double DepthUrineIsAdded { get; set; }

        /// <summary></summary>
        [Separator("Plant population modifier")]
        [Description("Enter the fraction of population decline due to defoliation (0-1):")]
        public double FractionPopulationDecline { get; set; }

        /// <summary> </summary>
        [Separator("Trampling")]
        [Description("Turn trampling on?")]
        public bool TramplingOn { get; set; }

        /// <summary> </summary>
        [Description("Maximum proportion of litter moved to the soil")]
        [Display(EnabledCallback = "IsTramplingTurnedOn")]
        public double MaximumPropLitterMovedToSoil { get; set; } = 0.1;

        /// <summary> </summary>
        [Description("Pasture removed at the maximum rate (e.g. 900 for heavy cattle, 1200 for ewes)")]
        [Display(EnabledCallback = "IsTramplingTurnedOn")]
        public double PastureConsumedAtMaximumRateOfLitterRemoval { get; set; } = 1200;

        /// <summary></summary>
        [Separator("Grazing species weighting")]
        [Description("Optional proportion weighting to graze the species. Must add up to the number of species.")]
        public double[] SpeciesCutProportions { get; set; }

        ////////////// Callbacks to enable/disable GUI parameters //////////////

        /// <summary></summary>
        public bool IsSimpleGrazingTurnedOn
        {
            get
            {
                return GrazingRotationType == GrazingRotationTypeEnum.SimpleRotation;
            }
        }

        /// <summary></summary>
        public bool IsTargetMassTurnedOn
        {
            get
            {
                return GrazingRotationType == GrazingRotationTypeEnum.TargetMass;
            }
        }

        /// <summary></summary>
        public bool IsNotTimingControlledElsewhere
        {
            get
            {
                return GrazingRotationType != GrazingRotationTypeEnum.TimingControlledElsewhere;
            }
        }

        /// <summary></summary>
        public bool IsFlexibleGrazingTurnedOn
        {
            get
            {
                return GrazingRotationType == GrazingRotationTypeEnum.Flexible;
            }
        }

        /// <summary></summary>
        public bool IsCNRatioDungEnabled
        {
            get
            {
                return FractionExcretedNToDung == null;
            }
        }

        /// <summary></summary>
        public bool IsFractionExcretedNToDungEnabled
        {
            get
            {
                return double.IsNaN(CNRatioDung) || CNRatioDung == 0;
            }
        }

        /// <summary></summary>
        public bool IsTramplingTurnedOn { get { return TramplingOn; } }

        ////////////// Outputs //////////////

        /// <summary>Number of days since grazing.</summary>
        [JsonIgnore]
        public int DaysSinceGraze { get; private set; }

        /// <summary></summary>
        [JsonIgnore]
        public int GrazingInterval { get; private set; }

        /// <summary>DM grazed</summary>
        [JsonIgnore]
        [Units("kgDM/ha")]
        public double GrazedDM { get; private set; }

        /// <summary>N in the DM grazed.</summary>
        [JsonIgnore]
        [Units("kgN/ha")]
        public double GrazedN { get; private set; }

        /// <summary>N in the DM grazed.</summary>
        [JsonIgnore]
        [Units("MJME/ha")]
        public double GrazedME { get; private set; }

        /// <summary>N in urine returned to the paddock.</summary>
        [JsonIgnore]
        [Units("kgN/ha")]
        public double AmountUrineNReturned { get; private set; }

        /// <summary>C in dung returned to the paddock.</summary>
        [JsonIgnore]
        [Units("kgDM/ha")]
        public double AmountDungWtReturned { get; private set; }

        /// <summary>N in dung returned to the paddock.</summary>
        [JsonIgnore]
        [Units("kgN/ha")]
        public double AmountDungNReturned { get; private set; }

        /// <summary>Mass of herbage just before grazing.</summary>
        [JsonIgnore]
        [Units("kgDM/ha")]
        public double PreGrazeDM { get; private set; }

        /// <summary>Mass of harvestable herbage just before grazing.</summary>
        [JsonIgnore]
        [Units("kgDM/ha")]
        public double PreGrazeHarvestableDM { get; private set; }

        /// <summary>Mass of herbage just after grazing.</summary>
        [JsonIgnore]
        [Units("kgDM/ha")]
        public double PostGrazeDM { get; private set; }

        /// <summary>Proportion of each species biomass to the total biomass.</summary>
        [JsonIgnore]
        [Units("0-1")]
        public double[] ProportionOfTotalDM { get; private set; }

        /// <summary>Did grazing happen today?</summary>
        [JsonIgnore]
        [Units("0-1")]
        public bool GrazedToday{ get; private set; }


        ////////////// Methods //////////////

        /// <summary>This method is invoked at the beginning of the simulation.</summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            ProportionOfTotalDM = new double[forages.Count];

            if (GrazingRotationType == GrazingRotationTypeEnum.TargetMass)
            {
                if (PreGrazeDMArray == null || PreGrazeDMArray.Length != 12)
                    throw new Exception("There must be 12 values input for the pre-grazing DM");
                if (PostGrazeDMArray == null || PostGrazeDMArray.Length != 12)
                    throw new Exception("There must be 12 values input for the post-grazing DM");
            }
            else if (GrazingRotationType == GrazingRotationTypeEnum.Flexible)
            {
                if (string.IsNullOrEmpty(FlexibleExpressionForTimingOfGrazing))
                    throw new Exception("You must specify an expression for timing of grazing.");
                expressionFunction = new CSharpExpressionFunction();
                expressionFunction.Parent = this;
                expressionFunction.Expression = "Convert.ToDouble(" + FlexibleExpressionForTimingOfGrazing + ")";
                expressionFunction.CompileExpression();
            }

            if (FractionExcretedNToDung != null && FractionExcretedNToDung.Length != 1 && FractionExcretedNToDung.Length != 12)
                throw new Exception("You must specify either a single value for 'proportion of defoliated nitrogen going to dung' or 12 monthly values.");

            if (SpeciesCutProportions == null)
                SpeciesCutProportions = MathUtilities.CreateArrayOfValues(1.0, forages.Count);

            if (SpeciesCutProportions.Sum() != forages.Count)
                throw new Exception("The species cut weightings must add up to the number of species.");

            if (SimpleGrazingFrequencyString != null && SimpleGrazingFrequencyString.Equals("end of month", StringComparison.InvariantCultureIgnoreCase))
                simpleGrazingFrequency = 0;
            else
                simpleGrazingFrequency = Convert.ToInt32(SimpleGrazingFrequencyString);

            if (FractionDefoliatedNToSoil == null || FractionDefoliatedNToSoil.Length == 0)
                FractionDefoliatedNToSoil = new double[] { 0 };

            // Initialise the days since grazing.
            if (GrazingRotationType == GrazingRotationTypeEnum.SimpleRotation)
                DaysSinceGraze = simpleGrazingFrequency;
            else if ((GrazingRotationType == GrazingRotationTypeEnum.TargetMass ||
                      GrazingRotationType == GrazingRotationTypeEnum.Flexible) &&
                      MinimumRotationLengthArray != null)
                DaysSinceGraze = Convert.ToInt32(MinimumRotationLengthArray[clock.Today.Month - 1]);
        }

        /// <summary>This method is invoked at the beginning of each day to perform management actions.</summary>
        [EventSubscribe("StartOfDay")]
        private void OnStartOfDay(object sender, EventArgs e)
        {
            DaysSinceGraze += 1;
            PostGrazeDM = 0.0;
            GrazedDM = 0.0;
            GrazedN = 0.0;
            GrazedME = 0.0;
            AmountDungNReturned = 0;
            AmountDungWtReturned = 0;
            AmountUrineNReturned = 0;
        }

        /// <summary>This method is invoked at the beginning of each day to perform management actions.</summary>
        [EventSubscribe("DoManagement")]
        private void OnDoManagement(object sender, EventArgs e)
        {
            // Calculate pre-grazed dry matter.
            PreGrazeDM = 0.0;
            PreGrazeHarvestableDM = 0.0;
            foreach (var forage in forages)
            {
                PreGrazeDM += forage.AboveGround.Wt;
                PreGrazeHarvestableDM += forage.AboveGroundHarvestable.Wt;
            }

            // Convert to kg/ha
            PreGrazeDM *= 10;
            PreGrazeHarvestableDM *= 10;

            // Determine if we can graze today.
            GrazedToday = false;
            if (GrazingRotationType == GrazingRotationTypeEnum.SimpleRotation)
                GrazedToday = SimpleRotation();
            else if (GrazingRotationType == GrazingRotationTypeEnum.TargetMass)
                GrazedToday = TargetMass();
            else if (GrazingRotationType == GrazingRotationTypeEnum.Flexible)
                GrazedToday = FlexibleTiming();

            if (NoGrazingStartString != null &&
                NoGrazingEndString != null &&
                DateUtilities.WithinDates(NoGrazingStartString, clock.Today, NoGrazingEndString))
                GrazedToday = false;

            // Perform grazing if necessary.
            if (GrazedToday)
                GrazeToResidual(residualBiomass);
        }

        /// <summary>Perform grazing.</summary>
        /// <param name="residual">The residual biomass to graze to (kg/ha).</param>
        public void GrazeToResidual(double residual)
        {
            var amountDMToRemove = Math.Max(0, PreGrazeDM - residual);
            Graze(amountDMToRemove);


            if (TramplingOn)
            {
                var proportionLitterMovedToSoil = Math.Min(MathUtilities.Divide(PastureConsumedAtMaximumRateOfLitterRemoval, amountDMToRemove, 0),
                                                           MaximumPropLitterMovedToSoil);
                surfaceOrganicMatter.Incorporate(proportionLitterMovedToSoil, depth: 100);
            }
        }

        /// <summary>Perform grazing</summary>
        /// <param name="amountDMToRemove">The amount of biomas to remove (kg/ha).</param>
        public void Graze(double amountDMToRemove)
        {
            GrazingInterval = DaysSinceGraze;  // i.e. yesterday's value
            DaysSinceGraze = 0;

            RemoveDMFromPlants(amountDMToRemove);

            AddUrineToSoil();

            AddDungToSurface();

            // Calculate post-grazed dry matter.
            PostGrazeDM = forages.Sum(forage => forage.AboveGround.Wt);

            // Calculate proportions of each species to the total biomass.
            for (int i = 0; i < forages.Count; i++)
            {
                var proportionToTotalDM = MathUtilities.Divide(forages[i].AboveGround.Wt, PostGrazeDM, 0);
                ProportionOfTotalDM[i] = proportionToTotalDM;
            }

            summary.WriteMessage(this, string.Format("Grazed {0:0.0} kgDM/ha, N content {1:0.0} kgN/ha, ME {2:0.0} MJME/ha", GrazedDM, GrazedN, GrazedME));

            // Reduce plant population if necessary.
            if (FractionPopulationDecline > 0)
            {
                foreach (var forage in forages)
                    forage.ReducePopulation(forage.Population * (1.0 - FractionPopulationDecline));
            }

            // Convert PostGrazeDM to kg/ha
            PostGrazeDM *= 10;

            // Invoke grazed event.
            Grazed?.Invoke(this, new EventArgs());
        }

        /// <summary>Add dung to the soil surface.</summary>
        private void AddDungToSurface()
        {
            var SOMData = new BiomassRemovedType();
            SOMData.crop_type = "RuminantDung_PastureFed";
            SOMData.dm_type = new string[] { SOMData.crop_type };
            SOMData.dlt_crop_dm = new float[] { (float)AmountDungWtReturned };
            SOMData.dlt_dm_n = new float[] { (float)AmountDungNReturned };
            SOMData.dlt_dm_p = new float[] { 0.0F };
            SOMData.fraction_to_residue = new float[] { 1.0F };
            BiomassRemoved.Invoke(SOMData);
        }

        /// <summary>Add urine to the soil.</summary>
        private void AddUrineToSoil()
        {
            if (DoUrineReturn == null)
            {
                // We will do the urine return.
                // find the layer that the fertilizer is to be added to.
                int layer = SoilUtilities.LayerIndexOfDepth(soilPhysical.Thickness, DepthUrineIsAdded);

                var ureaValues = Urea.kgha;
                ureaValues[layer] += AmountUrineNReturned;
                Urea.SetKgHa(SoluteSetterType.Fertiliser, ureaValues);
            }
            else
            {
                // Another model (e.g. urine patch) will do the urine return.
                DoUrineReturn.Invoke(this,
                    new UrineReturnType()
                    {
                        Amount = AmountUrineNReturned,
                        Depth = DepthUrineIsAdded,
                        GrazedDM = GrazedDM
                    });
            }
        }

        /// <summary>Return a value from an array that can have either 1 yearly value or 12 monthly values.</summary>
        private double GetValueFromMonthArray(double[] arr)
        {
            if (arr.Length == 1)
                return arr[0];
            else
                return arr[clock.Today.Month - 1];
        }

        /// <summary>Calculate whether simple rotation can graze today.</summary>
        /// <returns>True if can graze.</returns>
        private bool SimpleRotation()
        {
            bool isEndOfMonth = clock.Today.AddDays(1).Day == 1;
            if ((simpleGrazingFrequency == 0 && isEndOfMonth) ||
                (DaysSinceGraze >= simpleGrazingFrequency && simpleGrazingFrequency > 0))
            {
                residualBiomass = SimpleGrazingResidual;
                if (PreGrazeHarvestableDM > SimpleMinGrazable)
                    return true;
                else
                {
                    summary.WriteMessage(this, "Defoliation will not happen because there is not enough plant material.");
                    DaysSinceGraze = 0;
                }
            }
            return false;
        }

        /// <summary>Calculate whether a target mass rotation can graze today.</summary>
        /// <returns>True if can graze.</returns>
        private bool TargetMass()
        {
            residualBiomass = PostGrazeDMArray[clock.Today.Month - 1];

            // Don't graze if days since last grazing is < minimum
            if (MinimumRotationLengthArray != null && DaysSinceGraze < MinimumRotationLengthArray[clock.Today.Month - 1])
                return false;

            // Do graze if days since last grazing is > maximum
            if (MaximumRotationLengthArray != null && DaysSinceGraze > MaximumRotationLengthArray[clock.Today.Month - 1])
                return true;

            // Do graze if expression is true
            return PreGrazeHarvestableDM > PreGrazeDMArray[clock.Today.Month - 1];
        }

        /// <summary>Calculate whether a target mass and length rotation can graze today.</summary>
        /// <returns>True if can graze.</returns>
        private bool FlexibleTiming()
        {
            residualBiomass = FlexibleGrazePostDM;

            // Don't graze if days since last grazing is < minimum
            if (MinimumRotationLengthArray != null && DaysSinceGraze < MinimumRotationLengthArray[clock.Today.Month - 1])
                return false;

            // Do graze if days since last grazing is > maximum
            if (MaximumRotationLengthArray != null && DaysSinceGraze > MaximumRotationLengthArray[clock.Today.Month - 1])
                return true;

            // Do graze if expression is true
            else
                return expressionFunction.Value() == 1;
        }

        /// <summary>Remove biomass from the specified forage.</summary>
        /// <param name="removeAmount">The total amount to remove from all forages (kg/ha).</param>
        private void RemoveDMFromPlants(double removeAmount)
        {
            // This is a simple implementation. It proportionally removes biomass from organs.
            // What about non harvestable biomass?
            // What about PreferenceForGreenOverDead and PreferenceForLeafOverStems?

            if (removeAmount > 0)
            {
                // Remove a proportion of required DM from each species
                double totalHarvestableWt = 0.0;
                double totalWeightedHarvestableWt = 0.0;
                for (int i = 0; i < forages.Count; i++)
                {
                    var harvestableWt = forages[i].Organs.Sum(organ => organ.Live.Wt + organ.Dead.Wt);  // g/m2
                    totalHarvestableWt += harvestableWt;
                    totalWeightedHarvestableWt += SpeciesCutProportions[i] * harvestableWt;
                }

                var grazedForages = new List<Biomass>();
                for (int i = 0; i < forages.Count; i++)
                {
                    var harvestableWt = forages[i].Organs.Sum(organ => organ.Live.Wt + organ.Dead.Wt);  // g/m2
                    var proportion = harvestableWt * SpeciesCutProportions[i] / totalWeightedHarvestableWt;
                    var amountToRemove = removeAmount * proportion;
                    var grazed = forages[i].RemoveBiomass(amountToRemove);
                    var grazedMetabolisableEnergy = PotentialMEOfHerbage * grazed.DMDOfStructural;

                    GrazedDM += grazed.Wt;
                    GrazedN += grazed.N;
                    GrazedME += grazedMetabolisableEnergy * grazed.Wt;

                    grazedForages.Add(grazed);
                }

                // Check the amount grazed is the same as requested amount to graze.
                if (!MathUtilities.FloatsAreEqual(GrazedDM, removeAmount))
                    throw new Exception("Mass balance check fail. The amount of biomass removed by SimpleGrazing is not equal to amount that should have been removed.");

                double returnedToSoilWt = 0;
                double returnedToSoilN = 0;
                foreach (var grazedForage in grazedForages)
                {
                    returnedToSoilWt += (1 - grazedForage.DMDOfStructural) * grazedForage.Wt;
                    returnedToSoilN += GetValueFromMonthArray(FractionDefoliatedNToSoil) * grazedForage.N;
                }

                double dungNReturned;
                if (CNRatioDung == 0 || double.IsNaN(CNRatioDung))
                    dungNReturned = GetValueFromMonthArray(FractionExcretedNToDung) * returnedToSoilN;
                else
                {
                    const double CToDMRatio = 0.4; // 0.4 is C:DM ratio.
                    dungNReturned = Math.Min(returnedToSoilN, returnedToSoilWt * CToDMRatio / CNRatioDung);
                }

                AmountDungNReturned += dungNReturned;
                AmountDungWtReturned += returnedToSoilWt;
                AmountUrineNReturned += returnedToSoilN - dungNReturned;
            }
        }
    }
}
