using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.MediaEncoding;
using Microsoft.Extensions.Hosting;

namespace Emby.Server.Implementations.EntryPoints;

/// <summary>
/// <see cref="IHostedService"/> responsible for wiping the transcode cache once, eagerly, at server
/// startup. ASP.NET Core's generic host runs every registered hosted service's <see cref="StartAsync"/>
/// to completion before the web host starts accepting connections, so this always finishes before any
/// stream (transcode or Live TV buffer) can open a file in the same folder. Previously this wipe ran
/// from <see cref="MediaBrowser.MediaEncoding.Transcoding.TranscodeManager"/>'s constructor, which is a
/// lazily-constructed DI singleton: the wipe actually ran on whatever stream was requested first,
/// which could delete a Live TV buffer file already open for writing (jellyfin/jellyfin#17593).
/// </summary>
public sealed class TranscodeCacheCleaner : IHostedService
{
    private readonly ITranscodeManager _transcodeManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranscodeCacheCleaner"/> class.
    /// </summary>
    /// <param name="transcodeManager">The <see cref="ITranscodeManager"/>.</param>
    public TranscodeCacheCleaner(ITranscodeManager transcodeManager)
    {
        _transcodeManager = transcodeManager;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _transcodeManager.DeleteEncodedMediaCache();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
