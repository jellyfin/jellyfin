using System.Collections.Generic;
using System.Reflection;
using Emby.Server.Implementations;
using MediaBrowser.Controller;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Integration.Tests
{
    /// <summary>
    /// Implementation of the abstract <see cref="ApplicationHost" /> class.
    /// </summary>
    public class TestAppHost : CoreAppHost
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestAppHost" /> class.
        /// </summary>
        /// <param name="applicationPaths">The <see cref="ServerApplicationPaths" /> to be used by the <see cref="CoreAppHost" />.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory" /> to be used by the <see cref="CoreAppHost" />.</param>
        /// <param name="options">The <see cref="StartupOptions" /> to be used by the <see cref="CoreAppHost" />.</param>
        /// <param name="startup">The <see cref="IConfiguration" /> to be used by the <see cref="CoreAppHost" />.</param>
        public TestAppHost(
            IServerApplicationPaths applicationPaths,
            ILoggerFactory loggerFactory,
            IStartupOptions options,
            IConfiguration startup)
            : base(
                applicationPaths,
                loggerFactory,
                options,
                startup)
        {
        }

        /// <inheritdoc />
        protected override IEnumerable<Assembly> GetAssembliesWithPartsInternal()
        {
            foreach (var a in base.GetAssembliesWithPartsInternal())
            {
                yield return a;
            }

            yield return typeof(TestPlugin).Assembly;
        }
    }
}
