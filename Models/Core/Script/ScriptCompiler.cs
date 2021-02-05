namespace Models.Core.Script
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Encapsulates the ability to compile c# scripts into an assembly.
    /// </summary>
    /// <remarks>
    /// All c# scripts are compiled to a single assembly. This allows two-way
    /// referencing between c# scripts. 
    /// To use an instace of this class, callers will need to 'Add' their scripts.
    /// At some later point the APSIM infrastructure will compile all scripts 
    /// together and each caller (who add a script) will have their callback
    /// called to give them their newly created instance of their class.
    /// </remarks>
    [Serializable]
    public class ScriptCompiler
    {
        private static bool haveTrappedAssemblyResolveEvent = false;
        private static readonly object haveTrappedAssemblyResolveEventLock = new object();
        private const string tempFileNamePrefix = "APSIM";
        [NonSerialized]
        private CodeDomProvider provider;
        private readonly List<ModelScript> scripts = new List<ModelScript>();
        private static ScriptCompiler singletonInstance = new ScriptCompiler();
        private bool dirty = false;
        private CompilerResults result;

        /// <summary>Constructor.</summary>
        private ScriptCompiler()
        {
            // This looks weird but I'm trying to avoid having to call lock
            // everytime we come through here. If I remove this locking then
            // Jenkins runs very slowly (5 times slower for each sim). Presumably
            // this is because each simulation being run (from APSIMRunner) does the 
            // cleanup below.
            if (!haveTrappedAssemblyResolveEvent)
            {
                lock (haveTrappedAssemblyResolveEventLock)
                {
                    if (!haveTrappedAssemblyResolveEvent)
                    {
                        haveTrappedAssemblyResolveEvent = true;

                        // Trap the assembly resolve event.
                        AppDomain.CurrentDomain.AssemblyResolve -= new ResolveEventHandler(ResolveManagerAssemblies);
                        AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(ResolveManagerAssemblies);

                        // Clean up apsimx manager .dll files.
                        Cleanup();
                    }
                }
            }
        }

        /// <summary>Singleton instance.</summary>
        public static ScriptCompiler Instance => singletonInstance;

        /// <summary>Singleton instance.</summary>
        public bool DidCompile { get; private set; } = false;

        /// <summary>Additional assemblies to reference while compiling.</summary>
        public IEnumerable<string> ReferencedAssemblies { get; set; }

        /// <summary>Compile a c# script.</summary>
        /// <param name="code">The c# code to compile.</param>
        /// <param name="name">The name of the model owning the script. Used only in error messages.</param>
        /// <param name="onInstanceCreatedCallback">The callback to be invoked when a new instance of the script class is created.</param>
        /// <returns>Compile errors or null if no errors.</returns>
        public void AddScript(string code, string name, Action<object> onInstanceCreatedCallback)
        {
            if (code != null)
            {
                // If we already have this code, add the callback argument to the existing 
                // If we don't have this code, create a new ModelScript instance.
                var existingScript = scripts.Find(c => c.Hash == code.GetHashCode());
                if (existingScript == null)
                {
                    scripts.Add(new ModelScript(code, name, onInstanceCreatedCallback));
                    dirty = true;
                }
                else
                    existingScript.AddCallBack(name, onInstanceCreatedCallback);
            }
        }

        /// <summary>
        /// Compile the scripts. If successfull, all callbacks will be invoked. If not
        /// successfull, error messages will be returned.
        /// </summary>
        /// <returns>Error messages or null on successful compilation.</returns>
        public string Compile()
        {
            if (dirty)
            {
                dirty = false;
                DidCompile = true;

                // Compile all code.
                result = CompileTextToAssembly(GetAllCode(),
                                               GetReferenceAssemblies(ReferencedAssemblies, null));

                // If not successfull, return error messages.
                if (result.Errors.Count > 0)
                    return CreateErrorMessage(result);
            }
            else
                DidCompile = false;

            // If we have a compiled assembly then invoke all callbacks.
            if (result?.CompiledAssembly != null)
                scripts.ForEach(script => script.InvokeCallbacks(result.CompiledAssembly));

            return null;
        }

        /// <summary>Create an error message.</summary>
        /// <param name="result">Compiler results.</param>
        private string CreateErrorMessage(CompilerResults result)
        {
            var stringBuilder = new StringBuilder();
            foreach (CompilerError err in result.Errors)
            {
                // Determine which script the error applies to.
                var scriptContainingError = scripts.Find(script => script.ContainsLine(err.Line));
                stringBuilder.Append(scriptContainingError.WriteErrorMessage(err));
            }
            return stringBuilder.ToString();
        }

        /// <summary>A handler to resolve the loading of manager assemblies when binary deserialization happens.</summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public static Assembly ResolveManagerAssemblies(object sender, ResolveEventArgs args)
        {
            foreach (string fileName in Directory.GetFiles(Path.GetTempPath(), tempFileNamePrefix + "*.dll"))
                if (args.Name.Split(',')[0] == Path.GetFileNameWithoutExtension(fileName))
                    return Assembly.LoadFrom(fileName);
            return null;
        }

        /// <summary>Gets a list of assembly names that are needed for compiling.</summary>
        /// <param name="referencedAssemblies"></param>
        /// <param name="modelName">Name of model.</param>
        private IEnumerable<string> GetReferenceAssemblies(IEnumerable<string> referencedAssemblies, string modelName)
        {
            IEnumerable<string> references = new string[] 
            {
                "System.dll", 
                "System.Xml.dll", 
                "System.Windows.Forms.dll",
                "System.Data.dll", 
                "System.Core.dll", 
                Assembly.GetExecutingAssembly().Location,
                Assembly.GetEntryAssembly()?.Location,             // Not sure why this can be null in unit tests.
                typeof(MathNet.Numerics.Fit).Assembly.Location,
                typeof(APSIM.Shared.Utilities.MathUtilities).Assembly.Location,
                typeof(Newtonsoft.Json.JsonIgnoreAttribute).Assembly.Location,
                typeof(System.Drawing.Color).Assembly.Location,
            };

            // if (scripts != null)
            //     references = references.Concat(scripts.Where(p => !p.ModelFullPath.Contains($".{modelName}"))
            //                                          .Select(p => p.CompiledAssembly.Location));

            if (referencedAssemblies != null)
                references = references.Concat(referencedAssemblies);
            
            return references.Where(r => r != null);
        }

        /// <summary>Create c# code for all scripts.</summary>
        private string GetAllCode()
        {
            var stringBuilder = new StringBuilder();

            // Collect all using statements and c# code from all script instances.
            var usings = new List<string>();
            var allScript = new StringBuilder();
            int lineNumber = 1;
            foreach (var script in scripts)
            {
                script.StartLineNumberInOverallScript = lineNumber;
                usings.AddRange(script.Parser.GetUsingStatements());
                allScript.Append(script.Parser.GetScriptMinusUsings());

                lineNumber += script.Parser.NumberOfLines;
            }

            // Write all distinct using statements.
            foreach (var usingStatement in usings.Distinct())
                stringBuilder.AppendLine($"using {usingStatement};");

            // Write all class declarations.
            stringBuilder.AppendLine(allScript.ToString());

            // Return all script.
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Compile the specified 'code' into an executable assembly. If 'assemblyFileName'
        /// is null then compile to an in-memory assembly.
        /// </summary>
        /// <param name="code">The code to compile.</param>
        /// <param name="referencedAssemblies">Any referenced assemblies.</param>
        /// <returns>Any compile errors or null if compile was successful.</returns>
        private CompilerResults CompileTextToAssembly(string code, IEnumerable<string> referencedAssemblies = null)
        {
            if (provider == null)
                provider = CodeDomProvider.CreateProvider(CodeDomProvider.GetLanguageFromExtension(".cs"));

            var assemblyFileNameToCreate = Path.ChangeExtension(Path.Combine(Path.GetTempPath(), tempFileNamePrefix + Guid.NewGuid().ToString()), ".dll");

            CompilerParameters parameters = new CompilerParameters
            {
                GenerateInMemory = false,
                OutputAssembly = assemblyFileNameToCreate
            };
            string sourceFileName = Path.ChangeExtension(assemblyFileNameToCreate, ".cs");
            File.WriteAllText(sourceFileName, code);

            parameters.OutputAssembly = Path.ChangeExtension(assemblyFileNameToCreate, ".dll");
            parameters.TreatWarningsAsErrors = false;
            parameters.IncludeDebugInformation = true;
            parameters.WarningLevel = 2;
            foreach (var referencedAssembly in referencedAssemblies)
                parameters.ReferencedAssemblies.Add(referencedAssembly);
            parameters.TempFiles = new TempFileCollection(Path.GetTempPath());  // ensure that any temp files are in a writeable area
            parameters.TempFiles.KeepFiles = false;
            return provider.CompileAssemblyFromFile(parameters, new string[] { sourceFileName });
        }

        /// <summary>Cleanup old files.</summary>
        private void Cleanup()
        {
            // Clean up old files.
            var filesToCleanup = new List<string>();
            filesToCleanup.AddRange(Directory.GetFiles(Path.GetTempPath(), $"{tempFileNamePrefix}*.dll"));
            filesToCleanup.AddRange(Directory.GetFiles(Path.GetTempPath(), $"{tempFileNamePrefix}*.cs"));
            filesToCleanup.AddRange(Directory.GetFiles(Path.GetTempPath(), $"{tempFileNamePrefix}*.pdb"));

            foreach (string fileName in filesToCleanup)
            {
                try
                {
                    TimeSpan timeSinceLastAccess = DateTime.Now - File.GetLastAccessTime(fileName);
                    if (timeSinceLastAccess.Hours > 1)
                        File.Delete(fileName);
                }
                catch (Exception)
                {
                    // File locked?
                }
            }
        }

        /// <summary>Encapsulates a previous compilation.</summary>
        [Serializable]
        private class ModelScript
        {
            /// <summary>The model name.</summary>
            private List<string> names = new List<string>();

            /// <summary>Collection of callbacks to be invoked when script is compiled.</summary>
            private List<Action<object>> callbacks = new System.Collections.Generic.List<Action<object>>();

            /// <summary>Constructor</summary>
            /// <param name="code">The c# script code.</param>
            /// <param name="nameOfScript">The name of the script - used in error messages.</param>
            /// <param name="onInstanceCreatedCallback">The callback to be invoked when a new instance of the script class is created.</param>
            public ModelScript(string code, string nameOfScript, Action<object> onInstanceCreatedCallback)
            {
                Hash = code.GetHashCode();
                Parser = new ScriptParser(code);
                AddCallBack(nameOfScript, onInstanceCreatedCallback);
            }

            /// <summary>Add a callback.</summary>
            /// <param name="nameOfScript">The name of the script - used in error messages.</param>
            /// <param name="onInstanceCreatedCallback">The callback to be invoked when a new instance of the script class is created.</param>
            public void AddCallBack(string nameOfScript, Action<object> onInstanceCreatedCallback)
            {
                //if (!names.Contains(nameOfScript))
                {
                    names.Add(nameOfScript);
                    callbacks.Add(onInstanceCreatedCallback);
                }
            }

            /// <summary>The hash of c# code.</summary>
            public int Hash { get; }

            /// <summary>The code that was compiled.</summary>
            public ScriptParser Parser { get; }
            
            /// <summary>Gets or set the starting line number where the script is positioned in the overall script.</summary>
            public int StartLineNumberInOverallScript { get; set; }
            
            /// <summary>Invoke all callbacks.</summary>
            /// <param name="compiledAssembly">The compiled assembly that contains this script.</param>
            public void InvokeCallbacks(Assembly compiledAssembly)
            {
                var className = Parser.GetClassName();
                if (className != null)
                {
                    var instanceType = compiledAssembly.GetTypes().ToList().Find(t => t.Name == className);
                    foreach (var callback in callbacks)
                    {
                        // Create an instance of the class and give it to the model.
                        callback.Invoke(compiledAssembly.CreateInstance(instanceType.FullName));
                    }
                }
            }

            /// <summary>Is the line number (relative to overall script) part of this script?</summary>
            /// <param name="lineNumberRelativeToOverallScript"></param>
            internal bool ContainsLine(int lineNumberRelativeToOverallScript)
            {
                return lineNumberRelativeToOverallScript >= StartLineNumberInOverallScript &&
                       lineNumberRelativeToOverallScript < StartLineNumberInOverallScript + Parser.NumberOfLines;
            }

            /// <summary>Write an error message.</summary>
            /// <param name="err">The compiler error.</param>
            public string WriteErrorMessage(CompilerError err)
            {
                int lineNumberRelativeToThisScript = err.Line - StartLineNumberInOverallScript + 1;
                var stringBuilder = new StringBuilder();
                foreach (var name in names)
                    stringBuilder.AppendLine($"{name}, Line {lineNumberRelativeToThisScript}: {err.ErrorText}");
                return stringBuilder.ToString();
            }
        }
    }
}