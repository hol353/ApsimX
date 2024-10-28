using System;
using Models.Core;
using Models.PMF.Phen;

namespace Models.Functions.DemandFunctions
{
    /// <summary>Filling rate is calculated from grain number, a maximum mass to be filled and the duration of the filling process.</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class BerryFillingRateFunction : Model, IFunction
    {
        /// <summary>InitialGrowthPhase</summary>
        [Description("InitialGrowthPhase")]
        public String FirstPhase { get; set; }
        /// <summary>SecondaryGrowthPhase</summary>
        [Description("SecondaryGrowthPhase")]
        public String SecondPhase { get; set; }

        /// <summary>Wo1</summary>
        [Description("InitialDMatEndFlowering")]
        public double Wo1 { get; set; }
        /// <summary>Wf1</summary>
        [Description("MaxDMatVeraison")]
        public double Wf1 { get; set; }
        /// <summary>Mu1</summary>
        [Description("GrowthRate1")]
        public double Mu1 { get; set; }
        /// <summary>Wo2</summary>
        [Description("InitialDMatVeraison")]
        public double Wo2 { get; set; }
        /// <summary>Wf2</summary>
        [Description("MaxDMatLeafFall")]
        public double Wf2 { get; set; }
        /// <summary>Mu2</summary>
        [Description("GrowthRate2")]
        public double Mu2 { get; set; }

        /// <summary>
        /// Link to phenology
        /// </summary>
        [Link]
        public Phenology Phenology = null;
        /// <summary>
        /// Thermal time that drives berry development
        /// </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction TT = null;

        private double yesterdaysDM = 0;
        private double AccTT = 0;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (Phenology.CurrentPhaseName == FirstPhase)
            {
                AccTT += TT.Value(arrayIndex);
                double TodaysDM = 0;
                TodaysDM = Wf1 / (1 + (Wf1 - Wo1) / Wo1 * Math.Exp(-Mu1 * AccTT));
                double returnValue = TodaysDM - yesterdaysDM;
                yesterdaysDM = TodaysDM;
                return returnValue;
            }
            if (Phenology.CurrentPhaseName == SecondPhase)
            {
                AccTT += TT.Value(arrayIndex);
                double TodaysDM = 0;
                TodaysDM = Wf1 / (1 + (Wf1 - Wo1) / Wo1 * Math.Exp(-Mu1 * AccTT)) + Wf2 / (1 + (Wf2 - Wo2) / Wo2 * Math.Exp(-Mu2 * AccTT));
                double returnValue = TodaysDM - yesterdaysDM;
                yesterdaysDM = TodaysDM;
                return returnValue;
            }
            else
                return 0;
        }

        /// <summary>Called when crop is being prunned.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Pruning")]
        private void OnPruning(object sender, EventArgs e)
        {
            AccTT = 0;
            yesterdaysDM = 0;
        }
    }
}



