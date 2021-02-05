namespace Models.Core.Run
{
    using Models.Storage;
    using System;
    using System.Linq;

    /// <summary>
    /// This class encapsulates an instruction to replace a model.
    /// </summary>
    public class ModelReplacement : IReplacement
    {
        /// <summary>
        /// Model path to use to find the model to replace. If null, then
        /// multiple replacements are made using the model name for matching.
        /// </summary>
        private string path;

        /// <summary>The value to Model path to use to find the model to replace.</summary>
        private IModel replacement;

        /// <summary>Constructor</summary>
        /// <param name="pathOfModel">Model path to use to find the model to replace. If null, then multiple replacements are made using the model name for matching.</param>
        /// <param name="modelReplacement">The value to Model path to use to find the model to replace.</param>
        public ModelReplacement(string pathOfModel, IModel modelReplacement)
        {
            path = pathOfModel;
            replacement = modelReplacement;
        }

        /// <summary>Perform the actual replacement.</summary>
        /// <param name="simulation">The simulation to perform the replacements on.</param>
        public void Replace(IModel simulation)
        {
            if (path == null)
            {
                // Temporarily remove DataStore because we don't want to do any
                // replacements under DataStore.
                DataStore dataStore = simulation.FindChild<DataStore>();
                if (dataStore != null)
                    simulation.Children.Remove(dataStore);

                // Do replacements.
                foreach (IModel match in simulation.FindAllDescendants(replacement.Name).ToList())
                    ReplaceModel(match);

                // Reinstate DataStore.
                if (dataStore != null)
                    simulation.Children.Add(dataStore);
            }
            else
            {
                IModel match = simulation.FindByPath(path)?.Value as IModel;
                if (match == null)
                    throw new Exception("Cannot find a model on path: " + path);
                ReplaceModel(match);

                // In a multi-paddock context, we want to attempt to
                // replace the model in all paddocks.
                foreach (IModel paddock in simulation.FindAllDescendants<Zone>().ToList())
                {
                    match = paddock.FindByPath(path)?.Value as IModel;
                    if (match != null)
                        ReplaceModel(match);
                }
            }
        }

        /// <summary>Perform the actual replacement.</summary>
        private void ReplaceModel(IModel match)
        {
            // Fixme - this code should be in Structure.cs.
            IModel newModel = Apsim.Clone(replacement);
            int index = match.Parent.Children.IndexOf(match as Model);
            match.Parent.Children.Insert(index, newModel as Model);
            newModel.Parent = match.Parent;
            newModel.Name = match.Name;
            newModel.Enabled = match.Enabled;

            // If a resource model (e.g. maize) is copied into replacements, and its
            // property values changed, these changed values will be overriden with the
            // 'accepted' values from the official maize model when the simulation is
            // run, because the model's resource name is not null. This can be manually
            // rectified by editing the json, but such an intervention shouldn't be
            // necessary.
            if (newModel is ModelCollectionFromResource resourceModel)
                resourceModel.ResourceName = null;

            match.Parent.Children.Remove(match as Model);
            Apsim.ClearCaches(match);

            // Don't call newModel.Parent.OnCreated(), because if we're replacing
            // a child of a resource model, the resource model's OnCreated event
            // will make it reread the resource string and replace this child with
            // the 'official' child from the resource.
            Simulations.InitialiseModel(newModel);
        }
    }

}
