using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
{
    /// <summary>
    /// Interface IProviderManager
    /// </summary>
    public interface IProviderManager
    {
        /// <summary>
        /// Downloads the and save image.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="source">The source.</param>
        /// <param name="targetName">Name of the target.</param>
        /// <param name="saveLocally">if set to <c>true</c> [save locally].</param>
        /// <param name="resourcePool">The resource pool.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.String}.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        Task<string> DownloadAndSaveImage(BaseItem item, string source, string targetName, bool saveLocally, SemaphoreSlim resourcePool, CancellationToken cancellationToken);

        /// <summary>
        /// Saves the image.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="source">The source.</param>
        /// <param name="targetName">Name of the target.</param>
        /// <param name="saveLocally">if set to <c>true</c> [save locally].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.String}.</returns>
        Task<string> SaveImage(BaseItem item, Stream source, string targetName, bool saveLocally, CancellationToken cancellationToken);

        /// <summary>
        /// Saves to library filesystem.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="path">The path.</param>
        /// <param name="dataToSave">The data to save.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        Task SaveToLibraryFilesystem(BaseItem item, string path, Stream dataToSave, CancellationToken cancellationToken);

        /// <summary>
        /// Executes the metadata providers.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="allowSlowProviders">if set to <c>true</c> [allow slow providers].</param>
        /// <returns>Task{System.Boolean}.</returns>
        Task<ItemUpdateType?> ExecuteMetadataProviders(BaseItem item, CancellationToken cancellationToken, bool force = false, bool allowSlowProviders = true);

        /// <summary>
        /// Adds the metadata providers.
        /// </summary>
        /// <param name="providers">The providers.</param>
        void AddParts(IEnumerable<BaseMetadataProvider> providers);

        /// <summary>
        /// Gets the save path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="targetFileName">Name of the target file.</param>
        /// <param name="saveLocally">if set to <c>true</c> [save locally].</param>
        /// <returns>System.String.</returns>
        string GetSavePath(BaseItem item, string targetFileName, bool saveLocally);
    }
}