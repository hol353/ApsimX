using APSIM.Cli.Options;
using APSIM.Interop.Documentation;
using APSIM.Interop.Documentation.Formats;
using APSIM.Shared.Documentation;
using APSIM.Shared.Utilities;
using CommandLine;
using Models.Core;
using Models.Core.Apsim710File;
using Models.Core.ApsimFile;
using Models.Core.Run;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using APSIM.Documentation.Models;

namespace APSIM.Cli
{
    class Program
    {
        private static int exitCode = 0;

        static int Main(string[] args)
        {
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                new Parser(config =>
                {
                    config.AutoHelp = true;
                    config.HelpWriter = Console.Out;
                }).ParseArguments<RunOptions, DocumentOptions, ImportOptions>(args)
                .WithParsed<RunOptions>(Run)
                .WithParsed<DocumentOptions>(Document)
                .WithParsed<ImportOptions>(Import)
                .WithNotParsed(HandleParseError);
            }
            catch (Exception err)
            {
                Console.Error.WriteLine(err.ToString());
                exitCode = 1;
            }
            return exitCode;
        }

        /// <summary>
        /// Handles parser errors to ensure that a non-zero exit code
        /// is returned when parse errors are encountered.
        /// </summary>
        /// <param name="errors">Parse errors.</param>
        private static void HandleParseError(IEnumerable<Error> errors)
        {
            if ( !(errors.IsHelp() || errors.IsVersion()) )
                exitCode = 1;
        }

        private static void Run(RunOptions options)
        {
            if (options.Files == null || !options.Files.Any())
            {
                throw new ArgumentException($"No files were specified");
            }
            IEnumerable<string> files = options.Files.SelectMany(f => DirectoryUtilities.FindFiles(f, options.Recursive));
            if (!files.Any())
            {
                files = options.Files;
            }
            foreach (string file in files)
            {
                Simulations sims = FileFormat.ReadFromFile<Simulations>(file, 
                                        e => throw new Exception($"Error while trying to run {file}", e), false).NewModel as Simulations;

                Runner runner = new Runner(sims);
                List<Exception> errors = runner.Run();

                if (errors != null && errors.Count > 0)
                {
                    throw new AggregateException("File ran with errors", errors);
                }
            }
        }

        private static void Document(DocumentOptions options)
        {
            if (options.Files == null || !options.Files.Any())
                throw new ArgumentException($"No files were specified");
            IEnumerable<string> files = options.Files.SelectMany(f => DirectoryUtilities.FindFiles(f, options.Recursive));
            if (!files.Any())
                files = options.Files;
            foreach (string file in files)
            {
                Simulations sims = FileFormat.ReadFromFile<Simulations>(file, e => throw e, false).NewModel as Simulations;
                IModel model = sims;
                if (Path.GetExtension(file) == ".json")
                    sims.Links.Resolve(sims, true, true, false);
                if (!string.IsNullOrEmpty(options.Path))
                {
                    IVariable variable = model.FindByPath(options.Path);
                    if (variable == null)
                        throw new Exception($"Unable to resolve path {options.Path}");
                    object value = variable.Value;
                    if (value is IModel modelAtPath)
                        model = modelAtPath;
                    else
                        throw new Exception($"{options.Path} resolved to {value}, which is not a model");
                }

                string pdfFile = Path.ChangeExtension(file, ".pdf");
                string directory = Path.GetDirectoryName(file);
                PdfWriter writer = new PdfWriter(new PdfOptions(directory, null));
                IEnumerable<ITag> tags = options.ParamsDocs ? new ParamsInputsOutputs(model).Document() : AutoDocumentation.Document(model);
                // Make params/inputs/outputs docs landscape (they have some rather wide tables). Everything else, portrait.
                bool vertical = !options.ParamsDocs;
                writer.Write(pdfFile, tags, vertical);
            }
        }

        /// <summary>
        /// Import the files using the given options.
        /// </summary>
        /// <param name="options">Parsed CLI arguments.</param>
        private static void Import(ImportOptions options)
        {
            if (options.Files == null || !options.Files.Any())
                throw new ArgumentException($"No files were specified");

            IEnumerable<string> files = options.Files.SelectMany(f => DirectoryUtilities.FindFiles(f, options.Recursive));
            if (!files.Any())
                files = options.Files;

            foreach (string file in files)
            {
                var importer = new Importer();
                importer.ProcessFile(file, e=> { Console.WriteLine(e.ToString()); });
            }
        }
    }
}
