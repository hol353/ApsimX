using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.Run;
using Models.Storage;

namespace Models.PostSimulationTools
{
    /// <summary>
    /// This is a post simulation tool that transforms a table into a 'depth' table with layers
    /// going down the rows rather than across the fields. e.g.
    /// SOURCE TABLE:
    ///    Year     Col1(1)  Col1(2)  Col2(1)  Col2(2)
    ///    1970          10       11       12       13
    ///    1971          14       15       16       17
    /// TO:
    ///    Year     Col1  Col2
    ///    1970       10    12
    ///    1970       11    13
    ///    1971       14    16
    ///    1971       15    17
    /// </summary>
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(DataStore))]
    [ValidParent(ParentType = typeof(ParallelPostSimulationTool))]
    [ValidParent(ParentType = typeof(SerialPostSimulationTool))]
    [Serializable]
    public class CreateProfileTable : Model, IPostSimulationTool
    {
        /// <summary>Link to datastore</summary>
        [Link]
        private IDataStore dataStore = null;

        /// <summary>The name of the source table name.</summary>
        [Description("Name of source table")]
        [Display(Type = DisplayType.TableName)]
        public string SourceTableName { get; set; }

        /// <summary>Main run method for performing our calculations and storing data.</summary>
        public void Run()
        {
            if (string.IsNullOrEmpty(SourceTableName))
                throw new Exception($"Empty source table found in {Name}");

            var sourceData = dataStore.Reader.GetData(SourceTableName);
            if (sourceData != null)
            {
                // Determine the layered columns.
                var layeredColumns = DetermineLayeredColumns(sourceData.Rows[0]);

                // Determine the non layered columns.
                var nonLayeredColumns = DetermineNonLayeredColumns(sourceData.Rows[0]);

                // Determine the number of layers.
                var numLayers = DetermineNumberOfLayers(sourceData.Rows[0], layeredColumns.First());

                DataTable table = new DataTable(Name);
                foreach (DataColumn col in nonLayeredColumns)
                    table.Columns.Add(col);
                foreach (DataColumn col in layeredColumns)
                    table.Columns.Add(col);

                foreach (DataRow row  in sourceData.Rows)
                {
                    for (int layerIndex =  1; layerIndex <= numLayers; layerIndex++)
                    {
                        var newRow = table.NewRow();

                        // store the non layered columns
                        foreach (var column in nonLayeredColumns)
                            newRow[column] = row[column.ColumnName];

                        // store the layered columns
                        foreach (var column in layeredColumns)
                            newRow[column] = row[$"{column.ColumnName}({layerIndex})"];

                        table.Rows.Add(newRow);
                    }
                }

                // Give the new data table to the data store.
                dataStore.Writer.WriteTable(table);
            }
        }

        private static IEnumerable<DataColumn> DetermineNonLayeredColumns(DataRow dataRow)
        {
            string[] columnsToExclude = new string[] { "CheckpointName", "SimulationName" };
            List<DataColumn> columnNames = new();
            foreach (DataColumn column in dataRow.Table.Columns)
            {
                if (!columnsToExclude.Contains(column.ColumnName) &&
                    !column.ColumnName.Contains('('))
                    columnNames.Add(new DataColumn(column.ColumnName, column.DataType));
            }
            return columnNames;
        }

        private static IEnumerable<DataColumn> DetermineLayeredColumns(DataRow dataRow)
        {
            List<DataColumn> columnNames = new();
            foreach (DataColumn column in dataRow.Table.Columns)
            {
                string newColumnName = column.ColumnName;
                string bracketedValue = StringUtilities.SplitOffBracketedValue(ref newColumnName, '(', ')');
                if (newColumnName!= string.Empty &&
                    int.TryParse(bracketedValue, out int index) &&
                    !columnNames.Select(col => col.ColumnName).Contains(newColumnName))
                    columnNames.Add(new DataColumn(newColumnName, column.DataType));
            }
            if (!columnNames.Any())
                throw new Exception($"Cannot find any layered columns in table {dataRow.Table.TableName}.");
            return columnNames;
        }

        private static int DetermineNumberOfLayers(DataRow dataRow, DataColumn layeredColumn)
        {
            for (int i = 1; i <= 1000; i++)
            {
                if (!dataRow.Table.Columns.Contains($"{layeredColumn.ColumnName}({i})"))
                    return i - 1;
            }
            throw new Exception($"Cannot deterine the number of layers in table {dataRow.Table.TableName}");
        }
    }
}
