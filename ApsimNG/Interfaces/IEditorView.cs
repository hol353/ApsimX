using System;
using Gtk;
using UserInterface.EventArguments;
using Shared.Utilities;

namespace UserInterface.Interfaces
{
    /// <summary>
    /// What sort of text is this editor displaying?
    /// This is used to determine syntax highlighting rules.
    /// We could potentially add more options here in future if, say,
    /// we were to implement a python manager component.
    /// </summary>
    public enum EditorType
    {
        /// <summary>
        /// C# manager script.
        /// </summary>
        ManagerScript,

        /// <summary>
        /// Report.
        /// </summary>
        Report,

        /// <summary>
        /// Anything else - this will disable syntax highlighting.
        /// </summary>
        Other
    };

    /// <summary>
    /// This is IEditorView interface
    /// </summary>
    public interface IEditorView
    {
        /// <summary>
        /// Invoked when the editor needs context items (after user presses '.')
        /// </summary>
        event EventHandler<NeedContextItemsArgs> ContextItemsNeeded;

        /// <summary>
        /// Invoked when the user changes the text in the editor.
        /// </summary>
        event EventHandler TextHasChangedByUser;

        /// <summary>
        /// Invoked when the user leaves the text editor.
        /// </summary>
        event EventHandler LeaveEditor;

        /// <summary>
        /// Invoked when the user changes the style.
        /// </summary>
        event EventHandler StyleChanged;

        /// <summary>
        /// Invoked when the user drops a variable on the EditorView.
        /// </summary>
        public event EventHandler VariableDragDataReceived;

        /// <summary>
        /// Gets or sets the text property to get and set the content of the editor.
        /// </summary>
        string Text { get; set; }

        /// <summary>
        /// Gets or sets the lines property to get and set the lines in the editor.
        /// </summary>
        string[] Lines { get; set; }

        /// <summary>
        /// Gets or Sets the script as read only (editable)
        /// </summary>
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Controls syntax highlighting mode.
        /// </summary>
        EditorType Mode { get; set; }

        /// <summary>
        /// Gets or sets the characters that bring up the intellisense context menu.
        /// </summary>
        string IntelliSenseChars { get; set; }

        /// <summary>
        /// Gets the current line number
        /// </summary>
        int CurrentLineNumber { get; }

        /// <summary>
        /// Gets or sets the current location of the caret (column and line)
        /// </summary>
        ManagerCursorLocation Location { get; set; }

        /// <summary>
        /// Add a separator line to the context menu
        /// </summary>
        MenuItem AddContextSeparator();

        /// <summary>
        /// Add an action (on context menu) on the series grid.
        /// </summary>
        /// <param name="menuItemText">The text of the menu item</param>
        /// <param name="onClick">The event handler to call when menu is selected</param>
        /// <param name="shortcut">Describes the key to use as the accelerator</param>
        MenuItem AddContextActionWithAccel(string menuItemText, System.EventHandler onClick, string shortcut);

        /// <summary>
        /// Offset of the caret from the beginning of the text editor.
        /// </summary>
        int Offset { get; }

        /// <summary>
        /// Returns true iff this text editor has the focus
        /// (ie it can receive keyboard input).
        /// </summary>
        bool HasFocus { get; }

        /// <summary>
        /// Inserts text at a given offset in the editor.
        /// </summary>
        /// <param name="text">Text to be inserted.</param>
        void InsertAtCaret(string text);

        /// <summary>
        /// Inserts a new completion option at the caret, potentially overwriting a partially-completed word.
        /// </summary>
        /// <param name="triggerWord">
        /// Word to be overwritten. May be empty.
        /// This function will overwrite the last occurrence of this word before the caret.
        /// </param>
        /// <param name="completionOption">Completion option to be inserted.</param>
        void InsertCompletionOption(string completionOption, string triggerWord);

        /// <summary>
        /// Gets the location (in screen coordinates) of the cursor.
        /// </summary>
        /// <returns>Tuple, where item 1 is the x-coordinate and item 2 is the y-coordinate.</returns>
        System.Drawing.Point GetPositionOfCursor();

        /// <summary>
        /// Redraws the text editor.
        /// </summary>
        void Refresh();

        /// <summary>Gets or sets the widget visibility.</summary>
        bool Visible { get; set; }

        /// <summary>
        /// Hide the TextEditor
        /// </summary>
        public void Hide();

        /// <summary>
        /// Show the Text Editor
        /// </summary>
        public void Show();
    }
}
