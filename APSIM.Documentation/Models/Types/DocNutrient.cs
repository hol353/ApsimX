using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Documentation class for generic models
    /// </summary>
    public class DocNutrient : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocNutrient" /> class.
        /// </summary>
        public DocNutrient(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            Section section = GetSummaryAndRemarksSection(model);
            string stuctureTagText = CodeDocumentation.GetCustomTag(model.GetType(),"structure");
            string poolsTagText = CodeDocumentation.GetCustomTag(model.GetType(),"pools");
            string solutesTagText = CodeDocumentation.GetCustomTag(model.GetType(),"solutes");
            section.Add(new Section("Structure", new Paragraph(stuctureTagText)));
            section.Add(new Section("Pools",new Paragraph(poolsTagText)));
            section.Add(new Section("Solutes",new Paragraph(solutesTagText)));
            return new List<ITag>() {section};
        }
    }
}
