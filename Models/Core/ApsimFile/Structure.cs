namespace Models.Core.ApsimFile
{
    using System;
    using System.Collections.Generic;
    using Models.Core.Apsim710File;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml;

    /// <summary>
    /// A collection of methods for manipulating the structure of an .apsimx file.
    /// </summary>
    public static class Structure
    {
        /// <summary>
        /// Adds a model as a child to a parent model. Will throw if not allowed.
        /// </summary>
        /// <param name="modelToAdd">The model to add.</param>
        /// <param name="parent">The parent model to add it to.</param>
        public static IModel Add(IModel modelToAdd, IModel parent)
        {
            if (parent.ReadOnly)
                throw new Exception(string.Format("Unable to modify {0} - it is read-only.", parent.Name));

            if (modelToAdd is Simulations s && s.Children.Count == 1)
                modelToAdd = s.Children[0];

            modelToAdd.Parent = parent;
            modelToAdd.ParentAllDescendants();
            parent.Children.Add(modelToAdd);

            // Ensure the model name is valid.
            EnsureNameIsUnique(modelToAdd);

            // Call OnCreated
            Simulations.InitialiseModel(modelToAdd);

            // If the model is being added at runtime then need to resolve links and events.
            Simulation parentSimulation = parent.FindAncestor<Simulation>();
            if (parentSimulation != null && parentSimulation.IsRunning)
            {
                var links = new Links(parentSimulation.Services);
                links.Resolve(modelToAdd, true);
                var events = new Events(modelToAdd);
                events.ConnectEvents();

                // Call StartOfSimulation events
                events.PublishToModelAndChildren("StartOfSimulation", new object[] { parent, new EventArgs() });
            }

            Apsim.ClearCaches(modelToAdd);
            return modelToAdd;
        }

        /// <summary>Adds a new model (as specified by the string argument) to the specified parent.</summary>
        /// <param name="parent">The parent to add the model to</param>
        /// <param name="st">The string representing the new model</param>
        /// <returns>The newly created model.</returns>
        public static IModel Add(string st, IModel parent)
        {
            // The strategy here is to try and add the string as if it was a APSIM Next Gen.
            // string (json or xml). If that throws an exception then try adding it as if
            // it was an APSIM 7.10 string (xml). If that doesn't work throw 'invalid format' exception.
            List<Exception> creationExceptions;
            IModel modelToAdd = null;
            try
            {
                modelToAdd = FileFormat.ReadFromString<IModel>(st, out creationExceptions);
            }
            catch (Exception err)
            {
                if (err.Message.StartsWith("Unknown string encountered"))
                    throw;
            }

            if (modelToAdd == null)
            {
                // Try the string as if it was an APSIM 7.10 xml string.
                var xmlDocument = new XmlDocument();
                xmlDocument.LoadXml("<Simulation>" + st + "</Simulation>");
                var importer = new Importer();
                var rootNode = xmlDocument.DocumentElement as XmlNode;
                var convertedNode = importer.AddComponent(rootNode.ChildNodes[0], ref rootNode);
                rootNode.RemoveAll();
                rootNode.AppendChild(convertedNode);
                var newSimulationModel = FileFormat.ReadFromString<IModel>(rootNode.OuterXml, out creationExceptions);
                if (newSimulationModel == null || newSimulationModel.Children.Count == 0)
                    throw new Exception("Cannot add model. Invalid model being added.");
                modelToAdd = newSimulationModel.Children[0];
            }

            // Correctly parent all models.
            modelToAdd = Add(modelToAdd, parent);

            // Ensure the model name is valid.
            EnsureNameIsUnique(modelToAdd);

            // Call OnCreated
            Simulations.InitialiseModel(modelToAdd);

            return modelToAdd;
        }

        /// <summary>Renames a new model.</summary>
        /// <param name="model">The model to rename.</param>
        /// <param name="newName">The new name for the model.</param>
        /// <returns>The newly created model.</returns>
        public static void Rename(IModel model, string newName)
        {
            model.Name = newName;
            EnsureNameIsUnique(model);
            Apsim.ClearCaches(model);
        }

        /// <summary>Move a model from one parent to another.</summary>
        /// <param name="model">The model to move.</param>
        /// <param name="newParent">The new parente for the model.</param>
        public static void Move(IModel model, IModel newParent)
        {
            // Remove old model.
            if (model.Parent.Children.Remove(model as Model))
            {
                // Clear the cache for all models in scope of the model to be moved.
                // The models in scope will be different after the move so we will
                // need to do this again after we move the model.
                Apsim.ClearCaches(model);
                newParent.Children.Add(model as Model);
                model.Parent = newParent;
                EnsureNameIsUnique(model);
                Apsim.ClearCaches(model);
            }
            else
                throw new Exception("Cannot move model " + model.Name);
        }

        /// <summary>
        /// Give the specified model a unique name
        /// </summary>
        /// <param name="modelToCheck">The model to check the name of</param>
        private static void EnsureNameIsUnique(IModel modelToCheck)
        {
            string originalName = modelToCheck.Name;
            string newName = originalName;
            int counter = 0;
            IModel siblingWithSameName = modelToCheck.FindSibling(newName);
            while (siblingWithSameName != null && counter < 10000)
            {
                counter++;
                newName = originalName + counter.ToString();
                siblingWithSameName = modelToCheck.FindSibling(newName);
            }

            if (counter == 1000)
            {
                throw new Exception("Cannot create a unique name for model: " + originalName);
            }

            modelToCheck.Name = newName;
        }

        /// <summary>Deletes the specified model.</summary>
        /// <param name="model">The model.</param>
        public static bool Delete(IModel model)
        {
            Apsim.ClearCaches(model);
            return model.Parent.Children.Remove(model as Model);
        }
    }
}
