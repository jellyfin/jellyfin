using System;
using System.Threading.Tasks;

namespace Jellyfin.Database.Implementations;

internal sealed class NoOpAsyncDisposable : IAsyncDisposable
{
    public static readonly NoOpAsyncDisposable Instance = new NoOpAsyncDisposable();

    private NoOpAsyncDisposable()
    {
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
