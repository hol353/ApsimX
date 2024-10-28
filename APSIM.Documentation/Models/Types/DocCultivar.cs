using System.Collections.Generic;
using System.Data;
using System.Linq;
using APSIM.Shared.Documentation;
using Models.Core;
using Models.PMF;

namespace APSIM.Documentation.Models.Types;

/// <summary>
/// Documentation for the <see cref="Cultivar"/> class.
/// </summary>
public class DocCultivar : DocGeneric
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocPlant" /> class.
    /// </summary>
    public DocCultivar(IModel model) : base(model) { }

    /// <summary>
    /// Document the model.
    /// </summary>
    public override List<ITag> Document(int none = 0)
    {
        Section section = GetSummaryAndRemarksSection(model);

        Cultivar cultivarModel = model as Cultivar;

        // Get table of Parameter overrides.
        DataTable overridesTable = new();
        overridesTable.Columns.Add("Parameter overrides");
        if (cultivarModel.Command != null)
        {
            foreach (string paramOverride in (model as Cultivar).Command)
                overridesTable.Rows.Add(paramOverride);
        }
        Table paramOverridesDisplayTable = new(overridesTable);

        ITag aliasTag = cultivarModel.GetNames().Any() ? 
            new Section("Aliases", new Paragraph(string.Join(',', cultivarModel.GetNames()))) : 
            new Paragraph("");

        section.Add(aliasTag);
        section.Add(paramOverridesDisplayTable);

        return new List<ITag>() {section};
    }
}
