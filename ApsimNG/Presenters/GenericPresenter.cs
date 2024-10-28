using System.Collections.Generic;
using System.Text;
using APSIM.Shared.Documentation;
using APSIM.Documentation.Models;
using Models.Core;
using UserInterface.Views;

namespace UserInterface.Presenters
{
    /// <summary>
    /// Presenter of unspecified type
    /// </summary>
    public class GenericPresenter : IPresenter
    {
        /// <summary>
        /// The model
        /// </summary>
        private Model model;

        /// <summary>
        /// The view to use
        /// </summary>
        private IMarkdownView genericView;

        /// <summary>
        /// The explorer
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// Attach the view
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="view">The view to attach</param>
        /// <param name="explorerPresenter">The explorer</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.model = model as Model;
            this.genericView = view as IMarkdownView;
            this.explorerPresenter = explorerPresenter;

            // Just how much documentation do we want to generate?
            // For now, let's just use the component name and a basic description.

            // It's slightly simpler to generate Markdown for this, but it
            // would be pretty easy to build this directly as HTML
            /*
            List<ITag> tags = new List<ITag>();
            AutoDocumentation.Document(this.model, tags, 1, 0);

            StringBuilder contents = new StringBuilder();
            foreach (ITag tag in tags)
            {
                if (tag is Heading heading)
                {
                    contents.AppendLine();
                    contents.Append($"### {heading.text}");
                }
                else if (tag is Paragraph paragraph)
                {
                    contents.AppendLine();
                    contents.Append(paragraph.text);
                }
            }

            this.genericView.Text = contents.ToString();
            */
        }

        /// <summary>
        /// Detach the view
        /// </summary>
        public void Detach()
        {
        }
    }
}
