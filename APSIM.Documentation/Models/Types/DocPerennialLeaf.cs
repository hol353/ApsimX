using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.Functions;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Documentation class for generic models
    /// </summary>
    public class DocPerennialLeaf : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocPerennialLeaf" /> class.
        /// </summary>
        public DocPerennialLeaf(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            Section section = GetSummaryAndRemarksSection(model);
            
            List<ITag> subTags = new List<ITag>();

            // Document Constants
            foreach (var constant in model.FindAllChildren<Constant>())
                subTags.AddRange(AutoDocumentation.DocumentModel(constant));

            section.Add(new Section("Constants", subTags));

            return new List<ITag>() {section};
        }
    }
}
