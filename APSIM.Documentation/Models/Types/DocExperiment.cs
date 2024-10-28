using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models;
using Models.Core;
using Graph = Models.Graph;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Documentation class for generic models
    /// </summary>
    public class DocExperiment : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocGenericWithChildren" /> class.
        /// </summary>
        public DocExperiment(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            Section section = GetSummaryAndRemarksSection(model);

            foreach (IModel child in model.FindAllChildren())
            {
                if (child is Memo || child is Graph || child is Folder)
                {
                    section.Add(AutoDocumentation.DocumentModel(child));
                }
            }

            return new List<ITag>() {section};
        }
    }
}
