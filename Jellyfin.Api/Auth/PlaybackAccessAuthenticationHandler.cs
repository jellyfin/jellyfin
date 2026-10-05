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
/// Authenticates playback credentials only on explicitly opted-in media endpoints.
/// </summary>
public sealed class PlaybackAccessAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IPlaybackAccessManager _playbackAccessManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackAccessAuthenticationHandler"/> class.
    /// </summary>
    /// <param name="options">The authentication options.</param>
    /// <param name="logger">The logger factory.</param>
    /// <param name="encoder">The URL encoder.</param>
    /// <param name="playbackAccessManager">The playback access manager.</param>
    public PlaybackAccessAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IPlaybackAccessManager playbackAccessManager)
        : base(options, logger, encoder)
    {
        _playbackAccessManager = playbackAccessManager;
    }

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var values = Request.Query["PlaybackToken"];
        if (values.Count == 0 || Context.GetEndpoint()?.Metadata.GetMetadata<PlaybackAccessAttribute>() is null)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var grant = values.Count == 1 && values[0] is { } token ? _playbackAccessManager.Get(token) : null;
        if (grant is null || !MatchesRequest(Request, grant))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid playback grant."));
        }

        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(InternalClaimTypes.UserId, grant.UserId.ToString("N", CultureInfo.InvariantCulture)),
                new Claim(InternalClaimTypes.DeviceId, grant.DeviceId),
                new Claim(InternalClaimTypes.PlaybackToken, grant.Token),
                new Claim(InternalClaimTypes.PlaybackSessionId, grant.PlaySessionId)
            },
            Scheme.Name);
        // In particular, do not assign the owner's account role or a general API token.
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }

    internal static bool MatchesRequest(HttpRequest request, PlaybackAccessGrant grant)
    {
        if ((!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method))
            || request.Query.ContainsKey("Params")
            || request.Query.ContainsKey("LiveStreamId"))
        {
            return false;
        }

        var itemValue = request.RouteValues["itemId"] ?? request.RouteValues["routeItemId"];
        if (!Guid.TryParse(Convert.ToString(itemValue, CultureInfo.InvariantCulture), out var itemId) || !itemId.Equals(grant.ItemId))
        {
            return false;
        }

        var sourceValue = request.RouteValues["mediaSourceId"] ?? request.RouteValues["routeMediaSourceId"];
        var isSubtitle = sourceValue is not null;
        if (isSubtitle)
        {
            // Legacy subtitle parameters can override route values in the action.
            if (!string.Equals(Convert.ToString(sourceValue, CultureInfo.InvariantCulture), grant.MediaSourceId, StringComparison.Ordinal)
                || (request.Query.ContainsKey("itemId") && !MatchesItemQuery(request, grant.ItemId))
                || (request.Query.ContainsKey("MediaSourceId") && !MatchesQuery(request, "MediaSourceId", grant.MediaSourceId)))
            {
                return false;
            }
        }
        else if (!MatchesQuery(request, "MediaSourceId", grant.MediaSourceId)
                 || !MatchesQuery(request, "DeviceId", grant.DeviceId))
        {
            return false;
        }

        return MatchesQuery(request, "PlaySessionId", grant.PlaySessionId);
    }

    private static bool MatchesQuery(HttpRequest request, string key, string expected)
    {
        var values = request.Query[key];
        return values.Count == 1 && string.Equals(values[0], expected, StringComparison.Ordinal);
    }

    private static bool MatchesItemQuery(HttpRequest request, Guid expected)
    {
        var values = request.Query["itemId"];
        return values.Count == 1 && Guid.TryParse(values[0], out var actual) && actual.Equals(expected);
    }
}
