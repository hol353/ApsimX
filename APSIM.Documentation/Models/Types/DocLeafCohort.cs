using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.PMF.Organs;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Documentation class for generic models
    /// </summary>
    public class DocLeafCohort : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocGenericWithChildren" /> class.
        /// </summary>
        public DocLeafCohort(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            Section section = GetSummaryAndRemarksSection(model);
            section.Add(new Section("Area", new Paragraph($"Area = {(model as LeafCohort).Area}")));
            return new List<ITag>() {section};
        }
    }
}
