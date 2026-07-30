#pragma warning disable CS1591

using System;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal static class RedisKeyBuilder
{
    public static string ActiveProfile(Guid userId, string tokenHash)
        => $"cnx:active-profile:{userId:N}:{tokenHash}";

    public static string Home(Guid profileId, string snapshotKey)
        => $"cnx:home:{profileId:N}:{snapshotKey}";

    public static string RankingSnapshot(string rankingId)
        => $"cnx:ranking:{rankingId}:v1";
}
