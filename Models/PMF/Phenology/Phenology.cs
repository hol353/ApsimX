using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Models.Core;
using Models.Functions;
using Models.PMF.Interfaces;
using Newtonsoft.Json;

namespace Models.PMF.Phen
{
    /// <summary>
    /// The phenological development is simulated as the progression through a 
    /// series of developmental phases, each bound by distinct growth stage. 
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Plant))]
    public class Phenology : Model, IPhenology
    {

        ///1. Links
        ///------------------------------------------------------------------------------------------------

        [Link]
        private Plant plant = null;

        /// <summary>The thermal time</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction thermalTime = null;

        [Link(IsOptional = true)]
        private ZadokPMFWheat zadok = null; // This is here so that manager scripts can access it easily.

        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        private Age age = null;
        ///2. Private And Protected Fields
        /// -------------------------------------------------------------------------------------------------

        /// <summary>The phases</summary>
        private List<IPhase> phases = new List<IPhase>();

        /// <summary>The current phase index</summary>
        private int currentPhaseIndex;

        /// <summary>This lists all the stages that are pased on this day</summary>
        private List<string> stagesPassedToday = new List<string>();

        
        ///4. Public Events And Enums
        /// -------------------------------------------------------------------------------------------------

        /// <summary>Occurs when [phase changed].</summary>
        public event EventHandler<PhaseChangedType> PhaseChanged;

        /// <summary>Occurs when phase is set externally.</summary>
        public event EventHandler<StageSetType> StageWasReset;

        /// <summary>Occurs when emergence phase completed</summary>
        public event EventHandler PlantEmerged;

        /// <summary>Occurs when daily phenology timestep completed</summary>
        public event EventHandler PostPhenology;


        ///5. Public Properties
        /// --------------------------------------------------------------------------------------------------

        /// <summary>List of stages in phenology</summary>
        [JsonIgnore]
        public List<string> StageNames 
        {
            get
            {
                List<string> stages = new List<string>();
                stages.Add(phases[0].Start.ToString());
                foreach (IPhase p in  phases)
                {
                    stages.Add(p.End.ToString());
                }
            return stages;
            }
        }

        /// <summary>List of numerical stage codes</summary>
        [JsonIgnore]
        public List<int> StageCodes
        {
            get
            {
                List<int> stages = new List<int>();
                int current = 0;
                stages.Add(current);
                foreach (IPhase p in phases)
                {
                    current += 1;
                    stages.Add(current);
                }
                return stages;
            }
        }

        /// <summary>The Thermal time accumulated tt</summary>
        [JsonIgnore]
        public double AccumulatedTT { get; set; }

        /// <summary>The Thermal time accumulated tt following emergence</summary>
        [JsonIgnore]
        public double AccumulatedEmergedTT { get; set; }

        /// <summary>The emerged</summary>
        [JsonIgnore]
        public bool Emerged { get { return CurrentPhase.IsEmerged; } }

        /// <summary>A one based stage number.</summary>
        [JsonIgnore]
        public double Stage { get; set; }

        /// <summary>This property is used to retrieve or set the current phase name.</summary>
        [JsonIgnore]
        public string CurrentPhaseName
        {
            get
            {
                if (CurrentPhase == null)
                    return "";
                else
                    return CurrentPhase.Name;
            }
        }

        /// <summary>Return current stage name.</summary>
        [JsonIgnore]
        public string CurrentStageName
        {
            get
            {
                if (OnStartDayOf(CurrentPhase.Start))
                    return CurrentPhase.Start;
                else
                    return "";
            }
        }

        /// <summary>Gets the fraction in current phase.</summary>
        public double FractionInCurrentPhase
        {
            get
            {
                return CurrentPhase.FractionComplete;
            }
        }

        /// <summary>A utility property to return the current phase.</summary>
        [JsonIgnore]
        public IPhase CurrentPhase
        {
            get
            {
                if (phases == null || currentPhaseIndex >= phases.Count)
                    return null;
                else
                    return phases[currentPhaseIndex];
            }
        }

        /// <summary>Gets the current zadok stage number. Used in manager scripts.</summary>
        public double Zadok { get { return zadok?.Stage ?? 0; } }

        /// <summary>flag set to true is a phase does a stage set that increments currentPhaseNumber</summary>
        private bool currentPhaseNumberIncrementedByPhaseTimeStep { get; set; } = false;

        ///6. Public methods
        /// -----------------------------------------------------------------------------------------------------------

        /// <summary>Look for a particular phase and return it's index or -1 if not found.</summary>
        public int IndexFromPhaseName(string name)
        {
            for (int phaseIndex = 0; phaseIndex < phases.Count; phaseIndex++)
                if (String.Equals(phases[phaseIndex].Name, name, StringComparison.OrdinalIgnoreCase))
                    return phaseIndex;
            return -1;
        }

        /// <summary>Look for a particular stage and return it's index or -1 if not found.</summary>
        public int StartStagePhaseIndex(string stageName)
        {
            int startPhaseIndex = -1;
            int i = 0;
            while (startPhaseIndex == -1 && i < phases.Count())
            {
                if (phases[i].Start == stageName)
                    startPhaseIndex = i;
                i += 1;
            }
            if (startPhaseIndex == -1)
                throw new Exception("Cannot find phase beginning with: " + stageName);
            return startPhaseIndex;
        }

        /// <summary>Look for a particular stage and return it's index or -1 if not found.</summary>
        public int EndStagePhaseIndex(string stageName)
        {
            int endPhaseIndex = -1;
            int i = 0;
            while (endPhaseIndex == -1 && i < phases.Count())
            {
                if (phases[i].End == stageName)
                    endPhaseIndex = i;
                i += 1;
            }
            if (endPhaseIndex == -1)
                throw new Exception("Cannot find phase ending with: " + stageName);
            return endPhaseIndex;
        }

        /// <summary>Called to set the phenology to the last stage.</summary>
        public void SetToEndStage()
        {
            SetToStage((double)(phases.Count));
        }

        /// <summary>A function that resets phenology to a specified stage</summary>
        public void SetToStage(double newStage)
        {
            currentPhaseNumberIncrementedByPhaseTimeStep = true;
            int oldPhaseIndex = IndexFromPhaseName(CurrentPhase.Name);
            stagesPassedToday.Clear();

            if (newStage <= 0)
                throw new Exception(this + "Must pass positive stage to set to");
            if (newStage > phases.Count() + 1)
                throw new Exception(this + " Trying to set to non-existant stage");

            currentPhaseIndex = Convert.ToInt32(Math.Floor(newStage), CultureInfo.InvariantCulture) - 1;

            if (newStage < Stage)
            {
                //Make a list of phases to rewind
                List<IPhase> phasesToRewind = new List<IPhase>();
                foreach (IPhase phase in phases)
                {
                    if ((IndexFromPhaseName(phase.Name) >= currentPhaseIndex) && (IndexFromPhaseName(phase.Name) <= oldPhaseIndex))
                        phasesToRewind.Add(phase);
                }

                foreach (IPhase phase in phasesToRewind)
                {
                    if (!(phase is IPhaseWithTarget) && !(phase is GotoPhase) && !(phase is EndPhase) && !(phase is PhotoperiodPhase) && !(phase is LeafDeathPhase) && !(phase is DAWSPhase) && !(phase is StartPhase) && !(phase is GrazeAndRewind) && !(phase is StartGrowthPhase))
                    { throw new Exception("Can not rewind over phase of type " + phase.GetType()); }
                    if (phase is IPhaseWithTarget)
                    {
                        IPhaseWithTarget rewindingPhase = phase as IPhaseWithTarget;
                        AccumulatedTT -= rewindingPhase.ProgressThroughPhase;
                        AccumulatedEmergedTT -= rewindingPhase.ProgressThroughPhase;
                        phase.ResetPhase();
                    }
                    else
                        phase.ResetPhase();
                }
                AccumulatedTT = Math.Max(0, AccumulatedTT);
                AccumulatedEmergedTT = Math.Max(0, AccumulatedEmergedTT);

            }
            else
            {
                //Make a list of phases to fast forward
                List<IPhase> phasesToFastForward = new List<IPhase>();
                foreach (IPhase phase in phases)
                {
                    if (IndexFromPhaseName(phase.Name)>=oldPhaseIndex) //If the phase has not yet passed 
                    {
                        if (newStage == phases.Count) //If winding to the end add all phases
                            phasesToFastForward.Add(phase);
                        else if (IndexFromPhaseName(phase.Name) < (newStage - 1))// Inf only winding part way throug only add the relevent stages
                            phasesToFastForward.Add(phase);
                    }
                }
                foreach (IPhase phase in phasesToFastForward)
                {
                    if (phase is EndPhase)
                    {
                        stagesPassedToday.Add(phase.Start); //Fixme.  This is a pretty ordinary bit of programming to get around the fact we use a phenological stage to match observed values. We should change this so plant has a harvest tag to match on.
                    }
                    stagesPassedToday.Add(phase.End);
                    if (phase is IPhaseWithTarget)
                    {
                        IPhaseWithTarget PhaseSkipped = phase as IPhaseWithTarget;
                        AccumulatedTT += (PhaseSkipped.Target - PhaseSkipped.ProgressThroughPhase);
                        if (phase.IsEmerged==false) 
                        {
                            PlantEmerged?.Invoke(this, new EventArgs());
                        }
                        else
                        {
                            AccumulatedEmergedTT += (PhaseSkipped.Target - PhaseSkipped.ProgressThroughPhase);
                            PhaseSkipped.ProgressThroughPhase = PhaseSkipped.Target;
                        }
                    }
                    
                    PhaseChangedType PhaseChangedData = new PhaseChangedType();
                    PhaseChangedData.StageName = phase.End;
                    PhaseChanged?.Invoke(plant, PhaseChangedData);
                }
            }

            if (phases[currentPhaseIndex] is IPhaseWithTarget)
            {
                IPhaseWithTarget currentPhase = (phases[currentPhaseIndex] as IPhaseWithTarget);
                currentPhase.ProgressThroughPhase = currentPhase.Target * (newStage - currentPhaseIndex - 1);

                if (currentPhase.ProgressThroughPhase == 0)
                    stagesPassedToday.Add(currentPhase.Start);
            }
            if ((phases[currentPhaseIndex] is PhotoperiodPhase) || (phases[currentPhaseIndex] is LeafDeathPhase))
                stagesPassedToday.Add(phases[currentPhaseIndex].Start);

            StageSetType StageSetData = new StageSetType();
            StageSetData.StageNumber = newStage;
            StageWasReset?.Invoke(this, StageSetData);
        }

        /// <summary>Allows setting of age if phenology has an age child</summary>
        public void SetAge(double newAge)
        {
            if (age != null)
            {
                age.Years = (int)newAge;
                age.FractionComplete = newAge - age.Years;
            }
        }
        
        /// <summary> A utility function to return true if the simulation is on the first day of the specified stage. </summary>
        public bool OnStartDayOf(String stageName)
        {
            if (stagesPassedToday.Contains(stageName))
                return true;
            else
                return false;
        }

        /// <summary> A utility function to return true if the simulation is currently in the specified phase. </summary>
        public bool InPhase(String phaseName)
        {
            return String.Equals(CurrentPhase.Name, phaseName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary> A utility function to return true if the simulation is currently between the specified start and end stages. </summary>
        public bool Between(int startPhaseIndex, int endPhaseIndex)
        {
            if (phases == null)
                return false;

            if (startPhaseIndex > endPhaseIndex)
                throw new Exception("Start phase " + startPhaseIndex + " is after phase " + endPhaseIndex);

            return currentPhaseIndex >= startPhaseIndex && currentPhaseIndex <= endPhaseIndex;
        }

        /// <summary> A utility function to return true if the simulation is currently betweenthe specified start and end stages. </summary>
        public bool Between(String start, String end)
        {
            if (phases == null)
                return false;

            int startPhaseIndex = -1;
            int endPhaseIndex = -1;
            int i = 0;
            while (endPhaseIndex == -1 && i < phases.Count())
            {
                if (phases[i].Start == start)
                    startPhaseIndex = i;
                if (phases[i].End == end)
                    endPhaseIndex = i;
                i += 1;
            }

            if (startPhaseIndex == -1)
                throw new Exception("Cannot find phase: " + start);
            if (endPhaseIndex == -1)
                throw new Exception("Cannot find phase: " + end);
            if (startPhaseIndex > endPhaseIndex)
                throw new Exception("Start phase " + start + " is after phase " + end);

            return currentPhaseIndex >= startPhaseIndex && currentPhaseIndex <= endPhaseIndex;
        }

        /// <summary> A utility function to return true if the simulation is at or past the specified startstage.</summary>
        public bool Beyond(String start)
        {
            if (currentPhaseIndex >= phases.IndexOf(PhaseStartingWith(start)))
                return true;
            else
                return false;
        }
        /// <summary> A utility function to return true if the simulation is at or past the specified startstage.</summary>
        public bool BeyondPhase(int phaseIndex) => currentPhaseIndex > phaseIndex;

        /// <summary> A utility function to return true if the simulation is before the specified phaseIndex.</summary>
        public bool BeforePhase(int phaseIndex) => currentPhaseIndex < phaseIndex;

        /// <summary>A utility function to return the phenological phase that starts with the specified start stage name.</summary>
        public IPhase PhaseStartingWith(String start)
        {
            foreach (IPhase P in phases)
                if (P.Start == start)
                    return P;
            throw new Exception("Unable to find phase starting with " + start);
        }


        /// <summary>
        /// Resets the Vrn expression parameters for the CAMP model
        /// </summary>
        /// <param name="overRideFLNParams"></param>
        public void ResetCampVernParams(FinalLeafNumberSet overRideFLNParams)
        {
            CAMP camp = this.FindChild("CAMP") as CAMP;
            camp.ResetVernParams(overRideFLNParams);
        }

        // 7. Private methods
        // -----------------------------------------------------------------------------------------------------------
        //

        /// <summary>
        /// Refreshes the list of phases.
        /// </summary>
        private void RefreshPhases()
        {
            if (phases == null)
                phases = new List<IPhase>();
            else
                phases.Clear();

            foreach (IPhase phase in this.FindAllChildren<IPhase>())
                phases.Add(phase);
        }
        /// <summary>Called when model has been created.</summary>
        public override void OnCreated()
        {
            base.OnCreated();
            RefreshPhases();
        }

        /// <summary>
        /// Force emergence on the date called if emergence has not occurred already
        /// </summary>
        /// <param name="emergenceDate">Emergence date (dd-mmm)</param>
        public void SetEmergenceDate(string emergenceDate)
        {
            foreach (EmergingPhase ep in this.FindAllDescendants<EmergingPhase>())
                ep.EmergenceDate = emergenceDate;
            SetGerminationDate(plant.SowingDate.ToString("d-MMM", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Force germination on the date called if germination has not occurred already
        /// </summary>
        /// <param name="germinationDate">Germination date (dd-mmm).</param>
        public void SetGerminationDate(string germinationDate)
        {
            foreach (GerminatingPhase gp in this.FindAllDescendants<GerminatingPhase>())
                gp.GerminationDate = germinationDate;
        }

        /// <summary>
        /// Returns a DataTable with each Phase listed
        /// </summary>
        public DataTable GetPhaseTable()
        {
            DataTable phaseTable = new DataTable();
            phaseTable.Columns.Add("Phase Number", typeof(int));
            phaseTable.Columns.Add("Phase Name", typeof(string));
            phaseTable.Columns.Add("Initial Stage", typeof(string));
            phaseTable.Columns.Add("Final Stage", typeof(string));

            int n = 1;
            foreach (IPhase child in FindAllChildren<IPhase>())
            {
                DataRow row = phaseTable.NewRow();
                row[0] = n;
                row[1] = child.Name;
                row[2] = (child as IPhase).Start;
                row[3] = (child as IPhase).End;
                phaseTable.Rows.Add(row);
                n++;
            }
            return phaseTable;
        }

        /// <summary>Called when [simulation commencing].</summary>
        [EventSubscribe("Commencing")]
        private void OnCommencing(object sender, EventArgs e)
        {
            RefreshPhases();
            Clear();
        }

        /// <summary>Called when crop is ending</summary>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowingParameters data)
        {
            Clear();
            stagesPassedToday.Add(phases[0].Start);
        }

        /// <summary>Called by sequencer to perform phenology.</summary>
        [EventSubscribe("DoPhenology")]
        private void OnDoPhenology(object sender, EventArgs e)
        {
            if (PlantIsAlive)
            {
                if (thermalTime.Value() < 0)
                    throw new Exception("Negative Thermal Time, check the set up of the ThermalTime Function in" + this);

                // Calculate progression through current phase
                currentPhaseNumberIncrementedByPhaseTimeStep = false;
                double propOfDayToUse = 1;
                bool incrementPhase = CurrentPhase.DoTimeStep(ref propOfDayToUse);

                while (incrementPhase)
                {
                    stagesPassedToday.Add(CurrentPhase.End);
                    if (currentPhaseNumberIncrementedByPhaseTimeStep == false)
                    { //Phase index does not need to be incrementd if prior phase was go to as it will have set the correct phase index in the setStage call it makes
                        currentPhaseIndex = currentPhaseIndex + 1;
                    }
                    currentPhaseNumberIncrementedByPhaseTimeStep = false;

                    if (currentPhaseIndex >= phases.Count)
                        throw new Exception("Cannot transition to the next phase. No more phases exist");

                    if (CurrentPhase.IsEmerged)
                    {
                        PlantEmerged?.Invoke(this, new EventArgs());
                    }

                    PhaseChangedType PhaseChangedData = new PhaseChangedType();
                    PhaseChangedData.StageName = CurrentPhase.Start;
                    PhaseChanged?.Invoke(plant, PhaseChangedData);

                    if ((CurrentPhase is GotoPhase) || (CurrentPhase is GrazeAndRewind)) //If new phase is one that sets a new stage, do them now
                        CurrentPhase.DoTimeStep(ref propOfDayToUse);

                    incrementPhase = CurrentPhase.DoTimeStep(ref propOfDayToUse);
                }

                AccumulatedTT += thermalTime.Value();
                if (Emerged)
                    AccumulatedEmergedTT += thermalTime.Value();

                Stage = (currentPhaseIndex + 1) + CurrentPhase.FractionComplete;

                if (plant != null && plant.IsAlive && PostPhenology != null)
                    PostPhenology.Invoke(this, new EventArgs());
            }
        }

        /// <summary>Called when crop is being prunned.</summary>
        [EventSubscribe("Pruning")]
        private void OnPruning(object sender, EventArgs e)
        {
            
        }

        /// <summary>Called when crop is ending</summary>
        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs e)
        {
            Clear();
        }

        /// <summary>Called at the start of each day</summary>
        [EventSubscribe("StartOfDay")]
        private void OnStartOfDay(object sender, EventArgs e)
        {
            stagesPassedToday.Clear();
            //reset StagesPassedToday to zero to restart count for the new day
        }

        /// <summary> /// A helper property that checks the parent plant (old or new) to see if it is alive. /// </summary>
        private bool PlantIsAlive
        {
            get
            {
                if (plant != null && plant.IsAlive)
                    return true;
                return false;
            }
        }

        private void Clear()
        {
            Stage = 1;
            AccumulatedTT = 0;
            AccumulatedEmergedTT = 0;
            stagesPassedToday.Clear();
            currentPhaseIndex = 0;
            foreach (IPhase phase in phases)
                phase.ResetPhase();
        }
    }
}
