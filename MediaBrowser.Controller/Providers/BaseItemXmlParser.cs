using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Controller.Providers
{
    /// <summary>
    /// Provides a base class for parsing metadata xml
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BaseItemXmlParser<T>
        where T : BaseItem, new()
    {
        /// <summary>
        /// The logger
        /// </summary>
        protected ILogger Logger { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseItemXmlParser{T}" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public BaseItemXmlParser(ILogger logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// Fetches metadata for an item from one xml file
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="metadataFile">The metadata file.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public void Fetch(T item, string metadataFile, CancellationToken cancellationToken)
        {
            if (item == null)
            {
                throw new ArgumentNullException();
            }

            if (string.IsNullOrEmpty(metadataFile))
            {
                throw new ArgumentNullException();
            }

            var settings = new XmlReaderSettings
            {
                CheckCharacters = false,
                IgnoreProcessingInstructions = true,
                IgnoreComments = true,
                ValidationType = ValidationType.None
            };

            item.Taglines.Clear();
            item.Studios.Clear();
            item.Genres.Clear();
            item.People.Clear();
            item.Tags.Clear();

            //Fetch(item, metadataFile, settings, Encoding.GetEncoding("ISO-8859-1"), cancellationToken);
            Fetch(item, metadataFile, settings, Encoding.UTF8, cancellationToken);
        }

        /// <summary>
        /// Fetches the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="metadataFile">The metadata file.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private void Fetch(T item, string metadataFile, XmlReaderSettings settings, Encoding encoding, CancellationToken cancellationToken)
        {
            using (var streamReader = new StreamReader(metadataFile, encoding))
            {
                // Use XmlReader for best performance
                using (var reader = XmlReader.Create(streamReader, settings))
                {
                    reader.MoveToContent();

                    // Loop through each element
                    while (reader.Read())
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            FetchDataFromXmlNode(reader, item);
                        }
                    }
                }
            }
        }

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        /// <summary>
        /// Fetches metadata from one Xml Element
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
        protected virtual void FetchDataFromXmlNode(XmlReader reader, T item)
        {
            switch (reader.Name)
            {
                // DateCreated
                case "Added":
                    DateTime added;
                    if (DateTime.TryParse(reader.ReadElementContentAsString() ?? string.Empty, out added))
                    {
                        item.DateCreated = added.ToUniversalTime();
                    }
                    break;

                case "LocalTitle":
                    item.Name = reader.ReadElementContentAsString();
                    break;

                case "Type":
                    {
                        var type = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(type) && !type.Equals("none", StringComparison.OrdinalIgnoreCase))
                        {
                            item.DisplayMediaType = type;
                        }

                        break;
                    }

                case "CriticRating":
                    {
                        var text = reader.ReadElementContentAsString();
                        float value;
                        if (float.TryParse(text, NumberStyles.Any, _usCulture, out value))
                        {
                            item.CriticRating = value;
                        }

                        break;
                    }

                case "Budget":
                    {
                        var text = reader.ReadElementContentAsString();
                        double value;
                        if (double.TryParse(text, NumberStyles.Any, _usCulture, out value))
                        {
                            item.Budget = value;
                        }

                        break;
                    }

                case "Revenue":
                    {
                        var text = reader.ReadElementContentAsString();
                        double value;
                        if (double.TryParse(text, NumberStyles.Any, _usCulture, out value))
                        {
                            item.Revenue = value;
                        }

                        break;
                    }

                case "SortTitle":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.ForcedSortName = val;
                        }

                        break;
                    }

                case "Overview":
                case "Description":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.Overview = val;
                        }

                        break;
                    }

                case "CriticRatingSummary":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.CriticRatingSummary = val;
                        }

                        break;
                    }

                case "TagLine":
                    {
                        var tagline = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(tagline))
                        {
                            item.AddTagline(tagline);
                        }

                        break;
                    }

                case "PlaceOfBirth":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.ProductionLocations = new List<string> { val };
                        }

                        break;
                    }

                case "Website":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.HomePageUrl = val;
                        }

                        break;
                    }

                case "LockedFields":
                    {
                        var fields = new List<MetadataFields>();

                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            var list = val.Split('|').Select(i =>
                            {
                                MetadataFields field;

                                if (Enum.TryParse<MetadataFields>(i, true, out field))
                                {
                                    return (MetadataFields?)field;
                                }

                                return null;

                            }).Where(i => i.HasValue).Select(i => i.Value);

                            fields.AddRange(list);
                        }

                        item.LockedFields = fields;

                        break;
                    }

                case "TagLines":
                    {
                        using (var subtree = reader.ReadSubtree())
                        {
                            FetchFromTaglinesNode(subtree, item);
                        }
                        break;
                    }

                case "ContentRating":
                case "certification":
                case "MPAARating":
                case "ESRBRating":
                    {
                        var rating = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(rating))
                        {
                            item.OfficialRating = rating;
                        }
                        break;
                    }

                case "MPAADescription":
                    {
                        var rating = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(rating))
                        {
                            item.OfficialRatingDescription = rating;
                        }
                        break;
                    }

                case "CustomRating":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.CustomRating = val;
                        }
                        break;
                    }

                case "Runtime":
                case "RunningTime":
                    {
                        var text = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            int runtime;
                            if (int.TryParse(text.Split(' ')[0], NumberStyles.Integer, _usCulture, out runtime))
                            {
                                // For audio and video don't replace ffmpeg data
                                if (item is Video || item is Audio)
                                {
                                    item.OriginalRunTimeTicks = TimeSpan.FromMinutes(runtime).Ticks;
                                }
                                else
                                {
                                    item.RunTimeTicks = TimeSpan.FromMinutes(runtime).Ticks;
                                }
                            }
                        }
                        break;
                    }

                case "Genre":
                    {
                        foreach (var name in SplitNames(reader.ReadElementContentAsString()))
                        {
                            if (string.IsNullOrWhiteSpace(name))
                            {
                                continue;
                            }
                            item.AddGenre(name);
                        }
                        break;
                    }

                case "AspectRatio":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.AspectRatio = val;
                        }
                        break;
                    }

                case "LockData":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.DontFetchMeta = string.Equals("true", val, StringComparison.OrdinalIgnoreCase);
                        }
                        break;
                    }

                case "Network":
                    {
                        foreach (var name in SplitNames(reader.ReadElementContentAsString()))
                        {
                            if (string.IsNullOrWhiteSpace(name))
                            {
                                continue;
                            }
                            item.AddStudio(name);
                        }
                        break;
                    }

                case "Director":
                    {
                        foreach (var p in SplitNames(reader.ReadElementContentAsString()).Select(v => new PersonInfo { Name = v.Trim(), Type = PersonType.Director }))
                        {
                            if (string.IsNullOrWhiteSpace(p.Name))
                            {
                                continue;
                            }
                            item.AddPerson(p);
                        }
                        break;
                    }
                case "Writer":
                    {
                        foreach (var p in SplitNames(reader.ReadElementContentAsString()).Select(v => new PersonInfo { Name = v.Trim(), Type = PersonType.Writer }))
                        {
                            if (string.IsNullOrWhiteSpace(p.Name))
                            {
                                continue;
                            }
                            item.AddPerson(p);
                        }
                        break;
                    }

                case "Actors":
                    {

                        var actors = reader.ReadInnerXml();

                        if (actors.Contains("<"))
                        {
                            // This is one of the mis-named "Actors" full nodes created by MB2
                            // Create a reader and pass it to the persons node processor
                            FetchDataFromPersonsNode(new XmlTextReader(new StringReader("<Persons>" + actors + "</Persons>")), item);
                        }
                        else
                        {
                            // Old-style piped string
                            foreach (var p in SplitNames(actors).Select(v => new PersonInfo { Name = v.Trim(), Type = PersonType.Actor }))
                            {
                                if (string.IsNullOrWhiteSpace(p.Name))
                                {
                                    continue;
                                }
                                item.AddPerson(p);
                            }
                        }
                        break;
                    }

                case "GuestStars":
                    {
                        foreach (var p in SplitNames(reader.ReadElementContentAsString()).Select(v => new PersonInfo { Name = v.Trim(), Type = PersonType.GuestStar }))
                        {
                            if (string.IsNullOrWhiteSpace(p.Name))
                            {
                                continue;
                            }
                            item.AddPerson(p);
                        }
                        break;
                    }

                case "Trailer":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.AddTrailerUrl(val, false);
                        }
                        break;
                    }

                case "ReleaseYear":
                case "ProductionYear":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            int productionYear;
                            if (int.TryParse(val, out productionYear) && productionYear > 1850)
                            {
                                item.ProductionYear = productionYear;
                            }
                        }

                        break;
                    }

                case "Rating":
                case "IMDBrating":
                case "TGDBRating":
                    {

                        var rating = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(rating))
                        {
                            float val;
                            // All external meta is saving this as '.' for decimal I believe...but just to be sure
                            if (float.TryParse(rating.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out val))
                            {
                                item.CommunityRating = val;
                            }
                        }
                        break;
                    }

                case "BirthDate":
                case "PremiereDate":
                case "FirstAired":
                    {
                        var firstAired = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(firstAired))
                        {
                            DateTime airDate;

                            if (DateTime.TryParseExact(firstAired, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out airDate) && airDate.Year > 1850)
                            {
                                item.PremiereDate = airDate.ToUniversalTime();
                                item.ProductionYear = airDate.Year;
                            }
                        }

                        break;
                    }

                case "DeathDate":
                case "EndDate":
                    {
                        var firstAired = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(firstAired))
                        {
                            DateTime airDate;

                            if (DateTime.TryParseExact(firstAired, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out airDate) && airDate.Year > 1850)
                            {
                                item.EndDate = airDate.ToUniversalTime();
                            }
                        }

                        break;
                    }

                case "TvDbId":
                    var tvdbId = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(tvdbId))
                    {
                        item.SetProviderId(MetadataProviders.Tvdb, tvdbId);
                    }
                    break;

                case "GamesDbId":
                    var gamesdbId = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(gamesdbId))
                    {
                        item.SetProviderId(MetadataProviders.Gamesdb, gamesdbId);
                    }
                    break;

                case "Players":
                    {
                        var val = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            int num;

                            if (int.TryParse(val, NumberStyles.Integer, _usCulture, out num))
                            {
                                var game = item as Game;
                                if (game != null)
                                {
                                    game.PlayersSupported = num;
                                }
                            }
                        }
                        break;
                    }
                case "VoteCount":
                    {
                        var val = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            int num;

                            if (int.TryParse(val, NumberStyles.Integer, _usCulture, out num))
                            {
                                item.VoteCount = num;
                            }
                        }
                        break;
                    }
                case "GameSystem":
                    {
                        var val = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            var game = item as Game;
                            if (game != null)
                            {
                                game.GameSystem = val;
                            }
                        }
                        break;
                    }
                case "MusicbrainzId":
                    {
                        var mbz = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(mbz))
                        {
                            item.SetProviderId(MetadataProviders.Musicbrainz, mbz);
                        }
                        break;
                    }
                case "MusicBrainzReleaseGroupId":
                    {
                        var mbz = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(mbz))
                        {
                            item.SetProviderId(MetadataProviders.MusicBrainzReleaseGroup, mbz);
                        }
                        break;
                    }
                case "RottenTomatoesId":
                    var rtId = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(rtId))
                    {
                        item.SetProviderId(MetadataProviders.RottenTomatoes, rtId);
                    }
                    break;

                case "TMDbId":
                    var tmdb = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(tmdb))
                    {
                        item.SetProviderId(MetadataProviders.Tmdb, tmdb);
                    }
                    break;

                case "TMDbCollectionId":
                    var tmdbCollection = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(tmdbCollection))
                    {
                        item.SetProviderId(MetadataProviders.TmdbCollection, tmdbCollection);
                    }
                    break;

                case "TVcomId":
                    var TVcomId = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(TVcomId))
                    {
                        item.SetProviderId(MetadataProviders.Tvcom, TVcomId);
                    }
                    break;

                case "Zap2ItId":
                    var zap2ItId = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(zap2ItId))
                    {
                        item.SetProviderId(MetadataProviders.Zap2It, zap2ItId);
                    }
                    break;

                case "IMDB_ID":
                case "IMDB":
                case "IMDbId":
                    var imDbId = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(imDbId))
                    {
                        item.SetProviderId(MetadataProviders.Imdb, imDbId);
                    }
                    break;

                case "Genres":
                    {
                        using (var subtree = reader.ReadSubtree())
                        {
                            FetchFromGenresNode(subtree, item);
                        }
                        break;
                    }

                case "Tags":
                    {
                        using (var subtree = reader.ReadSubtree())
                        {
                            FetchFromTagsNode(subtree, item);
                        }
                        break;
                    }

                case "Persons":
                    {
                        using (var subtree = reader.ReadSubtree())
                        {
                            FetchDataFromPersonsNode(subtree, item);
                        }
                        break;
                    }

                case "ParentalRating":
                    {
                        using (var subtree = reader.ReadSubtree())
                        {
                            FetchFromParentalRatingNode(subtree, item);
                        }
                        break;
                    }

                case "Studios":
                    {
                        using (var subtree = reader.ReadSubtree())
                        {
                            FetchFromStudiosNode(subtree, item);
                        }
                        break;
                    }

                case "MediaInfo":
                    {
                        using (var subtree = reader.ReadSubtree())
                        {
                            FetchFromMediaInfoNode(subtree, item);
                        }
                        break;
                    }

                default:
                    reader.Skip();
                    break;
            }
        }

        /// <summary>
        /// Fetches from media info node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
        private void FetchFromMediaInfoNode(XmlReader reader, T item)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Video":
                            {
                                using (var subtree = reader.ReadSubtree())
                                {
                                    FetchFromMediaInfoVideoNode(subtree, item);
                                }
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Fetches from media info video node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
        private void FetchFromMediaInfoVideoNode(XmlReader reader, T item)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Format3D":
                            {
                                var video = item as Video;

                                if (video != null)
                                {
                                    var val = reader.ReadElementContentAsString();

                                    if (string.Equals("HSBS", val))
                                    {
                                        video.Video3DFormat = Video3DFormat.HalfSideBySide;
                                    }
                                    else if (string.Equals("HTAB", val))
                                    {
                                        video.Video3DFormat = Video3DFormat.HalfTopAndBottom;
                                    }
                                    else if (string.Equals("FTAB", val))
                                    {
                                        video.Video3DFormat = Video3DFormat.FullTopAndBottom;
                                    }
                                    else if (string.Equals("FSBS", val))
                                    {
                                        video.Video3DFormat = Video3DFormat.FullSideBySide;
                                    }
                                }
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Fetches from taglines node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
        private void FetchFromTaglinesNode(XmlReader reader, T item)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Tagline":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    item.AddTagline(val);
                                }
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Fetches from genres node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
        private void FetchFromGenresNode(XmlReader reader, T item)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Genre":
                            {
                                var genre = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(genre))
                                {
                                    item.AddGenre(genre);
                                }
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }
        }

        private void FetchFromTagsNode(XmlReader reader, T item)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Tag":
                            {
                                var tag = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(tag))
                                {
                                    item.AddTag(tag);
                                }
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Fetches the data from persons node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
        private void FetchDataFromPersonsNode(XmlReader reader, T item)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Person":
                        case "Actor":
                            {
                                using (var subtree = reader.ReadSubtree())
                                {
                                    foreach (var person in GetPersonsFromXmlNode(subtree))
                                    {
                                        item.AddPerson(person);
                                    }
                                }
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }
        }

        protected async Task FetchChaptersFromXmlNode(BaseItem item, XmlReader reader, IItemRepository repository, CancellationToken cancellationToken)
        {
            var runtime = item.RunTimeTicks ?? 0;

            using (reader)
            {
                var chapters = GetChaptersFromXmlNode(reader)
                    .Where(i => i.StartPositionTicks >= 0 && i.StartPositionTicks < runtime);

                await repository.SaveChapters(item.Id, chapters, cancellationToken).ConfigureAwait(false);
            }
        }

        private IEnumerable<ChapterInfo> GetChaptersFromXmlNode(XmlReader reader)
        {
            var chapters = new List<ChapterInfo>();

            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Chapter":
                            {
                                using (var subtree = reader.ReadSubtree())
                                {
                                    chapters.Add(GetChapterInfoFromXmlNode(subtree));
                                }
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }

            return chapters;
        }

        private ChapterInfo GetChapterInfoFromXmlNode(XmlReader reader)
        {
            var chapter = new ChapterInfo();

            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "StartPositionMs":
                            {
                                var val = reader.ReadElementContentAsString();

                                var ms = long.Parse(val, _usCulture);

                                chapter.StartPositionTicks = TimeSpan.FromMilliseconds(ms).Ticks;

                                break;
                            }

                        case "Name":
                            {
                                chapter.Name = reader.ReadElementContentAsString();
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }

            return chapter;
        }

        /// <summary>
        /// Fetches from studios node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
        private void FetchFromStudiosNode(XmlReader reader, T item)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Studio":
                            {
                                var studio = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(studio))
                                {
                                    item.AddStudio(studio);
                                }
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Fetches from parental rating node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
        private void FetchFromParentalRatingNode(XmlReader reader, T item)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        // Removed support for "Value" tag as it conflicted with MPAA rating but leaving this function for possible
                        // future support of "Description" -ebr

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the persons from XML node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>IEnumerable{PersonInfo}.</returns>
        private IEnumerable<PersonInfo> GetPersonsFromXmlNode(XmlReader reader)
        {
            var names = new List<string>();
            var type = "Actor";  // If type is not specified assume actor
            var role = string.Empty;

            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Name":
                            names.AddRange(SplitNames(reader.ReadElementContentAsString()));
                            break;

                        case "Type":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    type = val;
                                }
                                break;
                            }

                        case "Role":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    role = val;
                                }
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }

            return names.Select(n => new PersonInfo { Name = n.Trim(), Role = role, Type = type });
        }

        /// <summary>
        /// Used to split names of comma or pipe delimeted genres and people
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>IEnumerable{System.String}.</returns>
        private IEnumerable<string> SplitNames(string value)
        {
            value = value ?? string.Empty;

            // Only split by comma if there is no pipe in the string
            // We have to be careful to not split names like Matthew, Jr.
            var separator = value.IndexOf('|') == -1 && value.IndexOf(';') == -1 ? new[] { ',' } : new[] { '|', ';' };

            value = value.Trim().Trim(separator);

            return string.IsNullOrWhiteSpace(value) ? new string[] { } : Split(value, separator, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Provides an additional overload for string.split
        /// </summary>
        /// <param name="val">The val.</param>
        /// <param name="separators">The separators.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String[][].</returns>
        private static string[] Split(string val, char[] separators, StringSplitOptions options)
        {
            return val.Split(separators, options);
        }

    }
}
