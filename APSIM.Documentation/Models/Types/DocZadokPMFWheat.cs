using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.PMF.Phen;
using System.Data;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Base documentation class for models
    /// </summary>
    public class DocZadokPMFWheat : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ZadokPMFWheat" /> class.
        /// </summary>
        public DocZadokPMFWheat(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            Section section = GetSummaryAndRemarksSection(model);
                
            // Write a table containing growth phases and descriptions.
            DataTable table = new DataTable();
            table.Columns.Add("Growth Phase", typeof(string));
            table.Columns.Add("Descriptipon", typeof(string));
            DataRow row = table.NewRow();
            row[0] = "Germinating";
            row[1] = "ZadokStage = 5 x FractionThroughPhase";
            table.Rows.Add(row);
            row = table.NewRow();
            row[0] = "Emerging";
            row[1] = "ZadokStage = 5 + 5 x FractionThroughPhase";
            table.Rows.Add(row);
            row = table.NewRow();
            row[0] = "Vegetative";
            row[1] = "ZadokStage = 10 + Structure.LeafTipsAppeared";
            table.Rows.Add(row);
            row = table.NewRow();
            row[0] = "Reproductive";
            row[1] = "ZadokStage is interpolated from values of stage number using the following table";
            table.Rows.Add(row);
            Table growthPhaseTable = new(table);
            section.Add(new Section("List of growth phases", growthPhaseTable));

            // Write a table containing growth stages
            table = new DataTable();
            table.Columns.Add("Growth Stage", typeof(double));
            table.Columns.Add("Stage Name", typeof(string));
            table.Columns.Add("ZadokStage", typeof(int));

            row = table.NewRow();
            row[0] = 4.3;
            row[1] = "Pseudostem";
            row[2] = 30;
            table.Rows.Add(row);
            row = table.NewRow();
            row[0] = 4.9;
            row[1] = "Third node detectable";
            row[2] = 33;
            table.Rows.Add(row);
            row = table.NewRow();
            row[0] = 5.0;
            row[1] = "Flag leaf ligule just visible";
            row[2] = 39;
            table.Rows.Add(row);
            row = table.NewRow();
            row[0] = 6.0;
            row[1] = "Heading (Ear half emerged)";
            row[2] = 55;
            table.Rows.Add(row);
            row = table.NewRow();
            row[0] = 7.0;
            row[1] = "Flowering (Anthesis half-way)";
            row[2] = 65;
            table.Rows.Add(row);
            row = table.NewRow();
            row[0] = 8.0;
            row[1] = "Kernel water ripe";
            row[2] = 71;
            table.Rows.Add(row);
            row = table.NewRow();
            row[0] = 9.0;
            row[1] = "Hard dough";
            row[2] = 87;
            table.Rows.Add(row);
            row = table.NewRow();
            row[0] = 10.0;
            row[1] = "Ripening";
            row[2] = 90;
            table.Rows.Add(row);
            var growthStageTable = new Table(table);
            section.Add(new Section("List of growth stages", growthStageTable));
            
            return new List<ITag>() {section};
        }
    }
}

           
           