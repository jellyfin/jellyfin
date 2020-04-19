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
        /// <returns>The updated application builder.</returns>
        public static IApplicationBuilder UseJellyfinApiSwagger(this IApplicationBuilder applicationBuilder)
        {
            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            const string specEndpoint = "/swagger/v1/swagger.json";
            return applicationBuilder
                .UseSwagger()
                .UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint(specEndpoint, "Jellyfin API V1");
                    c.RoutePrefix = "api-docs/swagger";
                })
                .UseReDoc(c =>
                {
                    c.SpecUrl(specEndpoint);
                    c.RoutePrefix = "api-docs/redoc";
                });
        }
    }
}
