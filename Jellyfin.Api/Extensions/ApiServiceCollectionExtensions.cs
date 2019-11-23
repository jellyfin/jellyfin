using Emby.Server.Implementations;
using Jellyfin.Api.Auth;
using Jellyfin.Api.Auth.FirstTimeSetupOrElevatedPolicy;
using Jellyfin.Api.Auth.RequiresElevationPolicy;
using Jellyfin.Api.Controllers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Jellyfin.Api.Extensions
{
    public static class ApiServiceCollectionExtensions
    {
        public static IServiceCollection AddJellyfinApiAuthorization(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IAuthorizationHandler, FirstTimeSetupOrElevatedHandler>();
            serviceCollection.AddSingleton<IAuthorizationHandler, RequiresElevationHandler>();
            return serviceCollection.AddAuthorizationCore(options =>
            {
                options.AddPolicy(
                    "RequiresElevation",
                    policy =>
                    {
                        policy.AddAuthenticationSchemes("CustomAuthentication");
                        policy.AddRequirements(new RequiresElevationRequirement());
                    });
                options.AddPolicy(
                    "FirstTimeSetupOrElevated",
                    policy =>
                    {
                        policy.AddAuthenticationSchemes("CustomAuthentication");
                        policy.AddRequirements(new FirstTimeSetupOrElevatedRequirement());
                    });
            });
        }

        public static AuthenticationBuilder AddCustomAuthentication(this IServiceCollection serviceCollection)
        {
            return serviceCollection.AddAuthentication("CustomAuthentication")
                .AddScheme<AuthenticationSchemeOptions, CustomAuthenticationHandler>("CustomAuthentication", null);
        }

        public static IMvcBuilder AddJellyfinApi(this IServiceCollection serviceCollection, string baseUrl)
        {
            return serviceCollection.AddMvc(opts =>
                {
                    var policy = new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build();
                    opts.Filters.Add(new AuthorizeFilter(policy));
                    opts.EnableEndpointRouting = false;
                    opts.UseGeneralRoutePrefix(baseUrl);
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                // Clear app parts to avoid other assemblies being picked up
                .ConfigureApplicationPartManager(a => a.ApplicationParts.Clear())
                .AddApplicationPart(typeof(StartupController).Assembly)
                .AddControllersAsServices();
        }

        public static IServiceCollection AddJellyfinApiSwagger(this IServiceCollection serviceCollection)
        {
            return serviceCollection.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Jellyfin API", Version = "v1" });
            });
        }
    }
}
