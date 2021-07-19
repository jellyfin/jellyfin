using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.XbmcMetadata.Savers;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.XbmcMetadata.Parsers
{
    internal class NfoParserHelpers
    {
        private readonly ILogger _logger;

        public NfoParserHelpers(ILogger logger)
        {
            _logger = logger;
        }

        internal string? ReadStringFromNfo(XmlReader xmlReader)
        {
            var tagName = xmlReader.Name;
            var value = xmlReader.ReadElementContentAsString();
            _logger.LogDebug("Reading string {Value} from {TagName}", value, tagName);
            return value;
        }

        internal int? ReadIntFromNfo(XmlReader xmlReader)
        {
            var tagName = xmlReader.Name;
            var str = xmlReader.ReadElementContentAsString();
            if (int.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            {
                _logger.LogDebug("Reading int {Value} from {TagName}", value, tagName);
                return value;
            }

            return default;
        }

        internal float? ReadFloatFromNfo(XmlReader xmlReader)
        {
            var tagName = xmlReader.Name;
            var str = xmlReader.ReadElementContentAsString();
            if (float.TryParse(str, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
            {
                _logger.LogDebug("Reading float {Value} from {TagName}", value, tagName);
                return value;
            }

            return default;
        }

        internal DateTime? ReadDateFromNfo(XmlReader xmlReader)
        {
            var tagName = xmlReader.Name;
            var str = xmlReader.ReadElementContentAsString();
            if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            {
                var value = parsed.ToUniversalTime();
                _logger.LogDebug("Reading date {Value} from {TagName}", value, tagName);
                return value;
            }

            return default;
        }

        internal bool? ReadBoolFromNfo(XmlReader xmlReader)
        {
            var tagName = xmlReader.Name;
            var value = xmlReader.ReadElementContentAsBoolean();
            _logger.LogDebug("Reading bool {Value} from {TagName}", value, tagName);
            return value;
        }

        internal void ReadProviderIdFromNfo(XmlReader xmlReader, IHasProviderIds item, Dictionary<string, string> providerIds)
        {
            string tagName = xmlReader.Name;
            if (providerIds.TryGetValue(tagName, out string? providerIdValue))
            {
                var id = xmlReader.ReadElementContentAsString();
                if (!string.IsNullOrWhiteSpace(id))
                {
                    _logger.LogDebug("Setting {Provider} id to {Id}", providerIdValue, id);
                    item.SetProviderId(providerIdValue, id);
                }
            }
            else
            {
                xmlReader.Skip();
            }
        }

        internal string? ReadTrailerUrlFromNfo(XmlReader xmlReader)
        {
            var val = this.ReadStringFromNfo(xmlReader);

            if (!string.IsNullOrWhiteSpace(val))
            {
                var trailerUrl = val.Replace(BaseNfoSaver.KodiYouTubeWatchUrl, BaseNfoSaver.YouTubeWatchUrl, StringComparison.OrdinalIgnoreCase);
                _logger.LogDebug("Setting trailer url to {TrailerUrl}", trailerUrl);
                return trailerUrl;
            }

            return default;
        }

        internal string[] ReadStringArrayFromNfo(XmlReader xmlReader)
        {
            string tagName = xmlReader.Name;
            var val = xmlReader.ReadElementContentAsString();

            if (!string.IsNullOrWhiteSpace(val))
            {
                var array = val.Split('/')
                    .Select(i => i.Trim())
                    .Where(i => !string.IsNullOrWhiteSpace(i))
                    .ToArray();
                _logger.LogDebug("Setting {TagName} to {Array}", tagName, array);
                return array;
            }

            return Array.Empty<string>();
        }

        internal void ReadUniqueIdFromNfo(XmlReader xmlReader, BaseItem item)
        {
            if (xmlReader.IsEmptyElement)
            {
                xmlReader.Read();
                return;
            }

            var provider = xmlReader.GetAttribute("type");
            var id = xmlReader.ReadElementContentAsString();
            if (!string.IsNullOrWhiteSpace(provider) && !string.IsNullOrWhiteSpace(id))
            {
                item.SetProviderId(provider, id);
                _logger.LogDebug("Setting {Provider} id to {Id}", provider, id);
            }
        }

        internal void SetMovieids(XmlReader xmlReader, BaseItem item)
        {
            // get ids from attributes
            string? imdbId = xmlReader.GetAttribute("IMDB");
            string? tmdbId = xmlReader.GetAttribute("TMDB");

            // read id from content
            var contentId = xmlReader.ReadElementContentAsString();
            if (string.IsNullOrEmpty(imdbId) && contentId.Contains("tt", StringComparison.Ordinal))
            {
                imdbId = contentId;
            }
            else if (string.IsNullOrEmpty(tmdbId))
            {
                tmdbId = contentId;
            }

            if (!string.IsNullOrWhiteSpace(imdbId))
            {
                item.SetProviderId(MetadataProvider.Imdb, imdbId);
                _logger.LogDebug("Setting Imdb id to {Id}", imdbId);
            }

            if (!string.IsNullOrWhiteSpace(tmdbId))
            {
                item.SetProviderId(MetadataProvider.Tmdb, tmdbId);
                _logger.LogDebug("Setting Tmdb id to {Id}", tmdbId);
            }
        }

        internal void SetSeriesIds(XmlReader xmlReader, BaseItem item)
        {
            string? imdbId = xmlReader.GetAttribute("IMDB");
            string? tmdbId = xmlReader.GetAttribute("TMDB");
            string? tvdbId = xmlReader.GetAttribute("TVDB");

            if (string.IsNullOrWhiteSpace(tvdbId))
            {
                tvdbId = xmlReader.ReadElementContentAsString();
            }

            if (!string.IsNullOrWhiteSpace(imdbId))
            {
                item.SetProviderId(MetadataProvider.Imdb, imdbId);
                _logger.LogDebug("Setting Imdb id to {Id}", imdbId);
            }

            if (!string.IsNullOrWhiteSpace(tmdbId))
            {
                item.SetProviderId(MetadataProvider.Tmdb, tmdbId);
                _logger.LogDebug("Setting Tmdb id to {Id}", tmdbId);
            }

            if (!string.IsNullOrWhiteSpace(tvdbId))
            {
                item.SetProviderId(MetadataProvider.Tvdb, tvdbId);
                _logger.LogDebug("Setting Tvdb id to {Id}", tvdbId);
            }
        }

        /// <summary>
        /// Parses the ImageType from the nfo aspect property.
        /// </summary>
        /// <param name="aspect">The nfo aspect property.</param>
        /// <returns>The image type.</returns>
        internal static ImageType GetImageType(string aspect)
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

        internal static string GetPersonType(string type)
        {
            return type switch
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

        /// <summary>
        /// Used to split names of comma or pipe delimeted genres and people.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>IEnumerable{System.String}.</returns>
        internal static IEnumerable<string> SplitNames(string value)
        {
            // Only split by comma if there is no pipe in the string
            // We have to be careful to not split names like Matthew, Jr.
            var separator = !value.Contains('|', StringComparison.Ordinal) && !value.Contains(';', StringComparison.Ordinal)
                ? new[] { ',' }
                : new[] { '|', ';' };

            return value.Trim().Split(separator, StringSplitOptions.RemoveEmptyEntries);
        }

        internal static MetadataField[] ParseLockedFields(string lockedFields)
        {
            if (!string.IsNullOrWhiteSpace(lockedFields))
            {
                return lockedFields.Split('|').Select(i =>
                {
                    return Enum.TryParse(i, true, out MetadataField field) ? (MetadataField?)field : null;
                }).OfType<MetadataField>().ToArray();
            }

            return Array.Empty<MetadataField>();
        }
    }
}
