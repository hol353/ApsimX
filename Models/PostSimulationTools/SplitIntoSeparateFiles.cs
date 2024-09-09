using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using APSIM.Shared.Documentation.Extensions;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.Run;
using Models.Storage;

namespace Models.PostSimulationTools
{
    /// <summary>
    /// This is a post simulation tool that splits a data table into separate .csv files, one file
    /// for each SimulationName.
    /// </summary>
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(DataStore))]
    [ValidParent(ParentType = typeof(ParallelPostSimulationTool))]
    [ValidParent(ParentType = typeof(SerialPostSimulationTool))]
    [Serializable]
    public class SplitIntoSeparateFiles : Model, IPostSimulationTool
    {
        /// <summary>Link to datastore</summary>
        [Link]
        private IDataStore dataStore = null;

        /// <summary>The name of the source table name.</summary>
        [Description("Name of source table")]
        [Display(Type = DisplayType.TableName)]
        public string SourceTableName { get; set; }

        /// <summary>The names of the fields to keep.</summary>
        [Description("Names of fields to keep (CSV).")]
        [Display]
        public string[] FieldNames { get; set; }

        /// <summary>Write header row?.</summary>
        [Description("Write header row?")]
        [Display]
        public bool WriteHeaderRow { get; set; }

        /// <summary>Write header row?.</summary>
        [Description("Delimiter e.g. , or \\t")]
        [Display]
        public string Delimiter { get; set; }

        /// <summary>Pattern for naming files e.g. SoilTemp_{Location}_{Soil}.csv.</summary>
        [Description("Pattern for naming files e.g. SoilTemp_{Location}_{Soil}.csv")]
        [Display]
        public string FileNamePattern { get; set; }


        /// <summary>Main run method for performing our calculations and storing data.</summary>
        public void Run()
        {
            if (string.IsNullOrEmpty(SourceTableName))
                throw new Exception($"Empty source table found in {Name}");

            var sourceData = dataStore.Reader.GetData(SourceTableName);
            if (sourceData != null)
            {
                foreach (var group in sourceData.AsEnumerable().GroupBy(r => r["SimulationName"]))
                {
                    // Determine the filename
                    string fileName = Regex.Replace(FileNamePattern, @"(\{[\w\s:]+\})", m =>
                    {
                        string groupName = m.Groups[1].ToString();
                        string macroName = StringUtilities.SplitOffBracketedValue(ref groupName, '{', '}');
                        string format = StringUtilities.SplitOffAfterDelimiter(ref macroName, ":");
                        string value = group.First()[macroName].ToString();
                        if (string.IsNullOrEmpty(format))
                            return value;
                        else
                            return Convert.ToDouble(value).ToString(format);
                    });

                    // Remove unwanted columns.
                    var dt = group.CopyToDataTable();
                    var columnNamesToDelete = dt.Columns.Cast<DataColumn>()
                                                        .Select(col => col.ColumnName)
                                                        .Where(column => !FieldNames.Contains(column))
                                                        .ToArray();
                    foreach (string columnName in columnNamesToDelete)
                        dt.Columns.Remove(columnName);

                    // Create a .csv file and write a header row if necessary.
                    if (Delimiter == "\\t")
                        Delimiter = "\t";  // tab character
                    using StreamWriter writer = new(Path.Combine(Path.GetDirectoryName(dataStore.FileName), fileName));
                    DataTableUtilities.DataTableToText(dt, 0, Delimiter, WriteHeaderRow, writer, decimalFormatString: "F6");
                }
            }
        }
    }
}
