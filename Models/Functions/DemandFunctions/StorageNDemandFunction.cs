using System;
using Models.Core;
using Models.PMF.Interfaces;

namespace Models.Functions.DemandFunctions
{
    /// <summary>The partitioning of daily N supply to storage N attempts to bring the organ's N content to the maximum concentration.</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class StorageNDemandFunction : Model, IFunction
    {
        /// <summary>The maximum N concentration of the organ</summary>
        [Description("The maximum N concentration of the organ")]
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction maxNConc = null;

        /// <summary>Switch to modulate N demand</summary>
        [Description("Switch to modulate N demand")]
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction nitrogenDemandSwitch = null;

        private IArbitration parentOrgan = null;

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            bool ParentOrganIdentified = false;
            IModel ParentClass = this.Parent;
            while (!ParentOrganIdentified)
            {
                if (ParentClass is IArbitration)
                {
                    parentOrgan = ParentClass as IArbitration;
                    ParentOrganIdentified = true;
                    if (ParentClass is IPlant)
                        throw new Exception(Name + "cannot find parent organ to get Structural and Storage N status");
                }
                ParentClass = ParentClass.Parent;
            }
        }

        /// <summary>Gets the value.</summary>
        public double Value(int arrayIndex = -1)
        {
            double potentialAllocation = parentOrgan.potentialDMAllocation.Structural + parentOrgan.potentialDMAllocation.Metabolic;
            double NDeficit = Math.Max(0.0, maxNConc.Value() * (parentOrgan.Live.Wt + potentialAllocation) - parentOrgan.Live.N);
            NDeficit *= nitrogenDemandSwitch.Value();

            return Math.Max(0, NDeficit - parentOrgan.NDemand.Structural - parentOrgan.NDemand.Metabolic);
        }
    }
}
