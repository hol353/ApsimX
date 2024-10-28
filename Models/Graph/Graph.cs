using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using APSIM.Shared.Graphing;
using Models.Core;
using Models.Core.Run;
using Models.Factorial;
using Models.Storage;
using Newtonsoft.Json;

namespace Models
{

    /// <summary>
    /// Represents a graph
    /// </summary>
    [ViewName("UserInterface.Views.GraphView")]
    [PresenterName("UserInterface.Presenters.GraphPresenter")]
    [Serializable]
    [ValidParent(ParentType = typeof(Simulations))]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(Zone))]
    [ValidParent(ParentType = typeof(Experiment))]
    [ValidParent(ParentType = typeof(Morris))]
    [ValidParent(ParentType = typeof(Sobol))]
    [ValidParent(ParentType = typeof(Folder))]
    [ValidParent(ParentType = typeof(GraphPanel))]
    public class Graph : Model
    {
        /// <summary>The data tables on the graph.</summary>
        [NonSerialized]
        private Dictionary<string, DataTable> tables = new Dictionary<string, DataTable>();

        /// <summary>
        /// Gets or sets the caption at the bottom of the graph
        /// </summary>
        public string Caption { get; set; }

        /// <summary>
        /// Gets or sets a list of all axes
        /// </summary>
        public List<Axis> Axis { get; set; }

        /// <summary>
        /// Gets or sets a list of all series
        /// </summary>
        [JsonIgnore]
        public List<Series> Series { get { return FindAllChildren<Series>().ToList(); } }

        /// <summary>
        /// Gets or sets the location of the legend
        /// </summary>
        public LegendPosition LegendPosition { get; set; }

        /// <summary>
        /// Controls the orientation of legend items.
        /// </summary>
        public LegendOrientation LegendOrientation { get; set; }

        /// <summary>
        /// Gets or sets the location of the annotations - name/position map.
        /// </summary>
        public AnnotationPosition AnnotationLocation { get; set; }

        /// <summary>
        /// Gets or sets a list of raw grpah series that should be disabled.
        /// </summary>
        public List<string> DisabledSeries { get; set; }

        /// <summary>
        /// If set to true, the legend will be shown outside the graph area.
        /// </summary>
        public bool LegendOutsideGraph { get; set; }

        /// <summary>
        /// Descriptions of simulations that are in scope.
        /// </summary>
        [JsonIgnore]
        public List<SimulationDescription> SimulationDescriptions { get; set; }

        /// <summary>Gets the definitions to graph.</summary>
        /// <returns>A list of series definitions.</returns>
        /// <param name="storage">Storage service</param>
        /// <param name="simulationFilter">(Optional) Simulation name filter.</param>
        public IEnumerable<SeriesDefinition> GetDefinitionsToGraph(IStorageReader storage, List<string> simulationFilter = null)
        {
            EnsureAllAxesExist();

            var series = FindAllChildren<Series>().Where(g => g.Enabled);
            var definitions = new List<SeriesDefinition>();
            foreach (var s in series)
            {
                var seriesDefinitions = s.CreateSeriesDefinitions(storage, SimulationDescriptions, simulationFilter);
                definitions.AddRange(seriesDefinitions);
            }

            return definitions;
        }

        /// <summary>Gets the annotations to graph.</summary>
        /// <returns>A list of series annotations.</returns>
        public IEnumerable<IAnnotation> GetAnnotationsToGraph()
        {
            return FindAllChildren<IGraphable>()
                        .Where(g => g.Enabled)
                        .SelectMany(g => g.GetAnnotations());
        }

        /// <summary>
        /// Ensure that we have all necessary axis objects.
        /// </summary>
        private void EnsureAllAxesExist()
        {
            // Get a list of all axis types that are referenced by the series.
            List<AxisPosition> allAxisTypes = new List<AxisPosition>();
            foreach (Series series in Series)
            {
                allAxisTypes.Add(series.XAxis);
                allAxisTypes.Add(series.YAxis);
            }

            // Go through all graph axis objects. For each, check to see if it is still needed and
            // if so copy to our list.
            if (Axis == null)
                Axis = new List<Axis>();
            List<Axis> allAxes = new List<Axis>();
            bool unNeededAxisFound = false;
            foreach (Axis axis in Axis)
            {
                if (allAxisTypes.Contains(axis.Position))
                    allAxes.Add(axis);
                else
                    unNeededAxisFound = true;
            }

            // Go through all series and make sure an axis object is present in our AllAxes list. If
            // not then go create an axis object.
            bool axisWasAdded = false;
            foreach (Series S in Series)
            {
                Axis foundAxis = allAxes.Find(a => a.Position == S.XAxis);
                if (foundAxis == null)
                {
                    allAxes.Add(new Axis(S.XFieldName, S.XAxis));
                    axisWasAdded = true;
                }

                foundAxis = allAxes.Find(a => a.Position == S.YAxis);
                if (foundAxis == null)
                {
                    allAxes.Add(new Axis(S.YFieldName, S.YAxis));
                    axisWasAdded = true;
                }
            }

            if (unNeededAxisFound || axisWasAdded)
                Axis = allAxes;
        }

        /// <summary>
        /// Get all series definitions using the GraphPage API - which will
        /// load the series' data in parallel.
        /// </summary>
        public IEnumerable<SeriesDefinition> GetSeriesDefinitions()
        {
            // Using the graphpage API - this will load each series' data in parallel.
            GraphPage page = new GraphPage();
            page.Graphs.Add(this);
            return page.GetAllSeriesDefinitions(Parent, FindInScope<IDataStore>()?.Reader).FirstOrDefault()?.SeriesDefinitions;
        }

        /// <summary>
        /// Get a list of 'standardised' series objects which are to be shown on the graph.
        /// </summary>
        public IEnumerable<APSIM.Shared.Graphing.Series> GetSeries()
        {
            return GetSeries(GetSeriesDefinitions());
        }

        /// <summary>
        /// Get a list of 'standardised' series objects which are to be shown on the graph.
        /// </summary>
        /// <param name="definitions"></param>
        public IEnumerable<APSIM.Shared.Graphing.Series> GetSeries(IEnumerable<SeriesDefinition> definitions)
        {
            List<APSIM.Shared.Graphing.Series> series = new List<APSIM.Shared.Graphing.Series>();
            foreach (SeriesDefinition definition in definitions)
            {
                if (definition.Type == SeriesType.Bar)
                {
                    // Bar series
                    series.Add(new BarSeries(definition.Title,
                                             definition.Colour,
                                             definition.ShowInLegend,
                                             definition.X.Cast<object>().ToArray(),
                                             definition.Y.Cast<object>().ToArray(),
                                             definition.XFieldName,
                                             definition.YFieldName));
                }
                else if (definition.Type == SeriesType.Scatter)
                {
                    // Line graph
                    Line line = new Line(definition.Line, definition.LineThickness);
                    Marker marker = new Marker(definition.Marker, definition.MarkerSize, definition.MarkerModifier);
                    if (definition.XError == null && definition.YError == null)
                        series.Add(new LineSeries(definition.Title,
                                                  definition.Colour,
                                                  definition.ShowInLegend,
                                                  definition.X.Cast<object>().ToList(),
                                                  definition.Y.Cast<object>().ToList(),
                                                  line,
                                                  marker,
                                                  definition.XFieldName,
                                                  definition.YFieldName));
                    else
                        series.Add(new ErrorSeries($"{definition.Title} Error",
                                                   definition.Colour,
                                                   false,
                                                   definition.X.Cast<object>().ToList(),
                                                   definition.Y.Cast<object>().ToList(),
                                                   line,
                                                   marker,
                                                   LineThickness.Normal,
                                                   LineThickness.Normal,
                                                   definition.XError?.Cast<object>()?.ToList(),
                                                   definition.YError?.Cast<object>()?.ToList(),
                                                   definition.XFieldName,
                                                   definition.YFieldName));
                }
                else if (definition.Type == SeriesType.Region)
                {
                    // Two series, with the area between them shaded
                    series.Add(new RegionSeries(definition.Title,
                                                definition.Colour,
                                                definition.ShowInLegend,
                                                definition.X.Cast<object>().ToList(),
                                                definition.Y.Cast<object>().ToList(),
                                                definition.X2.Cast<object>().ToList(),
                                                definition.Y2.Cast<object>().ToList(),
                                                definition.XFieldName,
                                                definition.YFieldName));
                }
                else if (definition.Type == SeriesType.Area)
                {
                    // Line series with area between line and x-axis shaded
                    IEnumerable<object> x = definition.X.Cast<object>().ToList();
                    IEnumerable<object> y2 = Enumerable.Repeat(0d, x.Count()).Cast<object>().ToList();
                    series.Add(new RegionSeries(definition.Title,
                                                definition.Colour,
                                                definition.ShowInLegend,
                                                x,
                                                definition.Y.Cast<object>().ToList(),
                                                x,
                                                y2,
                                                definition.XFieldName,
                                                definition.YFieldName));
                }
                else if (definition.Type == SeriesType.StackedArea)
                {
                    try
                    {
                        if (definition.Y == null || !definition.Y.Cast<object>().Any())
                            throw new ArgumentNullException($"No y data");
                        Type yDataType = definition.Y.Cast<object>().FirstOrDefault().GetType();
                        if (!APSIM.Shared.Utilities.ReflectionUtilities.IsNumericType(yDataType))
                            throw new ArgumentException($"Y data must be numeric (actual type={yDataType}");

                        // Line series with area between it and previous series is shaded
                        LineSeries previous = series.OfType<LineSeries>().LastOrDefault();
                        if (previous == null)
                        {
                            // This is the first line series to be added. Just use a region
                            // series with y = 0 for the second curve.
                            List<object> x = definition.X.Cast<object>().ToList();
                            IEnumerable<object> y1 = Enumerable.Repeat(0d, x.Count).Cast<object>().ToArray();
                            series.Add(new RegionSeries(definition.Title,
                                                        definition.Colour,
                                                        definition.ShowInLegend,
                                                        x,
                                                        y1,
                                                        x,
                                                        definition.Y.Cast<object>().ToList(),
                                                        definition.XFieldName,
                                                        definition.YFieldName));
                        }
                        else
                        {
                            if (previous.Y == null || !previous.Y.Any())
                                throw new InvalidOperationException($"Previous line series contains no data.");
                            if (!APSIM.Shared.Utilities.ReflectionUtilities.IsNumericType(previous.Y.First().GetType()))
                                throw new InvalidOperationException($"Previous series' y-data is not numeric (type={previous.Y.First().GetType()}). Stacked area only works with numeric y-axis.");

                            // Get data from previous series definition. For now, the y-data must be numeric.
                            object[] x1 = previous.X.ToArray();
                            double[] y1 = previous.Y.Cast<double>().ToArray();

                            // Now get data from the current series (again, y-data must be numeric).
                            object[] x2 = definition.X.Cast<object>().ToArray();
                            double[] y = definition.Y.Cast<double>().ToArray();

                            // Now go through and add the corresponding y-values together
                            // and use this to create a region series.
                            series.Add(new RegionSeries(definition.Title,
                                                        definition.Colour,
                                                        definition.ShowInLegend,
                                                        x1,
                                                        y1.Cast<object>().ToArray(),
                                                        x2,
                                                        CalculateStackedArea(x1, y1, x2, y).Cast<object>().ToArray(),
                                                        definition.XFieldName,
                                                        definition.YFieldName));
                        }
                    }
                    catch (Exception err)
                    {
                        throw new Exception($"Unable to draw stacked area series {definition.Title}", err);
                    }
                }
                else if (definition.Type == SeriesType.Box)
                {
                    // Box/whisker series
                    series.Add(new BoxWhiskerSeries(definition.Title,
                                                    definition.Colour,
                                                    definition.ShowInLegend,
                                                    definition.X.Cast<object>().ToArray(),
                                                    definition.Y.Cast<object>().ToArray(),
                                                    new Line(definition.Line, definition.LineThickness),
                                                    new Marker(definition.Marker, definition.MarkerSize, definition.MarkerModifier),
                                                    definition.XFieldName,
                                                    definition.YFieldName));
                }
                else
                    throw new NotImplementedException($"Unknown series type {definition.Type}");
            }
            return series;
        }

        private static IEnumerable<double> CalculateStackedArea(object[] x1, double[] y1, object[] x2, double[] y)
        {
            // Each element in the y2 series will be the current definition's y value
            // added to the corresponding y value in the previous series.
            List<double> y2 = new List<double>();

            // The previous series' x data must be of the same type as the current
            // series' x data.
            Type xType = x1.First().GetType();
            Type x1Type = x1.First().GetType();
            if (xType != x1Type)
                throw new InvalidOperationException($"Previous line series' x data type ({x1Type}) is different to current line series' x data type ({xType})");

            // Cache a copy of the x2 data cast (casted?) to double.
            bool xIsFloatingPoint = xType == typeof(double) || xType == typeof(float);
            double[] numericX = null;
            if (xIsFloatingPoint)
                numericX = x2.Cast<double>().ToArray();

            for (int i = 0; i < x1.Length; i++)
            {
                object xVal = x1[i]; // x-value in the previous series

                // The previous series might not have exactly the same set of x
                // values as the new series. First we check if the new series
                // contains this x value. If it does not, we do a linear interp
                // to find an appropriate y-value.
                int index = -1;
                if (xIsFloatingPoint)
                    index = APSIM.Shared.Utilities.MathUtilities.SafeIndexOf(numericX, (double)xVal);
                else
                    index = Array.IndexOf(x1, xVal);
                if (index < 0)
                    index = i;

                double yVal = (double)y1[i];
                if (index >= 0)
                    yVal += y[i];
                else if (xIsFloatingPoint)
                    yVal += APSIM.Shared.Utilities.MathUtilities.LinearInterpReal((double)xVal, numericX, y, out bool didInterp);
                y2.Add(yVal);
            }
            return y2;
        }

        /// <summary>
        /// Generated a 'standardised' graph, using the given series definitions.
        /// This is used to speed up the loading of pages of graphs - where we
        /// will load data for all series definitions in parallel ahead of time.
        /// </summary>
        public APSIM.Shared.Documentation.Graph ToGraph(IEnumerable<SeriesDefinition> definitions)
        {
            LegendConfiguration legend = new LegendConfiguration(this.LegendOrientation, this.LegendPosition, !this.LegendOutsideGraph);
            var xAxis = this.Axis.FirstOrDefault(a => a.Position == AxisPosition.Bottom || a.Position == AxisPosition.Top);
            var yAxis = this.Axis.FirstOrDefault(a => a.Position == AxisPosition.Left || a.Position == AxisPosition.Right);
            return new APSIM.Shared.Documentation.Graph(this.Name, this.FullPath, this.GetSeries(definitions), xAxis, yAxis, legend);
        }
    }
}
