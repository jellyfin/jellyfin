using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Claims;
using Emby.Server.Implementations;
using Jellyfin.Api.Auth;
using Jellyfin.Api.Auth.AnonymousLanAccessPolicy;
using Jellyfin.Api.Auth.DefaultAuthorizationPolicy;
using Jellyfin.Api.Auth.FirstTimeSetupPolicy;
using Jellyfin.Api.Auth.LocalAccessOrRequiresElevationPolicy;
using Jellyfin.Api.Auth.SyncPlayAccessPolicy;
using Jellyfin.Api.Auth.UserPermissionPolicy;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Controllers;
using Jellyfin.Api.Formatters;
using Jellyfin.Api.ModelBinders;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions.Json;
using Jellyfin.Server.Configuration;
using Jellyfin.Server.Filters;
using MediaBrowser.Common.Api;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Session;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
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
            // The default handler must be first so that it is evaluated first
            serviceCollection.AddSingleton<IAuthorizationHandler, DefaultAuthorizationHandler>();
            serviceCollection.AddSingleton<IAuthorizationHandler, UserPermissionHandler>();
            serviceCollection.AddSingleton<IAuthorizationHandler, FirstTimeSetupHandler>();
            serviceCollection.AddSingleton<IAuthorizationHandler, AnonymousLanAccessHandler>();
            serviceCollection.AddSingleton<IAuthorizationHandler, SyncPlayAccessHandler>();
            serviceCollection.AddSingleton<IAuthorizationHandler, LocalAccessOrRequiresElevationHandler>();

            return serviceCollection.AddAuthorizationCore(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .AddAuthenticationSchemes(AuthenticationSchemes.CustomAuthentication)
                    .AddRequirements(new DefaultAuthorizationRequirement())
                    .Build();

                options.AddPolicy(Policies.AnonymousLanAccessPolicy, new AnonymousLanAccessRequirement());
                options.AddPolicy(Policies.CollectionManagement, new UserPermissionRequirement(PermissionKind.EnableCollectionManagement));
                options.AddPolicy(Policies.Download, new UserPermissionRequirement(PermissionKind.EnableContentDownloading));
                options.AddPolicy(Policies.FirstTimeSetupOrDefault, new FirstTimeSetupRequirement(requireAdmin: false));
                options.AddPolicy(Policies.FirstTimeSetupOrElevated, new FirstTimeSetupRequirement());
                options.AddPolicy(Policies.FirstTimeSetupOrIgnoreParentalControl, new FirstTimeSetupRequirement(false, false));
                options.AddPolicy(Policies.IgnoreParentalControl, new DefaultAuthorizationRequirement(validateParentalSchedule: false));
                options.AddPolicy(Policies.LiveTvAccess, new UserPermissionRequirement(PermissionKind.EnableLiveTvAccess));
                options.AddPolicy(Policies.LiveTvManagement, new UserPermissionRequirement(PermissionKind.EnableLiveTvManagement));
                options.AddPolicy(Policies.LocalAccessOrRequiresElevation, new LocalAccessOrRequiresElevationRequirement());
                options.AddPolicy(Policies.SyncPlayHasAccess, new SyncPlayAccessRequirement(SyncPlayAccessRequirementType.HasAccess));
                options.AddPolicy(Policies.SyncPlayCreateGroup, new SyncPlayAccessRequirement(SyncPlayAccessRequirementType.CreateGroup));
                options.AddPolicy(Policies.SyncPlayJoinGroup, new SyncPlayAccessRequirement(SyncPlayAccessRequirementType.JoinGroup));
                options.AddPolicy(Policies.SyncPlayIsInGroup, new SyncPlayAccessRequirement(SyncPlayAccessRequirementType.IsInGroup));
                options.AddPolicy(Policies.SubtitleManagement, new UserPermissionRequirement(PermissionKind.EnableSubtitleManagement));
                options.AddPolicy(Policies.LyricManagement, new UserPermissionRequirement(PermissionKind.EnableLyricManagement));
                options.AddPolicy(
                    Policies.RequiresElevation,
                    policy => policy.AddAuthenticationSchemes(AuthenticationSchemes.CustomAuthentication)
                        .RequireClaim(ClaimTypes.Role, UserRoles.Administrator));
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
        /// Extension method for adding the Jellyfin API to the service collection.
        /// </summary>
        /// <param name="serviceCollection">The service collection.</param>
        /// <param name="pluginAssemblies">An IEnumerable containing all plugin assemblies with API controllers.</param>
        /// <param name="config">The <see cref="NetworkConfiguration"/>.</param>
        /// <returns>The MVC builder.</returns>
        public static IMvcBuilder AddJellyfinApi(this IServiceCollection serviceCollection, IEnumerable<Assembly> pluginAssemblies, NetworkConfiguration config)
        {
            IMvcBuilder mvcBuilder = serviceCollection
                .AddCors()
                .AddTransient<ICorsPolicyProvider, CorsPolicyProvider>()
                .Configure<ForwardedHeadersOptions>(options =>
                {
                    // https://github.com/dotnet/aspnetcore/blob/master/src/Middleware/HttpOverrides/src/ForwardedHeadersMiddleware.cs
                    // Enable debug logging on Microsoft.AspNetCore.HttpOverrides.ForwardedHeadersMiddleware to help investigate issues.

                    if (config.KnownProxies.Length == 0)
                    {
                        options.ForwardedHeaders = ForwardedHeaders.None;
                        options.KnownNetworks.Clear();
                        options.KnownProxies.Clear();
                    }
                    else
                    {
                        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
                        AddProxyAddresses(config, config.KnownProxies, options);
                    }

                    // Only set forward limit if we have some known proxies or some known networks.
                    if (options.KnownProxies.Count != 0 || options.KnownNetworks.Count != 0)
                    {
                        options.ForwardLimit = null;
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

                    opts.ModelBinderProviders.Insert(0, new NullableEnumModelBinderProvider());
                })

                // Clear app parts to avoid other assemblies being picked up
                .ConfigureApplicationPartManager(a => a.ApplicationParts.Clear())
                .AddApplicationPart(typeof(StartupController).Assembly)
                .AddJsonOptions(options =>
                {
                    // Update all properties that are set in JsonDefaults
                    var jsonOptions = JsonDefaults.PascalCaseOptions;

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
                var version = typeof(ApplicationHost).Assembly.GetName().Version?.ToString(3) ?? "0.0.1";
                c.SwaggerDoc("api-docs", new OpenApiInfo
                {
                    Title = "Jellyfin API",
                    Version = version,
                    Extensions = new Dictionary<string, IOpenApiExtension>
                    {
                        {
                            "x-jellyfin-version",
                            new OpenApiString(version)
                        }
                    }
                });

                c.AddSecurityDefinition(AuthenticationSchemes.CustomAuthentication, new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Name = "Authorization",
                    Description = "API key header parameter"
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
                        return description?.ActionDescriptor.AttributeRouteInfo?.Name
                               ?? methodInfo?.Name
                               ?? null;
                    });

                // Allow parameters to properly be nullable.
                c.UseAllOfToExtendReferenceSchemas();
                c.SupportNonNullableReferenceTypes();

                // TODO - remove when all types are supported in System.Text.Json
                c.AddSwaggerTypeMappings();

                c.SchemaFilter<IgnoreEnumSchemaFilter>();
                c.OperationFilter<SecurityRequirementsOperationFilter>();
                c.OperationFilter<FileResponseFilter>();
                c.OperationFilter<FileRequestFilter>();
                c.OperationFilter<ParameterObsoleteFilter>();
                c.DocumentFilter<AdditionalModelFilter>();
            });
        }

        private static void AddPolicy(this AuthorizationOptions authorizationOptions, string policyName, IAuthorizationRequirement authorizationRequirement)
        {
            authorizationOptions.AddPolicy(policyName, policy =>
            {
                policy.AddAuthenticationSchemes(AuthenticationSchemes.CustomAuthentication).AddRequirements(authorizationRequirement);
            });
        }

        /// <summary>
        /// Sets up the proxy configuration based on the addresses/subnets in <paramref name="allowedProxies"/>.
        /// </summary>
        /// <param name="config">The <see cref="NetworkConfiguration"/> containing the config settings.</param>
        /// <param name="allowedProxies">The string array to parse.</param>
        /// <param name="options">The <see cref="ForwardedHeadersOptions"/> instance.</param>
        internal static void AddProxyAddresses(NetworkConfiguration config, string[] allowedProxies, ForwardedHeadersOptions options)
        {
            for (var i = 0; i < allowedProxies.Length; i++)
            {
                if (IPAddress.TryParse(allowedProxies[i], out var addr))
                {
                    AddIPAddress(config, options, addr, addr.AddressFamily == AddressFamily.InterNetwork ? NetworkConstants.MinimumIPv4PrefixSize : NetworkConstants.MinimumIPv6PrefixSize);
                }
                else if (NetworkUtils.TryParseToSubnet(allowedProxies[i], out var subnet))
                {
                    if (subnet is not null)
                    {
                        AddIPAddress(config, options, subnet.Prefix, subnet.PrefixLength);
                    }
                }
                else if (NetworkUtils.TryParseHost(allowedProxies[i], out var addresses, config.EnableIPv4, config.EnableIPv6))
                {
                    foreach (var address in addresses)
                    {
                        AddIPAddress(config, options, address, address.AddressFamily == AddressFamily.InterNetwork ? NetworkConstants.MinimumIPv4PrefixSize : NetworkConstants.MinimumIPv6PrefixSize);
                    }
                }
            }
        }

        private static void AddIPAddress(NetworkConfiguration config, ForwardedHeadersOptions options, IPAddress addr, int prefixLength)
        {
            if (addr.IsIPv4MappedToIPv6)
            {
                addr = addr.MapToIPv4();
            }

            if ((!config.EnableIPv4 && addr.AddressFamily == AddressFamily.InterNetwork) || (!config.EnableIPv6 && addr.AddressFamily == AddressFamily.InterNetworkV6))
            {
                return;
            }

            if (prefixLength == NetworkConstants.MinimumIPv4PrefixSize)
            {
                options.KnownProxies.Add(addr);
            }
            else
            {
                options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(addr, prefixLength));
            }
        }

        private static void AddSwaggerTypeMappings(this SwaggerGenOptions options)
        {
            /*
             * TODO remove when System.Text.Json properly supports non-string keys.
             * Used in BaseItemDto.ImageBlurHashes
             */
            options.MapType<Dictionary<ImageType, string>>(() =>
                new OpenApiSchema
                {
                    Type = "object",
                    AdditionalProperties = new OpenApiSchema
                    {
                        Type = "string"
                    }
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
                        _ => new OpenApiSchema
                        {
                            Type = "object",
                            AdditionalProperties = new OpenApiSchema
                            {
                                Type = "string"
                            }
                        })
                });

            // Support dictionary with nullable string value.
            options.MapType<Dictionary<string, string?>>(() =>
                new OpenApiSchema
                {
                    Type = "object",
                    AdditionalProperties = new OpenApiSchema
                    {
                        Type = "string",
                        Nullable = true
                    }
                });

            // Manually describe Flags enum.
            options.MapType<TranscodeReason>(() =>
                new OpenApiSchema
                {
                    Type = "array",
                    Items = new OpenApiSchema
                    {
                        Reference = new OpenApiReference
                        {
                            Id = nameof(TranscodeReason),
                            Type = ReferenceType.Schema,
                        }
                    }
                });

            // Swashbuckle doesn't use JsonOptions to describe responses, so we need to manually describe it.
            options.MapType<Version>(() => new OpenApiSchema
            {
                Type = "string"
            });
        }
    }
}
