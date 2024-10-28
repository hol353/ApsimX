using System;
using System.Collections.Generic;
using System.Linq;
using Models.Core;
using Newtonsoft.Json;

namespace Models.PMF
{
    /// <summary>
    /// A cultivar model - used to override properties of another model (typically a plant) at runtime.
    /// </summary>
    /// <remarks>
    /// This includes aliases to indicate other common crop names and commands to specify genotype parameters.
    /// The format of commands is "name=value". The "name" of parameter should include the full path under
    /// Plant function, e.g. [Phenology].Vernalisation.PhotopSens = 3.5.
    /// </remarks>
    [Serializable]
    [ViewName("UserInterface.Views.EditorView")]
    [PresenterName("UserInterface.Presenters.EditorPresenter")]
    [ValidParent(ParentType = typeof(Plant))]
    [ValidParent(ParentType = typeof(GrazPlan.Stock))]
    [ValidParent(ParentType = typeof(Folder))]
    [ValidParent(ParentType = typeof(ModelOverrides))]
    [ValidParent(ParentType = typeof(Sugarcane))]
    [ValidParent(ParentType = typeof(AgPasture.PastureSpecies))]
    public class Cultivar : Model, ILineEditor
    {
        /// <summary>Default constructor.</summary>
        /// <remarks>This is needed for AddModel to work.</remarks>
        public Cultivar()
        {
        }

        /// <summary>
        /// Constructor to initialise cultivar instance with specified commands
        /// </summary>
        /// <param name="commands">list of parameter overwrite commands</param>
        /// <param name="name">The name of the cultivar</param>
        public Cultivar (string name, string[] commands)
        {
            this.Name = name;
            Command = commands;
        }

        /// <summary>The model the cultivar is relative to.</summary>
        private IModel relativeToModel;

        /// <summary>The collection of undo overrides that undo the overrides.</summary>
        private IEnumerable<Overrides.Override> undos;

        /// <summary>The collection of commands that must be executed when applying this cultivar.</summary>
        public string[] Command { get; set; }

        /// <summary>The lines to return to the editor.</summary>
        [JsonIgnore]
        public IEnumerable<string> Lines { get { return Command; } set { Command = value.ToArray(); } }

        /// <summary>
        /// Return true if this cultivar has the same name as, or is an alias for, the given name.
        /// </summary>
        /// <param name="name">The name.</param>
        public bool IsKnownAs(string name)
        {
            return GetNames().Any(a => string.Equals(a, name, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Return all names by which this cultivar is known.
        /// </summary>
        public List<string> GetNames()
        {
            List<string> names = new List<string>();
            names.Add(Name);
            foreach (string name in FindAllChildren<Alias>().Select(a => a.Name))
                names.Add(name);
                
            return names;
        }

        /// <summary>Apply commands.</summary>
        /// <param name="model">The underlying model to apply the commands to</param>
        public void Apply(IModel model)
        {
            relativeToModel = model;
            if (Command != null)
                undos = Overrides.Apply(model, Overrides.ParseStrings(Command));
        }

        /// <summary>Undoes cultivar changes, if any.</summary>
        public void Unapply()
        {
            if (undos != null)
            {
                Overrides.Apply(relativeToModel, undos);
                relativeToModel = null;
                undos = null;
            }
        }

        /// <summary>
        /// Simulation is now completed. Make sure that we undo any commands. i.e. reset
        /// back to default state.
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            Unapply();
        }
    }
}
