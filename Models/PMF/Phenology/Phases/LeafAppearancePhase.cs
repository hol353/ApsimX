using System;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;
using Newtonsoft.Json;

namespace Models.PMF.Phen
{
    /// <summary>
    /// This phase goes from the specified start stage to the specified end stage and 
    /// it continues until the final main-stem leaf has finished expansion.
    /// The duration of this phase is determined by leaf appearance rate (Structure.Phyllochron)
    /// and the number of leaves produced on the mainstem (Structure.FinalLeafNumber). 
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Phenology))]
    public class LeafAppearancePhase : Model, IPhase
    {
        // 1. Links
        //----------------------------------------------------------------------------------------------------------------

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction FinalLeafNumber = null;

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction LeafNumber = null;

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction FullyExpandedLeafNo = null;

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction InitialisedLeafNumber = null;

        //2. Private and protected fields
        //-----------------------------------------------------------------------------------------------------------------

        private double LeafNoAtStart;
        private bool First = true;
        private double FractionCompleteYesterday = 0;
        private double TargetLeafForCompletion = 0;

        //5. Public properties
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>The start</summary>
        [Description("Start")]
        public string Start { get; set; }

        /// <summary>The end</summary>
        [Models.Core.Description("End")]
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
                double F = 0;
                F = (LeafNumber.Value() - LeafNoAtStart) / TargetLeafForCompletion;
                F = MathUtilities.Bound(F, 0, 1);
                return Math.Max(F, FractionCompleteYesterday); //Set to maximum of FractionCompleteYesterday so on days where final leaf number increases phenological stage is not wound back.
            }
        }


        //6. Public method
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Do our timestep development</summary>
        public bool DoTimeStep(ref double propOfDayToUse)
        {
            bool proceedToNextPhase = false;

            if (First)
            {
                LeafNoAtStart = LeafNumber.Value();
                TargetLeafForCompletion = FinalLeafNumber.Value() - LeafNoAtStart;
                First = false;
            }

            FractionCompleteYesterday = FractionComplete;

            //if (leaf.ExpandedCohortNo >= (leaf.InitialisedCohortNo))
            if (FullyExpandedLeafNo.Value() >= InitialisedLeafNumber.Value())
            {
                proceedToNextPhase = true;
                propOfDayToUse = 0.00001;  //assumes we use most of the Tt today to get to final leaf.  Should be calculated as a function of the phyllochron
            }

            return proceedToNextPhase;
        }

        /// <summary>Reset phase</summary>
        public void ResetPhase()
        {
            LeafNoAtStart = 0;
            FractionCompleteYesterday = 0;
            TargetLeafForCompletion = 0;
            First = true;
        }

        //7. Private methode
        //-----------------------------------------------------------------------------------------------------------------

        /// <summary>Called when [simulation commencing].</summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e) { ResetPhase(); }
    }
}
