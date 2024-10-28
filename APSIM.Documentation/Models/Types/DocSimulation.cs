using System.Collections.Generic;
using APSIM.Shared.Documentation;
using M = Models;
using Models.Core;

namespace APSIM.Documentation.Models.Types
{
    /// <summary>
    /// Base documentation class for models
    /// </summary>
    public class DocSimulation : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocSimulation" /> class.
        /// </summary>
        public DocSimulation(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            Section section = GetSummaryAndRemarksSection(model);
            
            foreach (IModel child in model.Children)
                if (child is M.Memo || child is M.Graph || child is M.Map || child is M.Manager)
                    section.Add(AutoDocumentation.DocumentModel(child));

            return new List<ITag>() {section};
        }
    }
}
