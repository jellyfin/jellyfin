using System;
using System.Threading.Tasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.Events
{
    /// <summary>
    /// Handles the firing of events.
    /// </summary>
    public class EventManager : IEventManager
    {
        private readonly ILogger<EventManager> _logger;
        private readonly IServerApplicationHost _appHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventManager"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="appHost">The application host.</param>
        public EventManager(ILogger<EventManager> logger, IServerApplicationHost appHost)
        {
            _logger = logger;
            _appHost = appHost;
        }

        /// <inheritdoc />
        public void Publish<T>(T eventArgs)
            where T : EventArgs
        {
            PublishInternal(eventArgs).GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public async Task PublishAsync<T>(T eventArgs)
            where T : EventArgs
        {
            await PublishInternal(eventArgs).ConfigureAwait(false);
        }

        private async Task PublishInternal<T>(T eventArgs)
            where T : EventArgs
        {
            using var scope = _appHost.ServiceProvider?.CreateScope();
            if (scope is null)
            {
                return;
            }

            foreach (var service in scope.ServiceProvider.GetServices<IEventConsumer<T>>())
            {
                try
                {
                    await service.OnEvent(eventArgs).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Uncaught exception in EventConsumer {Type}: ", service.GetType());
                }
            }
        }
    }
}
