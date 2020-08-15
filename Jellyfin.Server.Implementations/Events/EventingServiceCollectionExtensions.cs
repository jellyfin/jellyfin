using Jellyfin.Data.Events;
using Jellyfin.Data.Events.Users;
using Jellyfin.Server.Implementations.Events.Consumers;
using Jellyfin.Server.Implementations.Events.Consumers.Library;
using Jellyfin.Server.Implementations.Events.Consumers.Security;
using Jellyfin.Server.Implementations.Events.Consumers.Session;
using Jellyfin.Server.Implementations.Events.Consumers.Updates;
using Jellyfin.Server.Implementations.Events.Consumers.Users;
using MediaBrowser.Common.Updates;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Events.Session;
using MediaBrowser.Controller.Events.Updates;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.DependencyInjection;

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
        public static void AddEventServices(this IServiceCollection collection)
        {
            collection.AddScoped<IEventConsumer<SubtitleDownloadFailureEventArgs>, SubtitleDownloadFailureLogger>();

            collection.AddScoped<IEventConsumer<GenericEventArgs<AuthenticationResult>>, AuthenticationSucceededLogger>();
            collection.AddScoped<IEventConsumer<GenericEventArgs<AuthenticationRequest>>, AuthenticationFailedLogger>();

            collection.AddScoped<IEventConsumer<PlaybackStartEventArgs>, PlaybackStartLogger>();
            collection.AddScoped<IEventConsumer<PlaybackStopEventArgs>, PlaybackStopLogger>();
            collection.AddScoped<IEventConsumer<SessionStartedEventArgs>, SessionStartedLogger>();
            collection.AddScoped<IEventConsumer<SessionEndedEventArgs>, SessionEndedLogger>();

            collection.AddScoped<IEventConsumer<PluginInstalledEventArgs>, PluginInstalledLogger>();
            collection.AddScoped<IEventConsumer<PluginUninstalledEventArgs>, PluginUninstalledLogger>();
            collection.AddScoped<IEventConsumer<PluginUpdatedEventArgs>, PluginUpdatedLogger>();
            collection.AddScoped<IEventConsumer<InstallationFailedEventArgs>, PackageInstallationFailedLogger>();

            collection.AddScoped<IEventConsumer<UserCreatedEventArgs>, UserCreatedLogger>();
            collection.AddScoped<IEventConsumer<UserDeletedEventArgs>, UserDeletedLogger>();
            collection.AddScoped<IEventConsumer<UserLockedOutEventArgs>, UserLockedOutLogger>();
            collection.AddScoped<IEventConsumer<UserPasswordChangedEventArgs>, UserPasswordChangedLogger>();

            collection.AddScoped<IEventConsumer<TaskCompletionEventArgs>, TaskCompletedLogger>();
        }
    }
}
