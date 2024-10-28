using System;
using System.Collections.Generic;
using System.Linq;
using Models.Core;
using Models.PMF.Phen;

namespace Models.Functions
{
    /// <summary>
    /// This function has a value between the specified start and end phases.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("Returns the value of it child function to the PhaseLookup parent function if current phenology is between Start and end stages specified.")]
    public class PhaseLookupValue : Model, IFunction
    {
        /// <summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;

        /// <summary>The child functions</summary>
        private IEnumerable<IFunction> ChildFunctions;

        private int startStageIndex;

        private int endStageIndex;

        /// <summary>The start</summary>
        [Description("Start")]
        [Display(Type = DisplayType.CropStageName)]
        public string Start { get; set; }

        /// <summary>The end</summary>
        [Description("End")]
        [Display(Type = DisplayType.CropStageName)]
        public string End { get; set; }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        /// <exception cref="System.Exception">
        /// Phase start name not set: + Name
        /// or
        /// Phase end name not set: + Name
        /// </exception>
        public double Value(int arrayIndex = -1)
        {
            if (ChildFunctions == null)
                ChildFunctions = FindAllChildren<IFunction>().ToList();

            if (Start == "")
                throw new Exception("Phase start name not set:" + Name);
            if (End == "")
                throw new Exception("Phase end name not set:" + Name);

            if (Phenology != null && Phenology.Between(startStageIndex, endStageIndex) && ChildFunctions.Count() > 0)
            {
                IFunction Lookup = ChildFunctions.First() as IFunction;
                return Lookup.Value(arrayIndex);
            }
            else
                return 0.0;
        }

        /// <summary>Gets a value indicating whether [in phase].</summary>
        /// <value><c>true</c> if [in phase]; otherwise, <c>false</c>.</value>
        public bool InPhase
        {
            get
            {
                return Phenology.Between(startStageIndex, endStageIndex);
            }
        }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            startStageIndex = Phenology.StartStagePhaseIndex(Start);
            endStageIndex = Phenology.EndStagePhaseIndex(End);
        }
    }
}