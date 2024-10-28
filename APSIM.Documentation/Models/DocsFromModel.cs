using APSIM.Shared.Documentation.Extensions;
using System;
using System.Collections.Generic;
using APSIM.Interop.Markdown.Renderers;
using APSIM.Interop.Documentation;
using MigraDocCore.DocumentObjectModel;
using Models.Core;
using Models.Core.ApsimFile;
using System.IO;
using APSIM.Shared.Documentation;

namespace APSIM.Documentation.Models
{
    /// <summary>
    /// A pdf file which is built from a model.
    /// </summary>
    internal class DocsFromModel<T> : IDocumentationFile where T : IModel
    {
        /// <summary>
        /// Pdf generation options.
        /// </summary>
        private PdfOptions options;

        /// <summary>
        /// Display name of the file.
        /// </summary>
        public string Name { get; private set; } = "PDF";

        /// <summary>
        /// The output name of the file.
        /// </summary>
        public string OutputFileName { get; private set; }

        /// <summary>
        /// Create a new <see cref="DocsFromFile"/> instance for
        /// the given input files.
        /// </summary>
        /// <param name="output">Name of the file which will be generated.</param>
        /// <param name="options">Pdf generation options.</param>
        public DocsFromModel(string output, PdfOptions options)
        {
            this.options = options;
            OutputFileName = output;
        }

        /// <summary>
        /// Generate the auto-documentation at the given output path.
        /// </summary>
        /// <param name="path">Path to which the file will be generated.</param>
        public void Generate(string path)
        {
            // This document instance will be used to write all of the input files'
            // documentation to a single document.
            Document document = CreateDocument();
            PdfBuilder builder = new PdfBuilder(document, options);

            T model = Activator.CreateInstance<T>();
            foreach (ITag tag in DocumentModel(model))
                builder.Write(tag);
            builder.WriteBibliography();

            string outFile = Path.Combine(path, OutputFileName);
            PdfWriter.Save(document, outFile);
        }

        /// <summary>
        /// Document the model.
        /// </summary>
        /// <param name="model">Model to be documented.</param>
        protected virtual IEnumerable<ITag> DocumentModel(IModel model)
        {
            return AutoDocumentation.Document(model);
        }

        /// <summary>
        /// Create a standard document.
        /// </summary>
        protected virtual Document CreateDocument()
        {
            return PdfWriter.CreateStandardDocument();
        }
    }
}
