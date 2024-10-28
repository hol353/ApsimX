using System;
using System.Linq;
using System.Collections.Generic;
using Models.PMF;
using Models.Core;
using Models.Soils;
using Models.Surface;
using Models.Functions;
using Models.PMF.Interfaces;
using Models.ForageDigestibility;
using Newtonsoft.Json;
using APSIM.Shared.Utilities;

namespace Models.AgPasture
{

    /// <summary>
    /// A model for cutting pasture / plants and calculating and returning excreta to the
    /// soil based on the biomass cut. If this model is put at the top level of the simulation
    /// all child zones (paddocks) are treated uniformly (e.g. for urine patch modelling)
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Zone))]
    public class SimpleGrazing : Model
    {
        [Link] IClock clock = null;
        [Link] ISummary summary = null;
        [Link] Forages forages = null;
        [Link] ScriptCompiler compiler = null;

        private double residualBiomass;
        private IBooleanFunction expressionFunction;
        private int simpleGrazingFrequency;
        private List<ZoneWithForage> zones;

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
        public class UrineDungReturnType : EventArgs
        {
            /// <summary>Grazed dry matter (kg/ha)</summary>
            public double GrazedDM { get; set;  }

            /// <summary>N in grazed dry matter (kg/ha).</summary>
            public double GrazedN { get; set;  }

            /// <summary>Metabolisable energy in grazed dry matter.</summary>
            public double GrazedME { get; set; }
        }

        /// <summary>Invoked when a grazing occurs.</summary>
        public event EventHandler Grazed;

        /// <summary>Invoked when urine and dung is to be returned to soil.</summary>
        /// <remarks>
        /// This event provides a mechanism for another model to perform a
        /// urine and dung return to the soil. If no other model subscribes to this
        /// event then SimpleGrazing will do the return. This mechanism
        /// allows a urine patch model to work.
        /// </remarks>
        public event EventHandler<UrineDungReturnType> DoUrineDungReturn;

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
        [Display(VisibleCallback = nameof(IsSimpleGrazingTurnedOn))]
        public string SimpleGrazingFrequencyString { get; set; }

        /// <summary></summary>
        [Description("Minimum grazeable dry matter to trigger grazing (kgDM/ha). Set to zero to turn off.")]
        [Units("kgDM/ha")]
        [Display(VisibleCallback = "IsSimpleGrazingTurnedOn")]
        public double SimpleMinGrazable { get; set; }

        /// <summary></summary>
        [Description("Residual pasture mass after grazing (kgDM/ha)")]
        [Units("kgDM/ha")]
        [Display(VisibleCallback = "IsSimpleGrazingTurnedOn")]
        public double SimpleGrazingResidual { get; set; }

        /// <summary></summary>
        [Separator("Settings for the 'Target Mass' - all values by month from January")]
        [Description("Target mass of pasture to trigger grazing event, monthly values (kgDM/ha)")]
        [Units("kgDM/ha")]
        [Display(VisibleCallback = "IsTargetMassTurnedOn")]
        public double[] PreGrazeDMArray { get; set; }

        /// <summary></summary>
        [Description("Residual mass of pasture post grazing, monthly values (kgDM/ha)")]
        [Units("kgDM/ha")]
        [Display(VisibleCallback = "IsTargetMassTurnedOn")]
        public double[] PostGrazeDMArray { get; set; }

        /// <summary></summary>
        [Separator("Settings for flexible grazing")]
        [Description("Expression for timing of grazing (e.g. AGPRyegrass.CoverTotal > 0.95)")]
        [Display(VisibleCallback = "IsFlexibleGrazingTurnedOn")]
        public string FlexibleExpressionForTimingOfGrazing { get; set; }

        /// <summary></summary>
        [Description("Residual pasture mass after grazing (kgDM/ha)")]
        [Units("kgDM/ha")]
        [Display(VisibleCallback = "IsFlexibleGrazingTurnedOn")]
        public double FlexibleGrazePostDM { get; set; }

        /// <summary></summary>
        [Separator("Optional rules for rotation length")]
        [Description("Monthly maximum rotation length (days)")]
        [Units("days")]
        [Display(VisibleCallback = nameof(IsMaximumRotationLengthArrayTurnedOn))]
        public double[] MaximumRotationLengthArray { get; set; }

        /// <summary></summary>
        [Description("Monthly minimum rotation length (days)")]
        [Units("days")]
        [Display(VisibleCallback = nameof(IsMinimumRotationLengthArrayTurnedOn))]
        public double[] MinimumRotationLengthArray { get; set; }

        /// <summary></summary>
        [Separator("Optional no-grazing window")]
        [Description("Start of the no-grazing window (dd-mmm)")]
        [Display(VisibleCallback = "IsNotTimingControlledElsewhere")]
        public string NoGrazingStartString { get; set; }

        /// <summary></summary>
        [Description("End of the no-grazing window (dd-mmm)")]
        [Display(VisibleCallback = "IsNotTimingControlledElsewhere")]
        public string NoGrazingEndString { get; set; }

        /// <summary></summary>
        [Separator("Urine and Dung.")]

        [Description("Fraction of defoliated Biomass going to soil. Remainder is exported as animal product or to lanes/camps (0-1).")]
        public double[] FractionDefoliatedBiomassToSoil { get; set; } = new double[] { 1 };

        /// <summary></summary>
        [Description("Fraction of defoliated N going to soil. Remainder is exported as animal product or to lanes/camps (0-1).")]
        public double[] FractionDefoliatedNToSoil { get; set; }

        /// <summary></summary>
        [Description("Proportion of excreted N going to dung (0-1). Yearly or 12 monthly values. Blank means use C:N ratio of dung.")]
        [Display(VisibleCallback = "IsFractionExcretedNToDungEnabled")]
        public double[] FractionExcretedNToDung { get; set; }

        /// <summary></summary>
        [Description("C:N ratio of biomass for dung. If set to zero it will calculate the C:N using digestibility. ")]
        [Display(VisibleCallback = "IsCNRatioDungEnabled")]
        public double CNRatioDung { get; set; }

        /// <summary></summary>
        [Description("Depth that urine is added (mm)")]
        [Units("mm")]
        public double DepthUrineIsAdded { get; set; }

        /// <summary></summary>
        [Description("Send some fraction of the calculated dung off-paddock - usually this should be zero (0-1)")]
        [Units("0-1")]
        public double SendDungElsewhere { get; set; }

        /// <summary></summary>
        [Description("Send some fraction of the  calculated urine off-paddock - usually this should be zero (0-1)")]
        [Units("0-1")]
        public double SendUrineElsewhere { get; set; }

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
        [Display(VisibleCallback = "IsTramplingTurnedOn")]
        public double MaximumPropLitterMovedToSoil { get; set; } = 0.1;

        /// <summary> </summary>
        [Description("Pasture removed at the maximum rate (e.g. 900 for heavy cattle, 1200 for ewes)")]
        [Display(VisibleCallback = "IsTramplingTurnedOn")]
        public double PastureConsumedAtMaximumRateOfLitterRemoval { get; set; } = 1200;

        /// <summary></summary>
        [Separator("Grazing species weighting")]
        [Description("Optional relative weighting for grazing of forages. Must sum to the number of forages (inc. SurfaceOrganicMatter).")]
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

        /// <summary>
        /// Is maximum rotation length input array enabled in the GUI?
        /// </summary>
        public bool IsMaximumRotationLengthArrayTurnedOn => IsTargetMassTurnedOn || IsFlexibleGrazingTurnedOn;

        /// <summary>
        /// Is minimum rotation length array enabled in the GUI?
        /// </summary>
        public bool IsMinimumRotationLengthArrayTurnedOn => IsTargetMassTurnedOn || IsFlexibleGrazingTurnedOn;

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
        public double GrazedDM => zones.Sum(z => z.GrazedDM);

        /// <summary>N in the DM grazed.</summary>
        [JsonIgnore]
        [Units("kgN/ha")]
        public double GrazedN => zones.Sum(z => z.GrazedN);

        /// <summary>N in the DM grazed.</summary>
        [JsonIgnore]
        [Units("MJME/ha")]
        public double GrazedME => zones.Sum(z => z.GrazedME);

        /// <summary>N in urine returned to the paddock.</summary>
        [JsonIgnore]
        [Units("kgN/ha")]
        public double AmountUrineNReturned => zones.Sum(z => z.AmountUrineNReturned);

        /// <summary>C in dung returned to the paddock.</summary>
        [JsonIgnore]
        [Units("kgDM/ha")]
        public double AmountDungWtReturned => zones.Sum(z => z.AmountDungWtReturned);

        /// <summary>N in dung returned to the paddock.</summary>
        [JsonIgnore]
        [Units("kgN/ha")]
        public double AmountDungNReturned => zones.Sum(z => z.AmountDungNReturned);

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
            if (forages == null)
                throw new Exception("No forages component found in simulation.");
            var parentZone = Parent as Zone;
            if (parentZone == null)
                summary.WriteMessage(this, "When SimpleGrazing is in the top level of the simulation (above the paddocks) it is assumed that the child paddocks are zones within a paddock.",
                                     MessageType.Information);
            double areaOfAllZones = forages.ModelsWithDigestibleBiomass.Select(f => f.Zone)
                                                                       .Distinct()
                                                                       .Sum(z => z.Area);
            zones = forages.ModelsWithDigestibleBiomass.GroupBy(f => f.Zone,
                                                                f => f,
                                                                (z, f) => new ZoneWithForage(z, f.ToList(), areaOfAllZones, summary))
                                                       .ToList();


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
                if (CSharpExpressionFunction.Compile(FlexibleExpressionForTimingOfGrazing, this, compiler, out IBooleanFunction f, out string errors))
                    expressionFunction = f;
                else
                    throw new Exception(errors);
            }

            if (FractionExcretedNToDung != null && FractionExcretedNToDung.Length != 1 && FractionExcretedNToDung.Length != 12)
                throw new Exception("You must specify either a single value for 'proportion of defoliated nitrogen going to dung' or 12 monthly values.");

            // If we are at the top level of the simulation then look in first zone for number of forages.
            int numForages;
            if (Parent is Simulation)
                numForages = zones.First().NumForages;
            else
                numForages = zones.Where(z => z.Zone == this.Parent).First().NumForages;
            if (SpeciesCutProportions == null)
                SpeciesCutProportions = MathUtilities.CreateArrayOfValues(1.0, numForages);

            if (SpeciesCutProportions.Sum() != numForages)
                throw new Exception("The species cut weightings must add up to the number of species.");

            if (SimpleGrazingFrequencyString != null && SimpleGrazingFrequencyString.Equals("end of month", StringComparison.InvariantCultureIgnoreCase))
                simpleGrazingFrequency = 0;
            else
                simpleGrazingFrequency = Convert.ToInt32(SimpleGrazingFrequencyString);

            if (FractionDefoliatedNToSoil == null || FractionDefoliatedNToSoil.Length == 0)
                FractionDefoliatedNToSoil = new double[] { 1};

            if (FractionDefoliatedBiomassToSoil == null || FractionDefoliatedBiomassToSoil.Length == 0)
                FractionDefoliatedBiomassToSoil = new double[] { 0 };

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
            ProportionOfTotalDM = new double[zones.First().NumForages];
            PostGrazeDM = 0;

            foreach (var zone in zones)
                zone.OnStartOfDay();
        }

        /// <summary>This method is invoked at the beginning of each day to perform management actions.</summary>
        [EventSubscribe("DoManagement")]
        private void OnDoManagement(object sender, EventArgs e)
        {
            PreGrazeDM = zones.Sum(z => z.TotalDM);
            PreGrazeHarvestableDM = zones.Sum(z => z.HarvestableDM);

            // Determine if we can graze today.
            GrazedToday = false;
            if (GrazingRotationType == GrazingRotationTypeEnum.SimpleRotation)
                GrazedToday = SimpleRotation();
            else if (GrazingRotationType == GrazingRotationTypeEnum.TargetMass)
                GrazedToday = TargetMass();
            else if (GrazingRotationType == GrazingRotationTypeEnum.Flexible)
                GrazedToday = FlexibleTiming();

            if (NoGrazingStartString != null && NoGrazingStartString.Length > 0 &&
                NoGrazingEndString != null && NoGrazingEndString.Length > 0 &&
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
            GrazingInterval = DaysSinceGraze;  // i.e. yesterday's value
            DaysSinceGraze = 0;

            foreach (var zone in zones)
                zone.RemoveDMFromPlants(residual, SpeciesCutProportions);

            DoUrineDungTrampling();

            // Calculate post-grazed dry matter.
            PostGrazeDM = zones.Sum(z => z.TotalDM);

            // Calculate proportions of each species to the total biomass.
            for (int i = 0; i < zones.First().NumForages; i++)
                ProportionOfTotalDM[i] = zones.Select(z => z.ProportionsToTotal[i]).Average();

            summary.WriteMessage(this, string.Format("Grazed {0:0.0} kgDM/ha, N content {1:0.0} kgN/ha, ME {2:0.0} MJME/ha", GrazedDM, GrazedN, GrazedME), MessageType.Diagnostic);

            // Reduce plant population if necessary.
            if (MathUtilities.IsGreaterThan(FractionPopulationDecline, 0.0))
                foreach (var zone in zones)
                    zone.ReducePopulation(FractionPopulationDecline);

            // Invoke grazed event.
            Grazed?.Invoke(this, new EventArgs());
        }

        /// <summary>Add urine to the soil.</summary>
        private void DoUrineDungTrampling()
        {
            if (DoUrineDungReturn == null)
            {
                if (Parent is Zone)
                {
                    foreach (var zone in zones)
                        zone.DoUrineDungTrampling(clock.Today.Month, FractionDefoliatedBiomassToSoil,
                                                  FractionDefoliatedNToSoil, FractionExcretedNToDung,
                                                  CNRatioDung, DepthUrineIsAdded, TramplingOn,
                                                  PastureConsumedAtMaximumRateOfLitterRemoval, MaximumPropLitterMovedToSoil,
                                                  SendDungElsewhere, SendUrineElsewhere);
                }
                else
                    throw new Exception("Currently, when SimpleGrazing is at the top level of a simulation it must have a SimpleCow sibling present.");
            }
            else
            {
                // Another model (e.g. urine patch) will do the urine return.
                DoUrineDungReturn.Invoke(this,
                    new UrineDungReturnType()
                    {
                        GrazedDM = GrazedDM,
                        GrazedN = GrazedN,
                        GrazedME = GrazedME
                    });
            }
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
                if (MathUtilities.IsGreaterThan(PreGrazeHarvestableDM, SimpleMinGrazable))
                    return true;
                else
                {
                    summary.WriteMessage(this, "Defoliation will not happen because there is not enough plant material.", MessageType.Diagnostic);
                    DaysSinceGraze = 0;  //VOS why would this be zeroed if it is not grazed?
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
            // this was return PreGrazeHarvestableDM > PreGrazeDMArray[clock.Today.Month - 1]; but this assessment shoudl be against total DM
            return PreGrazeDM > PreGrazeDMArray[clock.Today.Month - 1];
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
                return expressionFunction.Value();  
        }


        private class ZoneWithForage
        {
            private SurfaceOrganicMatter surfaceOrganicMatter;
            private Solute urea;
            private IPhysical physical;
            private List<ModelWithDigestibleBiomass> forages;
            private double grazedDM;
            private double grazedN;
            private double grazedME;
            private double amountDungNReturned;
            private double amountDungWtReturned;
            private double amountUrineNReturned;
            private double dmRemovedToday;
            private double areaWeighting;
            private List<Forages.MaterialRemoved> grazedForages = new();
            private ISummary summary;

            /// <summary>onstructor</summary>
            /// <param name="zone">Our zone.</param>
            /// <param name="forages">Our forages.</param>
            /// <param name="areaOfAllZones">The area of all zones in the simulation.</param>
            /// <param name="summary">The Summary file.</param>
            public ZoneWithForage(Zone zone, List<ModelWithDigestibleBiomass> forages, double areaOfAllZones, ISummary summary)
            {
                this.Zone = zone;
                this.forages = forages;
                surfaceOrganicMatter = zone.FindInScope<SurfaceOrganicMatter>();
                urea = zone.FindInScope<Solute>("Urea");
                physical = zone.FindInScope<IPhysical>();
                areaWeighting = zone.Area / areaOfAllZones;
                this.summary = summary;
            }

            public Zone Zone { get; private set; }

            /// <summary>The number of forages in our care</summary>
            public int NumForages => forages.Count;

            /// <summary>Dry matter of all forages in zone, weighted for area on zone (kg/ha)</summary>
            public double TotalDM => forages.Sum(f => f.Material.Sum(m => m.Total.Wt) * 10) * areaWeighting;

            /// <summary>Harvestable dry matter of all forages in zone, weighted for area on zone (kg/ha)</summary>
            public double HarvestableDM => forages.Sum(f => f.Material.Sum(m => m.Consumable.Wt) * 10) * areaWeighting;

            /// <summary>Proportions of each species within the zone to the total dm within the zone (0-1).</summary>
            public List<double> ProportionsToTotal => forages.Select(f => f.Material.Sum(m => m.Total.Wt) / TotalDM).ToList();

            /// <summary>Area weighted grazed dry matter (kg/ha)</summary>
            public double GrazedDM => grazedDM * areaWeighting;

            /// <summary>Area weighted grazed nitrogen (kg N/ha)</summary>
            public double GrazedN => grazedN * areaWeighting;

            /// <summary>Area weighted metabolisable energy in grazed dry matter (kg/ha)</summary>
            public double GrazedME => grazedME * areaWeighting;

            /// <summary>Area weighted nitrogen in dung (kg N/ha)</summary>
            public double AmountDungNReturned => amountDungNReturned * areaWeighting;

            /// <summary>Area weighted dry matter in dung (kg/ha)</summary>
            public double AmountDungWtReturned => amountDungWtReturned * areaWeighting;

            /// <summary>Area weighted nitrogen in uring (kg N/ha)</summary>
            public double AmountUrineNReturned => amountUrineNReturned * areaWeighting;

            /// <summary>
            /// Called at start of day.
            /// </summary>
            public void OnStartOfDay()
            {
                grazedDM = 0.0;
                grazedN = 0.0;
                grazedME = 0.0;
                amountDungNReturned = 0;
                amountDungWtReturned = 0;
                amountUrineNReturned = 0;
                grazedForages.Clear();
            }

            /// <summary>
            /// Reduce the forage population,
            /// </summary>
            /// <param name="fractionPopulationDecline">The fraction to reduce to population to.</param>
            public void ReducePopulation(double fractionPopulationDecline)
            {
                foreach (var forage in forages)
                {
                    if ((forage as IModel) is IHasPopulationReducer populationReducer)
                        populationReducer.ReducePopulation(populationReducer.Population * (1.0 - fractionPopulationDecline));
                    else
                        throw new Exception($"Model {forage.Name} is unable to reduce its population due to grazing. Not implemented.");
                }
            }

            /// <summary>Remove biomass from the specified forage.</summary>
            /// <param name="residual">The residual to cut to (kg/ha).</param>
            /// <param name="speciesCutProportions">The proportions to cut each species.</param>
            public void RemoveDMFromPlants(double residual, double[] speciesCutProportions)
            {
                // This is a simple implementation. It proportionally removes biomass from organs.
                // What about non harvestable biomass?
                // What about PreferenceForGreenOverDead and PreferenceForLeafOverStems?
                double preGrazeDM = forages.Sum(f => f.Material.Sum(m => m.Total.Wt * 10));
                double removeAmount = Math.Max(0, preGrazeDM - residual) / 10; // to g/m2

                dmRemovedToday = removeAmount;
                if (MathUtilities.IsGreaterThan(removeAmount, 0.0))
                {
                    // Remove a proportion of required DM from each species
                    double totalHarvestableWt = 0.0;
                    double totalWeightedHarvestableWt = 0.0;
                    for (int i = 0; i < forages.Count; i++)
                    {
                        var harvestableWt = forages[i].Material.Sum(m => m.Consumable.Wt);  // g/m2
                        totalHarvestableWt += harvestableWt;
                        totalWeightedHarvestableWt += speciesCutProportions[i] * harvestableWt;
                    }

                    // If a fraction consumable was specified in the forages component by the user then the above calculated
                    // removeAmount might be > consumable amount. Constrain the removeAmount to the consumable 
                    // amount so that we don't get an exception thrown in ModelWithDigestibleBiomass.RemoveBiomass method
                    removeAmount = Math.Min(removeAmount, totalHarvestableWt);

                    for (int i = 0; i < forages.Count; i++)
                    {
                        var harvestableWt = forages[i].Material.Sum(m => m.Consumable.Wt);  // g/m2
                        var proportion = harvestableWt * speciesCutProportions[i] / totalWeightedHarvestableWt;
                        var amountToRemove = removeAmount * proportion;
                        if (MathUtilities.IsGreaterThan(amountToRemove, 0.0))
                        {
                            var grazed = forages[i].RemoveBiomass(amountToRemove: amountToRemove);
                            double grazedDigestibility = grazed.Digestibility;
                            var grazedMetabolisableEnergy = PotentialMEOfHerbage * grazedDigestibility;

                            grazedDM += grazed.Wt;  // kg/ha
                            grazedN += grazed.N;    // kg/ha
                            grazedME += grazedMetabolisableEnergy * grazed.Wt;

                            grazedForages.Add(grazed);
                        }
                    }
                }
            }

            /// <summary>
            /// Perform urine and dung return and trampling.
            /// </summary>
            /// <param name="month"></param>
            /// <param name="fractionDefoliatedBiomassToSoil"></param>
            /// <param name="fractionDefoliatedNToSoil"></param>
            /// <param name="fractionExcretedNToDung"></param>
            /// <param name="CNRatioDung"></param>
            /// <param name="depthUrineIsAdded"></param>
            /// <param name="doTrampling"></param>
            /// <param name="pastureConsumedAtMaximumRateOfLitterRemoval"></param>
            /// <param name="maximumPropLitterMovedToSoil"></param>
            /// <param name="sendDungElsewhere"></param>
            /// <param name="sendUrineElsewhere"></param>
            public void DoUrineDungTrampling(int month, double[] fractionDefoliatedBiomassToSoil,
                                    double[] fractionDefoliatedNToSoil,
                                    double[] fractionExcretedNToDung,
                                    double CNRatioDung,
                                    double depthUrineIsAdded,
                                    bool doTrampling,
                                    double pastureConsumedAtMaximumRateOfLitterRemoval,
                                    double maximumPropLitterMovedToSoil,
                                    double sendDungElsewhere,
                                    double sendUrineElsewhere)
            {
                var urineDung = UrineDungReturn.CalculateUrineDungReturn(grazedForages,  
                                                                         GetValueFromMonthArray(fractionDefoliatedBiomassToSoil, month),
                                                                         GetValueFromMonthArray(fractionDefoliatedNToSoil, month),
                                                                         GetValueFromMonthArray(fractionExcretedNToDung, month),
                                                                         CNRatioDung,
                                                                         sendUrineElsewhere,
                                                                         sendDungElsewhere);

                if (urineDung != null)
                {
                    amountDungNReturned += urineDung.DungNToSoil;
                    amountDungWtReturned += urineDung.DungWtToSoil;
                    amountUrineNReturned += urineDung.UrineNToSoil;

                    UrineDungReturn.DoUrineReturn(urineDung, physical.Thickness, urea, depthUrineIsAdded);
                    summary.WriteMessage(this.Zone, $"Urine N added to the soil of {urineDung.UrineNToSoil} to a depth of {depthUrineIsAdded} mm", MessageType.Diagnostic);
                    UrineDungReturn.DoDungReturn(urineDung, surfaceOrganicMatter);
                    summary.WriteMessage(this.Zone, $"Dung N and C added to the surface organic matter {urineDung.DungNToSoil}", MessageType.Diagnostic);

                    if (doTrampling)
                    {
                        var proportionLitterMovedToSoil = Math.Min(MathUtilities.Divide(pastureConsumedAtMaximumRateOfLitterRemoval, dmRemovedToday, 0),
                                                                    maximumPropLitterMovedToSoil);
                        surfaceOrganicMatter.Incorporate(proportionLitterMovedToSoil, depth: 100);
                    }
                }
            }

            /// <summary>Return a value from an array that can have either 1 yearly value or 12 monthly values.</summary>
            private static double GetValueFromMonthArray(double[] arr, int month)
            {
                if (arr == null)
                    return double.NaN;
                else if (arr.Length == 1)
                    return arr[0];
                else
                    return arr[month - 1];
            }
        }
    }
}
