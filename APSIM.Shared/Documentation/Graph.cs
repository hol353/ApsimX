using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Graphing;

namespace APSIM.Shared.Documentation
{
    /// <summary>
    /// A graph tag.
    /// </summary>
    /// <remarks>
    /// todo:
    /// - caption?
    /// </remarks>
    public class Graph : ITag, IGraph
    {
        /// <summary>
        /// The series to be shown on the graph.
        /// </summary>
        /// <value></value>
        public IEnumerable<Series> Series { get; private set; }

        /// <summary>
        /// The x axis.
        /// </summary>
        public Axis XAxis { get; private set; }

        /// <summary>
        /// The y axis.
        /// </summary>
        public Axis YAxis { get; private set; }

        /// <summary>
        /// Legend configuration.
        /// </summary>
        public ILegendConfiguration Legend { get; private set; }

        /// <summary>
        /// Graph Title.
        /// </summary>
        public string Title { get; private set; }

        /// <summary>
        /// Graph Path
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Constructs a graph tag instance.
        /// </summary>
        /// <param name="title">Title of the graph.</param>
        /// <param name="path">Path of graph.</param>
        /// <param name="series">The series to be shown on the graph.</param>
        /// <param name="xAxis">The x axis.</param>
        /// <param name="yAxis">The y axis.</param>
        /// <param name="legend">Legend configuration.</param>
        public Graph(string title, string path, IEnumerable<Series> series, Axis xAxis, Axis yAxis, LegendConfiguration legend)
        {
            Title = title;
            Path = path;
            Series = series;
            Legend = legend;
            XAxis = xAxis;
            YAxis = yAxis;
        }

        /// <summary>
        /// Get all data which would be rendered on the given axis (ie
        /// all x data for a top- or bottom-positioned axis, or all y
        /// data for a left- or right-positioned axis).
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private IEnumerable<IEnumerable<object>> GetSeries(AxisPosition position)
        {
            switch (position)
            {
                case AxisPosition.Bottom:
                case AxisPosition.Top:
                    return Series.Select(s => s.X);
                case AxisPosition.Left:
                case AxisPosition.Right:
                    return Series.Select(s => s.Y);
                default:
                    throw new NotImplementedException($"Unknown axis position {position}");
            }
        }

        /// <summary>Adds an ITag as a child of this ITag</summary>
        public void Add(ITag tag) {
            throw new Exception("Graph cannot have child tags");
        }

        /// <summary>Adds a list of ITags as a children of this ITag</summary>
        public void Add(List<ITag> tags) {
            throw new Exception("Graph cannot have child tags");
        }
    }
}
