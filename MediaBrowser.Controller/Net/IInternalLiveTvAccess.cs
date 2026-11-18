using Microsoft.AspNetCore.Http;

namespace MediaBrowser.Controller.Net;

/// <summary>
/// Authorizes server-owned encoder requests to Live TV input buffers.
/// </summary>
public interface IInternalLiveTvAccess
{
    /// <summary>
    /// The query parameter carrying the resource-bound internal capability.
    /// </summary>
    const string TokenQueryParameter = "InternalLiveTvToken";

    /// <summary>
    /// Authorizes an input URL only when it addresses a local Live TV buffer.
    /// </summary>
    /// <param name="path">The server-owned encoder input URL.</param>
    /// <returns>The authorized input URL, or the unchanged non-internal path.</returns>
    /// <remarks>
    /// Call this only when building encoder or probe arguments. Never publish the
    /// returned capability in a media source or client response.
    /// </remarks>
    string GetInputUrl(string path);

    /// <summary>
    /// Verifies an internal capability for the exact requested Live TV buffer.
    /// </summary>
    /// <param name="request">The incoming request.</param>
    /// <returns>Whether the request holds the server-issued resource capability.</returns>
    bool IsAuthorizedRequest(HttpRequest request);
}
