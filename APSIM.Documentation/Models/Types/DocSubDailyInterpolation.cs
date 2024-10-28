using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.Functions;
using Models.Interfaces;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Documentation class for SubDailyInterpolation
    /// </summary>
    public class DocSubDailyInterpolation : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocSubDailyInterpolation" /> class.
        /// </summary>
        public DocSubDailyInterpolation(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            Section section = GetSummaryAndRemarksSection(model);

            SubDailyInterpolation sub = model as SubDailyInterpolation;
            IIndexedFunction Response = sub.FindChild<IIndexedFunction>("Response");
            IInterpolationMethod InterpolationMethod = sub.FindChild<IInterpolationMethod>("InterpolationMethod");

            if (Response != null)
            {
                section.Add(new Paragraph($"{model.Name} is the {sub.agregationMethod.ToString().ToLower()} of sub-daily values from a {Response.GetType().Name}."));
                section.Add(new Paragraph($"Each of the interpolated {InterpolationMethod.OutputValueType}s are then passed into the following Response and the {sub.agregationMethod} taken to give daily {sub.Name}"));
                section.Add(AutoDocumentation.DocumentModel(Response));
                section.Add(AutoDocumentation.DocumentModel(InterpolationMethod as IModel));
            }

            return new List<ITag>() {section};
        }
    }
}
