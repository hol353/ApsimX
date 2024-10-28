using System.Collections.Generic;

namespace APSIM.Shared.Documentation
{
    /// <summary>
    /// A graph tag.
    /// </summary>
    public class GraphPage : ITag
    {
        /// <summary>
        /// The graphs to be displayed.
        /// </summary>
        public IEnumerable<IGraph> Graphs { get; private set; }

        /// <summary>
        /// Constructs a graph tag instance.
        /// </summary>
        /// <param name="graphs">Graphs to be displayed.</param>
        public GraphPage(IEnumerable<IGraph> graphs) => Graphs = graphs;
    }
}
