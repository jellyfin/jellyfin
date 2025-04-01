using System.Globalization;
using System.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;

namespace Emby.Server.Implementations.Library;

/// <summary>
/// IPathManager implementation.
/// </summary>
public class PathManager : IPathManager
{
    private readonly IServerConfigurationManager _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="PathManager"/> class.
    /// </summary>
    /// <param name="config">The server configuration manager.</param>
    public PathManager(
        IServerConfigurationManager config)
    {
        _config = config;
    }

    /// <inheritdoc />
    public string GetTrickplayDirectory(BaseItem item, bool saveWithMedia = false)
    {
        var basePath = _config.ApplicationPaths.TrickplayPath;
        var idString = item.Id.ToString("N", CultureInfo.InvariantCulture);

        return saveWithMedia
            ? Path.Combine(item.ContainingFolderPath, Path.ChangeExtension(item.Path, ".trickplay"))
            : Path.Combine(basePath, idString);
    }
}
