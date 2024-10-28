using System;
using Models.Core;
using Models.Functions;
using Newtonsoft.Json;

namespace Models.PMF.Phen
{
    /// <summary>
    /// This phase goes from the specified start stage to the specified end stage
    /// and extends from the end of the previous phase until the CompletionNodeNumber
    /// is achieved. The duration of this phase is determined by leaf appearance rate
    /// and the CompletionNodeNumber target
    /// </summary>
    [Serializable]
    [Description("This phase extends from the end of the previous phase until the Completion Leaf Number is achieved.  The duration of this phase is determined by leaf appearance rate and the completion leaf number target.")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class NodeNumberPhase : Model, IPhase
    {
        // 1. Links
        //----------------------------------------------------------------------------------------------------------------

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction LeafTipNumber = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction CompletionNodeNumber = null;

        //2. Private and protected fields
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>The cohort no at start</summary>
        private double NodeNoAtStart;
        /// <summary>The first</summary>
        private bool First = true;
        /// <summary>The fraction complete yesterday</summary>
        private double FractionCompleteYesterday = 0;


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
                double F = (LeafTipNumber.Value() - NodeNoAtStart) / (CompletionNodeNumber.Value() - NodeNoAtStart);
                if (F < 0) F = 0;
                if (F > 1) F = 1;
                return Math.Max(F, FractionCompleteYesterday); //Set to maximum of FractionCompleteYesterday so on days where final leaf number increases phenological stage is not wound back.
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
                NodeNoAtStart = LeafTipNumber.Value();
                First = false;
            }

            FractionCompleteYesterday = FractionComplete;

            if (LeafTipNumber.Value() >= CompletionNodeNumber.Value())
            {
                proceedToNextPhase = true;
                propOfDayToUse = 0.00001; //assumes we use most of the Tt today to get to specified node number.  Should be calculated as a function of the phyllochron
            }

            return proceedToNextPhase;
        }

        /// <summary>Reset phase</summary>
        public void ResetPhase()
        {
            NodeNoAtStart = 0;
            FractionCompleteYesterday = 0;
            First = true;
        }

        //7. Private methods
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Called when [simulation commencing].</summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            ResetPhase();
        }
    }
}
