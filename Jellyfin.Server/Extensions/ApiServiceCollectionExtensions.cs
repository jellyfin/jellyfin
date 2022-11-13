using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Emby.Server.Implementations;
using Jellyfin.Api.Auth;
using Jellyfin.Api.Auth.AnonymousLanAccessPolicy;
using Jellyfin.Api.Auth.DefaultAuthorizationPolicy;
using Jellyfin.Api.Auth.DownloadPolicy;
using Jellyfin.Api.Auth.FirstTimeOrIgnoreParentalControlSetupPolicy;
using Jellyfin.Api.Auth.FirstTimeSetupOrDefaultPolicy;
using Jellyfin.Api.Auth.FirstTimeSetupOrElevatedPolicy;
using Jellyfin.Api.Auth.IgnoreParentalControlPolicy;
using Jellyfin.Api.Auth.LocalAccessOrRequiresElevationPolicy;
using Jellyfin.Api.Auth.LocalAccessPolicy;
using Jellyfin.Api.Auth.RequiresElevationPolicy;
using Jellyfin.Api.Auth.SyncPlayAccessPolicy;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Controllers;
using Jellyfin.Api.ModelBinders;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions.Json;
using Jellyfin.Networking.Configuration;
using Jellyfin.Server.Configuration;
using Jellyfin.Server.Filters;
using Jellyfin.Server.Formatters;
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
            serviceCollection.AddSingleton<IAuthorizationHandler, DefaultAuthorizationHandler>();
            serviceCollection.AddSingleton<IAuthorizationHandler, DownloadHandler>();
            serviceCollection.AddSingleton<IAuthorizationHandler, FirstTimeSetupOrDefaultHandler>();
            serviceCollection.AddSingleton<IAuthorizationHandler, FirstTimeSetupOrElevatedHandler>();
            serviceCollection.AddSingleton<IAuthorizationHandler, IgnoreParentalControlHandler>();
            serviceCollection.AddSingleton<IAuthorizationHandler, FirstTimeOrIgnoreParentalControlSetupHandler>();
            serviceCollection.AddSingleton<IAuthorizationHandler, LocalAccessHandler>();
            serviceCollection.AddSingleton<IAuthorizationHandler, AnonymousLanAccessHandler>();
            serviceCollection.AddSingleton<IAuthorizationHandler, LocalAccessOrRequiresElevationHandler>();
            serviceCollection.AddSingleton<IAuthorizationHandler, RequiresElevationHandler>();
            serviceCollection.AddSingleton<IAuthorizationHandler, SyncPlayAccessHandler>();
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
                options.AddPolicy(
                    Policies.SyncPlayHasAccess,
                    policy =>
                    {
                        policy.AddAuthenticationSchemes(AuthenticationSchemes.CustomAuthentication);
                        policy.AddRequirements(new SyncPlayAccessRequirement(SyncPlayAccessRequirementType.HasAccess));
                    });
                options.AddPolicy(
                    Policies.SyncPlayCreateGroup,
                    policy =>
                    {
                        policy.AddAuthenticationSchemes(AuthenticationSchemes.CustomAuthentication);
                        policy.AddRequirements(new SyncPlayAccessRequirement(SyncPlayAccessRequirementType.CreateGroup));
                    });
                options.AddPolicy(
                    Policies.SyncPlayJoinGroup,
                    policy =>
                    {
                        policy.AddAuthenticationSchemes(AuthenticationSchemes.CustomAuthentication);
                        policy.AddRequirements(new SyncPlayAccessRequirement(SyncPlayAccessRequirementType.JoinGroup));
                    });
                options.AddPolicy(
                    Policies.SyncPlayIsInGroup,
                    policy =>
                    {
                        policy.AddAuthenticationSchemes(AuthenticationSchemes.CustomAuthentication);
                        policy.AddRequirements(new SyncPlayAccessRequirement(SyncPlayAccessRequirementType.IsInGroup));
                    });
                options.AddPolicy(
                    Policies.AnonymousLanAccessPolicy,
                    policy =>
                    {
                        policy.AddAuthenticationSchemes(AuthenticationSchemes.CustomAuthentication);
                        policy.AddRequirements(new AnonymousLanAccessRequirement());
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

                    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;

                    if (config.KnownProxies.Length == 0)
                    {
                        options.KnownNetworks.Clear();
                        options.KnownProxies.Clear();
                    }
                    else
                    {
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

                c.OperationFilter<SecurityRequirementsOperationFilter>();
                c.OperationFilter<FileResponseFilter>();
                c.OperationFilter<FileRequestFilter>();
                c.OperationFilter<ParameterObsoleteFilter>();
                c.DocumentFilter<AdditionalModelFilter>();
            });
        }

        /// <summary>
        /// Sets up the proxy configuration based on the addresses in <paramref name="allowedProxies"/>.
        /// </summary>
        /// <param name="config">The <see cref="NetworkConfiguration"/> containing the config settings.</param>
        /// <param name="allowedProxies">The string array to parse.</param>
        /// <param name="options">The <see cref="ForwardedHeadersOptions"/> instance.</param>
        internal static void AddProxyAddresses(NetworkConfiguration config, string[] allowedProxies, ForwardedHeadersOptions options)
        {
            for (var i = 0; i < allowedProxies.Length; i++)
            {
                if (IPNetAddress.TryParse(allowedProxies[i], out var addr))
                {
                    AddIpAddress(config, options, addr.Address, addr.PrefixLength);
                }
                else if (IPHost.TryParse(allowedProxies[i], out var host))
                {
                    foreach (var address in host.GetAddresses())
                    {
                        AddIpAddress(config, options, address, address.AddressFamily == AddressFamily.InterNetwork ? 32 : 128);
                    }
                }
            }
        }

        private static void AddIpAddress(NetworkConfiguration config, ForwardedHeadersOptions options, IPAddress addr, int prefixLength)
        {
            if ((!config.EnableIPV4 && addr.AddressFamily == AddressFamily.InterNetwork) || (!config.EnableIPV6 && addr.AddressFamily == AddressFamily.InterNetworkV6))
            {
                return;
            }

            // In order for dual-mode sockets to be used, IP6 has to be enabled in JF and an interface has to have an IP6 address.
            if (addr.AddressFamily == AddressFamily.InterNetwork && config.EnableIPV6)
            {
                // If the server is using dual-mode sockets, IPv4 addresses are supplied in an IPv6 format.
                // https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-5.0 .
                addr = addr.MapToIPv6();
            }

            if (prefixLength == 32)
            {
                options.KnownProxies.Add(addr);
            }
            else
            {
                options.KnownNetworks.Add(new IPNetwork(addr, prefixLength));
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
