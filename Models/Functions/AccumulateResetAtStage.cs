using System;
using System.Collections.Generic;
using System.Linq;
using Models.Core;
using Models.PMF.Phen;

namespace Models.Functions
{
    /// <summary>
    /// A function that accumulates values from child functions, and reset to zero each time the specisified stage is passed.
    /// </summary>
    [Serializable]
    [Description("Adds the value of all children functions to the previous day's accumulation and reset to zero each time the specisified stage is passed")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class AccumulateResetAtStage : Model, IFunction
    {
        /// Private class members
        /// -----------------------------------------------------------------------------------------------------------

        private double AccumulatedValue = 0;

        private IEnumerable<IFunction> ChildFunctions;

        ///Public Properties
        /// -----------------------------------------------------------------------------------------------------------

        /// <summary>The reset stage name</summary>
        [Description("(optional) Stage name to reset accumulation")]
        public string ResetStageName { get; set; }


        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            AccumulatedValue = 0;
        }

        /// <summary>Called by Plant.cs when phenology routines are complete.</summary>
        /// <param name="sender">Plant.cs</param>
        /// <param name="e">Event arguments</param>
        [EventSubscribe("PostPhenology")]
        private void PostPhenology(object sender, EventArgs e)
        {
            if (ChildFunctions == null)
                ChildFunctions = FindAllChildren<IFunction>().ToList();

            double DailyIncrement = 0.0;
            foreach (IFunction function in ChildFunctions)
            {
                DailyIncrement += function.Value();
            }

            AccumulatedValue += DailyIncrement;
        }

        /// <summary>Called when [phase changed].</summary>
        /// <param name="phaseChange">The phase change.</param>
        /// <param name="sender">Sender plant.</param>
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(object sender, PhaseChangedType phaseChange)
        {
            if (phaseChange.StageName == ResetStageName)
                AccumulatedValue = 0.0;
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            return AccumulatedValue;
        }

        /// <summary>Called when [EndCrop].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs e)
        {
            AccumulatedValue = 0;
        }
    }
}
