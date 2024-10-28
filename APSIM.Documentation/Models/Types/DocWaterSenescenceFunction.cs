using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.Functions;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Documentation class for generic models
    /// </summary>
    public class DocWaterSenescenceFunction : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocWaterSenescenceFunction" /> class.
        /// </summary>
        public DocWaterSenescenceFunction(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            Section section = GetSummaryAndRemarksSection(model);

            WaterSenescenceFunction waterSenFuncModel = model as WaterSenescenceFunction;

            section.Add(AutoDocumentation.DocumentModel(waterSenFuncModel.FindChild<IFunction>("senWaterTimeConst")));
            section.Add(AutoDocumentation.DocumentModel(waterSenFuncModel.FindChild<IFunction>("senThreshold")));
            section.Add(new Paragraph("SDRatio is the Water Supply divided by the Water Demand (found in Arbitrator). It will return 1.0 unless there is less Supply than Demand"));

            return new List<ITag>() {section};
        }
    }
}
