
namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Interface IVirtualFolderCreator
    /// </summary>
    public interface IVirtualFolderCreator
    {
        /// <summary>
        /// Gets the folder.
        /// </summary>
        /// <returns>Folder.</returns>
        BasePluginFolder GetFolder();
    }
}
