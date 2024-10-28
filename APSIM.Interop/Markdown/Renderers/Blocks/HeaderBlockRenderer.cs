using System;
using System.Linq;
using Markdig.Syntax;

namespace APSIM.Interop.Markdown.Renderers.Blocks
{
    /// <summary>
    /// This class renders a <see cref="HeadingBlock" /> object to a PDF document.
    /// </summary>
    public class HeadingBlockRenderer : PdfObjectRenderer<HeadingBlock>
    {
        /// <summary>
        /// Render the given heading block to the PDF document.
        /// </summary>
        /// <param name="renderer">The PDF renderer.</param>
        /// <param name="heading">The heading block to be renderered.</param>
        protected override void Write(PdfBuilder renderer, HeadingBlock heading)
        {
            if (heading.Level < 0)
                throw new InvalidOperationException($"Heading level is negative (heading text: '{heading}')");

            if (heading.Inline.Any())
            {
                renderer.StartNewParagraph();

                renderer.SetHeadingLevel((uint)heading.Level);
                renderer.WriteChildren(heading.Inline);
                renderer.ClearHeadingLevel();

                renderer.StartNewParagraph();
            }
        }
    }
}
