using System;
using Models.Core;
using Models.Functions;
using Models.PMF.Phen;

namespace Models.PMF.Struct
{
    /// <summary> 
    /// Each time the specified event occurs, bud number on each main-stem is set to:
    /// 
    /// *FractionOfBudBurst* * *BudNumber*
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Structure))]
    public class BudNumberFunction : Model
    {
        [Link]
        Plant Plant = null;

        [Link]
        Structure structure = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction FractionOfBudBurst = null;

        /// <summary>The stage on which bud number is set</summary>
        [Description("The event that triggers setting of the bud number")]
        public string SetStage { get; set; }

        /// <summary>Called when [phase changed].</summary>
        /// <param name="phaseChange">The phase change.</param>
        /// <param name="sender">Sender plant.</param>
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(object sender, PhaseChangedType phaseChange)
        {
            if (phaseChange.StageName == structure.CohortInitialisationStage)
                structure.PrimaryBudNo = Plant.SowingData.BudNumber;
            if (phaseChange.StageName == structure.LeafInitialisationStage)
            {
                structure.PrimaryBudNo = Plant.SowingData.BudNumber * FractionOfBudBurst.Value();
                structure.TotalStemPopn = structure.MainStemPopn;
            }
        }
    }
}
