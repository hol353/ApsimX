using System.Collections.Generic;
using System.IO;
using System.Linq;
using APSIM.Shared.Documentation;
using APSIM.Shared.Utilities;
using Models;
using Models.Core;
using Graph = Models.Graph;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Documentation class for generic models
    /// </summary>
    public class DocSimulations : DocGeneric
    {
        private static string PATH_REVIEW = "/Tests/UnderReview/";
        private static string PATH_VALIDATION = "/Tests/Validation/";
        private static string PATH_TUTORIAL = "/Examples/Tutorials/";

        /// <summary>
        /// Initializes a new instance of the <see cref="DocSimulations" /> class.
        /// </summary>
        public DocSimulations(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            List<ITag> tags = new List<ITag>();
            Simulations sims = model as Simulations;
            
            if (!string.IsNullOrEmpty(sims.FileName))
            {
                if (sims.FileName.Contains(PATH_REVIEW) || sims.FileName.Contains(PATH_REVIEW.Replace('/', '\\')) ||
                    sims.FileName.Contains(PATH_VALIDATION) || sims.FileName.Contains(PATH_VALIDATION.Replace('/', '\\')))
                {
                    tags.AddRange(DocumentValidation(model as Simulations));
                }
                else if (sims.FileName.Contains(PATH_TUTORIAL) || sims.FileName.Contains(PATH_TUTORIAL.Replace('/', '\\')))
                {
                    tags.AddRange(DocumentTutorial(model as Simulations));
                }
                else
                {
                    foreach(IModel child in sims.FindAllChildren())
                    {
                        tags.AddRange(AutoDocumentation.DocumentModel(child));
                    }
                }
            }

            return tags;
        }

        private List<ITag> DocumentValidation(Model m)
        {
            string name = Path.GetFileNameWithoutExtension((m as Simulations).FileName);
            string title = "The APSIM " + name + " Model";

            if (name.ToLower() == "SpeciesTable".ToLower()) //This is a special case file, we may want to review this in the future to remove/merge it
                return (new DocSpeciesTable(m)).Document();

            List<ITag> tags = new List<ITag>();
            List<ITag> modelTags = new List<ITag>();

            List<Memo> memos = m.FindAllChildren<Memo>().ToList();
            List<ITag> memoTags = new List<ITag>();
            if (name.ToLower() != "wheat")          //Wheat has the memo in both the validation and resource, so don't do it for that.
                    foreach (IModel child in memos)
                        memoTags.AddRange(AutoDocumentation.DocumentModel(child));

            // Find a single instance of all unique Plant models.
            IModel modelToDocument = m.FindDescendant(name);
            if (modelToDocument != null)
            {
                modelTags.AddRange(AutoDocumentation.DocumentModel(modelToDocument));
            }

            //Sort out heading
            Section firstSection = new Section(title, memoTags);
            foreach(ITag tag in modelTags)
            {
                if (tag.GetType() == typeof(Section))
                {
                    foreach(ITag subtag in (tag as Section).Children)
                        firstSection.Add(subtag);
                }
                else if (tag.GetType() == typeof(Paragraph))
                {
                    firstSection.Add(tag);
                }
            }
            
            tags.Add(firstSection);

            //Then just document the folders that aren't replacements
            foreach (IModel child in m.FindAllChildren<Folder>())
            {
                if(child.Name != "Replacements")
                    tags.AddRange(AutoDocumentation.DocumentModel(child));
            }

            return tags;
        }

        private List<ITag> DocumentTutorial(Model m)
        {
            List<ITag> tags = new List<ITag>();

            if (m is not Simulation)
            {
                string name = Path.GetFileNameWithoutExtension((m as Simulations).FileName);
                string title = name + " Tutorial";
                //Sort out heading
                var firstSection = new Section(title, new List<ITag>() { new Paragraph("----") });
                tags.Add(firstSection);
            }

            foreach(IModel child in m.FindAllChildren())
            {
                if (child is Simulation)
                { 
                    tags.Add(new Section(child.Name, DocumentTutorial(child as Simulation)));
                } 
                else if(child is Memo || child is Graph || (child is Folder && child.Name != "Replacements"))
                {
                    tags.AddRange(AutoDocumentation.DocumentModel(child));
                }
            }

            return tags;
        }
    }
}
