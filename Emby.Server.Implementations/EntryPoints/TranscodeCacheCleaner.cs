using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.MediaEncoding;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.EntryPoints;

/// <summary>
/// <see cref="IHostedService"/> responsible for wiping the transcode cache once, eagerly, at server
/// startup. It is registered before every other hosted service, so its <see cref="StartAsync"/> runs
/// to completion before any other hosted service's <see cref="StartAsync"/> starts, and ASP.NET Core's
/// generic host runs every registered hosted service's <see cref="StartAsync"/> to completion before
/// the web host starts accepting connections. Together, this always finishes before any stream
/// (transcode or Live TV buffer, whether opened by a request or resumed by another hosted service) can
/// open a file in the same folder. Previously this wipe ran from
/// <see cref="MediaBrowser.MediaEncoding.Transcoding.TranscodeManager"/>'s constructor, which is a
/// lazily-constructed DI singleton: the wipe actually ran on whatever stream was requested first,
/// which could delete a Live TV buffer file already open for writing (jellyfin/jellyfin#17593).
/// An error during the wipe is logged and does not prevent the server from starting.
/// </summary>
public sealed class TranscodeCacheCleaner : IHostedService
{
    private readonly ITranscodeManager _transcodeManager;
    private readonly ILogger<TranscodeCacheCleaner> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranscodeCacheCleaner"/> class.
    /// </summary>
    /// <param name="transcodeManager">The <see cref="ITranscodeManager"/>.</param>
    /// <param name="logger">The <see cref="ILogger{TranscodeCacheCleaner}"/>.</param>
    public TranscodeCacheCleaner(ITranscodeManager transcodeManager, ILogger<TranscodeCacheCleaner> logger)
    {
        _transcodeManager = transcodeManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _transcodeManager.DeleteEncodedMediaCache();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to wipe the transcode cache at startup, continuing server startup");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
