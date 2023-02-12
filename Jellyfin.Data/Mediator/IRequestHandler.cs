using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Data.Mediator;

/// <summary>
/// Defines a request handler that doesn't return anything.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
public interface IRequestHandler<in TRequest>
    where TRequest : IRequest
{
    /// <summary>
    /// Handles a request.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the handler action.</returns>
    ValueTask Handle(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Defines a request handler that returns a response.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles a request.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the handler action, containing the response.</returns>
    ValueTask<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
