using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Documentation class for generic models
    /// </summary>
    public class DocNodule : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocNodule" /> class.
        /// </summary>
        public DocNodule(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            Section section = GetSummaryAndRemarksSection(model);

            foreach (IModel child in model.FindAllChildren())
                section.Add(AutoDocumentation.DocumentModel(child));

            return new List<ITag>() {section};
        }
    }
}
