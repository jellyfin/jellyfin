using System;

namespace MediaBrowser.Common.Net;

/// <summary>
/// Result of <see cref="INetworkManager.ShouldAllowServerAccess" />.
/// </summary>
public enum RemoteAccessPolicyResult
{
    /// <summary>
    /// The connection should be allowed.
    /// </summary>
    Allow,

    /// <summary>
    /// The connection should be rejected since it is not from a local IP and remote access is disabled.
    /// </summary>
    RejectDueToRemoteAccessDisabled,

    /// <summary>
    /// The connection should be rejected since it is from a blocklisted IP.
    /// </summary>
    RejectDueToIPBlocklist,

    /// <summary>
    /// The connection should be rejected since it is from a remote IP that is not in the allowlist.
    /// </summary>
    RejectDueToNotAllowlistedRemoteIP,
}
