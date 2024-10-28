namespace APSIM.Shared.Documentation
{
    /// <summary>
    /// This class describes a header for a document - it has a title and should appear at the top of the document
    /// </summary>
    public class Header : ITag
    {
        /// <summary>The Header title.</summary>
        public string Title { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Header"/> class.
        /// </summary>
        /// <param name="title">The header title.</param>
        public Header(string title)
        {
            Title = title;
        }
    }
}
