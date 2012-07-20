using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Xml
{
    /// <summary>
    /// Fetches metadata fro a folder.
    /// Since folder.xml contains no folder-specific values, no overrides are needed
    /// </summary>
    public class FolderXmlParser : BaseItemXmlParser<Folder>
    {
    }
}
