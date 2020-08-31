using System;
using System.ComponentModel;
using System.Net.Http.Headers;
using Jellyfin.Api.TypeConverters;
using Jellyfin.Server.Extensions;
using Jellyfin.Server.Middleware;
using Jellyfin.Server.Models;
using MediaBrowser.Common;
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
        private readonly IApplicationHost _applicationHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup" /> class.
        /// </summary>
        /// <param name="serverConfigurationManager">The server configuration manager.</param>
        /// <param name="applicationHost">The application host.</param>
        public Startup(IServerConfigurationManager serverConfigurationManager, IApplicationHost applicationHost)
        {
            _serverConfigurationManager = serverConfigurationManager;
            _applicationHost = applicationHost;
        }

        /// <summary>
        /// Configures the service collection for the webhost.
        /// </summary>
        /// <param name="services">The service collection.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddResponseCompression();
            services.AddHttpContextAccessor();
            services.AddJellyfinApi(_serverConfigurationManager.Configuration.BaseUrl.TrimStart('/'));

            services.AddJellyfinApiSwagger();

            // configure custom legacy authentication
            services.AddCustomAuthentication();

            services.AddJellyfinApiAuthorization();

            services
                .AddHttpClient(NamedClient.Default, c =>
                {
                    c.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(_applicationHost.Name.Replace(' ', '-'), _applicationHost.ApplicationVersionString));
                })
                .ConfigurePrimaryHttpMessageHandler(x => new DefaultHttpClientHandler());

            services.AddHttpClient(NamedClient.MusicBrainz, c =>
                {
                    c.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(_applicationHost.Name.Replace(' ', '-'), _applicationHost.ApplicationVersionString));
                    c.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue($"({_applicationHost.ApplicationUserAgentAddress})"));
                })
                .ConfigurePrimaryHttpMessageHandler(x => new DefaultHttpClientHandler());
        }

        /// <summary>
        /// Configures the app builder for the webhost.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="env">The webhost environment.</param>
        /// <param name="serverApplicationHost">The server application host.</param>
        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env,
            IServerApplicationHost serverApplicationHost)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMiddleware<ExceptionMiddleware>();

            app.UseMiddleware<ResponseTimeMiddleware>();

            app.UseWebSockets();

            app.UseResponseCompression();

            // TODO app.UseMiddleware<WebSocketMiddleware>();

            app.UseAuthentication();
            app.UseJellyfinApiSwagger(_serverConfigurationManager);
            app.UseRouting();
            app.UseCors(ServerCorsPolicy.DefaultPolicyName);
            app.UseAuthorization();
            if (_serverConfigurationManager.Configuration.EnableMetrics)
            {
                // Must be registered after any middleware that could chagne HTTP response codes or the data will be bad
                app.UseHttpMetrics();
            }

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                if (_serverConfigurationManager.Configuration.EnableMetrics)
                {
                    endpoints.MapMetrics(_serverConfigurationManager.Configuration.BaseUrl.TrimStart('/') + "/metrics");
                }
            });

            app.Use(serverApplicationHost.ExecuteHttpHandlerAsync);

            // Add type descriptor for legacy datetime parsing.
            TypeDescriptor.AddAttributes(typeof(DateTime?), new TypeConverterAttribute(typeof(DateTimeTypeConverter)));
        }
    }
}
