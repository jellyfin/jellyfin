#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using Jellyfin.Extensions;
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
        private readonly IDirectoryService _directoryService;
        private Dictionary<string, string> _validProviderIds;

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
            ProviderManager = providerManager;
            _validProviderIds = new Dictionary<string, string>();
            _userManager = userManager;
            _userDataManager = userDataManager;
            _directoryService = directoryService;
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        protected ILogger Logger { get; }

        protected IProviderManager ProviderManager { get; }

        protected virtual bool SupportsUrlAfterClosingXmlTag => false;

        /// <summary>
        /// Fetches metadata for an item from one xml file.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="metadataFile">The metadata file.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException"><c>item</c> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><c>metadataFile</c> is <c>null</c> or empty.</exception>
        public void Fetch(MetadataResult<T> item, string metadataFile, CancellationToken cancellationToken)
        {
            if (item.Item == null)
            {
                throw new ArgumentException("Item can't be null.", nameof(item));
            }

            if (string.IsNullOrEmpty(metadataFile))
            {
                throw new ArgumentException("The metadata filepath was empty.", nameof(metadataFile));
            }

            _validProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var idInfos = ProviderManager.GetExternalIdInfos(item.Item);

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

            Fetch(item, metadataFile, GetXmlReaderSettings(), cancellationToken);
        }

        /// <summary>
        /// Fetches the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="metadataFile">The metadata file.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        protected virtual void Fetch(MetadataResult<T> item, string metadataFile, XmlReaderSettings settings, CancellationToken cancellationToken)
        {
            if (!SupportsUrlAfterClosingXmlTag)
            {
                using (var fileStream = File.OpenRead(metadataFile))
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                using (var reader = XmlReader.Create(streamReader, settings))
                {
                    item.ResetPeople();

                    reader.MoveToContent();
                    reader.Read();

                    // Loop through each element
                    while (!reader.EOF && reader.ReadState == ReadState.Interactive)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            FetchDataFromXmlNode(reader, item);
                        }
                        else
                        {
                            reader.Read();
                        }
                    }
                }

                return;
            }

            item.ResetPeople();

            // Need to handle a url after the xml data
            // http://kodi.wiki/view/NFO_files/movies

            var xml = File.ReadAllText(metadataFile);

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
                var endingXml = xml.AsSpan().Slice(index);

                ParseProviderLinks(item.Item, endingXml);

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
                ParseProviderLinks(item.Item, xml);

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
                            FetchDataFromXmlNode(reader, item);
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

        protected void ParseProviderLinks(T item, ReadOnlySpan<char> xml)
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
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            if (DateTime.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var added))
                            {
                                item.DateCreated = added;
                            }
                            else
                            {
                                Logger.LogWarning("Invalid Added value found: {Value}", val);
                            }
                        }

                        break;
                    }

                case "originaltitle":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrEmpty(val))
                        {
                            item.OriginalTitle = val;
                        }

                        break;
                    }

                case "name":
                case "title":
                case "localtitle":
                    item.Name = reader.ReadElementContentAsString();
                    break;

                case "sortname":
                    item.SortName = reader.ReadElementContentAsString();
                    break;

                case "criticrating":
                    {
                        var text = reader.ReadElementContentAsString();

                        if (!string.IsNullOrEmpty(text))
                        {
                            if (float.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                            {
                                item.CriticRating = value;
                            }
                        }

                        break;
                    }

                case "sorttitle":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.ForcedSortName = val;
                        }

                        break;
                    }

                case "biography":
                case "plot":
                case "review":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.Overview = val;
                        }

                        break;
                    }

                case "language":
                    {
                        var val = reader.ReadElementContentAsString();

                        item.PreferredMetadataLanguage = val;

                        break;
                    }

                case "watched":
                    {
                        var val = reader.ReadElementContentAsBoolean();

                        if (userData != null)
                        {
                            userData.Played = val;
                        }

                        break;
                    }

                case "playcount":
                    {
                        var val = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(val) && userData != null)
                        {
                            if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count))
                            {
                                userData.PlayCount = count;
                            }
                        }

                        break;
                    }

                case "lastplayed":
                    {
                        var val = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(val) && userData != null)
                        {
                            if (DateTime.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var added))
                            {
                                userData.LastPlayedDate = added;
                            }
                            else
                            {
                                Logger.LogWarning("Invalid lastplayed value found: {Value}", val);
                            }
                        }

                        break;
                    }

                case "countrycode":
                    {
                        var val = reader.ReadElementContentAsString();

                        item.PreferredMetadataCountryCode = val;

                        break;
                    }

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
                    item.Tagline = reader.ReadElementContentAsString();
                    break;

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
                    {
                        var rating = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(rating))
                        {
                            item.OfficialRating = rating;
                        }

                        break;
                    }

                case "customrating":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.CustomRating = val;
                        }

                        break;
                    }

                case "runtime":
                    {
                        var text = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            if (int.TryParse(text.AsSpan().LeftPart(' '), NumberStyles.Integer, CultureInfo.InvariantCulture, out var runtime))
                            {
                                item.RunTimeTicks = TimeSpan.FromMinutes(runtime).Ticks;
                            }
                        }

                        break;
                    }

                case "aspectratio":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val)
                            && item is IHasAspectRatio hasAspectRatio)
                        {
                            hasAspectRatio.AspectRatio = val;
                        }

                        break;
                    }

                case "lockdata":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.IsLocked = string.Equals("true", val, StringComparison.OrdinalIgnoreCase);
                        }

                        break;
                    }

                case "studio":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.AddStudio(val);
                        }

                        break;
                    }

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
                    {
                        var val = reader.ReadElementContentAsString();

                        var hasDisplayOrder = item as IHasDisplayOrder;
                        if (hasDisplayOrder != null)
                        {
                            if (!string.IsNullOrWhiteSpace(val))
                            {
                                hasDisplayOrder.DisplayOrder = val;
                            }
                        }

                        break;
                    }

                case "year":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            if (int.TryParse(val, out var productionYear) && productionYear > 1850)
                            {
                                item.ProductionYear = productionYear;
                            }
                        }

                        break;
                    }

                case "rating":
                    {
                        var rating = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(rating))
                        {
                            // All external meta is saving this as '.' for decimal I believe...but just to be sure
                            if (float.TryParse(rating.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var val))
                            {
                                item.CommunityRating = val;
                            }
                        }

                        break;
                    }

                case "ratings":
                    {
                        if (!reader.IsEmptyElement)
                        {
                            using var subtree = reader.ReadSubtree();
                            FetchFromRatingsNode(subtree, item);
                        }
                        else
                        {
                            reader.Read();
                        }

                        break;
                    }

                case "aired":
                case "formed":
                case "premiered":
                case "releasedate":
                    {
                        var formatString = nfoConfiguration.ReleaseDateFormat;

                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            if (DateTime.TryParseExact(val, formatString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date) && date.Year > 1850)
                            {
                                item.PremiereDate = date;
                                item.ProductionYear = date.Year;
                            }
                        }

                        break;
                    }

                case "enddate":
                    {
                        var formatString = nfoConfiguration.ReleaseDateFormat;

                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            if (DateTime.TryParseExact(val, formatString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date) && date.Year > 1850)
                            {
                                item.EndDate = date;
                            }
                        }

                        break;
                    }

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
                        FetchThumbNode(reader, itemResult, "thumb");
                        break;
                    }

                case "fanart":
                    {
                        if (reader.IsEmptyElement)
                        {
                            reader.Read();
                            break;
                        }

                        using var subtree = reader.ReadSubtree();
                        if (!subtree.ReadToDescendant("thumb"))
                        {
                            break;
                        }

                        FetchThumbNode(subtree, itemResult, "fanart");
                        break;
                    }

                default:
                    string readerName = reader.Name;
                    if (_validProviderIds.TryGetValue(readerName, out string? providerIdValue))
                    {
                        var id = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(providerIdValue) && !string.IsNullOrWhiteSpace(id))
                        {
                            item.SetProviderId(providerIdValue, id);
                        }
                    }
                    else
                    {
                        reader.Skip();
                    }

                    break;
            }
        }

        private void FetchThumbNode(XmlReader reader, MetadataResult<T> itemResult, string parentNode)
        {
            var artType = reader.GetAttribute("aspect");
            var val = reader.ReadElementContentAsString();

            // artType is null if the thumb node is a child of the fanart tag
            // -> set image type to fanart
            if (string.IsNullOrWhiteSpace(artType) && parentNode.Equals("fanart", StringComparison.Ordinal))
            {
                artType = "fanart";
            }
            else if (string.IsNullOrWhiteSpace(artType))
            {
                // Sonarr writes thumb tags for posters without aspect property
                artType = "poster";
            }

            // skip:
            // - empty uri
            // - tag containing '.' because we can't set images for seasons, episodes or movie sets within series or movies
            if (string.IsNullOrEmpty(val) || artType.Contains('.', StringComparison.Ordinal))
            {
                return;
            }

            ImageType imageType = GetImageType(artType);

            if (!Uri.TryCreate(val, UriKind.Absolute, out var uri))
            {
                Logger.LogError("Image location {Path} specified in nfo file for {ItemName} is not a valid URL or file path.", val, itemResult.Item.Name);
                return;
            }

            if (uri.IsFile)
            {
                // only allow one item of each type
                if (itemResult.Images.Any(x => x.Type == imageType))
                {
                    return;
                }

                var fileSystemMetadata = _directoryService.GetFile(val);
                // non existing file returns null
                if (fileSystemMetadata == null || !fileSystemMetadata.Exists)
                {
                    Logger.LogWarning("Artwork file {Path} specified in nfo file for {ItemName} does not exist.", uri, itemResult.Item.Name);
                    return;
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
                if (itemResult.RemoteImages.Any(x => x.Type == imageType))
                {
                    return;
                }

                itemResult.RemoteImages.Add((uri.ToString(), imageType));
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

        private void FetchFromRatingsNode(XmlReader reader, T item)
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
                        case "rating":
                        {
                            if (reader.IsEmptyElement)
                            {
                                reader.Read();
                                continue;
                            }

                            var ratingName = reader.GetAttribute("name");

                            using var subtree = reader.ReadSubtree();
                            FetchFromRatingNode(subtree, item, ratingName);

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

        private void FetchFromRatingNode(XmlReader reader, T item, string? ratingName)
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
                        case "value":
                            var val = reader.ReadElementContentAsString();

                            if (!string.IsNullOrWhiteSpace(val))
                            {
                                if (float.TryParse(val, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var ratingValue))
                                {
                                    // if ratingName contains tomato --> assume critic rating
                                    if (ratingName != null &&
                                        ratingName.Contains("tomato", StringComparison.OrdinalIgnoreCase) &&
                                        !ratingName.Contains("audience", StringComparison.OrdinalIgnoreCase))
                                    {
                                        item.CriticRating = ratingValue;
                                    }
                                    else
                                    {
                                        item.CommunityRating = ratingValue;
                                    }
                                }
                            }

                            break;
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
                            name = reader.ReadElementContentAsString();
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
                                    if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intVal))
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
        /// Used to split names of comma or pipe delimited genres and people.
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
                "fanart" => ImageType.Backdrop,
                // unknown type (including "poster") --> primary
                _ => ImageType.Primary,
            };
        }
    }
}
