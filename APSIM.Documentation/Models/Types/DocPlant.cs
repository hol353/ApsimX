using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using APSIM.Shared.Documentation;
using Models;
using Models.Core;
using Models.PMF;
using Graph = Models.Graph;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Base documentation class for models
    /// </summary>
    public class DocPlant : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocPlant" /> class.
        /// </summary>
        public DocPlant(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            List<ITag> newTags = new List<ITag>();

            Section mainSection = GetSummaryAndRemarksSection(model);
            Section cultivarSection = null;

            newTags.Add(new Paragraph($"The model is constructed from the following list of software components. Details of the implementation and model parameterisation are provided in the following sections."));

            // Write Plant Model Table
            DataTable tableData = new DataTable();
            tableData.Columns.Add("Component Name", typeof(string));
            tableData.Columns.Add("Component Type", typeof(string));
            foreach (IModel child in this.model.Children)
            {
                if (child.GetType() != typeof(Memo) && child.GetType() != typeof(Cultivar) && child.GetType() != typeof(Folder) && child.GetType() != typeof(CompositeBiomass))
                {
                    DataRow row = tableData.NewRow();
                    row[0] = child.Name;
                    row[1] = child.GetType().ToString();
                    tableData.Rows.Add(row);
                }
            }
            newTags.Add(new Section("Plant Model Components", new Table(tableData)));

            // Write Composite Biomass Table
            DataTable tableDataBiomass = new DataTable();
            tableDataBiomass.Columns.Add("Component Name", typeof(string));
            tableDataBiomass.Columns.Add("Component Type", typeof(string));
            foreach (IModel child in this.model.Children)
            {
                if (child.GetType() == typeof(CompositeBiomass))
                {
                    DataRow row = tableDataBiomass.NewRow();
                    row[0] = child.Name;
                    row[1] = child.GetType().ToString();
                    tableDataBiomass.Rows.Add(row);
                }
            }
            if (tableDataBiomass.Rows.Count > 0) 
            {
                newTags.Add(new Section("Composite Biomass", new Table(tableDataBiomass)));
            }

            //Write cultivars table
            List<Cultivar> cultivars = model.FindAllDescendants<Cultivar>().ToList();
            if (cultivars.Count > 0) 
            {
                DataTable cultivarNameTable = new();
                cultivarNameTable.Columns.Add("Cultivar Name");
                cultivarNameTable.Columns.Add("Alternative Name(s)");
                foreach (Cultivar cultivarChild in cultivars)
                {
                    string altNames = cultivarChild.GetNames().Any() ? string.Join(',', cultivarChild.GetNames()) : string.Empty;
                    cultivarNameTable.Rows.Add(new string[] { cultivarChild.Name, altNames});
                }
                newTags.Add(new Section("Cultivars", new Table(cultivarNameTable)));
            }

            //Write children
            List<ITag> children = new List<ITag>();
            foreach (IModel child in this.model.Children)
                if (child as Memo == null && child as CompositeBiomass == null && child as Folder == null && child as Cultivar == null)
                    children.AddRange(new List<ITag>() { GetSummaryAndRemarksSection(child) });
            newTags.Add(new Section("Child Components", children));

            mainSection.Add(newTags);

            newTags = new List<ITag>() { mainSection };
            if (cultivarSection != null)
                newTags.Add(cultivarSection);

            return newTags;
        }
    }
}
