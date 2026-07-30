#pragma warning disable CS1591, SA1402, SA1649

using System;
using System.Linq;
using System.Text.Json;
using MediaBrowser.Controller.CustomNetflix;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal static class CustomNetflixHomeSnapshots
{
    public const int MinLimit = 1;

    public const int MaxLimit = 50;

    public static readonly TimeSpan SnapshotTtl = TimeSpan.FromMinutes(5);

    public static int NormalizeLimit(int limit)
        => Math.Clamp(limit, MinLimit, MaxLimit);

    public static string SnapshotKey(int normalizedLimit)
        => $"v4:l{NormalizeLimit(normalizedLimit)}";

    public static string[] CacheKeys(Guid profileId)
        => Enumerable.Range(MinLimit, MaxLimit - MinLimit + 1)
            .Select(limit => RedisKeyBuilder.Home(profileId, SnapshotKey(limit)))
            .ToArray();

    public static string Serialize(
        Guid profileId,
        string snapshotKey,
        CustomNetflixHomeResponseDto response,
        DateTime generatedAt,
        DateTime expiresAt)
        => JsonSerializer.Serialize(new HomeSnapshotPayload(
            profileId,
            snapshotKey,
            generatedAt,
            expiresAt,
            response));

    public static CustomNetflixHomeResponseDto? Deserialize(string json, Guid profileId, string snapshotKey, DateTime utcNow)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<HomeSnapshotPayload>(json);
            if (payload is null
                || !payload.ProfileId.Equals(profileId)
                || !string.Equals(payload.SnapshotKey, snapshotKey, StringComparison.Ordinal)
                || payload.ExpiresAt <= utcNow)
            {
                return null;
            }

            return payload.Response;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record HomeSnapshotPayload(
        Guid ProfileId,
        string SnapshotKey,
        DateTime GeneratedAt,
        DateTime ExpiresAt,
        CustomNetflixHomeResponseDto Response);
}
