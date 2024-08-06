using System.Net.Mime;
using Emby.Server.Implementations.EntryPoints;
using Jellyfin.Api.Middleware;
using Jellyfin.LiveTv.Extensions;
using Jellyfin.LiveTv.Recordings;
using Jellyfin.MediaEncoding.Hls.Extensions;
using Jellyfin.Networking;
using Jellyfin.Server.Extensions;
using Jellyfin.Server.HealthChecks;
using Jellyfin.Server.Implementations;
using Jellyfin.Server.Implementations.Extensions;
using Jellyfin.Server.Infrastructure;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Extensions;
using MediaBrowser.XbmcMetadata;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Prometheus;

namespace Jellyfin.Server
{
    /// <summary>
    /// Startup configuration for the Kestrel webhost.
    /// </summary>
    public class Startup
    {
        private readonly CoreAppHost _serverApplicationHost;
        private readonly IServerConfigurationManager _serverConfigurationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup" /> class.
        /// </summary>
        /// <param name="appHost">The server application host.</param>
        public Startup(CoreAppHost appHost)
        {
            _serverApplicationHost = appHost;
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

            // TODO remove once this is fixed upstream https://github.com/dotnet/aspnetcore/issues/34371
            services.AddSingleton<IActionResultExecutor<PhysicalFileResult>, SymlinkFollowingPhysicalFileResultExecutor>();
            services.AddJellyfinApi(_serverApplicationHost.GetApiPluginAssemblies(), _serverConfigurationManager.GetNetworkConfiguration());
            services.AddJellyfinDbContext();
            services.AddJellyfinApiSwagger();

            // configure custom legacy authentication
            services.AddCustomAuthentication();

            services.AddJellyfinApiAuthorization();

            _serverApplicationHost.AddHttpClient(services, NamedClient.Default);
            _serverApplicationHost.AddHttpClient(services, NamedClient.DirectIp);

            services.AddHealthChecks()
                .AddCheck<DbContextFactoryHealthCheck<JellyfinDbContext>>(nameof(JellyfinDbContext));

            services.AddHlsPlaylistGenerator();
            services.AddLiveTvServices();

            services.AddHostedService<RecordingsHost>();
            services.AddHostedService<AutoDiscoveryHost>();
            services.AddHostedService<PortForwardingHost>();
            services.AddHostedService<NfoUserDataSaver>();
            services.AddHostedService<LibraryChangedNotifier>();
            services.AddHostedService<UserDataChangeNotifier>();
            services.AddHostedService<RecordingNotifier>();
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

                mainApp.UseForwardedHeaders();
                mainApp.UseMiddleware<ExceptionMiddleware>();

                mainApp.UseMiddleware<ResponseTimeMiddleware>();

                mainApp.UseWebSockets();

                mainApp.UseResponseCompression();

                mainApp.UseCors();

                if (config.RequireHttps && _serverApplicationHost.ListenWithHttps)
                {
                    mainApp.UseHttpsRedirection();
                }

                // This must be injected before any path related middleware.
                mainApp.UsePathTrim();

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
                        ContentTypeProvider = extensionProvider
                    });

                    mainApp.UseRobotsRedirection();
                }

                mainApp.UseStaticFiles();
                mainApp.UseAuthentication();
                mainApp.UseJellyfinApiSwagger(_serverConfigurationManager);
                mainApp.UseQueryStringDecoding();
                mainApp.UseRouting();
                mainApp.UseAuthorization();

                mainApp.UseLanFiltering();
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
                        endpoints.MapMetrics();
                    }

                    endpoints.MapHealthChecks("/health");
                });
            });
        }
    }
}
