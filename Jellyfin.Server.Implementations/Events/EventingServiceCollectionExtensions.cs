using System.Collections.Generic;
using System.Reflection;
using Jellyfin.Server.Implementations.Events.Consumers.Updates;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Events.Security;
using MediaBrowser.Controller.Events.Updates;
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
        /// <param name="assemblies">An enumerable containing the assemblies to register handlers from.</param>
        public static void AddEventServices(this IServiceCollection collection, ILoggerFactory loggerFactory, IEnumerable<Assembly> assemblies)
        {
            foreach (var assembly in assemblies)
            {
                collection.AutoRegisterHandlersFromAssembly(assembly);
            }

            collection.AddRebus(configure => configure
                .Logging(l => l.MicrosoftExtensionsLogging(loggerFactory))
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "Events"))
                .Routing(r => r.TypeBased()
                    .MapAssemblyOf<AuthenticationSucceededEventArgs>("Events")
                    .MapFallback("Events")));

            // Update consumers
            collection.AddScoped<IEventConsumer<PluginInstallingEventArgs>, PluginInstallingNotifier>();
            collection.AddScoped<IEventConsumer<PluginUninstalledEventArgs>, PluginUninstalledLogger>();
            collection.AddScoped<IEventConsumer<PluginUninstalledEventArgs>, PluginUninstalledNotifier>();
            collection.AddScoped<IEventConsumer<PluginUpdatedEventArgs>, PluginUpdatedLogger>();
        }
    }
}
