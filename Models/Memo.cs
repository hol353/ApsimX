using System;
using Models.Core;

namespace Models
{

    /// <summary>This is a memo/text component that stores user entered text information.</summary>
    [Serializable]
    [ViewName("ApsimNG.Resources.Glade.MemoView.glade")]
    [PresenterName("UserInterface.Presenters.MemoPresenter")]
    [ValidParent(DropAnywhere = true)]
    public class Memo : Model
    {
        /// <summary>Gets or sets the memo text.</summary>
        [Description("Text of the memo")]
        public string Text { get; set; }
    }
}
