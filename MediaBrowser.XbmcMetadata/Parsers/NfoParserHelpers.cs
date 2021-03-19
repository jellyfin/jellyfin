using System;
using System.Globalization;
using System.Xml;

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
    }
}
