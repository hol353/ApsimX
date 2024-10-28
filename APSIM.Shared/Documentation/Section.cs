using System.Collections.Generic;
using System.Linq;

namespace APSIM.Shared.Documentation
{
    /// <summary>
    /// This class describes a section in a document - it has a title and contains
    /// multiple child tags.
    /// </summary>
    public class Section : ITag
    {
        /// <summary>Child tags.</summary>
        public List<ITag> Children { get; set; }

        /// <summary>The section title.</summary>
        public string Title { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Section"/> class.
        /// </summary>
        /// <param name="title">The section title. Can be null.</param>
        /// <param name="children">The child tags.</param>
        public Section(string title, IEnumerable<ITag> children)
        {
            Title = title;
            this.Children = children.ToList();
        }

        /// <summary>
        /// Initializes a new <see cref="Section"/> with no title.
        /// </summary>
        /// <param name="children">The child tags.</param>
        public Section(IEnumerable<ITag> children)
        {
            Title = null;
            this.Children = children.ToList();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Section"/> class.
        /// </summary>
        /// <param name="title">The section title. Can be null.</param>
        public Section(string title)
        {
            Title = title;
            this.Children = new List<ITag>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Section"/> class.
        /// </summary>
        public Section()
        {
            Title = null;
            this.Children = new List<ITag>();
        }

        /// <summary>
        /// Create a <see cref="Section"/> instance with a single child.
        /// </summary>
        /// <param name="title">The section title. Can be null.</param>
        /// <param name="child">The child tag.</param>
        public Section(string title, ITag child)
        {
            Title = title;
            Children = new List<ITag>() {child};
        }

        /// <summary>
        /// Check if a section is empty. A section is defined as non-empty
        /// iff it contains any content (ie any non-section children, or
        /// any non-section children of section children etc).
        /// </summary>
        public bool IsEmpty()
        {
            foreach (ITag tag in Children)
            {
                if (tag is Section section)
                {
                    // If the child section is non-empty, then this
                    // section is non-empty.
                    if (!section.IsEmpty())
                        return false;
                }
                else
                    // If the section contains any children which aren't
                    // a section, then this section is non-empty.
                    return false;
            }
            return true;
        }

        /// <summary>Adds an ITag as a child of this ITag</summary>
        public void Add(ITag tag) {
            Children.Add(tag);
        }

        /// <summary>Adds a list of ITags as a children of this ITag</summary>
        public void Add(IEnumerable<ITag> tags) {
            Children.AddRange(tags);
        }
    }
}
