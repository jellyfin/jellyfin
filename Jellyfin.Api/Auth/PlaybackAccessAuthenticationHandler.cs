using System;
using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using MediaBrowser.Controller.Streaming;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jellyfin.Api.Auth;

/// <summary>
/// Playback grant authentication handler.
/// </summary>
public class PlaybackAccessAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>
    /// The query parameter carrying the playback grant token.
    /// </summary>
    public const string TokenParameter = "PlaybackToken";

    private readonly IPlaybackAccessManager _playbackAccessManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackAccessAuthenticationHandler"/> class.
    /// </summary>
    /// <param name="playbackAccessManager">Instance of the <see cref="IPlaybackAccessManager"/> interface.</param>
    /// <param name="options">Options monitor.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="encoder">The url encoder.</param>
    public PlaybackAccessAuthenticationHandler(
        IPlaybackAccessManager playbackAccessManager,
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
        _playbackAccessManager = playbackAccessManager;
    }

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? token = Request.Query[TokenParameter];
        if (string.IsNullOrEmpty(token))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var grant = _playbackAccessManager.Get(token);
        if (grant is null || !MatchesRequest(grant))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid playback grant."));
        }

        // The grant owner's role and access token are deliberately not carried over.
        var claims = new[]
        {
            new Claim(InternalClaimTypes.UserId, grant.UserId.ToString("N", CultureInfo.InvariantCulture)),
            new Claim(InternalClaimTypes.DeviceId, grant.DeviceId),
            new Claim(InternalClaimTypes.PlaybackToken, grant.Token)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private bool MatchesRequest(PlaybackAccessGrant grant)
    {
        var route = Request.RouteValues;
        var query = Request.Query;
        if (!Guid.TryParse(route["itemId"] as string, out var itemId) || !itemId.Equals(grant.ItemId))
        {
            return false;
        }

        // The subtitle playlist takes its media source from the route.
        if (route["mediaSourceId"] is string mediaSourceId)
        {
            return string.Equals(mediaSourceId, grant.MediaSourceId, StringComparison.Ordinal);
        }

        // Params and LiveStreamId would replace the media source that is checked here.
        return query["MediaSourceId"] == grant.MediaSourceId
            && query["DeviceId"] == grant.DeviceId
            && query["PlaySessionId"] == grant.PlaySessionId
            && !query.ContainsKey("Params")
            && !query.ContainsKey("LiveStreamId");
    }
}
