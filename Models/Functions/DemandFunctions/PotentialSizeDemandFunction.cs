using System;
using Models.Core;
using Models.PMF.Phen;

namespace Models.Functions.DemandFunctions
{
    /// <summary>Demand is calculated from the product of potential growth increment, organ number and thermal time.</summary>
    [Serializable]
    public class PotentialSizeDemandFunction : Model, IFunction
    {
        private int startStageIndex;

        private int endStageIndex;

        /// <summary>The start stage name</summary>
        public string StartStageName = "";

        /// <summary>The end stage name</summary>
        public string EndStageName = "";

        /// <summary>The potential growth increment</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction PotentialGrowthIncrement = null;

        /// <summary>The organ number</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction OrganNumber = null;

        /// <summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;

        /// <summary>The thermal time</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction ThermalTime = null;

        /// <summary>The accum thermal time</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction AccumThermalTime = null;

        /// <summary>Gets the accumulated thermal time.</summary>
        /// <value>The accumulated thermal time.</value>
        [Units("oCd")]
        public double AccumulatedThermalTime //FIXME.  This is not used in Code, check is needed
        {
            get { return AccumThermalTime.Value(); }
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (Phenology.Between(startStageIndex, endStageIndex))
                return PotentialGrowthIncrement.Value(arrayIndex) * OrganNumber.Value(arrayIndex) * ThermalTime.Value(arrayIndex);
            else
                return 0;
        }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            startStageIndex = Phenology.StartStagePhaseIndex(StartStageName);
            endStageIndex = Phenology.EndStagePhaseIndex(EndStageName);
        }

    }
}


