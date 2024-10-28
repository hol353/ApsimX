using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Documentation;
using DocumentationGraphPage = APSIM.Shared.Documentation.GraphPage;

namespace APSIM.Documentation;

/// <summary>
/// Contains utility methods for Documentation.
/// </summary>
public static class DocumentationUtilities
{
    /// <summary>
    /// Gets a documentation list without empty tags.
    /// </summary>
    /// <param name="tags"></param>
    /// <returns></returns>
    public static List<ITag> CleanEmptySections(List<ITag> tags)
    {
        bool blankFound = CheckForBlankTags(tags);
        while (blankFound)
        {
            List<ITag> cleanedTags = CreateListWithoutEmptyTags(tags);
            tags = cleanedTags;
            blankFound = CheckForBlankTags(tags);
        }
        return tags;
    }

    /// <summary>
    /// Cleans a list of documentation tags.
    /// </summary>
    /// <param name="tags"></param>
    /// <returns>Tuple with boolean that notifies if any tags where empty and a list of tags that are not empty.</returns>
    private static List<ITag> CreateListWithoutEmptyTags(List<ITag> tags)
    {            
        List<ITag> newTags = new();
        foreach(ITag tag in tags)
        {
            if (tag is Section)
            {
                string title = (tag as Section).Title;
                List<ITag> children = (tag as Section).Children;
                if (children.Count > 0)
                {
                    children = CreateListWithoutEmptyTags(children);
                    newTags.Add(new Section(title, children));
                }
            }
            else if (tag is Paragraph paragraph)
            {
                if (!string.IsNullOrWhiteSpace(paragraph.text))
                    newTags.Add(tag);
            }
            else if (tag is DocumentationGraphPage graphPage)
            {
                if (graphPage.Graphs.Count() > 0)
                    newTags.Add(tag);
            }
            else
            {
                newTags.Add(tag);
            }
        }
        return newTags;
    }

    /// <summary>
    /// Cleans a list of documentation tags.
    /// </summary>
    /// <param name="tags"></param>
    /// <returns>Tuple with boolean that notifies if any tags where empty and a list of tags that are not empty.</returns>
    private static bool CheckForBlankTags(List<ITag> tags)
    {
        bool blankFound = false;
        foreach(ITag tag in tags)
        {
            if (tag is Section)
            {
                List<ITag> children = (tag as Section).Children;
                if (children.Count > 0)
                {
                    bool result = CheckForBlankTags(children);
                    if (result == true)
                        blankFound = true;
                }
                else
                {
                    blankFound = true;
                }
            }
            else if (tag is Paragraph paragraph)
            {
                if (string.IsNullOrWhiteSpace(paragraph.text))
                    blankFound = true;
            }
            else if (tag is DocumentationGraphPage graphPage)
            {
                if (graphPage.Graphs.Count() == 0)
                    blankFound = true;
            }
        }
        return blankFound;
    }

    /// <summary>
    /// Replaces a starting section with a header, or adds a header if first tag is a paragraph
    /// </summary>
    /// <param name="title"></param>
    /// <param name="tags"></param>
    /// <returns></returns>
    public static List<ITag> AddHeader(string title, List<ITag> tags) {

        List<ITag> newTags = new List<ITag>();
        ITag first = tags.First();
        
        if (tags.First().GetType() == typeof(Paragraph))
        {
            newTags.Add(new Section(title, first));
            tags.Remove(first);
            newTags.AddRange(tags);
            return newTags;
        }
        else
        {
            return tags;
        }
    }
}