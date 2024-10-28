using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using System.Linq;
using Models;
using Models.Storage;
using Models.Factorial;
using System.Data;
using System;
using ModelsGraph = Models.Graph;
using ModelsGraphPage = Models.GraphPage;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Base documentation class for models
    /// </summary>
    public class DocFolder : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocGenericWithChildren" /> class.
        /// </summary>
        public DocFolder(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            Section section = GetSummaryAndRemarksSection(model);
            // Remove summary, summary is superfluous.
            if (section.Children.Count >= 2)
                section.Children.RemoveAt(0);
            
            foreach(Map map in model.FindAllChildren<Map>().Where(map => map.Enabled))
            {
                section.Add(AutoDocumentation.DocumentModel(map));
            }

            // Write experiment descriptions. We don't call experiment.Document() here,
            // because we want to just show the experiment design (a string) and put it
            // inside a table cell.
            IEnumerable<Experiment> experiments = model.FindAllChildren<Experiment>().Where(experiment => experiment.Enabled);
            if (experiments.Any())
            {
                List<ITag> experimentsTag = new List<ITag>();
                DataTable table = new DataTable();
                table.Columns.Add("Experiment Name", typeof(string));
                table.Columns.Add("Design (Number of Treatments)", typeof(string));

                foreach (Experiment experiment in experiments)
                {
                    DataRow row = table.NewRow();
                    row[0] = experiment.Name;
                    row[1] = experiment.GetDesign();
                    table.Rows.Add(row);
                }
                experimentsTag.Add(new Table(table));
                section.Add(new Section("List of experiments", experimentsTag));
            }
            
            // Write page of graphs.
            List<ModelsGraph> childGraphs = model.FindAllChildren<ModelsGraph>().Where(f => f.Enabled).ToList();
            List<IGraph> childIGraphs = new List<IGraph>();
            foreach(ModelsGraph graph in childGraphs)
            {
                bool hide = graph.FindAllAncestors<Folder>().Where(a => !a.ShowInDocs).Any();
                if (!hide)
                    childIGraphs.Add(graph.ToGraph(graph.GetSeriesDefinitions()));
            }
                
            section.Add(new Shared.Documentation.GraphPage(childIGraphs));

            // Document graphs under a experiment
            foreach (Experiment exp in model.FindAllChildren<Experiment>().Where(f => f.Enabled))
            {
                List<ITag> expTags = new List<ITag>();
                foreach (Memo memo in exp.FindAllChildren<Memo>())
                    expTags.AddRange(AutoDocumentation.DocumentModel(memo));

                childGraphs = exp.FindAllDescendants<ModelsGraph>().Where(f => f.Enabled).ToList();
                childIGraphs = new List<IGraph>();
                foreach(ModelsGraph graph in childGraphs)
                {
                    bool hide = graph.FindAllAncestors<Folder>().Where(a => !a.ShowInDocs).Any();
                    if (!hide)
                        childIGraphs.Add(graph.ToGraph(graph.GetSeriesDefinitions()));
                }

                if (childIGraphs.Count > 0)
                    expTags.Add(new Shared.Documentation.GraphPage(childIGraphs));

                if (expTags.Count > 0)
                    section.Add(new Section(exp.Name, expTags));
            }

            // Document graphs under a simulation
            foreach (Simulation sim in model.FindAllChildren<Simulation>().Where(f => f.Enabled))
            {
                List<ITag> simTags = new List<ITag>();
                foreach (Memo memo in sim.FindAllChildren<Memo>())
                    simTags.AddRange(AutoDocumentation.DocumentModel(memo));

                childGraphs = sim.FindAllDescendants<ModelsGraph>().Where(f => f.Enabled).ToList();
                childIGraphs = new List<IGraph>();
                foreach(ModelsGraph graph in childGraphs)
                {
                    bool hide = graph.FindAllAncestors<Folder>().Where(a => !a.ShowInDocs).Any();
                    if (!hide)
                        childIGraphs.Add(graph.ToGraph(graph.GetSeriesDefinitions()));
                }
                    
                if (childIGraphs.Count > 0)
                    simTags.Add(new Shared.Documentation.GraphPage(childIGraphs));

                if (simTags.Count > 0)
                    section.Add(new Section(sim.Name, simTags));
            }

            // Document child folders.
            foreach (Folder folder in model.FindAllChildren<Folder>().Where(f => f.Enabled))
                section.Add(AutoDocumentation.DocumentModel(folder));
            
            return new List<ITag>() {section};
        }

        /// <summary>
        /// Gets child graphs from a folder model.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<APSIM.Shared.Documentation.Graph> GetChildGraphs()
        {
            var graphs = new List<APSIM.Shared.Documentation.Graph>();
            var page = new ModelsGraphPage();
            page.Graphs.AddRange(model.FindAllChildren<ModelsGraph>().Where(g => g.Enabled));
            var storage = model.FindInScope<IDataStore>();
            List<ModelsGraphPage.GraphDefinitionMap> definitionMaps = new();
            if (storage != null)
                definitionMaps.AddRange(page.GetAllSeriesDefinitions(model, storage.Reader));
            foreach (var map in definitionMaps)
            {
                try
                {
                    graphs.Add(map.Graph.ToGraph(map.SeriesDefinitions));
                }
                catch (Exception err)
                {
                    Console.Error.WriteLine(err);
                }
            }
            return graphs;
        }

    }


}
