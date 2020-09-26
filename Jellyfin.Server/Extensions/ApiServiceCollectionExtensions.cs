using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using Jellyfin.Api.Auth;
using Jellyfin.Api.Auth.DefaultAuthorizationPolicy;
using Jellyfin.Api.Auth.DownloadPolicy;
using Jellyfin.Api.Auth.FirstTimeOrIgnoreParentalControlSetupPolicy;
using Jellyfin.Api.Auth.FirstTimeSetupOrDefaultPolicy;
using Jellyfin.Api.Auth.FirstTimeSetupOrElevatedPolicy;
using Jellyfin.Api.Auth.IgnoreParentalControlPolicy;
using Jellyfin.Api.Auth.LocalAccessOrRequiresElevationPolicy;
using Jellyfin.Api.Auth.LocalAccessPolicy;
using Jellyfin.Api.Auth.RequiresElevationPolicy;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Controllers;
using Jellyfin.Server.Configuration;
using Jellyfin.Server.Filters;
using Jellyfin.Server.Formatters;
using MediaBrowser.Common.Json;
using MediaBrowser.Model.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using AuthenticationSchemes = Jellyfin.Api.Constants.AuthenticationSchemes;

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
            serviceCollection.AddSingleton<IAuthorizationHandler, DefaultAuthorizationHandler>();
            serviceCollection.AddSingleton<IAuthorizationHandler, DownloadHandler>();
            serviceCollection.AddSingleton<IAuthorizationHandler, FirstTimeSetupOrDefaultHandler>();
            serviceCollection.AddSingleton<IAuthorizationHandler, FirstTimeSetupOrElevatedHandler>();
            serviceCollection.AddSingleton<IAuthorizationHandler, IgnoreParentalControlHandler>();
            serviceCollection.AddSingleton<IAuthorizationHandler, FirstTimeOrIgnoreParentalControlSetupHandler>();
            serviceCollection.AddSingleton<IAuthorizationHandler, LocalAccessHandler>();
            serviceCollection.AddSingleton<IAuthorizationHandler, LocalAccessOrRequiresElevationHandler>();
            serviceCollection.AddSingleton<IAuthorizationHandler, RequiresElevationHandler>();
            return serviceCollection.AddAuthorizationCore(options =>
            {
                options.AddPolicy(
                    Policies.DefaultAuthorization,
                    policy =>
                    {
                        policy.AddAuthenticationSchemes(AuthenticationSchemes.CustomAuthentication);
                        policy.AddRequirements(new DefaultAuthorizationRequirement());
                    });
                options.AddPolicy(
                    Policies.Download,
                    policy =>
                    {
                        policy.AddAuthenticationSchemes(AuthenticationSchemes.CustomAuthentication);
                        policy.AddRequirements(new DownloadRequirement());
                    });
                options.AddPolicy(
                    Policies.FirstTimeSetupOrDefault,
                    policy =>
                    {
                        policy.AddAuthenticationSchemes(AuthenticationSchemes.CustomAuthentication);
                        policy.AddRequirements(new FirstTimeSetupOrDefaultRequirement());
                    });
                options.AddPolicy(
                    Policies.FirstTimeSetupOrElevated,
                    policy =>
                    {
                        policy.AddAuthenticationSchemes(AuthenticationSchemes.CustomAuthentication);
                        policy.AddRequirements(new FirstTimeSetupOrElevatedRequirement());
                    });
                options.AddPolicy(
                    Policies.IgnoreParentalControl,
                    policy =>
                    {
                        policy.AddAuthenticationSchemes(AuthenticationSchemes.CustomAuthentication);
                        policy.AddRequirements(new IgnoreParentalControlRequirement());
                    });
                options.AddPolicy(
                    Policies.FirstTimeSetupOrIgnoreParentalControl,
                    policy =>
                    {
                        policy.AddAuthenticationSchemes(AuthenticationSchemes.CustomAuthentication);
                        policy.AddRequirements(new FirstTimeOrIgnoreParentalControlSetupRequirement());
                    });
                options.AddPolicy(
                    Policies.LocalAccessOnly,
                    policy =>
                    {
                        policy.AddAuthenticationSchemes(AuthenticationSchemes.CustomAuthentication);
                        policy.AddRequirements(new LocalAccessRequirement());
                    });
                options.AddPolicy(
                    Policies.LocalAccessOrRequiresElevation,
                    policy =>
                    {
                        policy.AddAuthenticationSchemes(AuthenticationSchemes.CustomAuthentication);
                        policy.AddRequirements(new LocalAccessOrRequiresElevationRequirement());
                    });
                options.AddPolicy(
                    Policies.RequiresElevation,
                    policy =>
                    {
                        policy.AddAuthenticationSchemes(AuthenticationSchemes.CustomAuthentication);
                        policy.AddRequirements(new RequiresElevationRequirement());
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
        /// <param name="pluginAssemblies">An IEnumerable containing all plugin assemblies with API controllers.</param>
        /// <param name="knownProxies">A list of all known proxies to trust for X-Forwarded-For.</param>
        /// <returns>The MVC builder.</returns>
        public static IMvcBuilder AddJellyfinApi(this IServiceCollection serviceCollection, IEnumerable<Assembly> pluginAssemblies, IReadOnlyList<string> knownProxies)
        {
            IMvcBuilder mvcBuilder = serviceCollection
                .AddCors()
                .AddTransient<ICorsPolicyProvider, CorsPolicyProvider>()
                .Configure<ForwardedHeadersOptions>(options =>
                {
                    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                    for (var i = 0; i < knownProxies.Count; i++)
                    {
                        if (IPAddress.TryParse(knownProxies[i], out var address))
                        {
                            options.KnownProxies.Add(address);
                        }
                    }
                })
                .AddMvc(opts =>
                {
                    // Allow requester to change between camelCase and PascalCase
                    opts.RespectBrowserAcceptHeader = true;

                    opts.OutputFormatters.Insert(0, new CamelCaseJsonProfileFormatter());
                    opts.OutputFormatters.Insert(0, new PascalCaseJsonProfileFormatter());

                    opts.OutputFormatters.Add(new CssOutputFormatter());
                    opts.OutputFormatters.Add(new XmlOutputFormatter());
                })

                // Clear app parts to avoid other assemblies being picked up
                .ConfigureApplicationPartManager(a => a.ApplicationParts.Clear())
                .AddApplicationPart(typeof(StartupController).Assembly)
                .AddJsonOptions(options =>
                {
                    // Update all properties that are set in JsonDefaults
                    var jsonOptions = JsonDefaults.GetPascalCaseOptions();

                    // From JsonDefaults
                    options.JsonSerializerOptions.ReadCommentHandling = jsonOptions.ReadCommentHandling;
                    options.JsonSerializerOptions.WriteIndented = jsonOptions.WriteIndented;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = jsonOptions.DefaultIgnoreCondition;
                    options.JsonSerializerOptions.NumberHandling = jsonOptions.NumberHandling;

                    options.JsonSerializerOptions.Converters.Clear();
                    foreach (var converter in jsonOptions.Converters)
                    {
                        options.JsonSerializerOptions.Converters.Add(converter);
                    }

                    // From JsonDefaults.PascalCase
                    options.JsonSerializerOptions.PropertyNamingPolicy = jsonOptions.PropertyNamingPolicy;
                });

            foreach (Assembly pluginAssembly in pluginAssemblies)
            {
                mvcBuilder.AddApplicationPart(pluginAssembly);
            }

            return mvcBuilder.AddControllersAsServices();
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
                c.SwaggerDoc("api-docs", new OpenApiInfo { Title = "Jellyfin API", Version = "v1" });
                c.AddSecurityDefinition(AuthenticationSchemes.CustomAuthentication, new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Name = "X-Emby-Authorization",
                    Description = "API key header parameter"
                });

                var securitySchemeRef = new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = AuthenticationSchemes.CustomAuthentication },
                };

                // TODO: Apply this with an operation filter instead of globally
                // https://github.com/domaindrivendev/Swashbuckle.AspNetCore#add-security-definitions-and-requirements
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { securitySchemeRef, Array.Empty<string>() }
                });

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
                    $"{description.ActionDescriptor.RouteValues["controller"]}_{description.RelativePath}");

                // Use method name as operationId
                c.CustomOperationIds(
                    description =>
                    {
                        description.TryGetMethodInfo(out MethodInfo methodInfo);
                        // Attribute name, method name, none.
                        return description?.ActionDescriptor?.AttributeRouteInfo?.Name
                               ?? methodInfo?.Name
                               ?? null;
                    });

                // TODO - remove when all types are supported in System.Text.Json
                c.AddSwaggerTypeMappings();

                c.OperationFilter<FileResponseFilter>();
            });
        }

        private static void AddSwaggerTypeMappings(this SwaggerGenOptions options)
        {
            /*
             * TODO remove when System.Text.Json supports non-string keys.
             * Used in Jellyfin.Api.Controller.GetChannels.
             */
            options.MapType<Dictionary<ImageType, string>>(() =>
                new OpenApiSchema
                {
                    Type = "object",
                    Properties = typeof(ImageType).GetEnumNames().ToDictionary(
                        name => name,
                        name => new OpenApiSchema
                        {
                            Type = "string",
                            Format = "string"
                        })
                });

            /*
             * Support BlurHash dictionary
             */
            options.MapType<Dictionary<ImageType, Dictionary<string, string>>>(() =>
                new OpenApiSchema
                {
                    Type = "object",
                    Properties = typeof(ImageType).GetEnumNames().ToDictionary(
                        name => name,
                        name => new OpenApiSchema
                        {
                            Type = "object", Properties = new Dictionary<string, OpenApiSchema>
                            {
                                {
                                    "string",
                                    new OpenApiSchema
                                    {
                                        Type = "string",
                                        Format = "string"
                                    }
                                }
                            }
                        })
                });
        }
    }
}
