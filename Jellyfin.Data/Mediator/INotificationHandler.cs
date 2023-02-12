using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Data.Mediator;

/// <summary>
/// Defines a notification handler.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    /// <summary>
    /// Handles a notification.
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the handler action.</returns>
    ValueTask Handle(TNotification notification, CancellationToken cancellationToken);
}
