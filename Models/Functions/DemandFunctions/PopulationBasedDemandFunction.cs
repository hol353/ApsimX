using System;
using Models.Core;
using Models.PMF;

namespace Models.Functions.DemandFunctions
{
    /// <summary>Demand is calculated from the product of growth rate, thermal time and population.</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class PopulationBasedDemandFunction : Model, IFunction
    {
        /// <summary>The thermal time</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction ThermalTime = null;

        /// <summary>The number of growing organs</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction OrganPopulation = null;

        /// <summary>The phenology</summary>
        [Link(IsOptional = true)]
        private Models.PMF.Phen.Phenology Phenology = null;

        /// <summary>The expansion stress</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("0-1")]
        IFunction ExpansionStress = null;

        /// <summary>The maximum organ wt</summary>
        [Description("Size individual organs will grow to when fully supplied with DM")]
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g")]
        IFunction MaximumOrganWt = null;

        /// <summary>The start stage</summary>
        [Description("Stage when organ growth starts ")]
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction StartStage = null;

        /// <summary>The growth duration</summary>
        [Description("ThermalTime duration of organ growth ")]
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("^o^Cd")]
        IFunction GrowthDuration = null;

        /// <summary>The accumulated thermal time</summary>
        private double AccumulatedThermalTime = 0;
        /// <summary>The thermal time today</summary>
        private double ThermalTimeToday = 0;

        /// <summary>Called when DoDailyInitialisation invoked</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            if ((Phenology.Stage >= StartStage.Value()) && (AccumulatedThermalTime < GrowthDuration.Value()))
            {
                ThermalTimeToday = Math.Min(ThermalTime.Value(), GrowthDuration.Value() - AccumulatedThermalTime);
                AccumulatedThermalTime += ThermalTimeToday;
            }
            else if (Phenology.Stage < StartStage.Value())
            {
                AccumulatedThermalTime = 0.0;
            }
        }


        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            double Value = 0.0;
            if ((Phenology.Stage >= StartStage.Value(arrayIndex)) && (AccumulatedThermalTime < GrowthDuration.Value(arrayIndex)))
            {
                double Rate = MaximumOrganWt.Value(arrayIndex) / GrowthDuration.Value(arrayIndex);
                Value = Rate * ThermalTimeToday * OrganPopulation.Value(arrayIndex);
            }

            return Value * ExpansionStress.Value(arrayIndex);
        }

        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowingParameters data)
        {
            AccumulatedThermalTime = 0;
        }

    }
}


