using System;
using Models.Core;
using Models.PMF;
using Models.PMF.Phen;

namespace Models.Functions.DemandFunctions
{
    /// <summary>This function calculates DM demand beyond the start stage as the product of current organ wt (g), relative growth rate and the specified organ number.</summary>
    [Serializable]
    public class RelativeGrowthRateDemandFunction : Model, IFunction
    {
        /// <summary>The initial wt</summary>
        public double InitialWt = 0;

        /// <summary>The initial stage name</summary>
        public string InitialStageName = "";

        /// <summary>The relative growth rate</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction RelativeGrowthRate = null;

        /// <summary>The organ number</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction OrganNumber = null;

        /// <summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;

        /// <summary>The live</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        Biomass Live = null;

        /// <summary>The start wt</summary>
        double StartWt = 0;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (Phenology.OnStartDayOf(InitialStageName) && StartWt == 0)
                StartWt = InitialWt;                                   //This is to initiate mass so relative growth rate can kick in
            double CurrentOrganWt = Math.Max(StartWt, Live.Wt / OrganNumber.Value(arrayIndex));
            double OrganDemand = CurrentOrganWt * RelativeGrowthRate.Value(arrayIndex);
            return OrganDemand * OrganNumber.Value(arrayIndex);
        }
    }
}


