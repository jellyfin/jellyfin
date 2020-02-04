using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
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
    public abstract class BaseXmlSaver : IMetadataFileSaver
    {
        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        public BaseXmlSaver(IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataManager, ILogger logger)
        {
            FileSystem = fileSystem;
            ConfigurationManager = configurationManager;
            LibraryManager = libraryManager;
            UserManager = userManager;
            UserDataManager = userDataManager;
            Logger = logger;
        }

        protected IFileSystem FileSystem { get; private set; }
        protected IServerConfigurationManager ConfigurationManager { get; private set; }
        protected ILibraryManager LibraryManager { get; private set; }
        protected IUserManager UserManager { get; private set; }
        protected IUserDataManager UserDataManager { get; private set; }
        protected ILogger Logger { get; private set; }

        public string Name => XmlProviderUtils.Name;

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

        public void Save(BaseItem item, CancellationToken cancellationToken)
        {
            var path = GetSavePath(item);

            using (var memoryStream = new MemoryStream())
            {
                Save(item, memoryStream, path);

                memoryStream.Position = 0;

                cancellationToken.ThrowIfCancellationRequested();

                SaveToFile(memoryStream, path);
            }
        }

        private void SaveToFile(Stream stream, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            // On Windows, savint the file will fail if the file is hidden or readonly
            FileSystem.SetAttributes(path, false, false);

            using (var filestream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
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

        private void Save(BaseItem item, Stream stream, string xmlPath)
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
                    AddCommonNodes(baseItem, writer, LibraryManager, UserManager, UserDataManager, FileSystem, ConfigurationManager);
                }

                WriteCustomElements(item, writer);

                writer.WriteEndElement();

                writer.WriteEndDocument();
            }
        }

        protected abstract void WriteCustomElements(BaseItem item, XmlWriter writer);

        public const string DateAddedFormat = "yyyy-MM-dd HH:mm:ss";

        /// <summary>
        /// Adds the common nodes.
        /// </summary>
        /// <returns>Task.</returns>
        public static void AddCommonNodes(BaseItem item, XmlWriter writer, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataRepo, IFileSystem fileSystem, IServerConfigurationManager config)
        {
            if (!string.IsNullOrEmpty(item.OfficialRating))
            {
                writer.WriteElementString("ContentRating", item.OfficialRating);
            }

            writer.WriteElementString("Added", item.DateCreated.ToLocalTime().ToString("G"));

            writer.WriteElementString("LockData", item.IsLocked.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());

            if (item.LockedFields.Length > 0)
            {
                writer.WriteElementString("LockedFields", string.Join("|", item.LockedFields));
            }

            if (item.CriticRating.HasValue)
            {
                writer.WriteElementString("CriticRating", item.CriticRating.Value.ToString(UsCulture));
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
                    writer.WriteElementString("BirthDate", item.PremiereDate.Value.ToLocalTime().ToString("yyyy-MM-dd"));
                }
                else if (!(item is Episode))
                {
                    writer.WriteElementString("PremiereDate", item.PremiereDate.Value.ToLocalTime().ToString("yyyy-MM-dd"));
                }
            }

            if (item.EndDate.HasValue)
            {
                if (item is Person)
                {
                    writer.WriteElementString("DeathDate", item.EndDate.Value.ToLocalTime().ToString("yyyy-MM-dd"));
                }
                else if (!(item is Episode))
                {
                    writer.WriteElementString("EndDate", item.EndDate.Value.ToLocalTime().ToString("yyyy-MM-dd"));
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
                writer.WriteElementString("Rating", item.CommunityRating.Value.ToString(UsCulture));
            }

            if (item.ProductionYear.HasValue && !(item is Person))
            {
                writer.WriteElementString("ProductionYear", item.ProductionYear.Value.ToString(UsCulture));
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
                var timespan = TimeSpan.FromTicks(runTimeTicks.Value);

                writer.WriteElementString("RunningTime", Math.Floor(timespan.TotalMinutes).ToString(UsCulture));
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
                        writer.WriteElementString("SortOrder", person.SortOrder.Value.ToString(UsCulture));
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
        /// <typeparam name="T"></typeparam>
        public static void AddMediaInfo<T>(T item, XmlWriter writer)
            where T : BaseItem
        {
            var video = item as Video;

            if (video != null)
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
