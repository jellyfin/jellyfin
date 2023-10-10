using MediaBrowser.Model.System;
using Microsoft.AspNetCore.Http;

namespace MediaBrowser.Controller;

/// <summary>
/// A service for managing the application instance.
/// </summary>
public interface ISystemManager
{
    /// <summary>
    /// Gets the system info.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>The <see cref="SystemInfo"/>.</returns>
    SystemInfo GetSystemInfo(HttpRequest request);

    /// <summary>
    /// Gets the public system info.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>The <see cref="PublicSystemInfo"/>.</returns>
    PublicSystemInfo GetPublicSystemInfo(HttpRequest request);

    /// <summary>
    /// Starts the application restart process.
    /// </summary>
    void Restart();

    /// <summary>
    /// Starts the application shutdown process.
    /// </summary>
    void Shutdown();
}
