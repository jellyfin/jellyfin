using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using MediaBrowser.Common.Extensions;
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
        /// Gets the date added format.
        /// </summary>
        public const string DateAddedFormat = "yyyy-MM-dd HH:mm:ss";

        private static readonly CultureInfo _usCulture = new CultureInfo("en-US");

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseXmlSaver"/> class.
        /// </summary>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="configurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="userDataManager">Instance of the <see cref="IUserDataManager"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{BaseXmlSaver}"/> interface.</param>
        public BaseXmlSaver(IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataManager, ILogger<BaseXmlSaver> logger)
        {
            FileSystem = fileSystem;
            ConfigurationManager = configurationManager;
            LibraryManager = libraryManager;
            UserManager = userManager;
            UserDataManager = userDataManager;
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
        /// Gets the user manager.
        /// </summary>
        protected IUserManager UserManager { get; private set; }

        /// <summary>
        /// Gets the user data manager.
        /// </summary>
        protected IUserDataManager UserDataManager { get; private set; }

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
        {
            return "Item";
        }

        /// <summary>
        /// Determines whether [is enabled for] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="updateType">Type of the update.</param>
        /// <returns><c>true</c> if [is enabled for] [the specified item]; otherwise, <c>false</c>.</returns>
        public abstract bool IsEnabledFor(BaseItem item, ItemUpdateType updateType);

        /// <inheritdoc />
        public void Save(BaseItem item, CancellationToken cancellationToken)
        {
            var path = GetSavePath(item);

            using var memoryStream = new MemoryStream();
            Save(item, memoryStream);

            memoryStream.Position = 0;

            cancellationToken.ThrowIfCancellationRequested();

            SaveToFile(memoryStream, path);
        }

        private void SaveToFile(Stream stream, string path)
        {
            var directory = Path.GetDirectoryName(path) ?? throw new ArgumentException($"Provided path ({path}) is not valid.", nameof(path));
            Directory.CreateDirectory(directory);
            // On Windows, savint the file will fail if the file is hidden or readonly
            FileSystem.SetAttributes(path, false, false);

            // use FileShare.None as this bypasses dotnet bug dotnet/runtime#42790 .
            using (var filestream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.CopyTo(filestream);
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
                Logger.LogError(ex, "Error setting hidden attribute on {path}", path);
            }
        }

        private void Save(BaseItem item, Stream stream)
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = Encoding.UTF8,
                CloseOutput = false
            };

            using (var writer = XmlWriter.Create(stream, settings))
            {
                var root = GetRootElementName(item);

                writer.WriteStartDocument(true);

                writer.WriteStartElement(root);

                var baseItem = item;

                if (baseItem != null)
                {
                    AddCommonNodes(baseItem, writer, LibraryManager);
                }

                WriteCustomElements(item, writer);

                writer.WriteEndElement();

                writer.WriteEndDocument();
            }
        }

        /// <summary>
        /// Write custom elements.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="writer">The xml writer.</param>
        protected abstract void WriteCustomElements(BaseItem item, XmlWriter writer);

        /// <summary>
        /// Adds the common nodes.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="writer">The xml writer.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        public static void AddCommonNodes(BaseItem item, XmlWriter writer, ILibraryManager libraryManager)
        {
            if (!string.IsNullOrEmpty(item.OfficialRating))
            {
                writer.WriteElementString("ContentRating", item.OfficialRating);
            }

            writer.WriteElementString("Added", item.DateCreated.ToLocalTime().ToString("G", CultureInfo.InvariantCulture));

            writer.WriteElementString("LockData", item.IsLocked.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());

            if (item.LockedFields.Length > 0)
            {
                writer.WriteElementString("LockedFields", string.Join("|", item.LockedFields));
            }

            if (item.CriticRating.HasValue)
            {
                writer.WriteElementString("CriticRating", item.CriticRating.Value.ToString(_usCulture));
            }

            if (!string.IsNullOrEmpty(item.Overview))
            {
                writer.WriteElementString("Overview", item.Overview);
            }

            if (!string.IsNullOrEmpty(item.OriginalTitle))
            {
                writer.WriteElementString("OriginalTitle", item.OriginalTitle);
            }

            if (!string.IsNullOrEmpty(item.CustomRating))
            {
                writer.WriteElementString("CustomRating", item.CustomRating);
            }

            if (!string.IsNullOrEmpty(item.Name) && !(item is Episode))
            {
                writer.WriteElementString("LocalTitle", item.Name);
            }

            var forcedSortName = item.ForcedSortName;
            if (!string.IsNullOrEmpty(forcedSortName))
            {
                writer.WriteElementString("SortTitle", forcedSortName);
            }

            if (item.PremiereDate.HasValue)
            {
                if (item is Person)
                {
                    writer.WriteElementString("BirthDate", item.PremiereDate.Value.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                }
                else if (!(item is Episode))
                {
                    writer.WriteElementString("PremiereDate", item.PremiereDate.Value.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                }
            }

            if (item.EndDate.HasValue)
            {
                if (item is Person)
                {
                    writer.WriteElementString("DeathDate", item.EndDate.Value.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                }
                else if (!(item is Episode))
                {
                    writer.WriteElementString("EndDate", item.EndDate.Value.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                }
            }

            if (item.RemoteTrailers.Count > 0)
            {
                writer.WriteStartElement("Trailers");

                foreach (var trailer in item.RemoteTrailers)
                {
                    writer.WriteElementString("Trailer", trailer.Url);
                }

                writer.WriteEndElement();
            }

            if (item.ProductionLocations.Length > 0)
            {
                writer.WriteStartElement("Countries");

                foreach (var name in item.ProductionLocations)
                {
                    writer.WriteElementString("Country", name);
                }

                writer.WriteEndElement();
            }

            var hasDisplayOrder = item as IHasDisplayOrder;
            if (hasDisplayOrder != null && !string.IsNullOrEmpty(hasDisplayOrder.DisplayOrder))
            {
                writer.WriteElementString("DisplayOrder", hasDisplayOrder.DisplayOrder);
            }

            if (item.CommunityRating.HasValue)
            {
                writer.WriteElementString("Rating", item.CommunityRating.Value.ToString(_usCulture));
            }

            if (item.ProductionYear.HasValue && !(item is Person))
            {
                writer.WriteElementString("ProductionYear", item.ProductionYear.Value.ToString(_usCulture));
            }

            var hasAspectRatio = item as IHasAspectRatio;
            if (hasAspectRatio != null)
            {
                if (!string.IsNullOrEmpty(hasAspectRatio.AspectRatio))
                {
                    writer.WriteElementString("AspectRatio", hasAspectRatio.AspectRatio);
                }
            }

            if (!string.IsNullOrEmpty(item.PreferredMetadataLanguage))
            {
                writer.WriteElementString("Language", item.PreferredMetadataLanguage);
            }

            if (!string.IsNullOrEmpty(item.PreferredMetadataCountryCode))
            {
                writer.WriteElementString("CountryCode", item.PreferredMetadataCountryCode);
            }

            // Use original runtime here, actual file runtime later in MediaInfo
            var runTimeTicks = item.RunTimeTicks;

            if (runTimeTicks.HasValue)
            {
                var timespan = TimeSpan.FromTicks(runTimeTicks!.Value);

                writer.WriteElementString("RunningTime", Math.Floor(timespan.TotalMinutes).ToString(_usCulture));
            }

            if (item.ProviderIds != null)
            {
                foreach (var providerKey in item.ProviderIds.Keys)
                {
                    var providerId = item.ProviderIds[providerKey];
                    if (!string.IsNullOrEmpty(providerId))
                    {
                        writer.WriteElementString(providerKey + "Id", providerId);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(item.Tagline))
            {
                writer.WriteStartElement("Taglines");
                writer.WriteElementString("Tagline", item.Tagline);
                writer.WriteEndElement();
            }

            if (item.Genres.Length > 0)
            {
                writer.WriteStartElement("Genres");

                foreach (var genre in item.Genres)
                {
                    writer.WriteElementString("Genre", genre);
                }

                writer.WriteEndElement();
            }

            if (item.Studios.Length > 0)
            {
                writer.WriteStartElement("Studios");

                foreach (var studio in item.Studios)
                {
                    writer.WriteElementString("Studio", studio);
                }

                writer.WriteEndElement();
            }

            if (item.Tags.Length > 0)
            {
                writer.WriteStartElement("Tags");

                foreach (var tag in item.Tags)
                {
                    writer.WriteElementString("Tag", tag);
                }

                writer.WriteEndElement();
            }

            var people = libraryManager.GetPeople(item);

            if (people.Count > 0)
            {
                writer.WriteStartElement("Persons");

                foreach (var person in people)
                {
                    writer.WriteStartElement("Person");
                    writer.WriteElementString("Name", person.Name);
                    writer.WriteElementString("Type", person.Type);
                    writer.WriteElementString("Role", person.Role);

                    if (person.SortOrder.HasValue)
                    {
                        writer.WriteElementString("SortOrder", person.SortOrder.Value.ToString(_usCulture));
                    }

                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            var boxset = item as BoxSet;
            if (boxset != null)
            {
                AddLinkedChildren(boxset, writer, "CollectionItems", "CollectionItem");
            }

            var playlist = item as Playlist;
            if (playlist != null && !Playlist.IsPlaylistFile(playlist.Path))
            {
                AddLinkedChildren(playlist, writer, "PlaylistItems", "PlaylistItem");
            }

            var hasShares = item as IHasShares;
            if (hasShares != null)
            {
                AddShares(hasShares, writer);
            }

            AddMediaInfo(item, writer);
        }

        /// <summary>
        /// Add shares.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="writer">The xml writer.</param>
        public static void AddShares(IHasShares item, XmlWriter writer)
        {
            writer.WriteStartElement("Shares");

            foreach (var share in item.Shares)
            {
                writer.WriteStartElement("Share");

                writer.WriteElementString("UserId", share.UserId);
                writer.WriteElementString(
                    "CanEdit",
                    share.CanEdit.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Appends the media info.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="writer">The xml writer.</param>
        /// <typeparam name="T">Type of item.</typeparam>
        public static void AddMediaInfo<T>(T item, XmlWriter writer)
            where T : BaseItem
        {
            if (item is Video video)
            {
                if (video.Video3DFormat.HasValue)
                {
                    switch (video.Video3DFormat.Value)
                    {
                        case Video3DFormat.FullSideBySide:
                            writer.WriteElementString("Format3D", "FSBS");
                            break;
                        case Video3DFormat.FullTopAndBottom:
                            writer.WriteElementString("Format3D", "FTAB");
                            break;
                        case Video3DFormat.HalfSideBySide:
                            writer.WriteElementString("Format3D", "HSBS");
                            break;
                        case Video3DFormat.HalfTopAndBottom:
                            writer.WriteElementString("Format3D", "HTAB");
                            break;
                        case Video3DFormat.MVC:
                            writer.WriteElementString("Format3D", "MVC");
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// ADd linked children.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="writer">The xml writer.</param>
        /// <param name="pluralNodeName">The plural node name.</param>
        /// <param name="singularNodeName">The singular node name.</param>
        public static void AddLinkedChildren(Folder item, XmlWriter writer, string pluralNodeName, string singularNodeName)
        {
            var items = item.LinkedChildren
                .Where(i => i.Type == LinkedChildType.Manual)
                .ToList();

            if (items.Count == 0)
            {
                return;
            }

            writer.WriteStartElement(pluralNodeName);

            foreach (var link in items)
            {
                if (!string.IsNullOrWhiteSpace(link.Path) || !string.IsNullOrWhiteSpace(link.LibraryItemId))
                {
                    writer.WriteStartElement(singularNodeName);
                    if (!string.IsNullOrWhiteSpace(link.Path))
                    {
                        writer.WriteElementString("Path", link.Path);
                    }

                    if (!string.IsNullOrWhiteSpace(link.LibraryItemId))
                    {
                        writer.WriteElementString("ItemId", link.LibraryItemId);
                    }

                    writer.WriteEndElement();
                }
            }

            writer.WriteEndElement();
        }

        private bool IsPersonType(PersonInfo person, string type)
        {
            return string.Equals(person.Type, type, StringComparison.OrdinalIgnoreCase) || string.Equals(person.Role, type, StringComparison.OrdinalIgnoreCase);
        }
    }
}
