using System;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.ShareLinks;

namespace Jellyfin.Server.Implementations.ShareLinks;

/// <summary>
/// Evaluates media sharing permission from the live server configuration and user row.
/// </summary>
public class ShareLinkPolicyService : IShareLinkPolicyService
{
    private readonly IServerConfigurationManager _serverConfigurationManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShareLinkPolicyService"/> class.
    /// </summary>
    /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    public ShareLinkPolicyService(IServerConfigurationManager serverConfigurationManager)
    {
        _serverConfigurationManager = serverConfigurationManager;
    }

    /// <inheritdoc />
    public bool IsSharingAllowed(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return ShareLinkPolicyEvaluator.IsSharingAllowed(
            _serverConfigurationManager.Configuration.MediaSharingPolicy,
            user.MediaSharingPolicy);
    }
}
