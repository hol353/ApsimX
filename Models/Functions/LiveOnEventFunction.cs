using System;
using Models.Core;
using Models.PMF.Phen;

namespace Models.Functions
{
    /// <summary>
    /// Returns the live value of a child function depending on whether an event has occurred.
    /// Similar to OnEventFunction, except that OnEventFunction only updates its value
    /// when phase changes. This function will always return the live value of the appropriate child.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class LiveOnEventFunction : Model, IFunction
    {
        /// <summary>The set event</summary>
        [Description("The event that triggers change from pre to post event value")]
        public string SetEvent { get; set; }

        /// <summary>The re set event</summary>
        [Description("(optional) The event resets to pre event value")]
        public string ReSetEvent { get; set; }

        /// <summary>
        /// When true, we return the pre-event value.
        /// When false, we return the post-event value.
        /// </summary>
        private bool preEvent;

        /// <summary>The pre event value</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction PreEventValue = null;

        /// <summary>The post event value</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction PostEventValue = null;

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            preEvent = true;
        }

        /// <summary>Called when [phase changed].</summary>
        /// <param name="phaseChange">The phase change.</param>
        /// <param name="sender">Sender plant.</param>
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(object sender, PhaseChangedType phaseChange)
        {
            if (phaseChange.StageName == SetEvent)
                preEvent = false;

            if (phaseChange.StageName == ReSetEvent)
                preEvent = true;
        }

        /// <summary>Called when crop is being harvested.</summary>
        [EventSubscribe("Cutting")]
        private void OnHarvesting(object sender, EventArgs e)
        {
            preEvent = true;
        }

        /// <summary>Gets the value.</summary>
        public double Value(int arrayIndex = -1)
        {
            return preEvent ? PreEventValue.Value() : PostEventValue.Value();
        }
    }
}
