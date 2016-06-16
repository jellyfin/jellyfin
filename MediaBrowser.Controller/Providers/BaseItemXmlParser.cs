using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;

namespace MediaBrowser.Controller.Providers
{
    /// <summary>
    /// Provides a base class for parsing metadata xml
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BaseItemXmlParser<T>
        where T : BaseItem
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
        public void Fetch(MetadataResult<T> item, string metadataFile, CancellationToken cancellationToken)
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
        private void Fetch(MetadataResult<T> item, string metadataFile, XmlReaderSettings settings, Encoding encoding, CancellationToken cancellationToken)
        {
            item.ResetPeople();

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
        /// <param name="itemResult">The item result.</param>
        protected virtual void FetchDataFromXmlNode(XmlReader reader, MetadataResult<T> itemResult)
        {
            var item = itemResult.Item;

            switch (reader.Name)
            {
                // DateCreated
                case "Added":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            DateTime added;
                            if (DateTime.TryParse(val, out added))
                            {
                                item.DateCreated = added.ToUniversalTime();
                            }
                            else
                            {
                                Logger.Warn("Invalid Added value found: " + val);
                            }
                        }
                        break;
                    }

                case "OriginalTitle":
                    {
                        var val = reader.ReadElementContentAsString();

                        var hasOriginalTitle = item as IHasOriginalTitle;
                        if (hasOriginalTitle != null)
                        {
                            if (!string.IsNullOrEmpty(hasOriginalTitle.OriginalTitle))
                            {
                                hasOriginalTitle.OriginalTitle = val;
                            }
                        }
                        break;
                    }

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

                        var hasCriticRating = item as IHasCriticRating;

                        if (hasCriticRating != null && !string.IsNullOrEmpty(text))
                        {
                            float value;
                            if (float.TryParse(text, NumberStyles.Any, _usCulture, out value))
                            {
                                hasCriticRating.CriticRating = value;
                            }
                        }

                        break;
                    }

                case "Budget":
                    {
                        var text = reader.ReadElementContentAsString();
                        var hasBudget = item as IHasBudget;
                        if (hasBudget != null)
                        {
                            double value;
                            if (double.TryParse(text, NumberStyles.Any, _usCulture, out value))
                            {
                                hasBudget.Budget = value;
                            }
                        }

                        break;
                    }

                case "Revenue":
                    {
                        var text = reader.ReadElementContentAsString();
                        var hasBudget = item as IHasBudget;
                        if (hasBudget != null)
                        {
                            double value;
                            if (double.TryParse(text, NumberStyles.Any, _usCulture, out value))
                            {
                                hasBudget.Revenue = value;
                            }
                        }

                        break;
                    }

                case "Metascore":
                    {
                        var text = reader.ReadElementContentAsString();
                        var hasMetascore = item as IHasMetascore;
                        if (hasMetascore != null)
                        {
                            float value;
                            if (float.TryParse(text, NumberStyles.Any, _usCulture, out value))
                            {
                                hasMetascore.Metascore = value;
                            }
                        }

                        break;
                    }

                case "AwardSummary":
                    {
                        var text = reader.ReadElementContentAsString();
                        var hasAwards = item as IHasAwards;
                        if (hasAwards != null)
                        {
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                hasAwards.AwardSummary = text;
                            }
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

                case "ShortOverview":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            var hasShortOverview = item as IHasShortOverview;

                            if (hasShortOverview != null)
                            {
                                hasShortOverview.ShortOverview = val;
                            }
                        }

                        break;
                    }

                case "CriticRatingSummary":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            var hasCriticRating = item as IHasCriticRating;

                            if (hasCriticRating != null)
                            {
                                hasCriticRating.CriticRatingSummary = val;
                            }
                        }

                        break;
                    }

                case "Language":
                    {
                        var val = reader.ReadElementContentAsString();

                        item.PreferredMetadataLanguage = val;

                        break;
                    }

                case "CountryCode":
                    {
                        var val = reader.ReadElementContentAsString();

                        item.PreferredMetadataCountryCode = val;

                        break;
                    }

                case "PlaceOfBirth":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            var person = item as Person;
                            if (person != null)
                            {
                                person.PlaceOfBirth = val;
                            }
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

                case "Countries":
                    {
                        using (var subtree = reader.ReadSubtree())
                        {
                            FetchFromCountriesNode(subtree, item);
                        }
                        break;
                    }

                case "ContentRating":
                case "MPAARating":
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

                case "RunningTime":
                    {
                        var text = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            int runtime;
                            if (int.TryParse(text.Split(' ')[0], NumberStyles.Integer, _usCulture, out runtime))
                            {
                                item.RunTimeTicks = TimeSpan.FromMinutes(runtime).Ticks;
                            }
                        }
                        break;
                    }

                case "AspectRatio":
                    {
                        var val = reader.ReadElementContentAsString();

                        var hasAspectRatio = item as IHasAspectRatio;
                        if (!string.IsNullOrWhiteSpace(val) && hasAspectRatio != null)
                        {
                            hasAspectRatio.AspectRatio = val;
                        }
                        break;
                    }

                case "LockData":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.IsLocked = string.Equals("true", val, StringComparison.OrdinalIgnoreCase);
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
                        foreach (var p in SplitNames(reader.ReadElementContentAsString()).Select(v => new Entities.PersonInfo { Name = v.Trim(), Type = PersonType.Director }))
                        {
                            if (string.IsNullOrWhiteSpace(p.Name))
                            {
                                continue;
                            }
                            itemResult.AddPerson(p);
                        }
                        break;
                    }
                case "Writer":
                    {
                        foreach (var p in SplitNames(reader.ReadElementContentAsString()).Select(v => new Entities.PersonInfo { Name = v.Trim(), Type = PersonType.Writer }))
                        {
                            if (string.IsNullOrWhiteSpace(p.Name))
                            {
                                continue;
                            }
                            itemResult.AddPerson(p);
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
                            FetchDataFromPersonsNode(new XmlTextReader(new StringReader("<Persons>" + actors + "</Persons>")), itemResult);
                        }
                        else
                        {
                            // Old-style piped string
                            foreach (var p in SplitNames(actors).Select(v => new Entities.PersonInfo { Name = v.Trim(), Type = PersonType.Actor }))
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

                case "GuestStars":
                    {
                        foreach (var p in SplitNames(reader.ReadElementContentAsString()).Select(v => new Entities.PersonInfo { Name = v.Trim(), Type = PersonType.GuestStar }))
                        {
                            if (string.IsNullOrWhiteSpace(p.Name))
                            {
                                continue;
                            }
                            itemResult.AddPerson(p);
                        }
                        break;
                    }

                case "Trailer":
                    {
                        var val = reader.ReadElementContentAsString();

                        var hasTrailers = item as IHasTrailers;
                        if (hasTrailers != null)
                        {
                            if (!string.IsNullOrWhiteSpace(val))
                            {
                                hasTrailers.AddTrailerUrl(val, false);
                            }
                        }
                        break;
                    }

                case "DisplayOrder":
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

                case "Trailers":
                    {
                        using (var subtree = reader.ReadSubtree())
                        {
                            var hasTrailers = item as IHasTrailers;
                            if (hasTrailers != null)
                            {
                                FetchDataFromTrailersNode(subtree, hasTrailers);
                            }
                        }
                        break;
                    }

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
                case "MusicBrainzAlbumId":
                    {
                        var mbz = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(mbz))
                        {
                            item.SetProviderId(MetadataProviders.MusicBrainzAlbum, mbz);
                        }
                        break;
                    }
                case "MusicBrainzAlbumArtistId":
                    {
                        var mbz = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(mbz))
                        {
                            item.SetProviderId(MetadataProviders.MusicBrainzAlbumArtist, mbz);
                        }
                        break;
                    }
                case "MusicBrainzArtistId":
                    {
                        var mbz = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(mbz))
                        {
                            item.SetProviderId(MetadataProviders.MusicBrainzArtist, mbz);
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
                case "TVRageId":
                    {
                        var id = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(id))
                        {
                            item.SetProviderId(MetadataProviders.TvRage, id);
                        }
                        break;
                    }
                case "AudioDbArtistId":
                    {
                        var id = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(id))
                        {
                            item.SetProviderId(MetadataProviders.AudioDbArtist, id);
                        }
                        break;
                    }
                case "AudioDbAlbumId":
                    {
                        var id = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(id))
                        {
                            item.SetProviderId(MetadataProviders.AudioDbAlbum, id);
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

                case "IMDB":
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

                case "PlotKeywords":
                    {
                        using (var subtree = reader.ReadSubtree())
                        {
                            FetchFromKeywordsNode(subtree, item);
                        }
                        break;
                    }

                case "Persons":
                    {
                        using (var subtree = reader.ReadSubtree())
                        {
                            FetchDataFromPersonsNode(subtree, itemResult);
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

                case "Shares":
                    {
                        using (var subtree = reader.ReadSubtree())
                        {
                            var hasShares = item as IHasShares;
                            if (hasShares != null)
                            {
                                FetchFromSharesNode(subtree, hasShares);
                            }
                        }
                        break;
                    }

                case "Format3D":
                    {
                        var video = item as Video;

                        if (video != null)
                        {
                            var val = reader.ReadElementContentAsString();

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

                default:
                    reader.Skip();
                    break;
            }
        }

        private void FetchFromSharesNode(XmlReader reader, IHasShares item)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Share":
                            {
                                using (var subtree = reader.ReadSubtree())
                                {
                                    var share = GetShareFromNode(subtree);
                                    if (share != null)
                                    {
                                        item.Shares.Add(share);
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

        private Share GetShareFromNode(XmlReader reader)
        {
            var share = new Share();

            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "UserId":
                            {
                                share.UserId = reader.ReadElementContentAsString();
                                break;
                            }

                        case "CanEdit":
                            {
                                share.CanEdit = string.Equals(reader.ReadElementContentAsString(), true.ToString(), StringComparison.OrdinalIgnoreCase);
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }

            return share;
        }

        private void FetchFromCountriesNode(XmlReader reader, T item)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Country":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    var hasProductionLocations = item as IHasProductionLocations;
                                    if (hasProductionLocations != null)
                                    {
                                        if (!string.IsNullOrWhiteSpace(val))
                                        {
                                            hasProductionLocations.AddProductionLocation(val);
                                        }
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
                                    var hasTaglines = item as IHasTaglines;
                                    if (hasTaglines != null)
                                    {
                                        if (!string.IsNullOrWhiteSpace(val))
                                        {
                                            hasTaglines.AddTagline(val);
                                        }
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

        private void FetchFromTagsNode(XmlReader reader, BaseItem item)
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

        private void FetchFromKeywordsNode(XmlReader reader, BaseItem item)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "PlotKeyword":
                            {
                                var tag = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(tag))
                                {
                                    item.AddKeyword(tag);
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
        private void FetchDataFromPersonsNode(XmlReader reader, MetadataResult<T> item)
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
                                        if (string.IsNullOrWhiteSpace(person.Name))
                                        {
                                            continue;
                                        }
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

        private void FetchDataFromTrailersNode(XmlReader reader, IHasTrailers item)
        {
            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Trailer":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    item.AddTrailerUrl(val, false);
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

        protected List<ChapterInfo> FetchChaptersFromXmlNode(BaseItem item, XmlReader reader)
        {
            using (reader)
            {
                return GetChaptersFromXmlNode(reader)
                    .Where(i => i.StartPositionTicks >= 0)
                    .ToList();
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
        /// Gets the persons from XML node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>IEnumerable{PersonInfo}.</returns>
        private IEnumerable<PersonInfo> GetPersonsFromXmlNode(XmlReader reader)
        {
            var name = string.Empty;
            var type = PersonType.Actor;  // If type is not specified assume actor
            var role = string.Empty;
            int? sortOrder = null;

            reader.MoveToContent();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Name":
                            name = reader.ReadElementContentAsString() ?? string.Empty;
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
                        case "SortOrder":
                            {
                                var val = reader.ReadElementContentAsString();

                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    int intVal;
                                    if (int.TryParse(val, NumberStyles.Integer, _usCulture, out intVal))
                                    {
                                        sortOrder = intVal;
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

            var personInfo = new PersonInfo
            {
                Name = name.Trim(),
                Role = role,
                Type = type,
                SortOrder = sortOrder
            };

            return new[] { personInfo };
        }

        protected LinkedChild GetLinkedChild(XmlReader reader)
        {
            reader.MoveToContent();

            var linkedItem = new LinkedChild
            {
                Type = LinkedChildType.Manual
            };

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Path":
                            {
                                linkedItem.Path = reader.ReadElementContentAsString();
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }

            // This is valid
            if (!string.IsNullOrWhiteSpace(linkedItem.Path))
            {
                return linkedItem;
            }

            return null;
        }

        protected Share GetShare(XmlReader reader)
        {
            reader.MoveToContent();

            var item = new Share();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "UserId":
                            {
                                item.UserId = reader.ReadElementContentAsString();
                                break;
                            }

                        case "CanEdit":
                            {
                                item.CanEdit = string.Equals(reader.ReadElementContentAsString(), "true", StringComparison.OrdinalIgnoreCase);
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }

            // This is valid
            if (!string.IsNullOrWhiteSpace(item.UserId))
            {
                return item;
            }

            return null;
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
