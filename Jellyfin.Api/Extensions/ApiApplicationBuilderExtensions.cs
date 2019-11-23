using Microsoft.AspNetCore.Builder;

namespace Jellyfin.Api.Extensions
{
    public static class ApiApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseJellyfinApiSwagger(this IApplicationBuilder applicationBuilder)
        {
            applicationBuilder.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            return applicationBuilder.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Jellyfin API V1");
            });
        }
    }
}
