#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Providers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.XbmcMetadata.Configuration;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.XbmcMetadata.Parsers
{
    public class BaseNfoParser<T>
        where T : BaseItem
    {
        private readonly IConfigurationManager _config;
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataManager;
        private readonly Dictionary<string, string> _validProviderIds;
        private readonly IProviderManager _providerManager;
        private readonly IDirectoryService _directoryService;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseNfoParser{T}" /> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        /// <param name="config">Instance of the <see cref="IConfigurationManager"/> interface.</param>
        /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="userDataManager">Instance of the <see cref="IUserDataManager"/> interface.</param>
        /// <param name="directoryService">Instance of the <see cref="IDirectoryService"/> interface.</param>
        public BaseNfoParser(
            ILogger logger,
            IConfigurationManager config,
            IProviderManager providerManager,
            IUserManager userManager,
            IUserDataManager userDataManager,
            IDirectoryService directoryService)
        {
            Logger = logger;
            _config = config;
            _providerManager = providerManager;
            _validProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _userManager = userManager;
            _userDataManager = userDataManager;
            _directoryService = directoryService;
        }

        protected CultureInfo UsCulture { get; } = new CultureInfo("en-US");

        /// <summary>
        /// Gets the logger.
        /// </summary>
        protected ILogger Logger { get; }

        protected virtual bool SupportsUrlAfterClosingXmlTag => false;

        /// <summary>
        /// Fetches metadata for an item from one xml file.
        /// </summary>
        /// <param name="metadataResult">The metadata result.</param>
        /// <param name="nfoPath">The nfo path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException"><c>metadataResult</c> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><c>nfoPath</c> is <c>null</c> or empty.</exception>
        public void Fetch(MetadataResult<T> metadataResult, string nfoPath, CancellationToken cancellationToken)
        {
            if (metadataResult.Item == null)
            {
                throw new ArgumentException("Item can't be null.", nameof(metadataResult));
            }

            if (string.IsNullOrEmpty(nfoPath))
            {
                throw new ArgumentException("The metadata filepath was empty.", nameof(nfoPath));
            }

            CreateProviderIdMappings(metadataResult.Item);

            Fetch(metadataResult, nfoPath, GetXmlReaderSettings(), cancellationToken);
        }

        /// <summary>
        /// Fetches the specified item.
        /// </summary>
        /// <param name="metadataResult">The metadata result.</param>
        /// <param name="nfoPath">The nfo path.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        protected virtual void Fetch(MetadataResult<T> metadataResult, string nfoPath, XmlReaderSettings settings, CancellationToken cancellationToken)
        {
            if (!SupportsUrlAfterClosingXmlTag)
            {
                using (var fileStream = File.OpenRead(nfoPath))
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                using (var reader = XmlReader.Create(streamReader, settings))
                {
                    metadataResult.ResetPeople();

                    reader.MoveToContent();
                    reader.Read();

                    // Loop through each element
                    while (!reader.EOF && reader.ReadState == ReadState.Interactive)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            FetchDataFromXmlNode(reader, metadataResult);
                        }
                        else
                        {
                            reader.Read();
                        }
                    }
                }

                return;
            }

            using (var fileStream = File.OpenRead(nfoPath))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
            {
                metadataResult.ResetPeople();

                // Need to handle a url after the xml data
                // http://kodi.wiki/view/NFO_files/movies

                var xml = streamReader.ReadToEnd();

                // Find last closing Tag
                // Need to do this in two steps to account for random > characters after the closing xml
                var index = xml.LastIndexOf(@"</", StringComparison.Ordinal);

                // If closing tag exists, move to end of Tag
                if (index != -1)
                {
                    index = xml.IndexOf('>', index);
                }

                if (index != -1)
                {
                    var endingXml = xml.Substring(index);

                    ParseProviderLinks(metadataResult.Item, endingXml);

                    // If the file is just an imdb url, don't go any further
                    if (index == 0)
                    {
                        return;
                    }

                    xml = xml.Substring(0, index + 1);
                }
                else
                {
                    // If the file is just provider urls, handle that
                    ParseProviderLinks(metadataResult.Item, xml);

                    return;
                }

                // These are not going to be valid xml so no sense in causing the provider to fail and spamming the log with exceptions
                try
                {
                    using (var stringReader = new StringReader(xml))
                    using (var reader = XmlReader.Create(stringReader, settings))
                    {
                        reader.MoveToContent();
                        reader.Read();

                        // Loop through each element
                        while (!reader.EOF && reader.ReadState == ReadState.Interactive)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                FetchDataFromXmlNode(reader, metadataResult);
                            }
                            else
                            {
                                reader.Read();
                            }
                        }
                    }
                }
                catch (XmlException)
                {
                }
            }
        }

        protected void ParseProviderLinks(T item, string xml)
        {
            if (ProviderIdParsers.TryFindImdbId(xml, out var imdbId))
            {
                item.SetProviderId(MetadataProvider.Imdb, imdbId.ToString());
            }

            if (item is Movie)
            {
                if (ProviderIdParsers.TryFindTmdbMovieId(xml, out var tmdbId))
                {
                    item.SetProviderId(MetadataProvider.Tmdb, tmdbId.ToString());
                }
            }

            if (item is Series)
            {
                if (ProviderIdParsers.TryFindTmdbSeriesId(xml, out var tmdbId))
                {
                    item.SetProviderId(MetadataProvider.Tmdb, tmdbId.ToString());
                }

                if (ProviderIdParsers.TryFindTvdbId(xml, out var tvdbId))
                {
                    item.SetProviderId(MetadataProvider.Tvdb, tvdbId.ToString());
                }
            }
        }

        protected virtual void FetchDataFromXmlNode(XmlReader reader, MetadataResult<T> itemResult)
        {
            var item = itemResult.Item;

            var nfoConfiguration = _config.GetNfoConfiguration();
            UserItemData? userData = null;
            if (!string.IsNullOrWhiteSpace(nfoConfiguration.UserId))
            {
                var user = _userManager.GetUserById(Guid.Parse(nfoConfiguration.UserId));
                userData = _userDataManager.GetUserData(user, item);
            }

            switch (reader.Name)
            {
                case "dateadded":
                    item.DateCreated = reader.ReadDateFromNfo() ?? item.DateCreated;
                    break;

                case "originaltitle":
                    item.OriginalTitle = reader.ReadStringFromNfo() ?? item.OriginalTitle;
                    break;

                case "name":
                case "title":
                case "localtitle":
                    item.Name = reader.ReadStringFromNfo() ?? item.Name;
                    break;

                case "sortname":
                    item.SortName = reader.ReadStringFromNfo() ?? item.SortName;
                    break;

                case "criticrating":
                    item.CriticRating = reader.ReadFloatFromNfo() ?? item.CriticRating;
                    break;

                case "sorttitle":
                    item.SortName = reader.ReadStringFromNfo() ?? item.SortName;
                    break;

                case "biography":
                case "plot":
                case "review":
                    item.Overview = reader.ReadStringFromNfo() ?? item.Overview;
                    break;

                case "language":
                    item.PreferredMetadataLanguage = reader.ReadStringFromNfo() ?? item.PreferredMetadataLanguage;
                    break;

                case "watched":
                    if (userData != null)
                    {
                        userData.Played = reader.ReadBoolFromNfo() ?? userData.Played;
                    }

                    reader.Read();
                    break;

                case "playcount":
                    if (userData != null)
                    {
                        userData.PlayCount = reader.ReadIntFromNfo() ?? userData.PlayCount;
                    }

                    reader.Read();
                    break;

                case "lastplayed":
                    if (userData != null)
                    {
                        userData.LastPlayedDate = reader.ReadDateFromNfo() ?? userData.LastPlayedDate;
                    }

                    reader.Read();
                    break;

                case "countrycode":
                    item.PreferredMetadataCountryCode = reader.ReadStringFromNfo() ?? item.PreferredMetadataCountryCode;
                    break;

                case "lockedfields":
                    var locked = reader.ReadStringFromNfo() ?? string.Empty;
                    item.LockedFields = NfoParserHelpers.ParseLockedFields(locked);
                    break;

                case "tagline":
                    item.Tagline = reader.ReadStringFromNfo() ?? item.Tagline;
                    break;

                case "country":
                    item.ProductionLocations = reader.ReadStringArrayFromNfo();
                    break;

                case "mpaa":
                    item.OfficialRating = reader.ReadStringFromNfo() ?? item.OfficialRating;
                    break;

                case "customrating":
                    item.CustomRating = reader.ReadStringFromNfo() ?? item.CustomRating;
                    break;

                case "runtime":
                    item.RunTimeTicks = TimeSpan.FromMinutes(reader.ReadIntFromNfo() ?? 0.0).Ticks;
                    break;

                case "aspectratio":
                    if (item is IHasAspectRatio hasAspectRatio)
                    {
                        hasAspectRatio.AspectRatio = reader.ReadStringFromNfo() ?? hasAspectRatio.AspectRatio;
                    }

                    break;

                case "lockdata":
                    item.IsLocked = reader.ReadBoolFromNfo() ?? item.IsLocked;
                    break;

                case "studio":
                    var studio = reader.ReadStringFromNfo();
                    if (!string.IsNullOrWhiteSpace(studio))
                    {
                        item.AddStudio(studio);
                    }

                    break;

                case "director":
                    NfoSubtreeParsers<T>.ReadPersonInfoFromNfo(reader, itemResult, PersonType.Director);
                    break;

                case "credits":
                    NfoSubtreeParsers<T>.ReadPersonInfoFromNfo(reader, itemResult, PersonType.Writer);
                    break;

                case "writer":
                    NfoSubtreeParsers<T>.ReadPersonInfoFromNfo(reader, itemResult, PersonType.Writer);
                    break;

                case "actor":
                    NfoSubtreeParsers<T>.ReadActorNode(reader, itemResult);
                    break;

                case "trailer":
                    var parsed = reader.ReadTrailerUrlFromNfo();
                    if (string.IsNullOrWhiteSpace(parsed))
                    {
                        break;
                    }

                    item.AddTrailerUrl(parsed);
                    break;

                case "displayorder":
                    if (item is IHasDisplayOrder hasDisplayOrder)
                    {
                        hasDisplayOrder.DisplayOrder = reader.ReadStringFromNfo() ?? hasDisplayOrder.DisplayOrder;
                    }

                    break;

                case "year":
                    item.ProductionYear = reader.ReadIntFromNfo() ?? item.ProductionYear;
                    break;

                case "rating":
                    item.CommunityRating = reader.ReadFloatFromNfo() ?? item.CommunityRating;
                    break;

                case "aired":
                case "formed":
                case "premiered":
                case "releasedate":
                    item.PremiereDate = reader.ReadDateFromNfo() ?? item.PremiereDate;
                    item.ProductionYear = item.PremiereDate?.Year ?? item.ProductionYear;
                    break;

                case "enddate":
                    item.EndDate = reader.ReadDateFromNfo() ?? item.EndDate;
                    break;

                case "genre":
                    var genres = reader.ReadStringArrayFromNfo();
                    foreach (var genre in genres)
                    {
                        item.AddGenre(genre);
                    }

                    break;

                case "style":
                case "tag":
                    var tag = reader.ReadStringFromNfo();
                    if (!string.IsNullOrWhiteSpace(tag))
                    {
                        item.AddTag(tag);
                    }

                    break;

                case "fileinfo":
                    NfoSubtreeParsers<T>.ReadFileinfoSubtree(reader, item);
                    break;

                case "uniqueid":
                    reader.ReadUniqueIdFromNfo(item);
                    break;

                case "thumb":
                    NfoSubtreeParsers<T>.ReadThumbNode(reader, itemResult, _directoryService, Logger);
                    break;

                // Read Provider Ids
                default:
                    reader.ReadProviderIdFromNfo(item, _validProviderIds);
                    break;
            }
        }

        // todo remove
        internal XmlReaderSettings GetXmlReaderSettings()
            => new XmlReaderSettings()
            {
                ValidationType = ValidationType.None,
                CheckCharacters = false,
                IgnoreProcessingInstructions = true,
                IgnoreComments = true
            };

        /// <summary>
        /// Create Mappings for Provider Ids enabled for item.
        /// </summary>
        /// <param name="item">The item to create the mappings for.</param>
        private void CreateProviderIdMappings(T item)
        {
            var idInfos = _providerManager.GetExternalIdInfos(item);

            foreach (var info in idInfos)
            {
                var id = info.Key + "Id";
                if (!_validProviderIds.ContainsKey(id))
                {
                    _validProviderIds.Add(id, info.Key);
                }
            }

            // Additional Mappings
            _validProviderIds.Add("collectionnumber", "TmdbCollection");
            _validProviderIds.Add("tmdbcolid", "TmdbCollection");
            _validProviderIds.Add("imdb_id", "Imdb");
        }
    }
}
