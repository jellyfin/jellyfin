using System;
using System.ComponentModel;
using System.Data.OleDb;
using System.Net.Http.Headers;
using Jellyfin.Api.TypeConverters;
using Jellyfin.Server.Extensions;
using Jellyfin.Server.HealthChecks;
using Jellyfin.Server.Middleware;
using Jellyfin.Server.Models;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prometheus;

namespace Jellyfin.Server
{
    /// <summary>
    /// Startup configuration for the Kestrel webhost.
    /// </summary>
    public class Startup
    {
        private readonly IServerConfigurationManager _serverConfigurationManager;
        private readonly IServerApplicationHost _serverApplicationHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup" /> class.
        /// </summary>
        /// <param name="serverConfigurationManager">The server configuration manager.</param>
        /// <param name="serverApplicationHost">The server application host.</param>
        public Startup(
            IServerConfigurationManager serverConfigurationManager,
            IServerApplicationHost serverApplicationHost)
        {
            _serverConfigurationManager = serverConfigurationManager;
            _serverApplicationHost = serverApplicationHost;
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
            services.AddJellyfinApi(
                _serverConfigurationManager.Configuration.BaseUrl.TrimStart('/'),
                _serverApplicationHost.GetApiPluginAssemblies());

            services.AddJellyfinApiSwagger();

            // configure custom legacy authentication
            services.AddCustomAuthentication();

            services.AddJellyfinApiAuthorization();

            var productHeader = new ProductInfoHeaderValue(
                _serverApplicationHost.Name.Replace(' ', '-'),
                _serverApplicationHost.ApplicationVersionString);
            services
                .AddHttpClient(NamedClient.Default, c =>
                {
                    c.DefaultRequestHeaders.UserAgent.Add(productHeader);
                })
                .ConfigurePrimaryHttpMessageHandler(x => new DefaultHttpClientHandler());

            services.AddHttpClient(NamedClient.MusicBrainz, c =>
                {
                    c.DefaultRequestHeaders.UserAgent.Add(productHeader);
                    c.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue($"({_serverApplicationHost.ApplicationUserAgentAddress})"));
                })
                .ConfigurePrimaryHttpMessageHandler(x => new DefaultHttpClientHandler());

            services.AddHealthChecks()
                .AddCheck<JellyfinDbHealthCheck>("JellyfinDb");
        }

        /// <summary>
        /// Configures the app builder for the webhost.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="env">The webhost environment.</param>
        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMiddleware<ExceptionMiddleware>();

            app.UseMiddleware<ResponseTimeMiddleware>();

            app.UseWebSockets();

            app.UseResponseCompression();

            app.UseCors(ServerCorsPolicy.DefaultPolicyName);

            if (_serverConfigurationManager.Configuration.RequireHttps
                && _serverApplicationHost.ListenWithHttps)
            {
                app.UseHttpsRedirection();
            }

            app.UseAuthentication();
            app.UseJellyfinApiSwagger(_serverConfigurationManager);
            app.UseRouting();
            app.UseAuthorization();
            if (_serverConfigurationManager.Configuration.EnableMetrics)
            {
                // Must be registered after any middleware that could chagne HTTP response codes or the data will be bad
                app.UseHttpMetrics();
            }

            app.UseLanFiltering();
            app.UseIpBasedAccessValidation();
            app.UseBaseUrlRedirection();
            app.UseWebSocketHandler();
            app.UseServerStartupMessage();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                if (_serverConfigurationManager.Configuration.EnableMetrics)
                {
                    endpoints.MapMetrics(_serverConfigurationManager.Configuration.BaseUrl.TrimStart('/') + "/metrics");
                }

                endpoints.MapHealthChecks(_serverConfigurationManager.Configuration.BaseUrl.TrimStart('/') + "/health");
            });

            // Add type descriptor for legacy datetime parsing.
            TypeDescriptor.AddAttributes(typeof(DateTime?), new TypeConverterAttribute(typeof(DateTimeTypeConverter)));
        }
    }
}
