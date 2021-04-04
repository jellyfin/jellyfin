using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.XbmcMetadata.Savers;

namespace MediaBrowser.XbmcMetadata.Parsers
{
    internal static class NfoParserHelpers
    {
        internal static string? ReadStringFromNfo(this XmlReader xmlReader)
        {
            var value = xmlReader.ReadElementContentAsString();
            return value;
        }

        internal static int? ReadIntFromNfo(this XmlReader xmlReader)
        {
            var str = xmlReader.ReadElementContentAsString();
            if (int.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            return default;
        }

        internal static float? ReadFloatFromNfo(this XmlReader xmlReader)
        {
            var str = xmlReader.ReadElementContentAsString();
            if (float.TryParse(str, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            return default;
        }

        internal static DateTime? ReadDateFromNfo(this XmlReader xmlReader)
        {
            var str = xmlReader.ReadElementContentAsString();
            if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var value))
            {
                return value.ToUniversalTime();
            }

            return default;
        }

        internal static bool? ReadBoolFromNfo(this XmlReader xmlReader)
        {
            var value = xmlReader.ReadElementContentAsBoolean();
            return value;
        }

        internal static void ReadProviderIdFromNfo(this XmlReader xmlReader, IHasProviderIds item, Dictionary<string, string> providerIds)
        {
            string readerName = xmlReader.Name;
            if (providerIds.TryGetValue(readerName, out string? providerIdValue))
            {
                var id = xmlReader.ReadElementContentAsString();
                if (!string.IsNullOrWhiteSpace(providerIdValue) && !string.IsNullOrWhiteSpace(id))
                {
                    item.SetProviderId(providerIdValue, id);
                }
            }
            else
            {
                xmlReader.Skip();
            }
        }

        internal static string? ReadTrailerUrlFromNfo(this XmlReader reader)
        {
            var val = reader.ReadStringFromNfo();

            if (!string.IsNullOrWhiteSpace(val))
            {
                return val.Replace(BaseNfoSaver.KodiYouTubeWatchUrl, BaseNfoSaver.YouTubeWatchUrl, StringComparison.OrdinalIgnoreCase);
            }

            return default;
        }

        internal static string[] ReadStringArrayFromNfo(this XmlReader reader)
        {
            var val = reader.ReadElementContentAsString();

            if (!string.IsNullOrWhiteSpace(val))
            {
                return val.Split('/')
                    .Select(i => i.Trim())
                    .Where(i => !string.IsNullOrWhiteSpace(i))
                    .ToArray();
            }

            return Array.Empty<string>();
        }

        internal static void ReadUniqueIdFromNfo(this XmlReader reader, BaseItem item)
        {
            if (reader.IsEmptyElement)
            {
                reader.Read();
                return;
            }

            var provider = reader.GetAttribute("type");
            var id = reader.ReadElementContentAsString();
            if (!string.IsNullOrWhiteSpace(provider) && !string.IsNullOrWhiteSpace(id))
            {
                item.SetProviderId(provider, id);
            }
        }

        internal static void SetSeriesIds(XmlReader reader, BaseItem item)
        {
            string? imdbId = reader.GetAttribute("IMDB");
            string? tmdbId = reader.GetAttribute("TMDB");
            string? tvdbId = reader.GetAttribute("TVDB");

            if (string.IsNullOrWhiteSpace(tvdbId))
            {
                tvdbId = reader.ReadElementContentAsString();
            }

            if (!string.IsNullOrWhiteSpace(imdbId))
            {
                item.SetProviderId(MetadataProvider.Imdb, imdbId);
            }

            if (!string.IsNullOrWhiteSpace(tmdbId))
            {
                item.SetProviderId(MetadataProvider.Tmdb, tmdbId);
            }

            if (!string.IsNullOrWhiteSpace(tvdbId))
            {
                item.SetProviderId(MetadataProvider.Tvdb, tvdbId);
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

            value = value.Trim().Trim(separator);

            return string.IsNullOrWhiteSpace(value) ? Array.Empty<string>() : value.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        }

        internal static MetadataField[] ParseLockedFields(string lockedFields)
        {
            if (!string.IsNullOrWhiteSpace(lockedFields))
            {
                return lockedFields.Split('|').Select(i =>
                {
                    if (Enum.TryParse(i, true, out MetadataField field))
                    {
                        return (MetadataField?)field;
                    }

                    return null;
                }).OfType<MetadataField>().ToArray();
            }

            return Array.Empty<MetadataField>();
        }
    }
}
