using System.Collections.Generic;
using System.Threading.Tasks;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Library;

/// <summary>
/// A service to manage virtual folders.
/// </summary>
public interface IVirtualFolderManager
{
    /// <summary>
    /// Gets the default view.
    /// </summary>
    /// <param name="includeRefreshState">A value indicating whether to update the refresh state.</param>
    /// <returns>The virtual folders.</returns>
    IEnumerable<VirtualFolderInfo> GetVirtualFolders(bool includeRefreshState = false);

    /// <summary>
    /// Adds a virtual folder.
    /// </summary>
    /// <param name="name">The name of the new virtual folder.</param>
    /// <param name="collectionType">The collection type options.</param>
    /// <param name="options">The library options.</param>
    /// <param name="refreshLibrary">A value indicating whether to refresh the associated library.</param>
    /// <returns>The async task.</returns>
    Task AddVirtualFolder(string name, CollectionTypeOptions? collectionType, LibraryOptions options, bool refreshLibrary);

    /// <summary>
    /// Renames a virtual folder.
    /// </summary>
    /// <param name="name">The current name.</param>
    /// <param name="newName">The new name.</param>
    /// <param name="refreshLibrary">A value indicating whether to refresh the associated library.</param>
    /// <returns>The async task.</returns>
    Task RenameVirtualFolder(string name, string newName, bool refreshLibrary);

    /// <summary>
    /// Removes a virtual folder.
    /// </summary>
    /// <param name="name">The name of the virtual folder.</param>
    /// <param name="refreshLibrary">A value indicating whether to refresh the associated library.</param>
    /// <returns>The async task.</returns>
    Task RemoveVirtualFolder(string name, bool refreshLibrary);

    /// <summary>
    /// Adds a media path to a virtual folder.
    /// </summary>
    /// <param name="virtualFolderName">The name of the virtual folder.</param>
    /// <param name="mediaPath">The media path info.</param>
    /// <param name="refreshLibrary">A value indicating whether to refresh the associated library.</param>
    void AddMediaPath(string virtualFolderName, MediaPathInfo mediaPath, bool refreshLibrary);

    /// <summary>
    /// Updates a media path in a virtual folder.
    /// </summary>
    /// <param name="virtualFolderName">The name of the virtual folder.</param>
    /// <param name="mediaPath">The media path info.</param>
    void UpdateMediaPath(string virtualFolderName, MediaPathInfo mediaPath);

    /// <summary>
    /// Removes a media path from a virtual folder.
    /// </summary>
    /// <param name="virtualFolderName">The name of the virtual folder.</param>
    /// <param name="mediaPath">The media path.</param>
    /// <param name="refreshLibrary">A value indicating whether to refresh the associated library.</param>
    void RemoveMediaPath(string virtualFolderName, string mediaPath, bool refreshLibrary);
}
