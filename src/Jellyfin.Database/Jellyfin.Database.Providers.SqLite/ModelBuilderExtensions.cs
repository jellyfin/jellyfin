using System;
using Jellyfin.Server.Implementations.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Jellyfin.Server.Implementations
{
    /// <summary>
    /// Model builder extensions.
    /// </summary>
    public static class ModelBuilderExtensions
    {
        /// <summary>
        /// Specify value converter for the object type.
        /// </summary>
        /// <param name="modelBuilder">The model builder.</param>
        /// <param name="converter">The <see cref="ValueConverter{TModel,TProvider}"/>.</param>
        /// <typeparam name="T">The type to convert.</typeparam>
        /// <returns>The modified <see cref="ModelBuilder"/>.</returns>
        public static ModelBuilder UseValueConverterForType<T>(this ModelBuilder modelBuilder, ValueConverter converter)
        {
            var type = typeof(T);
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == type)
                    {
                        property.SetValueConverter(converter);
                    }
                }
            }

            return modelBuilder;
        }

        /// <summary>
        /// Specify the default <see cref="DateTimeKind"/>.
        /// </summary>
        /// <param name="modelBuilder">The model builder to extend.</param>
        /// <param name="kind">The <see cref="DateTimeKind"/> to specify.</param>
        public static void SetDefaultDateTimeKind(this ModelBuilder modelBuilder, DateTimeKind kind)
        {
            modelBuilder.UseValueConverterForType<DateTime>(new DateTimeKindValueConverter(kind));
            modelBuilder.UseValueConverterForType<DateTime?>(new DateTimeKindValueConverter(kind));
        }
    }
}
