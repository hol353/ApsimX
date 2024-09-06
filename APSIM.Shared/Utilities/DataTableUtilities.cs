using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace APSIM.Shared.Utilities
{


    /// <summary>
    /// Some utilities for manipulating a data table.
    /// </summary>
    public static class DataTableUtilities
    {
        /// <summary>
        /// Add a value to the specified data table
        /// </summary>
        static public void AddValue(DataTable table, string columnName, string value, int startRow, int count)
        {
            string[] Values = new string[count];
            for (int i = 0; i != count; i++)
                Values[i] = value;
            AddColumn(table, columnName, Values, startRow, count);
        }

        /// <summary>
        /// Add a value to the specified data table
        /// </summary>
        static public void AddValue(DataTable table, string columnName, double value, int startRow, int count)
        {
            string[] values = new string[count];
            for (int i = 0; i != count; i++)
            {
                if (value == MathUtilities.MissingValue)
                    values[i] = "";
                else
                    values[i] = value.ToString();
            }
            AddColumn(table, columnName, values, startRow, count);
        }


        /// <summary>
        /// Add a column of values to the specified data table
        /// </summary>
        static public void AddColumn(DataTable table, string columnName, double[] values, int startRow, int count)
        {
            if (table.Columns.IndexOf(columnName) == -1)
                table.Columns.Add(columnName, typeof(double));

            if (values == null)
                return;

            // Make sure there are enough values in the table.
            while (table.Rows.Count < values.Length + startRow)
                table.Rows.Add(table.NewRow());

            int row = startRow;
            for (int Index = 0; Index != values.Length; Index++)
            {
                if (values[Index] != MathUtilities.MissingValue)
                    table.Rows[row][columnName] = values[Index];
                else
                    table.Rows[row][columnName] = DBNull.Value;
                row++;
            }
        }

        /// <summary>
        /// Add a column of values to the specified data table
        /// </summary>
        static public void AddColumn(DataTable table, string columnName, double[] values)
        {
            int Count = 0;
            if (values != null)
                Count = values.Length;
            AddColumn(table, columnName, values, 0, Count);
        }

        /// <summary>
        /// Add a column of values to the specified data table
        /// </summary>
        static public void AddColumn(DataTable table, string columnName, string[] values)
        {
            int count = 0;
            if (values != null)
                count = values.Length;
            AddColumn(table, columnName, values, 0, count);
        }

        /// <summary>
        /// Add a column of values to the specified data table
        /// </summary>
        static public void AddColumn<T>(DataTable table, string columnName, IEnumerable<T> values, int startRow, int count)
        {
            if (table.Columns.IndexOf(columnName) == -1)
                table.Columns.Add(columnName, typeof(T));

            if (values == null)
                return;

            int row = startRow;
            foreach (var value in values)
            {
                while (row >= table.Rows.Count)
                    table.Rows.Add(table.NewRow());

                if (IsValid(value))
                    table.Rows[row][columnName] = value;
                row++;
            }
        }

        static private bool IsValid(object value)
        {
            return value != null &&
                   (value.GetType() == typeof(string) && !string.IsNullOrEmpty((string)value) ||
                    value.GetType() == typeof(double) && !double.IsNaN((double)value));
        }

        /// <summary>
        /// Add a column of values to the specified data table
        /// </summary>
        /// <param name="table">The table to add values to</param>
        /// <param name="columnName">The name of the column</param>
        /// <param name="values">The values to add to the table.</param>
        static public void AddColumnOfObjects(System.Data.DataTable table, string columnName, IEnumerable values)
        {
            // Make sure the table has the specified column
            if (!table.Columns.Contains(columnName))
            {
                if (values == null)
                    table.Columns.Add(columnName);
                else
                    table.Columns.Add(columnName, values.GetType().GetElementType());
            }

            if (values != null)
            {
                int row = 0;
                foreach (object value in values)
                {
                    // Make sure we have enough rows.
                    if (table.Rows.Count <= row)
                    {
                        table.Rows.Add(table.NewRow());
                    }

                    // Determine if this value should be put into the table.
                    // If the value is a double.NaN then don't put into table.
                    // All other values do get inserted.
                    bool insertValue = true;
                    if (value is double && (double.IsNaN((double)value) || (double)value == MathUtilities.MissingValue))
                    {
                        insertValue = false;
                    }

                    // Set the cell value in table.
                    if (insertValue)
                    {
                        table.Rows[row][columnName] = value;
                    }

                    row++;
                }
            }
        }

        /// <summary>
        /// Get a column of values from the specified data table
        /// </summary>
        /// <param name="table"></param>
        /// <param name="columnName"></param>
        /// <param name="culture">Culture settings used for parsing doubles.</param>
        static public double[] GetColumnAsDoubles(DataTable table, string columnName, CultureInfo culture)
        {
            return GetColumnAsDoubles(table, columnName, table.Rows.Count, culture);
        }

        /// <summary>
        /// Get a column as doubles
        /// </summary>
        /// <param name="table"></param>
        /// <param name="columnName"></param>
        /// <param name="numValues"></param>
        /// <param name="culture">Culture settings used for parsing doubles.</param>
        /// <returns></returns>
        static public double[] GetColumnAsDoubles(DataTable table, string columnName, int numValues, CultureInfo culture)
        {
            double[] values = new double[numValues];
            for (int Row = 0; Row != table.Rows.Count && Row != numValues; Row++)
            {
                if (table.Rows[Row][columnName].ToString() == "")
                    values[Row] = double.NaN;
                else
                    values[Row] = Convert.ToDouble(table.Rows[Row][columnName], culture);
            }
            return values;
        }

        /// <summary>
        /// Get a column as doubles
        /// </summary>
        /// <param name="table"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        static public double[] GetColumnAsDoubles(DataView table, string columnName)
        {
            int NumValues = table.Count;
            double[] Values = new double[NumValues];
            for (int Row = 0; Row != table.Count; Row++)
            {
                if (table[Row][columnName].ToString() == "")
                    Values[Row] = double.NaN;
                else
                    Values[Row] = Convert.ToDouble(table[Row][columnName], System.Globalization.CultureInfo.InvariantCulture);
            }
            return Values;
        }

        /// <summary>
        /// Get a column as long integers
        /// </summary>
        /// <param name="table"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        static public long[] GetColumnAsLongInts(DataTable table, string columnName)
        {
            long[] values = new long[table.Rows.Count];
            for (int Row = 0; Row != table.Rows.Count; Row++)
                values[Row] = Convert.ToInt64(table.Rows[Row][columnName], CultureInfo.InvariantCulture);

            return values;
        }

        /// <summary>
        /// Get a column as integers
        /// </summary>
        /// <param name="table"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        static public int[] GetColumnAsIntegers(DataTable table, string columnName)
        {
            int[] values = new int[table.Rows.Count];
            for (int Row = 0; Row != table.Rows.Count; Row++)
                values[Row] = Convert.ToInt32(table.Rows[Row][columnName], CultureInfo.InvariantCulture);

            return values;
        }

        /// <summary>
        /// Get a column as integers
        /// </summary>
        /// <param name="table"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        static public int[] GetColumnAsIntegers(DataView table, string columnName)
        {
            int numValues = table.Count;
            int[] values = new int[numValues];
            for (int row = 0; row != table.Count; row++)
            {
                if (table[row][columnName].ToString() == "")
                    values[row] = int.MinValue;
                else
                    values[row] = Convert.ToInt32(table[row][columnName], System.Globalization.CultureInfo.InvariantCulture);
            }
            return values;
        }

        /// <summary>
        /// Get a column as doubles.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="columnName"></param>
        /// <param name="numValues"></param>
        /// <param name="startRow"></param>
        /// <param name="culture">Culture settings used to parse string values to double.</param>
        /// <returns></returns>
        static public double[] GetColumnAsDoubles(DataTable table, string columnName, int numValues, int startRow, CultureInfo culture)
        {
            double[] values;
            values = new double[numValues - startRow];

            int index = 0;
            for (int row = startRow; row != table.Rows.Count && index != numValues; row++)
            {
                if (table.Rows[row][columnName].ToString() == string.Empty)
                    values[index] = double.NaN;
                else
                {
                    try
                    {
                        values[index] = Convert.ToDouble(table.Rows[row][columnName], culture);
                    }
                    catch (Exception)
                    {
                        values[index] = double.NaN;
                    }
                }
                index++;
            }
            if (MathUtilities.ValuesInArray(values))
                return values;
            else
                return null;
        }

        /// <summary>Get columns as doubles within specific data range</summary>
        /// <param name="table">The data table to get the values from</param>
        /// <param name="colName">The name of the column to be referenced in the data table</param>
        /// <param name="firstDate">The start date of the data to be returned</param>
        /// <param name="lastDate">The end date of the data to be returned</param>
        /// <returns>The specified column of data, filtered by the dates, as an array of doubles. </returns>
        public static double[] GetColumnAsDoubles(DataTable table, string colName, DateTime firstDate, DateTime lastDate)
        {
            var result = from row in table.AsEnumerable()
                         where (DataTableUtilities.GetDateFromRow(row) >= firstDate &&
                                DataTableUtilities.GetDateFromRow(row) <= lastDate)
                         select new
                         {
                             val = row.Field<float>(colName)
                         };

            List<double> rValues = new List<double>();
            foreach (var row in result)
                rValues.Add(row.val);

            return rValues.ToArray();
        }



        /// <summary>
        /// Get a column of values from the specified data table
        /// </summary>
        static public string[] GetColumnAsStrings(DataTable table, string columnName, CultureInfo culture)
        {
            return GetColumnAsStrings(table, columnName, table.Rows.Count, culture);
        }

        /// <summary>
        /// Get a column as strings
        /// </summary>
        /// <param name="table"></param>
        /// <param name="columnName"></param>
        /// <param name="numValues"></param>
        /// <param name="culture">The culture settings used to convert to string values.</param>
        /// <returns></returns>
        static public string[] GetColumnAsStrings(DataTable table, string columnName, int numValues, CultureInfo culture)
        {
            string[] values = new string[numValues];
            for (int row = 0; row != table.Rows.Count && row != numValues; row++)
                values[row] = Convert.ToString(table.Rows[row][columnName], culture);
            return values;
        }

        /// <summary>
        /// Get a column as strings.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="columnName"></param>
        /// <param name="numValues"></param>
        /// <param name="startRow"></param>
        /// <param name="culture">Culture settings used to convert to string values.</param>
        /// <returns></returns>
        static public string[] GetColumnAsStrings(DataTable table, string columnName, int numValues, int startRow, CultureInfo culture)
        {
            string[] values = new string[numValues];
            int index = 0;
            for (int Row = startRow; Row != table.Rows.Count && index != numValues; Row++)
            {
                values[index] = Convert.ToString(table.Rows[Row][columnName], culture);
                index++;
            }
            return values;
        }

        /// <summary>
        /// Get a column as strings
        /// </summary>
        /// <param name="view">The data view.</param>
        /// <param name="columnName">The column name.</param>
        /// <returns></returns>
        static public string[] GetColumnAsStrings(DataView view, string columnName)
        {
            string[] values = new string[view.Count];
            for (int row = 0; row != view.Count; row++)
                values[row] = view[row][columnName].ToString();

            return values;
        }

        /// <summary>
        /// Get a column as dates.
        /// </summary>
        /// <param name="table">The data table that contains the data required</param>
        /// <param name="columnName">The name of the Date Column</param>
        /// <param name="numValues"></param>
        /// <param name="startRow"></param>
        /// <returns>An array of dates</returns>
        static public DateTime[] GetColumnAsDates(DataTable table, string columnName, int numValues, int startRow)
        {
            DateTime[] values = new DateTime[numValues];
            int index = 0;
            for (int row = startRow; row < table.Rows.Count && index < numValues; row++)
            {
                string date = DateUtilities.ValidateDateString(table.Rows[row][columnName].ToString());
                if (date == null)
                    values[index] = DateTime.MinValue;
                else
                    values[index] = DateUtilities.GetDate(date);
                index++;
            }
            return values;
        }

        /// <summary>
        /// Get a column as dates.
        /// </summary>
        /// <param name="table">The data table that contains the data required</param>
        /// <param name="columnName">The name of the Date Column</param>
        /// <returns>An array of dates</returns>
        static public DateTime[] GetColumnAsDates(DataTable table, string columnName)
        {
            DateTime[] values = new DateTime[table.Rows.Count];
            for (int row = 0; row != table.Rows.Count; row++)
            {
                string date = DateUtilities.ValidateDateString(table.Rows[row][columnName].ToString());
                if (date == null)
                    values[row] = DateTime.MinValue;
                else
                    values[row] = DateUtilities.GetDate(date);
            }
            return values;
        }

        /// <summary>
        /// Get a column as dates
        /// </summary>
        /// <param name="view">The data view.</param>
        /// <param name="columnName">The column name.</param>
        /// <returns>An array of dates</returns>
        static public DateTime[] GetColumnAsDates(DataView view, string columnName)
        {
            DateTime[] values = new DateTime[view.Count];
            for (int row = 0; row != view.Count; row++)
            {
                if (Convert.IsDBNull(view[row][columnName]))
                    values[row] = DateTime.MinValue;
                else
                    values[row] = Convert.ToDateTime(view[row][columnName], CultureInfo.InvariantCulture);
            }

            return values;
        }

        /// <summary>Get a column as dates.</summary>
        /// <param name="table">The data table that contains the data required</param>
        /// <param name="colName">The name of the Date Column</param>
        /// <param name="firstDate">The Start date for the date range required</param>
        /// <param name="lastDate">The ending date for the date range required</param>
        /// <returns>An array of dates</returns>
        static public DateTime[] GetColumnAsDates(DataTable table, string colName, DateTime firstDate, DateTime lastDate)
        {
            //where row.Field<DateTime>(colName) >= firstDate
            var result = from row in table.AsEnumerable()
                         where (DataTableUtilities.GetDateFromRow(row) >= firstDate &&
                                DataTableUtilities.GetDateFromRow(row) <= lastDate)
                         select row;

            List<DateTime> rValues = new List<DateTime>();
            foreach (var row in result)
                rValues.Add(Convert.ToDateTime(row[colName], CultureInfo.InvariantCulture));

            return rValues.ToArray();

        }

        /// <summary>Gets string array of the months from a datatable</summary>
        /// <param name="table">The datatable to seach</param>
        /// <param name="firstDate">The start of the date range to search</param>
        /// <param name="lastDate">The end of the date range to search</param>
        /// <returns>A String array containing the distinct month names (abbreviated), in order, ie, "Jan, Feb, Mar"</returns>
        static public string[] GetDistinctMonthsasStrings(DataTable table, DateTime firstDate, DateTime lastDate)
        {
            //where row.Field<DateTime>(colName) >= firstDate
            var result = (from row in table.AsEnumerable()
                          where (DataTableUtilities.GetDateFromRow(row) >= firstDate &&
                                 DataTableUtilities.GetDateFromRow(row) <= lastDate)
                          orderby DataTableUtilities.GetDateFromRow(row)
                          select new
                          {
                              Month = DataTableUtilities.GetDateFromRow(row).Month
                          }).Distinct();

            List<string> rValues = new List<string>();
            foreach (var row in result)
                rValues.Add(CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(row.Month));

            return rValues.ToArray();
        }


        /// <summary>
        /// Get a list of column names
        /// </summary>
        static public string[] GetColumnNames(this DataTable table)
        {
            if (table != null)
            {
                string[] columnNames = new string[table.Columns.Count];
                for (int col = 0; col != table.Columns.Count; col++)
                    columnNames[col] = table.Columns[col].ColumnName;
                return columnNames;
            }
            else
                return new string[0];
        }

        /// <summary>
        /// Get number of non blank values in column of the specified data table
        /// </summary>
        static public int GetNumberOfNonBlankRows(DataTable table, string columnName)
        {
            for (int row = table.Rows.Count - 1; row >= 0; row--)
            {
                if (table.Rows[row][columnName].ToString() != "")
                    return row + 1;
            }
            return table.Rows.Count;
        }

        /// <summary>
        /// Get a date from the specified row
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        static public DateTime GetDateFromRow(DataRow row)
        {
            // ---------------------------------------------------------------------
            // Try and return a date for the specified row in the specified table.
            // Will throw if there is no date found.
            // ---------------------------------------------------------------------
            int Year = 0;
            int Month = 0;
            int Day = 0;
            for (int Col = 0; Col != row.Table.Columns.Count; Col++)
            {
                string ColumnName = row.Table.Columns[Col].ColumnName.ToLower();
                if (ColumnName == "date")
                {
                    if (row.Table.Columns[Col].DataType == typeof(DateTime))
                        return (DateTime)row[Col];
                    else
                        return DateTime.Parse(row[Col].ToString(), CultureInfo.InvariantCulture);
                }
                else if (ColumnName == "year")
                    Year = Convert.ToInt32(row[Col], CultureInfo.InvariantCulture);
                else if (ColumnName == "month")
                    Month = Convert.ToInt32(row[Col], CultureInfo.InvariantCulture);
                else if (ColumnName == "day")
                    Day = Convert.ToInt32(row[Col], CultureInfo.InvariantCulture);
            }
            if (Year > 0)
            {
                if (Day > 0)
                    return new DateTime(Year, 1, 1).AddDays(Day - 1);
                else
                    Day = 1;
                if (Month == 0)
                    Month = 1;
                return new DateTime(Year, Month, Day);
            }
            throw new Exception("Cannot find a date columns. " +
                                "There must be one of the following combinations of columns: " +
                                "[a date column] OR " +
                                "[a year and day column] OR" +
                                "[a year, month and day column]");
        }

        /// <summary>
        /// Filter the specified table for the given date range.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="startYear"></param>
        /// <param name="endYear"></param>
        /// <returns></returns>
        static public DataView FilterTableForYear(DataTable table, int startYear, int endYear)
        {
            // ---------------------------------------------------------------------
            // Filter the specified data table for the specified year range.
            // ---------------------------------------------------------------------
            DataView view = new DataView();
            view.Table = table;
            if (table.Columns.IndexOf("year") != -1)
                view.RowFilter = "Year >= " + startYear.ToString() + " and Year <= " + endYear;

            else if (table.Columns.IndexOf("date") != -1)
            {
                // This uses system locale to decode a date string, we should really
                // be using the units attribute instead.
                DateTime d1 = new DateTime(startYear, 1, 1);
                string filter = string.Format(System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat, "Date >= #{0}#", d1);
                DateTime d2 = new DateTime(endYear, 12, 31);
                filter += string.Format(System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat, "AND Date <= #{0}#", d2);
                view.RowFilter = filter;
            }
            else
                throw new Exception("Cannot find a date column in data");
            return view;
        }

        /// <summary>
        /// Get the distinct rows from the specified table using the values in the specified column
        /// </summary>
        /// <param name="table"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        static public List<string> GetDistinctValues(DataTable table, string columnName)
        {
            // ---------------------------------------------------------------------
            // Return a list of unique values for the specified column in the
            // specified table.
            // ---------------------------------------------------------------------
            List<string> values = new List<string>();

            foreach (DataRow row in table.Rows)
            {
                if (values.IndexOf(row[columnName].ToString()) == -1)
                    values.Add(row[columnName].ToString());
            }
            return values;
        }

        /// <summary>
        /// Get a list of monthly sums for the specified data view.
        /// </summary>
        /// <param name="view"></param>
        /// <returns></returns>
        static public DataTable MonthlySums(DataView view)
        {
            // ----------------------------------------------------------------------------------
            // From the daily data in the Metfile object, calculate monthly sums of all variables
            // ----------------------------------------------------------------------------------
            DataTable monthlyData = new DataTable();
            monthlyData.TableName = "MonthlyData";

            if (view.Table.Columns.IndexOf("Date") == -1)
                monthlyData.Columns.Add("Date", Type.GetType("System.DateTime"));

            foreach (DataColumn Column in view.Table.Columns)
                monthlyData.Columns.Add(Column.ColumnName, Column.DataType);

            int previousMonth = 0;
            DataRow monthRow = null;
            for (int row = 0; row != view.Count; row++)
            {
                DateTime rowDate = DataTableUtilities.GetDateFromRow(view[row].Row);
                if (previousMonth != rowDate.Month)
                {
                    monthRow = monthlyData.NewRow();
                    monthlyData.Rows.Add(monthRow);
                    monthRow["Date"] = rowDate;
                    previousMonth = rowDate.Month;
                }

                foreach (DataColumn Column in view.Table.Columns)
                {
                    if (Convert.IsDBNull(monthRow[Column.ColumnName]))
                        monthRow[Column.ColumnName] = view[row][Column.ColumnName];
                    else if (Column.DataType.ToString() == "System.Single" || Column.DataType.ToString() == "System.Double")
                        monthRow[Column.ColumnName] = Convert.ToDouble(monthRow[Column.ColumnName], System.Globalization.CultureInfo.InvariantCulture) +
                                                      Convert.ToDouble(view[row][Column.ColumnName], System.Globalization.CultureInfo.InvariantCulture);
                    else
                        monthRow[Column.ColumnName] = view[row][Column.ColumnName];

                }
            }
            return monthlyData;
        }

/// <summary>
/// Read a file into a datatable.
/// </summary>
/// <param name="filePath"></param>
/// <param name="convertFunc">Optional function to convert data types</param>
/// <param name="delimiter">Delimiter to use</param>
/// <param name="linesToSkip"></param>
public static DataTable ReadDataTable(string filePath, char delimiter = ' ', int linesToSkip = 0,
                                      Func<int, string, object> convertFunc = null)
{
    DataTable tbl = new DataTable();

    string[] lines = File.ReadAllLines(filePath)
                         .Skip(linesToSkip)
                         .ToArray();
    var headings = lines[0].Split(delimiter);
    for(int col = 0; col < headings.Length; col++)
        tbl.Columns.Add(new DataColumn(headings[col]));

    foreach(string line in lines.Skip(1))
    {
        var cols = line.Split(delimiter);

        DataRow dr = tbl.NewRow();
        for(int cIndex=0; cIndex < headings.Length; cIndex++)
        {
            string word = cols[cIndex];
            object value;
            if (convertFunc == null)
                value = word;
            else
                value = convertFunc(cIndex, word);

            dr[cIndex] = value;
        }

        tbl.Rows.Add(dr);
    }

    return tbl;
}


        /// <summary>
        /// Write the specified DataTable to a CSV string, excluding the specified column names.
        /// </summary>
        static public void DataTableToText(DataTable data, int startColumnIndex, string delimiter, bool showHeadings, TextWriter writer, bool excelFriendly = false, string decimalFormatString = "F3")
        {
            // Convert the data table to a table of strings. This will make it easier for
            // calculating widths.
            DataTable stringTable = new DataTable();
            foreach (DataColumn col in data.Columns)
                stringTable.Columns.Add(col.ColumnName, typeof(string));
            foreach (DataRow row in data.Rows)
            {
                DataRow newRow = stringTable.NewRow();
                foreach (DataColumn column in data.Columns)
                    newRow[column.Ordinal] = ConvertObjectToString(row[column], decimalFormatString);
                stringTable.Rows.Add(newRow);
            }

            //Sort Rows by SimulationName in alphabetical order
            if (stringTable.Columns.Contains("SimulationName"))
            {
                DataView dv = stringTable.DefaultView;
                dv.Sort = "SimulationName ASC";
                if (stringTable.Columns.Contains("Clock.Today"))
                    dv.Sort += ", Clock.Today ASC";
                stringTable = dv.ToTable();
            }

            // Need to work out column widths
            List<int> columnWidths = new List<int>();
            foreach (DataColumn column in stringTable.Columns)
            {
                int width = column.ColumnName.Length;
                foreach (DataRow row in stringTable.Rows)
                    width = System.Math.Max(width, row[column].ToString().Length);
                columnWidths.Add(width);
            }

            // Write out column headings.
            if (showHeadings)
            {
                for (int i = startColumnIndex; i < stringTable.Columns.Count; i++)
                {
                    if (i > startColumnIndex)
                        writer.Write(delimiter);
                    if (excelFriendly)
                        writer.Write(stringTable.Columns[i].ColumnName);
                    else
                        writer.Write(stringTable.Columns[i].ColumnName);
                }
                writer.Write(Environment.NewLine);
            }

            // Write out each row.
            foreach (DataRow row in stringTable.Rows)
            {
                for (int i = startColumnIndex; i < stringTable.Columns.Count; i++)
                {
                    if (i > startColumnIndex)
                        writer.Write(delimiter);
                    if (excelFriendly)
                        writer.Write(row[i]);
                    else
                        writer.Write(row[i]);
                }
                writer.Write(Environment.NewLine);
            }
        }

        /// <summary>
        /// Convert the specified object to a string.
        /// </summary>
        private static string ConvertObjectToString(object obj, string decimalFormatString)
        {
            if (obj is DateTime)
            {
                DateTime D = Convert.ToDateTime(obj, CultureInfo.InvariantCulture);
                return D.ToString("yyyy-MM-dd");
            }
            else if (obj is float || obj is double)
                return string.Format("{0:" + decimalFormatString + "}", obj);
            else
                return obj.ToString();
        }

        /// <summary>Merges the columns and rows from one specified table to another.</summary>
        /// <remarks>The builtin DataTable.merge needs the fields to be the same type.
        /// This method will instead try and conver the fields.</remarks>
        /// <param name="from">The from table</param>
        /// <param name="to">The destination table.</param>
        public static void CopyRows(DataTable from, DataTable to)
        {
            foreach (DataRow row in from.Rows)
            {
                DataRow newRow = to.NewRow();
                foreach (DataColumn column in from.Columns)
                {
                    if (!Convert.IsDBNull(row[column]))
                    {
                        if (to.Columns.Contains(column.ColumnName))
                        {
                            Type toDataType = to.Columns[column.ColumnName].DataType;
                            bool conversionNeeded = column.DataType != toDataType;
                            if (conversionNeeded)
                            {
                                if (row[column].ToString() == "-1.#IND00")
                                    newRow[column.ColumnName] = double.NaN;
                                else if (toDataType == typeof(float))
                                    newRow[column.ColumnName] = Convert.ToSingle(row[column], CultureInfo.InvariantCulture);
                                else if (toDataType == typeof(double))
                                    newRow[column.ColumnName] = Convert.ToDouble(row[column], System.Globalization.CultureInfo.InvariantCulture);
                                else if (toDataType == typeof(int))
                                    newRow[column.ColumnName] = Convert.ToInt32(row[column], CultureInfo.InvariantCulture);
                                else if (toDataType == typeof(string))
                                    newRow[column.ColumnName] = row[column].ToString();
                                else
                                    throw new Exception("Cannot convert from type '" + column.DataType.ToString() +
                                                        "' to type '" + toDataType.ToString() + "'");
                            }
                            else
                                newRow[column.ColumnName] = row[column];
                        }
                    }
                }
                to.Rows.Add(newRow);
            }
        }

        /// <summary>
        /// Merge a collection of data tables.
        /// </summary>
        /// <param name="tables">The tables to be merged.</param>
        public static DataTable Merge(IEnumerable<DataTable> tables)
        {
            if (tables == null)
                return null;

            DataTable result = new DataTable(tables.FirstOrDefault()?.TableName);
            foreach (DataTable table in tables)
                result.Merge(table);
            return result;
        }

        /// <summary>
        /// Rename a column
        /// </summary>
        /// <param name="table">The table.</param>
        /// <param name="oldName">The old column name.</param>
        /// <param name="newName">The new column name.</param>
        public static void RenameColumn(DataTable table, string oldName, string newName)
        {
            var i = table.Columns.IndexOf(oldName);
            if (i == -1)
                throw new Exception("Cannot find column: " + oldName);
            table.Columns[i].ColumnName = newName;
        }

        /// <summary>
        /// Reorder a column in a data table.
        /// </summary>
        /// <param name="table">The table.</param>
        /// <param name="name">The column name to reposition.</param>
        /// <param name="newColumnIndex">The new column index.</param>
        public static void ReorderColumn(DataTable table, string name, int newColumnIndex)
        {
            var i = table.Columns.IndexOf(name);
            if (i == -1)
                throw new Exception("Cannot find column: " + name);
            table.Columns[i].SetOrdinal(newColumnIndex);
        }

        /// <summary>
        /// Delete a column in a data table.
        /// </summary>
        /// <param name="table">The table.</param>
        /// <param name="name">The column name to delete.</param>
        public static void DeleteColumn(DataTable table, string name)
        {
            table.Columns.Remove(name);
        }

        /// <summary>
        /// Copy all rows in 'from' to the 'to' table, inserting them at 'index'
        /// </summary>
        /// <param name="from">Source data table</param>
        /// <param name="to">Destination data table</param>
        /// <param name="index">Index to insert the new rows.</param>
        public static void InsertRowsAt(DataTable from, DataTable to, int index)
        {
            for (int i = 0; i < from.Rows.Count; i++)
            {
                // Create a new row.
                DataRow newRow = to.NewRow();
                newRow.ItemArray = from.Rows[i].ItemArray;
                to.Rows.InsertAt(newRow, index + i);
            }
        }

        /// <summary>
        /// Return true if the specified data column has a DbNull in every row.
        /// </summary>
        /// <param name="dataColumn">The column to check.</param>
        public static bool ColumnIsNull(DataColumn dataColumn)
        {
            foreach (DataRow row in dataColumn.Table.Rows)
                if (!Convert.IsDBNull(row[dataColumn]))
                    return false;
            return true;
        }

        /// <summary>
        /// Convert a data table to markdown syntax.
        /// </summary>
        /// <param name="table">The data table to convert to string.</param>
        /// <param name="writeHeaders">Write the table headers to html?</param>
        public static string ToMarkdown(DataTable table, bool writeHeaders)
        {
            StringBuilder result = new StringBuilder();

            int[] columnWidths = new int[table.Columns.Count];
            for (int i = 0; i < table.Columns.Count; i++)
                columnWidths[i] = table.Columns[i].ColumnName.Length;

            foreach (DataRow row in table.Rows)
                for (int i = 0; i < table.Columns.Count; i++)
                    if (row[i] != null && row[i].ToString().Length > columnWidths[i])
                        columnWidths[i] = row[i].ToString().Length;

            // Add 1 to all column widths.
            columnWidths = columnWidths.Select(x => x + 1).ToArray();

            // Write table headings.
            if (writeHeaders)
            {
                result.Append("|");
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    int padding = columnWidths[i] - table.Columns[i].ColumnName.Length;
                    result.Append(table.Columns[i].ColumnName);
                    result.Append(new string(' ', padding));
                    result.Append("|");
                }
                result.AppendLine();

                // Need a separator between headings and data.
                // Needs to be hyphens (-) separated by a | at each column boundary.
                result.Append('|');
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    result.Append(new string('-', columnWidths[i]));
                    result.Append('|');
                }
                result.AppendLine();
            }

            // Write table rows.
            foreach (DataRow row in table.Rows)
            {
                result.Append('|');
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    int padding = columnWidths[i] - row[i]?.ToString()?.Length ?? 0;
                    result.Append(row[i]);
                    result.Append(new string(' ', padding));
                    result.Append("|");
                }
                result.AppendLine();
            }

            return result.ToString();
        }

        /// <summary>
        /// Convert a data table to html snippet.
        /// </summary>
        /// <param name="table">The data table to convert to string.</param>
        /// <param name="writeHeaders">Write the table headers to html?</param>
        /// <returns>HTML snippet. Never returns null.</returns>
        public static string ToHTML(DataTable table, bool writeHeaders)
        {
            var builder = new StringBuilder();
            if (table.Rows.Count > 0)
            {
                builder.Append("<table border='1px' cellpadding='5' cellspacing='0' ");
                builder.AppendLine("style='border: solid 1px Silver;'>");
                if (writeHeaders)
                {
                    builder.AppendLine("<tr align='left' valign='top'>");
                    foreach (DataColumn c in table.Columns)
                    {
                        builder.Append("<td align='left' valign='top'><b>");
                        builder.Append(c.ColumnName);
                        builder.AppendLine("</b></td>");
                    }
                    builder.AppendLine("</tr>");
                }
                foreach (DataRow r in table.Rows)
                {
                    builder.AppendLine("<tr align='left' valign='top'>");
                    foreach (DataColumn c in table.Columns)
                    {
                        builder.AppendLine("<td align='left' valign='top'>");
                        builder.AppendLine(r[c.ColumnName].ToString());
                        builder.AppendLine("</td>");
                    }
                    builder.AppendLine("</tr>");
                }
                builder.AppendLine("</table>");

            }
            return builder.ToString();
        }

        /// <summary>
        /// Convert a csv string into a data table. Note that the datatable of each
        /// column will be string.
        /// </summary>
        /// <param name="tableName">Name of the table to create.</param>
        /// <param name="csv">The csv string.</param>
        /// <returns>The created datatable.</returns>
        public static DataTable FromCSV(string tableName, string csv)
        {
            var sr = new StringReader(csv);
            string[] headers = sr.ReadLine().Split(',');
            DataTable dt = new DataTable(tableName);
            foreach (string header in headers)
                dt.Columns.Add(header.Trim());

            var line = sr.ReadLine();
            while (!string.IsNullOrEmpty(line))
            {
                string[] rows = Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
                DataRow dr = dt.NewRow();
                for (int i = 0; i < headers.Length; i++)
                    dr[i] = rows[i];
                dt.Rows.Add(dr);
                line = sr.ReadLine();
            }

            // Go through all columns and see if the column datatype can be made more
            // specific than string e.g. convert string to double
            for (int colIndex = 0; colIndex < dt.Columns.Count; colIndex++)
            {
                Type typeToConvertTo = null;
                foreach (DataRow row in dt.Rows)
                {
                    if (Int32.TryParse((string)row[colIndex], out int intValue))
                    {
                        if (typeToConvertTo == null)
                            typeToConvertTo = typeof(int);
                        else if (typeToConvertTo != typeof(int))
                        {
                            typeToConvertTo = null;
                            break;
                        }
                    }
                    else if (Double.TryParse((string)row[colIndex], out double doubleValue))
                    {
                        if (typeToConvertTo == null)
                            typeToConvertTo = typeof(double);
                        else if (typeToConvertTo != typeof(double))
                        {
                            typeToConvertTo = null;
                            break;
                        }
                    }
                    else if (DateTime.TryParse((string)row[colIndex], out DateTime dateValue))
                    {
                        if (typeToConvertTo == null)
                            typeToConvertTo = typeof(DateTime);
                        else if (typeToConvertTo != typeof(DateTime))
                        {
                            typeToConvertTo = null;
                            break;
                        }
                    }
                    else
                        typeToConvertTo = typeof(string);
                }
                if (typeToConvertTo != null && typeToConvertTo != typeof(string))
                    ConvertDataTableOfColumn(dt, dt.Columns[colIndex].ColumnName, typeToConvertTo);
            }

            return dt;
        }

        /// <summary>Convert a DataTable to a comma separated text string.</summary>
        /// <param name="table">The table to convert.</param>
        public static string ToCSV(DataTable table)
        {
            System.IO.StringWriter writer = new System.IO.StringWriter();
            DataTableUtilities.DataTableToText(table, 0, ",", true, writer);
            return writer.ToString();
        }

        /// <summary>
        /// Convert a csv string into a data table. Note that the datatable of each
        /// column will be string.
        /// </summary>
        /// <param name="table">The data table.</param>
        /// <param name="columnName">The column name to convert.</param>
        /// <param name="newDataType">The new data type of the column.</param>
        public static void ConvertDataTableOfColumn(DataTable table, string columnName, Type newDataType)
        {
            var oldColumn = table.Columns[columnName];
            if (oldColumn == null)
                throw new Exception("Cannot find column " + columnName);
            var newColumn = table.Columns.Add("NewColumn", newDataType);
            newColumn.SetOrdinal(oldColumn.Ordinal);

            // Move all data to new column.
            foreach (DataRow row in table.Rows)
            {
                if (string.IsNullOrEmpty(row[oldColumn].ToString()))
                {
                    if (newDataType == typeof(double))
                        row[newColumn] = double.NaN;
                    else
                        row[newColumn] = Convert.DBNull;
                }
                else
                    row[newColumn] = Convert.ChangeType(row[oldColumn], newDataType);
            }

            // Delete old column
            table.Columns.Remove(oldColumn);

            // Rename new column.
            newColumn.ColumnName = columnName;
        }

        /// <summary>
        /// Utility method which sums a specific field in a collection of data rows.
        /// </summary>
        /// <param name="rows">Rows to be summed.</param>
        /// <param name="fieldName">Name of the field to be summed.</param>
        /// <returns></returns>
        private static double SumOfRows(DataRow[] rows, string fieldName)
        {
            double total = 0;
            foreach (DataRow row in rows)
            {
                double field;
                if (double.TryParse(row.Field<object>(fieldName)?.ToString(), out field))
                    total += field;
                else
                {
                    DateTime date = DataTableUtilities.GetDateFromRow(row);
                    throw new Exception("Invalid data in column " + fieldName + " on date " + date.ToShortDateString() + " (day of year = " + date.DayOfYear + ")");
                }
            }
            return total;
        }

        /// <summary>
        /// Utility method which averages a specific field in a collection of data rows.
        /// </summary>
        /// <param name="rows">Rows to be summed.</param>
        /// <param name="fieldName">Name of the field to be summed.</param>
        /// <returns></returns>
        private static double AverageOfRows(DataRow[] rows, string fieldName)
        {
            double total = 0;
            foreach (DataRow row in rows)
            {
                double field;
                if (double.TryParse(row.Field<object>(fieldName)?.ToString(), out field))
                    total += field;
                else
                {
                    DateTime date = DataTableUtilities.GetDateFromRow(row);
                    throw new Exception("Invalid data in column " + fieldName + " on date " + date.ToShortDateString() + " (day of year = " + date.DayOfYear + ")");
                }
            }
            return MathUtilities.Divide(total, rows.Length, 0);
        }

        /// <summary>
        /// Returns monthly totals for the given variable.
        /// </summary>
        /// <param name="table">The data table containing the data.</param>
        /// <param name="fieldName">The field name to look at.</param>
        /// <param name="firstDate">Only data after this date will be used.</param>
        /// <param name="lastDate">Only data before this date will be used.</param>
        /// <returns>Array of tuples. Each tuple contains a date (month) and the total of the field's values for that month.</returns>
        public static Tuple<DateTime, double>[] MonthlyTotals(DataTable table, string fieldName, DateTime firstDate, DateTime lastDate)
        {
            if (table.Rows.Count < 1)
                return null;
            var result = from row in table.AsEnumerable()
                         where (DataTableUtilities.GetDateFromRow(row) >= firstDate &&
                                DataTableUtilities.GetDateFromRow(row) <= lastDate)
                         group row by new
                         {
                             Year = DataTableUtilities.GetDateFromRow(row).Year,
                             Month = DataTableUtilities.GetDateFromRow(row).Month,
                         } into grp
                         select new
                         {
                             Year = grp.Key.Year,
                             Month = grp.Key.Month,
                             Total = SumOfRows(grp.AsEnumerable().ToArray(), fieldName)
                         };
            return result.Select(r => new Tuple<DateTime, double>(new DateTime(r.Year, r.Month, 1), r.Total)).ToArray();
        }

        /// <summary>
        /// Return longterm average monthly totals for the given variable.
        /// </summary>
        /// <remarks>
        ///
        /// Assumes a a date can be derived from the data table using the
        /// DataTable.GetDateFromRow function.
        /// </remarks>
        /// <param name="table">The data table containing the data</param>
        /// <param name="fieldName">The field name to look at</param>
        /// <param name="firstDate">Only data after this date will be used</param>
        /// <param name="lastDate">Only data before this date will be used</param>
        /// <returns>An array of 12 numbers or null if no data in table.</returns>
        public static double[] AverageMonthlyTotals(System.Data.DataTable table, string fieldName, DateTime firstDate, DateTime lastDate)
        {
            if (table.Rows.Count > 0)
            {
                // This first query gives monthly totals for each year.
                var result = from row in table.AsEnumerable()
                             where (DataTableUtilities.GetDateFromRow(row) >= firstDate &&
                                    DataTableUtilities.GetDateFromRow(row) <= lastDate)
                             group row by new
                             {
                                 Year = DataTableUtilities.GetDateFromRow(row).Year,
                                 Month = DataTableUtilities.GetDateFromRow(row).Month,
                             } into grp
                             select new
                             {
                                 Year = grp.Key.Year,
                                 Month = grp.Key.Month,
                                 Total = SumOfRows(grp.AsEnumerable().ToArray(), fieldName)
                             };

                // This second query gives average monthly totals using the first query.
                var result2 = from row in result
                              group row by new
                              {
                                  Month = row.Month,
                              } into grp
                              select new
                              {
                                  Month = grp.Key.Month,
                                  Avg = grp.Average(row => row.Total)
                              };


                List<double> totals = new List<double>();
                foreach (var row in result2)
                    totals.Add(row.Avg);

                return totals.ToArray();
            }

            return null;
        }


        /// <summary>
        /// Return longterm average monthly averages for the given variable.
        /// </summary>
        /// <remarks>
        ///
        /// Assumes a a date can be derived from the data table using the
        /// DataTable.GetDateFromRow function.
        /// </remarks>
        /// <param name="table">The data table containing the data</param>
        /// <param name="fieldName">The field name to look at</param>
        /// <param name="firstDate">Only data after this date will be used</param>
        /// <param name="lastDate">Only data before this date will be used</param>
        /// <returns>An array of 12 numbers or null if no data in table.</returns>
        public static double[] AverageMonthlyAverages(System.Data.DataTable table, string fieldName, DateTime firstDate, DateTime lastDate)
        {
            if (table.Rows.Count > 0)
            {
                // This first query gives monthly totals for each year.
                var result = from row in table.AsEnumerable()
                             where (DataTableUtilities.GetDateFromRow(row) >= firstDate &&
                                    DataTableUtilities.GetDateFromRow(row) <= lastDate)
                             group row by new
                             {
                                 Year = DataTableUtilities.GetDateFromRow(row).Year,
                                 Month = DataTableUtilities.GetDateFromRow(row).Month,
                             } into grp
                             select new
                             {
                                 Year = grp.Key.Year,
                                 Month = grp.Key.Month,
                                 Avg = AverageOfRows(grp.AsEnumerable().ToArray(), fieldName)
                             };

                // This second query gives average monthly totals using the first query.
                var result2 = from row in result
                              group row by new
                              {
                                  Month = row.Month,
                              } into grp
                              select new
                              {
                                  Month = grp.Key.Month,
                                  Avg = grp.Average(row => row.Avg)
                              };


                List<double> totals = new List<double>();
                foreach (var row in result2)
                    totals.Add(row.Avg);

                return totals.ToArray();
            }

            return null;
        }

        /// <summary>
        /// Return yearly totals for the given variable.
        /// </summary>
        /// <remarks>
        /// Assumes a a date can be derived from the data table using the
        /// DataTable.GetDateFromRow function.
        /// </remarks>
        /// <param name="table">The data table containing the data</param>
        /// <param name="fieldName">The field name to look at</param>
        /// <param name="firstDate">Only data after this date will be used</param>
        /// <param name="lastDate">Only data before this date will be used</param>
        /// <returns>An array of yearly totals or null if no data in table.</returns>
        public static double[] YearlyTotals(System.Data.DataTable table, string fieldName, DateTime firstDate, DateTime lastDate)
        {
            if (table.Rows.Count > 0)
            {
                var result = from row in table.AsEnumerable()
                             where (DataTableUtilities.GetDateFromRow(row) >= firstDate &&
                                    DataTableUtilities.GetDateFromRow(row) <= lastDate)
                             group row by new
                             {
                                 Year = DataTableUtilities.GetDateFromRow(row).Year,
                             } into grp
                             select new
                             {
                                 Year = grp.Key.Year,
                                 Total = SumOfRows(grp.AsEnumerable().ToArray(), fieldName)
                             };

                List<double> totals = new List<double>();
                foreach (var row in result)
                    totals.Add(row.Total);

                return totals.ToArray();
            }

            return null;
        }

        /// <summary>
        /// Return yearly averages for the given variable.
        /// </summary>
        /// <remarks>
        /// Assumes a a date can be derived from the data table using the
        /// DataTable.GetDateFromRow function.
        /// </remarks>
        /// <param name="table">The data table containing the data</param>
        /// <param name="fieldName">The field name to look at</param>
        /// <param name="firstDate">Only data after this date will be used</param>
        /// <param name="lastDate">Only data before this date will be used</param>
        /// <returns>An array of yearly totals or null if no data in table.</returns>
        public static double[] YearlyAverages(System.Data.DataTable table, string fieldName, DateTime firstDate, DateTime lastDate)
        {
            if (table.Rows.Count > 0)
            {
                var result = from row in table.AsEnumerable()
                             where (DataTableUtilities.GetDateFromRow(row) >= firstDate &&
                                    DataTableUtilities.GetDateFromRow(row) <= lastDate)
                             group row by new
                             {
                                 Year = DataTableUtilities.GetDateFromRow(row).Year,
                             } into grp
                             select new
                             {
                                 Year = grp.Key.Year,
                                 Total = AverageOfRows(grp.AsEnumerable().ToArray(), fieldName)
                             };

                List<double> totals = new List<double>();
                foreach (var row in result)
                    totals.Add(row.Total);

                return totals.ToArray();
            }

            return null;
        }

        /// <summary>
        /// Return average daily totals for each month for the the given variable.
        /// </summary>
        /// <remarks>
        /// Assumes a a date can be derived from the data table using the
        /// DataTable.GetDateFromRow function.
        /// </remarks>
        /// <param name="table">The data table containing the data</param>
        /// <param name="fieldName">The field name to look at</param>
        /// <param name="firstDate">Only data after this date will be used</param>
        /// <param name="lastDate">Only data before this date will be used</param>
        /// <returns>An array of monthly averages or null if no data in table.</returns>
        public static double[] AverageDailyTotalsForEachMonth(System.Data.DataTable table, string fieldName, DateTime firstDate, DateTime lastDate)
        {
            if (table.Rows.Count > 0)
            {
                var result = from row in table.AsEnumerable()
                             where (DataTableUtilities.GetDateFromRow(row) >= firstDate &&
                                    DataTableUtilities.GetDateFromRow(row) <= lastDate)
                             group row by new
                             {
                                 Month = DataTableUtilities.GetDateFromRow(row).Month,
                                 Year = DataTableUtilities.GetDateFromRow(row).Year,
                             } into grp
                             select new
                             {
                                 Year = grp.Key.Year,
                                 Total = MathUtilities.Divide(SumOfRows(grp.AsEnumerable().ToArray(), fieldName), grp.AsEnumerable().Count(), 0)
                             };
                List<double> totals = new List<double>();
                foreach (var row in result)
                    totals.Add(row.Total);

                return totals.ToArray();
            }

            return null;
        }

        /// <summary>
        /// Checks if table has a double as its very first value.
        /// </summary>
        /// <param name="table"></param>
        /// <returns>a <see cref="bool">bool</see></returns>
        public static bool IsTablesFirstValueDouble(DataTable table)
        {
            return Double.TryParse(table.Rows[0][0] as string, out double firstValue);
        }
    }
}