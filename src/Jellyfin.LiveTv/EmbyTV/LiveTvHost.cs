using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.LiveTv;
using Microsoft.Extensions.Hosting;

namespace Jellyfin.LiveTv.EmbyTV;

/// <summary>
/// <see cref="IHostedService"/> responsible for initializing Live TV.
/// </summary>
public sealed class LiveTvHost : IHostedService
{
    private readonly EmbyTV _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="LiveTvHost"/> class.
    /// </summary>
    /// <param name="services">The available <see cref="ILiveTvService"/>s.</param>
    public LiveTvHost(IEnumerable<ILiveTvService> services)
    {
        _service = services.OfType<EmbyTV>().First();
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken) => _service.Start();

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
