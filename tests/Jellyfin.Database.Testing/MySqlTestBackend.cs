namespace Jellyfin.Database.Testing;

/// <summary>
/// Creates MySQL databases, on a container or on the server named by <c>JELLYFIN_TEST_MYSQL_CONNECTIONSTRING</c>.
/// </summary>
public sealed class MySqlTestBackend : MySqlFamilyTestBackend
{
    private MySqlTestBackend()
        : base(TestDbProvider.MySql)
    {
    }

    public static MySqlTestBackend Instance { get; } = new();
}
