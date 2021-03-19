#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using MediaBrowser.XbmcMetadata.Savers;
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
                // DateCreated
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

                    break;

                case "playcount":
                    if (userData != null)
                    {
                        userData.PlayCount = reader.ReadIntFromNfo() ?? userData.PlayCount;
                    }

                    break;

                case "lastplayed":
                    if (userData != null)
                    {
                        userData.LastPlayedDate = reader.ReadDateFromNfo() ?? userData.LastPlayedDate;
                    }

                    break;

                case "countrycode":
                    item.PreferredMetadataCountryCode = reader.ReadStringFromNfo() ?? item.PreferredMetadataCountryCode;
                    break;

                // todo
                case "lockedfields":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.LockedFields = val.Split('|').Select(i =>
                            {
                                if (Enum.TryParse(i, true, out MetadataField field))
                                {
                                    return (MetadataField?)field;
                                }

                                return null;
                            }).OfType<MetadataField>().ToArray();
                        }

                        break;
                    }

                case "tagline":
                    item.Tagline = reader.ReadStringFromNfo() ?? item.Tagline;
                    break;

                // todo
                case "country":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.ProductionLocations = val.Split('/')
                                .Select(i => i.Trim())
                                .Where(i => !string.IsNullOrWhiteSpace(i))
                                .ToArray();
                        }

                        break;
                    }

                case "mpaa":
                    item.OfficialRating = reader.ReadStringFromNfo() ?? item.OfficialRating;
                    break;

                case "customrating":
                    item.CustomRating = reader.ReadStringFromNfo() ?? item.CustomRating;
                    break;

                // todo
                case "runtime":
                    {
                        var text = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            if (int.TryParse(text.Split(' ')[0], NumberStyles.Integer, UsCulture, out var runtime))
                            {
                                item.RunTimeTicks = TimeSpan.FromMinutes(runtime).Ticks;
                            }
                        }

                        break;
                    }

                case "aspectratio":
                    if (item is IHasAspectRatio hasAspectRatio)
                    {
                        hasAspectRatio.AspectRatio = reader.ReadStringFromNfo() ?? hasAspectRatio.AspectRatio;
                    }

                    break;

                case "lockdata":
                    item.IsLocked = reader.ReadBoolFromNfo() ?? item.IsLocked;
                    break;

                // todo
                case "studio":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.AddStudio(val);
                        }

                        break;
                    }

                // todo
                case "director":
                    {
                        var val = reader.ReadElementContentAsString();
                        foreach (var p in SplitNames(val).Select(v => new PersonInfo { Name = v.Trim(), Type = PersonType.Director }))
                        {
                            if (string.IsNullOrWhiteSpace(p.Name))
                            {
                                continue;
                            }

                            itemResult.AddPerson(p);
                        }

                        break;
                    }

                // todo
                case "credits":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            var parts = val.Split('/').Select(i => i.Trim())
                                .Where(i => !string.IsNullOrEmpty(i));

                            foreach (var p in parts.Select(v => new PersonInfo { Name = v.Trim(), Type = PersonType.Writer }))
                            {
                                if (string.IsNullOrWhiteSpace(p.Name))
                                {
                                    continue;
                                }

                                itemResult.AddPerson(p);
                            }
                        }

                        break;
                    }

                // todo
                case "writer":
                    {
                        var val = reader.ReadElementContentAsString();
                        foreach (var p in SplitNames(val).Select(v => new PersonInfo { Name = v.Trim(), Type = PersonType.Writer }))
                        {
                            if (string.IsNullOrWhiteSpace(p.Name))
                            {
                                continue;
                            }

                            itemResult.AddPerson(p);
                        }

                        break;
                    }

                // todo
                case "actor":
                    {
                        if (!reader.IsEmptyElement)
                        {
                            using (var subtree = reader.ReadSubtree())
                            {
                                var person = GetPersonFromXmlNode(subtree);

                                if (!string.IsNullOrWhiteSpace(person.Name))
                                {
                                    itemResult.AddPerson(person);
                                }
                            }
                        }
                        else
                        {
                            reader.Read();
                        }

                        break;
                    }

                // todo
                case "trailer":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            val = val.Replace("plugin://plugin.video.youtube/?action=play_video&videoid=", BaseNfoSaver.YouTubeWatchUrl, StringComparison.OrdinalIgnoreCase);

                            item.AddTrailerUrl(val);
                        }

                        break;
                    }

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

                // todo
                case "aired":
                case "formed":
                case "premiered":
                case "releasedate":
                    {
                        var formatString = nfoConfiguration.ReleaseDateFormat;

                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            if (DateTime.TryParseExact(val, formatString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date) && date.Year > 1850)
                            {
                                item.PremiereDate = date.ToUniversalTime();
                                item.ProductionYear = date.Year;
                            }
                        }

                        break;
                    }

                // todo
                case "enddate":
                    {
                        var formatString = nfoConfiguration.ReleaseDateFormat;

                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            if (DateTime.TryParseExact(val, formatString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date) && date.Year > 1850)
                            {
                                item.EndDate = date.ToUniversalTime();
                            }
                        }

                        break;
                    }

                // todo
                case "genre":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            var parts = val.Split('/')
                                .Select(i => i.Trim())
                                .Where(i => !string.IsNullOrWhiteSpace(i));

                            foreach (var p in parts)
                            {
                                item.AddGenre(p);
                            }
                        }

                        break;
                    }

                // todo
                case "style":
                case "tag":
                    {
                        var val = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.AddTag(val);
                        }

                        break;
                    }

                // todo
                case "fileinfo":
                    {
                        if (!reader.IsEmptyElement)
                        {
                            using (var subtree = reader.ReadSubtree())
                            {
                                FetchFromFileInfoNode(subtree, item);
                            }
                        }
                        else
                        {
                            reader.Read();
                        }

                        break;
                    }

                // todo
                case "uniqueid":
                    {
                        if (reader.IsEmptyElement)
                        {
                            reader.Read();
                            break;
                        }

                        var provider = reader.GetAttribute("type");
                        var id = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(provider) && !string.IsNullOrWhiteSpace(id))
                        {
                            item.SetProviderId(provider, id);
                        }

                        break;
                    }

                case "thumb":
                    {
                        var artType = reader.GetAttribute("aspect");
                        var val = reader.ReadElementContentAsString();

                        // skip:
                        // - empty aspect tag
                        // - empty uri
                        // - tag containing '.' because we can't set images for seasons, episodes or movie sets within series or movies
                        if (string.IsNullOrEmpty(artType) || string.IsNullOrEmpty(val) || artType.Contains('.', StringComparison.Ordinal))
                        {
                            break;
                        }

                        ImageType imageType = GetImageType(artType);

                        if (!Uri.TryCreate(val, UriKind.Absolute, out var uri))
                        {
                            Logger.LogError("Image location {Path} specified in nfo file for {ItemName} is not a valid URL or file path.", val, item.Name);
                            break;
                        }

                        if (uri.IsFile)
                        {
                            // only allow one item of each type
                            if (itemResult.Images.Any(x => x.Type == imageType))
                            {
                                break;
                            }

                            var fileSystemMetadata = _directoryService.GetFile(val);
                            // non existing file returns null
                            if (fileSystemMetadata == null || !fileSystemMetadata.Exists)
                            {
                                Logger.LogWarning("Artwork file {Path} specified in nfo file for {ItemName} does not exist.", uri, item.Name);
                                break;
                            }

                            itemResult.Images.Add(new LocalImageInfo()
                            {
                                FileInfo = fileSystemMetadata,
                                Type = imageType
                            });
                        }
                        else
                        {
                            // only allow one item of each type
                            if (itemResult.RemoteImages.Any(x => x.type == imageType))
                            {
                                break;
                            }

                            itemResult.RemoteImages.Add((uri.ToString(), imageType));
                        }

                        break;
                    }

                // Read Provider Ids
                default:
                    reader.ReadProviderIdFromNfo(item, _validProviderIds);
                    break;
            }
        }

        private void FetchFromFileInfoNode(XmlReader reader, T item)
        {
            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "streamdetails":
                            {
                                if (reader.IsEmptyElement)
                                {
                                    reader.Read();
                                    continue;
                                }

                                using (var subtree = reader.ReadSubtree())
                                {
                                    FetchFromStreamDetailsNode(subtree, item);
                                }

                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
                else
                {
                    reader.Read();
                }
            }
        }

        private void FetchFromStreamDetailsNode(XmlReader reader, T item)
        {
            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "video":
                            {
                                if (reader.IsEmptyElement)
                                {
                                    reader.Read();
                                    continue;
                                }

                                using (var subtree = reader.ReadSubtree())
                                {
                                    FetchFromVideoNode(subtree, item);
                                }

                                break;
                            }

                        case "subtitle":
                            {
                                if (reader.IsEmptyElement)
                                {
                                    reader.Read();
                                    continue;
                                }

                                using (var subtree = reader.ReadSubtree())
                                {
                                    FetchFromSubtitleNode(subtree, item);
                                }

                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
                else
                {
                    reader.Read();
                }
            }
        }

        private void FetchFromVideoNode(XmlReader reader, T item)
        {
            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "format3d":
                            {
                                var val = reader.ReadElementContentAsString();

                                var video = item as Video;

                                if (video != null)
                                {
                                    if (string.Equals("HSBS", val, StringComparison.OrdinalIgnoreCase))
                                    {
                                        video.Video3DFormat = Video3DFormat.HalfSideBySide;
                                    }
                                    else if (string.Equals("HTAB", val, StringComparison.OrdinalIgnoreCase))
                                    {
                                        video.Video3DFormat = Video3DFormat.HalfTopAndBottom;
                                    }
                                    else if (string.Equals("FTAB", val, StringComparison.OrdinalIgnoreCase))
                                    {
                                        video.Video3DFormat = Video3DFormat.FullTopAndBottom;
                                    }
                                    else if (string.Equals("FSBS", val, StringComparison.OrdinalIgnoreCase))
                                    {
                                        video.Video3DFormat = Video3DFormat.FullSideBySide;
                                    }
                                    else if (string.Equals("MVC", val, StringComparison.OrdinalIgnoreCase))
                                    {
                                        video.Video3DFormat = Video3DFormat.MVC;
                                    }
                                }

                                break;
                            }

                        case "aspect":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (item is Video video)
                                {
                                    video.AspectRatio = val;
                                }

                                break;
                            }

                        case "width":
                            {
                                var val = reader.ReadElementContentAsInt();

                                if (item is Video video)
                                {
                                    video.Width = val;
                                }

                                break;
                            }

                        case "height":
                            {
                                var val = reader.ReadElementContentAsInt();

                                if (item is Video video)
                                {
                                    video.Height = val;
                                }

                                break;
                            }

                        case "durationinseconds":
                            {
                                var val = reader.ReadElementContentAsInt();

                                if (item is Video video)
                                {
                                    video.RunTimeTicks = new TimeSpan(0, 0, val).Ticks;
                                }

                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
                else
                {
                    reader.Read();
                }
            }
        }

        private void FetchFromSubtitleNode(XmlReader reader, T item)
        {
            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "language":
                            {
                                _ = reader.ReadElementContentAsString();

                                if (item is Video video)
                                {
                                    video.HasSubtitles = true;
                                }

                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
                else
                {
                    reader.Read();
                }
            }
        }

        /// <summary>
        /// Gets the persons from XML node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>IEnumerable{PersonInfo}.</returns>
        private PersonInfo GetPersonFromXmlNode(XmlReader reader)
        {
            var name = string.Empty;
            var type = PersonType.Actor;  // If type is not specified assume actor
            var role = string.Empty;
            int? sortOrder = null;
            string? imageUrl = null;

            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "name":
                            name = reader.ReadElementContentAsString() ?? string.Empty;
                            break;

                        case "role":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    role = val;
                                }

                                break;
                            }

                        case "type":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    type = val switch
                                    {
                                        PersonType.Composer => PersonType.Composer,
                                        PersonType.Conductor => PersonType.Conductor,
                                        PersonType.Director => PersonType.Director,
                                        PersonType.Lyricist => PersonType.Lyricist,
                                        PersonType.Producer => PersonType.Producer,
                                        PersonType.Writer => PersonType.Writer,
                                        PersonType.GuestStar => PersonType.GuestStar,
                                        // unknown type --> actor
                                        _ => PersonType.Actor
                                    };
                                }

                                break;
                            }

                        case "order":
                        case "sortorder":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    if (int.TryParse(val, NumberStyles.Integer, UsCulture, out var intVal))
                                    {
                                        sortOrder = intVal;
                                    }
                                }

                                break;
                            }

                        case "thumb":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    imageUrl = val;
                                }

                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
                else
                {
                    reader.Read();
                }
            }

            return new PersonInfo
            {
                Name = name.Trim(),
                Role = role,
                Type = type,
                SortOrder = sortOrder,
                ImageUrl = imageUrl
            };
        }

        internal XmlReaderSettings GetXmlReaderSettings()
            => new XmlReaderSettings()
            {
                ValidationType = ValidationType.None,
                CheckCharacters = false,
                IgnoreProcessingInstructions = true,
                IgnoreComments = true
            };

        /// <summary>
        /// Used to split names of comma or pipe delimeted genres and people.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>IEnumerable{System.String}.</returns>
        private IEnumerable<string> SplitNames(string value)
        {
            // Only split by comma if there is no pipe in the string
            // We have to be careful to not split names like Matthew, Jr.
            var separator = !value.Contains('|', StringComparison.Ordinal) && !value.Contains(';', StringComparison.Ordinal)
                ? new[] { ',' }
                : new[] { '|', ';' };

            value = value.Trim().Trim(separator);

            return string.IsNullOrWhiteSpace(value) ? Array.Empty<string>() : value.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        }

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

        /// <summary>
        /// Parses the ImageType from the nfo aspect property.
        /// </summary>
        /// <param name="aspect">The nfo aspect property.</param>
        /// <returns>The image type.</returns>
        private static ImageType GetImageType(string aspect)
        {
            return aspect switch
            {
                "banner" => ImageType.Banner,
                "clearlogo" => ImageType.Logo,
                "discart" => ImageType.Disc,
                "landscape" => ImageType.Thumb,
                "clearart" => ImageType.Art,
                // unknown type (including "poster") --> primary
                _ => ImageType.Primary,
            };
        }
    }
}
