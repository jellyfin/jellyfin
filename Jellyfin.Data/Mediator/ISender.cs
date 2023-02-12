using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Data.Mediator;

/// <summary>
/// Denotes a mechanism for sending requests to a single handler.
/// </summary>
public interface ISender
{
    /// <summary>
    /// Sends a request to a single handler.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <returns>A task representing the send operation.</returns>
    ValueTask Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest;

    /// <summary>
    /// Sends a request to a single handler.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <returns>A task representing the send operation, containing the handler response.</returns>
    ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
