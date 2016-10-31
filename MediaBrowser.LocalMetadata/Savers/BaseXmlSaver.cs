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

            //var hasTrailers = item as IHasTrailers;
            //if (hasTrailers != null)
            //{
            //    if (hasTrailers.RemoteTrailers.Count > 0)
            //    {
            //        builder.Append("<Trailers>");

            //        foreach (var trailer in hasTrailers.RemoteTrailers)
            //        {
            //            builder.Append("<Trailer>" + SecurityElement.Escape(trailer.Url) + "</Trailer>");
            //        }

            //        builder.Append("</Trailers>");
            //    }
            //}

            ////if (hasProductionLocations.ProductionLocations.Count > 0)
            ////{
            ////    builder.Append("<Countries>");

            ////    foreach (var name in hasProductionLocations.ProductionLocations)
            ////    {
            ////        builder.Append("<Country>" + SecurityElement.Escape(name) + "</Country>");
            ////    }

            ////    builder.Append("</Countries>");
            ////}

            //var hasDisplayOrder = item as IHasDisplayOrder;
            //if (hasDisplayOrder != null && !string.IsNullOrEmpty(hasDisplayOrder.DisplayOrder))
            //{
            //    builder.Append("<DisplayOrder>" + SecurityElement.Escape(hasDisplayOrder.DisplayOrder) + "</DisplayOrder>");
            //}

            //var hasMetascore = item as IHasMetascore;
            //if (hasMetascore != null && hasMetascore.Metascore.HasValue)
            //{
            //    builder.Append("<Metascore>" + SecurityElement.Escape(hasMetascore.Metascore.Value.ToString(UsCulture)) + "</Metascore>");
            //}

            //var hasAwards = item as IHasAwards;
            //if (hasAwards != null && !string.IsNullOrEmpty(hasAwards.AwardSummary))
            //{
            //    builder.Append("<AwardSummary>" + SecurityElement.Escape(hasAwards.AwardSummary) + "</AwardSummary>");
            //}

            //var hasBudget = item as IHasBudget;
            //if (hasBudget != null)
            //{
            //    if (hasBudget.Budget.HasValue)
            //    {
            //        builder.Append("<Budget>" + SecurityElement.Escape(hasBudget.Budget.Value.ToString(UsCulture)) + "</Budget>");
            //    }

            //    if (hasBudget.Revenue.HasValue)
            //    {
            //        builder.Append("<Revenue>" + SecurityElement.Escape(hasBudget.Revenue.Value.ToString(UsCulture)) + "</Revenue>");
            //    }
            //}

            //if (item.CommunityRating.HasValue)
            //{
            //    builder.Append("<Rating>" + SecurityElement.Escape(item.CommunityRating.Value.ToString(UsCulture)) + "</Rating>");
            //}
            //if (item.VoteCount.HasValue)
            //{
            //    builder.Append("<VoteCount>" + SecurityElement.Escape(item.VoteCount.Value.ToString(UsCulture)) + "</VoteCount>");
            //}

            //if (item.ProductionYear.HasValue && !(item is Person))
            //{
            //    builder.Append("<ProductionYear>" + SecurityElement.Escape(item.ProductionYear.Value.ToString(UsCulture)) + "</ProductionYear>");
            //}

            //if (!string.IsNullOrEmpty(item.HomePageUrl))
            //{
            //    builder.Append("<Website>" + SecurityElement.Escape(item.HomePageUrl) + "</Website>");
            //}

            //var hasAspectRatio = item as IHasAspectRatio;
            //if (hasAspectRatio != null)
            //{
            //    if (!string.IsNullOrEmpty(hasAspectRatio.AspectRatio))
            //    {
            //        builder.Append("<AspectRatio>" + SecurityElement.Escape(hasAspectRatio.AspectRatio) + "</AspectRatio>");
            //    }
            //}

            //if (!string.IsNullOrEmpty(item.PreferredMetadataLanguage))
            //{
            //    builder.Append("<Language>" + SecurityElement.Escape(item.PreferredMetadataLanguage) + "</Language>");
            //}
            //if (!string.IsNullOrEmpty(item.PreferredMetadataCountryCode))
            //{
            //    builder.Append("<CountryCode>" + SecurityElement.Escape(item.PreferredMetadataCountryCode) + "</CountryCode>");
            //}

            //// Use original runtime here, actual file runtime later in MediaInfo
            //var runTimeTicks = item.RunTimeTicks;

            //if (runTimeTicks.HasValue)
            //{
            //    var timespan = TimeSpan.FromTicks(runTimeTicks.Value);

            //    builder.Append("<RunningTime>" + Convert.ToInt32(timespan.TotalMinutes).ToString(UsCulture) + "</RunningTime>");
            //}

            //if (item.ProviderIds != null)
            //{
            //    foreach (var providerKey in item.ProviderIds.Keys)
            //    {
            //        var providerId = item.ProviderIds[providerKey];
            //        if (!string.IsNullOrEmpty(providerId))
            //        {
            //            builder.Append(string.Format("<{0}>{1}</{0}>", providerKey + "Id", SecurityElement.Escape(providerId)));
            //        }
            //    }
            //}

            //if (!string.IsNullOrWhiteSpace(item.Tagline))
            //{
            //    builder.Append("<Taglines>");
            //    builder.Append("<Tagline>" + SecurityElement.Escape(item.Tagline) + "</Tagline>");
            //    builder.Append("</Taglines>");
            //}

            //if (item.Genres.Count > 0)
            //{
            //    builder.Append("<Genres>");

            //    foreach (var genre in item.Genres)
            //    {
            //        builder.Append("<Genre>" + SecurityElement.Escape(genre) + "</Genre>");
            //    }

            //    builder.Append("</Genres>");
            //}

            //if (item.Studios.Count > 0)
            //{
            //    builder.Append("<Studios>");

            //    foreach (var studio in item.Studios)
            //    {
            //        builder.Append("<Studio>" + SecurityElement.Escape(studio) + "</Studio>");
            //    }

            //    builder.Append("</Studios>");
            //}

            //if (item.Tags.Count > 0)
            //{
            //    builder.Append("<Tags>");

            //    foreach (var tag in item.Tags)
            //    {
            //        builder.Append("<Tag>" + SecurityElement.Escape(tag) + "</Tag>");
            //    }

            //    builder.Append("</Tags>");
            //}

            //if (item.Keywords.Count > 0)
            //{
            //    builder.Append("<PlotKeywords>");

            //    foreach (var tag in item.Keywords)
            //    {
            //        builder.Append("<PlotKeyword>" + SecurityElement.Escape(tag) + "</PlotKeyword>");
            //    }

            //    builder.Append("</PlotKeywords>");
            //}

            //var people = libraryManager.GetPeople(item);

            //if (people.Count > 0)
            //{
            //    builder.Append("<Persons>");

            //    foreach (var person in people)
            //    {
            //        builder.Append("<Person>");
            //        builder.Append("<Name>" + SecurityElement.Escape(person.Name) + "</Name>");
            //        builder.Append("<Type>" + SecurityElement.Escape(person.Type) + "</Type>");
            //        builder.Append("<Role>" + SecurityElement.Escape(person.Role) + "</Role>");

            //        if (person.SortOrder.HasValue)
            //        {
            //            builder.Append("<SortOrder>" + SecurityElement.Escape(person.SortOrder.Value.ToString(UsCulture)) + "</SortOrder>");
            //        }

            //        builder.Append("</Person>");
            //    }

            //    builder.Append("</Persons>");
            //}

            //var boxset = item as BoxSet;
            //if (boxset != null)
            //{
            //    AddLinkedChildren(boxset, builder, "CollectionItems", "CollectionItem");
            //}

            //var playlist = item as Playlist;
            //if (playlist != null)
            //{
            //    AddLinkedChildren(playlist, builder, "PlaylistItems", "PlaylistItem");
            //}

            //var hasShares = item as IHasShares;
            //if (hasShares != null)
            //{
            //    AddShares(hasShares, builder);
            //}

            AddMediaInfo(item, writer);
        }

        public static void AddShares(IHasShares item, StringBuilder builder)
        {
            //builder.Append("<Shares>");

            //foreach (var share in item.Shares)
            //{
            //    builder.Append("<Share>");

            //    builder.Append("<UserId>" + SecurityElement.Escape(share.UserId) + "</UserId>");
            //    builder.Append("<CanEdit>" + SecurityElement.Escape(share.CanEdit.ToString().ToLower()) + "</CanEdit>");

            //    builder.Append("</Share>");
            //}

            //builder.Append("</Shares>");
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

        public static void AddLinkedChildren(Folder item, StringBuilder builder, string pluralNodeName, string singularNodeName)
        {
            //var items = item.LinkedChildren
            //    .Where(i => i.Type == LinkedChildType.Manual)
            //    .ToList();

            //if (items.Count == 0)
            //{
            //    return;
            //}

            //builder.Append("<" + pluralNodeName + ">");
            //foreach (var link in items)
            //{
            //    builder.Append("<" + singularNodeName + ">");

            //    if (!string.IsNullOrWhiteSpace(link.Path))
            //    {
            //        builder.Append("<Path>" + SecurityElement.Escape((link.Path)) + "</Path>");
            //    }

            //    builder.Append("</" + singularNodeName + ">");
            //}
            //builder.Append("</" + pluralNodeName + ">");
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

                        // Loop through each element
                        while (reader.Read())
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
                        }
                    }
                }
            }
        }
    }
}
