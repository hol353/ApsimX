using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.AgPasture;
using System.Data;
using System.Linq;
using APSIM.Shared.Utilities;

namespace APSIM.Documentation.Models.Types
{
    /// <summary>
    /// Documentation class for SpeciesTable file
    /// </summary>
    public class DocSpeciesTable : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public DocSpeciesTable(IModel model): base(model) {}

        /// <summary>
        /// Document the model.
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            List<ITag> tags = new List<ITag>();
            IEnumerable<PastureSpecies> models = model.FindAllInScope<PastureSpecies>();

            DataTable table = new DataTable();
            table.Columns.Add("Parameter name");
            if (models.Any())
            {
                foreach (var model in models)
                    table.Columns.Add(model.Name);

                var parameterNames = Resource.GetModelParameterNames(models.First().ResourceName);

                foreach (var parameterName in parameterNames)
                {
                    var row = table.NewRow();
                    row["Parameter name"] = parameterName;
                    foreach (var model in models)
                    {
                        IVariable variable = model.FindByPath(parameterName);
                        if (variable != null)
                        {
                            var value = variable.Value;
                            if (value != null)
                            {
                                if (value is double[])
                                    value = StringUtilities.BuildString(value as double[], "f4");
                                row[model.Name] = value.ToString();
                            }
                        }
                    }
                    table.Rows.Add(row);
                }
            }

            Paragraph text = new Paragraph("Listed in the table below are the default values for all parameters for all AgPasture species");

            Section main = new Section("SpeciesTable", new List<ITag>());
            main.Add(text);
            main.Add(new Table(table));

            tags.Add(main);

            return tags;
        }
    }
}
