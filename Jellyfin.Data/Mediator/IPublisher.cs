using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Data.Mediator;

/// <summary>
/// Defines a mechanism for publishing notifications to multiple handlers.
/// </summary>
public interface IPublisher
{
    /// <summary>
    /// Sends a notification to multiple handlers.
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="TNotification">The notification type.</typeparam>
    /// <returns>A task that represents the notification publishing.</returns>
    ValueTask Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}
