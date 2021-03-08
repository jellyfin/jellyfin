using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Emby.Drawing;
using Emby.Server.Implementations;
using Emby.Server.Implementations.Session;
using Jellyfin.Api.WebSocketListeners;
using Jellyfin.Drawing.Skia;
using Jellyfin.Server.Implementations;
using Jellyfin.Server.Implementations.Activity;
using Jellyfin.Server.Implementations.Events;
using Jellyfin.Server.Implementations.Users;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.BaseItemManager;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server
{
    /// <summary>
    /// Implementation of the abstract <see cref="ApplicationHost" /> class.
    /// </summary>
    public class CoreAppHost : ApplicationHost
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CoreAppHost" /> class.
        /// </summary>
        /// <param name="applicationPaths">The <see cref="ServerApplicationPaths" /> to be used by the <see cref="CoreAppHost" />.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory" /> to be used by the <see cref="CoreAppHost" />.</param>
        /// <param name="options">The <see cref="StartupOptions" /> to be used by the <see cref="CoreAppHost" />.</param>
        /// <param name="startupConfig">The <see cref="IConfiguration" /> to be used by the <see cref="CoreAppHost" />.</param>
        /// <param name="fileSystem">The <see cref="IFileSystem" /> to be used by the <see cref="CoreAppHost" />.</param>
        /// <param name="collection">The <see cref="IServiceCollection"/> to be used by the <see cref="CoreAppHost"/>.</param>
        public CoreAppHost(
            IServerApplicationPaths applicationPaths,
            ILoggerFactory loggerFactory,
            IStartupOptions options,
            IConfiguration startupConfig,
            IFileSystem fileSystem,
            IServiceCollection collection)
            : base(
                applicationPaths,
                loggerFactory,
                options,
                startupConfig,
                fileSystem,
                collection)
        {
        }

        /// <inheritdoc/>
        protected override void RegisterServices()
        {
            // Register an image encoder
            bool useSkiaEncoder = SkiaEncoder.IsNativeLibAvailable();
            Type imageEncoderType = useSkiaEncoder
                ? typeof(SkiaEncoder)
                : typeof(NullImageEncoder);
            ServiceCollection.AddSingleton(typeof(IImageEncoder), imageEncoderType);

            // Log a warning if the Skia encoder could not be used
            if (!useSkiaEncoder)
            {
                Logger.LogWarning($"Skia not available. Will fallback to {nameof(NullImageEncoder)}.");
            }

            ServiceCollection.AddDbContextPool<JellyfinDb>(
                 options => options.UseSqlite($"Filename={Path.Combine(ApplicationPaths.DataPath, "jellyfin.db")}"));

            ServiceCollection.AddEventServices();
            ServiceCollection.AddSingleton<IBaseItemManager, BaseItemManager>();
            ServiceCollection.AddSingleton<IEventManager, EventManager>();
            ServiceCollection.AddSingleton<JellyfinDbProvider>();

            ServiceCollection.AddSingleton<IActivityManager, ActivityManager>();
            ServiceCollection.AddSingleton<IUserManager, UserManager>();
            ServiceCollection.AddSingleton<IDisplayPreferencesManager, DisplayPreferencesManager>();

            // TODO search the assemblies instead of adding them manually?
            ServiceCollection.AddSingleton<IWebSocketListener, SessionWebSocketListener>();
            ServiceCollection.AddSingleton<IWebSocketListener, ActivityLogWebSocketListener>();
            ServiceCollection.AddSingleton<IWebSocketListener, ScheduledTasksWebSocketListener>();
            ServiceCollection.AddSingleton<IWebSocketListener, SessionInfoWebSocketListener>();

            base.RegisterServices();
        }

        /// <inheritdoc />
        protected override void RestartInternal() => Program.Restart();

        /// <inheritdoc />
        protected override IEnumerable<Assembly> GetAssembliesWithPartsInternal()
        {
            // Jellyfin.Server
            yield return typeof(CoreAppHost).Assembly;

            // Jellyfin.Server.Implementations
            yield return typeof(JellyfinDb).Assembly;
        }

        /// <inheritdoc />
        protected override void ShutdownInternal() => Program.Shutdown();
    }
}
