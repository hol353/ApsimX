using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using APSIM.Interop.Documentation;
using APSIM.Interop.Markdown.Renderers.Blocks;
using APSIM.Interop.Markdown.Renderers.Inlines;
using APSIM.Shared.Utilities;
using Markdig.Renderers;
using Markdig.Syntax;
using APSIM.Interop.Documentation.Extensions;
using APSIM.Interop.Markdown.Renderers.Extras;
using APSIM.Interop.Documentation.Renderers;
using System.Diagnostics;
using APSIM.Interop.Utility;
using APSIM.Interop.Documentation.Helpers;

using MigraDocCore.DocumentObjectModel;
using MigraDocCore.DocumentObjectModel.Tables;
using MigraDocCore.DocumentObjectModel.MigraDoc.DocumentObjectModel.Shapes;
using static MigraDocCore.DocumentObjectModel.MigraDoc.DocumentObjectModel.Shapes.ImageSource;
using Color = MigraDocCore.DocumentObjectModel.Color;
using PdfSharpCore.Drawing;
using PdfSharpCore.Fonts;


namespace APSIM.Interop.Markdown.Renderers
{
    /// <summary>
    /// This class exposes an API for building a PDF document.
    /// It's used by the tag renderers and markdown object renderers.
    /// </summary>
    public class PdfBuilder : RendererBase
    {
        /// <summary>
        /// Static constructor to initialise PDFSharp ImageSource.
        /// </summary>
        static PdfBuilder()
        {

            if (ImageSource.ImageSourceImpl == null)
                ImageSource.ImageSourceImpl = new PdfSharpCore.Utils.ImageSharpImageSource<SixLabors.ImageSharp.PixelFormats.Rgba32>();

            // This is a bit tricky on non-Windows platforms. 
            // Normally PdfSharp tries to get a Windows DC for associated font information
            // See https://alex-maz.info/pdfsharp_150 for the work-around we can apply here.
            // See also http://stackoverflow.com/questions/32726223/pdfsharp-migradoc-font-resolver-for-embedded-fonts-system-argumentexception
            // The work-around is to register our own fontresolver. We don't need to do this on Windows.
            if (!ProcessUtilities.CurrentOS.IsWindows && !(GlobalFontSettings.FontResolver is FontResolver))
                GlobalFontSettings.FontResolver = new FontResolver();
        }

        /// <summary>
        /// This struct describes a link in the PDF document. This provides
        /// a convenient way to insert multiple pieces of formatted text
        /// into a single migradoc hyperlink object.
        /// </summary>
        private struct Link
        {
            /// <summary>
            /// Link URI.
            /// </summary>
            public string Uri { get; set; }

            /// <summary>
            /// The MigraDoc hyperlink object.
            /// </summary>
            public Hyperlink LinkObject { get; set; }
        }

        /// <summary>
        /// The citation helper used by this particular <see cref="PdfBuilder"/>
        /// instance. Normally this will be a pointer to <see cref="globalCitationHelper"/>
        /// unless the user specifies a custom bib file.
        /// </summary>
        private ICitationHelper citationHelper;

        /// <summary>
        /// Conversion factor from points to pixels.
        /// </summary>
        private const double pointsToPixels = 96.0 / 72;

        /// <summary>
        /// Padding between the table columns (proportional to column width).
        /// </summary>
        private const double columnPadding = 0.5;

        /// <summary>
        /// This is used for measure text extents in table cells,
        /// as MigraDoc does not support automatic column widths.
        /// </summary>
        private static readonly XGraphics graphics = XGraphics.CreateMeasureContext(new XSize(2000, 2000), XGraphicsUnit.Point, XPageDirection.Downwards);

        /// <summary>
        /// The PDF Document.
        /// </summary>
        private Document document;

        /// <summary>
        /// Keeps track of whether we need to start a new paragraph.
        /// </summary>
        /// <remarks>
        /// Certain object renderers need to ensure that their content goes into its own
        /// unique paragraph - and that subsequent renderers will be unable to write to
        /// this paragraph. To this end, they will call <see cref="StartNewParagraph"/>
        /// before and after writing their child objects to the paragraph. This causes a
        /// problem in table cells however, because an empty paragraph will cause the cell
        /// height to be far too high. This flag is a workaround for the issue; New
        /// paragraphs are only actually created when they are required (in calls to
        /// <see cref="GetLastParagraph"/>), iff this flag is set to true. Calls to
        /// <see cref="StartNewParagraph"/> simply set this to true.
        /// <remarks>
        private bool startNewParagraph;

        /// <summary>
        /// Style stack. This is used to manage styles for nested inline/block elements.
        /// This is exposed via the <see cref="PushStyle(TextStyle)"/> and 
        /// <see cref="PopStyle"/> functions.
        /// </summary>
        /// <typeparam name="TextStyle">Text styles.</typeparam>
        private Stack<TextStyle> styleStack = new Stack<TextStyle>();

        /// <summary>
        /// Link state. This allows for link style/state to be applied to nested
        /// children of a link container.
        /// </summary>
        /// <remarks>
        /// Technically we could use a stack here, as we do for text style. However,
        /// I'm not really sure what nested links look like, or how they should be
        /// rendered. So for now, nested links will throw a not implemented exception.
        /// </remarks>
        private Link? linkState = null;

        /// <summary>
        /// Current heading state. If not null, this is set to the heading level
        /// of the current text. This should really be incorporated somehow into TextStyle.
        /// </summary>
        private uint? headingLevel = null;

        /// <summary>
        /// Are we currently in a list item? If so, calls to <see cref="StartNewParagraph"/>
        /// won't actually create a new paragraph.
        /// </summary>
        private bool inListItem = false;

        /// <summary>
        /// Are we currently writing to a table cell?
        /// </summary>
        private bool inTableCell = false;

        /// <summary>
        /// Index of the current table cell being edited.
        /// </summary>
        /// <remarks>
        /// The way in which table cells are handled by migradoc is quite strange.
        /// </remarks>
        private int tableCellIndex = 0;

        /// <summary>
        /// When a heading is added to the document, these indices are used as
        /// the heading number (e.g. if the stack contains 1, 2, and 3, the
        /// heading will be written as "1.2.3. Heading Text").
        /// </summary>
        private Stack<uint> headingIndices = new Stack<uint>();

        /// <summary>
        /// Cache for looking up renderers based on tag type.
        /// </summary>
        /// <typeparam name="Type">Tag type.</typeparam>
        /// <typeparam name="ITagRenderer">Renderer instance capable of rendering the matching type.</typeparam>
        /// <returns></returns>
        private Dictionary<Type, ITagRenderer> renderersLookup = new Dictionary<Type, ITagRenderer>();

        /// <summary>
        /// Renderers which this PDF writer will use to write the tags to the PDF document.
        /// </summary>
        private IEnumerable<ITagRenderer> renderers;

        /// <summary>
        /// References which will be used to create the bibliography.
        /// </summary>
        /// <typeparam name="string">Short name of the reference.</typeparam>
        /// <typeparam name="ICitation">The full citation.</typeparam>
        private Dictionary<string, ICitation> references = new Dictionary<string, ICitation>();

        /// <summary>
        /// Number of figures in the document.
        /// </summary>
        internal uint FigureNumber { get; private set; } = 0;

        /// <summary>
        /// Create a <see cref="PdfBuilder" /> instance.
        /// </summary>
        /// <param name="doc"></param>
        public PdfBuilder(Document doc, PdfOptions options)
        {
            document = doc;

            headingIndices.Push(0);

            ObjectRenderers.Add(new ReferenceInlineRenderer());
            ObjectRenderers.Add(new AutolinkInlineRenderer());
            ObjectRenderers.Add(new CodeInlineRenderer());
            ObjectRenderers.Add(new EmphasisInlineRenderer());
            ObjectRenderers.Add(new HtmlEntityInlineRenderer());
            ObjectRenderers.Add(new HtmlInlineRenderer());
            ObjectRenderers.Add(new LineBreakInlineRenderer());
            ObjectRenderers.Add(new LinkInlineRenderer(string.Empty));
            ObjectRenderers.Add(new LiteralInlineRenderer());

            ObjectRenderers.Add(new CodeBlockRenderer());
            ObjectRenderers.Add(new HeadingBlockRenderer());
            ObjectRenderers.Add(new HtmlBlockRenderer());
            ObjectRenderers.Add(new ListBlockRenderer());
            ObjectRenderers.Add(new ParagraphBlockRenderer());
            ObjectRenderers.Add(new QuoteBlockRenderer());
            ObjectRenderers.Add(new ThematicBreakBlockRenderer());

            ObjectRenderers.Add(new TableCellRenderer());
            ObjectRenderers.Add(new TableRowRenderer());
            ObjectRenderers.Add(new TableRenderer());

            renderers = DefaultRenderers();

            ChangeOptions(options);
        }

        /// <summary>
        /// Use the given tag renderer to render applicable tags.
        /// </summary>
        /// <param name="tagRenderer">The tag renderer.</param>
        internal void UseTagRenderer(ITagRenderer tagRenderer)
        {
            if (tagRenderer == null)
                throw new ArgumentNullException(nameof(tagRenderer));
            renderers = renderers.Append(tagRenderer);
        }

        public void ChangeOptions(PdfOptions options)
        {
            if (options.CitationResolver == null)
                throw new ArgumentNullException(nameof(options.CitationResolver));

            citationHelper = options.CitationResolver;
            ObjectRenderers.Replace<LinkInlineRenderer>(new LinkInlineRenderer(options.ImagePath));
            renderers = renderers.Where(r => !(r is ImageTagRenderer)).Append(new ImageTagRenderer(options.ImagePath)).ToList();
        }

        /// <summary>
        /// Render the markdown object to the PDF document.
        /// </summary>
        /// <param name="markdownObject">The markdown object to be rendered.</param>
        public override object Render(MarkdownObject markdownObject)
        {
            Write(markdownObject);
            return document;
        }

        /// <summary>
        /// Push a style onto the style stack. This style will be applied to
        /// all additions to the PDF until it is removed, via <see cref="PopStyle"/>.
        /// </summary>
        /// <param name="style">The style to be added to the style stack.</param>
        /// <remarks>
        /// Virtual, to facilitate unit tests.
        /// </remarks>
        public virtual void PushStyle(TextStyle style)
        {
            styleStack.Push(style);
        }

        /// <summary>
        /// Pop a style from the style stack.
        /// </summary>
        public void PopStyle()
        {
            try
            {
                styleStack.Pop();
            }
            catch (Exception err)
            {
                throw new InvalidOperationException("Markdown renderer is missing a call to ApplyNestedStyle()", err);
            }
        }

        /// <summary>
        /// Set the current link state. It is an error to call this if the
        /// link state is already set (ie in the case of nested links).
        /// Every call to <see cref="SetLinkState"/> *must* have a matching call to
        /// <see cref="ClearLinkState"/>.
        /// </summary>
        /// <param name="linkUri">Link URI.</param>
        /// <remarks>
        /// Could remove virtual keyword if/when we extract an interface - it's
        /// really only here for unit tests.
        /// </remarks>
        public virtual void SetLinkState(string linkUri)
        {
            SetLinkState(linkUri, HyperlinkType.Web);
        }

        /// <summary>
        /// Start a bookmark with the given URI. It is an error to call this if the
        /// link state is already set. Every call to <see cref="StartBookmark(string)"/>
        /// *must* have a matching call to <see crf="ClearLinkState()"/>.
        /// </summary>
        /// <param name="uri">Bookmark URI (should start with a hash(#)).</param>
        public void StartBookmark(string uri)
        {
            if (!uri.StartsWith("#"))
                Debug.WriteLine($"WARNING: creating bookmark with uri {uri}, but URI doesn't start with a bookmark - this is probably a mistake.");
            SetLinkState(uri, HyperlinkType.Bookmark);
        }

        /// <summary>
        /// Clear the current link state. This should be called after rendering
        /// all children of a link.
        /// </summary>
        public void ClearLinkState()
        {
            if (linkState == null)
                // Should this be a Debug.WriteLine()?
                throw new InvalidOperationException("Renderer is missing a call to SetLinkState()");
            linkState = null;
        }

        /// <summary>
        /// Increment the heading depth. (e.g. to create a subheading.)
        /// </summary>
        public void PushSubHeading()
        {
            // It's possible to increment the heading depth twice in a row.
            // This would mean going from "1. ..." to "1.1.1. ...". To
            // allow this, we eneed to increment the toplevel heading index
            // iff it's zero. Normally the index is only incremented from zero
            // when we write the first heading at that particular depth. In
            // this case, there is no section 1.1, so we need to increment it
            // manually in order to prevent section 1.0.1 from occurring.
            if (headingIndices.Peek() == 0)
                IncrementHeadingIndex();

            headingIndices.Push(0);
            // Nothing else should be written to the previous paragraph.
            startNewParagraph = true;
        }

        /// <summary>
        /// Decrement the heading depth. (E.g. when finished with a subsection.)
        /// </summary>
        public void PopSubHeading()
        {
            if (headingIndices.Count <= 1)
                throw new InvalidOperationException("Unable to decrement heading depth: already at the toplevel");
            headingIndices.Pop();

            // We do not increment the heading indices at this time.
            // That will occur immediately before the heading is written.

            // Nothing else should be written to the previous paragraph.
            startNewParagraph = true;
        }

        /// <summary>
        /// Add a heading. This method is provided for convenience, but doesn't
        /// substyles in parts of the heading.
        /// </summary>
        /// <remarks>
        /// To add custom styling to parts of the heading, use:
        /// - <see cref="SetHeadingLevel"/>
        /// - <see cref="AppendText"/>
        /// - <see cref="ClearHeadingLevel"/>
        /// </remarks>
        /// <param name="text">Heading text.</param>
        public void AppendHeading(string text)
        {
            if (headingIndices.Count < 1)
                // Should maybe be a Debug.WriteLine();
                throw new InvalidOperationException("Programmer is missing a call to ClearHeadingLevel()");

            SetHeadingLevel((uint)headingIndices.Count);
            Paragraph paragraph = GetLastParagraph();
            AppendText(text, TextStyle.Normal, paragraph);
            paragraph.AddBookmark($"#{text}");
            ClearHeadingLevel();
            StartNewParagraph();
        }

        /// <summary>
        /// Set the heading level. All calls to this function should be accompanied
        /// by a call to <see cref="ClearHeadingLevel" />. It is an error to set the
        /// heading level if the heading level is already set (ie nested headings).
        /// 
        /// For subheadings (e.g. 1.2.3), use <see cref="PushSubHeading()"/> and
        /// <see cref="PopSubHeading"/>.
        /// </summary>
        /// <param name="headingLevel">Heading level.</param>
        public void SetHeadingLevel(uint headingLevel)
        {
            if (this.headingLevel != null)
                throw new NotImplementedException("Nested headings not supported.");
            this.headingLevel = headingLevel;

            // We can't increment the heading level after writing the heading, because
            // the user may want to add subheadings under this particular heading index.
            // Therefore, we need to increment the topmost heading index before writing
            // the heading text.
            IncrementHeadingIndex();
            WriteHeadingIndices();
        }

        /// <summary>
        /// Set the current link state. It is an error to call this if the
        /// link state is already set (ie in the case of nested links).
        /// Every call to <see cref="SetLinkState"/> *must* have a matching call to
        /// <see cref="ClearLinkState"/>.
        /// </summary>
        /// <param name="linkUri">Link URI.</param>
        /// <param name="linkType">Type of hyperlink to be created.</param>
        private void SetLinkState(string linkUri, HyperlinkType linkType)
        {
            if (linkState != null)
                throw new NotImplementedException("Nested links are not supported.");
            linkState = new Link()
            {
                Uri = linkUri,
                // todo: Implement other link types (ie local files)
                LinkObject = GetLastParagraph().AddHyperlink(linkUri, linkType)
            };
        }

        /// <summary>
        /// Increment the topmost heading index.
        /// </summary>
        private void IncrementHeadingIndex()
        {
            if (headingIndices.Any())
                headingIndices.Push(headingIndices.Pop() + 1);
            else
            {
                // This shouldn't happen in normal execution, but it could
                // occur due to programming error.
                Debug.WriteLine($"WARNING: heading index stack is empty. Programmer likely has mismatched calls to PushSubHeading() or PopSubHeading()");
                headingIndices.Push(0);
            }
        }

        /// <summary>
        /// Clear the currenet heading level. This should be called after rendering
        /// the contents of a heading.
        /// </summary>
        public void ClearHeadingLevel()
        {
            if (headingLevel == null)
                throw new InvalidOperationException("Heading level is already null. Renderer is missing a call to SetHeadingLevel().");
            headingLevel = null;
        }

        /// <summary>
        /// Start a new list item. Every call to this function must have a matching
        /// call to <see cref="FinishListItem()" />.
        /// </summary>
        /// <param name="bulletPointSymbol">The bullet point symbol to be used for the list item.</param>
        public void StartListItem(string bulletPointSymbol)
        {
            if (inListItem)
                throw new NotImplementedException("Nested lists not implemented (or programmer is missing a call to FinishListItem())");
            inListItem = true;
            GetLastParagraph().AddText(bulletPointSymbol);
        }

        /// <summary>
        /// Finish a list item.
        /// </summary>
        public void FinishListItem()
        {
            if (!inListItem)
                throw new NotImplementedException("Nested lists not implemented (or programmer is missing a call to StartListItem())");
            inListItem = false;
            GetLastParagraph().AddText("\n");
        }

        /// <summary>
        /// Append text to the PDF document, and create a bookmark to the text.
        /// </summary>
        /// <remarks>
        /// Can remove virtual if this is added to an interface. This is only
        /// required for unit tests.
        /// </remarks>
        /// <param name="text">Text to be appended.</param>
        /// <param name="textStyle">Style to be applied to the text.</param>
        public virtual void AppendReference(string text, TextStyle textStyle)
        {
            if (!references.TryGetValue(text, out ICitation citation))
            {
                citation = citationHelper.Lookup(text);
                references.Add(text, citation);
            }

            if (citation == null)
                // If no matching citation found, just insert plaintext.
                AppendText($"[{text}]", textStyle);
            else
            {
                // Don't link to the citation's URL. Always link to the full citation
                // in the bibliography, which may in turn link to the citation's
                // external URL (if it has one).
                SetLinkState(text, HyperlinkType.Bookmark);

                // Insert the citation's in-text version.
                AppendText(citation.InTextCite, textStyle);

                // Remove link state.
                ClearLinkState();
            }
        }

        /// <summary>
        /// Append text to the PDF document.
        /// </summary>
        /// <param name="text">Text to be appended.</param>
        /// <param name="textStyle">Style to be applied to the text.</param>
        /// <remarks>
        /// Virtual to facilitate unit tests, this could be removed if we
        /// extract an interface from this class...
        /// </remarks>
        public virtual void AppendText(string text, TextStyle textStyle)
        {
            AppendText(text, textStyle, GetLastParagraph());
        }

        /// <summary>
        /// Append an image to the PDF document.
        /// </summary>
        /// <param name="image">Image to be appended.</param>
        public void AppendImage(SkiaSharp.SKImage image)
        {
            AppendImage(image, GetLastParagraph());
        }

        /// <summary>
        /// Create a new table with the specified number of columns.
        /// Any calls to <see cref="StartTable"/> *must* have a matching call
        /// to <see cref="FinishTable"/>.
        /// </summary>
        /// <param name="numColumns">Number of columns in the table.</param>
        public void StartTable(int numColumns)
        {
            if (inTableCell)
                throw new NotImplementedException("Nested tables not implemented.");
            Table table = GetLastSection().AddTable();
            ApplyTableStyle(table);
            for (int i = 0; i < numColumns; i++)
                table.Columns.AddColumn();
        }

        /// <summary>
        /// Finish the given table. This indicates that any subsequent content
        /// is to be written after the table, rather than into the table.
        /// Any calls to <see cref="StartTable"/> *must* have a matching call
        /// to <see cref="FinishTable"/>.
        /// </summary>
        public void FinishTable()
        {
            // Now that all of the table cells have been rendered, we should
            // adjust the width partitioning. By default, all columns are given
            // equal width, but this is often wasteful.
            //
            // First, we find the widest cell in each column and record its
            // width. If the sum of these widths is less than the total page
            // width, then we can allocate these widths and be done with it.
            // Otherwise, we allocate widths based on the ratio of the column's
            // width demand to the total width demand, as a proportion of total
            // page width.
            Table table = GetLastSection().GetLastTable();
            List<double> maxWidths = Enumerable.Repeat<double>(0, table.Columns.Count).ToList();

            // The maximum of all minimum widths for the column. The minimum width is the
            // width of the column with text wrapping - ie the smallest the column can get
            // without text overflowing the cell boundaries.
            List<double> maxMinWidths = Enumerable.Repeat<double>(0, table.Columns.Count).ToList();

            // fixme: this font size doesn't account for the different text
            // sizes/formats that the contents of the cells can use.
            double fontSize = document.Styles.Normal.Font.Size;
            string fontName = document.Styles.Normal.Font.Name;
            XFont gdiFont = new XFont(fontName, fontSize);

            for (int i = 0; i < table.Rows.Count; i++)
            {
                for (int j = 0; j < table.Columns.Count; j++)
                {
                    Cell cell = table.Rows[i][j];
                    // Measure the text of the cell. todo: should probably account for
                    // style such as heading/bold which can theoretically affect font
                    // size. For now though, this is good enough.
                    IEnumerable<Paragraph> paragraphs = cell.Elements.OfType<Paragraph>();
                    foreach (string text in paragraphs.Select(p => p.GetRawText()))
                    {
                        // Allow an extra 20pt for column/cell padding.
                        Unit naturalWidth = Measure(text, gdiFont);
                        Unit minWidth = Measure(text, gdiFont, preferred: false);

                        if (naturalWidth > maxWidths[j])
                            maxWidths[j] = naturalWidth;

                        if (minWidth > maxMinWidths[j])
                            maxMinWidths[j] = minWidth;
                    }
                }
            }

            double totalWidth = maxWidths.Sum();
            GetPageSize(GetLastSection(), out double pageWidth, out _);
            pageWidth /= pointsToPixels;

            List<double> widths = Enumerable.Repeat(0d, maxWidths.Count).ToList();

            // Allocate space for all columns which cannot shrink (ie cannot wrap text).
            double totalRemainingRequestedSpace = 0;
            for (int i = 0; i < maxWidths.Count; i++)
            {
                if (MathUtilities.FloatsAreEqual(maxWidths[i], maxMinWidths[i]))
                    widths[i] = maxMinWidths[i];
                else
                    totalRemainingRequestedSpace += maxWidths[i];
            }
            // Now allocate space for the remaining columns proportional to the total remaining
            // requested and available space.
            double totalRemainingSpace = pageWidth - widths.Sum();
            for (int i = 0; i < maxWidths.Count; i++)
            {
                if (!MathUtilities.FloatsAreEqual(maxWidths[i], maxMinWidths[i]))
                    widths[i] = totalRemainingSpace * maxWidths[i] / totalRemainingRequestedSpace;
            }

            for (int i = 0; i < maxWidths.Count; i++)
                table.Columns[i].Width = widths[i];
        }

        /// <summary>
        /// Measure the width of a string as it would appear in the given font.
        /// </summary>
        /// <param name="text">A string to be measured.</param>
        /// <param name="gdiFont">Font used for measuring.</param>
        /// <param name="preferred">True to measure the preferred width (ie with no text wrapping), false to measure the min width (ie with maximal text wrapping).</param>
        private Unit Measure(string text, XFont gdiFont, bool preferred = true)
        {
            string toMeasure = preferred ? text : text.Split(' ').OrderByDescending(s => s.Length).FirstOrDefault();
            return Unit.FromPoint(20 + graphics.MeasureString(toMeasure, gdiFont).Width);
        }

        /// <summary>
        /// Start a new row in the current table.
        /// </summary>
        /// <param name="header"></param>
        public void StartTableRow(bool header)
        {
            try
            {
                if (inTableCell)
                    // The other possibility is that the renderer is missing a call to FinishCell().
                    throw new InvalidOperationException("Nested tables not implemented");

                Table table = GetLastSection().GetLastTable();
                if (table.Columns.Count == 0)
                    throw new InvalidOperationException("Table contains no columns");

                Row row = table.AddRow();
                if (header)
                    ApplyRowHeaderStyle(row);

                tableCellIndex = 0;
            }
            catch (Exception err)
            {
                throw new Exception("Unable to create a new table row", err);
            }
        }

        /// <summary>
        /// Start editing a table cell.
        /// </summary>
        public void StartTableCell()
        {
            if (inTableCell)
                // An exception here may be a bit harsh.
                throw new InvalidOperationException($"Programmer is missing a call to FinishTableCell().");

            // todo: Do we need to add cells to the row?
            inTableCell = true;
        }

        /// <summary>
        /// Finish editing a table cell.
        /// </summary>
        public void FinishTableCell()
        {
            if (!inTableCell)
                throw new InvalidOperationException($"Programmer is missing a call to StartTableCell().");

            // Cell cell = GetLastSection().GetLastTable().GetLastRow().Cells[tableCellIndex];
            // for (int i = cell.Elements.Count - 1; i >= 0; i--)
            // {
            //     if (cell.Elements[i] is Paragraph paragraph)
            //     {
            //         IEnumerable<Text> textElements = paragraph.GetTextElements();
            //         if (textElements.All(t => string.IsNullOrWhiteSpace(t.Content)))
            //             cell.Elements.RemoveObjectAt(i);
            //         break;
            //     }
            // }
            inTableCell = false;
            tableCellIndex++;
        }

        /// <summary>
        /// Append a table to the PDF document.
        /// </summary>
        /// <param name="data">Table to be appended.</param>
        public void AppendTable(DataTable data)
        {
            Table table = GetLastSection().AddTable();
            ApplyTableStyle(table);

            // Add the columns.
            foreach (DataColumn column in data.Columns)
                table.AddColumn();

            // Add a row containing column headings.
            Row row = table.AddRow();
            ApplyRowHeaderStyle(row);
            for (int i = 0; i < data.Columns.Count; i++)
                row[i].AddParagraph(data.Columns[i].ColumnName);

            // Add the data.
            foreach (DataRow dataRow in data.Rows)
            {
                StartTableRow(false);
                for (int i = 0; i < data.Columns.Count; i++)
                {
                    string text = dataRow[i]?.ToString() ?? "";
                    var paragraph = new APSIM.Shared.Documentation.Paragraph(text);
                    StartTableCell();
                    Write(paragraph);
                    FinishTableCell();
                }
            }

            // This will fix the column width partitioning.
            FinishTable();
        }

        /// <summary>
        /// Append a horizontal rule after the last paragraph.
        /// </summary>
        /// <remarks>
        /// This is only virtual to facilitate unit tests.
        /// If we ever extract an interface for pdf operations, we
        /// could make this non-virtual once more.
        /// </remarks>
        public virtual void AppendHorizontalRule()
        {
            Style hrStyle = GetHorizontalRuleStyle();
            GetLastParagraph().Format = hrStyle.ParagraphFormat.Clone();
            StartNewParagraph();
        }

        /// <summary>
        /// Start a new paragraph. Will do nothing if the last paragraph is empty.
        /// </summary>
        public void StartNewParagraph()
        {
            if (linkState != null)
                throw new InvalidOperationException("Unable to append text to new paragraph when linkState is set (how can a link span multiple paragraphs?). Renderer is missing a call to ClearLinkState().");

            // Never start a new paragraph while partway through a list item.
            // Need to rethink this, as it might have some unintended consequences
            // for certain object renderers (is a table allowed in a list item?).
            if (!inListItem)
                startNewParagraph = true;
        }

        /// <summary>
        /// Get the page width and height in pixels.
        /// </summary>
        /// <remarks>
        /// This is required by any tag renderer which needs to generate
        /// an image in memory (e.g. graphs, nutrient directed graph,
        /// map component, etc).
        /// </remarks>
        /// <param name="width">Page width (in px).</param>
        /// <param name="height">Page height (in px).</param>
        public void GetPageSize(out double width, out double height)
        {
            GetPageSize(GetLastSection(), out width, out height);
        }

        /// <summary>
        /// Add a bibliography to the document.
        /// </summary>
        public void WriteBibliography()
        {
            // Ensure that bibliography content does not go into
            // same paragraph as any existing content.
            StartNewParagraph();

            // Ensure references in bibliography are sorted alphabetically
            // by their full text.
            IEnumerable<KeyValuePair<string, ICitation>> sorted = references.Where(c => c.Value != null).OrderBy(c => c.Value.BibliographyText);

            if (sorted.Any())
                AppendHeading("References");


            foreach ((string name, ICitation citation) in sorted)
            {

                // If a URL is provided for this citation, insert the citation
                // as a hyperlink.
                bool isLink = !string.IsNullOrEmpty(citation.URL);
                if (isLink)
                    SetLinkState(citation.URL);

                // Write the citation text.
                AppendText(citation.BibliographyText, TextStyle.Bibliography);
                GetLastParagraph().AddBookmark(name);

                // Clear link state if a URL was provided.
                if (isLink)
                    ClearLinkState();

                // Ensure that any subsequent additions to the document
                // are written to a new paragraph.
                StartNewParagraph();
            }
        }

        /// <summary>
        /// Create a new paragraph and return it. If the most recent paragraph
        /// in the current context is empty, that paragraph will be returned
        /// instead, and a new paragraph will not be created or added to the
        /// document.
        /// </summary>
        private Paragraph CreateNewParagraph()
        {
            // Only add a new paragraph if the last paragraph contains any text.
            Paragraph lastParagraph = GetLastParagraph();
            if (lastParagraph.IsEmpty())
                return lastParagraph;
            else
            {
                Section section = GetLastSection();
                if (inTableCell)
                    return section.GetLastTable().GetLastRow().Cells[tableCellIndex].AddParagraph();
                else
                    return section.AddParagraph();
            }
        }

        /// <summary>
        /// Find an appropriate tag renderer, and use it to render the
        /// given tag to the PDF document.
        /// </summary>
        /// <param name="tag">Tag to be rendered.</param>
        public void Write(APSIM.Shared.Documentation.ITag tag)
        {
            ITagRenderer tagRenderer = GetTagRenderer(tag);
            tagRenderer.Render(tag, this);
        }

        /// <summary>
        /// Increment the figure count.
        /// </summary>
        public void IncrementFigureNumber()
        {
            FigureNumber++;
        }

        /// <summary>
        /// Write the heading indices (e.g. 1.2.3.2).
        /// </summary>
        private void WriteHeadingIndices()
        {
            AppendText($"{string.Join(".", headingIndices.Reverse())} ", TextStyle.Normal);
        }

        /// <summary>
        /// Get a tag renderer capcable of rendering the given tag.
        /// Throws if no suitable renderer is found.
        /// </summary>
        /// <param name="tag">The tag to be rendered.</param>
        private ITagRenderer GetTagRenderer(APSIM.Shared.Documentation.ITag tag)
        {
            Type tagType = tag.GetType();
            if (!renderersLookup.TryGetValue(tagType, out ITagRenderer tagRenderer))
            {
                tagRenderer = renderers.FirstOrDefault(r => r.CanRender(tag));
                if (tagRenderer == null)
                    throw new NotImplementedException($"Unknown tag type {tag.GetType()}: no matching renderers found.");
                renderersLookup[tagType] = tagRenderer;
            }
            return tagRenderer;
        }

        /// <summary>
        /// Get the default tag renderers.
        /// </summary>
        private static IEnumerable<ITagRenderer> DefaultRenderers()
        {
            List<ITagRenderer> result = new List<ITagRenderer>(7);
            result.Add(new ImageTagRenderer());
            result.Add(new ParagraphTagRenderer());
            result.Add(new TableTagRenderer());
            result.Add(new GraphTagRenderer());
            result.Add(new GraphPageTagRenderer());
            result.Add(new HeaderTagRenderer());
            result.Add(new SectionTagRenderer());
            result.Add(new MapTagRenderer());
            result.Add(new DirectedGraphTagRenderer());
            return result;
        }

        /// <summary>
        /// Append text to the given paragraph.
        /// </summary>
        /// <param name="text">Text to be appended.</param>
        /// <param name="textStyle">Style to be applied to the text.</param>
        /// <param name="paragraph">Paragraph to which the text should be appended.</param>
        private void AppendText(string text, TextStyle textStyle, Paragraph paragraph)
        {
            if (linkState == null)
            {
                string style = CreateStyle(textStyle);
                // We need to copy the paragraph format from the style to the paragraph,
                // otherwise it won't have an effect in paragraphs containing >1 text element.
                // Copy the paragraph format from the style, but not the font. The font should
                // be retrieved on a per-FormattedText instance basis. Otherwise, the font
                // of the last FormattedText in the paragraph will overwrite that of all others
                // in the paragraph. Technically, by doing this we're overwriting the paragraph
                // format of all earlier FormattedTexts anyway, but this is the best workaround
                // I could find.
                var font = paragraph.Format.Font.Clone();
                paragraph.Format = document.Styles[style].ParagraphFormat.Clone();
                paragraph.Format.Font = font;

                paragraph.Format.FirstLineIndent = document.Styles[style].ParagraphFormat.FirstLineIndent;
                paragraph.AddFormattedText(text, style);
            }
            else
                ((Link)linkState).LinkObject.AddFormattedText(text, CreateStyle(textStyle));
        }

        /// <summary>
        /// Append an image to the PDF document.
        /// </summary>
        /// <param name="image">Image to be appended.</param>
        /// <param name="paragraph">The paragraph to which the image should be appended.</param>
        private void AppendImage(SkiaSharp.SKImage image, Paragraph paragraph)
        {
            // The image could potentially be too large. Therfore we read it,
            // adjust the size to fit the page better (if necessary), and add
            // the modified image to the paragraph.

            GetPageSize(paragraph.Section, out double pageWidth, out double pageHeight);

            // Note: the first argument passed to the FromStream() function
            // is the name of the image. Thist must be unique throughout the document,
            // otherwise you will run into problems with duplicate imgages.
            IImageSource imageSource = ImageSource.FromStream(Guid.NewGuid().ToString().Replace("-", ""), () =>
            {
                image = ReadAndResizeImage(image, pageWidth, pageHeight);
                Stream stream = new MemoryStream(image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100).ToArray());
                stream.Seek(0, SeekOrigin.Begin);
                return stream;
            });
            if (linkState == null)
                paragraph.AddImage(imageSource);
            else
                ((Link)linkState).LinkObject.AddImage(imageSource);

        }

        /// <summary>
        /// Get the page size in pixels.
        /// </summary>
        /// <param name="section"></param>
        /// <param name="pageWidth"></param>
        /// <param name="pageHeight"></param>
        private static void GetPageSize(Section section, out double pageWidth, out double pageHeight)
        {
            PageSetup pageSetup = section.PageSetup;
            if (pageSetup.PageWidth.Point == 0 || pageSetup.PageHeight.Point == 0)
                pageSetup = section.Document.DefaultPageSetup;
            pageWidth = (pageSetup.PageWidth.Point - pageSetup.LeftMargin.Point - pageSetup.RightMargin.Point) * pointsToPixels;
            double spaceBelowParagraph = section.Document.Styles.Normal.ParagraphFormat.SpaceAfter.Point;
            pageHeight = (pageSetup.PageHeight.Point - pageSetup.TopMargin.Point - pageSetup.BottomMargin.Point - spaceBelowParagraph) * pointsToPixels;
            if (pageSetup.Orientation == Orientation.Landscape)
            {
                double tmp = pageWidth;
                pageWidth = pageHeight;
                pageHeight = tmp;
            }
        }

        /// <summary>
        /// Ensures that an image's dimensions are less than the specified target
        /// width and height, resizing the image as necessary without changing the
        /// aspect ratio.
        /// </summary>
        /// <param name="image">The image to be resized.</param>
        /// <param name="targetWidth">Max allowed width of the image in pixels.</param>
        /// <param name="targetHeight">Max allowed height of the image in pixels.</param>
        private static SkiaSharp.SKImage ReadAndResizeImage(SkiaSharp.SKImage image, double targetWidth, double targetHeight)
        {
            if ( (targetWidth > 0 && image.Width > targetWidth) || (targetHeight > 0 && image.Height > targetHeight) )
                image = ImageUtilities.ResizeImage(image, targetWidth, targetHeight);
            return image;
        }

        /// <summary>
        /// Return the last paragraph in the document. Adds a new paragraph if none
        /// exist in the document.
        /// </summary>
        private Paragraph GetLastParagraph()
        {
            if (startNewParagraph)
            {
                startNewParagraph = false;
                return CreateNewParagraph();
            }
            Section section = GetLastSection();
            if (inTableCell)
            {
                Table table = section.GetLastTable();
                if (tableCellIndex >= table.Columns.Count)
                    throw new InvalidOperationException($"Attempted to write past the final column in the table. Programmer is possibly missing a call to StartTableRow()");
                Cell cell = table.GetLastRow().Cells[tableCellIndex];
                Paragraph paragraph = cell.Elements.OfType<Paragraph>().LastOrDefault();
                if (paragraph == null)
                    return cell.AddParagraph();
                else
                    return paragraph;
            }
            else
            {
                if (section.LastParagraph == null)
                    return section.AddParagraph();
                return section.LastParagraph;   
            }
        }

        /// <summary>
        /// Return the last section in the document. Adds a new section if none exist
        /// in the document.
        /// </summary>
        private Section GetLastSection()
        {
            if (document.LastSection == null)
            {
                Section section = document.AddSection();
                // Due to what I would consider a bug in migradoc, a section's
                // elements collection is not initialised until we access it,
                // by either adding something, or just reading its value. Failure
                // to do one of these things will result in a NullReferenceException
                // being thrown if we, for example, try to get the last paragraph.
                _ = section.Elements;
            }
            return document.LastSection;
        }

        /// <summary>
        /// Get a font size for a given heading level.
        /// I'm not sure what units this is using - I've just copied
        /// it from existing code in ApsimNG.
        /// </summary>
        /// <param name="headingLevel">The heading level.</param>
        /// <returns></returns>
        protected Unit GetFontSizeForHeading(uint headingLevel)
        {
            switch (headingLevel)
            {
                case 1:
                    return 14;
                case 2:
                    return 12;
                case 3:
                    return 11;
                case 4:
                    return 10;
                case 5:
                    return 9;
                default:
                    return 8;
            }
        }

        /// <summary>
        /// Return an <see cref="OutlineLevel"/> appropriate for a heading of
        /// the given heading level (1 - 6). The outline level is used to
        /// show the heading in the document outline (aka table of contents).
        /// </summary>
        /// <param name="headingLevel">Heading level.</param>
        private OutlineLevel GetHeadingOutlineLevel(uint headingLevel)
        {
            switch (headingLevel)
            {
                case 1:
                    return OutlineLevel.Level1;
                case 2:
                    return OutlineLevel.Level2;
                case 3:
                    return OutlineLevel.Level3;
                case 4:
                    return OutlineLevel.Level4;
                case 5:
                    return OutlineLevel.Level5;
                case 6:
                    return OutlineLevel.Level6;
                default:
                    throw new NotImplementedException("Heading levels greater than 6 are not supported");
            }
        }

        /// <summary>
        /// Get the aggregated style from the style stack.
        /// </summary>
        private TextStyle GetNestedStyle()
        {
            if (styleStack.Any())
                return styleStack.Aggregate((x, y) => x | y);
            return TextStyle.Normal;
        }

        /// <summary>
        /// Create a style in the document corresponding to the given
        /// text style and return the name of the style.
        /// </summary>
        /// <param name="style">The style to be created.</param>
        protected string CreateStyle(TextStyle style)
        {
            style |= GetNestedStyle();
            string name = GetStyleName(style);
            if (string.IsNullOrEmpty(name))
                return document.Styles.Normal.Name;
            Style documentStyle = document.Styles[name];
            if (documentStyle == null)
                documentStyle = document.Styles.AddStyle(name, document.Styles.Normal.Name);
            //else /* if (!string.IsNullOrEmpty(baseName)) */
            //{
            //    documentStyle = document.Styles[name];
            //    if (documentStyle == null)
            //        documentStyle = document.Styles.AddStyle(name, baseName);
            //}

            if ( (style & TextStyle.Italic) == TextStyle.Italic)
                documentStyle.Font.Italic = true;
            if ( (style & TextStyle.Strong) == TextStyle.Strong)
                documentStyle.Font.Bold = true;
            if ( (style & TextStyle.Underline) == TextStyle.Underline)
                // MigraDoc actually supports different line styles.
                documentStyle.Font.Underline = Underline.Single;
            if ( (style & TextStyle.Strikethrough) == TextStyle.Strikethrough)
                throw new NotImplementedException();
            if ( (style & TextStyle.Superscript) == TextStyle.Superscript)
                documentStyle.Font.Superscript = true;
            if ( (style & TextStyle.Subscript) == TextStyle.Subscript)
                documentStyle.Font.Subscript = true;
            if ( (style & TextStyle.Quote) == TextStyle.Quote)
            {
                documentStyle.ParagraphFormat.FirstLineIndent = Unit.FromPoint(10);
                documentStyle.Font.Color = new Color(122, 130, 139);
                documentStyle.ParagraphFormat.Borders.Left.Visible = true;
                documentStyle.ParagraphFormat.Borders.Left.Color = Colors.DarkGray;
                documentStyle.ParagraphFormat.Borders.Left.Width = Unit.FromPoint(1);
                documentStyle.ParagraphFormat.Borders.DistanceFromBottom = Unit.FromPoint(5);
                documentStyle.ParagraphFormat.SpaceAfter = Unit.FromPoint(5);
            }
            if ( (style & TextStyle.Code) == TextStyle.Code)
            {
                // TBI - shading, syntax highlighting?
                documentStyle.Font.Name = "courier";
                documentStyle.ParagraphFormat.Borders.Width = Unit.FromPoint(0.5);
                documentStyle.ParagraphFormat.Borders.Color = Colors.DarkGray;
                documentStyle.ParagraphFormat.Borders.Visible = true;
                Unit padding = Unit.FromPoint(10);
                documentStyle.ParagraphFormat.Borders.DistanceFromBottom = padding;
                documentStyle.ParagraphFormat.Borders.DistanceFromTop = padding;
                documentStyle.ParagraphFormat.Borders.DistanceFromLeft = padding;
                documentStyle.ParagraphFormat.Borders.DistanceFromRight = padding;
            }
            if ( (style & TextStyle.Bibliography) == TextStyle.Bibliography)
            {
                // We indent all lines by 1cm, and also indent the first line by -1cm.
                // This has the effect of indenting all lines except the first line.
                documentStyle.ParagraphFormat.LeftIndent = Unit.FromCentimeter(1);
                documentStyle.ParagraphFormat.FirstLineIndent = Unit.FromCentimeter(-1);
            }
            // todo: do we need to set link style here?
            if (linkState != null)
            {
                // Links can be blue and underlined.
                documentStyle.Font.Color = new Color(0x08, 0x08, 0xef);
                // documentStyle.Font.Underline = Underline.Single;
            }
            if (headingLevel != null)
            {
                documentStyle.Font.Size = GetFontSizeForHeading((uint)headingLevel);
                documentStyle.Font.Bold = true;

                // Because some of the existing autodocs use >6 levels of nested headings,
                // I'm going to allow this practice to continue, with the caveat that any
                // such headings won't appear in the document outline. It would be better
                // to yield a warning or error in such cases.
                if (headingLevel <= 6)
                    documentStyle.ParagraphFormat.OutlineLevel = GetHeadingOutlineLevel((uint)headingIndices.Count);
            }

            return name;
        }

        /// <summary>
        /// Generate a name for the given style object. The style name generated by
        /// this function is deterministic in that if all style objects with the same
        /// property values will generate the same style names.
        /// 
        /// This means that the PDF document will have 1 style for bold text, 1 style
        /// for bold + italic text, etc.
        /// </summary>
        /// <param name="style">Text style.</param>
        protected string GetStyleName(TextStyle style)
        {
            string result = style.ToString().Replace(", ", "");
            if (linkState != null)
                result += "Link";
            if (headingLevel != null)
                result += $"Heading{headingLevel}";
            return result;
        }
    
        /// <summary>
        /// Get a horizontal rule style.
        /// </summary>
        /// <returns></returns>
        private Style GetHorizontalRuleStyle()
        {
            string styleName = "HorizontalRule";
            if (document.Styles[styleName] != null)
                return document.Styles[styleName];
            Style style = document.Styles.AddStyle(styleName, document.Styles.Normal.Name);
            Border hr = new Border();
            hr.Width = Unit.FromPoint(1);
            hr.Color = Colors.DarkGray;
            style.ParagraphFormat.Borders.Bottom = hr;
            style.ParagraphFormat.LineSpacing = 0;
            style.ParagraphFormat.SpaceBefore = 15;
            return style;
        }

        /// <summary>
        /// Apply the "standard" table style to the given table.
        /// </summary>
        /// <param name="table"></param>
        private static void ApplyTableStyle(Table table)
        {
            table.KeepTogether = true;
            table.Borders.Color = Colors.Black;
            table.Borders.Width = 0.5;
        }

        /// <summary>
        /// Make a row appear as a table header row.
        /// </summary>
        /// <param name="row">The row to be modified.</param>
        private void ApplyRowHeaderStyle(Row row)
        {
            row.HeadingFormat = true;
            row.Shading.Color = Colors.LightBlue;
            row.Format.Alignment = ParagraphAlignment.Left;
            row.VerticalAlignment = VerticalAlignment.Center;
            row.Format.Font.Bold = true;
            for (int i = 0; i < row.Table.Columns.Count; i++)
            {
                Cell cell = row.Cells[i];
                cell.Format.Font.Bold = true;
                cell.Format.Alignment = ParagraphAlignment.Left;
                cell.VerticalAlignment = VerticalAlignment.Center;
            }
        }
    }
}
