using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.Mediator;

/// <inheritdoc />
public class JellyfinMediator : IMediator
{
    private readonly ILogger<JellyfinMediator> _logger;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="JellyfinMediator"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="serviceProvider">The service provider.</param>
    public JellyfinMediator(ILogger<JellyfinMediator> logger, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        var handlers = _serviceProvider.GetServices<INotificationHandler<TNotification>>();

        foreach (var handler in handlers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await handler.Handle(notification, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Handler {HandlerName}, failed", handler.GetType().Name);
            }
        }
    }

    /// <inheritdoc />
    public ValueTask Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        var handler = _serviceProvider.GetService<IRequestHandler<TRequest>>()
            ?? throw new InvalidOperationException($"No handler exists for type {request.GetType()}");

        return handler.Handle(request, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var handler = _serviceProvider.GetService<IRequestHandler<IRequest<TResponse>, TResponse>>()
            ?? throw new InvalidOperationException($"No handler exists for type {request.GetType()}");

        return handler.Handle(request, cancellationToken);
    }
}
