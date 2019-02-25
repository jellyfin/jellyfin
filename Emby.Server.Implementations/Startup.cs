using System;
using System.Linq;
using MediaBrowser.Api;
using MediaBrowser.Controller;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations
{
    public class Startup
	    {
	        public IConfiguration Configuration { get; }

	        public Startup(IConfiguration configuration) => Configuration = configuration;

            // Use this method to add services to the container.
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddRouting();
            }

            // Use this method to configure the HTTP request pipeline.
            public void Configure(IApplicationBuilder app)
            {

            }
	    }

}
