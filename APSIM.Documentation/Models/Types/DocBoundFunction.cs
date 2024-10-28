using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using System.Linq;
using Models.Functions;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Documentation for BoundFunction model
    /// </summary>
    public class DocBoundFunction : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocBoundFunction" /> class.
        /// </summary>
        public DocBoundFunction(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            Section section = GetSummaryAndRemarksSection(model);
            
            List<IFunction> childFunctions = model.FindAllChildren<IFunction>().ToList();

            IFunction Upper = null;
            IFunction Lower = null;
            foreach (IFunction child in childFunctions)
            {
                if (child.Name.CompareTo("Upper") == 0)
                    Upper = child;
                if (child.Name.CompareTo("Lower") == 0)
                    Lower = child;
            }

            foreach (IFunction child in childFunctions)
            {
                if (child != Upper && child != Lower)
                {
                    section.Add(new Paragraph($"{model.Name} is the value of {child.Name} bound between a lower and upper bound where:"));
                    section.Add(AutoDocumentation.DocumentModel(child));
                }
            }
            if (Lower != null)
                section.Add(AutoDocumentation.DocumentModel(Lower));
            if (Upper != null)
                section.Add(AutoDocumentation.DocumentModel(Upper));

            return new List<ITag>() {section};
        }
    }
}
