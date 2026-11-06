using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Library;

/// <summary>
/// Provides stream redirect information for an external library item at request time.
/// </summary>
public interface IStreamRedirectProvider
{
    /// <summary>
    /// Gets the provider name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the provider order. Lower values run first. Use <see cref="int.MaxValue"/> for fallback providers.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Gets a redirect result for the given item, or <c>null</c> if this provider does not handle it.
    /// </summary>
    /// <param name="item">The item being streamed.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="StreamRedirectResult"/>, or <c>null</c>.</returns>
    Task<StreamRedirectResult?> GetRedirectAsync(BaseItem item, CancellationToken cancellationToken);
}
