using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Paseto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.ShareLinks;

namespace Jellyfin.Server.Implementations.ShareLinks;

/// <summary>
/// Creates and validates scoped, encrypted share link tokens bound to a per-user key.
/// </summary>
public sealed class ShareLinkTokenService : IShareLinkTokenService
{
    private const int MaxActiveLinksPerUser = 1000;
    private const long MaxExpirationSeconds = 365L * 24 * 60 * 60;
    private const int MinDurationSeconds = 60;
    private const int MaxDurationSeconds = 7_776_000;
    private const int JtiLength = 32;

    private static readonly JsonSerializerOptions PayloadJsonOptions = new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IUserManager _userManager;
    private readonly IShareLinkRepository _shareLinkRepository;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShareLinkTokenService"/> class.
    /// </summary>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="shareLinkRepository">Instance of the <see cref="IShareLinkRepository"/> interface.</param>
    /// <param name="timeProvider">The clock used for issuance, expiration and first-access timestamps.</param>
    public ShareLinkTokenService(IUserManager userManager, IShareLinkRepository shareLinkRepository, TimeProvider timeProvider)
    {
        _userManager = userManager;
        _shareLinkRepository = shareLinkRepository;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<ShareLinkCreationResult> CreateAsync(
        Guid userId,
        Guid itemId,
        long? expiresAtUnixSeconds,
        int? durationSeconds,
        CancellationToken cancellationToken = default)
    {
        long now = _timeProvider.GetUtcNow().ToUnixTimeSeconds();

        if (expiresAtUnixSeconds is null && durationSeconds is null)
        {
            return new ShareLinkCreationResult(null, null, ShareLinkCreateErrorCode.ExpirationOrDurationRequired);
        }

        if (expiresAtUnixSeconds is { } expiresAt && (expiresAt <= now || expiresAt > now + MaxExpirationSeconds))
        {
            return new ShareLinkCreationResult(null, null, ShareLinkCreateErrorCode.InvalidExpiration);
        }

        if (durationSeconds is { } duration && (duration < MinDurationSeconds || duration > MaxDurationSeconds))
        {
            return new ShareLinkCreationResult(null, null, ShareLinkCreateErrorCode.InvalidDuration);
        }

        var user = _userManager.GetUserById(userId);
        if (user is null || user.HasPermission(PermissionKind.IsDisabled))
        {
            return new ShareLinkCreationResult(null, null, ShareLinkCreateErrorCode.UserDisabled);
        }

        var existingLinks = await _shareLinkRepository.ListByUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (CountActiveLinks(existingLinks, now) >= MaxActiveLinksPerUser)
        {
            return new ShareLinkCreationResult(null, null, ShareLinkCreateErrorCode.ActiveLinkLimitReached);
        }

        if (!ShareLinkKeyHelper.TryDecode(user.ShareLinkKey, out byte[]? keyBytes))
        {
            user.ShareLinkKey = ShareLinkKeyHelper.Generate();
            await _userManager.UpdateUserAsync(user).ConfigureAwait(false);

            if (!ShareLinkKeyHelper.TryDecode(user.ShareLinkKey, out keyBytes))
            {
                throw new InvalidOperationException("Failed to generate a valid share link key.");
            }
        }

        string jti = CreateJti();
        string payload = JsonSerializer.Serialize(
            new ShareLinkPayload
            {
                Jti = jti,
                UserId = userId,
                ItemId = itemId,
                IssuedAt = now,
                ExpiresAt = expiresAtUnixSeconds,
                DurationSeconds = durationSeconds
            },
            PayloadJsonOptions);
        string footer = $"{{\"uid\":\"{userId:N}\"}}";
        string token = PasetoV4Local.Encrypt(keyBytes, payload, footer);

        var shareLink = new ShareLink(jti, userId, itemId, now)
        {
            ExpiresAt = expiresAtUnixSeconds,
            DurationSeconds = durationSeconds
        };

        await _shareLinkRepository.CreateAsync(shareLink, cancellationToken).ConfigureAwait(false);

        return new ShareLinkCreationResult(token, shareLink, null);
    }

    /// <inheritdoc />
    public async Task<ShareLinkValidationResult> ValidateAsync(
        string shareToken,
        Guid? routeItemId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(shareToken);

        long now = _timeProvider.GetUtcNow().ToUnixTimeSeconds();

        if (!TryGetFooterUid(shareToken, out Guid footerUserId, out string footer))
        {
            return Failure(ShareLinkErrorCode.DecryptFailed);
        }

        var user = _userManager.GetUserById(footerUserId);
        if (user is null || user.HasPermission(PermissionKind.IsDisabled))
        {
            return Failure(ShareLinkErrorCode.UserDisabled);
        }

        if (!ShareLinkKeyHelper.TryDecode(user.ShareLinkKey, out byte[]? keyBytes))
        {
            return Failure(ShareLinkErrorCode.DecryptFailed);
        }

        string payload;
        try
        {
            payload = PasetoV4Local.Decrypt(keyBytes, shareToken, footer);
        }
        catch (PasetoException)
        {
            return Failure(ShareLinkErrorCode.DecryptFailed);
        }

        if (!TryParsePayload(payload, out ShareLinkPayload? claims) || claims is null)
        {
            return Failure(ShareLinkErrorCode.DecryptFailed);
        }

        if (!claims.UserId.Equals(footerUserId))
        {
            return Failure(ShareLinkErrorCode.DecryptFailed);
        }

        if (routeItemId is { } requestedItemId && !requestedItemId.Equals(claims.ItemId))
        {
            return Failure(ShareLinkErrorCode.ItemMismatch);
        }

        var shareLink = await _shareLinkRepository.GetByIdAsync(claims.Jti!, cancellationToken).ConfigureAwait(false);
        if (shareLink is null)
        {
            return Failure(ShareLinkErrorCode.LinkStateMissing);
        }

        if (shareLink.IsRevoked)
        {
            return Failure(ShareLinkErrorCode.LinkRevoked);
        }

        if (claims.ExpiresAt is { } expiresAt && now >= expiresAt)
        {
            return Failure(ShareLinkErrorCode.LinkExpired);
        }

        int? durationSeconds = shareLink.DurationSeconds ?? claims.DurationSeconds;
        if (durationSeconds is { } duration)
        {
            long? firstAccessedAt = shareLink.FirstAccessedAt;
            if (firstAccessedAt is null)
            {
                bool firstAccessSet = await _shareLinkRepository.TrySetFirstAccessAsync(claims.Jti!, now, cancellationToken).ConfigureAwait(false);
                if (firstAccessSet)
                {
                    firstAccessedAt = now;
                }
                else
                {
                    var reloaded = await _shareLinkRepository.GetByIdAsync(claims.Jti!, cancellationToken).ConfigureAwait(false);
                    if (reloaded is null)
                    {
                        return Failure(ShareLinkErrorCode.LinkStateMissing);
                    }

                    firstAccessedAt = reloaded.FirstAccessedAt;
                }
            }

            if (firstAccessedAt is { } firstAccess && now >= firstAccess + duration)
            {
                return Failure(ShareLinkErrorCode.LinkExpired);
            }
        }

        return new ShareLinkValidationResult(
            claims.Jti,
            claims.UserId,
            claims.ItemId,
            claims.IssuedAt,
            claims.ExpiresAt,
            claims.DurationSeconds,
            null);
    }

    private static ShareLinkValidationResult Failure(ShareLinkErrorCode errorCode)
    {
        return new ShareLinkValidationResult(null, Guid.Empty, Guid.Empty, 0, null, null, errorCode);
    }

    private static string CreateJti()
    {
        byte[] bytes = new byte[JtiLength / 2];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static int CountActiveLinks(IReadOnlyList<ShareLink> links, long now)
    {
        int active = 0;
        foreach (var link in links)
        {
            if (link.IsRevoked)
            {
                continue;
            }

            if (link.ExpiresAt is { } expiresAt && now >= expiresAt)
            {
                continue;
            }

            if (link.DurationSeconds is { } duration
                && link.FirstAccessedAt is { } firstAccessedAt
                && now >= firstAccessedAt + duration)
            {
                continue;
            }

            active++;
        }

        return active;
    }

    private static bool TryGetFooterUid(string shareToken, out Guid userId, out string footer)
    {
        userId = Guid.Empty;
        footer = string.Empty;

        if (string.IsNullOrEmpty(shareToken))
        {
            return false;
        }

        string[] parts = shareToken.Split('.');
        if (parts.Length != 4 || parts[3].Length == 0)
        {
            return false;
        }

        byte[] footerBytes;
        try
        {
            footerBytes = DecodeBase64Url(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(footerBytes);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("uid", out var uidElement)
                || uidElement.ValueKind != JsonValueKind.String
                || !Guid.TryParse(uidElement.GetString(), out userId))
            {
                return false;
            }

            footer = Encoding.UTF8.GetString(footerBytes);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static byte[] DecodeBase64Url(string value)
    {
        string base64 = value.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2:
                base64 += "==";
                break;
            case 3:
                base64 += "=";
                break;
            case 1:
                throw new FormatException("Invalid base64url length.");
            default:
                break;
        }

        return Convert.FromBase64String(base64);
    }

    private static bool TryParsePayload(string payload, out ShareLinkPayload? claims)
    {
        claims = null;
        try
        {
            claims = JsonSerializer.Deserialize<ShareLinkPayload>(payload, PayloadJsonOptions);
        }
        catch (JsonException)
        {
            return false;
        }

        if (claims is null || claims.Jti is null || claims.Jti.Length != JtiLength || !IsLowerHex(claims.Jti))
        {
            return false;
        }

        return true;
    }

    private static bool IsLowerHex(string value)
    {
        foreach (char character in value)
        {
            if (!((character >= '0' && character <= '9') || (character >= 'a' && character <= 'f')))
            {
                return false;
            }
        }

        return true;
    }

    private sealed record ShareLinkPayload
    {
        [JsonPropertyName("jti")]
        public string? Jti { get; init; }

        [JsonPropertyName("uid")]
        public Guid UserId { get; init; }

        [JsonPropertyName("iid")]
        public Guid ItemId { get; init; }

        [JsonPropertyName("iat")]
        public long IssuedAt { get; init; }

        [JsonPropertyName("exp")]
        public long? ExpiresAt { get; init; }

        [JsonPropertyName("dur")]
        public int? DurationSeconds { get; init; }
    }
}
