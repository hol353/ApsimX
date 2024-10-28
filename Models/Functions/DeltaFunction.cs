using System;
using Models.Core;
using Models.PMF.Phen;

namespace Models.Functions
{
    /// <summary>
    /// This function returns the daily delta for its child function
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("Stores the value of its child function (called Integral) from yesterday and returns the difference between that and todays value of the child function")]
    public class DeltaFunction : Model, IFunction
    {
        //Class members
        /// <summary>The accumulated value</summary>
        private double YesterdaysValue = 0;

        /// <summary>The start stage name</summary>
        [Description("StartStageName")]
        [Display(Type = DisplayType.CropStageName)]
        public string StartStageName { get; set; }

        /// <summary>The child function to return a delta for</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction Integral = null;

        /// <summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;

        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            YesterdaysValue = 0;
        }

        [EventSubscribe("DoCatchYesterday")]
        private void OnDoCatchYesterday(object sender, EventArgs e)
        {
             YesterdaysValue = Integral.Value();
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (StartStageName != null)
            {
                if (Phenology.Beyond(StartStageName))
                {
                    return Integral.Value(arrayIndex) - YesterdaysValue;
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return Integral.Value(arrayIndex) - YesterdaysValue;
            }
        }

        /// <summary>Called when [EndCrop].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs e)
        {
            YesterdaysValue = 0;
        }

        /// <summary>Called when [EndCrop].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StageWasReset")]
        private void OnStageReset(object sender, StageSetType e)
        {
            YesterdaysValue = Integral.Value();
        }
    }
}
