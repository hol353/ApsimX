using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Graphing;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.Run;
using Newtonsoft.Json;

namespace Models
{
    /// <summary>
    /// Represents a panel of graphs which has more flexibility than the
    /// page of graphs shown by a folder.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GraphPanelView")]
    [PresenterName("UserInterface.Presenters.GraphPanelPresenter")]
    [ValidParent(ParentType = typeof(ISimulationDescriptionGenerator))]
    [ValidParent(ParentType = typeof(Folder))]
    [ValidParent(ParentType = typeof(Simulations))]
    [ValidParent(ParentType = typeof(Zone))]
    public class GraphPanel : Model, IPostSimulationTool
    {
        /// <summary>
        /// When set to anything other than default, changes the legend
        /// position of all child graphs.
        /// </summary>
        /// <remarks>
        /// This is basically a duplicate of Graph.LegendPositionType, but the
        /// key difference is that this enum has an extra value - default which
        /// tells the UI to respect each graph's individual legend position.
        /// </remarks>
        public enum LegendPositionType
        {
            /// <summary>
            /// Respect each graph's individual legend position.
            /// </summary>
            Default,

            /// <summary>
            /// Top Left
            /// </summary>
            TopLeft,

            /// <summary>
            /// Top Right
            /// </summary>
            TopRight,

            /// <summary>
            /// Bottom Left
            /// </summary>
            BottomLeft,

            /// <summary>
            /// Bottom Right
            /// </summary>
            BottomRight,

            /// <summary>
            /// Left-middle
            /// </summary>
            LeftMiddle,

            /// <summary>
            /// Right-middle
            /// </summary>
            RightMiddle,

            /// <summary>
            /// Top of the graph, in the middle
            /// </summary>
            TopCenter,

            /// <summary>
            /// Bottom of the graph, in the middle
            /// </summary>
            BottomCenter
        };

        /// <summary>
        /// When set to anything other than default, changes the legend
        /// orientation of all child graphs.
        /// </summary>
        /// <remarks>
        /// This is basically a duplicate of Graph.LegendPositionType, but the
        /// key difference is that this enum has an extra value - default, which
        /// tells the UI to respect each graph's individual legend position.
        /// </remarks>
        public enum LegendOrientationType
        {
            /// <summary>
            /// Default legend orientation - respect each graph's
            /// individual settings.
            /// </summary>
            Default,

            /// <summary>
            /// Forces all graphs to use vertical legend orientation.
            /// </summary>
            Vertical,

            /// <summary>
            /// Forces all graphs to use horizontal legend orientation.
            /// </summary>
            Horizontal
        }

        /// <summary>
        /// Called when the model is deserialised.
        /// </summary>
        public override void OnCreated()
        {
            base.OnCreated();
            if (this.FindChild<Manager>() == null)
            {
                Manager script = new Manager();
                script.Name = "Config";
                script.Code = ReflectionUtilities.GetResourceAsString("Models.Resources.Scripts.GraphPanelScriptTemplate.cs");
                Children.Insert(0, script);
            }

            base.OnCreated();
        }

        /// <summary>
        /// Clears the cache after simulations are run.
        /// </summary>
        public void Run()
        {
            Cache.Clear();
        }

        /// <summary>
        /// Hide individual graph titles?
        /// </summary>
        [Description("Hide individual graph titles?")]
        public bool HideTitles { get; set; }

        /// <summary>
        /// Font Size.
        /// </summary>
        [Description("Font Size")]
        public double FontSize { get; set; } = 14;

        /// <summary>
        /// Marker Size. Defaults to MarkerSizeType.Normal.
        /// </summary>
        [Description("Marker Size")]
        public MarkerSize MarkerSize { get; set; }

        /// <summary>
        /// Use same x-axis scales for all graphs?
        /// </summary>
        [Separator("Axis settings")]
        [Description("Use same x-axis scales in all tabs?")]
        public bool SameXAxes { get; set; }

        /// <summary>
        /// Use same y-axis scales for all graphs?
        /// </summary>
        [Description("Use same y-axis scales in all tabs?")]
        public bool SameYAxes { get; set; }

        /// <summary>
        /// Move legends outside graph area?
        /// </summary>
        [Separator("Legend settings")]
        [Description("Move legends outside graph area?")]
        public bool LegendOutsideGraph { get; set; }

        /// <summary>
        /// Graph legend position. Applies to all graphs.
        /// </summary>
        [Description("Graph legend position. Applies to all graphs. Set to Default to make graphs individually customisable.")]
        public LegendPositionType LegendPosition { get; set; } = LegendPositionType.Default;

        /// <summary>
        /// Graph legend orientation. Applies to all graphs.
        /// </summary>
        [Description("Graph legend orientation. Applies to all graphs. Set to default to make graphs individually customisable.")]
        public LegendOrientationType LegendOrientation { get; set; } = LegendOrientationType.Default;

        /// <summary>
        /// Number of columns in page of graphs.
        /// </summary>
        [Separator("Panel settings")]
        [Description("Number of columns of graphs per tab")]
        public int NumCols { get; set; } = 2;

        /// <summary>
        /// Script which controls tab generation.
        /// </summary>
        public IGraphPanelScript Script
        {
            get
            {
                Manager manager = this.FindChild<Manager>();
                return manager?.Children?.FirstOrDefault() as IGraphPanelScript;
            }
        }

        /// <summary>
        /// The cache can be huge so it cannot be de/serialized. Need to have
        /// field + property combo because we need the [NonSerialized]
        /// attribute, which is only legal on a field. If we just had a
        /// property, the backing field would be serialized because it wouldn't
        /// have a [NonSerialized] attribute.
        /// </summary>
        [JsonIgnore]
        [NonSerialized]
        private Dictionary<string, Dictionary<int, List<SeriesDefinition>>> cache = new Dictionary<string, Dictionary<int, List<SeriesDefinition>>>();

        /// <summary>
        /// Cached graph data.
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, Dictionary<int, List<SeriesDefinition>>> Cache { get { return cache; } set { cache = value; } }
    }
}
