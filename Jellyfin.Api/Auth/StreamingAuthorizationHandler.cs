using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using MediaBrowser.Controller.Streaming;
using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Api.Auth;

/// <summary>
/// Completes streaming authorization after the default user and network checks.
/// </summary>
public sealed class StreamingAuthorizationHandler : AuthorizationHandler<StreamingAuthorizationRequirement>
{
    private readonly IPlaybackAccessManager _playbackAccessManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamingAuthorizationHandler"/> class.
    /// </summary>
    /// <param name="playbackAccessManager">The playback access manager.</param>
    public StreamingAuthorizationHandler(IPlaybackAccessManager playbackAccessManager)
    {
        _playbackAccessManager = playbackAccessManager;
    }

    /// <inheritdoc />
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, StreamingAuthorizationRequirement requirement)
    {
        if (!context.HasFailed && context.User.Identities.Any(identity => identity.IsAuthenticated
            && (identity.AuthenticationType == AuthenticationSchemes.CustomAuthentication
                || identity.AuthenticationType == AuthenticationSchemes.PlaybackAccess)))
        {
            context.Succeed(requirement);
            var playbackIdentity = context.User.Identities.FirstOrDefault(identity => identity.AuthenticationType == AuthenticationSchemes.PlaybackAccess);
            var token = playbackIdentity?.FindFirst(InternalClaimTypes.PlaybackToken)?.Value;
            if (token is not null)
            {
                _playbackAccessManager.Touch(token);
            }
        }

        return Task.CompletedTask;
    }
}
