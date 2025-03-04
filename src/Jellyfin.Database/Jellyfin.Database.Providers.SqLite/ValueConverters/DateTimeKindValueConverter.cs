using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Jellyfin.Server.Implementations.ValueConverters
{
    /// <summary>
    /// ValueConverter to specify kind.
    /// </summary>
    public class DateTimeKindValueConverter : ValueConverter<DateTime, DateTime>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DateTimeKindValueConverter"/> class.
        /// </summary>
        /// <param name="kind">The kind to specify.</param>
        /// <param name="mappingHints">The mapping hints.</param>
        public DateTimeKindValueConverter(DateTimeKind kind, ConverterMappingHints? mappingHints = null)
            : base(v => v.ToUniversalTime(), v => DateTime.SpecifyKind(v, kind), mappingHints)
        {
        }
    }
}
