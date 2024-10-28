using System;
using System.Collections.Generic;
using System.Linq;
using Models.Core;
using Models.PMF.Phen;

namespace Models.Functions
{
    /// <summary>
    /// A function that accumulates values from child functions
    /// </summary>
    [Serializable]
    [Description("Adds the value of all children functions to the previous day's accumulation between start and end phases")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class AccumulateAtEvent : Model, IFunction
    {
        ///Links
        /// -----------------------------------------------------------------------------------------------------------

        /// <summary>Link to an event service.</summary>
        [Link]
        private IEvent events = null;

        /// <summary>The phenology</summary>
        [Link]
        Phenology phenology = null;

        /// Private fields
        /// -----------------------------------------------------------------------------------------------------------

        private double accumulatedValue = 0;

        private IEnumerable<IFunction> childFunctions;

        private int startStageIndex;

        private int endStageIndex;

        ///Public Properties
        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>The start stage name</summary>
        [Description("Stage name to start accumulation")]
        [Display(Type = DisplayType.CropStageName)]
        public string StartStageName { get; set; }

        /// <summary>The end stage name</summary>
        [Description("Stage name to stop accumulation")]
        [Display(Type = DisplayType.CropStageName)]
        public string EndStageName { get; set; }

        /// <summary>The end stage name</summary>
        [Description("Event name to accumulate")]
        public string AccumulateEventName { get; set; }

        ///6. Public methods
        /// -----------------------------------------------------------------------------------------------------------
        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            return accumulatedValue;
        }

        ///7. Private methods
        /// -----------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Connect event handlers.
        /// </summary>
        /// <param name="sender">Sender object..</param>
        /// <param name="args">Event data.</param>
        [EventSubscribe("SubscribeToEvents")]
        private void OnConnectToEvents(object sender, EventArgs args)
        {
            events.Subscribe(AccumulateEventName, OnCalcEvent);
        }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            accumulatedValue = 0;

            startStageIndex = phenology.StartStagePhaseIndex(StartStageName);
            endStageIndex = phenology.EndStagePhaseIndex(EndStageName);
        }

        /// <summary>Called by Plant.cs when phenology routines are complete.</summary>
        /// <param name="sender">Plant.cs</param>
        /// <param name="e">Event arguments</param>
        private void OnCalcEvent(object sender, EventArgs e)
        {
            if (childFunctions == null)
                childFunctions = FindAllChildren<IFunction>().ToList();

            if (phenology.Between(startStageIndex, endStageIndex))
            {
                double DailyIncrement = 0.0;
                foreach (IFunction function in childFunctions)
                {
                    DailyIncrement += function.Value();
                }

                accumulatedValue += DailyIncrement;
            }
        }

        /// <summary>Called when [EndCrop].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs e)
        {
            accumulatedValue = 0;
        }
    }
}
