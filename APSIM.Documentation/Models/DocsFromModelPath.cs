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
using System.Linq;
using APSIM.Shared.Extensions.Collections;
using System.Diagnostics;

namespace APSIM.Documentation.Models
{
    /// <summary>
    /// A pdf file which is built from a model path and a file path.
    /// The file path should point to an .apsimx file on disk.
    /// The model path should point to a model inside this file. This model
    /// will be documented.
    /// </summary>
    internal class DocsFromModelPath : IDocumentationFile
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
        /// Path to an .apsimx file on disk.
        /// </summary>
        private string filePath;

        /// <summary>
        /// Path to a model in the file specified by
        /// <see cref="filePath"/>.
        /// </summary>
        private string modelPath;

        /// <summary>
        /// Should the rest of the file be documented? If true, the root model's
        /// docs will be written after those of the specified model. If false,
        /// only the specified model's documentation will be written.
        /// </summary>
        private bool documentRestOfFile;

        /// <summary>
        /// Create a new <see cref="DocsFromFile"/> instance for
        /// the given input files.
        /// </summary>
        /// <param name="filePath">Path to an .apsimx file on disk.</param>
        /// <param name="modelPath">Path to a model inside the .apsimx file.</param>
        /// <param name="output">Name of the file which will be generated.</param>
        /// <param name="options">Pdf generation options.</param>
        /// <param name="documentRestOfFile">Should the rest of the file be documented?</param>
        public DocsFromModelPath(string filePath, string modelPath, string output, PdfOptions options, bool documentRestOfFile)
        {
            this.options = options;
            OutputFileName = output;
            this.filePath = filePath;
            this.modelPath = modelPath;
            this.documentRestOfFile = documentRestOfFile;
        }

        /// <summary>
        /// Generate the auto-documentation at the given output path.
        /// </summary>
        /// <param name="path">Path to which the file will be generated.</param>
        public void Generate(string path)
        {
            // This document instance will be used to write all of the input files'
            // documentation to a single document.
            Document document = PdfWriter.CreateStandardDocument();
            PdfBuilder builder = new PdfBuilder(document, options);

            // Read the file.
            Simulations rootNode = FileFormat.ReadFromFile<Simulations>(filePath, e => throw e, false).NewModel as Simulations;

            // Attempt to resolve the model path inside the file.
            IVariable variable = rootNode.FindByPath(modelPath);

            // Ensure that we found a model.
            object result = variable?.Value;
            IModel model = result as IModel;
            if (variable == null)
                throw new Exception($"Failed to resolve path {modelPath} in file {filePath}");
            if (result == null)
                throw new Exception($"When resolving path {modelPath} in file {filePath}: {modelPath} resolved to null");
            if (model == null)
                throw new Exception($"Attempted to read model from path {modelPath} in file {filePath}, but the path resolves to an object of type {result.GetType()}");

            // Attempt to resolve links for the given model.
            rootNode.Links.Resolve(model, true, true, false);

            // Document the model.
            IEnumerable<ITag> tags = AutoDocumentation.Document(model);

            // Document the rest of the file afterwards if necessary.
            if (documentRestOfFile)
                tags = tags.AppendMany(AutoDocumentation.Document(rootNode));

            // Write tags to document.
            foreach (ITag tag in tags)
                builder.Write(tag);

            // Write bibliography at end of document.
            builder.WriteBibliography();

            // Write to PDF file at the specified path.
            string outFile = Path.Combine(path, OutputFileName);
            PdfWriter.Save(document, outFile);
        }
    }
}
