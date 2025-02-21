using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.LocalMetadata.Savers
{
    /// <inheritdoc />
    public abstract class BaseXmlSaver : IMetadataFileSaver
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseXmlSaver"/> class.
        /// </summary>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="configurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{BaseXmlSaver}"/> interface.</param>
        protected BaseXmlSaver(IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, ILogger<BaseXmlSaver> logger)
        {
            FileSystem = fileSystem;
            ConfigurationManager = configurationManager;
            LibraryManager = libraryManager;
            Logger = logger;
        }

        /// <summary>
        /// Gets the file system.
        /// </summary>
        protected IFileSystem FileSystem { get; private set; }

        /// <summary>
        /// Gets the configuration manager.
        /// </summary>
        protected IServerConfigurationManager ConfigurationManager { get; private set; }

        /// <summary>
        /// Gets the library manager.
        /// </summary>
        protected ILibraryManager LibraryManager { get; private set; }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        protected ILogger<BaseXmlSaver> Logger { get; private set; }

        /// <inheritdoc />
        public string Name => XmlProviderUtils.Name;

        /// <inheritdoc />
        public string GetSavePath(BaseItem item)
        {
            return GetLocalSavePath(item);
        }

        /// <summary>
        /// Gets the save path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        protected abstract string GetLocalSavePath(BaseItem item);

        /// <summary>
        /// Gets the name of the root element.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        protected virtual string GetRootElementName(BaseItem item)
            => "Item";

        /// <summary>
        /// Determines whether [is enabled for] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="updateType">Type of the update.</param>
        /// <returns><c>true</c> if [is enabled for] [the specified item]; otherwise, <c>false</c>.</returns>
        public abstract bool IsEnabledFor(BaseItem item, ItemUpdateType updateType);

        /// <inheritdoc />
        public async Task SaveAsync(BaseItem item, CancellationToken cancellationToken)
        {
            var path = GetSavePath(item);
            var directory = Path.GetDirectoryName(path) ?? throw new InvalidDataException($"Provided path ({path}) is not valid.");
            Directory.CreateDirectory(directory);

            // On Windows, saving the file will fail if the file is hidden or readonly
            FileSystem.SetAttributes(path, false, false);

            var fileStreamOptions = new FileStreamOptions()
            {
                Mode = FileMode.Create,
                Access = FileAccess.Write,
                Share = FileShare.None
            };

            var filestream = new FileStream(path, fileStreamOptions);
            await using (filestream.ConfigureAwait(false))
            {
                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    Encoding = Encoding.UTF8,
                    Async = true
                };

                var writer = XmlWriter.Create(filestream, settings);
                await using (writer.ConfigureAwait(false))
                {
                    var root = GetRootElementName(item);

                    await writer.WriteStartDocumentAsync(true).ConfigureAwait(false);

                    await writer.WriteStartElementAsync(null, root, null).ConfigureAwait(false);

                    var baseItem = item;

                    if (baseItem is not null)
                    {
                        await AddCommonNodesAsync(baseItem, writer).ConfigureAwait(false);
                    }

                    await WriteCustomElementsAsync(item, writer).ConfigureAwait(false);

                    await writer.WriteEndElementAsync().ConfigureAwait(false);

                    await writer.WriteEndDocumentAsync().ConfigureAwait(false);
                }
            }

            if (ConfigurationManager.Configuration.SaveMetadataHidden)
            {
                SetHidden(path, true);
            }
        }

        private void SetHidden(string path, bool hidden)
        {
            try
            {
                FileSystem.SetHidden(path, hidden);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error setting hidden attribute on {Path}", path);
            }
        }

        /// <summary>
        /// Write custom elements.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="writer">The xml writer.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        protected abstract Task WriteCustomElementsAsync(BaseItem item, XmlWriter writer);

        /// <summary>
        /// Adds the common nodes.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="writer">The xml writer.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        private async Task AddCommonNodesAsync(BaseItem item, XmlWriter writer)
        {
            if (!string.IsNullOrEmpty(item.OfficialRating))
            {
                await writer.WriteElementStringAsync(null, "ContentRating", null, item.OfficialRating).ConfigureAwait(false);
            }

            await writer.WriteElementStringAsync(null, "Added", null, item.DateCreated.ToLocalTime().ToString("G", CultureInfo.InvariantCulture)).ConfigureAwait(false);

            await writer.WriteElementStringAsync(null, "LockData", null, item.IsLocked.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()).ConfigureAwait(false);

            if (item.LockedFields.Length > 0)
            {
                await writer.WriteElementStringAsync(null, "LockedFields", null, string.Join('|', item.LockedFields)).ConfigureAwait(false);
            }

            if (item.CriticRating.HasValue)
            {
                await writer.WriteElementStringAsync(null, "CriticRating", null, item.CriticRating.Value.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(item.Overview))
            {
                await writer.WriteElementStringAsync(null, "Overview", null, item.Overview).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(item.OriginalTitle))
            {
                await writer.WriteElementStringAsync(null, "OriginalTitle", null, item.OriginalTitle).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(item.CustomRating))
            {
                await writer.WriteElementStringAsync(null, "CustomRating", null, item.CustomRating).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(item.Name) && item is not Episode)
            {
                await writer.WriteElementStringAsync(null, "LocalTitle", null, item.Name).ConfigureAwait(false);
            }

            var forcedSortName = item.ForcedSortName;
            if (!string.IsNullOrEmpty(forcedSortName))
            {
                await writer.WriteElementStringAsync(null, "SortTitle", null, forcedSortName).ConfigureAwait(false);
            }

            if (item.PremiereDate.HasValue)
            {
                if (item is Person)
                {
                    await writer.WriteElementStringAsync(null, "BirthDate", null, item.PremiereDate.Value.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).ConfigureAwait(false);
                }
                else if (item is not Episode)
                {
                    await writer.WriteElementStringAsync(null, "PremiereDate", null, item.PremiereDate.Value.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).ConfigureAwait(false);
                }
            }

            if (item.EndDate.HasValue)
            {
                if (item is Person)
                {
                    await writer.WriteElementStringAsync(null, "DeathDate", null, item.EndDate.Value.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).ConfigureAwait(false);
                }
                else if (item is not Episode)
                {
                    await writer.WriteElementStringAsync(null, "EndDate", null, item.EndDate.Value.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).ConfigureAwait(false);
                }
            }

            if (item.RemoteTrailers.Count > 0)
            {
                await writer.WriteStartElementAsync(null, "Trailers", null).ConfigureAwait(false);

                foreach (var trailer in item.RemoteTrailers)
                {
                    await writer.WriteElementStringAsync(null, "Trailer", null, trailer.Url).ConfigureAwait(false);
                }

                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }

            if (item.ProductionLocations.Length > 0)
            {
                await writer.WriteStartElementAsync(null, "Countries", null).ConfigureAwait(false);

                foreach (var name in item.ProductionLocations)
                {
                    await writer.WriteElementStringAsync(null, "Country", null, name).ConfigureAwait(false);
                }

                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }

            if (item is IHasDisplayOrder hasDisplayOrder && !string.IsNullOrEmpty(hasDisplayOrder.DisplayOrder))
            {
                await writer.WriteElementStringAsync(null, "DisplayOrder", null, hasDisplayOrder.DisplayOrder).ConfigureAwait(false);
            }

            if (item.CommunityRating.HasValue)
            {
                await writer.WriteElementStringAsync(null, "Rating", null, item.CommunityRating.Value.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
            }

            if (item.ProductionYear.HasValue && item is not Person)
            {
                await writer.WriteElementStringAsync(null, "ProductionYear", null, item.ProductionYear.Value.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
            }

            if (item is IHasAspectRatio hasAspectRatio)
            {
                if (!string.IsNullOrEmpty(hasAspectRatio.AspectRatio))
                {
                    await writer.WriteElementStringAsync(null, "AspectRatio", null, hasAspectRatio.AspectRatio).ConfigureAwait(false);
                }
            }

            if (!string.IsNullOrEmpty(item.PreferredMetadataLanguage))
            {
                await writer.WriteElementStringAsync(null, "Language", null, item.PreferredMetadataLanguage).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(item.PreferredMetadataCountryCode))
            {
                await writer.WriteElementStringAsync(null, "CountryCode", null, item.PreferredMetadataCountryCode).ConfigureAwait(false);
            }

            // Use original runtime here, actual file runtime later in MediaInfo
            var runTimeTicks = item.RunTimeTicks;

            if (runTimeTicks.HasValue)
            {
                var timespan = TimeSpan.FromTicks(runTimeTicks.Value);

                await writer.WriteElementStringAsync(null, "RunningTime", null, Math.Floor(timespan.TotalMinutes).ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
            }

            if (item.ProviderIds is not null)
            {
                foreach (var providerKey in item.ProviderIds.Keys)
                {
                    var providerId = item.ProviderIds[providerKey];
                    if (!string.IsNullOrEmpty(providerId))
                    {
                        await writer.WriteElementStringAsync(null, providerKey + "Id", null, providerId).ConfigureAwait(false);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(item.Tagline))
            {
                await writer.WriteStartElementAsync(null, "Taglines", null).ConfigureAwait(false);
                await writer.WriteElementStringAsync(null, "Tagline", null, item.Tagline).ConfigureAwait(false);
                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }

            if (item.Genres.Length > 0)
            {
                await writer.WriteStartElementAsync(null, "Genres", null).ConfigureAwait(false);

                foreach (var genre in item.Genres)
                {
                    await writer.WriteElementStringAsync(null, "Genre", null, genre).ConfigureAwait(false);
                }

                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }

            if (item.Studios.Length > 0)
            {
                await writer.WriteStartElementAsync(null, "Studios", null).ConfigureAwait(false);

                foreach (var studio in item.Studios)
                {
                    await writer.WriteElementStringAsync(null, "Studio", null, studio).ConfigureAwait(false);
                }

                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }

            if (item.Tags.Length > 0)
            {
                await writer.WriteStartElementAsync(null, "Tags", null).ConfigureAwait(false);

                foreach (var tag in item.Tags)
                {
                    await writer.WriteElementStringAsync(null, "Tag", null, tag).ConfigureAwait(false);
                }

                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }

            var people = LibraryManager.GetPeople(item);

            if (people.Count > 0)
            {
                await writer.WriteStartElementAsync(null, "Persons", null).ConfigureAwait(false);

                foreach (var person in people)
                {
                    await writer.WriteStartElementAsync(null, "Person", null).ConfigureAwait(false);
                    await writer.WriteElementStringAsync(null, "Name", null, person.Name).ConfigureAwait(false);
                    await writer.WriteElementStringAsync(null, "Type", null, person.Type.ToString()).ConfigureAwait(false);
                    await writer.WriteElementStringAsync(null, "Role", null, person.Role).ConfigureAwait(false);

                    if (person.SortOrder.HasValue)
                    {
                        await writer.WriteElementStringAsync(null, "SortOrder", null, person.SortOrder.Value.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
                    }

                    await writer.WriteEndElementAsync().ConfigureAwait(false);
                }

                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }

            if (item is BoxSet boxset)
            {
                await AddLinkedChildren(boxset, writer, "CollectionItems", "CollectionItem").ConfigureAwait(false);
            }

            if (item is Playlist playlist && !Playlist.IsPlaylistFile(playlist.Path))
            {
                await writer.WriteElementStringAsync(null, "OwnerUserId", null, playlist.OwnerUserId.ToString("N")).ConfigureAwait(false);
                await AddLinkedChildren(playlist, writer, "PlaylistItems", "PlaylistItem").ConfigureAwait(false);
            }

            if (item is IHasShares hasShares)
            {
                await AddSharesAsync(hasShares, writer).ConfigureAwait(false);
            }

            await AddMediaInfo(item, writer).ConfigureAwait(false);
        }

        /// <summary>
        /// Add shares.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="writer">The xml writer.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        private static async Task AddSharesAsync(IHasShares item, XmlWriter writer)
        {
            await writer.WriteStartElementAsync(null, "Shares", null).ConfigureAwait(false);

            foreach (var share in item.Shares)
            {
                await writer.WriteStartElementAsync(null, "Share", null).ConfigureAwait(false);

                await writer.WriteElementStringAsync(null, "UserId", null, share.UserId.ToString()).ConfigureAwait(false);
                await writer.WriteElementStringAsync(
                    null,
                    "CanEdit",
                    null,
                    share.CanEdit.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()).ConfigureAwait(false);

                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }

            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Appends the media info.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="writer">The xml writer.</param>
        /// <typeparam name="T">Type of item.</typeparam>
        /// <returns>The task object representing the asynchronous operation.</returns>
        private static Task AddMediaInfo<T>(T item, XmlWriter writer)
            where T : BaseItem
        {
            if (item is Video video && video.Video3DFormat.HasValue)
            {
                return video.Video3DFormat switch
                {
                    Video3DFormat.FullSideBySide =>
                        writer.WriteElementStringAsync(null, "Format3D", null, "FSBS"),
                    Video3DFormat.FullTopAndBottom =>
                        writer.WriteElementStringAsync(null, "Format3D", null, "FTAB"),
                    Video3DFormat.HalfSideBySide =>
                        writer.WriteElementStringAsync(null, "Format3D", null, "HSBS"),
                    Video3DFormat.HalfTopAndBottom =>
                        writer.WriteElementStringAsync(null, "Format3D", null, "HTAB"),
                    Video3DFormat.MVC =>
                        writer.WriteElementStringAsync(null, "Format3D", null, "MVC"),
                    _ => Task.CompletedTask
                };
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// ADd linked children.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="writer">The xml writer.</param>
        /// <param name="pluralNodeName">The plural node name.</param>
        /// <param name="singularNodeName">The singular node name.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        private static async Task AddLinkedChildren(Folder item, XmlWriter writer, string pluralNodeName, string singularNodeName)
        {
            var items = item.LinkedChildren
                .Where(i => i.Type == LinkedChildType.Manual)
                .ToList();

            if (items.Count == 0)
            {
                return;
            }

            await writer.WriteStartElementAsync(null, pluralNodeName, null).ConfigureAwait(false);

            foreach (var link in items)
            {
                if (!string.IsNullOrWhiteSpace(link.Path) || !string.IsNullOrWhiteSpace(link.LibraryItemId))
                {
                    await writer.WriteStartElementAsync(null, singularNodeName, null).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(link.Path))
                    {
                        await writer.WriteElementStringAsync(null, "Path", null, link.Path).ConfigureAwait(false);
                    }

                    if (!string.IsNullOrWhiteSpace(link.LibraryItemId))
                    {
                        await writer.WriteElementStringAsync(null, "ItemId", null, link.LibraryItemId).ConfigureAwait(false);
                    }

                    await writer.WriteEndElementAsync().ConfigureAwait(false);
                }
            }

            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }
}
