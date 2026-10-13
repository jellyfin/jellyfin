using Jellyfin.Database.Testing;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Postgres;

public class PostgresTestBackendTests
{
    [ProviderFact(TestDbProvider.Postgres)]
    public void Dispose_DropsTheDatabase()
    {
        var database = TestDatabaseEnvironment.Backend.Create(TestSchemaSource.Model, []);
        var connectionString = new NpgsqlConnectionStringBuilder(RelationalOptionsExtension.Extract(database.Options).ConnectionString);
        var name = connectionString.Database!;
        connectionString.Database = "postgres";

        Assert.True(DatabaseExists(connectionString.ConnectionString, name));

        database.Dispose();

        Assert.False(DatabaseExists(connectionString.ConnectionString, name));
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public void Create_InContainerMode_KeepsTheDataDirectoryUnderTheMountedStorage()
    {
        Assert.SkipUnless(PostgresTestBackend.Instance.CanRunServerTools, "The server is not a container started by the backend.");

        using var database = TestDatabaseEnvironment.Backend.Create(TestSchemaSource.Model, []);
        using var connection = new NpgsqlConnection(RelationalOptionsExtension.Extract(database.Options).ConnectionString);
        connection.Open();
        using var command = new NpgsqlCommand("SHOW data_directory", connection);

        Assert.Equal("/var/lib/postgresql/pgdata", (string)command.ExecuteScalar()!);
    }

    private static bool DatabaseExists(string adminConnectionString, string name)
    {
        using var connection = new NpgsqlConnection(adminConnectionString);
        connection.Open();
        using var command = new NpgsqlCommand("SELECT count(*) FROM pg_database WHERE datname = @name", connection);
        command.Parameters.AddWithValue("name", name);
        return (long)command.ExecuteScalar()! == 1;
    }
}
