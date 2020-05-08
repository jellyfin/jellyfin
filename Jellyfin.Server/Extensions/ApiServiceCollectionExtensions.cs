using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using Jellyfin.Api;
using Jellyfin.Api.Auth;
using Jellyfin.Api.Auth.FirstTimeSetupOrElevatedPolicy;
using Jellyfin.Api.Auth.RequiresElevationPolicy;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Controllers;
using Jellyfin.Server.Formatters;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Jellyfin.Server.Extensions
{
    /// <summary>
    /// API specific extensions for the service collection.
    /// </summary>
    public static class ApiServiceCollectionExtensions
    {
        /// <summary>
        /// Adds jellyfin API authorization policies to the DI container.
        /// </summary>
        /// <param name="serviceCollection">The service collection.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddJellyfinApiAuthorization(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IAuthorizationHandler, FirstTimeSetupOrElevatedHandler>();
            serviceCollection.AddSingleton<IAuthorizationHandler, RequiresElevationHandler>();
            return serviceCollection.AddAuthorizationCore(options =>
            {
                options.AddPolicy(
                    Policies.RequiresElevation,
                    policy =>
                    {
                        policy.AddAuthenticationSchemes(AuthenticationSchemes.CustomAuthentication);
                        policy.AddRequirements(new RequiresElevationRequirement());
                    });
                options.AddPolicy(
                    Policies.FirstTimeSetupOrElevated,
                    policy =>
                    {
                        policy.AddAuthenticationSchemes(AuthenticationSchemes.CustomAuthentication);
                        policy.AddRequirements(new FirstTimeSetupOrElevatedRequirement());
                    });
            });
        }

        /// <summary>
        /// Adds custom legacy authentication to the service collection.
        /// </summary>
        /// <param name="serviceCollection">The service collection.</param>
        /// <returns>The updated service collection.</returns>
        public static AuthenticationBuilder AddCustomAuthentication(this IServiceCollection serviceCollection)
        {
            return serviceCollection.AddAuthentication(AuthenticationSchemes.CustomAuthentication)
                .AddScheme<AuthenticationSchemeOptions, CustomAuthenticationHandler>(AuthenticationSchemes.CustomAuthentication, null);
        }

        /// <summary>
        /// Extension method for adding the jellyfin API to the service collection.
        /// </summary>
        /// <param name="serviceCollection">The service collection.</param>
        /// <param name="baseUrl">The base url for the API.</param>
        /// <returns>The MVC builder.</returns>
        public static IMvcBuilder AddJellyfinApi(this IServiceCollection serviceCollection, string baseUrl)
        {
            return serviceCollection.AddMvc(opts =>
                {
                    opts.UseGeneralRoutePrefix(baseUrl);
                    opts.OutputFormatters.Insert(0, new CamelCaseJsonProfileFormatter());
                    opts.OutputFormatters.Insert(0, new PascalCaseJsonProfileFormatter());
                })

                // Clear app parts to avoid other assemblies being picked up
                .ConfigureApplicationPartManager(a => a.ApplicationParts.Clear())
                .AddApplicationPart(typeof(StartupController).Assembly)
                .AddJsonOptions(options =>
                {
                    // Setting the naming policy to null leaves the property names as-is when serializing objects to JSON.
                    options.JsonSerializerOptions.PropertyNamingPolicy = null;
                })
                .AddControllersAsServices();
        }

        /// <summary>
        /// Adds Swagger to the service collection.
        /// </summary>
        /// <param name="serviceCollection">The service collection.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddJellyfinApiSwagger(this IServiceCollection serviceCollection)
        {
            return serviceCollection.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("api-docs", new OpenApiInfo { Title = "Jellyfin API" });

                // Add all xml doc files to swagger generator.
                var xmlFiles = Directory.GetFiles(
                    AppContext.BaseDirectory,
                    "*.xml",
                    SearchOption.TopDirectoryOnly);

                foreach (var xmlFile in xmlFiles)
                {
                    c.IncludeXmlComments(xmlFile);
                }

                // Order actions by route path, then by http method.
                c.OrderActionsBy(description =>
                    $"{description.ActionDescriptor.RouteValues["controller"]}_{description.HttpMethod}");

                // Use method name as operationId
                c.CustomOperationIds(description =>
                    description.TryGetMethodInfo(out MethodInfo methodInfo) ? methodInfo.Name : null);
            });
        }
    }
}
