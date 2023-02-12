namespace Jellyfin.Data.Mediator;

/// <summary>
/// Marker interface to denote a request with no response.
/// </summary>
public interface IRequest
{
}

/// <summary>
/// Marker interface to denote a request with a response.
/// </summary>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface IRequest<out TResponse>
{
}
