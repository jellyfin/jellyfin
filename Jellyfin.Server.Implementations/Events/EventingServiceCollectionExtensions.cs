﻿using Jellyfin.Data.Events.System;
using Jellyfin.Server.Implementations.Events.Consumers.Security;
using Jellyfin.Server.Implementations.Events.Consumers.Session;
using Jellyfin.Server.Implementations.Events.Consumers.System;
using Jellyfin.Server.Implementations.Events.Consumers.Updates;
using MediaBrowser.Common.Updates;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Events.Security;
using MediaBrowser.Controller.Events.Session;
using MediaBrowser.Controller.Events.Updates;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.ServiceProvider;
using Rebus.Transport.InMem;

namespace Jellyfin.Server.Implementations.Events
{
    /// <summary>
    /// A class containing extensions to <see cref="IServiceCollection"/> for eventing.
    /// </summary>
    public static class EventingServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the event services to the service collection.
        /// </summary>
        /// <param name="collection">The service collection.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        public static void AddEventServices(this IServiceCollection collection, ILoggerFactory loggerFactory)
        {
            collection.AutoRegisterHandlersFromAssembly(typeof(AuthenticationSucceededLogger).Assembly);

            collection.AddRebus(configure => configure
                .Logging(l => l.MicrosoftExtensionsLogging(loggerFactory))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "Events"))
                .Routing(r => r.TypeBased()
                    .MapAssemblyOf<AuthenticationSucceededEventArgs>("Events")
                    .MapFallback("Events")));

            // Session consumers
            collection.AddScoped<IEventConsumer<PlaybackStartEventArgs>, PlaybackStartLogger>();
            collection.AddScoped<IEventConsumer<PlaybackStopEventArgs>, PlaybackStopLogger>();
            collection.AddScoped<IEventConsumer<SessionEndedEventArgs>, SessionEndedLogger>();
            collection.AddScoped<IEventConsumer<SessionStartedEventArgs>, SessionStartedLogger>();

            // System consumers
            collection.AddScoped<IEventConsumer<PendingRestartEventArgs>, PendingRestartNotifier>();
            collection.AddScoped<IEventConsumer<TaskCompletionEventArgs>, TaskCompletedLogger>();
            collection.AddScoped<IEventConsumer<TaskCompletionEventArgs>, TaskCompletedNotifier>();

            // Update consumers
            collection.AddScoped<IEventConsumer<PluginInstallationCancelledEventArgs>, PluginInstallationCancelledNotifier>();
            collection.AddScoped<IEventConsumer<InstallationFailedEventArgs>, PluginInstallationFailedLogger>();
            collection.AddScoped<IEventConsumer<InstallationFailedEventArgs>, PluginInstallationFailedNotifier>();
            collection.AddScoped<IEventConsumer<PluginInstalledEventArgs>, PluginInstalledLogger>();
            collection.AddScoped<IEventConsumer<PluginInstalledEventArgs>, PluginInstalledNotifier>();
            collection.AddScoped<IEventConsumer<PluginInstallingEventArgs>, PluginInstallingNotifier>();
            collection.AddScoped<IEventConsumer<PluginUninstalledEventArgs>, PluginUninstalledLogger>();
            collection.AddScoped<IEventConsumer<PluginUninstalledEventArgs>, PluginUninstalledNotifier>();
            collection.AddScoped<IEventConsumer<PluginUpdatedEventArgs>, PluginUpdatedLogger>();
        }
    }
}
