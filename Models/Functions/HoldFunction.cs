using System;
using Models.Core;
using Models.PMF.Phen;

namespace Models.Functions
{
    /// <summary>
    /// Returns the a value which is updated daily until a given stage is reached, beyond which it is held constant
    /// </summary>
    [Serializable]
    [Description("Returns the ValueToHold which is updated daily until the WhenToHold stage is reached, beyond which it is held constant")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class HoldFunction : Model, IFunction
    {
        /// <summary>The _ value</summary>
        private double _Value = 0;

        /// <summary>The set event</summary>
        [Description("Phenological stage at which value stops updating and is held constant")]
        [Display(Type = DisplayType.CropStageName)]
        public string WhenToHold { get; set; }

        /// <summary>The value to hold after event</summary>
        [Link(Type = LinkType.Child)]
        IFunction ValueToHold = null;

        /// <summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnCommencing(object sender, EventArgs e)
        {
            GetValue();
        }

        /// <summary>Called by Plant.cs when phenology routines are complete.</summary>
        /// <param name="sender">Plant.cs</param>
        /// <param name="e">Event arguments</param>
        [EventSubscribe("DoUpdate")]
        private void OnDoUpdate(object sender, EventArgs e)
        {
            if (Phenology.Beyond(WhenToHold))
            {
                //Do nothing, hold value constant
            }
            else
                GetValue();
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            return _Value;
        }

        /// <summary>Get value</summary>
        private void GetValue()
        {
            _Value = ValueToHold.Value();
        }
    }
}