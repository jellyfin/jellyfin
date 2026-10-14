namespace Jellyfin.Database.Testing;

/// <summary>
/// Creates MariaDB databases, on a container or on the server named by <c>JELLYFIN_TEST_MARIADB_CONNECTIONSTRING</c>.
/// </summary>
public sealed class MariaDbTestBackend : MySqlFamilyTestBackend
{
    private MariaDbTestBackend()
        : base(TestDbProvider.MariaDb)
    {
    }

    public static MariaDbTestBackend Instance { get; } = new();
}
