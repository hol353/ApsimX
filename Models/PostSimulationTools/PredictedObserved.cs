using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.Run;
using Models.Factorial;
using Models.Storage;

namespace Models.PostSimulationTools
{

    /// <summary>
    /// Reads the contents of a file (in apsim format) and stores into the DataStore.
    /// If the file has a column name of 'SimulationName' then this model will only input data for those rows
    /// where the data in column 'SimulationName' matches the name of the simulation under which
    /// this input model sits.
    /// If the file does NOT have a 'SimulationName' column then all data will be input.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(DataStore))]
    [ValidParent(ParentType = typeof(Folder))]
    [ValidParent(typeof(ParallelPostSimulationTool))]
    [ValidParent(ParentType = typeof(SerialPostSimulationTool))]
    public class PredictedObserved : Model, IPostSimulationTool
    {
        [Link]
        private IDataStore dataStore = null;

        /// <summary> First field name used for match.</summary>
        private string fieldNameUsedForMatch = null;
        /// <summary> Second field name used for match.</summary>
        private string fieldName2UsedForMatch = null;
        /// <summary> Third field name used for match.</summary>
        private string fieldName3UsedForMatch = null;
        /// <summary> Fourth field name used for match.</summary>
        private string fieldName4UsedForMatch = null;

        /// <summary>Gets or sets the name of the predicted table.</summary>
        [Description("Predicted table")]
        [Display(Type = DisplayType.TableName)]
        public string PredictedTableName { get; set; }

        /// <summary>Gets or sets the name of the observed table.</summary>
        [Description("Observed table")]
        [Display(Type = DisplayType.TableName)]
        public string ObservedTableName { get; set; }

        /// <summary>Gets or sets the field name used for match.</summary>
        [Description("Field name to use for matching predicted with observed data")]
        [Display(Type = DisplayType.DropDown, Values = nameof(CommonColumns))]
        public string FieldNameUsedForMatch
        {
            get { return fieldNameUsedForMatch; }
            set
            {
                if (value == "")
                    fieldNameUsedForMatch = null;
                else fieldNameUsedForMatch = value;
            }
        }

        /// <summary>Gets or sets the second field name used for match.</summary>
        [Description("Second field name to use for matching predicted with observed data (optional)")]
        [Display(Type = DisplayType.DropDown, Values = nameof(CommonColumns))]
        public string FieldName2UsedForMatch
        {
            get { return fieldName2UsedForMatch; }
            set
            {
                if (value == "")
                    fieldName2UsedForMatch = null;
                else fieldName2UsedForMatch = value;
            }
        }

        /// <summary>Gets or sets the third field name used for match.</summary>
        [Description("Third field name to use for matching predicted with observed data (optional)")]
        [Display(Type = DisplayType.DropDown, Values = nameof(CommonColumns))]
        public string FieldName3UsedForMatch
        {
            get { return fieldName3UsedForMatch; }
            set
            {
                if (value == "")
                    fieldName3UsedForMatch = null;
                else fieldName3UsedForMatch = value;
            }
        }

        /// <summary>Gets or sets the third field name used for match.</summary>
        [Description("Fourth field name to use for matching predicted with observed data (optional)")]
        [Display(Type = DisplayType.DropDown, Values = nameof(CommonColumns))]
        public string FieldName4UsedForMatch
        {
            get { return fieldName4UsedForMatch; }
            set
            {
                if (value == "")
                    fieldName4UsedForMatch = null;
                else fieldName4UsedForMatch = value;
            }
        }

        /// <summary>
        /// Normally, the only columns added to the PredictedObserved table are
        /// those which exist in both predicted and observed tables. If this is
        /// checked, all columns from both tables will be added to the
        /// PredictedObserved table.
        /// </summary>
        [Tooltip("Normally, the only columns added to the PredictedObserved table are those which exist in both predicted and observed tables. If this is checked, all columns from both tables will be added to the PredictedObserved table.")]
        [Description("Include all columns in PredictedObserved table")]
        public bool AllColumns { get; set; }

        /// <summary>Main run method for performing our calculations and storing data.</summary>
        public void Run()
        {
            try
            {
                if (PredictedTableName == null || ObservedTableName == null)
                    return;

                DataTable dt = dataStore.Reader.GetDataUsingSql("SELECT * FROM _Simulations");
                if (dt == null)
                    throw new ApsimXException(this, "Datastore is empty, please re-run simulations");

                // If neither the predicted nor obseved tables have been modified during
                // the most recent simulations run, don't do anything.
                if (dataStore?.Writer != null &&
                !(dataStore.Writer.TablesModified.Contains(PredictedTableName) || dataStore.Writer.TablesModified.Contains(ObservedTableName)))
                    return;

                IEnumerable<string> predictedDataNames = dataStore.Reader.ColumnNames(PredictedTableName);
                IEnumerable<string> observedDataNames = dataStore.Reader.ColumnNames(ObservedTableName);

                if (predictedDataNames == null)
                    throw new ApsimXException(this, "Could not find model data table: " + PredictedTableName);

                if (!predictedDataNames.Any())
                    throw new Exception($"Predicted table '{PredictedTableName}' does not exist. Check Reports and re-run simulation.");

                if (!observedDataNames.Any())
                    throw new Exception($"Observed table '{ObservedTableName}' does not exist. Check Inputs and refresh datastore.");

                if (observedDataNames == null)
                    throw new ApsimXException(this, "Could not find observed data table: " + ObservedTableName);

                // get the common columns between these lists of columns
                List<string> commonCols = predictedDataNames.Intersect(observedDataNames).ToList();
                if (commonCols.Count == 0)
                    throw new Exception($"Predicted table '{PredictedTableName}' and observed table '{ObservedTableName}' do not have any columns with the same name.");
                // This should be all columns which exist in one table but not both.
                IEnumerable<string> uncommonCols = predictedDataNames.Except(observedDataNames).Union(observedDataNames.Except(predictedDataNames));

                IEnumerable<string> fieldNamesToMatch = new string[] { FieldNameUsedForMatch, FieldName2UsedForMatch, FieldName3UsedForMatch, FieldName4UsedForMatch }
                                                        .Where(f => !string.IsNullOrEmpty(f))
                                                        .Select(f => f == "SimulationName" ? "SimulationID" : f);

                bool useSimulationNameForMatch = fieldNamesToMatch.Contains("SimulationID");

                StringBuilder query = new StringBuilder("SELECT ");
                for (int i = 0; i < commonCols.Count; i++)
                {
                    string s = commonCols[i];
                    if (i != 0)
                        query.Append(", ");

                    if (fieldNamesToMatch.Contains(s))
                        query.Append($"O.\"{s}\"");
                    else
                        query.Append($"O.\"{s}\" AS \"Observed.{s}\", P.\"{s}\" AS \"Predicted.{s}\"");
                }

                // Add columns which exist in one table but not both.
                foreach (string uncommonCol in uncommonCols)
                {
                    // Basically this hack is here to allow error data to be added to the p/o graphs.
                    // This is kind of terrible, but I really don't want to duplicate every
                    // column from both predicted and observed tables if we don't have to.
                    // This does raise the question of whether we should be creating a "PredictedObserved"
                    // table at all, since we're actually duplicating data in the DB by doing so.
                    if (AllColumns || uncommonCol.EndsWith("Error"))
                    {
                        if (predictedDataNames.Contains(uncommonCol))
                            query.Append($", P.\"{uncommonCol}\" as \"Predicted.{uncommonCol}\"");
                        else if (observedDataNames.Contains(uncommonCol))
                            query.Append($", O.\"{uncommonCol}\" as \"Observed.{uncommonCol}\"");
                    }
                }

                query.AppendLine();
                query.AppendLine($"FROM [{ObservedTableName}] O");
                query.AppendLine($"INNER JOIN [{PredictedTableName}] P");
                query.Append($"USING ([CheckpointID]");

                foreach (var fieldName in fieldNamesToMatch)
                    query.Append($", \"{fieldName}\"");
                query.AppendLine(")");

                int checkpointID = dataStore.Writer.GetCheckpointID("Current");
                query.AppendLine("WHERE [CheckpointID] = " + checkpointID);
                query.Replace("O.\"SimulationID\" AS \"Observed.SimulationID\", P.\"SimulationID\" AS \"Predicted.SimulationID\"", "O.\"SimulationID\" AS \"SimulationID\"");
                query.Replace("O.\"CheckpointID\" AS \"Observed.CheckpointID\", P.\"CheckpointID\" AS \"Predicted.CheckpointID\"", "O.\"CheckpointID\" AS \"CheckpointID\"");

                if (Parent is Folder)
                {
                    // Limit it to particular simulations in scope.
                    List<string> simulationNames = new List<string>();
                    foreach (Experiment experiment in this.FindAllInScope<Experiment>())
                    {
                        var names = experiment.GenerateSimulationDescriptions().Select(s => s.Name);
                        simulationNames.AddRange(names);
                    }

                    foreach (Simulation simulation in this.FindAllInScope<Simulation>())
                        if (!(simulation.Parent is Experiment))
                            simulationNames.Add(simulation.Name);

                    if (useSimulationNameForMatch)
                    {
                        query.Append(" AND O.[SimulationID] in (");
                        foreach (string simulationName in simulationNames)
                        {
                            if (simulationName != simulationNames[0])
                                query.Append(',');
                            query.Append(dataStore.Writer.GetSimulationID(simulationName, null));
                        }
                        query.Append(")");
                    }
                }

                DataTable predictedObservedData = dataStore.Reader.GetDataUsingSql(query.ToString());

                if (predictedObservedData != null)
                {
                    // Add in error columns for each data column.
                    foreach (string columnName in commonCols)
                    {
                        if (predictedObservedData.Columns.Contains("Predicted." + columnName) &&
                            predictedObservedData.Columns["Predicted." + columnName].DataType == typeof(double))
                        {
                            var predicted = DataTableUtilities.GetColumnAsDoubles(predictedObservedData, "Predicted." + columnName, CultureInfo.InvariantCulture);
                            var observed = DataTableUtilities.GetColumnAsDoubles(predictedObservedData, "Observed." + columnName, CultureInfo.InvariantCulture);
                            if (predicted.Length > 0 && predicted.Length == observed.Length)
                            {
                                var errorData = MathUtilities.Subtract(predicted, observed);
                                var errorColumnName = "Pred-Obs." + columnName;
                                var errorColumn = predictedObservedData.Columns.Add(errorColumnName, typeof(double));
                                DataTableUtilities.AddColumn(predictedObservedData, errorColumnName, errorData);
                                predictedObservedData.Columns[errorColumnName].SetOrdinal(predictedObservedData.Columns["Predicted." + columnName].Ordinal + 1);
                            }
                        }

                    }

                    // Write table to datastore.
                    predictedObservedData.TableName = this.Name;
                    dataStore.Writer.WriteTable(predictedObservedData);

                    List<string> unitFieldNames = new List<string>();
                    List<string> unitNames = new List<string>();

                    foreach (string fieldName in commonCols)
                    {
                        string units = dataStore.Reader.Units(PredictedTableName, fieldName);
                        if (units != null && units != "()")
                        {
                            string unitsMinusBrackets = units.Replace("(", "").Replace(")", "");
                            unitFieldNames.Add("Predicted." + fieldName);
                            unitNames.Add(unitsMinusBrackets);
                            unitFieldNames.Add("Observed." + fieldName);
                            unitNames.Add(unitsMinusBrackets);
                        }
                    }

                    if (unitNames.Count > 0)
                    {
                        // The Writer replaces tables, rather than appends to them,
                        // so we actually need to re-write the existing units table values
                        // Is there a better way to do this?
                        DataView allUnits = new DataView(dataStore.Reader.GetData("_Units"));
                        allUnits.Sort = "TableName";
                        DataTable tableNames = allUnits.ToTable(true, "TableName");
                        foreach (DataRow row in tableNames.Rows)
                        {
                            string tableName = row["TableName"] as string;
                            List<string> colNames = new List<string>();
                            List<string> unitz = new List<string>();
                            foreach (DataRowView rowView in allUnits.FindRows(tableName))
                            {
                                colNames.Add(rowView["ColumnHeading"].ToString());
                                unitz.Add(rowView["Units"].ToString());
                            }
                            dataStore.Writer.AddUnits(tableName, colNames, unitz);
                        }
                        dataStore.Writer.AddUnits(Name, unitFieldNames, unitNames);
                    }
                }
                else
                {
                    // Determine what went wrong.
                    DataTable predictedData = dataStore.Reader.GetDataUsingSql("SELECT * FROM [" + PredictedTableName + "]");
                    DataTable observedData = dataStore.Reader.GetDataUsingSql("SELECT * FROM [" + ObservedTableName + "]");
                    if (predictedData == null || predictedData.Rows.Count == 0)
                        throw new Exception(Name + ": Cannot find any predicted data.");
                    else if (observedData == null || observedData.Rows.Count == 0)
                        throw new Exception(Name + ": Cannot find any observed data in node: " + ObservedTableName + ". Check for missing observed file or move " + ObservedTableName + " to top of child list under DataStore (order is important!)");
                    else
                        throw new Exception(Name + ": Observed data was found but didn't match the predicted values. Make sure the values in the SimulationName column match the simulation names in the user interface. Also ensure column names in the observed file match the APSIM report column names.");
                }
            }
            catch (Exception err)
            {
                string fileName = "Unknown";
                if ((dataStore as DataStore).Parent is Simulation simulation)
                    fileName = simulation.FileName;
                else if ((dataStore as DataStore).Parent is Simulations simulations)
                    fileName = simulations.FileName;
                throw new Exception($"Error in PredictedObserved tool {Name}. File: {fileName}", err);
            }
        }

        /// <summary>
        /// Returns all columns which exist in both predicted and observed tables.
        /// </summary>
        public string[] CommonColumns()
        {
            if (string.IsNullOrEmpty(PredictedTableName) || string.IsNullOrEmpty(ObservedTableName))
                return new string[0];

            IDataStore storage = FindInScope<IDataStore>();
            if (!storage.Reader.TableNames.Contains(PredictedTableName) || !storage.Reader.TableNames.Contains(ObservedTableName))
                return new string[0];

            IEnumerable<string> predictedColumns = storage.Reader.GetColumns(PredictedTableName).Select(c => c.Item1);
            IEnumerable<string> observedColumns = storage.Reader.GetColumns(ObservedTableName).Select(c => c.Item1);

            List<string> intersect = predictedColumns.Intersect(observedColumns).ToList();

            //These columns will always exist, however may have been added when the table was loaded.
            //We need to add them back in here for PredictObserve
            if (!intersect.Contains("SimulationName"))
            {
                int pos = intersect.IndexOf("SimulationID");
                intersect.Insert(pos + 1, "SimulationName");
            }

            if (!intersect.Contains("CheckpointName"))
            {
                int pos = intersect.IndexOf("CheckpointID");
                intersect.Insert(pos + 1, "CheckpointName");
            }

            return intersect.ToArray();
        }
    }
}
