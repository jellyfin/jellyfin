using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;

namespace MediaBrowser.LocalMetadata
{
    /// <summary>
    /// The BaseXmlProvider.
    /// </summary>
    /// <typeparam name="T">Type of provider.</typeparam>
    public abstract class BaseXmlProvider<T> : ILocalMetadataProvider<T>, IHasItemChangeMonitor, IHasOrder
        where T : BaseItem, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseXmlProvider{T}"/> class.
        /// </summary>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        protected BaseXmlProvider(IFileSystem fileSystem)
        {
            this.FileSystem = fileSystem;
        }

        /// <inheritdoc />
        public string Name => XmlProviderUtils.Name;

        /// After Nfo
        /// <inheritdoc />
        public virtual int Order => 1;

        /// <summary>
        /// Gets the IFileSystem.
        /// </summary>
        protected IFileSystem FileSystem { get; }

        /// <summary>
        /// Gets metadata for item.
        /// </summary>
        /// <param name="info">The item info.</param>
        /// <param name="directoryService">Instance of the <see cref="IDirectoryService"/> interface.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The metadata for item.</returns>
        public Task<MetadataResult<T>> GetMetadata(
            ItemInfo info,
            IDirectoryService directoryService,
            CancellationToken cancellationToken)
        {
            var result = new MetadataResult<T>();

            var file = GetXmlFile(info, directoryService);

            if (file is null)
            {
                return Task.FromResult(result);
            }

            var path = file.FullName;

            try
            {
                result.Item = new T();

                Fetch(result, path, cancellationToken);
                result.HasMetadata = true;
            }
            catch (FileNotFoundException)
            {
                result.HasMetadata = false;
            }
            catch (IOException)
            {
                result.HasMetadata = false;
            }

            return Task.FromResult(result);
        }

        /// <summary>
        /// Get metadata from path.
        /// </summary>
        /// <param name="result">Resulting metadata.</param>
        /// <param name="path">The path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        protected abstract void Fetch(MetadataResult<T> result, string path, CancellationToken cancellationToken);

        /// <summary>
        /// Get metadata from xml file.
        /// </summary>
        /// <param name="info">Item inf.</param>
        /// <param name="directoryService">Instance of the <see cref="IDirectoryService"/> interface.</param>
        /// <returns>The file system metadata.</returns>
        protected abstract FileSystemMetadata? GetXmlFile(ItemInfo info, IDirectoryService directoryService);

        /// <inheritdoc />
        public bool HasChanged(BaseItem item, IDirectoryService directoryService)
        {
            var file = GetXmlFile(new ItemInfo(item), directoryService);

            if (file is null)
            {
                return false;
            }

            return file.Exists && FileSystem.GetLastWriteTimeUtc(file) > item.DateLastSaved;
        }
    }
}
