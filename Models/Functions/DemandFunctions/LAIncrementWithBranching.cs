using System;
using System.Linq;
using Models.Core;

namespace Models.Functions.DemandFunctions
{
    /// <summary>Daily increment in leaf area is calculated from plant population, number of nodes on the main stem, number of branches,
    /// area of the largest leaf (or leaf pair), rate of leaf appearance, and relative leaf area. It is assumed that the main stem and all 
    /// branches are similar when fully grown.</summary>
    [Serializable]
    public class LAIncrementWithBranching : Model, IFunction
    {
        /// <summary>The plant population</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction plantNumber = null;

        /// <summary>The number of nodes on the main stem</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction nodeNumber = null;

        /// <summary>The number of branches (excluding the main stem)</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction branchNumber = null;

        /// <summary>The area of the largest leaf</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction areaLargestLeaf = null;

        /// <summary>The rate of leaf appearance</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction lAR = null;

        /// <summary>The relative leaf area as a function of nodeNumber</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private XYPairs relativeArea = null;

        /// <summary>The lag (in terms of node number) between each individual branch (including the main stem) and the main stem</summary>
        private int[] branchNodeLag = Enumerable.Repeat(-1, 20).ToArray();

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            double currNodeNumber = nodeNumber.Value();
            int numBranches = (int)branchNumber.Value();

            branchNodeLag[0] = 0;
            double cumDelta = relativeArea.ValueIndexed(currNodeNumber - branchNodeLag[0]);

            for (int i = 1; i < numBranches + 1; i++)
            {
                if (branchNodeLag[i] < 0)
                    branchNodeLag[i] = (int)currNodeNumber;

                cumDelta += relativeArea.ValueIndexed(currNodeNumber - branchNodeLag[i]);
            }
            return cumDelta * plantNumber.Value() * areaLargestLeaf.Value() * lAR.Value();
        }
    }
}