using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using ApsimNG.Classes;
using Gtk;
using TreeModel = Gtk.ITreeModel;


namespace UserInterface.Views
{

    /// <summary>A list view.</summary>
    public class ListView : ViewBase, IListView
    {
        /// <summary>The main tree view control.</summary>
        private Gtk.TreeView tree;

        private Button submitButton;

        /// <summary>The data table used to populate the tree view.</summary>
        private DataTable table;

        /// <summary>The columns of the tree view.</summary>
        private List<TreeViewColumn> columns = new List<TreeViewColumn>();

        /// <summary>The cells of the tree view.</summary>
        private List<CellRendererText> cells = new List<CellRendererText>();

        private List<Type> columnTypes = new List<Type>();

        /// <summary>The list store behind the gtk listview.</summary>
        ListStore store = null;

        /// <summary>The popup context menu.</summary>
        private Gtk.Menu contextMenu;

        /// <summary>The sort model given to the tree control.</summary>
        private TreeModelSort sort;

        /// <summary>The sort column name.</summary>
        private string sortColumn;

        /// <summary>The sort type.</summary>
        private bool sortAscending;

        /// <summary>
        /// A reference to an editor view for drag and drop functionality.
        /// </summary>
        private EditorView editor;

        /// <summary>Event handler for drag start event.</summary>
        public event EventHandler DragStart;

        /// <summary>Event handler for double-click event on TreeView. </summary>
        public event EventHandler DoubleClicked;

        public event EventHandler<ListViewArgs> TreeHover;

        /// <summary>Constructor</summary>
        public ListView()
        {

        }

        /// <summary>Constructor</summary>
        public ListView(ViewBase owner, Gtk.TreeView treeView, Gtk.Menu menu = null, EditorView editor = null, Button submitButton = null) : base(owner)
        {
            tree = treeView;
            tree.HasTooltip = true;
            tree.QueryTooltip += OnTreeHover;
            mainWidget = tree;
            tree.ButtonReleaseEvent += OnTreeClicked;
            tree.ButtonPressEvent += OnTreeButtonDown;
            tree.RowActivated += OnTreeDoubleClicked;
            this.editor = editor;
            TargetEntry[] target_table = new TargetEntry[] {
               new TargetEntry("application/x-model-component", TargetFlags.App, 0)
            };
            Gdk.DragAction actions = Gdk.DragAction.Copy | Gdk.DragAction.Link | Gdk.DragAction.Move;
            Drag.SourceSet(tree, Gdk.ModifierType.Button1Mask, target_table, actions);
            // makes the destination for a drop and drop action.
            if (editor != null)
                Drag.DestSet(editor.GetSourceView(), 0, target_table, actions);
            tree.DragBegin += OnTreeDragBegin;
            mainWidget.Destroyed += OnMainWidgetDestroyed;
            contextMenu = menu;
            tree.Selection.Mode = SelectionMode.Multiple;
            tree.RubberBanding = true;
            tree.CanFocus = true;
            if (submitButton != null)
            {
                this.submitButton = submitButton;
                submitButton.Pressed += SubmitButtonPressed;
            }

        }

        private void SubmitButtonPressed(object sender, EventArgs e)
        {
            try
            {
                string target = "https://github.com/APSIMInitiative/ApsimX/issues/new?assignees=&labels=New+common+report+event%2Fvariable&projects=&template=new-common-report-event-variable.yml";
                Process.Start(new ProcessStartInfo
                {
                    FileName = target,
                    CreateNoWindow = true,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occured trying to open link to create new common report event/variable. {ex}");
            }
        }

        private void OnTreeHover(object o, QueryTooltipArgs args)
        {
            try
            {
                if (tree.Model != null)
                {
                    ListViewArgs newArgs = new ListViewArgs { QueryTooltipArgs = args };
                    tree.ConvertWidgetToTreeCoords(args.X, args.Y, out int tx, out int ty);
                    newArgs.NewX = tx; // TODO: need to store new coordinates for use in ShowTooltip method.
                    newArgs.NewY = ty;

                
                    var pathpos = tree.GetPathAtPos(tx, ty, out TreePath path);
                    if (path == null)
                    {
                        // This will happen when the user moves the mouse over
                        // a region of the TreeView object which doesn't have
                        // any actual widgets (ie below the area with cells).
                        // Calling gtk_tree_model_get_iter() with a NULL path
                        // is not a valid (or useful) operation, and will, at
                        // best, result in a warning message being printed to
                        // stdout. The best thing we can do in this case is
                        // nothing.
                        return;
                    }

                    TreeModel model = tree.Model;
                    var modelIterator = model.GetIter(out TreeIter iter, path);
                    if (modelIterator)
                    {
                        if (path.Indices[0] < 0)
                            newArgs.ListViewRowIndex = 0;
                        else newArgs.ListViewRowIndex = path.Indices[0];
                    }


                    if (TreeHover != null)
                        TreeHover.Invoke(o, newArgs);
                }
            }
            catch (Exception error)
            {
                ShowError(error);
            }
        }

        private void OnTreeDoubleClicked(object o, RowActivatedArgs args)
        {
            try
            {
                DoubleClicked?.Invoke(o, args);
            }
            catch (Exception error)
            {
                ShowError(error);
            }
        }

        private void OnTreeDragBegin(object sender, DragBeginArgs args)
        {
            if (table.Columns.Contains("Description"))
            {
                if (SelectedIndicies.Length > 0)
                {
                    ReportDragObject dragObject = new()
                    {
                        Index = SelectedIndicies[0],
                        Description = table.Rows[SelectedIndicies[0]]["Description"].ToString(),
                        Code = table.Rows[SelectedIndicies[0]]["Code"].ToString(),
                        OtherArgs = args
                    };

                    DragStart.Invoke(sender, dragObject);
                }
            }

        }

        /// <summary>Invoked when the user changes the selection</summary>
        public event EventHandler Changed;

        /// <summary>Get or sets the datasource for the view.</summary>
        public DataTable DataSource
        {
            get
            {
                return table;
            }
            set
            {
                table = value;
                PopulateTreeView();
            }
        }

        /// <summary>Gets or sets the selected rows in the data source.</summary>
        public int[] SelectedIndicies
        {
            get
            {
                var indexes = new List<int>();
                foreach (var row in tree.Selection.GetSelectedRows())
                    if (store.GetIter(out TreeIter treeiter, row))
                        indexes.Add(row.Indices[0]);
                return indexes.ToArray();
            }
            set
            {
                foreach (var rowIndex in value)
                    tree.Selection.SelectPath(new TreePath(new int[] { rowIndex }));
            }
        }

        /// <summary>Sets the render details for particular cells.</summary>
        public List<CellRendererDescription> CellRenderDetails { get; set; } = new List<CellRendererDescription>();

        /// <summary>The column to sort on.</summary>
        public string SortColumn
        {
            get
            {
                return sortColumn;
            }
            set
            {
                sortColumn = value;
                SetTreeSortModel();
            }
        }

        /// <summary>The column to sort on.</summary>
        public bool SortAscending
        {
            get
            {
                return sortAscending;
            }
            set
            {
                sortAscending = value;
                SetTreeSortModel();
            }
        }

        /// <summary>Add a column to the list view.</summary>
        /// <param name="columnName">The column heading.</param>
        /// <param name="colType">The System.Type if other than string</param>
        public void AddColumn(string columnName, System.Type colType = null)
        {
            if (columnName == null)
                columnTypes.Add(typeof(string));    // default is string
            else
                columnTypes.Add(colType);
            var cell = new CellRendererText();
            cells.Add(cell);
            var colIndex = -1;
            if (SortColumn != null)
                colIndex = columns.Count;
            var newColumn = new TreeViewColumn
            {
                Title = columnName,
                Resizable = true,
                SortColumnId = colIndex,
                Sizing = TreeViewColumnSizing.GrowOnly,
                Visible = true
            };
            if (columnName == "Code")
                newColumn.Visible = false;
            newColumn.PackStart(cell, false);
            newColumn.AddAttribute(cell, "text", columns.Count);
            newColumn.AddNotification("width", OnColumnWidthChange);
            newColumn.SetCellDataFunc(cell, OnFormatColumn);
            columns.Add(newColumn);
            tree.AppendColumn(newColumn);
        }

        /// <summary>Clear all columns and data.</summary>
        public void Clear()
        {
            // Clear all columns from tree.
            while (tree.Columns.Length > 0)
                tree.RemoveColumn(tree.Columns[0]);
            columns.Clear();
            columnTypes.Clear();
            cells.Clear();
            store = null;
        }

        /// <summary>Clear all data.</summary>
        public void ClearRows()
        {
            store?.Clear();
        }

        /// <summary>Add a new row to list view.</summary>
        /// <param name="itemArray">Items to put into row.</param>
        public void AddRow(object[] itemArray)
        {
            if (store == null)
            {
                store = new ListStore(columnTypes.ToArray());
                tree.Model = store;
            }
            store.AppendValues(itemArray);
        }

        /// <summary>Get a row from the list view.</summary>
        /// <param name="index">Index of row to return.</param>
        /// <returns>The items making up the row.</returns>
        public object[] GetRow(int index)
        {
            if (store != null)
            {
                var path = new TreePath(new int[] { index });
                if (store.GetIter(out TreeIter treeiter, path))
                {
                    var values = new object[columns.Count];
                    for (int colIndex = 0; colIndex < columns.Count; colIndex++)
                        values[colIndex] = store.GetValue(treeiter, colIndex);
                    return values;
                }
            }
            return null;
        }

        /// <summary>Add a new row to list view.</summary>
        /// <param name="index">The index of the row to remove.</param>
        public void RemoveRow(int index)
        {
            if (store != null)
            {
                var path = new TreePath(new int[] { index });
                if (store.GetIter(out TreeIter treeiter, path))
                    store.Remove(ref treeiter);
            }
        }

        /// <summary> Sets text of tooltip with corresponding code string. </summary>
        /// <param name="reportVariableCode">a string that is ReportVariable.Code</param>
        /// <param name="args"></param>
        public void ShowTooltip(string reportVariableCode, ListViewArgs args)
        {
            //tree.ConvertWidgetToTreeCoords(args.NewX, NewY, out int tx, out int ty);

            if (tree.GetPathAtPos(args.NewX, args.NewY, out TreePath path))
            {
                TreeModel model = tree.Model;
                if (model.GetIter(out TreeIter iter, path))
                {
                    args.QueryTooltipArgs.Tooltip.Text = reportVariableCode;
                    // Tells the TreeView to show tooltip.
                    args.QueryTooltipArgs.RetVal = true;
                }
            }
        }

        /// <summary>The main widget has been destroyed.</summary>
        private void OnMainWidgetDestroyed(object sender, EventArgs e)
        {
            try
            {
                tree.ButtonPressEvent -= OnTreeButtonDown;
                tree.ButtonReleaseEvent -= OnTreeClicked;
                mainWidget.Destroyed -= OnMainWidgetDestroyed;
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>Populate the tree view.</summary>
        private void PopulateTreeView()
        {
            Clear();

            // initialise column headers            
            for (int i = 0; i < table.Columns.Count; i++)
            {
                DataColumn col = table.Columns[i];
                AddColumn(col.ColumnName, col.DataType);
            }

            // Populate with rows.
            foreach (DataRow row in table.Rows)
                AddRow(row.ItemArray);

        }

        /// <summary>
        /// Invoked for every cell in grid.
        /// </summary>
        /// <param name="col">The column.</param>
        /// <param name="baseCell">The cell.</param>
        /// <param name="model">The tree model.</param>
        /// <param name="iter">The tree iterator.</param>
        private void OnFormatColumn(TreeViewColumn col, CellRenderer baseCell, TreeModel model, TreeIter iter)
        {
            try
            {
                if (CellRenderDetails != null)
                {
                    TreePath path = tree.Model.GetPath(iter);
                    if (path != null)
                    {
                        int rowIndex = path.Indices[0];
                        int colIndex = columns.FindIndex(c => c.Title == col.Title);

                        CellRendererDescription renderDetails = CellRenderDetails.Find(c => c.ColumnIndex == colIndex && c.RowIndex == rowIndex);
                        if (renderDetails != null)
                        {
                            var cell = baseCell as CellRendererText;
                            cell.Strikethrough = renderDetails.StrikeThrough;
                            if (renderDetails.Bold)
                                cell.Weight = (int)Pango.Weight.Bold;
                            if (renderDetails.Italics)
                                cell.Font = "Sans Italic";
                            cell.Foreground = renderDetails.Colour.Name; // ?
                        }
                        else if (baseCell is CellRendererText)
                        {
                            var cell = baseCell as CellRendererText;
                            cell.Strikethrough = false;
                            cell.Weight = (int)Pango.Weight.Normal;
                            cell.Font = "Normal";
                        }
                    }
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>The grid has readjusted the column widths.</summary>
        private void OnColumnWidthChange(object sender, EventArgs e)
        {
            //try
            //{
            //    TreeViewColumn col = sender as TreeViewColumn;
            //    int index = columns.IndexOf(col);
            //    Application.Invoke(delegate
            //    {
            //    // if something is going wrong with column width, this is probably causing it                
            //    cells[index].Width = col.Width - 4;
            //        cells[index].Ellipsize = EllipsizeMode.End;
            //    });
            //}
            //catch (Exception err)
            //{
            //    ShowError(err);
            //}
        }

        /// <summary>
        /// Called when the user pushes the mouse button down.
        /// If it's a right click event, we will prevent the
        /// signal from propagating any further. If we don't do this,
        /// the selection (if multiple rows are selected) will be
        /// cleared before the button release event is fired. The
        /// result will be right clicking on a selection and having
        /// the selection disappear, which is not what would be expected.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event data.</param>
        [GLib.ConnectBefore]
        private void OnTreeButtonDown(object sender, ButtonPressEventArgs args)
        {
            try
            {
                tree.GetPathAtPos((int)args.Event.X, (int)args.Event.Y, out TreePath path);
                if (args.Event.Button == 3 && tree.Selection.GetSelectedRows().Contains(path))
                    args.RetVal = true;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Event handler for clicking on the TreeView. 
        /// Shows the context menu (if and only if the click is a right click).
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event arguments</param>
        [GLib.ConnectBefore]
        private void OnTreeClicked(object sender, ButtonReleaseEventArgs e)
        {
            try
            {
                if (e.Event.Button == 1) // left click
                    Changed?.Invoke(sender, new EventArgs());

                else if (e.Event.Button == 3) // right click
                {
                    TreePath path;
                    tree.GetPathAtPos((int)e.Event.X, (int)e.Event.Y, out path);

                    // By default, Gtk will un-select the selected rows when a normal (non-shift/ctrl) click is registered.
                    // Setting e.Retval to true will stop the default Gtk ButtonPress event handler from being called after 
                    // we return from this handler, which in turn means that the rows will not be deselected.
                    e.RetVal = tree.Selection.GetSelectedRows().Contains(path);
                    if (contextMenu != null)
                    {
                        contextMenu.ShowAll();
                        contextMenu.Popup();
                    }
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// A method used when a view is wrapping a gtk control.
        /// </summary>
        /// <param name="ownerView">The owning view.</param>
        /// <param name="gtkControl">The gtk control being wrapped.</param>
        protected override void Initialise(ViewBase ownerView, GLib.Object gtkControl)
        {
            owner = ownerView;
            tree = (Gtk.TreeView)gtkControl;
            tree.ButtonReleaseEvent += OnTreeClicked;
            mainWidget = tree;
            mainWidget.Destroyed += OnMainWidgetDestroyed;
            tree.Selection.Mode = SelectionMode.Multiple;
            tree.RubberBanding = true;
            tree.CanFocus = true;
        }

        /// <summary>the sort column.</summary>
        private void SetTreeSortModel()
        {
            if (sortColumn != null)
            {
                var sortColumnIndex = columns.FindIndex(c => c.Title == sortColumn);
                if (sortColumnIndex != -1)
                {
                    var sortMultpilier = 1;
                    if (!SortAscending)
                        sortMultpilier = -1;
                    sort = new TreeModelSort(new TreeModelFilter(store, null))
                    {
                        // By default, sort by start time descending.
                        DefaultSortFunc = (model, a, b) => sortMultpilier * SortData(model, a, b, sortColumnIndex)
                    };
                    for (int i = 0; i < columns.Count; i++)
                        sort.SetSortFunc(i, (model, a, b) => SortData(model, a, b, i));
                    tree.Model = sort;
                }
            }
        }

        /// <summary>
        /// Compares 2 elements from the ListStore and returns an indication of their relative values. 
        /// </summary>
        /// <param name="model">Model of the ListStore.</param>
        /// <param name="a">Path to the first row.</param>
        /// <param name="b">Path to the second row.</param>
        /// <param name="i">Column to take values from.</param>
        /// <returns></returns>
        private int SortData(TreeModel model, TreeIter a, TreeIter b, int i)
        {
            object o1 = model.GetValue(a, i);
            object o2 = model.GetValue(b, i);
            if (o1 != null && o2 != null)
                return string.Compare(o1.ToString(), o2.ToString());
            return 0;
        }


    }

    /// <summary>An interface for a list view</summary>
    public interface IListView
    {
        /// <summary>Invoked when the user changes the selection</summary>
        event EventHandler Changed;

        /// <summary> Invoked when a row in the ListView is dragged. </summary>
        event EventHandler DragStart;

        /// <summary> Invoked when a row in the ListView is double-clicked. </summary>
        event EventHandler DoubleClicked;

        event EventHandler<ListViewArgs> TreeHover;

        /// <summary>Get or sets the datasource for the view.</summary>
        DataTable DataSource { get; set; }

        /// <summary>Gets or sets the selected row in the data source.</summary>
        int[] SelectedIndicies { get; set; }

        /// <summary>Sets the render details for particular cells.</summary>
        List<CellRendererDescription> CellRenderDetails { get; set; }

        /// <summary>Add a column to the list view.</summary>
        /// <param name="columnName">The column heading.</param>
        /// <param name="colType">The System.Type if other than string</param>
        void AddColumn(string columnName, System.Type colType = null);

        /// <summary>Clear all columns and data.</summary>
        void Clear();

        /// <summary>Clear all data.</summary>
        void ClearRows();

        /// <summary>Add a new row to list view.</summary>
        /// <param name="itemArray">A list of items to add.</param>
        void AddRow(object[] itemArray);

        /// <summary>Add a new row to list view.</summary>
        /// <param name="index">The index of the row to remove.</param>
        void RemoveRow(int index);
    }
}