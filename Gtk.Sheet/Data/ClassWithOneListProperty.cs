using System.Collections;
using System.Reflection;
using Models.Core;

namespace Gtk.Sheet;

/// <summary>
/// A SheetDataProvider that wraps a list of instances.
/// </summary>
class ClassWithOneListProperty : IDataProvider
{
    private readonly List<List<PropertyMetadata>> properties = new();
    private readonly PropertyInfo property;
    private readonly object obj;
    private readonly int[] firstElement = new int[] { 0 };

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="obj">The object instance.</param>
    public ClassWithOneListProperty(PropertyInfo property, object obj)
    {
        this.property = property;
        this.obj = obj;
        var list =  property.GetValue(obj, null) as IList;
        if (list != null)
        {
            foreach (var itemInstance in list)
                DataProviderFactory.Create(itemInstance, (properties) =>
                {
                        this.properties.Add(properties);
                });
        }
    }

    /// <summary>An event invoked when a cell changes.</summary>
    public event IDataProvider.CellChangedDelegate CellChanged;

    /// <summary>Gets the number of columns of data.</summary>
    public int ColumnCount => properties.Any() ? properties.First().Count : 0;

    /// <summary>Gets the number of rows of data.</summary>
    public int RowCount => properties.Count;

    /// <summary>The model.</summary>
    public IModel Model => throw new NotImplementedException();

    /// <summary>Get the name of a column.</summary>
    /// <param name="columnIndex">Column index.</param>
    public string GetColumnName(int columnIndex)
    {
        if (columnIndex < ColumnCount)
            return properties.First()[columnIndex].Alias;
        throw new Exception($"Invalid column index: {columnIndex}");
    }

    /// <summary>Get the units of a column.</summary>
    /// <param name="columnIndex">Column index.</param>
    public string GetColumnUnits(int columnIndex)
    {
        if (columnIndex < ColumnCount)
            return properties.First()[columnIndex].Units;
        throw new Exception($"Invalid column index: {columnIndex}");
    }

    /// <summary>Get the allowable units of a column.</summary>
    /// <param name="columnIndex">Column index.</param>
    public IReadOnlyList<string> GetColumnValidUnits(int columnIndex)
    {
        if (columnIndex < ColumnCount)
            return properties.First()[columnIndex].ValidUnits;
        throw new Exception($"Invalid column index: {columnIndex}");
    }

    /// <summary>Get the cell state.</summary>
    /// <param name="columnIndex">Column index of cell.</param>
    /// <param name="rowIndex">Row index of cell.</param>
    public SheetCellState GetCellState(int columnIndex, int rowIndex)
    {
        if (columnIndex >= ColumnCount || rowIndex >= RowCount)
            return SheetCellState.Normal;
        if (properties[rowIndex][columnIndex].Metadata != null &&
            properties[rowIndex][columnIndex].Metadata.Count > 0 &&
            properties[rowIndex][columnIndex].Metadata.All(m => m == SheetCellState.ReadOnly))
            return SheetCellState.ReadOnly;
        if (properties[rowIndex][columnIndex].Metadata == null ||
            rowIndex >= properties[rowIndex][columnIndex].Metadata.Count)
            return SheetCellState.Normal;
        else
            return properties[rowIndex][columnIndex].Metadata.First();
    }

    /// <summary>Set the cell state.</summary>
    /// <param name="colIndex">Column index of cell.</param>
    /// <param name="rowIndex">Row index of cell.</param>
    /// <param name="state">The cell state</param>
    public void SetCellState(int colIndex, int rowIndex)
    {
        throw new System.NotImplementedException("Cannot change the state of a cell in a properties grid.");
    }

    /// <summary>Get the contents of a cell.</summary>
    /// <param name="columnIndex">Column index of cell.</param>
    /// <param name="rowIndex">Row index of cell.</param>
    public string GetCellContents(int columnIndex, int rowIndex)
    {
        if (columnIndex >= ColumnCount ||
            rowIndex >= RowCount ||
            properties[rowIndex][columnIndex].Values == null)
            return null;
        else
            return properties[rowIndex][columnIndex].Values.First();
    }

    /// <summary>Set the contents of a cell.</summary>
    /// <param name="colIndices">Column index of cell.</param>
    /// <param name="rowIndices">Row index of cell.</param>
    /// <param name="values">The value.</param>
    public void SetCellContents(int[] colIndices, int[] rowIndices, string[] values)
    {
        // Ensure properties list has the correct number of rows.
        int numValues = rowIndices.Max();
        while (properties.Count <= numValues)
        {
            var list =  property.GetValue(obj, null) as IList;
            list.Add(Activator.CreateInstance(property.PropertyType.GetGenericArguments().First()));
            DataProviderFactory.Create(list[list.Count-1], (properties) =>
            {
                    this.properties.Add(properties);
            });
        }

        // Set property values.
        foreach (var item in colIndices.Zip(rowIndices, values))
            properties[item.Second][item.First].SetValues(firstElement, new string[] { item.Third });

        CellChanged?.Invoke(this, colIndices, rowIndices, values);
    }

    /// <summary>Delete the specified rows.</summary>
    /// <param name="rowIndices">Row indexes of cell.</param>
    public void DeleteRows(int[] rowIndices)
    {
        if (rowIndices.Length > 0)
        {
            Array.Sort(rowIndices);
            foreach (var rowIndex in rowIndices.Reverse())
            {
                if (rowIndex < RowCount)
                {
                    var list =  property.GetValue(obj, null) as IList;
                    list.RemoveAt(rowIndex);
                    property.SetValue(obj, list);
                    properties.RemoveAt(rowIndex);
                }
            }

            CellChanged?.Invoke(this, null, rowIndices, null);
        }
    }
}