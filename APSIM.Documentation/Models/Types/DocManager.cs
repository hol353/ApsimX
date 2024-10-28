using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models;
using Models.Core;

namespace APSIM.Documentation.Models.Types
{
    /// <summary>
    /// Documentation class for generic models
    /// </summary>
    public class DocManager : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocManager" /> class.
        /// </summary>
        public DocManager(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            Section section = GetSummaryAndRemarksSection(model);

            Section parameters = new Section("Parameters", new List<ITag>());
            foreach (KeyValuePair<string, string> pair in (model as Manager).Parameters)
                parameters.Add(new Paragraph(pair.Key + " = " + pair.Value));

            section.Add(parameters);

            return new List<ITag>() {section};
        }
    }
}