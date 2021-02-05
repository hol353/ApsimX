using System;
using System.Collections.Generic;
using System.Linq;
using Models.Core;
using APSIM.Shared.Utilities;
using Models.Core.Script;

namespace Models.Functions
{

    /// <summary>
    /// A c# expression is evaluated.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class CSharpExpressionFunction : Model, IFunction, ICustomDocumentation
    {
        /// <summary>An instance of an IFunction that is our compiled expression.</summary>
        private IFunction expressionFunction;

        /// <summary>The expression.</summary>
        [Description("C# expression")]
        public string Expression { get; set; }

        /// <summary>Gets the value of the expression.</summary>
        public double Value(int arrayIndex = -1)
        {
            return expressionFunction.Value(arrayIndex);
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // add a heading.
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));
                // write memos.
                foreach (IModel memo in this.FindAllChildren<Memo>())
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

                string st = Expression?.Replace(".Value()", "");
                tags.Add(new AutoDocumentation.Paragraph(Name + " = " + st, indent));

                foreach (IModel child in this.FindAllChildren<IFunction>())
                    AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent + 1);
            }
        }

        /// <summary>
        /// Compile the expression and return the compiled function.
        /// </summary>
        public void CompileExpression()
        {
            // From a list of visible models in scope, create [Link] lines e.g.
            //    [Link] Clock Clock;
            //    [Link] Weather Weather;
            // and namespace lines e.g.
            //    using Models.Clock;
            //    using Models;
            var models = Parent.FindAllInScope().ToList().Where(model => !model.IsHidden && 
                                                              model.GetType() != typeof(Graph) &&
                                                              model.GetType() != typeof(Series) &&
                                                              model.GetType().Name != "StorageViaSockets");
            var linkList = new List<string>();
            var namespaceList = new SortedSet<string>();
            foreach (var model in models)
            {
                if (Expression.Contains(model.Name))
                {
                    linkList.Add($"        [Link(ByName=true)] {model.GetType().Name} {model.Name};");
                    namespaceList.Add("using " + model.GetType().Namespace + ";");
                }
            }
            var namespaces = StringUtilities.BuildString(namespaceList.Distinct(), Environment.NewLine);
            var links = StringUtilities.BuildString(linkList.Distinct(), Environment.NewLine);

            // Get template c# script.
            var template = ReflectionUtilities.GetResourceAsString("Models.Resources.Scripts.CSharpExpressionTemplate.cs");

            // Replace the "using Models;" namespace place holder with the namesspaces above.
            template = template.Replace("using Models;", namespaces);

            template = template.Replace("class Script", $"class {Name}Script");

            // Replace the link place holder in the template with links created above.
            template = template.Replace("        [Link] Clock Clock;", links.ToString());

            // Replace the expression place holder in the template with the real expression.
            template = template.Replace("return 123456;", "return " + Expression + ";");

            // Create a new manager that will compile the expression.
            ScriptCompiler.Instance.AddScript(template, FullPath, OnInstanceCreated);
        }

        /// <summary>
        /// An instance of the expression function has been compiled.
        /// </summary>
        /// <param name="instance"></param>
        private void OnInstanceCreated(object instance)
        {
            expressionFunction = instance as IFunction;
            (expressionFunction as IModel).Parent = this;

            // Resolve links
            var linkResolver = new Links();
            linkResolver.Resolve(expressionFunction as IModel, true);
        }

        /// <summary>
        /// Handler method for the start of simulation event.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            CompileExpression();
        }
    }
}