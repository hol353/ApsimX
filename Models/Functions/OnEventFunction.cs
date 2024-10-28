using System;
using Models.Core;
using Models.PMF.Phen;

namespace Models.Functions
{
    /// <summary>
    /// Returns the a value depending on whether an event has occurred.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class OnEventFunction : Model, IFunction
    {
        /// <summary>The _ value</summary>
        private double _Value = 0;

        /// <summary>The set event</summary>
        [Description("The event that triggers change from pre to post event value")]
        public string SetEvent { get; set; }

        /// <summary>The re set event</summary>
        [Description("(optional) The event resets to pre event value")]
        public string ReSetEvent { get; set; }


        /// <summary>The pre event value</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction PreEventValue = null;
        /// <summary>The post event value</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction PostEventValue = null;

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Sowing")]
        private void OnSowing(object sender, EventArgs e)
        {
            _Value = PreEventValue.Value();
        }

        /// <summary>Called when [phase changed].</summary>
        /// <param name="phaseChange">The phase change.</param>
        /// <param name="sender">Sender plant.</param>
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(object sender, PhaseChangedType phaseChange)
        {
            if (phaseChange.StageName == SetEvent)
                _Value = PostEventValue.Value();

            if (phaseChange.StageName == ReSetEvent)
                _Value = PreEventValue.Value();
        }

        /// <summary>Called when crop is being harvested.</summary>
        [EventSubscribe("Cutting")]
        private void OnHarvesting(object sender, EventArgs e)
        {
            _Value = PreEventValue.Value();
        }

        /// <summary>Gets the value.</summary>
        public double Value(int arrayIndex = -1)
        {
            return _Value;
        }
    }
}
