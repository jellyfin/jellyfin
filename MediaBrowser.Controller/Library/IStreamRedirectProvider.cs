using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Library;

/// <summary>
/// Provides a redirect URL for an external library item at stream request time.
/// </summary>
public interface IStreamRedirectProvider
{
    /// <summary>
    /// Gets the provider name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a redirect URL for the given item, or <c>null</c> if this provider does not handle it.
    /// </summary>
    /// <param name="item">The item being streamed.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The redirect URL, or <c>null</c>.</returns>
    Task<string?> GetRedirectUrlAsync(BaseItem item, CancellationToken cancellationToken);
}
