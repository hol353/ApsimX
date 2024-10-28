using System;
using System.Data;
using System.Collections.Generic;

namespace APSIM.Shared.Documentation
{
    /// <summary>Describes an auto-doc table command.</summary>
    public class Table : ITag
    {
        /// <summary>The data to show in the table.</summary>
        public DataView data { get; private set; }

        /// <summary>The data to show in the table.</summary>
        public DataTable Data { get {return data.ToTable();} }

        /// <summary>The indent level.</summary>
        public int indent;

        /// <summary>Max width of each column (in terms of number of characters).</summary>
        public int ColumnWidth { get; private set; }

        /// <summary>Max width of each column (in terms of number of characters).</summary>
        public string Style { get; private set; } = "Table";

        /// <summary>
        /// Initializes a new instance of the <see cref="Table"/> class.
        /// </summary>
        /// <param name="data">The column / row data.</param>
        /// <param name="indent">The indentation.</param>
        /// <param name="width">Max width of each column (in terms of number of characters).</param>
        /// <param name="style">The style to use for the table.</param>
        public Table(DataTable data, int indent = 0, int width = 50, string style = "Table")
        {
            this.data = new DataView(data);
            this.indent = indent;
            this.ColumnWidth = width;
            Style = style;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Table"/> class.
        /// </summary>
        /// <param name="data">The column / row data.</param>
        /// <param name="indent">The indentation.</param>
        /// <param name="width">Max width of each column (in terms of number of characters).</param>
        /// <param name="style">The style to use for the table.</param>
        public Table(DataView data, int indent = 0, int width = 50, string style = "Table")
        {
            this.data = data;
            this.indent = indent;
            this.ColumnWidth = width;
            Style = style;
        }

        /// <summary>Adds an ITag as a child of this ITag</summary>
        public void Add(ITag tag) {
            throw new Exception("Table cannot have child tags");
        }

        /// <summary>Adds a list of ITags as a children of this ITag</summary>
        public void Add(List<ITag> tags) {
            throw new Exception("Table cannot have child tags");
        }
    }
}
