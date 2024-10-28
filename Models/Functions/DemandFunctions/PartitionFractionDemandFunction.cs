using System;
using Models.Core;
using Models.PMF.Interfaces;

namespace Models.Functions.DemandFunctions
{
    /// <summary>Returns the product of its PartitionFraction and the total DM supplied to the arbitrator by all organs.</summary>
    [Serializable]
    public class PartitionFractionDemandFunction : Model, IFunction
    {
        /// <summary>The partition fraction</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        IFunction PartitionFraction = null;

        /// <summary>The arbitrator</summary>
        [Link]
        IArbitrator arbitrator = null;

        [Link(Type = LinkType.Ancestor)]
        IOrgan parentOrgan = null;

        /// <summary>Gets the value.</summary>
        public double Value(int arrayIndex = -1)
        {
            if (PartitionFraction.Value(arrayIndex) < 0)
                throw new Exception("PartitionFraction in " + this.parentOrgan.Name + " is returning a negative value");
            if (arbitrator != null)
                return arbitrator.TotalDMFixationSupply * PartitionFraction.Value(arrayIndex);
            else
                return 0;
        }
    }
}
