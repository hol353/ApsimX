using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Gtk.Sheet;
using Models.Core;
using Models.Interfaces;
using Models.Soils;
using Models.Utilities;
using UserInterface.Commands;
using UserInterface.EventArguments;
using UserInterface.Interfaces;
using UserInterface.Views;

namespace UserInterface.Presenters
{
    /// <summary>
    /// A generic grid presenter for displaying a data table and allowing editing.
    /// This is built to run within other presenters, so pass in a ContainerView in the attach method to have it drawn.
    /// Table Presenter can be used for a two grid view, or for additional functionality like intellisense
    /// </summary>
    class GridPresenter : IPresenter
    {
        /// <summary>Stores a reference to the model.</summary>
        private IModel model;

        /// <summary>The data provider.</summary>
        private IDataProvider dataProvider;

        /// <summary>The sheet widget.</summary>
        private SheetWidget grid;

        // /// <summary>Currently Selected Row. Used to detect when a cell is slected</summary>
        // private int selectedRow = -1;

        // /// <summary>Currently Selected Column. Used to detect when a cell is slected</summary>
        // private int selectedColumn = -1;

        /// <summary>The container that houses the sheet.</summary>
        private ContainerView sheetContainer;

        /// <summary>The popup context menu helper.</summary>
        private ContextMenuHelper contextMenuHelper;

        /// <summary>The popup context menu.</summary>
        private MenuView contextMenu;

        /// <summary>Parent explorer presenter.</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>Parent explorer presenter.</summary>
        private List<string> contextMenuOptions;

        /// <summary>
        /// The intellisense.
        /// </summary>
        private IntellisensePresenter intellisense;

        /// <summary>A replace model command to enable the undo system to work.</summary>
        private ReplaceModelCommand replaceModelCommand;

        /// <summary>Delegate for a CellChanged event.</summary>
        /// <param name="dataProvider">The data provider.</param>
        /// <param name="colIndices">The indices of the columns that were changed.</param>
        /// <param name="rowIndices">The indices of the rows that were changed.</param>
        /// <param name="values">The values of the cells changed.</param>
        public delegate void CellChangedDelegate(IDataProvider dataProvider, int[] colIndices, int[] rowIndices, string[] values);

        /// <summary>An event invoked when a cell changes.</summary>
        public event CellChangedDelegate CellChanged;

        /// <summary>Delegate for a SelectedCellChanged event.</summary>
        /// <param name="colIndex">The index of the column that was changed.</param>
        /// <param name="rowIndex">The index of the row that was changed.</param>
        public delegate void SelectedCellChangedDelegate(int colIndex, int rowIndex);

        ///// <summary>An event invoked when a cell changes.</summary>
        //public event SelectedCellChangedDelegate SelectedCellChanged;

        /// <summary>
        /// Attach the model to the view.
        /// </summary>
        /// <param name="model">The model to connect to.</param>
        /// <param name="v">The view to connect to.</param>
        /// <param name="parentPresenter">The parent explorer presenter.</param>
        public void Attach(object model, object v, ExplorerPresenter parentPresenter)
        {
            this.model = model as IModel;
            explorerPresenter = parentPresenter;

            if (model as IDataProvider != null)
            {
                // e.g. DataStorePresenter goes through here.
                dataProvider = model as IDataProvider;
            }
            else
            {
                dataProvider = ModelToSheetDataProvider.ToSheetDataProvider(model);
                var viewBase = v as ViewBase;
                sheetContainer = new ContainerView(viewBase, viewBase.MainWidget as Gtk.Container);
                replaceModelCommand = new ReplaceModelCommand(this.model.Clone() as IModel, null);
                explorerPresenter.CommandHistory.Add(replaceModelCommand, execute: false);
            }

            //we are receiving a container from another presenter to put the grid into
            if (v as ContainerView != null)
            {
                sheetContainer = v as ContainerView;
            }
            //we are receiving a GridView and should setup and load into that directly
            else if (v as IGridView != null)
            {
                IGridView view = v as IGridView;
                sheetContainer = view.Grid1;
                view.ShowGrid(1, true, parentPresenter.GetView() as ExplorerView);
                view.SetDescriptionText("");
                view.SetLabelHeight(0);
                AddContextMenuOptions(new string[] { "Cut", "Copy", "Paste", "Delete", "Select All" });
            }

            //Create the sheet widget here.
            SetupSheet(dataProvider);

            explorerPresenter.CommandHistory.ModelChanged += OnModelChanged;
            if (dataProvider != null)
                dataProvider.CellChanged += OnCellChanged;

            //this is created with AddIntellisense by another presenter if intellisense is required
            intellisense = null;

            Refresh();
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            explorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;
            contextMenuHelper.ContextMenu -= OnContextMenuPopup;

            if (dataProvider != null)
                dataProvider.CellChanged -= OnCellChanged;

            SaveGridToModel();

            grid.Cleanup();

            if (intellisense != null)
            {
                intellisense.Cleanup();
                //intellisense.ItemSelected -= OnIntellisenseItemSelected;
                intellisense.ContextItemsNeeded -= OnIntellisenseNeedContextItems;
            }

        }

        public void SetupSheet(IDataProvider dataProvider)
        {
            // Determine if sheet is editable
            bool gridIsEditable;
            if (dataProvider == null)
                gridIsEditable = false;
            else
                gridIsEditable = dataProvider.RowCount == 0;
            if (dataProvider != null)
            {
                for (int rowIndex = 0; rowIndex < dataProvider.RowCount; rowIndex++)
                    for (int columnIndex = 0; columnIndex < dataProvider.ColumnCount; columnIndex++)
                        if (dataProvider.GetCellState(columnIndex, rowIndex) != SheetCellState.ReadOnly)
                        {
                            gridIsEditable = true;
                            break;
                        }
            }
            else
                gridIsEditable = true;

            grid = new SheetWidget(sheetContainer.Widget,
                                   dataProvider,
                                   multiSelect: true,
                                   onException: (err) => ViewBase.MasterView.ShowError(err),
                                   gridIsEditable: gridIsEditable,
                                   blankRowAtBottom: gridIsEditable);

            contextMenu = new MenuView();
            contextMenuHelper = new ContextMenuHelper(grid);
            contextMenuHelper.ContextMenu += OnContextMenuPopup;
            if (contextMenuOptions == null)
                contextMenuOptions = new List<string>(); //this will be populated by a presenter
        }

        /// <summary>
        /// Provide a new data provider to populate the table with.
        /// Used by presentors that are displaying data instead of editting a model. (eg DataStore)
        /// </summary>
        /// <param name="dataProvider"></param>
        /// <param name="frozenColumns"></param>
        /// <param name="frozenRows"></param>
        public void PopulateWithDataProvider(IDataProvider dataProvider)
        {
            grid.Cleanup();
            SetupSheet(dataProvider);
        }

        /// <summary>Refresh the grid.</summary>
        public void Refresh()
        {
            grid.SetDataProvider(dataProvider);
            grid?.UpdateScrollBars();
        }

        /// <summary>The number of rows of data in the grid.</summary>
        public int RowCount()
        {
            return grid.RowCount - grid.NumberFrozenRows;
        }

        /// <summary>
        /// Adds options to the right-click context menu, valid options are:
        /// Cut - Any Editable Cell
        /// Copy - Any Cell
        /// Paste - Any Editable Cell
        /// Delete - Any Editable Cell
        /// Select All - Any Cell
        /// Units - Change Units on Solute grids, only on row == 1
        /// </summary>
        public void AddContextMenuOptions(string[] options)
        {
            if (contextMenuOptions == null)
                contextMenuOptions = new List<string>();
            foreach (string text in options)
            {
                string textLower = text.ToLower();
                if (textLower == "cut" || textLower == "copy" || textLower == "paste" || textLower == "delete" || textLower == "select all" || textLower == "units")
                    contextMenuOptions.Add(textLower);
                else
                    throw new Exception(text + " is not a valid context menu option for a Grid");
            }
        }

        /// <summary>
        /// Adds intellisense to the grid.
        /// </summary>
        public void AddIntellisense(Model model)
        {
            // this.model = model;
            // intellisense = new IntellisensePresenter(sheetContainer as ViewBase);
            // intellisense.ItemSelected += OnIntellisenseItemSelected;
            //grid.Sheet.CellEditor.ShowIntellisense += OnIntellisenseNeedContextItems;
        }

        /// <summary>
        /// User has changed a cell.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="colIndices"></param>
        /// <param name="rowIndices"></param>
        /// <param name="values"></param>
        /// <param name="colIndex">The indices of the column that was changed.</param>
        /// <param name="rowIndex">The indices of the row that was changed.</param>
        private void OnCellChanged(IDataProvider sender, int[] colIndices, int[] rowIndices, string[] values)
        {
            if (CellChanged != null)
            {
                try
                {
                    SaveGridToModel();
                    CellChanged?.Invoke(sender, colIndices, rowIndices, values);
                    Refresh();
                }
                catch (Exception err)
                {
                    explorerPresenter.MainPresenter.ShowError(err.ToString());
                }
            }
        }

        /// <summary>
        /// User has right clicked - display popup menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnContextMenuPopup(object sender, ContextMenuEventArgs e)
        {
            bool validCell = grid.CellHitTest((int)e.X, (int)e.Y, out int columnIndex, out int rowIndex);

            if (validCell)
            {
                bool isReadOnly = grid.IsCellReadOnly(columnIndex, rowIndex);
                var menuItems = new List<MenuDescriptionArgs>();

                foreach (string option in contextMenuOptions)
                {
                    if (option.CompareTo("units") == 0 && model is Solute) //used by solute grids to change units
                    {
                        if (rowIndex == 1)
                        {
                            foreach (string units in new List<string>{"ppm", "kgha"})
                            {
                                var menuItem = new MenuDescriptionArgs()
                                {
                                    Name = units,
                                };
                                menuItem.OnClick += OnUnitsChanged;
                                menuItems.Add(menuItem);
                            }
                        }
                    }

                    bool showingUnits = false;
                    if (contextMenuOptions.Contains("units") && rowIndex == 1)
                        showingUnits = true;

                    if (!showingUnits)
                    {
                        if (option.CompareTo("cut") == 0 && !isReadOnly)
                        {
                            MenuDescriptionArgs menuItem = new MenuDescriptionArgs()
                            {
                                Name = "Cut",
                                ShortcutKey = "Ctrl+X"
                            };
                            menuItem.OnClick += OnCut;
                            menuItems.Add(menuItem);
                        }
                        if (option.CompareTo("copy") == 0)
                        {
                            MenuDescriptionArgs menuItem = new MenuDescriptionArgs()
                            {
                                Name = "Copy",
                                ShortcutKey = "Ctrl+C"
                            };
                            menuItem.OnClick += OnCopy;
                            menuItems.Add(menuItem);
                        }
                        if (option.CompareTo("paste") == 0 && !isReadOnly)
                        {
                            MenuDescriptionArgs menuItem = new MenuDescriptionArgs()
                            {
                                Name = "Paste",
                                ShortcutKey = "Ctrl+V"
                            };
                            menuItem.OnClick += OnPaste;
                            menuItems.Add(menuItem);
                        }
                        if (option.CompareTo("delete") == 0 && !isReadOnly)
                        {
                            MenuDescriptionArgs menuItem = new MenuDescriptionArgs()
                            {
                                Name = "Delete",
                                ShortcutKey = "Delete"
                            };
                            menuItem.OnClick += OnDelete;
                            menuItems.Add(menuItem);
                        }
                        if (option.CompareTo("select all") == 0)
                        {
                            MenuDescriptionArgs menuItem = new MenuDescriptionArgs()
                            {
                                Name = "Select All",
                                ShortcutKey = ""
                            };
                            menuItem.OnClick += OnSelectAll;
                            menuItems.Add(menuItem);
                        }
                    }
                }

                if (menuItems.Count > 0)
                {
                    contextMenu.Populate(menuItems);
                    contextMenu.Show();
                }
            }
        }

        /// <summary>
        /// User has selected cut.
        /// </summary>
        private void OnUnitsChanged(object sender, EventArgs e)
        {
            Solute solute = (model as Solute);
            Solute.UnitsEnum newUnits = Solute.UnitsEnum.ppm;
            if ((sender as Gtk.MenuItem).Label == "kgha")
                newUnits = Solute.UnitsEnum.kgha;
            explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(solute, "InitialValuesUnits", newUnits));
        }

        /// <summary>
        /// User has selected cut.
        /// </summary>
        private void OnCut(object sender, EventArgs e)
        {
            grid.Cut();
        }

        /// <summary>
        /// User has selected copy.
        /// </summary>
        private void OnCopy(object sender, EventArgs e)
        {
            grid.Copy();
        }

        /// <summary>
        /// User has selected paste.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPaste(object sender, EventArgs e)
        {
            grid.Paste();
        }

        /// <summary>
        /// User has selected cut.
        /// </summary>
        private void OnDelete(object sender, EventArgs e)
        {
            grid.Delete();
        }

        /// <summary>
        /// User has selected cut.
        /// </summary>
        private void OnSelectAll(object sender, EventArgs e)
        {
            grid.SelectAll();
        }

        /// <summary>
        /// The mode has changed (probably via undo/redo).
        /// </summary>
        /// <param name="changedModel">The model with changes</param>
        private void OnModelChanged(object changedModel)
        {
            IModel changed = changedModel as IModel;
            if (changed != null)
            {
                dataProvider = ModelToSheetDataProvider.ToSheetDataProvider(changedModel as IModel);
                Refresh();
            }
        }

        /// <summary>Save the contents of the grid to the model.</summary>
        private void SaveGridToModel()
        {
            if (replaceModelCommand != null)
                replaceModelCommand.Replacement = model as IModel;
        }

        /// <summary>
        /// Invoked when the user types a . into the editter.
        /// Inserts the selected item at the caret.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnIntellisenseNeedContextItems(object sender, NeedContextItemsArgs args)
        {
            // try
            // {
            //     if (intellisense.GenerateGridCompletions(args.Code, args.Code.Length, model, true, false, false, false))
            //         intellisense.Show(args.Coordinates.X, args.Coordinates.Y);
            // }
            // catch (Exception err)
            // {
            //     explorerPresenter.MainPresenter.ShowError(err);
            // }
        }

        // /// <summary>
        // /// Invoked when the user selects an item in the intellisense.
        // /// Inserts the selected item at the caret.
        // /// </summary>
        // /// <param name="sender">Sender object.</param>
        // /// <param name="args">Event arguments.</param>
        // private void OnIntellisenseItemSelected(object sender, IntellisenseItemSelectedArgs args)
        // {
        //     try
        //     {
        //         grid.Sheet.CellEditor.EndEdit();
        //         grid.Sheet.CellSelector.GetSelection(out int columnIndex, out int rowIndex);
        //         string text = grid.Sheet.DataProvider.GetCellContents(columnIndex, rowIndex);
        //         grid.Sheet.DataProvider.SetCellContents(new int[]{columnIndex},
        //                                                 new int[] {rowIndex},
        //                                                 new string[]{text + args.ItemSelected});
        //         grid.Sheet.CalculateBounds(columnIndex, rowIndex);

        //         grid.Sheet.CellEditor.Edit(); //keep editting window open
        //     }
        //     catch (Exception err)
        //     {
        //         explorerPresenter.MainPresenter.ShowError(err);
        //     }
        // }
    }

}