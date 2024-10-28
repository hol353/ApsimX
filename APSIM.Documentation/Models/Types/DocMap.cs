using System.Collections.Generic;
using APSIM.Shared.Documentation;
using APSIM.Interop.Mapping;
using Models.Core;
using Models;

namespace APSIM.Documentation.Models.Types
{

    /// <summary>
    /// Documentation for Map
    /// </summary>
    public class DocMap : DocGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DocMap" /> class.
        /// </summary>
        public DocMap(IModel model): base(model) {}

        /// <summary>
        /// Document the model
        /// </summary>
        public override List<ITag> Document(int none = 0)
        {
            Section section = GetSectionTitle(model);
            Map map = model as Map;
            section.Add(new MapTag(map.Center, map.Zoom, map.GetCoordinates()));
            return new List<ITag>() {section};
        }        
    }
}
