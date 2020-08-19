using System;
using System.ComponentModel;
using System.Globalization;

namespace Jellyfin.Api.TypeConverters
{
    /// <summary>
    /// Custom datetime parser.
    /// </summary>
    public class DateTimeTypeConverter : TypeConverter
    {
        /// <inheritdoc />
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }

        /// <inheritdoc />
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string dateString)
            {
                // Mark Played Item.
                if (DateTime.TryParseExact(dateString, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dateTime))
                {
                    return dateTime;
                }

                // Get Activity Logs.
                if (DateTime.TryParse(dateString, null, DateTimeStyles.RoundtripKind, out dateTime))
                {
                    return dateTime;
                }
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
}
