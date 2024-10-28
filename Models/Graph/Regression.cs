using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using APSIM.Shared.Documentation.Extensions;
using APSIM.Shared.Graphing;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.Run;
using Models.Storage;

namespace Models
{

    /// <summary>
    /// A regression model.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Series))]
    [ValidParent(ParentType = typeof(Graph))]
    public class Regression : Model, ICachableGraphable
    {
        /// <summary>The stats from the regression</summary>
        private List<MathUtilities.RegrStats> stats = new List<MathUtilities.RegrStats>();

        /// <summary>The colours to use for each equation.</summary>
        private List<Color> equationColours = new List<Color>();

        /// <summary>
        /// Gets or sets a value indicating whether a regression should be shown for each series.
        /// </summary>
        /// <value><c>true</c> if [for each series]; otherwise, <c>false</c>.</value>
        [Description("Display regression line and equation for each series?")]
        public bool ForEachSeries { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a regression should be shown for each series.
        /// </summary>
        /// <value><c>true</c> if [for each series]; otherwise, <c>false</c>.</value>
        [Description("Display 1:1 line?")]
        public bool showOneToOne { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether a regression should be shown for each series.
        /// </summary>
        /// <value><c>true</c> if [for each series]; otherwise, <c>false</c>.</value>
        [Description("Display equation?")]
        public bool showEquation { get; set; } = true;

        /// <summary>Get a list of all actual series to put on the graph.</summary>
        /// <param name="storage">Storage service</param>
        /// <param name="simDescriptions">A list of simulation descriptions that are in scope.</param>
        /// <param name="simulationsFilter">Unused simulation names filter.</param>
        public IEnumerable<SeriesDefinition> CreateSeriesDefinitions(IStorageReader storage,
                                                                  List<SimulationDescription> simDescriptions,
                                                                  List<string> simulationsFilter = null)
        {
            Series seriesAncestor = FindAncestor<Series>();
            IEnumerable<SeriesDefinition> definitions;
            if (seriesAncestor == null)
            {
                Graph graph = FindAncestor<Graph>();
                if (graph == null)
                    throw new Exception("Regression model must be a descendant of a series");
                definitions = graph.FindAllChildren<Series>().SelectMany(s => s.CreateSeriesDefinitions(storage, simDescriptions, simulationsFilter));
            }
            else
                definitions = seriesAncestor.CreateSeriesDefinitions(storage, simDescriptions, simulationsFilter);

            return GetSeriesToPutOnGraph(storage, definitions, simulationsFilter);
        }

        /// <summary>Get a list of all actual series to put on the graph.</summary>
        /// <param name="storage">Storage service (required for access to checkpoint names).</param>
        /// <param name="definitions">Series definitions to be used (allows for caching of data).</param>
        /// <param name="simulationsFilter">Unused simulation names filter.</param>
        public IEnumerable<SeriesDefinition> GetSeriesToPutOnGraph(IStorageReader storage, IEnumerable<SeriesDefinition> definitions, List<string> simulationsFilter = null)
        {
            stats.Clear();
            equationColours.Clear();

            int checkpointNumber = 0;
            List<SeriesDefinition> regressionLines = new List<SeriesDefinition>();
            
            if(!Enabled)
                return regressionLines;

            foreach (var checkpointName in storage.CheckpointNames)
            {
                if (checkpointName != "Current" && !storage.GetCheckpointShowOnGraphs(checkpointName)) // smh
                    // If "Show on graphs" is disabled on this checkpoint, skip it.
                    continue;

                // Get all x/y data
                List<double> x = new List<double>();
                List<double> y = new List<double>();
                foreach (SeriesDefinition definition in definitions)
                {
                    if (definition.CheckpointName == checkpointName)
                    {
                        if (definition.X != null && definition.Y != null)
                        {
                            if (ReflectionUtilities.IsNumericType(definition.X.GetType().GetElementType()) && ReflectionUtilities.IsNumericType(definition.Y.GetType().GetElementType()))
                            {
                                x.AddRange(definition.X.Cast<object>().Select(xi => Convert.ToDouble(xi, CultureInfo.InvariantCulture)).ToArray());
                                y.AddRange(definition.Y.Cast<object>().Select(yi => Convert.ToDouble(yi, CultureInfo.InvariantCulture)).ToArray());
                            }
                        }
                    }
                }
                try
                {
                    if (ForEachSeries)
                    {
                        // Display a regression line for each series.
                        // todo - should this also filter on checkpoint name?
                        foreach (SeriesDefinition definition in definitions)
                        {
                            if (HasDefinitionAxesGotNaN(definition.X, definition.Y))
                            {
                                List<List<double>> cleanXAndYLists = CreateCleanDefinitionAxisLists(definition);
                                if (cleanXAndYLists[0].Count() > 1 && cleanXAndYLists[1].Count() > 1)
                                    CreateRegressionsSeriesAndLines(regressionLines, cleanXAndYLists[0], cleanXAndYLists[1], definition.Colour, definition.Title);
                            }
                            else
                                if (definition.X is double[] && definition.Y is double[])
                                CreateRegressionsSeriesAndLines(regressionLines, definition.X, definition.Y, definition.Colour, definition.Title);
                        }
                    }
                    else
                    {
                        var regressionLineName = "Regression line";
                        if (checkpointName != "Current")
                            regressionLineName = "Regression line (" + checkpointName + ")";

                        // Display a single regression line for all data.
                        if (x.Count > 0 && y.Count == x.Count)
                        {
                            SeriesDefinition regressionSeries = PutRegressionLineOnGraph(x, y, ColourUtilities.ChooseColour(checkpointNumber), regressionLineName);
                            if (regressionSeries != null)
                            {
                                regressionLines.Add(regressionSeries);
                                equationColours.Add(ColourUtilities.ChooseColour(checkpointNumber));
                            }
                        }
                    }

                    if (showOneToOne)
                    {
                        if (x.Count > 0 && y.Count == x.Count)
                            regressionLines.Add(Put1To1LineOnGraph(x, y));
                    }
                }
                catch (Exception err)
                {
                    IEnumerable<string> xs = definitions.Select(d => d.XFieldName).Distinct();
                    IEnumerable<string> ys = definitions.Select(d => d.YFieldName).Distinct();
                    string xFields = string.Join(", ", xs);
                    string yFields = string.Join(", ", ys);
                    throw new InvalidOperationException($"Unable to create regression line for checkpoint {checkpointName}. (x variables = [{xFields}], y variables = [{yFields}])", err);
                }
                checkpointNumber++;
            }

            return regressionLines;
        }

        /// <summary>Return a list of extra fields that the definition should read.</summary>
        /// <param name="seriesDefinition">The calling series definition.</param>
        /// <returns>A list of fields - never null.</returns>
        public IEnumerable<string> GetExtraFieldsToRead(SeriesDefinition seriesDefinition)
        {
            return new string[0];
        }

        /// <summary>Puts the regression line and 1:1 line on graph.</summary>
        /// <param name="x">The x data.</param>
        /// <param name="y">The y data.</param>
        /// <param name="colour">The colour of the regression line.</param>
        /// <param name="title">The title to put in the legend.</param>
        private SeriesDefinition PutRegressionLineOnGraph(IEnumerable x, IEnumerable y, Color colour, string title)
        {
            MathUtilities.RegrStats stat = MathUtilities.CalcRegressionStats(title, y, x);
            if (stat != null)
            {
                stats.Add(stat);
                double minimumX = MathUtilities.Min(x);
                double maximumX = MathUtilities.Max(x);
                double minimumY = MathUtilities.Min(y);
                double maximumY = MathUtilities.Max(y);

                //AddPaddingToRegressionLines(ref minimumX, ref maximumX);

                double lowestAxisScale = Math.Min(minimumX, minimumY);
                double largestAxisScale = Math.Max(maximumX, maximumY);

                var regressionDefinition = new SeriesDefinition
                    (title, colour,
                     new double[] { minimumX, maximumX },
                     new double[] { stat.Slope * minimumX + stat.Intercept, stat.Slope * maximumX + stat.Intercept });
                return regressionDefinition;
            }
            throw new Exception($"Unable to generate regression line for series {title} - there is no data");

        }

        /// <summary>Puts the 1:1 line on graph.</summary>
        /// <param name="x">The x data.</param>
        /// <param name="y">The y data.</param>
        private static SeriesDefinition Put1To1LineOnGraph(IEnumerable<double> x, IEnumerable<double> y)
        {
            MathUtilities.GetBounds(x, y, out double minX, out double maxX, out double minY, out double maxY);
            double lowestAxisScale = Math.Min(minX, minY);
            double largestAxisScale = Math.Max(maxX, maxY);

            return new SeriesDefinition
                ("1:1 line", Color.Empty,
                new double[] { lowestAxisScale, largestAxisScale },
                new double[] { lowestAxisScale, largestAxisScale },
                LineType.Dash, MarkerType.None);
        }

        /// <summary>Called by the graph presenter to get a list of all annotations to put on the graph.</summary>
        public IEnumerable<IAnnotation> GetAnnotations()
        {
            if (showEquation)
            {
                for (int i = 0; i < stats.Count; i++)
                {
                    // Add an equation annotation.
                    TextAnnotation equation = new TextAnnotation();
                    StringBuilder text = new StringBuilder();
                    text.AppendLine($"y={stats[i].Slope:F2}x{(stats[i].Intercept >= 0 ? "+" : "")}{stats[i].Intercept:F2}, r\u00B2={stats[i].R2:F2}, n={stats[i].n:F0}");
                    text.AppendLine($"RMSE={stats[i].RMSE:F2}, MAE={stats[i].MAE:F2}, ME={stats[i].ME:F2}");
                    text.AppendLine($"NSE={stats[i].NSE:F2}, RSR={stats[i].RSR:F2}, RMR={stats[i].RMR:F2}");
                    equation.Name = $"Regression{i}";
                    equation.text = text.ToString();
                    equation.colour = equationColours[i];
                    equation.leftAlign = true;
                    equation.textRotation = 0;
                    if (stats.Count > 1)
                    {
                        equation.x = double.MinValue;  // More than one stats equation. Use default positioning
                        equation.y = double.MinValue;
                    }
                    yield return equation;
                }
            }
        }

        /// <summary>
        /// Returns true if NaN is found in either axis IEnumerable. Also returns true if either list is null.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>

        /// <returns></returns>
        private bool HasDefinitionAxesGotNaN(IEnumerable x, IEnumerable y)
        {
            bool nanFoundInAxes = false;
            if (x == null)
                nanFoundInAxes = true;
            if (y == null)
                nanFoundInAxes = true;
            if (x != null)
                foreach (double xValue in x)
                    if (double.IsNaN(xValue))
                        nanFoundInAxes = true;
            if (y != null)
                foreach (double yValue in y)
                    if (double.IsNaN(yValue))
                        nanFoundInAxes = true;
            return nanFoundInAxes;
        }

        /// <summary>
        /// Returns a List of List type double. Each List double will be a List of all values without NaNs.
        /// </summary>
        /// <param name="definition"></param>
        /// <returns></returns>
        private List<List<double>> CreateCleanDefinitionAxisLists(SeriesDefinition definition)
        {
            List<double> cleanDefinitionXList = new();
            List<double> cleanDefinitionYList = new();
            if (definition.X != null)
            {
                for (int i = 0; i < definition.X.Count(); i++)
                {
                    bool isEitherAxisNaN = false;
                    if (double.IsNaN(((double[])definition.X)[i]))
                        isEitherAxisNaN = true;
                    if (double.IsNaN(((double[])definition.Y)[i]))
                        isEitherAxisNaN = true;
                    if (!isEitherAxisNaN)
                    {
                        cleanDefinitionXList.Add(((double[])definition.X)[i]);
                        cleanDefinitionYList.Add(((double[])definition.Y)[i]);
                    }
                }
            }
            return new List<List<double>> { cleanDefinitionXList, cleanDefinitionYList };
        }

        
        private void CreateRegressionsSeriesAndLines(List<SeriesDefinition> regressionLines, IEnumerable xAxisList, IEnumerable yAxisList, Color seriesDefinitionColor, string regressionLineName)
        {
            SeriesDefinition regressionSeries = PutRegressionLineOnGraph(xAxisList, yAxisList, seriesDefinitionColor, regressionLineName);
            if (regressionSeries != null)
            {
                regressionLines.Add(regressionSeries);
                equationColours.Add(seriesDefinitionColor);
            }
        }

        /// <summary>
        /// Adds padding to regression lines.
        /// </summary>
        /// <param name="minimumX"></param>
        /// <param name="maximumX"></param>
        private void AddPaddingToRegressionLines(ref double minimumX, ref double maximumX)
        {
            // Add padding to end of line so that it goes through final point for readability
            double padding = Math.Abs(maximumX - minimumX) * 0.1;
            minimumX -= padding;
            maximumX += padding;
        }
    }
}
