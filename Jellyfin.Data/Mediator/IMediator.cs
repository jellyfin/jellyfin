namespace Jellyfin.Data.Mediator;

/// <summary>
/// Marker interface for <see cref="IPublisher"/> and <see cref="ISender"/>.
/// </summary>
public interface IMediator : IPublisher, ISender
{
}
