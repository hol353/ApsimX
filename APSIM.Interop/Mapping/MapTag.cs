using System.Collections.Generic;
using APSIM.Shared.Documentation;
using Models.Mapping;

namespace APSIM.Interop.Mapping
{
    /// <summary>
    /// A map which can be displayed in autodocs.
    /// </summary>
    public class MapTag : ITag
    {
        /// <summary>
        /// Coordinate of the center of the map.
        /// </summary>
        public Coordinate Center { get; private set; }

        /// <summary>
        /// Zoom level of the map.
        /// </summary>
        /// <remarks>
        /// todo: check units.
        /// </remarks>
        public double Zoom { get; private set; }

        /// <summary>
        /// Coordinates for markers to be displayed on the map.
        /// </summary>
        public IEnumerable<Coordinate> Markers { get; private set; }

        /// <summary>
        /// Create a new <see cref="MapTag"/> instance.
        /// </summary>
        /// <param name="center">Coordinate of the center of the map.</param>
        /// <param name="zoom">Zoom level of the map.</param>
        /// <param name="markers">Coordinates for markers to be displayed on the map.</param>
        public MapTag(Coordinate center, double zoom, IEnumerable<Coordinate> markers)
        {
            Center = center;
            Zoom = zoom;
            Markers = markers;
        }
    }
}
