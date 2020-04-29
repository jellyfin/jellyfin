using MediaBrowser.Controller.Configuration;
using Jellyfin.Server.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Jellyfin.Server.Extensions
{
    /// <summary>
    /// Extensions for adding API specific functionality to the application pipeline.
    /// </summary>
    public static class ApiApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds swagger and swagger UI to the application pipeline.
        /// </summary>
        /// <param name="applicationBuilder">The application builder.</param>
        /// <param name="serverConfigurationManager">The server configuration.</param>
        /// <returns>The updated application builder.</returns>
        public static IApplicationBuilder UseJellyfinApiSwagger(
            this IApplicationBuilder applicationBuilder,
            IServerConfigurationManager serverConfigurationManager)
        {
            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.

            var baseUrl = serverConfigurationManager.Configuration.BaseUrl.Trim('/');
            if (!string.IsNullOrEmpty(baseUrl))
            {
                baseUrl += '/';
            }

            return applicationBuilder
                .UseSwagger(c =>
                {
                    c.RouteTemplate = $"/{baseUrl}api-docs/{{documentName}}/openapi.json";
                })
                .UseSwaggerUI(c =>
                {
                    c.DocumentTitle = "Jellyfin API v1";
                    c.SwaggerEndpoint($"/{baseUrl}api-docs/v1/openapi.json", "Jellyfin API v1");
                    c.RoutePrefix = $"{baseUrl}api-docs/v1/swagger";
                })
                .UseReDoc(c =>
                {
                    c.DocumentTitle = "Jellyfin API v1";
                    c.SpecUrl($"/{baseUrl}api-docs/v1/openapi.json");
                    c.RoutePrefix = $"{baseUrl}api-docs/v1/redoc";
                });
        }
    }
}
