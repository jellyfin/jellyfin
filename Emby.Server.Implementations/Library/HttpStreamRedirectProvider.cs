using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace Emby.Server.Implementations.Library;

/// <summary>
/// Redirects external items with HTTP media paths directly to their remote URL.
/// Runs last so plugin providers can take priority.
/// </summary>
public class HttpStreamRedirectProvider : IStreamRedirectProvider
{
    /// <inheritdoc />
    public string Name => "Http";

    /// <inheritdoc />
    public int Order => int.MaxValue;

    /// <inheritdoc />
    public Task<string?> GetRedirectUrlAsync(BaseItem item, CancellationToken cancellationToken)
    {
        if (item.SourceType != SourceType.External)
        {
            return Task.FromResult<string?>(null);
        }

        var path = item.Path;
        if (string.IsNullOrEmpty(path)
            || (!path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
        {
            return Task.FromResult<string?>(null);
        }

        return Task.FromResult<string?>(path);
    }
}
