namespace Models.Core.Run
{
    using APSIM.Shared.JobRunning;
    using Models.Soils.Standardiser;
    using Models.Storage;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// Encapsulates all the bits that are need to construct a simulation
    /// and the associated metadata describing a simulation.
    /// </summary>
    [Serializable]
    public class SimulationDescription : IRunnable, IReportsStatus
    {
        /// <summary>The top level simulations instance.</summary>
        private IModel topLevelModel;

        /// <summary>The base simulation.</summary>
        private Simulation baseSimulation;

        /// <summary>A list of all replacements to apply to simulation to run.</summary>
        private List<IReplacement> replacementsToApply = new List<IReplacement>();

        /// <summary>Do we clone the simulation before running?</summary>
        private bool doClone;

        /// <summary>
        /// The actual simulation object to run
        /// </summary>
        public Simulation SimulationToRun { get; private set; } = null;

        /// <summary>
        /// Returns the job's progress as a real number in range [0, 1].
        /// </summary>
        public double Progress
        {
            get
            {
                return SimulationToRun?.Progress ?? 0;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sim">The simulation to run.</param>
        /// <param name="name">The name of the simulation.</param>
        /// <param name="clone">Clone the simulation passed in before running?</param>
        public SimulationDescription(Simulation sim, string name = null, bool clone = true)
        {
            baseSimulation = sim;
            if (sim != null)
            {
                IModel topLevel = sim;
                while (topLevel.Parent != null)
                    topLevel = topLevel.Parent;
                topLevelModel = topLevel;
            }

            if (name == null && baseSimulation != null)
                Name = baseSimulation.Name;
            else
                Name = name;
            doClone = clone;
        }

        /// <summary>name</summary>
        public string Name { get; }

        /// <summary>Gets / sets the list of descriptors for this simulaton.</summary>
        public List<Descriptor> Descriptors { get; set; } = new List<Descriptor>();

        /// <summary>Gets or sets the DataStore for this simulaton.</summary>
        public IDataStore Storage
        {
            get
            {
                var scope = new ScopingRules();
                return scope.FindAll(baseSimulation).First(model => model is IDataStore) as IDataStore;
            }
        }

        /// <summary>Status message.</summary>
        public string Status => SimulationToRun?.Status;

        /// <summary>
        /// Add an override to replace an existing model, as specified by the
        /// path, with a replacement model.
        /// </summary>
        /// <param name="replacement">An instance of a replacement that needs to be applied when simulation is run.</param>
        public void AddOverride(IReplacement replacement)
        {
            replacementsToApply.Add(replacement);
        }

        /// <summary>
        /// Add a property override to replace an existing value, as specified by a
        /// path.
        /// </summary>
        /// <param name="path">The path to use to locate the model to replace.</param>
        /// <param name="replacement">The model to use as the replacement.</param>
        public void AddOverride(string path, object replacement)
        {
            replacementsToApply.Add(new PropertyReplacement(path, replacement));
        }

        /// <summary>Run the simulation.</summary>
        /// <param name="cancelToken"></param>
        public void Run(CancellationTokenSource cancelToken)
        {
            SimulationToRun = ToSimulation();
            SimulationToRun.Run(cancelToken);
        }

        /// <summary>
        /// Convert the simulation decription to a simulation.
        /// path.
        /// </summary>
        public Simulation ToSimulation()
        {
            try
            {
                AddReplacements();

                Simulation newSimulation;
                if (doClone)
                {
                    newSimulation = Apsim.Clone(baseSimulation) as Simulation;

                    // After a binary clone, we need to force all managers to
                    // recompile their scripts. This is to work around an issue
                    // where scripts will change during deserialization. See issue
                    // #4463 and the TestMultipleChildren test inside ReportTests.
                    Simulations.InitialiseModel(newSimulation);
                }
                else
                    newSimulation = baseSimulation;

                if (string.IsNullOrWhiteSpace(Name))
                    newSimulation.Name = baseSimulation.Name;
                else
                    newSimulation.Name = Name;

                newSimulation.Parent = null;
                newSimulation.ParentAllDescendants();
                replacementsToApply.ForEach(r => r.Replace(newSimulation));

                // Give the simulation the descriptors.
                if (newSimulation.Descriptors == null || Descriptors.Count > 0)
                    newSimulation.Descriptors = Descriptors;
                newSimulation.Services = GetServices();

                // Standardise the soil.
                var soils = newSimulation.FindAllDescendants<Soils.Soil>();
                foreach (Soils.Soil soil in soils)
                    SoilStandardiser.Standardise(soil);

                newSimulation.ClearCaches();
                return newSimulation;
            }
            catch (Exception err)
            {
                var message = "Error in file: " + baseSimulation.FileName + " Simulation: " + Name;
                throw new Exception(message, err);
            }
        }

        private List<object> GetServices()
        {
            List<object> services = new List<object>();
            if (topLevelModel is Simulations sims)
            {
                // If the top-level model is a simulations object, it will have access
                // to services such as the checkpoints. This should be passed into the
                // simulation to be used in link resolution. If we don't provide these
                // services to the simulation, it will not be able to resolve links to
                // checkpoints.
                services = sims.GetServices();
            }
            else
            {
                IModel storage = topLevelModel.FindInScope<DataStore>();
                services.Add(storage);
            }

            return services;
        }

        /// <summary>
        /// Return true if this simulation has a descriptor.
        /// </summary>
        /// <param name="descriptor">The descriptor to search for.</param>
        public bool HasDescriptor(Descriptor descriptor)
        {
            return Descriptors.Find(d => d.Name == descriptor.Name && d.Value == descriptor.Value) != null;
        }

        /// <summary>Add any replacements to all simulation descriptions.</summary>
        private void AddReplacements()
        {
            if (topLevelModel != null)
            {
                IModel replacements = topLevelModel.FindChild<Replacements>();
                if (replacements != null && replacements.Enabled)
                {
                    foreach (IModel replacement in replacements.Children)
                    {
                        var modelReplacement = new ModelReplacement(null, replacement);
                        replacementsToApply.Insert(0, modelReplacement);
                    }
                }
            }
        }

        /// <summary>Encapsulates a descriptor for a simulation.</summary>
        [Serializable]
        public class Descriptor
        {
            /// <summary>The name of the descriptor.</summary>
            public string Name { get; set; }

            /// <summary>The value of the descriptor.</summary>
            public string Value { get; set; }

            /// <summary>Constructor</summary>
            /// <param name="name">Name of the descriptor.</param>
            /// <param name="value">Value of the descriptor.</param>
            public Descriptor(string name, string value)
            {
                Name = name;
                Value = value;
            }
        }


        /// <summary>Compare two list of descriptors for equality.</summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>true if the are the same.</returns>
        public static bool Equals(List<SimulationDescription.Descriptor> x, List<SimulationDescription.Descriptor> y)
        {
            if (x.Count != y.Count)
                return false;
            for (int i = 0; i < x.Count; i++)
            {
                if (x[i].Name != y[i].Name ||
                    x[i].Value != y[i].Value)
                    return false;
            }
            return true;
        }

    }
}
