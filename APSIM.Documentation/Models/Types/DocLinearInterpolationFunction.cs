using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.Functions;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Documentation class for LinearInterpolationFunction
    /// </summary>
    public class DocLinearInterpolationFunction : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocLinearInterpolationFunction" /> class.
        /// </summary>
        public DocLinearInterpolationFunction(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            Section section = GetSummaryAndRemarksSection(model);

            section.Add(new Paragraph($"*{model.Name}* is calculated using linear interpolation"));

            XYPairs xyPairs = model.FindChild<XYPairs>();
            if (xyPairs != null)
                section.Add(AutoDocumentation.DocumentModel(xyPairs));

            return new List<ITag>() {section};
        }
    }
}
