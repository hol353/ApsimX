using System;
using Models.Core;
using Models.PMF.Organs;
using Models.PMF.Struct;
using Newtonsoft.Json;

namespace Models.PMF.Phen
{
    /// <summary>
    /// This phase goes from the specified start stage to the specified end stage,
    /// which occurs when all leaves have fully senesced.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class LeafDeathPhase : Model, IPhase
    {
        // 1. Links
        //----------------------------------------------------------------------------------------------------------------

        [Link]
        private Leaf leaf = null;

        [Link]
        private Structure structure = null;

        //2. Private and protected fields
        //-----------------------------------------------------------------------------------------------------------------
        private double DeadNodeNoAtStart = 0;
        private bool First = true;


        //5. Public properties
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>The start</summary>
        [Description("Start")]
        public string Start { get; set; }

        /// <summary>The end</summary>
        [Description("End")]
        public string End { get; set; }

        /// <summary>Is the phase emerged from the ground?</summary>
        [Description("Is the phase emerged?")]
        public bool IsEmerged { get; set; } = true;

        /// <summary>Return a fraction of phase complete.</summary>
        [JsonIgnore]
        public double FractionComplete
        {
            get
            {
                double F = (leaf.DeadCohortNo - DeadNodeNoAtStart) / (structure.finalLeafNumber.Value() - DeadNodeNoAtStart);
                if (F < 0) F = 0;
                if (F > 1) F = 1;
                return F;
            }
        }

        //6. Public methods
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Do our timestep development</summary>
        public bool DoTimeStep(ref double propOfDayToUse)
        {
            bool proceedToNextPhase = false;

            if (First)
            {
                DeadNodeNoAtStart = leaf.DeadCohortNo;
                First = false;
            }

            if ((leaf.DeadCohortNo >= structure.finalLeafNumber.Value()) || (leaf.CohortsInitialised == false))
            {
                proceedToNextPhase = true;
                propOfDayToUse = 0.00001;
            }
            return proceedToNextPhase;
        }

        /// <summary>Resets the phase.</summary>
        public void ResetPhase()
        {
            DeadNodeNoAtStart = 0;
            First = true;
        }

        /// <summary>Called when [simulation commencing].</summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            ResetPhase();
        }

    }
}
