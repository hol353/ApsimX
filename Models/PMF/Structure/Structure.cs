using System;
using Models.Core;
using Models.Functions;
using Models.Interfaces;
using Models.PMF.Phen;
using Newtonsoft.Json;

namespace Models.PMF.Struct
{
    /// <summary>
    /// The structure model simulates morphological development of the plant to inform the Leaf class 
    /// when and how many leaves and branches appear and provides an estimate of height.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Plant))]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class Structure : Model, IStructure
    {
        // 1. Links
        //-------------------------------------------------------------------------------------------
        [Link]
        private Plant plant = null;

        [Link]
        private ILeaf leaf = null;

        [Link]
        private Phenology phenology = null;

        /// <summary>The thermal time</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction thermalTime = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction phyllochron = null;

        /// <summary>The main stem final node number</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction finalLeafNumber = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction heightModel = null;

        /// <summary>Branching rate</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction branchingRate = null;

        /// <summary>Branch mortality</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction branchMortality = null;


        /// <summary>The Stage that cohorts are initialised on</summary>
        [Description("The Stage that cohorts are initialised on")]
        public string CohortInitialisationStage { get; set; } = "Germination";

        /// <summary>The Stage that leaves are initialised on</summary>
        [Description("The Stage that leaves are initialised on")]
        public string LeafInitialisationStage { get; set; } = "Emergence";


        // 2. Private fields
        //-------------------------------------------------------------------------------------------

        private bool leavesInitialised;

        private bool cohortsInitialised;

        private bool firstPass;


        // 4. Public Events And Enums
        //-------------------------------------------------------------------------------------------

        /// <summary>Occurs when plant Germinates.</summary>
        public event EventHandler InitialiseLeafCohorts;

        /// <summary>Occurs when ever an new vegetative leaf cohort is initiated on the stem apex.</summary>
        public event EventHandler<CohortInitParams> AddLeafCohort;

        /// <summary>Occurs when ever an new leaf tip appears.</summary>
        public event EventHandler<ApparingLeafParams> LeafTipAppearance;


        // 5. Public properties
        //-------------------------------------------------------------------------------------------
        /// <summary>The Leaf Appearance Data </summary>
        [JsonIgnore]
        public CohortInitParams InitParams { get; set; }

        /// <summary>CohortToInitialise</summary>
        [JsonIgnore]
        public int CohortToInitialise { get; set; }

        /// <summary>TipToAppear</summary>
        [JsonIgnore]
        public int TipToAppear { get; set; }

        /// <summary>Did another leaf appear today?</summary>
        [JsonIgnore]
        public bool TimeForAnotherLeaf { get; set; }

        /// <summary>Have all leaves appeared?</summary>
        [JsonIgnore]
        public bool AllLeavesAppeared { get; set; }

        /// <summary>The Leaf Appearance Data </summary>
        [JsonIgnore]
        public ApparingLeafParams CohortParams { get; set; }

        /// <summary>Gets or sets the primary bud no.</summary>
        [JsonIgnore]
        public double PrimaryBudNo { get; set; }

        /// <summary>Gets or sets the total stem popn.</summary>
        [JsonIgnore]
        public double TotalStemPopn { get; set; }

        //Plant leaf number state variables
        /// <summary>Number of mainstem nodes which have their tips appeared</summary>
        [JsonIgnore]
        public double PotLeafTipsAppeared { get; set; }

        /// <summary>"Number of mainstem nodes which have their tips appeared"</summary>
        [JsonIgnore]
        public double LeafTipsAppeared { get; set; }

        /// <summary>Number of leaves appeared per plant including all main stem and branch leaves</summary>
        [JsonIgnore]
        public double PlantTotalNodeNo { get; set; }

        /// <summary>Gets or sets the proportion branch mortality.</summary>
        [JsonIgnore]
        public double ProportionBranchMortality { get; set; }

        /// <summary>Gets or sets the proportion plant mortality.</summary>
        [JsonIgnore]
        public double ProportionPlantMortality { get; set; }

        /// <value>The change in HaunStage each day.</value>
        [JsonIgnore]
        public double DeltaHaunStage { get; set; }

        /// <value>The delta node number.</value>
        [JsonIgnore]
        public double DeltaTipNumber { get; set; }

        /// <summary>The number of branches, used by zadoc class for calcualting zadoc score in the 20's</summary>
        [JsonIgnore]
        public double BranchNumber { get; set; }

        /// <summary>The relative size of the current cohort.  Is always 1.0 apart for the final cohort where it can be less than 1.0 if final leaf number is not an interger value</summary>
        [JsonIgnore]
        public double NextLeafProportion { get; set; }

        /// <summary> The change in plant population due to plant mortality set in the plant class </summary>
        [JsonIgnore]
        public double DeltaPlantPopulation { get; set; }

        /// <summary>"Number of mainstems per meter"</summary>
        [JsonIgnore]
        public double MainStemPopn { get { return plant.Population * PrimaryBudNo; } }

        /// <summary>Number of leaves yet to appear</summary>
        [JsonIgnore]
        public double RemainingNodeNo { get { return finalLeafNumber.Value() - LeafTipsAppeared; } }

        /// <summary>Gets the height.</summary>
        [JsonIgnore]
        public double Height { get; private set; }

        /// <summary>Number of appeared leaves per primary bud unit including all main stem and branch leaves</summary>
        [JsonIgnore]
        public double PrimaryBudTotalNodeNo { get { return PlantTotalNodeNo / PrimaryBudNo; } }

        /// <summary>Relative progress toward final leaf.</summary>
        [JsonIgnore]
        public double RelativeNodeApperance { get { return LeafTipsAppeared / finalLeafNumber.Value(); } }
        /// <summary>Total number of leaves per shoot .</summary>
        [JsonIgnore]
        public double TotalLeavesPerShoot { get; set; }

        // 6. Public methods
        //-------------------------------------------------------------------------------------------
        /// <summary>Clears this instance.</summary>
        public void Clear()
        {
            TotalStemPopn = 0;
            TotalLeavesPerShoot = 0;
            PotLeafTipsAppeared = 0;
            PlantTotalNodeNo = 0;
            ProportionBranchMortality = 0;
            ProportionPlantMortality = 0;
            DeltaTipNumber = 0;
            DeltaHaunStage = 0;
            leavesInitialised = false;
            cohortsInitialised = false;
            firstPass = false;
            Height = 0;
            LeafTipsAppeared = 0;
            BranchNumber = 0;
            NextLeafProportion = 0;
            DeltaPlantPopulation = 0;
        }

        // 7. Private methods
        //-------------------------------------------------------------------------------------------

        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        [EventSubscribe("StartOfDay")]
        private void OnStartOfDay(object sender, EventArgs e)
        {
            DeltaPlantPopulation = 0;
            ProportionPlantMortality = 0;
        }

        /// <summary>Called when [do daily initialisation].</summary>
        [EventSubscribe("DoDailyInitialisation")]
        protected void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            if (phenology != null && phenology.OnStartDayOf("Emergence"))
                LeafTipsAppeared = 1.0;
        }

        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        [EventSubscribe("DoPotentialPlantGrowth")]
        private void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            if (cohortsInitialised)
            {
                DeltaHaunStage = 0;
                if (phyllochron.Value() > 0)
                    DeltaHaunStage = thermalTime.Value() / phyllochron.Value();
                if (leavesInitialised)
                {
                    bool AllCohortsInitialised = (leaf.InitialisedCohortNo >= finalLeafNumber.Value());
                    AllLeavesAppeared = (leaf.AppearedCohortNo == leaf.InitialisedCohortNo);
                    bool LastLeafAppearing = ((Math.Truncate(LeafTipsAppeared) + 1) == leaf.InitialisedCohortNo);

                    if ((AllCohortsInitialised) && (LastLeafAppearing))
                    {
                        // fixme - this guard is really a bandaid over some problems in the leaf cohort code
                        // which seems to assume that growth duration is never 0. Can't set NextLeafProp to
                        // 0, because then GrowthDuration will be 0. However it can be close to 0 if final leaf #
                        // happens to have a very small fractional part (e.g. 14.00001).
                        // If your crop is never progressing to flag leaf, this might be why (try reducing epsilon).
                        NextLeafProportion = Math.Max(1e-10, 1 - (leaf.InitialisedCohortNo - finalLeafNumber.Value()));
                    }
                    else
                    {
                        NextLeafProportion = 1.0;
                    }

                    //Increment MainStemNode Number based on phyllochorn and theremal time
                    if (firstPass == true)
                    {
                        firstPass = false;
                        DeltaTipNumber = 0; //Don't increment node number on day of emergence
                    }
                    else
                    {
                        DeltaTipNumber = DeltaHaunStage; //DeltaTipNumber is only positive after emergence whereas deltaHaunstage is positive from germination
                    }

                    PotLeafTipsAppeared += DeltaTipNumber;
                    LeafTipsAppeared = Math.Min(PotLeafTipsAppeared, finalLeafNumber.Value());

                    TimeForAnotherLeaf = PotLeafTipsAppeared >= (leaf.AppearedCohortNo + 1);
                    int LeavesToAppear = (int)(LeafTipsAppeared - (leaf.AppearedCohortNo - (1 - NextLeafProportion)));

                    //Each time main-stem node number increases by one or more initiate the additional cohorts until final leaf number is reached
                    if (TimeForAnotherLeaf && (AllCohortsInitialised == false))
                    {
                        int i = 1;
                        for (i = 1; i <= LeavesToAppear; i++)
                        {
                            CohortToInitialise += 1;
                            InitParams = new CohortInitParams() { };
                            InitParams.Rank = CohortToInitialise;
                            if (AddLeafCohort != null)
                                AddLeafCohort.Invoke(this, InitParams);
                        }
                    }

                    //Each time main-stem node number increases by one appear another cohort until all cohorts have appeared
                    if (TimeForAnotherLeaf && (AllLeavesAppeared == false))
                    {
                        int i = 1;
                        for (i = 1; i <= LeavesToAppear; i++)
                        {
                            TotalStemPopn += branchingRate.Value() * MainStemPopn;
                            BranchNumber += branchingRate.Value();

                            TotalLeavesPerShoot += BranchNumber + 1;
                            DoLeafTipAppearance();
                        }
                    }

                    //Reduce population if there has been plant mortality 
                    if (DeltaPlantPopulation > 0)
                        TotalStemPopn -= DeltaPlantPopulation * TotalStemPopn / plant.Population;

                    //Reduce stem number incase of mortality
                    double PropnMortality = 0;
                    PropnMortality = branchMortality.Value();
                    {
                        double DeltaPopn = Math.Min(PropnMortality * (TotalStemPopn - MainStemPopn), TotalStemPopn - plant.Population);
                        TotalStemPopn -= DeltaPopn;
                        ProportionBranchMortality = PropnMortality;

                    }

                }
            }
        }

        /// <summary>Called when [phase changed].</summary>
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(object sender, PhaseChangedType phaseChange)
        {
            if (phaseChange.StageName == CohortInitialisationStage)
            {
                InitialiseLeafCohorts?.Invoke(this, new EventArgs());
                cohortsInitialised = true;
            }

            if (phaseChange.StageName == LeafInitialisationStage)
            {
                NextLeafProportion = 1.0;
                DoLeafInitilisation();
            }
        }

        /// <summary>Does the actual growth.</summary>
        [EventSubscribe("DoActualPlantPartioning")]
        private void OnDoActualPlantGrowth(object sender, EventArgs e)
        {
            if (plant.IsAlive)
            {
                PlantTotalNodeNo = leaf.PlantAppearedLeafNo;
            }
        }
        /// <summary>Method that calculates parameters for leaf cohort to appear and then calls event so leaf calss can make cohort appear</summary>
        public void DoLeafTipAppearance()
        {
            TipToAppear += 1;
            CohortParams = new ApparingLeafParams() { };
            CohortParams.CohortToAppear = TipToAppear;
            CohortParams.TotalStemPopn = TotalStemPopn;
            CohortParams.CohortAge = 0;
            CohortParams.FinalFraction = NextLeafProportion;
            if (LeafTipAppearance != null)
                LeafTipAppearance.Invoke(this, CohortParams);
        }

        /// <summary> Called on the day of emergence to get the initials leaf cohorts to appear </summary>
        private void DoLeafInitilisation()
        {
            CohortToInitialise = leaf.CohortsAtInitialisation;
            for (int i = 1; i <= leaf.TipsAtEmergence; i++)
            {
                InitParams = new CohortInitParams();
                PotLeafTipsAppeared += 1;
                CohortToInitialise += 1;
                InitParams.Rank = CohortToInitialise;
                AddLeafCohort?.Invoke(this, InitParams);
                DoLeafTipAppearance();
                leavesInitialised = true;
                firstPass = true;
            }
        }

        /// <summary>Updates the height.</summary>
        public void UpdateHeight()
        {
            Height = heightModel.Value();
        }
        /// <summary>Resets the stem popn.</summary>
        public void ResetStemPopn()
        {
            TotalStemPopn = MainStemPopn;
        }

        /// <summary>Called when [simulation commencing].</summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Clear();
        }

        /// <summary>Called when crop is ending</summary>
        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs e)
        {
            Clear();
            CohortToInitialise = 0;
            TipToAppear = 0;
            PotLeafTipsAppeared = 0;
            ResetStemPopn();
        }

        /// <summary>Called when crop is ending</summary>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowingParameters Sow)
        {
            if (Sow.Plant == plant)
            {
                Clear();
                if (Sow.MaxCover <= 0.0)
                    throw new Exception("MaxCover must exceed zero in a Sow event.");
                PrimaryBudNo = Sow.BudNumber;
                TotalStemPopn = MainStemPopn;
            }
        }

        /// <summary>Called when crop recieves a remove biomass event from manager</summary>
        public void DoThin(double ProportionRemoved)
        {
            plant.Population *= (1 - ProportionRemoved);
            TotalStemPopn *= (1 - ProportionRemoved);
            leaf.DoThin(ProportionRemoved);
        }

        /// <summary> Removes nodes from main-stem in defoliation event  </summary>
        public void DoNodeRemoval(int NodesToRemove)
        {
            //Remove nodes from Structure properties
            LeafTipsAppeared = Math.Max(LeafTipsAppeared - NodesToRemove, 0);
            PotLeafTipsAppeared = Math.Max(PotLeafTipsAppeared - NodesToRemove, 0);

            //Remove corresponding cohorts from leaf
            int NodesStillToRemove = Math.Min(NodesToRemove + leaf.ApicalCohortNo, leaf.InitialisedCohortNo);
            while (NodesStillToRemove > 0)
            {
                TipToAppear -= 1;
                CohortToInitialise -= 1;
                leaf.RemoveHighestLeaf();
                NodesStillToRemove -= 1;
            }
            //TipToAppear = Math.Max(TipToAppear + leaf.CohortsAtInitialisation, 1);
            CohortToInitialise = Math.Max(CohortToInitialise, 1);
            if (CohortToInitialise == LeafTipsAppeared) // If leaf appearance had reached final leaf number need to add another cohort back to get things moving again.
                CohortToInitialise += 1;
            InitParams = new CohortInitParams() { };
            InitParams.Rank = CohortToInitialise;
            if (AddLeafCohort != null)
                AddLeafCohort.Invoke(this, InitParams);
            //Reinitiate apical cohorts ready for regrowth
            if (leaf.InitialisedCohortNo == 0) //If all nodes have been removed initalise again
            {
                leaf.Reset();
                InitialiseLeafCohorts.Invoke(this, new EventArgs());
                DoLeafInitilisation();
            }
        }
    }


}



