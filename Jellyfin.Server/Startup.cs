using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.RateLimiting;
using Emby.Server.Implementations.EntryPoints;
using Emby.Server.Implementations.Localization;
using Jellyfin.Api.Middleware;
using Jellyfin.Database.Implementations;
using Jellyfin.LiveTv.Channels;
using Jellyfin.MediaEncoding.Hls.Extensions;
using Jellyfin.Networking;
using Jellyfin.Networking.HappyEyeballs;
using Jellyfin.Server.Extensions;
using Jellyfin.Server.HealthChecks;
using Jellyfin.Server.Implementations.CustomNetflix;
using Jellyfin.Server.Implementations.Extensions;
using MediaBrowser.Common.Api;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Extensions;
using MediaBrowser.XbmcMetadata;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using Prometheus;

namespace Jellyfin.Server
{
    /// <summary>
    /// Startup configuration for the Kestrel webhost.
    /// </summary>
    public class Startup
    {
        private static readonly Regex _versionedAssetRegex = new(
            @"\.[a-f0-9]{8,}\.",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        private readonly CoreAppHost _serverApplicationHost;
        private readonly IConfiguration _configuration;
        private readonly IServerConfigurationManager _serverConfigurationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup" /> class.
        /// </summary>
        /// <param name="appHost">The server application host.</param>
        /// <param name="configuration">The used Configuration.</param>
        public Startup(CoreAppHost appHost, IConfiguration configuration)
        {
            _serverApplicationHost = appHost;
            _configuration = configuration;
            _serverConfigurationManager = appHost.ConfigurationManager;
        }

        /// <summary>
        /// Configures the service collection for the webhost.
        /// </summary>
        /// <param name="services">The service collection.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddResponseCompression();
            services.AddHttpContextAccessor();
            services.AddHttpsRedirection(options =>
            {
                options.HttpsPort = _serverApplicationHost.HttpsPort;
            });

            services.AddJellyfinApi(_serverApplicationHost.GetApiPluginAssemblies(), _serverConfigurationManager.GetNetworkConfiguration());
            services.AddJellyfinDbContext(_serverApplicationHost.ConfigurationManager, _configuration);
            services.AddCustomNetflixServices(_configuration);
            services.AddJellyfinApiSwagger();

            // configure custom legacy authentication
            services.AddCustomAuthentication();

            services.AddJellyfinApiAuthorization();
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy<(IPAddress Address, int PermitLimit, int WindowSeconds)>("PublicRegistration", context =>
                {
                    var configuration = _serverConfigurationManager.Configuration;
                    var partitionKey = (
                        context.GetNormalizedRemoteIP(),
                        Math.Max(1, configuration.PublicUserRegistrationMaxAttemptsPerWindow),
                        Math.Max(1, configuration.PublicUserRegistrationWindowSeconds));

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey,
                        key => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = key.Item2,
                            Window = TimeSpan.FromSeconds(key.Item3),
                            QueueLimit = 0,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        });
                });
            });

            var productHeader = new ProductInfoHeaderValue(
                _serverApplicationHost.Name.Replace(' ', '-'),
                _serverApplicationHost.ApplicationVersionString);
            var acceptJsonHeader = new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json, 1.0);
            var acceptXmlHeader = new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Xml, 0.9);
            var acceptAnyHeader = new MediaTypeWithQualityHeaderValue("*/*", 0.8);
            Func<IServiceProvider, HttpMessageHandler> eyeballsHttpClientHandlerDelegate = (_) => new SocketsHttpHandler()
            {
                AutomaticDecompression = DecompressionMethods.All,
                RequestHeaderEncodingSelector = (_, _) => Encoding.UTF8,
                ConnectCallback = HttpClientExtension.OnConnect
            };

            Func<IServiceProvider, HttpMessageHandler> defaultHttpClientHandlerDelegate = (_) => new SocketsHttpHandler()
            {
                AutomaticDecompression = DecompressionMethods.All,
                RequestHeaderEncodingSelector = (_, _) => Encoding.UTF8
            };

            services.AddHttpClient(NamedClient.Default, c =>
                {
                    c.DefaultRequestHeaders.UserAgent.Add(productHeader);
                    c.DefaultRequestHeaders.Accept.Add(acceptJsonHeader);
                    c.DefaultRequestHeaders.Accept.Add(acceptXmlHeader);
                    c.DefaultRequestHeaders.Accept.Add(acceptAnyHeader);
                })
                .ConfigurePrimaryHttpMessageHandler(eyeballsHttpClientHandlerDelegate);

            services.AddHttpClient(NamedClient.MusicBrainz, c =>
                {
                    c.DefaultRequestHeaders.UserAgent.Add(productHeader);
                    c.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue($"({_serverApplicationHost.ApplicationUserAgentAddress})"));
                    c.DefaultRequestHeaders.Accept.Add(acceptXmlHeader);
                    c.DefaultRequestHeaders.Accept.Add(acceptAnyHeader);
                })
                .ConfigurePrimaryHttpMessageHandler(eyeballsHttpClientHandlerDelegate);

            services.AddHttpClient(NamedClient.DirectIp, c =>
                {
                    c.DefaultRequestHeaders.UserAgent.Add(productHeader);
                    c.DefaultRequestHeaders.Accept.Add(acceptJsonHeader);
                    c.DefaultRequestHeaders.Accept.Add(acceptXmlHeader);
                    c.DefaultRequestHeaders.Accept.Add(acceptAnyHeader);
                })
                .ConfigurePrimaryHttpMessageHandler(defaultHttpClientHandlerDelegate);

            services.AddHealthChecks()
                .AddCheck<DbContextFactoryHealthCheck<JellyfinDbContext>>(nameof(JellyfinDbContext), tags: ["ready"]);

            services.AddHlsPlaylistGenerator();
            services.AddSingleton<IChannelManager, ChannelManager>();

            var serverUICulture = _serverConfigurationManager.Configuration.UICulture;
            if (string.IsNullOrEmpty(serverUICulture))
            {
                serverUICulture = "en-US";
            }

            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(serverUICulture);

            services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedUICultures = LocalizationManager.GetSupportedUICultures();
                options.SupportedCultures = supportedUICultures;
                options.SupportedUICultures = supportedUICultures;
                options.DefaultRequestCulture = new RequestCulture(serverUICulture);
                options.ApplyCurrentCultureToResponseHeaders = true;
                options.FallBackToParentCultures = true;
                options.FallBackToParentUICultures = true;
            });

            services.AddHostedService<AutoDiscoveryHost>();
            services.AddHostedService<NfoUserDataSaver>();
            services.AddHostedService<LibraryChangedNotifier>();
            services.AddHostedService<UserDataChangeNotifier>();
        }

        /// <summary>
        /// Configures the app builder for the webhost.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="env">The webhost environment.</param>
        /// <param name="appConfig">The application config.</param>
        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env,
            IConfiguration appConfig)
        {
            app.UseBaseUrlRedirection();

            // Wrap rest of configuration so everything only listens on BaseUrl.
            var config = _serverConfigurationManager.GetNetworkConfiguration();
            app.Map(config.BaseUrl, mainApp =>
            {
                if (env.IsDevelopment())
                {
                    mainApp.UseDeveloperExceptionPage();
                }

                // Liveness must remain reachable while startup is incomplete and
                // independently of remote-access policy or external dependencies.
                mainApp.UseHealthChecks("/live", new HealthCheckOptions
                {
                    Predicate = _ => false
                });

                mainApp.UseForwardedHeaders();
                mainApp.UseMiddleware<ExceptionMiddleware>();

                mainApp.UseMiddleware<ResponseTimeMiddleware>();

                mainApp.UseWebSockets();

                mainApp.UseResponseCompression();

                mainApp.UseCors();

                mainApp.UseRequestLocalization();

                if (config.RequireHttps)
                {
                    if (!env.IsDevelopment())
                    {
                        mainApp.UseHsts();
                    }

                    if (_serverApplicationHost.ListenWithHttps)
                    {
                        mainApp.UseHttpsRedirection();
                    }
                }

                if (appConfig.HostWebClient())
                {
                    var extensionProvider = new FileExtensionContentTypeProvider();

                    // subtitles octopus requires .data, .mem files.
                    extensionProvider.Mappings.Add(".data", MediaTypeNames.Application.Octet);
                    extensionProvider.Mappings.Add(".mem", MediaTypeNames.Application.Octet);
                    mainApp.UseDefaultFiles(new DefaultFilesOptions
                    {
                        FileProvider = new PhysicalFileProvider(_serverConfigurationManager.ApplicationPaths.WebPath),
                        RequestPath = "/web"
                    });
                    mainApp.UseStaticFiles(new StaticFileOptions
                    {
                        FileProvider = new PhysicalFileProvider(_serverConfigurationManager.ApplicationPaths.WebPath),
                        RequestPath = "/web",
                        ContentTypeProvider = extensionProvider,
                        OnPrepareResponse = (context) =>
                        {
                            if (Path.GetFileName(context.File.Name).Equals("index.html", StringComparison.Ordinal))
                            {
                                context.Context.Response.Headers.CacheControl = new StringValues("no-cache");
                            }
                            else if (context.Context.Request.QueryString.HasValue
                                     || _versionedAssetRegex.IsMatch(context.File.Name))
                            {
                                context.Context.Response.Headers.CacheControl = new StringValues("public,max-age=31536000,immutable");
                            }
                        }
                    });

                    mainApp.UseRobotsRedirection();
                }

                mainApp.UseStaticFiles();
                mainApp.UseAuthentication();
                mainApp.UseJellyfinApiSwagger(_serverConfigurationManager);
                mainApp.UseQueryStringDecoding();
                mainApp.UseRouting();
                mainApp.UseRateLimiter();
                mainApp.UseAuthorization();

                mainApp.UseIPBasedAccessValidation();
                mainApp.UseWebSocketHandler();
                mainApp.UseServerStartupMessage();

                if (_serverConfigurationManager.Configuration.EnableMetrics)
                {
                    // Must be registered after any middleware that could change HTTP response codes or the data will be bad
                    mainApp.UseHttpMetrics();
                }

                mainApp.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                    if (_serverConfigurationManager.Configuration.EnableMetrics)
                    {
                        endpoints.MapMetrics()
                            .RequireAuthorization(Policies.RequiresElevation);
                    }

                    endpoints.MapHealthChecks("/ready", new HealthCheckOptions
                    {
                        Predicate = registration => registration.Tags.Contains("ready"),
                        ResultStatusCodes =
                        {
                            [HealthStatus.Healthy] = StatusCodes.Status200OK,
                            [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
                            [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
                        }
                    });
                    endpoints.MapHealthChecks("/health");
                });
            });
        }
    }
}
