using System;
using Models.Core;
using Models.Functions;

namespace Models.PMF.Organs
{
    /// <summary>
    /// Keep tracking LAI and nodes after plant reaches buds visible stage
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(SimpleLeaf))]
    public class BasialBuds : Model
    {
        /// <summary>The reset stage name</summary>
        [Description("(optional) Stage name to reset accumulation")]
        public string ResetStageName { get; set; }
        /// <summary>
        /// NodeNumber for basial buds
        /// </summary> 
        public double NodeNumber { get; set; }
        /// <summary>
        /// Leaf Area for basial buds
        /// </summary> 
        public double LAI { get; set; }
        /// <summary>
        /// Nodenumber Function
        /// </summary>
        [Link(Type = LinkType.Child, ByName = true)] private IFunction Deltanodenumber = null;
        /// <summary>
        /// Leaf Area Index Function
        /// </summary>
        [Link(Type = LinkType.Child, ByName = true)] private IFunction Deltalai = null;

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            LAI = 0;
            NodeNumber = 0;
        }

        [EventSubscribe("PostPhenology")]
        private void PostPhenology(object sender, EventArgs e)
        {
            DaysAfterCutting += 1;
            LAI += Deltalai.Value();
            NodeNumber += Deltanodenumber.Value();
            if (DaysAfterCutting == 3)
            {
                LAI = 0;
                NodeNumber = 0;
            }
        }

        private int DaysAfterCutting = 0;

        /// <summary>Called when [cut].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Cutting")]
        private void OnCut(object sender, EventArgs e)
        {
            DaysAfterCutting = 0;
        }

        /// <summary>Called when [cut].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Harvesting")]
        private void OnHarvest(object sender, EventArgs e)
        {
            DaysAfterCutting = 0;
        }
        /// <summary>Called when [cut].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Grazing")]
        private void OnGraze(object sender, EventArgs e)
        {
            DaysAfterCutting = 0;
        }

        /// <summary>Called when [cut].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Pruning")]
        private void OnPrune(object sender, EventArgs e)
        {
            DaysAfterCutting = 0;
        }

        /// <summary>Called when [EndCrop].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs e)
        {
            LAI = 0;
            NodeNumber = 0;
        }

    }
}