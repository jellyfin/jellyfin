using Jellyfin.MediaEncoding.Hls.Playlist;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.MediaEncoding.Hls.Extensions
{
    /// <summary>
    /// Extensions for the <see cref="IServiceCollection"/> interface.
    /// </summary>
    public static class MediaEncodingHlsServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the hls playlist generators to the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="serviceCollection">An instance of the <see cref="IServiceCollection"/> interface.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddHlsPlaylistGenerator(this IServiceCollection serviceCollection)
        {
            return serviceCollection.AddSingleton<IDynamicHlsPlaylistGenerator, DynamicHlsPlaylistGenerator>();
        }
    }
}
