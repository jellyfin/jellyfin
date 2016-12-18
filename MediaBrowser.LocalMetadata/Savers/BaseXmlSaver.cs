using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Xml;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Xml;

namespace MediaBrowser.LocalMetadata.Savers
{
    public abstract class BaseXmlSaver : IMetadataFileSaver
    {
        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        private static readonly Dictionary<string, string> CommonTags = new[] {

                    "Added",
                    "AspectRatio",
                    "AudioDbAlbumId",
                    "AudioDbArtistId",
                    "AwardSummary",
                    "BirthDate",
                    "Budget",
                    
                    // Deprecated. No longer saving in this field.
                    "certification",

                    "Chapters",
                    "ContentRating",
                    "Countries",
                    "CustomRating",
                    "CriticRating",
                    "CriticRatingSummary",
                    "DeathDate",
                    "DisplayOrder",
                    "EndDate",
                    "Genres",
                    "Genre",
                    "GamesDbId",
                    
                    // Deprecated. No longer saving in this field.
                    "IMDB_ID",

                    "IMDB",
                    
                    // Deprecated. No longer saving in this field.
                    "IMDbId",

                    "Language",
                    "LocalTitle",
                    "OriginalTitle",
                    "LockData",
                    "LockedFields",
                    "Format3D",
                    "Metascore",
                    
                    // Deprecated. No longer saving in this field.
                    "MPAARating",

                    "MPAADescription",

                    "MusicBrainzArtistId",
                    "MusicBrainzAlbumArtistId",
                    "MusicBrainzAlbumId",
                    "MusicBrainzReleaseGroupId",

                    // Deprecated. No longer saving in this field.
                    "MusicbrainzId",

                    "Overview",
                    "ShortOverview",
                    "Persons",
                    "PlotKeywords",
                    "PremiereDate",
                    "ProductionYear",
                    "Rating",
                    "Revenue",
                    "RottenTomatoesId",
                    "RunningTime",
                    
                    // Deprecated. No longer saving in this field.
                    "Runtime",

                    "SortTitle",
                    "Studios",
                    "Tags",
                    
                    // Deprecated. No longer saving in this field.
                    "TagLine",

                    "Taglines",
                    "TMDbCollectionId",
                    "TMDbId",

                    // Deprecated. No longer saving in this field.
                    "Trailer",

                    "Trailers",
                    "TVcomId",
                    "TvDbId",
                    "Type",
                    "TVRageId",
                    "VoteCount",
                    "Website",
                    "Zap2ItId",
                    "CollectionItems",
                    "PlaylistItems",
                    "Shares"

        }.ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);

        public BaseXmlSaver(IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataManager, ILogger logger, IXmlReaderSettingsFactory xmlReaderSettingsFactory)
        {
            FileSystem = fileSystem;
            ConfigurationManager = configurationManager;
            LibraryManager = libraryManager;
            UserManager = userManager;
            UserDataManager = userDataManager;
            Logger = logger;
            XmlReaderSettingsFactory = xmlReaderSettingsFactory;
        }

        protected IFileSystem FileSystem { get; private set; }
        protected IServerConfigurationManager ConfigurationManager { get; private set; }
        protected ILibraryManager LibraryManager { get; private set; }
        protected IUserManager UserManager { get; private set; }
        protected IUserDataManager UserDataManager { get; private set; }
        protected ILogger Logger { get; private set; }
        protected IXmlReaderSettingsFactory XmlReaderSettingsFactory { get; private set; }

        protected ItemUpdateType MinimumUpdateType
        {
            get
            {
                return ItemUpdateType.MetadataDownload;
            }
        }

        public string Name
        {
            get
            {
                return XmlProviderUtils.Name;
            }
        }

        public string GetSavePath(IHasMetadata item)
        {
            return GetLocalSavePath(item);
        }

        /// <summary>
        /// Gets the save path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        protected abstract string GetLocalSavePath(IHasMetadata item);

        /// <summary>
        /// Gets the name of the root element.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        protected virtual string GetRootElementName(IHasMetadata item)
        {
            return "Item";
        }

        /// <summary>
        /// Determines whether [is enabled for] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="updateType">Type of the update.</param>
        /// <returns><c>true</c> if [is enabled for] [the specified item]; otherwise, <c>false</c>.</returns>
        public abstract bool IsEnabledFor(IHasMetadata item, ItemUpdateType updateType);

        protected virtual List<string> GetTagsUsed()
        {
            return new List<string>();
        }

        public void Save(IHasMetadata item, CancellationToken cancellationToken)
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
            FileSystem.CreateDirectory(Path.GetDirectoryName(path));

            var file = FileSystem.GetFileInfo(path);

            var wasHidden = false;

            // This will fail if the file is hidden
            if (file.Exists)
            {
                if (file.IsHidden)
                {
                    FileSystem.SetHidden(path, false);
                    wasHidden = true;
                }
                if (file.IsReadOnly)
                {
                    FileSystem.SetReadOnly(path, false);
                }
            }

            using (var filestream = FileSystem.GetFileStream(path, FileOpenMode.Create, FileAccessMode.Write, FileShareMode.Read))
            {
                stream.CopyTo(filestream);
            }

            if (wasHidden || ConfigurationManager.Configuration.SaveMetadataHidden)
            {
                FileSystem.SetHidden(path, true);
            }
        }

        private void Save(IHasMetadata item, Stream stream, string xmlPath)
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = Encoding.UTF8,
                CloseOutput = false
            };

            using (XmlWriter writer = XmlWriter.Create(stream, settings))
            {
                var root = GetRootElementName(item);

                writer.WriteStartDocument(true);

                writer.WriteStartElement(root);

                var baseItem = item as BaseItem;

                if (baseItem != null)
                {
                    AddCommonNodes(baseItem, writer, LibraryManager, UserManager, UserDataManager, FileSystem, ConfigurationManager);
                }

                WriteCustomElements(item, writer);

                var tagsUsed = GetTagsUsed();

                try
                {
                    AddCustomTags(xmlPath, tagsUsed, writer, Logger, FileSystem);
                }
                catch (FileNotFoundException)
                {

                }
                catch (IOException)
                {

                }
                catch (XmlException ex)
                {
                    Logger.ErrorException("Error reading existng xml", ex);
                }

                writer.WriteEndElement();

                writer.WriteEndDocument();
            }
        }

        protected abstract void WriteCustomElements(IHasMetadata item, XmlWriter writer);

        public const string DateAddedFormat = "yyyy-MM-dd HH:mm:ss";

        /// <summary>
        /// Adds the common nodes.
        /// </summary>
        /// <returns>Task.</returns>
        public static void AddCommonNodes(BaseItem item, XmlWriter writer, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataRepo, IFileSystem fileSystem, IServerConfigurationManager config)
        {
            var writtenProviderIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrEmpty(item.OfficialRating))
            {
                writer.WriteElementString("ContentRating", item.OfficialRating);
            }

            if (!string.IsNullOrEmpty(item.OfficialRatingDescription))
            {
                writer.WriteElementString("MPAADescription", item.OfficialRatingDescription);
            }

            writer.WriteElementString("Added", item.DateCreated.ToLocalTime().ToString("G"));

            writer.WriteElementString("LockData", item.IsLocked.ToString().ToLower());

            if (item.LockedFields.Count > 0)
            {
                writer.WriteElementString("LockedFields", string.Join("|", item.LockedFields.Select(i => i.ToString()).ToArray()));
            }

            if (!string.IsNullOrEmpty(item.DisplayMediaType))
            {
                writer.WriteElementString("Type", item.DisplayMediaType);
            }

            if (item.CriticRating.HasValue)
            {
                writer.WriteElementString("CriticRating", item.CriticRating.Value.ToString(UsCulture));
            }

            if (!string.IsNullOrEmpty(item.CriticRatingSummary))
            {
                writer.WriteElementString("CriticRatingSummary", item.CriticRatingSummary);
            }

            if (!string.IsNullOrEmpty(item.Overview))
            {
                writer.WriteElementString("Overview", item.Overview);
            }

            if (!string.IsNullOrEmpty(item.OriginalTitle))
            {
                writer.WriteElementString("OriginalTitle", item.OriginalTitle);
            }
            if (!string.IsNullOrEmpty(item.ShortOverview))
            {
                writer.WriteElementString("ShortOverview", item.ShortOverview);
            }
            if (!string.IsNullOrEmpty(item.CustomRating))
            {
                writer.WriteElementString("CustomRating", item.CustomRating);
            }

            if (!string.IsNullOrEmpty(item.Name) && !(item is Episode))
            {
                writer.WriteElementString("LocalTitle", item.Name);
            }

            if (!string.IsNullOrEmpty(item.ForcedSortName))
            {
                writer.WriteElementString("SortTitle", item.ForcedSortName);
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

            var hasTrailers = item as IHasTrailers;
            if (hasTrailers != null)
            {
                if (hasTrailers.RemoteTrailers.Count > 0)
                {
                    writer.WriteStartElement("Trailers");

                    foreach (var trailer in hasTrailers.RemoteTrailers)
                    {
                        writer.WriteElementString("Trailer", trailer.Url);
                    }

                    writer.WriteEndElement();
                }
            }

            if (item.ProductionLocations.Count > 0)
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

            var hasMetascore = item as IHasMetascore;
            if (hasMetascore != null && hasMetascore.Metascore.HasValue)
            {
                writer.WriteElementString("Metascore", hasMetascore.Metascore.Value.ToString(UsCulture));
            }

            var hasAwards = item as IHasAwards;
            if (hasAwards != null && !string.IsNullOrEmpty(hasAwards.AwardSummary))
            {
                writer.WriteElementString("AwardSummary", hasAwards.AwardSummary);
            }

            var hasBudget = item as IHasBudget;
            if (hasBudget != null)
            {
                if (hasBudget.Budget.HasValue)
                {
                    writer.WriteElementString("Budget", hasBudget.Budget.Value.ToString(UsCulture));
                }

                if (hasBudget.Revenue.HasValue)
                {
                    writer.WriteElementString("Revenue", hasBudget.Revenue.Value.ToString(UsCulture));
                }
            }

            if (item.CommunityRating.HasValue)
            {
                writer.WriteElementString("Rating", item.CommunityRating.Value.ToString(UsCulture));
            }
            if (item.VoteCount.HasValue)
            {
                writer.WriteElementString("VoteCount", item.VoteCount.Value.ToString(UsCulture));
            }

            if (item.ProductionYear.HasValue && !(item is Person))
            {
                writer.WriteElementString("ProductionYear", item.ProductionYear.Value.ToString(UsCulture));
            }

            if (!string.IsNullOrEmpty(item.HomePageUrl))
            {
                writer.WriteElementString("Website", item.HomePageUrl);
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

                writer.WriteElementString("RunningTime", Convert.ToInt32(timespan.TotalMinutes).ToString(UsCulture));
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

            if (item.Genres.Count > 0)
            {
                writer.WriteStartElement("Genres");

                foreach (var genre in item.Genres)
                {
                    writer.WriteElementString("Genre", genre);
                }

                writer.WriteEndElement();
            }

            if (item.Studios.Count > 0)
            {
                writer.WriteStartElement("Studios");

                foreach (var studio in item.Studios)
                {
                    writer.WriteElementString("Studio", studio);
                }

                writer.WriteEndElement();
            }

            if (item.Tags.Count > 0)
            {
                writer.WriteStartElement("Tags");

                foreach (var tag in item.Tags)
                {
                    writer.WriteElementString("Tag", tag);
                }

                writer.WriteEndElement();
            }

            if (item.Keywords.Count > 0)
            {
                writer.WriteStartElement("PlotKeywords");

                foreach (var tag in item.Keywords)
                {
                    writer.WriteElementString("PlotKeyword", tag);
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
            if (playlist != null)
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
                writer.WriteElementString("CanEdit", share.CanEdit.ToString().ToLower());

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
                if (!string.IsNullOrWhiteSpace(link.Path))
                {
                    writer.WriteStartElement(singularNodeName);
                    writer.WriteElementString("Path", link.Path);
                    writer.WriteEndElement();
                }
            }

            writer.WriteEndElement();
        }

        private static bool IsPersonType(PersonInfo person, string type)
        {
            return string.Equals(person.Type, type, StringComparison.OrdinalIgnoreCase) || string.Equals(person.Role, type, StringComparison.OrdinalIgnoreCase);
        }

        private void AddCustomTags(string path, List<string> xmlTagsUsed, XmlWriter writer, ILogger logger, IFileSystem fileSystem)
        {
            var settings = XmlReaderSettingsFactory.Create(false);

            settings.CheckCharacters = false;
            settings.IgnoreProcessingInstructions = true;
            settings.IgnoreComments = true;

            using (var fileStream = fileSystem.OpenRead(path))
            {
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                {
                    // Use XmlReader for best performance
                    using (var reader = XmlReader.Create(streamReader, settings))
                    {
                        try
                        {
                            reader.MoveToContent();
                        }
                        catch (Exception ex)
                        {
                            logger.ErrorException("Error reading existing xml tags from {0}.", ex, path);
                            return;
                        }

                        reader.Read();

                        // Loop through each element
                        while (!reader.EOF && reader.ReadState == ReadState.Interactive)
                        {
                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                var name = reader.Name;

                                if (!CommonTags.ContainsKey(name) && !xmlTagsUsed.Contains(name, StringComparer.OrdinalIgnoreCase))
                                {
                                    writer.WriteNode(reader, false);
                                }
                                else
                                {
                                    reader.Skip();
                                }
                            }
                            else
                            {
                                reader.Read();
                            }
                        }
                    }
                }
            }
        }
    }
}
