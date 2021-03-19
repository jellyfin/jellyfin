using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using MediaBrowser.Model.Entities;

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
    }
}
