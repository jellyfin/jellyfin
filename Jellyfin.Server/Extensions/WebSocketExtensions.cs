using Emby.Dlna.ConnectionManager;
using Jellyfin.Api.WebSockets;
using Jellyfin.Server.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Server.Extensions
{
    /// <summary>
    /// Web socket extensions.
    /// </summary>
    public static class WebSocketExtensions
    {
        /// <summary>
        /// Add a socket manager.
        /// </summary>
        /// <param name="services">Instance of the <see cref="IServiceCollection"/> interface.</param>
        /// <returns>Modified <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddWebSocketManager(this IServiceCollection services)
        {
            services.AddTransient<WebSocketConnectionManager>();

            // TODO dynamically add all handlers?
            services.AddSingleton<LegacyWebSocketHandler>();
            return services;
        }

        /// <summary>
        /// Map websocket handler.
        /// </summary>
        /// <param name="app">Instance of the <see cref="IApplicationBuilder"/> interface.</param>
        /// <param name="path">Path to map to.</param>
        /// <param name="handler">Socket handler to map.</param>
        /// <returns>Modified <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder MapWebSocketManager(
            this IApplicationBuilder app,
            PathString path,
            BaseWebSocketHandler handler)
        {
            return app.Map(path, a => a.UseMiddleware<WebSocketManagerMiddleware>(handler));
        }
    }
}
