using System.Collections.Generic;
using System.Reflection;
using Models.Core;
using Models;
using Models.Functions;
using APSIM.Shared.Graphing;
using Series = Models.Series;
using UserInterface.Views;
using Models.Utilities;
using APSIM.Documentation.Models;
using Gtk.Sheet;

namespace UserInterface.Presenters
{
	/// <summary>
	/// The presenter class for populating an InitialWater view with an InitialWater model.
	/// </summary>
	public class XYPairsPresenter : IPresenter
    {
        /// <summary>
        /// The XYPairs model.
        /// </summary>
        private XYPairs xYPairs;

        /// <summary>
        /// The initial XYPairs view;
        /// </summary>
        private XYPairsView xYPairsView;

        /// <summary>
        /// The Explorer Presenter
        /// </summary>
        private ExplorerPresenter presenter;

        /// <summary>
        /// The Grid Presenter
        /// </summary>
        private GridPresenter gridPresenter;

        /// <summary>
        /// A reference to the 'graphPresenter' responsible for our graph.
        /// </summary>
        private GraphPresenter graphPresenter;

        /// <summary>
        /// Our graph.
        /// </summary>
        private Graph graph;

        /// <summary>
        /// A list of all properties in the variables grid.
        /// </summary>
        private List<VariableProperty> propertiesInGrid = new List<VariableProperty>();

        /// <summary>
        /// Attach the view to the model.
        /// </summary>
        /// <param name="model">The initial water model</param>
        /// <param name="view">The initial water view</param>
        /// <param name="explorerPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.xYPairs = model as XYPairs;
            this.xYPairsView = view as XYPairsView;
            this.presenter = explorerPresenter as ExplorerPresenter;

            gridPresenter = new GridPresenter();
            gridPresenter.Attach(model, this.xYPairsView.VariablesGrid, this.presenter);
            gridPresenter.AddContextMenuOptions(new string[] { "Cut", "Copy", "Paste", "Delete", "Select All" });

            // Populate the graph.
            this.graph = Utility.Graph.CreateGraphFromResource("ApsimNG.Resources.XYPairsGraph.xml");
            this.xYPairs.Children.Add(this.graph);
            this.graph.Parent = this.xYPairs;
            (this.graph.Series[0] as Series).XFieldName = graph.Parent.FullPath + ".X";
            (this.graph.Series[0] as Series).YFieldName = graph.Parent.FullPath + ".Y";
            this.graphPresenter = new GraphPresenter();
            this.presenter.ApsimXFile.Links.Resolve(graphPresenter);
            this.graphPresenter.Attach(this.graph, this.xYPairsView.Graph, this.presenter);
            string xAxisTitle = LookForXAxisTitle();
            if (xAxisTitle != null)
            {
                xYPairsView.Graph.FormatAxis(AxisPosition.Bottom, xAxisTitle, false, double.NaN, double.NaN, double.NaN, false, false);
            }

            string yAxisTitle = LookForYAxisTitle();
            if (yAxisTitle != null)
            {
                xYPairsView.Graph.FormatAxis(AxisPosition.Left, yAxisTitle, false, double.NaN, double.NaN, double.NaN, false, false);
            }

            xYPairsView.Graph.FormatTitle(xYPairs.Parent.Name);

            this.gridPresenter.CellChanged += OnCellChanged;
            this.presenter.CommandHistory.ModelChanged += this.OnModelChanged;
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            this.gridPresenter.CellChanged -= OnCellChanged;
            this.presenter.CommandHistory.ModelChanged -= this.OnModelChanged;
            this.xYPairs.Children.Remove(this.graph);
        }

        /// <summary>
        /// Look for an x axis title and units.
        /// </summary>
        /// <returns>The x axis title or null if not found.</returns>
        private string LookForXAxisTitle()
        {
            // See if parent has an XProperty property
            PropertyInfo xProperty = xYPairs.Parent.GetType().GetProperty("XProperty");
            if (xProperty != null)
            {
                string propertyName = xProperty.GetValue(xYPairs.Parent, null).ToString();
                IVariable variable = xYPairs.FindByPath(propertyName);
                if (variable != null && variable.UnitsLabel != null)
                {
                    return propertyName + " " + variable.UnitsLabel;
                }

                return propertyName;
            }
            else if (xYPairs.Parent is LinearInterpolationFunction)
            {
                var xValue = xYPairs.Parent.FindChild("XValue");
                if (xValue is VariableReference)
                    return (xValue as VariableReference).VariableName;
                else
                    return "XValue";
            }
            else if (xYPairs.Parent is SubDailyInterpolation)
            {
                return "Air temperature (oC)";
            }
            else if (xYPairs.Parent is SoilTemperatureWeightedFunction)
            {
                return "Weighted soil temperature (oC)";
            }
            else if (xYPairs.Parent is WeightedTemperatureFunction)
            {
                return "Weighted air temperature (oC)";
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Return the y axis title.
        /// </summary>
        /// <returns>The axis title</returns>
        private string LookForYAxisTitle()
        {
            IModel modelContainingLinkField = xYPairs.Parent.Parent;
            var units = AutoDocumentation.GetUnits(modelContainingLinkField, xYPairs.Parent.Name);
            if (!string.IsNullOrEmpty(units))
                return xYPairs.Parent.Name + " (" + units.ToString() + ")";

            return xYPairs.Parent.Name;
        }

        /// <summary>Invoked when a grid cell has changed.</summary>
        /// <param name="dataProvider">The provider that contains the data.</param>
        /// <param name="colIndices">The indices of the columns of the cells that were changed.</param>
        /// <param name="rowIndices">The indices of the rows of the cells that were changed.</param>
        /// <param name="values">The cell values.</param>
        private void OnCellChanged(IDataProvider dataProvider, int[] colIndices, int[] rowIndices, string[] values)
        {
            // Refresh the graph.
            if (this.graph != null)
                this.graphPresenter.DrawGraph();
        }

        /// <summary>
        /// The model has changed. Update the view.
        /// </summary>
        /// <param name="changedModel">The model that has changed.</param>
        private void OnModelChanged(object changedModel)
        {
            // Refresh the graph.
            if (this.graph != null)
                this.graphPresenter.DrawGraph();
        }
    }
}
