#pragma warning disable CS1591

namespace Jellyfin.Server.Implementations.CustomNetflix;

public sealed class CustomNetflixOptions
{
    public string? PostgreSqlConnectionString { get; set; }

    public string? RedisConnectionString { get; set; }

    public int MaxProfilesPerAccount { get; set; } = 5;

    public bool IsPostgreSqlEnabled => !string.IsNullOrWhiteSpace(PostgreSqlConnectionString);

    public bool IsRedisEnabled => !string.IsNullOrWhiteSpace(RedisConnectionString);
}
