using System;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.PMF.Interfaces;
using Models.PMF.Struct;
using Newtonsoft.Json;

namespace Models.PMF.Organs
{
    ///<summary>
    /// A leaf cohort model
    /// </summary>
    /// <remarks>
    ///
    /// @startuml
    /// Initialized -> Appeared: Appearance
    /// Appeared -> Expanded: GrowthDuration
    /// Expanded -> Senescing: LagDuration
    /// Senescing -> Senesced: SenescenceDuration
    /// Senesced -> Detaching: DetachmentLagDuration
    /// Detaching -> Detached: DetachmentDuration
    /// Initialized ->Expanded: IsGrowing
    /// Initialized -> Senesced: IsAlive
    /// Initialized -> Senesced: IsGreen
    /// Initialized -> Senescing: IsNotSenescing
    /// Senescing -> Senesced: IsSenescing
    /// Expanded -> Detached: IsFullyExpanded
    /// Senesced -> Detached: ShouldBeDead
    /// Senesced -> Detached: Finished
    /// Appeared -> Detached: IsAppeared
    /// Initialized -> Detached: IsInitialised
    /// @enduml
    ///
    /// Leaf death
    /// ------------------------
    /// The leaf area, structural biomass and structural nitrogen of
    /// green (live) parts is subtracted by a fraction.
    ///
    /// </remarks>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class LeafCohort : Model
    {
        #region Paramater Input Classes

        /// <summary>The plant</summary>
        [Link]
        private Plant Plant = null;

        /// <summary>The structure</summary>
        [Link]
        private Structure Structure = null;

        /// <summary>The leaf</summary>
        [Link]
        private Leaf Leaf = null;

        /// <summary>The leaf apex model</summary>
        [Link(IsOptional = true)]
        public IApex Apex = null;


        /// <summary>The leaf apex model</summary>
        public ApexGroup ApexCohort;

        /// <summary>The clock</summary>
        [Link]
        public IClock Clock = null;

        [Link]
        private ISurfaceOrganicMatter SurfaceOrganicMatter = null;

        /// <summary>The live</summary>
        [JsonIgnore]
        public Biomass Live = new Biomass();

        /// <summary>The dead</summary>
        [JsonIgnore]
        public Biomass Dead = new Biomass();

        /// <summary>The live start</summary>
        public Biomass LiveStart = null;

        #endregion

        #region Class Fields

        /// <summary>The rank</summary>
        [Description("Rank")]
        public int Rank { get; set; } // 1 based ranking

        /// <summary>The area</summary>
        [Description("Area")]
        [Units("mm2")]
        public double Area { get; set; }

        //Leaf coefficients
        /// <summary>The age</summary>
        [JsonIgnore]
        public double Age;

        /// <summary>The n reallocation factor</summary>
        public double NReallocationFactor;

        /// <summary>The dm reallocation factor</summary>
        public double DMReallocationFactor;

        /// <summary>The n retranslocation factor</summary>
        public double NRetranslocationFactor;

        /// <summary>The dm retranslocation factor</summary>
        public double DMRetranslocationFactor;

        /// <summary>The functional n conc</summary>
        private double FunctionalNConc;

        /// <summary>The luxary n conc</summary>
        private double LuxaryNConc;

        /// <summary>The summary</summary>
        [Link]
        private ISummary Summary = null;

        /// <summary>The structural fraction</summary>
        [JsonIgnore]
        public double StructuralFraction;

        /// <summary>The non structural fraction</summary>
        [JsonIgnore]
        public double StorageFraction;

        /// <summary>The maximum live area</summary>
        [JsonIgnore]
        public double MaxLiveArea;

        /// <summary>The maximum live area</summary>
        [JsonIgnore]
        public double MaxCohortPopulation;

        /// <summary>The growth duration</summary>
        [JsonIgnore]
        public double GrowthDuration;

        /// <summary>The lag duration</summary>
        [JsonIgnore]
        public double LagDuration;

        /// <summary>The senescence duration</summary>
        [JsonIgnore]
        public double SenescenceDuration;

        /// <summary>The detachment lag duration</summary>
        [JsonIgnore]
        public double DetachmentLagDuration;

        /// <summary>The detachment duration</summary>
        [JsonIgnore]
        public double DetachmentDuration;

        /// <summary>The specific leaf area maximum</summary>
        [JsonIgnore]
        public double SpecificLeafAreaMax;

        /// <summary>The specific leaf area minimum</summary>
        [JsonIgnore]
        public double SpecificLeafAreaMin;

        /// <summary>The maximum n conc</summary>
        [JsonIgnore]
        public double MaximumNConc;

        /// <summary>The minimum n conc</summary>
        [JsonIgnore]
        public double MinimumNConc;

        /// <summary>The initial n conc</summary>
        [JsonIgnore]
        public double InitialNConc;

        /// <summary>The live area</summary>
        [JsonIgnore]
        public double LiveArea;

        /// <summary>The dead area</summary>
        [JsonIgnore]
        public double DeadArea;

        /// <summary>The maximum area</summary>
        [JsonIgnore]
        public double MaxArea;

        /// <summary>The maximum area</summary>
        [JsonIgnore]
        public double LeafSizeShape = 0.01;

        /// <summary>The size of senessing leaves relative to the other leaves in teh cohort</summary>
        [JsonIgnore]
        public double SenessingLeafRelativeSize = 1;

        /// <summary>Gets or sets the cover above.</summary>
        /// <value>The cover above.</value>
        [JsonIgnore]
        public double CoverAbove { get; set; }

        /// <summary>The shade induced sen rate</summary>
        public double ShadeInducedSenRate;

        /// <summary>The senesced frac</summary>
        public double SenescedFrac;

        /// <summary>The detached frac</summary>
        private double DetachedFrac;

        /// <summary>The cohort population</summary>
        [JsonIgnore]
        public double CohortPopulation; //Number of leaves in this cohort

        /// <summary>The cell division stress factor</summary>
        [JsonIgnore]
        public double CellDivisionStressFactor = 1;

        /// <summary>The cell division stress accumulation</summary>
        [JsonIgnore]
        public double CellDivisionStressAccumulation;

        /// <summary>The cell division stress days</summary>
        [JsonIgnore]
        public double CellDivisionStressDays;

        //Leaf Initial status paramaters
        /// <summary>The leaf start n retranslocation supply</summary>
        [JsonIgnore]
        public double LeafStartNRetranslocationSupply;

        /// <summary>The leaf start n reallocation supply</summary>
        [JsonIgnore]
        public double LeafStartNReallocationSupply;

        /// <summary>The leaf start dm retranslocation supply</summary>
        [JsonIgnore]
        public double LeafStartDMRetranslocationSupply;

        /// <summary>The leaf start dm reallocation supply</summary>
        [JsonIgnore]
        public double LeafStartDMReallocationSupply;

        /// <summary>The leaf start area</summary>
        [JsonIgnore]
        public double LeafStartArea;

        /// <summary>
        /// The leaf start metabolic n reallocation supply
        /// </summary>
        [JsonIgnore]
        public double LeafStartMetabolicNReallocationSupply;

        /// <summary>
        /// The leaf start non structural n reallocation supply
        /// </summary>
        [JsonIgnore]
        public double LeafStartStorageNReallocationSupply;

        /// <summary>
        /// The leaf start metabolic n retranslocation supply
        /// </summary>
        [JsonIgnore]
        public double LeafStartMetabolicNRetranslocationSupply;

        /// <summary>
        /// The leaf start non structural n retranslocation supply
        /// </summary>
        [JsonIgnore]
        public double LeafStartStorageNRetranslocationSupply;

        /// <summary>
        /// The leaf start metabolic dm reallocation supply
        /// </summary>
        [JsonIgnore]
        public double LeafStartMetabolicDMReallocationSupply;

        /// <summary>
        /// The leaf start non structural dm reallocation supply
        /// </summary>
        [JsonIgnore]
        public double LeafStartStorageDMReallocationSupply;

        //variables used in calculating daily supplies and deltas
        /// <summary>Gets the DM amount detached (send to surface OM) (g/m2)</summary>
        [JsonIgnore]
        public Biomass Detached { get; set; }

        /// <summary>Gets the DM amount removed from the system (harvested, grazed, etc) (g/m2)</summary>
        [JsonIgnore]
        public Biomass Removed { get; set; }

        /// <summary>The delta potential area</summary>
        public double DeltaPotentialArea;

        /// <summary>The delta water constrained area</summary>
        public double DeltaStressConstrainedArea;

        /// <summary>The delta carbon constrained area</summary>
        public double DeltaCarbonConstrainedArea;

        /// <summary>The potential structural dm allocation</summary>
        public double PotentialStructuralDMAllocation;

        /// <summary>The potential metabolic dm allocation</summary>
        public double PotentialMetabolicDMAllocation;

        /// <summary>The metabolic n reallocated</summary>
        public double MetabolicNReallocated;

        /// <summary>The metabolic wt reallocated</summary>
        public double MetabolicWtReallocated;

        /// <summary>The non structural n reallocated</summary>
        public double StorageNReallocated;

        /// <summary>The non structural wt reallocated</summary>
        public double StorageWtReallocated;

        /// <summary>The metabolic n retranslocated</summary>
        public double MetabolicNRetranslocated;

        /// <summary>The non structural n retrasnlocated</summary>
        public double StorageNRetrasnlocated;

        /// <summary>The dm retranslocated</summary>
        public double DMRetranslocated;

        /// <summary>The metabolic n allocation</summary>
        public double MetabolicNAllocation;

        /// <summary>The structural dm allocation</summary>
        public double StructuralDMAllocation;

        /// <summary>The metabolic dm allocation</summary>
        public double MetabolicDMAllocation;

        #endregion

        #region Class Properties

        /// <summary>Has the leaf chort been initialised?</summary>
        [JsonIgnore]
        public bool IsInitialised;

        /// <summary>Gets a value indicating whether this instance has not appeared.</summary>
        /// <value>
        /// <c>true</c> if this instance is not appeared; otherwise, <c>false</c>.
        /// </value>
        public bool IsNotAppeared
        {
            get { return IsInitialised && Age <= 0; }
        }

        /// <summary>Gets a value indicating whether this instance is growing.</summary>
        /// <value>
        /// <c>true</c> if this instance is growing; otherwise, <c>false</c>.
        /// </value>
        public bool IsGrowing
        {
            get { return Age < GrowthDuration; }
        }

        /// <summary>Gets or sets a value indicating whether this instance is appeared.</summary>
        /// <value>
        /// <c>true</c> if this instance is appeared; otherwise, <c>false</c>.
        /// </value>
        [JsonIgnore]
        public bool IsAppeared { get; set; }

        /// <summary>Gets a value indicating whether this instance is fully expanded.</summary>
        /// <value>
        /// <c>true</c> if this instance is fully expanded; otherwise, <c>false</c>.
        /// </value>
        public bool IsFullyExpanded
        {
            get { return IsAppeared && Age > GrowthDuration; }
        }

        /// <summary>Gets a value indicating whether this instance is green.</summary>
        /// <value><c>true</c> if this instance is green; otherwise, <c>false</c>.</value>
        public bool IsGreen
        {
            get { return Age < GrowthDuration + LagDuration + SenescenceDuration; }
        }
        /// <summary>Gets a value indicating whether this instance is senescing.</summary>
        /// <value>
        /// <c>true</c> if this instance is senescing; otherwise, <c>false</c>.
        /// </value>
        public bool IsSenescing
        {
            get { return Age > GrowthDuration + LagDuration && Age < GrowthDuration + LagDuration + SenescenceDuration; }

        }
        /// <summary>Gets a value indicating whether this instance is not senescing.</summary>
        /// <value>
        /// <c>true</c> if this instance is not senescing; otherwise, <c>false</c>.
        /// </value>
        public bool IsNotSenescing
        {
            get { return Age < GrowthDuration + LagDuration; }
        }
        /// <summary>Gets a value indicating whether this <see cref="LeafCohort"/> is finished.</summary>
        /// <value><c>true</c> if finished; otherwise, <c>false</c>.</value>
        public bool Finished
        {
            get { return IsAppeared && !IsGreen; }
        }
        /// <summary>Gets a value indicating whether this instance is dead.</summary>
        /// <value><c>true</c> if this instance is dead; otherwise, <c>false</c>.</value>
        public bool IsDead
        {
            get
            {
                return MathUtilities.FloatsAreEqual(LiveArea, 0.0) && !MathUtilities.FloatsAreEqual(DeadArea, 0.0);
            }
        }
        /// <summary>Gets the maximum size.</summary>

        /// <summary>Gets the fraction expanded.</summary>
        /// <value>The fraction expanded.</value>
        public double FractionExpanded
        {
            get
            {
                if (Age <= 0)
                    return 0;
                if (Age >= GrowthDuration)
                    return 1;
                return Age / GrowthDuration;
            }
        }

        /// <summary>Gets the specific area.</summary>
        /// <value>The specific area.</value>
        public double SpecificArea
        {
            get
            {
                if (Live.Wt > 0)
                    return LiveArea / Live.Wt;
                return 0;
            }
        }

        /// <summary>MaintenanceRespiration</summary>
        public double MaintenanceRespiration { get; private set; }
        #endregion

        #region Arbitration methods

        /// <summary>Gets the structural dm demand.</summary>
        /// <value>The structural dm demand.</value>
        public double StructuralDMDemand
        {
            get
            {
                if (IsGrowing)
                {
                    double TotalDMDemand = Math.Min(DeltaPotentialArea / ((SpecificLeafAreaMax + SpecificLeafAreaMin) / 2),
                        DeltaStressConstrainedArea / SpecificLeafAreaMin);
                    if (TotalDMDemand < 0)
                        throw new Exception("Negative DMDemand in" + this);
                    return TotalDMDemand * StructuralFraction;

                }
                return 0;
            }
        }

        /// <summary>Gets the metabolic dm demand.</summary>
        /// <value>The metabolic dm demand.</value>
        public double MetabolicDMDemand
        {
            get
            {
                if (IsGrowing)
                {
                    double TotalDMDemand = Math.Min(DeltaPotentialArea / ((SpecificLeafAreaMax + SpecificLeafAreaMin) / 2),
                        DeltaStressConstrainedArea / SpecificLeafAreaMin);
                    return TotalDMDemand * (1 - StructuralFraction);
                }
                return 0;
            }
        }

        /// <summary>Gets the non structural dm demand.</summary>
        /// <value>The non structural dm demand.</value>
        public double StorageDMDemand
        {
            get
            {
                if (IsNotSenescing)
                {
                    double MaxStorageDM = (MetabolicDMDemand + StructuralDMDemand + LiveStart.MetabolicWt +
                                                 LiveStart.StructuralWt) * StorageFraction;
                    return Math.Max(0.0, MaxStorageDM - LiveStart.StorageWt);
                }
                return 0.0;
            }
        }

        /// <summary>Gets the structural n demand.</summary>
        /// <value>The structural n demand.</value>
        public double StructuralNDemand
        {
            get
            {
                if ((IsNotSenescing) && (ShadeInducedSenRate == 0.0))
                    // Assuming a leaf will have no demand if it is senescing and will have no demand if it is is shaded conditions
                    return MinimumNConc * PotentialStructuralDMAllocation;
                return 0.0;
            }
        }

        /// <summary>Gets the non structural n demand.</summary>
        /// <value>The non structural n demand.</value>
        public double StorageNDemand
        {
            get
            {
                if (IsNotSenescing && (ShadeInducedSenRate == 0.0) && (StorageFraction > 0))
                    // Assuming a leaf will have no demand if it is senescing and will have no demand if it is is shaded conditions.  Also if there is
                    return Math.Max(0.0, LuxaryNConc * (LiveStart.StructuralWt + LiveStart.MetabolicWt
                                                      + PotentialStructuralDMAllocation + PotentialMetabolicDMAllocation) -
                                         Live.StorageN);
                return 0.0;
            }
        }

        /// <summary>Gets the metabolic n demand.</summary>
        /// <value>The metabolic n demand.</value>
        public double MetabolicNDemand
        {
            get
            {
                if (IsNotSenescing && (ShadeInducedSenRate == 0.0))
                    // Assuming a leaf will have no demand if it is senescing and will have no demand if it is is shaded conditions
                    return FunctionalNConc * PotentialMetabolicDMAllocation;

                return 0.0;
            }
        }

        /// <summary>Sets the dm allocation.</summary>
        /// <value>The dm allocation.</value>
        /// <exception cref="System.Exception">
        /// -ve DM Allocation to Leaf Cohort
        /// or
        /// DM Allocated to Leaf Cohort is in excess of its Demand
        /// or
        /// A leaf cohort cannot supply that amount for DM Reallocation
        /// or
        /// Leaf cohort given negative DM Reallocation
        /// or
        /// Negative DM retranslocation from a Leaf Cohort
        /// or
        /// A leaf cohort cannot supply that amount for DM retranslocation
        /// </exception>
        public BiomassAllocationType DMAllocation
        {
            set
            {
                //Firstly allocate DM
                if (value.Structural + value.Storage + value.Metabolic < -0.0000000001)
                    throw new Exception("-ve DM Allocation to Leaf Cohort");
                if (value.Structural + value.Storage + value.Metabolic - (StructuralDMDemand + MetabolicDMDemand + StorageDMDemand) > 0.001)
                    throw new Exception("DM Allocated to Leaf Cohort is in excess of its Demand");
                if (StructuralDMDemand + MetabolicDMDemand + StorageDMDemand > 0)
                {
                    StructuralDMAllocation = value.Structural;
                    MetabolicDMAllocation = value.Metabolic;
                    Live.StructuralWt += value.Structural;
                    Live.MetabolicWt += value.Metabolic;
                    Live.StorageWt += value.Storage;
                }

                //Then remove reallocated DM
                if (value.Reallocation -
                    (LeafStartMetabolicDMReallocationSupply + LeafStartStorageDMReallocationSupply) >
                    0.00000000001)
                    throw new Exception("A leaf cohort cannot supply that amount for DM Reallocation");
                if (value.Reallocation < -0.0000000001)
                    throw new Exception("Leaf cohort given negative DM Reallocation");
                if (value.Reallocation > 0.0)
                {
                    StorageWtReallocated = Math.Min(LeafStartStorageDMReallocationSupply, value.Reallocation);
                    //Reallocate Storage first
                    MetabolicWtReallocated = Math.Max(0.0,
                            Math.Min(value.Reallocation - StorageWtReallocated, LeafStartMetabolicDMReallocationSupply));
                    //Then reallocate metabolic DM
                    Live.StorageWt -= StorageWtReallocated;
                    Live.MetabolicWt -= MetabolicWtReallocated;
                }

                //Then remove retranslocated DM
                if (value.Retranslocation < -0.0000000001)
                    throw new Exception("Negative DM retranslocation from a Leaf Cohort");
                if (value.Retranslocation > LeafStartDMRetranslocationSupply)
                    throw new Exception("A leaf cohort cannot supply that amount for DM retranslocation");
                if ((value.Retranslocation > 0) && (LeafStartDMRetranslocationSupply > 0))
                    Live.StorageWt -= value.Retranslocation;
            }
        }

        /// <summary>Sets the n allocation.</summary>
        /// <value>The n allocation.</value>
        /// <exception cref="System.Exception">
        /// A leaf cohort cannot supply that amount for N Reallocation
        /// or
        /// Leaf cohort given negative N Reallocation
        /// or
        /// A leaf cohort cannot supply that amount for N Retranslocation
        /// or
        /// Leaf cohort given negative N Retranslocation
        /// </exception>
        public BiomassAllocationType NAllocation
        {
            set
            {
                //Fresh allocations
                Live.StructuralN += value.Structural;
                Live.MetabolicN += value.Metabolic;
                Live.StorageN += value.Storage;
                //Reallocation
                if (value.Reallocation -
                    (LeafStartMetabolicNReallocationSupply + LeafStartStorageNReallocationSupply) > 0.00000000001)
                    throw new Exception("A leaf cohort cannot supply that amount for N Reallocation");
                if (value.Reallocation < -0.0000000001)
                    throw new Exception("Leaf cohort given negative N Reallocation");
                if (value.Reallocation > 0.0)
                {
                    StorageNReallocated = Math.Min(LeafStartStorageNReallocationSupply, value.Reallocation);
                    //Reallocate Storage first
                    MetabolicNReallocated = Math.Max(0.0, value.Reallocation - LeafStartStorageNReallocationSupply);
                    //Then reallocate metabolic N
                    Live.StorageN -= StorageNReallocated;
                    Live.MetabolicN -= MetabolicNReallocated;
                }
                //Retranslocation
                if (value.Retranslocation -
                    (LeafStartMetabolicNRetranslocationSupply + LeafStartStorageNRetranslocationSupply) >
                    0.00000000001)
                    throw new Exception("A leaf cohort cannot supply that amount for N Retranslocation");
                if (value.Retranslocation < -0.0000000001)
                    throw new Exception("Leaf cohort given negative N Retranslocation");
                if (value.Retranslocation > 0.0)
                {
                    StorageNRetrasnlocated = Math.Min(LeafStartStorageNRetranslocationSupply,
                        value.Retranslocation); //Reallocate Storage first
                    MetabolicNRetranslocated = Math.Max(0.0,
                            value.Retranslocation - LeafStartStorageNRetranslocationSupply);
                    //Then reallocate metabolic N
                    Live.StorageN -= StorageNRetrasnlocated;
                    Live.MetabolicN -= MetabolicNRetranslocated;
                }
            }
        }

        /// <summary>Sets the dm potential allocation.</summary>
        /// <value>The dm potential allocation.</value>
        /// <exception cref="System.Exception">
        /// -ve Potential DM Allocation to Leaf Cohort
        /// or
        /// Potential DM Allocation to Leaf Cohortis in excess of its Demand
        /// or
        /// -ve Potential DM Allocation to Leaf Cohort
        /// or
        /// Potential DM Allocation to Leaf Cohortis in excess of its Demand
        /// </exception>
        public BiomassPoolType DMPotentialAllocation
        {
            set
            {
                if (value.Structural < -0.0000000001)
                    throw new Exception("-ve Potential DM Allocation to Leaf Cohort");
                if ((value.Structural - StructuralDMDemand) > 0.0000000001)
                    throw new Exception("Potential DM Allocation to Leaf Cohortis in excess of its Demand");
                if (value.Metabolic < -0.0000000001)
                    throw new Exception("-ve Potential DM Allocation to Leaf Cohort");
                if ((value.Metabolic - MetabolicDMDemand) > 0.0000000001)
                    throw new Exception("Potential DM Allocation to Leaf Cohortis in excess of its Demand");

                if (StructuralDMDemand > 0)
                    PotentialStructuralDMAllocation = value.Structural;
                if (MetabolicDMDemand > 0)
                    PotentialMetabolicDMAllocation = value.Metabolic;
            }
        }
        #endregion

        #region Functions
        /// <summary>Constructor</summary>
        public LeafCohort()
        {
            Detached = new Biomass();
            Removed = new Biomass();
        }

        /// <summary>Returns a clone of this object</summary>
        /// <returns></returns>
        public virtual LeafCohort Clone()
        {
            LeafCohort newLeaf = (LeafCohort)MemberwiseClone();
            newLeaf.Live = new Biomass();
            newLeaf.Dead = new Biomass();
            newLeaf.Detached = new Biomass();
            newLeaf.Removed = new Biomass();
            return newLeaf;
        }

        /// <summary>Does the initialisation.</summary>
        public void DoInitialisation(LeafCohortParameters CohortParameters)
        {
            IsInitialised = true;
            Age = 0;

            // All this stuff below is here to ensure FN is not < 1 on the day of emergence
            double functionalNConc = (CohortParameters.CriticalNConc.Value() -
                                          CohortParameters.MinimumNConc.Value() * CohortParameters.StructuralFraction.Value()) *
                                         (1 / (1 - CohortParameters.StructuralFraction.Value()));
            Live.StructuralWt = 0.00001;
            Live.MetabolicWt = 0.00001;
            Live.StorageWt = 0.00001;
            Live.StructuralN = Live.StructuralWt * functionalNConc;
            Live.MetabolicN = Live.MetabolicWt * functionalNConc;
            Live.StorageN = Live.StorageWt * functionalNConc;
        }

        /// <summary>Does the appearance.</summary>
        /// <param name="cohortParams">The leaf fraction.</param>
        /// <param name="leafCohortParameters">The leaf cohort parameters.</param>
        public void DoAppearance(ApparingLeafParams cohortParams, LeafCohortParameters leafCohortParameters)
        {
            if (Apex != null)
            {
                ApexCohort = new ApexGroup
                {
                    GroupAge = Apex.GroupAge,
                    GroupSize = Apex.GroupSize,
                    Number = Apex.Number
                };
                CohortPopulation = Apex.LeafTipAppearance(Plant.Population, cohortParams.TotalStemPopn);
            }
            else
                CohortPopulation = Structure.TotalStemPopn;
            CohortPopulation *= cohortParams.FinalFraction;
            Age = cohortParams.CohortAge;

            Name = "Leaf" + Rank.ToString();
            IsAppeared = true;

            MaxArea = leafCohortParameters.MaxArea.Value() * CellDivisionStressFactor;
            GrowthDuration = leafCohortParameters.GrowthDuration.Value() * cohortParams.FinalFraction;
            LagDuration = leafCohortParameters.LagDuration.Value();
            SenescenceDuration = leafCohortParameters.SenescenceDuration.Value();
            DetachmentLagDuration = leafCohortParameters.DetachmentLagDuration.Value();
            DetachmentDuration = leafCohortParameters.DetachmentDuration.Value();
            StructuralFraction = leafCohortParameters.StructuralFraction.Value();
            SpecificLeafAreaMax = leafCohortParameters.SpecificLeafAreaMax.Value();
            SpecificLeafAreaMin = leafCohortParameters.SpecificLeafAreaMin.Value();
            MaximumNConc = leafCohortParameters.MaximumNConc.Value();
            MinimumNConc = leafCohortParameters.MinimumNConc.Value();
            StorageFraction = leafCohortParameters.StorageFraction.Value();
            InitialNConc = leafCohortParameters.InitialNConc.Value();

            if (MathUtilities.FloatsAreEqual(MaxArea, 0))
                throw new Exception("LeafCohortParameters.MaxArea is zero. This is invalid due to the equation:  Age = Area / MaxArea * GrowthDuration");
            if (MathUtilities.FloatsAreEqual(MaxArea, 0))
                throw new Exception("LeafCohortParameters.SpecificLeafAreaMax+LeafCohortParameters.SpecificLeafAreaMin cannot be zero.");
            if (MathUtilities.FloatsAreEqual(StructuralFraction, 1))
                throw new Exception("LeafCohortParameters.StructuralFraction cannot have a value of 1.");
            if (MathUtilities.FloatsAreEqual(StructuralFraction, 0))
                throw new Exception("LeafCohortParameters.StructuralFraction cannot have a value of 0.");

            if (Area > 0) //Only set age for cohorts that have an area specified in the xml.
            {
                if (Area > MaxArea)
                {
                    Summary.WriteMessage(this, "Initial area is more than max area for cohort " + Rank.ToString() + ".", MessageType.Warning);
                    Area = MaxArea;
                }

                Age = Area / MaxArea * GrowthDuration;
            }
            //FIXME.  The size function is not linear so this does not give an exact starting age.  Should re-arange the the size function to return age for a given area to initialise age on appearance.
            LiveArea = Area * CohortPopulation;
            Live.StructuralWt = LiveArea / ((SpecificLeafAreaMax + SpecificLeafAreaMin) / 2) * StructuralFraction;
            Live.StructuralN = Live.StructuralWt * InitialNConc;
            // FunctionalNConc = (CriticalNConc * (DM.Structural + DM.Metabolic) - MinimumNConc * DM.Structural)) / DM.Metabolic
            //                 = (CriticalNConc - MinimumNConc * (DM.Structural / (DM.Structural + DM.Metabolic))) / (DM.Metabolic / (DM.Structural + DM.Metabolic))
            FunctionalNConc = (leafCohortParameters.CriticalNConc.Value() -
                               leafCohortParameters.MinimumNConc.Value() * StructuralFraction) *
                              (1 / (1 - StructuralFraction));
            LuxaryNConc = leafCohortParameters.MaximumNConc.Value() -
                           leafCohortParameters.CriticalNConc.Value();
            Live.MetabolicWt = Live.StructuralWt * 1 / StructuralFraction - Live.StructuralWt;
            Live.StorageWt = 0;
            Live.StructuralN = Live.StructuralWt * MinimumNConc;
            Live.MetabolicN = Live.MetabolicWt * FunctionalNConc;
            Live.StorageN = 0;
            NReallocationFactor = leafCohortParameters.NReallocationFactor.Value();
            DMReallocationFactor = leafCohortParameters.DMReallocationFactor.Value();
            NRetranslocationFactor = leafCohortParameters.NRetranslocationFactor.Value();
            DMRetranslocationFactor = leafCohortParameters.DMRetranslocationFactor.Value();
            LeafSizeShape = leafCohortParameters.LeafSizeShapeParameter.Value();
        }

        /// <summary>Does the potential growth.</summary>
        /// <param name="tt">The tt.</param>
        /// <param name="extinctionCoefficient">The extinctionCoefficient.</param>
        /// <param name="leafCohortParameters">The leaf cohort parameters.</param>
        public void DoPotentialGrowth(double tt, double extinctionCoefficient, LeafCohortParameters leafCohortParameters)
        {
            //Reduce leaf Population in Cohort due to plant mortality
            double startPopulation = CohortPopulation;


            if (Structure.ProportionPlantMortality > 0)
                CohortPopulation -= CohortPopulation * Structure.ProportionPlantMortality;

            //Reduce leaf Population in Cohort  due to branch mortality
            if ((Structure.ProportionBranchMortality > 0) && (CohortPopulation > Structure.MainStemPopn))
                //Ensure we there are some branches.
                CohortPopulation -= Math.Min(Structure.ProportionBranchMortality * (CohortPopulation - Structure.MainStemPopn), CohortPopulation - Plant.Population);

            double propnStemMortality = MathUtilities.Divide(startPopulation - CohortPopulation, startPopulation, 0);

            //Calculate Accumulated Stress Factor for reducing potential leaf size
            if (IsNotAppeared)
            {
                CellDivisionStressDays += 1;
                CellDivisionStressAccumulation += leafCohortParameters.CellDivisionStressValue;
                CellDivisionStressFactor = Math.Max(CellDivisionStressAccumulation / CellDivisionStressDays, 0.01);
            }

            if (IsAppeared)
            {
                //Accelerate thermal time accumulation if crop is water stressed.
                double thermalTime;
                if (IsFullyExpanded && IsNotSenescing)
                    thermalTime = tt * leafCohortParameters.LagAccelerationValue;
                else if (IsSenescing)
                    thermalTime = tt * leafCohortParameters.SenescenceAccelerationValue;
                else thermalTime = tt;

                //Modify leaf area using tillering approach
                double sizeAgeFactor = 1;
                if (Apex != null && ApexCohort.GroupSize.Length > 0)
                {
                    double totalf = ApexCohort.GroupSize[0];
                    for (int i = 1; i < ApexCohort.GroupAge.Length; i++)
                    {
                        double f = leafCohortParameters.LeafSizeAgeMultiplier.Value(((int)ApexCohort.GroupAge[i] - 1));
                        totalf += f * ApexCohort.GroupSize[i];
                    }
                    sizeAgeFactor = totalf / ApexCohort.GroupSize.Sum();
                }

                //Leaf area growth parameters
                DeltaPotentialArea = PotentialAreaGrowthFunction(thermalTime) * sizeAgeFactor;
                //Calculate delta leaf area in the absence of water stress
                DeltaStressConstrainedArea = DeltaPotentialArea * leafCohortParameters.ExpansionStressValue;
                //Reduce potential growth for water stress

                CoverAbove = Leaf.CoverAboveCohort(Rank, extinctionCoefficient); // Calculate cover above leaf cohort (unit??? FIXME-EIT)
                ShadeInducedSenRate = leafCohortParameters.ShadeInducedSenescenceRate.Value();
                SenessingLeafRelativeSize = leafCohortParameters.SenessingLeafRelativeSize.Value();
                SenescedFrac = FractionSenescing(thermalTime, propnStemMortality, SenessingLeafRelativeSize, leafCohortParameters);

                // Doing leaf mass growth in the cohort
                Biomass liveBiomass = new Biomass(Live);

                //Set initial leaf status values
                LeafStartArea = LiveArea;
                LiveStart = new Biomass(Live);

                //If the model allows reallocation of senescent DM do it.
                if ((DMReallocationFactor > 0) && (SenescedFrac > 0))
                {
                    // DM to reallocate.

                    LeafStartMetabolicDMReallocationSupply = LiveStart.MetabolicWt * SenescedFrac * DMReallocationFactor;
                    LeafStartStorageDMReallocationSupply = LiveStart.StorageWt * SenescedFrac *
                                                                 DMReallocationFactor;

                    LeafStartDMReallocationSupply = LeafStartMetabolicDMReallocationSupply +
                                                    LeafStartStorageDMReallocationSupply;
                    liveBiomass.MetabolicWt -= LeafStartMetabolicDMReallocationSupply;
                    liveBiomass.StorageWt -= LeafStartStorageDMReallocationSupply;

                }
                else
                {
                    LeafStartMetabolicDMReallocationSupply =
                        LeafStartStorageDMReallocationSupply = LeafStartDMReallocationSupply = 0;
                }

                LeafStartDMRetranslocationSupply = liveBiomass.StorageWt * DMRetranslocationFactor;
                //Nretranslocation is that which occurs before uptake (senessed metabolic N and all non-structuralN)
                LeafStartMetabolicNReallocationSupply = SenescedFrac * liveBiomass.MetabolicN * NReallocationFactor;
                LeafStartStorageNReallocationSupply = SenescedFrac * liveBiomass.StorageN * NReallocationFactor;
                //Retranslocated N is only that which occurs after N uptake. Both Non-structural and metabolic N are able to be retranslocated but metabolic N will only be moved if remobilisation of non-structural N does not meet demands
                LeafStartMetabolicNRetranslocationSupply = Math.Max(0.0,
                    liveBiomass.MetabolicN * (1 - SenescedFrac) * NRetranslocationFactor);
                LeafStartStorageNRetranslocationSupply = Math.Max(0.0,
                    liveBiomass.StorageN * (1 - SenescedFrac) * NRetranslocationFactor);
                LeafStartNReallocationSupply = LeafStartStorageNReallocationSupply + LeafStartMetabolicNReallocationSupply;
                LeafStartNRetranslocationSupply = LeafStartStorageNRetranslocationSupply + LeafStartMetabolicNRetranslocationSupply;

                //zero locals variables
                PotentialStructuralDMAllocation = 0;
                PotentialMetabolicDMAllocation = 0;
                DMRetranslocated = 0;
                MetabolicNReallocated = 0;
                StorageNReallocated = 0;
                MetabolicWtReallocated = 0;
                StorageWtReallocated = 0;
                MetabolicNRetranslocated = 0;
                StorageNRetrasnlocated = 0;
                MetabolicNAllocation = 0;
                StructuralDMAllocation = 0;
                MetabolicDMAllocation = 0;
            }
        }

        /// <summary>Does the actual growth.</summary>
        /// <param name="tt">The tt.</param>
        /// <param name="leafCohortParameters">The leaf cohort parameters.</param>
        public void DoActualGrowth(double tt, LeafCohortParameters leafCohortParameters)
        {
            if (!IsAppeared)
                return;

            //Accellerate thermal time accumulation if crop is water stressed.
            double thermalTime;
            if (IsFullyExpanded && IsNotSenescing)
                thermalTime = tt * leafCohortParameters.LagAcceleration.Value();
            else if (IsSenescing)
                thermalTime = tt * leafCohortParameters.SenescenceAcceleration.Value();
            else thermalTime = tt;

            //Growing leaf area after DM allocated
            DeltaCarbonConstrainedArea = (StructuralDMAllocation + MetabolicDMAllocation) * SpecificLeafAreaMax;
            //Fixme.  Live.Nonstructural should probably be included in DM supply for leaf growth also
            double deltaActualArea = Math.Min(DeltaStressConstrainedArea, DeltaCarbonConstrainedArea);


            //Fixme.  Live.Storage should probably be included in DM supply for leaf growth also
            LiveArea += deltaActualArea;

            //Senessing leaf area
            double areaSenescing = LiveArea * SenescedFrac;
            double areaSenescingN = 0;
            if ((Live.MetabolicNConc <= MinimumNConc) && (MetabolicNRetranslocated - MetabolicNAllocation > 0.0))
                areaSenescingN = LeafStartArea * (MetabolicNRetranslocated - MetabolicNAllocation) / LiveStart.MetabolicN;

            double leafAreaLoss = Math.Max(areaSenescing, areaSenescingN);
            if (leafAreaLoss > 0)
                SenescedFrac = Math.Min(1.0, leafAreaLoss / LeafStartArea);

            double structuralWtSenescing = SenescedFrac * LiveStart.StructuralWt;
            double structuralNSenescing = SenescedFrac * LiveStart.StructuralN;
            double metabolicWtSenescing = SenescedFrac * LiveStart.MetabolicWt;
            double metabolicNSenescing = SenescedFrac * LiveStart.MetabolicN;
            double StorageWtSenescing = SenescedFrac * LiveStart.StorageWt;
            double StorageNSenescing = SenescedFrac * LiveStart.StorageN;

            DeadArea = DeadArea + leafAreaLoss;
            LiveArea = LiveArea - leafAreaLoss;
            // Final leaf area of cohort that will be integrated in Leaf.cs? (FIXME-EIT)

            Live.StructuralWt -= structuralWtSenescing;
            Dead.StructuralWt += structuralWtSenescing;

            Live.StructuralN -= structuralNSenescing;
            Dead.StructuralN += structuralNSenescing;

            Live.MetabolicWt -= Math.Max(0.0, metabolicWtSenescing - MetabolicWtReallocated);
            Dead.MetabolicWt += Math.Max(0.0, metabolicWtSenescing - MetabolicWtReallocated);


            Live.MetabolicN -= Math.Max(0.0, metabolicNSenescing - MetabolicNReallocated - MetabolicNRetranslocated);
            //Don't Seness todays N if it has been taken for reallocation
            Dead.MetabolicN += Math.Max(0.0, metabolicNSenescing - MetabolicNReallocated - MetabolicNRetranslocated);

            Live.StorageN -= Math.Max(0.0,
                StorageNSenescing - StorageNReallocated - StorageNRetrasnlocated);
            //Dont Senesess todays Storage N if it was retranslocated or reallocated
            Dead.StorageN += Math.Max(0.0,
                StorageNSenescing - StorageNReallocated - StorageNRetrasnlocated);

            Live.StorageWt -= Math.Max(0.0, StorageWtSenescing - DMRetranslocated);
            Live.StorageWt = Math.Max(0.0, Live.StorageWt);

            Dead.StorageWt += Math.Max(0.0,
                StorageWtSenescing - DMRetranslocated - StorageWtReallocated);

            //Do Maintenance respiration
            MaintenanceRespiration = 0;
            if (leafCohortParameters.MaintenanceRespirationFunction != null && (Live.MetabolicWt + Live.StorageWt) > 0)
            {
                MaintenanceRespiration += Live.MetabolicWt * leafCohortParameters.MaintenanceRespirationFunction.Value();
                MaintenanceRespiration += Live.StorageWt * leafCohortParameters.MaintenanceRespirationFunction.Value();
            }

            Age = Age + thermalTime;

            // Do Detachment of this Leaf Cohort
            // ---------------------------------
            DetachedFrac = FractionDetaching(thermalTime);
            if (DetachedFrac > 0.0)
            {
                double detachedWt = Dead.Wt * DetachedFrac;
                double detachedN = Dead.N * DetachedFrac;

                DeadArea *= 1 - DetachedFrac;
                Dead.StructuralWt *= 1 - DetachedFrac;
                Dead.StructuralN *= 1 - DetachedFrac;
                Dead.StorageWt *= 1 - DetachedFrac;
                Dead.StorageN *= 1 - DetachedFrac;
                Dead.MetabolicWt *= 1 - DetachedFrac;
                Dead.MetabolicN *= 1 - DetachedFrac;

                if (detachedWt > 0)
                {
                    SurfaceOrganicMatter.Add(detachedWt * 10, detachedN * 10, 0, Plant.PlantType, "Leaf");
                    Biomass todaysDetached = new()
                    {
                        StructuralWt = detachedWt * 10,
                        StructuralN = detachedN * 10
                    };
                    Detached.Add(todaysDetached);
                }
            }
        }

        /// <summary>Does the kill.</summary>
        /// <param name="fraction">The fraction.</param>
        public void DoKill(double fraction)
        {
            if (!IsInitialised)
                return;

            double change = LiveArea * fraction;
            LiveArea -= change;
            DeadArea += change;

            change = Live.StructuralWt * fraction;
            Live.StructuralWt -= change;
            Dead.StructuralWt += change;

            change = Live.StorageWt * fraction;
            Live.StorageWt -= change;
            Dead.StorageWt += change;

            change = Live.StructuralN * fraction;
            Live.StructuralN -= change;
            Dead.StructuralN += change;

            change = Live.StorageN * fraction;
            Live.StorageN -= change;
            Dead.StorageN += change;
        }

        /// <summary>Does the frost.</summary>
        /// <param name="fraction">The fraction.</param>
        public void DoFrost(double fraction)
        {
            if (IsAppeared)
                DoKill(fraction);
        }

        /// <summary>Does the zeroing of some varibles.</summary>
        public void DoDailyCleanup()
        {
            Detached.Clear();
            Removed.Clear();
        }

        /// <summary>Potential delta LAI</summary>
        /// <param name="tt">thermal-time</param>
        /// <returns>(mm2 leaf/cohort position/m2 soil/day)</returns>
        public double PotentialAreaGrowthFunction(double tt)
        {
            if (Age > GrowthDuration)
            {
                return 0;
            }
            double ageToday = Math.Min(Age + tt, GrowthDuration);

            double leafSizeDelta = SizeFunction(ageToday) - SizeFunction(Age);
            //mm2 of leaf expanded in one day at this cohort (Today's minus yesterday's Area/cohort)
            double growth = CohortPopulation * leafSizeDelta;
            // Daily increase in leaf area for that cohort position in a per m2 basis (mm2/m2/day)
            if (growth < 0)
                throw new Exception("Netagive potential leaf area expansion in" + this);
            return growth;
        }

        /// <summary>Potential average leaf size for today per cohort (no stress)</summary>
        /// <param name="tt">Thermal-time accumulation since cohort initiation</param>
        /// <returns>Average leaf size (mm2/leaf)</returns>
        protected double SizeFunction(double tt)
        {
            if (GrowthDuration <= 0)
                throw new Exception(
                    "Trying to calculate leaf size with a growth duration parameter value of zero won't work");
            double oneLessShape = 1 - LeafSizeShape;
            double alpha = -Math.Log((1 / oneLessShape - 1) / (MaxArea / (MaxArea * LeafSizeShape) - 1)) / GrowthDuration;
            double leafSize = MaxArea / (1 + (MaxArea / (MaxArea * LeafSizeShape) - 1) * Math.Exp(-alpha * tt));
            double y0 = MaxArea / (1 + (MaxArea / (MaxArea * LeafSizeShape) - 1) * Math.Exp(-alpha * 0));
            double yDiffprop = y0 / (MaxArea / 2);
            double scaledLeafSize = (leafSize - y0) / (1 - yDiffprop);
            return scaledLeafSize;
        }

        /// <summary>Live leaf number</summary>
        public double LiveStemNumber(LeafCohortParameters leafCohortParameters)
        {
            if (Age <= 0)
            {
                return 0;
            }
            if (Apex == null)
                return 0;
            else
            {
                double _lagDuration;
                double _senescenceDuration;
                double _GrowthDuration;
                double lsn = 0;
                for (int i = 0; i < ApexCohort.GroupAge.Length; i++)
                {

                    if (i == 0)
                    {
                        _lagDuration = LagDuration;
                        _senescenceDuration = SenescenceDuration;
                        _GrowthDuration = GrowthDuration;
                    }
                    else
                    {
                        _GrowthDuration = GrowthDuration * leafCohortParameters.LeafSizeAgeMultiplier.Value((int)ApexCohort.GroupAge[i] - 1);
                        _lagDuration = LagDuration * leafCohortParameters.LagDurationAgeMultiplier.Value((int)ApexCohort.GroupAge[i] - 1);
                        _senescenceDuration = SenescenceDuration * leafCohortParameters.SenescenceDurationAgeMultiplier.Value((int)ApexCohort.GroupAge[i] - 1);
                    }

                    if (Age >= 0 && Age < _lagDuration + _GrowthDuration + _senescenceDuration / 2)
                    {
                        lsn += ApexCohort.GroupSize[i];
                    }
                }
                return lsn * CohortPopulation / MathUtilities.Sum(ApexCohort.GroupSize);
            }
        }

        /// <summary>Fractions the senescing.</summary>
        /// <param name="tt">The tt.</param>
        /// <param name="stemMortality">The stem mortality.</param>
        /// <param name="senessingLeafRelativeSize">The relative size of senessing tillers leaves relative to the other leaves in the cohort</param>
        /// <param name="leafCohortParameters">The associated leaf cohort parameters</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Bad Fraction Senescing</exception>
        public double FractionSenescing(double tt, double stemMortality, double senessingLeafRelativeSize, LeafCohortParameters leafCohortParameters)
        {
            //Calculate fraction of leaf area senessing based on age and shading.  This is used to to calculate change in leaf area and Nreallocation supply.
            if (!IsAppeared)
                return 0;

            double _lagDuration;
            double _senescenceDuration;
            double fracSenAge = 0;
            if (Apex != null && ApexCohort.GroupAge.Length > 0)
            {
                for (int i = 0; i < ApexCohort.GroupAge.Length; i++)
                {
                    if (i == 0)
                    {
                        _lagDuration = LagDuration;
                        _senescenceDuration = SenescenceDuration;
                    }
                    else
                    {
                        _lagDuration = LagDuration * leafCohortParameters.LagDurationAgeMultiplier.Value((int)ApexCohort.GroupAge[i] - 1);
                        _senescenceDuration = SenescenceDuration * leafCohortParameters.SenescenceDurationAgeMultiplier.Value((int)ApexCohort.GroupAge[i] - 1);
                    }

                    double ttInSenPhase = Math.Max(0.0, Age + tt - _lagDuration - GrowthDuration);
                    double _fracSenAge = 0;
                    if (ttInSenPhase > 0)
                    {
                        double leafDuration = GrowthDuration + _lagDuration + _senescenceDuration;
                        double remainingTt = Math.Max(0, leafDuration - Age);

                        if (remainingTt == 0)
                            _fracSenAge = 1;
                        else
                            _fracSenAge = Math.Min(1, Math.Min(tt, ttInSenPhase) / remainingTt);
                        if ((_fracSenAge > 1) || (_fracSenAge < 0))
                            throw new Exception("Bad Fraction Senescing");
                    }
                    else
                    {
                        _fracSenAge = 0;
                    }

                    fracSenAge += _fracSenAge * ApexCohort.GroupSize[i];
                }
                fracSenAge = fracSenAge / ApexCohort.GroupSize.Sum();
            }
            else
            {
                double ttInSenPhase = Math.Max(0.0, Age + tt - LagDuration - GrowthDuration);
                if (ttInSenPhase > 0)
                {
                    double leafDuration = GrowthDuration + LagDuration + SenescenceDuration;
                    double remainingTt = Math.Max(0, leafDuration - Age);

                    if (remainingTt == 0)
                        fracSenAge = 1;
                    else
                        fracSenAge = Math.Min(1, Math.Min(tt, ttInSenPhase) / remainingTt);
                    if ((fracSenAge > 1) || (fracSenAge < 0))
                        throw new Exception("Bad Fraction Senescing");
                }
                else
                    fracSenAge = 0;
            }

            MaxLiveArea = Math.Max(MaxLiveArea, LiveArea);
            MaxCohortPopulation = Math.Max(MaxCohortPopulation, CohortPopulation);

            double fracSenShade = 0;
            if (LiveArea > 0)
            {
                fracSenShade = Math.Min(MaxLiveArea * ShadeInducedSenRate, LiveArea) / LiveArea;
                fracSenShade += stemMortality * senessingLeafRelativeSize;
                fracSenShade = Math.Min(fracSenShade, 1.0);
            }

            return Math.Max(fracSenAge, fracSenShade);
        }

        /// <summary>Fractions the detaching.</summary>
        /// <param name="tt">The thermal time.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Bad Fraction Detaching</exception>
        public double FractionDetaching(double tt)
        {
            double fracDetach;
            double ttInDetachPhase = Math.Max(0.0,
                Age + tt - LagDuration - GrowthDuration - SenescenceDuration - DetachmentLagDuration);
            if (ttInDetachPhase > 0)
            {
                double leafDuration = GrowthDuration + LagDuration + SenescenceDuration + DetachmentLagDuration +
                                      DetachmentDuration;
                double remainingTt = Math.Max(0, leafDuration - Age);

                if (remainingTt == 0)
                    fracDetach = 1;
                else
                    fracDetach = Math.Min(1, Math.Min(tt, ttInDetachPhase) / remainingTt);
                if ((fracDetach > 1) || (fracDetach < 0))
                    throw new Exception("Bad Fraction Detaching");
            }
            else
                fracDetach = 0;

            return fracDetach;
        }

        #endregion

    }
}