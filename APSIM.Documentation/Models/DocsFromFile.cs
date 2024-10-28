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
    /// A pdf file which is built from multiple input (.apsimx) files.
    /// </summary>
    internal class DocsFromFile : IDocumentationFile
    {
        /// <summary>
        /// Input files. Autodocs will be built for each of these files
        /// into a single document.
        /// </summary>
        private IEnumerable<string> inputFiles;

        /// <summary>
        /// Pdf generation options.
        /// </summary>
        private PdfOptions options;

        /// <summary>
        /// Display name of the file.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The output name of the file.
        /// </summary>
        public string OutputFileName { get; }

        /// <summary>
        /// Create a new <see cref="DocsFromFile"/> instance for the given input file.
        /// </summary>
        /// <param name="name">Name to show on web site.</param>
        /// <param name="input">The input file.</param>
        /// <param name="output">Name of the file which will be generated.</param>
        /// <param name="options">Pdf generation options.</param>
        public DocsFromFile(string name, string input, string output, PdfOptions options) : this(name, input.ToEnumerable(), output, options)
        {
        }

        /// <summary>
        /// Create a new <see cref="DocsFromFile"/> instance for
        /// the given input files.
        /// </summary>
        /// <param name="name">Name to show on web site.</param>
        /// <param name="inputs">Input files from which a document will be generated.</param>
        /// <param name="output">Name of the file which will be generated.</param>
        /// <param name="options">Pdf generation options.</param>
        public DocsFromFile(string name, IEnumerable<string> inputs, string output, PdfOptions options)
        {
            Name = name;
            inputFiles = inputs;
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

            foreach (string file in inputFiles)
            {
                string filePath = Path.GetDirectoryName(file);
                builder.ChangeOptions(new PdfOptions(filePath, options.CitationResolver));
                AppendToDocument(builder, file);
            }
            builder.WriteBibliography();

            string outFile = Path.Combine(path, OutputFileName);
            PdfWriter.Save(document, outFile);
        }

        /// <summary>
        /// Append the given input file to a pdf document.
        /// </summary>
        /// <param name="builder">Pdf builder API.</param>
        /// <param name="file">Input file.</param>
        private void AppendToDocument(PdfBuilder builder, string file)
        {
            try
            {
                Simulations model = FileFormat.ReadFromFile<Simulations>(file, e => throw e, false).NewModel as Simulations;

                // This is a hack. We can't resolve links for "validation" files
                // which contain experiments, sims, etc, because the simulations
                // aren't prepared for a run, so some links may not be resolvable.
                // Fortunately, components in these files generally don't require
                // links to be resolved in order to document themselves. Therefore,
                // we only attempt to resolve links for model resources (*.json).
                if (Path.GetExtension(file) == ".json")
                    model.Links.Resolve(model, true, true, false);

                foreach (ITag tag in DocumentModel(model))
                    builder.Write(tag);
            }
            catch (Exception err)
            {
                throw new Exception($"Unable to generate documentation for file {file}", err);
            }
        }

        protected virtual Document CreateDocument()
        {
            return PdfWriter.CreateStandardDocument();
        }

        /// <summary>
        /// Document the model.
        /// </summary>
        /// <param name="model">Model to be documented.</param>
        protected virtual IEnumerable<ITag> DocumentModel(IModel model)
        {
            return AutoDocumentation.Document(model);
        }
    }
}
