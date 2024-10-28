using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models;
using Models.Core;
using System;
using System.Data;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Base documentation class for models
    /// </summary>
    public class DocClock : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocClock" /> class.
        /// </summary>
        public DocClock(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            Section section = GetSummaryAndRemarksSection(model);
            section.Add(new Section(model.Name, GetModelEventsInvoked(typeof(Clock), "OnDoCommence(object _, CommenceArgs e)", "CLEM", true)));
            return new List<ITag>() {section};
        }

        /// <summary>
        /// Gets a list of Event Handles that are Invoked in the provided function
        /// </summary>
        /// <remarks>
        /// Model source file must be included as embedded resource in project xml
        /// </remarks>
        protected List<ITag> GetModelEventsInvoked(Type type, string functionName, string filter = "", bool filterOut = false)
        {
            List<ITag> tags = new List<ITag>();
            List<string[]> eventNames = CodeDocumentation.GetEventsInvokedInOrder(type, functionName);

            List<string[]> eventNamesFiltered = new List<string[]>();
            if (filter.Length > 0)
            {
                foreach (string[] name in eventNames)
                    if (name[0].Contains(filter) == !filterOut)
                    { 
                        eventNamesFiltered.Add(name); 
                    }           
            }
            tags.Add(new Paragraph($"Function {functionName} of Model {model.Name} contains the following Events in the given order.\n"));

            DataTable data = new DataTable();
            data.Columns.Add("Event Handle", typeof(string));
            data.Columns.Add("Summary", typeof(string));

            for (int i = 1; i < eventNamesFiltered.Count; i++)
            {
                string[] parts = eventNamesFiltered[i];

                DataRow row = data.NewRow();
                data.Rows.Add(row);
                row["Event Handle"] = parts[0];
                row["Summary"] = parts[1];
            }
            tags.Add(new Table(data));
            return tags;
        }
    }
}
