using APSIM.Shared.JobRunning;
using Models.Core.Run;
using Models.Core.Script;
using Models.Factorial;
using Models.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Models.Core
{
    /// <summary>
    /// # [Name]
    /// A simulation model
    /// </summary>
    [ValidParent(ParentType = typeof(Simulations))]
    [ValidParent(ParentType = typeof(Experiment))]
    [ValidParent(ParentType = typeof(Morris))]
    [ValidParent(ParentType = typeof(Sobol))]
    [Serializable]
    [ScopedModel]
    public class Simulation : Model, IRunnable, ISimulationDescriptionGenerator, ICustomDocumentation, IReportsStatus
    {
        [Link]
        private ISummary summary = null;

        [NonSerialized]
        private ScopingRules scope = null;

        /// <summary>Invoked when simulation is about to commence.</summary>
        public event EventHandler Commencing;

        /// <summary>Invoked to signal start of simulation.</summary>
        public event EventHandler<CommenceArgs> DoCommence;

        /// <summary>Invoked when the simulation is completed.</summary>
        public event EventHandler Completed;

        /// <summary>Return total area.</summary>
        public double Area
        {
            get
            {
                return this.FindAllChildren<Zone>().Sum(z => (z as Zone).Area);
            }
        }


        /// <summary>
        /// An enum that is used to indicate message severity when writing messages to the .db
        /// </summary>
        public enum ErrorLevel
        {
            /// <summary>Information</summary>
            Information,

            /// <summary>Warning</summary>
            Warning,

            /// <summary>Error</summary>
            Error
        };

        /// <summary>
        /// An enum that is used to indicate message severity when writing messages to the status window.
        /// </summary>
        public enum MessageType
        {
            /// <summary>Information</summary>
            Information,

            /// <summary>Warning</summary>
            Warning
        };

        /// <summary>Returns the object responsible for scoping rules.</summary>
        public ScopingRules Scope
        {
            get
            {
                if (scope == null)
                {
                    scope = new ScopingRules();
                }
                return scope;
            }
        }

        /// <summary>A locater object for finding models and variables.</summary>
        [NonSerialized]
        private Locater locater;

        /// <summary>Cache to speed up scope lookups.</summary>
        /// <value>The locater.</value>
        public Locater Locater
        {
            get
            {
                if (locater == null)
                {
                    locater = new Locater();
                }

                return locater;
            }
        }

        /// <summary>
        /// Returns the job's progress as a real number in range [0, 1].
        /// </summary>
        public double Progress
        {
            get
            {
                Clock c = this.FindChild<Clock>();
                if (c == null)
                    return 0;
                else
                    return c.FractionComplete;
            }
        }

        /// <summary>Is the simulation running?</summary>
        public bool IsRunning { get; private set; } = false;

        /// <summary>A list of keyword/value meta data descriptors for this simulation.</summary>
        public List<SimulationDescription.Descriptor> Descriptors { get; set; }

        /// <summary>Gets the value of a variable or model.</summary>
        /// <param name="namePath">The name of the object to return</param>
        /// <returns>The found object or null if not found</returns>
        public object Get(string namePath)
        {
            return Locater.Get(namePath, this);
        }

        /// <summary>Get the underlying variable object for the given path.</summary>
        /// <param name="namePath">The name of the variable to return</param>
        /// <returns>The found object or null if not found</returns>
        public IVariable GetVariableObject(string namePath)
        {
            return Locater.GetInternal(namePath, this);
        }

        /// <summary>Sets the value of a variable. Will throw if variable doesn't exist.</summary>
        /// <param name="namePath">The name of the object to set</param>
        /// <param name="value">The value to set the property to</param>
        public void Set(string namePath, object value)
        {
            Locater.Set(namePath, this, value);
        }

        /// <summary>Return the filename that this simulation sits in.</summary>
        /// <value>The name of the file.</value>
        [JsonIgnore]
        public string FileName { get; set; }

        /// <summary>Collection of models that will be used in resolving links. Can be null.</summary>
        [JsonIgnore]
        public List<object> Services { get; set; } = new List<object>();

        /// <summary>Status message.</summary>
        public string Status => FindAllDescendants<IReportsStatus>().FirstOrDefault(s => !string.IsNullOrEmpty(s.Status))?.Status;

        /// <summary>
        /// Simulation has completed. Clear scope and locator
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            ClearCaches();
        }

        /// <summary>
        /// Clears the existing Scoping Rules
        /// </summary>
        public void ClearCaches()
        {
            Scope.Clear();
            Locater.Clear();
        }

        /// <summary>Gets the next job to run</summary>
        public List<SimulationDescription> GenerateSimulationDescriptions()
        {
            var simulationDescription = new SimulationDescription(this);

            // Add a folderName descriptor.
            var folderNode = FindAncestor<Folder>();
            if (folderNode != null)
                simulationDescription.Descriptors.Add(new SimulationDescription.Descriptor("FolderName", folderNode.Name));

            simulationDescription.Descriptors.Add(new SimulationDescription.Descriptor("SimulationName", Name));

            foreach (var zone in this.FindAllDescendants<Zone>())
                simulationDescription.Descriptors.Add(new SimulationDescription.Descriptor("Zone", zone.Name));

            return new List<SimulationDescription>() { simulationDescription };
        }

        /// <summary>
        /// Runs the simulation on the current thread and waits for the simulation
        /// to complete before returning to caller. Simulation is NOT cloned before
        /// running. Use instance of Runner to get more options for running a 
        /// simulation or groups of simulations. 
        /// </summary>
        /// <param name="cancelToken">Is cancellation pending?</param>
        public void Run(CancellationTokenSource cancelToken = null)
        {
            // If the cancelToken is null then give it a default one. This can happen 
            // when called from the unit tests.
            if (cancelToken == null)
                cancelToken = new CancellationTokenSource();

            // Remove disabled models.
            RemoveDisabledModels(this);

            // If this simulation was not created from deserialisation then we need
            // to parent all child models correctly and call OnCreated for each model.
            bool hasBeenDeserialised = Children.Count > 0 && Children[0].Parent == this;
            if (!hasBeenDeserialised)
                Simulations.InitialiseModel(this);

            // Call OnPreLink in all models.
            // Note the ToList(). This is important because some models can
            // add/remove models from the simulations tree in their OnPreLink()
            // method, and FindAllDescendants() is lazy.
            FindAllDescendants().ToList().ForEach(model => model.OnPreLink());

            if (Services == null || Services.Count < 1)
            {
                var simulations = FindAncestor<Simulations>();
                if (simulations != null)
                    Services = simulations.GetServices();
                else
                {
                    Services = new List<object>();
                    IDataStore storage = this.FindInScope<IDataStore>();
                    if (storage != null)
                        Services.Add(this.FindInScope<IDataStore>());
                }
            }

            var links = new Links(Services);
            var events = new Events(this);

            try
            {
                // Connect all events.
                events.ConnectEvents();

                // Resolve all links
                links.Resolve(this, true);

                IsRunning = true;

                // Invoke our commencing event to let all models know we're about to start.
                Commencing?.Invoke(this, new EventArgs());

                // Begin running the simulation.
                DoCommence?.Invoke(this, new CommenceArgs() { CancelToken = cancelToken });
            }
            catch (Exception err)
            {
                // Exception occurred. Write error to summary.
                string errorMessage = "ERROR in file: " + FileName + Environment.NewLine +
                                      "Simulation name: " + Name + Environment.NewLine;
                errorMessage += err.ToString();

                summary?.WriteError(this, errorMessage);

                // Rethrow exception
                throw new Exception(errorMessage, err);
            }
            finally
            {
                // Signal that the simulation is complete.
                Completed?.Invoke(this, new EventArgs());

                // Disconnect our events.
                events.DisconnectEvents();

                // Unresolve all links.
                links.Unresolve(this, true);

                IsRunning = false;
            }
        }

        /// <summary>
        /// Remove all disabled child models from the specified model.
        /// </summary>
        /// <param name="model"></param>
        private void RemoveDisabledModels(IModel model)
        {
            model.Children.RemoveAll(child => !child.Enabled);
            model.Children.ForEach(child => RemoveDisabledModels(child));
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // document children
                foreach (IModel child in Children)
                    AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent);
            }
        }

        /// <summary>
        /// Gets the locater model.
        /// </summary>
        protected override Locater Locator()
        {
            return Locater;
        }
    }
}