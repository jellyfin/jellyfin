#pragma warning disable CS1591

using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal sealed class CustomNetflixSchemaState
{
    private readonly TaskCompletionSource _ready =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool IsReady => _ready.Task.IsCompletedSuccessfully;

    public void MarkReady()
        => _ready.TrySetResult();

    public Task WaitUntilReadyAsync(CancellationToken cancellationToken)
        => _ready.Task.WaitAsync(cancellationToken);
}
