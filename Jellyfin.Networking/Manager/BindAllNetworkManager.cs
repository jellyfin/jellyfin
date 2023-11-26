using System.Collections.Generic;
using System.Net;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Networking.Manager;

/// <summary>
///     Defines a network manager that handels the specific networking requirements for github codespaces.
/// </summary>
public class BindAllNetworkManager : NetworkManager
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BindAllNetworkManager"/> class.
    /// </summary>
    /// <param name="configurationManager">IServerConfigurationManager instance.</param>
    /// <param name="startupConfig">The <see cref="IConfiguration"/> instance holding startup parameters.</param>
    /// <param name="logger">Logger to use for messages.</param>
    public BindAllNetworkManager(MediaBrowser.Common.Configuration.IConfigurationManager configurationManager, IConfiguration startupConfig, ILogger<NetworkManager> logger)
        : base(configurationManager, startupConfig, logger)
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<IPData> GetAllBindInterfaces(bool individualInterfaces = false)
    {
        return new IPData[]
        {
            new(IPAddress.Any, null)
        };
    }
}
