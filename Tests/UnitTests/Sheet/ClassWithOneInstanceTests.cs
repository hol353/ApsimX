using Models.Core;
using NUnit.Framework;
using Gtk.Sheet;
using System.Collections.Generic;

namespace UnitTests.Sheet;

[TestFixture]
class ClassWithOneListPropertyTests
{
    class ClassWithUnits
    {
        [Display]
        public List<Layer> Profile { get; set; } = new()
        {
            new() { Depth = "0-100" },
            new() { Depth = "100-200" }
        };

        public class Layer
        {
            [Display]
            [Units("mm")]
            public string Depth { get; set; }
        }
    }
    /// <summary>Ensure units are found.</summary>
    [Test]
    public void TestStaticUnits()
    {
        var dataProvider = DataProviderFactory.Create(new ClassWithUnits());

        Assert.That(dataProvider.ColumnCount, Is.EqualTo(1));
        Assert.That(dataProvider.RowCount, Is.EqualTo(2));
        Assert.That(dataProvider.GetColumnName(0), Is.EqualTo("Depth"));
        Assert.That(dataProvider.GetColumnUnits(0), Is.EqualTo("mm"));
        Assert.That(dataProvider.GetCellContents(0, 0), Is.EqualTo("0-100"));
        Assert.That(dataProvider.GetCellContents(0, 1), Is.EqualTo("100-200"));
        Assert.That(dataProvider.GetCellState(0, 0), Is.EqualTo(SheetCellState.Normal));
        Assert.That(dataProvider.GetCellState(0, 1), Is.EqualTo(SheetCellState.Normal));
    }

    class ClassWithDynamicUnits
    {
        [Display]
        public List<Layer> Profile { get; set; } = new()
        {
            new() { Depth = "0-100" },
            new() { Depth = "100-200" }
        };

        public class Layer
        {
            [Display]
            public string Depth { get; set; }

            public string DepthUnits => "mm";
        }
    }
    /// <summary>Ensure units are found.</summary>
    [Test]
    public void TestDynamicUnits()
    {
        var dataProvider = DataProviderFactory.Create(new ClassWithDynamicUnits());
        Assert.That(dataProvider.ColumnCount, Is.EqualTo(1));
        Assert.That(dataProvider.RowCount, Is.EqualTo(2));
        Assert.That(dataProvider.GetColumnName(0), Is.EqualTo("Depth"));
        Assert.That(dataProvider.GetColumnUnits(0), Is.EqualTo("mm"));
    }

    class ClassWithValidUnits
    {
        [Display]
        public List<Layer> Profile { get; set; } = new()
        {
            new() { Depth = "0-100" },
            new() { Depth = "100-200" }
        };

        public class Layer
        {
            public enum UnitsEnum
            {
                X,
                Y
            }

            [Display]
            public string Depth { get; set; }

            public UnitsEnum DepthUnits => UnitsEnum.X;
        }
    }
    /// <summary>Ensure valid units are found.</summary>
    [Test]
    public void TestValidUnits()
    {
        var dataProvider = DataProviderFactory.Create(new ClassWithValidUnits());
        Assert.That(dataProvider.GetColumnUnits(0), Is.EqualTo("X"));
        Assert.That(dataProvider.GetColumnValidUnits(0), Is.EqualTo(new string[] { "X", "Y" }));
    }

    class ClassWithFormat
    {
        [Display]
        public List<Layer> Profile { get; set; } = new()
        {
            new() { Value = 1 },
            new() { Value = 2 }
        };

        public class Layer
        {
            [Display(Format = "N2")]
            public double Value { get; set; }
        }
    }
    /// <summary>Ensure format is found.</summary>
    [Test]
    public void TestFormat()
    {
        var dataProvider = DataProviderFactory.Create(new ClassWithFormat());
        Assert.That(dataProvider.ColumnCount, Is.EqualTo(1));
        Assert.That(dataProvider.RowCount, Is.EqualTo(2));
        Assert.That(dataProvider.GetColumnName(0), Is.EqualTo("Value"));
        Assert.That(dataProvider.GetColumnUnits(0), Is.Null);
        Assert.That(dataProvider.GetCellContents(0, 0), Is.EqualTo("1.00"));
        Assert.That(dataProvider.GetCellContents(0, 1), Is.EqualTo("2.00"));
        Assert.That(dataProvider.GetCellState(0, 0), Is.EqualTo(SheetCellState.Normal));
        Assert.That(dataProvider.GetCellState(0, 1), Is.EqualTo(SheetCellState.Normal));
    }

    class ClassWithReadonly
    {
        [Display]
        public List<Layer> Profile { get; set; } = new()
        {
            new(1),
            new(2)
        };

        public class Layer
        {
            public Layer(double value) { Value = value; }
            [Display]
            public double Value { get; }
        }
    }

    /// <summary>Ensure readonly properties are found.</summary>
    [Test]
    public void TestReadonly()
    {
        var dataProvider = DataProviderFactory.Create(new ClassWithReadonly());
        Assert.That(dataProvider.GetCellState(0, 0), Is.EqualTo(SheetCellState.ReadOnly));
        Assert.That(dataProvider.GetCellState(0, 1), Is.EqualTo(SheetCellState.ReadOnly));
    }

    class ClassWithMetadata
    {
        [Display]
        public List<Layer> Profile { get; set; } = new()
        {
            new() { Value = 1, ValueMetadata = "Calculated" },
            new() { Value = 2 }
        };

        public class Layer
        {
            [Display(Format = "N2")]
            public double Value { get; set; }

            public string ValueMetadata { get; set; }
        }

    }

    /// <summary>Ensure metadata properties are found and used.</summary>
    [Test]
    public void TestMetadata()
    {
        var dataProvider = DataProviderFactory.Create(new ClassWithMetadata());
        Assert.That(dataProvider.GetCellState(0, 0), Is.EqualTo(SheetCellState.Calculated));
        Assert.That(dataProvider.GetCellState(0, 1), Is.EqualTo(SheetCellState.Normal));
    }

    class ClassWithAlias
    {
        [Display]
        public List<Layer> Profile { get; set; } = new()
        {
            new() { Value = 1 },
            new() { Value = 2 }
        };

        public class Layer
        {
            [Display(DisplayName = "Alias")]
            public double Value { get; set; }
        }
    }

    /// <summary>Ensure alias is found and used.</summary>
    [Test]
    public void TestDisplayName()
    {
        var dataProvider = DataProviderFactory.Create(new ClassWithAlias());
        Assert.That(dataProvider.GetColumnName(0), Is.EqualTo("Alias"));
    }

    class ClassWithNull
    {
        [Display]
        public List<Layer> Profile { get; set; } = null;

        public class Layer
        {
            [Display(DisplayName = "Alias")]
            public double Value { get; set; }
        }
    }

    /// <summary>Ensure a null property can be handled.</summary>
    [Test]
    public void TestNullProperty()
    {
        var dataProvider = DataProviderFactory.Create(new ClassWithNull());
        Assert.That(dataProvider.RowCount, Is.EqualTo(0));
        Assert.That(dataProvider.GetCellContents(0, 0), Is.Null);
    }

    /// <summary>Ensure cannot change the value of a readonly property.</summary>
    [Test]
    public void TestSetValuesDoesntChangeReadonlyProperties()
    {
        ClassWithReadonly model = new();
        var dataProvider = DataProviderFactory.Create(model);

        dataProvider.SetCellContents(colIndices: new int[] { 0 },
                                     rowIndices: new int[] { 0 },
                                     values: new string[] { "ZZZZ" });
        Assert.That(dataProvider.GetCellContents(0, 0), Is.EqualTo("1"));
    }

    /// <summary>Ensure can change the value of a property.</summary>
    [Test]
    public void TestSetValuesWorks()
    {
        ClassWithUnits model = new();
        var dataProvider = DataProviderFactory.Create(model);

        dataProvider.SetCellContents(colIndices: new int[] { 0 },
                                     rowIndices: new int[] { 0 },
                                     values: new string[] { "ZZZZ" });
        Assert.That(model.Profile[0].Depth, Is.EqualTo("ZZZZ"));
        Assert.That(model.Profile[1].Depth, Is.EqualTo("100-200"));
    }

    /// <summary>Ensure we can add values to property.</summary>
    [Test]
    public void TestSetValuesWillExpandArray()
    {
        ClassWithUnits model = new();
        var dataProvider = DataProviderFactory.Create(model);

        dataProvider.SetCellContents(colIndices: new int[] { 0 },
                                     rowIndices: new int[] { 2 },
                                     values: new string[] { "ZZZZ" });
        Assert.That(dataProvider.ColumnCount, Is.EqualTo(1));
        Assert.That(dataProvider.RowCount, Is.EqualTo(3));
        Assert.That(dataProvider.GetCellContents(0, 0), Is.EqualTo("0-100"));
        Assert.That(dataProvider.GetCellContents(0, 1), Is.EqualTo("100-200"));
        Assert.That(dataProvider.GetCellContents(0, 2), Is.EqualTo("ZZZZ"));

        Assert.That(model.Profile[0].Depth, Is.EqualTo("0-100"));
        Assert.That(model.Profile[1].Depth, Is.EqualTo("100-200"));
        Assert.That(model.Profile[2].Depth, Is.EqualTo("ZZZZ"));
    }

    /// <summary>Ensure 'calculated' metadata properties change when data is set.</summary>
    [Test]
    public void TestSetValueChangesMetadata()
    {
        ClassWithMetadata model = new();
        var dataProvider = DataProviderFactory.Create(model);
        dataProvider.SetCellContents(colIndices: new int[] { 0 },
                                     rowIndices: new int[] { 0, 1 },
                                     values: new string[] { "3.0", "4.0" });
        Assert.That(dataProvider.GetCellState(0, 0), Is.EqualTo(SheetCellState.Normal));
        Assert.That(dataProvider.GetCellState(0, 1), Is.EqualTo(SheetCellState.Normal));
        Assert.That(model.Profile[0].ValueMetadata, Is.Null);
        Assert.That(model.Profile[1].ValueMetadata, Is.Null);
    }

    /// <summary>Ensure the CellChanged event is invoked when data is changed.</summary>
    [Test]
    public void TestSetValueInvokesDataChangedNotification()
    {
        ClassWithUnits model = new();
        var dataProvider = DataProviderFactory.Create(model);
        bool invoked = false;
        dataProvider.CellChanged += (s, c, r, v) => invoked = true;

        dataProvider.SetCellContents(colIndices: new int[] { 0 },
                                     rowIndices: new int[] { 1 },
                                     values: new string[] { "ZZZZ" });

        Assert.That(invoked, Is.True);
    }

    /// <summary>
    /// Ensure data provider can handle setting a value to a blank. This is what the sheet control does
    /// when the user hits 'Delete' on a cell.
    /// </summary>
    [Test]
    public void TestCanSetValueToBlank()
    {
        ClassWithFormat model = new();
        var dataProvider = DataProviderFactory.Create(model);
        dataProvider.SetCellContents(colIndices: new int[] { 0 },
                                     rowIndices: new int[] { 1 },
                                     values: new string[] { "" });

        Assert.That(model.Profile[1].Value, Is.NaN);
    }

    class ClassWithMultipleProperties
    {
        [Display]
        public List<Layer> Profile { get; set; } = new()
        {
            new() { A = "a1", B = "b1", C = "c1", D = "d1" },
            new() { A = "a2", B = "b2", C = "c2", D = "d2" },
            new() { A = "a3", B = "b3", C = "c3", D = "d3" },
            new() { A = "a4", B = "b4", C = "c4", D = "d4" },
        };

        public class Layer
        {
            [Display]
            public string A { get; set; }

            [Display]
            public string B { get; set; }

            [Display]
            public string C { get; set; }

            [Display]
            public string D { get; set; }
        }
    }

    /// <summary>Ensure can delete entire row of grid.</summary>
    [Test]
    public void DeleteEntireRow()
    {
        var model = new ClassWithMultipleProperties();
        var dataProvider = DataProviderFactory.Create(model);

        dataProvider.DeleteRows(new int[] { 1, 2 });

        Assert.That(dataProvider.ColumnCount, Is.EqualTo(4));
        Assert.That(dataProvider.RowCount, Is.EqualTo(2));
        Assert.That(dataProvider.GetCellContents(0, 0), Is.EqualTo("a1"));
        Assert.That(dataProvider.GetCellContents(1, 0), Is.EqualTo("b1"));
        Assert.That(dataProvider.GetCellContents(2, 0), Is.EqualTo("c1"));
        Assert.That(dataProvider.GetCellContents(3, 0), Is.EqualTo("d1"));
        Assert.That(dataProvider.GetCellContents(0, 1), Is.EqualTo("a4"));
        Assert.That(dataProvider.GetCellContents(1, 1), Is.EqualTo("b4"));
        Assert.That(dataProvider.GetCellContents(2, 1), Is.EqualTo("c4"));
        Assert.That(dataProvider.GetCellContents(3, 1), Is.EqualTo("d4"));

        // Ensure model is also updated.
        Assert.That(model.Profile.Count, Is.EqualTo(2));
        Assert.That(model.Profile[0].A, Is.EqualTo("a1"));
        Assert.That(model.Profile[1].A, Is.EqualTo("a4"));
    }

}
