using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Core;
using Models;

namespace APSIM.Documentation.Models.Types;

/// <summary>
/// Documentation for the <see cref="Memo"/> class.
/// </summary>
public class DocMemo : DocGeneric
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocMemo" /> class.
    /// </summary>
    public DocMemo(IModel model) : base(model) { }

    /// <summary>
    /// Document the model.
    /// </summary>
    public override List<ITag> Document(int none = 0)
    {
        return new List<ITag>() {new Paragraph((model as Memo).Text)};
    }
}
