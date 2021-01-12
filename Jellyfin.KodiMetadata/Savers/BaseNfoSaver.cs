using System;
using System.IO;
using System.Threading;
using Jellyfin.KodiMetadata.Models;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.KodiMetadata.Savers
{
    /// <summary>
    /// The base nfo metadata saver.
    /// </summary>
    /// <typeparam name="T1">The base item to save.</typeparam> // todo maybe not used
    /// <typeparam name="T2">The nfo object type.</typeparam>
    public abstract class BaseNfoSaver<T1, T2> : IMetadataFileSaver
        where T1 : BaseItem
        where T2 : BaseNfo, new()
    {
        private readonly ILogger<BaseNfoSaver<T1, T2>> _logger;
        private readonly IXmlSerializer _xmlSerializer;
        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _configurationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseNfoSaver{T1, T2}"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{TCategoryName}"/> interface.</param>
        /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="configurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        protected BaseNfoSaver(
            ILogger<BaseNfoSaver<T1, T2>> logger,
            IXmlSerializer xmlSerializer,
            IFileSystem fileSystem,
            IServerConfigurationManager configurationManager)
        {
            _logger = logger;
            _xmlSerializer = xmlSerializer;
            _fileSystem = fileSystem;
            _configurationManager = configurationManager;
        }

        /// <inheritdoc />
        public string Name => "Nfo";

        /// <summary>
        /// Gets the minimum type of update for rewriting the nfo.
        /// </summary>
        protected ItemUpdateType MinimumUpdateType
        {
            get
            {
                // TODO
                // if (_configurationManager.GetNfoConfiguration().SaveImagePathsInNfo)
                // {
                //     return ItemUpdateType.ImageUpdate;
                // }

                return ItemUpdateType.MetadataDownload;
            }
        }

        /// <inheritdoc />
        public void Save(BaseItem item, CancellationToken cancellationToken)
        {
            var nfo = new T2();
            MapJellyfinToNfoObject(item, nfo);
            using var memoryStream = new MemoryStream();
            _xmlSerializer.SerializeToStream(nfo, memoryStream);

            cancellationToken.ThrowIfCancellationRequested();

            SaveToFile(memoryStream, GetSavePath(item));
        }

        /// <inheritdoc />
        public abstract string GetSavePath(BaseItem item);

        /// <inheritdoc />
        public abstract bool IsEnabledFor(BaseItem item, ItemUpdateType updateType);

        /// <summary>
        /// Maps the base item to the <see cref="T2"/> nfo object.
        /// </summary>
        /// <param name="item">The base item to map to the nfo.</param>
        /// <param name="nfo">The nfo to map to.</param>
        protected virtual void MapJellyfinToNfoObject(BaseItem item, T2 nfo)
        {
            throw new System.NotImplementedException();
        }

        private void SaveToFile(Stream stream, string path)
        {
            var directory = Path.GetDirectoryName(path) ?? throw new ArgumentException($"Provided path ({path}) is not valid.", nameof(path));
            Directory.CreateDirectory(directory);

            // On Windows, savint the file will fail if the file is hidden or readonly
            _fileSystem.SetAttributes(path, false, false);

            using (var filestream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                stream.CopyTo(filestream);
            }

            if (_configurationManager.Configuration.SaveMetadataHidden)
            {
                try
                {
                    _fileSystem.SetHidden(path, true);
                }
                catch (IOException ex)
                {
                    _logger.LogError(ex, "Error setting hidden attribute on {Path}", path);
                }
            }
        }
    }
}
