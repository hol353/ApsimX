using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.PMF;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Documentation class for generic models
    /// </summary>
    public class DocAlias : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocAlias" /> class.
        /// </summary>
        public DocAlias(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            Section section = GetSummaryAndRemarksSection(model);
            section.Add(new Paragraph($"An alias for {(model as Alias).FindAncestor<Cultivar>()?.Name}"));
            return new List<ITag>() {section};
        }
    }
}
