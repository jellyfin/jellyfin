#pragma warning disable CS1591

using System;
using Jellyfin.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Server.Implementations.Activity
{
    public class JellyfinDbProvider
    {
        private readonly IServiceProvider serviceProvider;

        public JellyfinDbProvider(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            serviceProvider.GetService<JellyfinDb>().Database.EnsureCreated();
        }

        public JellyfinDb GetConnection()
        {
            return serviceProvider.GetService<JellyfinDb>();
        }
    }
}
