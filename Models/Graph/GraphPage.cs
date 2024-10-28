using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APSIM.Shared.Utilities;
using APSIM.Shared.Documentation;
using Models.CLEM;
using Models.Core;
using Models.Core.Run;
using Models.Factorial;
using Models.Storage;

namespace Models
{

    /// <summary>Descibes a page of graphs for the tags system.</summary>
    public class GraphPage : ITag
    {
        /// <summary>The image to put into the doc.</summary>
        public List<Graph> Graphs { get; set; } = new List<Graph>();

        /// <summary>Unique name for image. Used to save image to temp folder.</summary>
        public string Name { get; set; }

        /// <summary>
        /// Get series definitions for all graphs.
        /// </summary>
        /// <param name="parent">Parent model.</param>
        /// <param name="storage">Storage reader</param>
        /// <param name="simulationFilter">(Optional) Simulation name filter.</param>
        /// <returns></returns>
        public List<GraphDefinitionMap> GetAllSeriesDefinitions(IModel parent, IStorageReader storage, List<string> simulationFilter = null)
        {
            // Get all simulation descriptions.
            var simulationDescriptions = FindSimulationDescriptions(parent);

            // Get a list of series definitions from all graphs.
            var allDefinitions = new List<GraphDefinitionMap>();
            foreach (var graph in Graphs)
            {
                graph.SimulationDescriptions = simulationDescriptions;
                allDefinitions.Add(
                    new GraphDefinitionMap()
                    {
                        Graph = graph,
                        SeriesDefinitions = graph.GetDefinitionsToGraph(storage, simulationFilter).ToList()
                    });
            }

            // Read data for all definitions.
            ReadAllData(storage, allDefinitions.SelectMany(d => d.SeriesDefinitions), simulationDescriptions);

            // Get each series to add child definitions.
            for (int g = 0; g < allDefinitions.Count; g++)
                foreach (var s in allDefinitions[g].Graph.FindAllChildren<Series>())
                    allDefinitions[g].SeriesDefinitions.AddRange(s.CreateChildSeriesDefinitions(storage, simulationDescriptions, allDefinitions[g].SeriesDefinitions.Where(sd => sd.Series == s), simulationFilter));

            // Remove series that have no data.
            foreach (var definition in allDefinitions)
                definition.SeriesDefinitions.RemoveAll(d => !MathUtilities.ValuesInArray(d.X) || !MathUtilities.ValuesInArray(d.Y));

            return allDefinitions;
        }

        /// <summary>
        /// Find and return a list of all simulation descriptions.
        /// </summary>
        public static List<SimulationDescription> FindSimulationDescriptions(IModel model)
        {
            // Find a parent that heads the scope that we're going to graph
            IModel parent = FindParent(model);
            if (parent is Simulation && parent.Parent is Experiment)
                throw new Exception("Graph scope is incorrect if placed under a Simulation in an Experiment. It should be a child of the Experiment instead.");

            List<SimulationDescription> simulationDescriptions = new List<SimulationDescription>();
            while (simulationDescriptions.Count == 0 && parent != null) {
                // Create a list of all simulation/zone objects that we're going to graph.
                simulationDescriptions = GetSimulationDescriptionsUnderModel(parent);
                parent = parent.Parent;
            }

            return simulationDescriptions;
        }

        /// <summary>Find a parent to base our series on.</summary>
        private static IModel FindParent(IModel model)
        {
            Type[] parentTypesToMatch = new Type[] { typeof(Simulation), typeof(Zone), typeof(ZoneCLEM), typeof(Experiment),
                                                     typeof(Folder), typeof(Simulations) };

            IModel obj = model;
            do
            {
                foreach (Type typeToMatch in parentTypesToMatch)
                    if (typeToMatch.IsAssignableFrom(obj.GetType()))
                        return obj;
                obj = obj.Parent;
            }
            while (obj != null);
            return obj;
        }

        /// <summary>
        /// Get a list of simulation descriptions that are a child of the specified model.
        /// </summary>
        /// <param name="model">The model and it's child models to scan.</param>
        private static List<SimulationDescription> GetSimulationDescriptionsUnderModel(IModel model)
        {
            var simulationDescriptions = new List<SimulationDescription>();
            if (model is ISimulationDescriptionGenerator)
                simulationDescriptions.AddRange((model as ISimulationDescriptionGenerator).GenerateSimulationDescriptions());
            else
            {
                foreach (IModel child in model.Children)
                {
                    if (child is Simulation || child is ISimulationDescriptionGenerator || child is Folder)
                        simulationDescriptions.AddRange(GetSimulationDescriptionsUnderModel(child));
                }
            }
            return simulationDescriptions;
        }

        /// <summary>
        /// Create a datatable that covers a collection of series definitions.
        /// </summary>
        /// <param name="storage">A data store reader.</param>
        /// <param name="series">A list of series definitions.</param>
        /// <param name="simulationDescriptions">A list of simulation descriptions.</param>
        /// <returns></returns>
        private static void ReadAllData(IStorageReader storage, IEnumerable<SeriesDefinition> series,
                                        List<SimulationDescription> simulationDescriptions)
        {
            var definitionsToProcess = series.ToList();

            // Remove all series that already have data. I'm not sure
            // under what circumstance this would happen.
            definitionsToProcess.RemoveAll(d => d.X != null && d.Y != null);

            // Process and remove all series that have a table name specified.
            var definitionsWithNoTable = definitionsToProcess.Where(d => d.Series.TableName == null);
            foreach (var d in definitionsWithNoTable)
                d.GetDataFromModels();
            definitionsToProcess.RemoveAll(d => definitionsWithNoTable.Contains(d));

            var allTableNames = definitionsToProcess.Select(d => d.Series.TableName)
                                                    .Distinct();

            // Get a list of inscope simulation names.
            var allSimulationNamesInScope = new List<string>();
            foreach (var d in definitionsToProcess)
                if (d.InScopeSimulationNames != null)
                    allSimulationNamesInScope.AddRange(d.InScopeSimulationNames);
            var inScopeSimulationNames = allSimulationNamesInScope.Distinct();
            if (!inScopeSimulationNames.Any())
                inScopeSimulationNames = null;

            foreach (var tableName in allTableNames)
            {
                var definitionsUsingThisTable = definitionsToProcess.Where(d => d.Series.TableName == tableName);
                var checkpointNames = definitionsUsingThisTable.Select(d => d.CheckpointName)
                                                               .Distinct();
                var fieldsThatExist = storage.ColumnNames(tableName);

                var fieldNames = definitionsUsingThisTable.SelectMany(d => d.GetFieldNames(fieldsThatExist))
                                                          .Distinct();

                // Only attempt to read simulation names if this table actually contains
                // a simulation name or ID column. Some tables (ie observed data from excel)
                // don't necessarily have these columns.
                IEnumerable<string> simulationNames = fieldsThatExist.Contains("SimulationID") || fieldsThatExist.Contains("SimulationName") ? inScopeSimulationNames : null;
                foreach (var checkpointName in checkpointNames)
                {
                    try
                    {
                        var table = storage.GetData(tableName, checkpointName, simulationNames, fieldNames);

                        // Tell each series definition to read its data.
                        var definitions = definitionsToProcess.Where(d => d.Series.TableName == tableName && d.CheckpointName == checkpointName);
                        Parallel.ForEach(definitions, (definition) =>
                            definition.ReadData(table, simulationDescriptions, storage));
                    }
                    catch (Exception error)
                    {
                        throw new Exception($"Failed to fetch data for checkpoint {checkpointName}", error);
                    }
                }
            }
        }


        /// <summary>A map of between a graph and its series definitions.</summary>
        public class GraphDefinitionMap
        {
            /// <summary>The graph.</summary>
            public Graph Graph { get; set; }

            /// <summary>The series definitions.</summary>
            public List<SeriesDefinition> SeriesDefinitions { get; set; }
        }
    }
}
