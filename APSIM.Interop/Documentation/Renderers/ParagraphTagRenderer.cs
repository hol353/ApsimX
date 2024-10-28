using APSIM.Interop.Markdown.Parsers.Inlines;
using APSIM.Interop.Markdown.Renderers;
using APSIM.Shared.Documentation;
using Markdig;
using Markdig.Parsers;
using Markdig.Syntax;

namespace APSIM.Interop.Documentation.Renderers
{
    /// <summary>
    /// A class which can use a <see cref="PdfBuilder" /> to render an
    /// <see cref="ITag" /> to a PDF document.
    /// </summary>
    /// <typeparam name="T">The type of tag which this class can render.</typeparam>
    internal class ParagraphTagRenderer : TagRendererBase<Paragraph>
    {
        /// <summary>
        /// Making this a constant for now. Theoretically, we could expose options
        /// (e.g. for using various extras supported by markdig), but we don't need
        /// it for now so for now it will be faster to reuse a single pipeline each
        /// time we need to render a document, rather than constructing one each time.
        /// </summary>
        private static readonly MarkdownPipeline pipeline;

        /// <summary>
        /// Static constructor, called once, to initialise a 'standard' markdown pipeline
        /// which will be used to parse all markdown documents.
        /// </summary>
        static ParagraphTagRenderer()
        {
            MarkdownPipelineBuilder builder = new MarkdownPipelineBuilder().UsePipeTables().UseEmphasisExtras();
            builder.InlineParsers.Add(new ReferenceInlineParser());
            pipeline = builder.Build();
        }

        /// <summary>
        /// Render the given Paragraph tag to the PDF document.
        /// </summary>
        /// <param name="paragraph">Paragraph tag to be rendered.</param>
        /// <param name="renderer">PDF renderer to use for rendering the tag.</param>
        protected override void Render(Paragraph paragraph, PdfBuilder renderer)
        {
            // Add the Paragraph to a new paragraph.
            if (!string.IsNullOrWhiteSpace(paragraph.text))
            {
                MarkdownDocument document = MarkdownParser.Parse(paragraph.text, pipeline);
                renderer.Render(document);
            }
        }
    }
}
