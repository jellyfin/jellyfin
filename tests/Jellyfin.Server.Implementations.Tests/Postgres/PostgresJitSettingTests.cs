using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.DbConfiguration;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Postgres;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Postgres;

public sealed class PostgresJitSettingTests : DatabaseTestFixture
{
    private const string Password = "jit-test";

    private readonly CollectingLogger<PostgresDatabaseProvider> _logger = new();
    private readonly IJellyfinDatabaseProvider _provider;
    private readonly List<string> _roles = [];

    public PostgresJitSettingTests()
    {
        _provider = new PostgresDatabaseProvider(_logger);
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task ApplyDatabaseSettings_JitIsOn_NewAndPooledSessionsHaveItOff()
    {
        var role = await CreateRoleAsync();

        var before = await ReadSessionAsync(role);
        await ApplyAsync(role);
        var after = await ReadSessionAsync(role);
        var reused = await ReadSessionAsync(role);

        Assert.Equal("on", before.Jit);
        Assert.NotEqual(before.BackendPid, after.BackendPid);
        Assert.Equal("off", after.Jit);
        Assert.Equal(after.BackendPid, reused.BackendPid);
        Assert.Equal("off", reused.Jit);
        Assert.Equal(["jit=off"], await ReadRoleSettingsAsync(role));
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task ApplyDatabaseSettings_JitIsAlreadyOff_ChangesNothing()
    {
        var role = await CreateRoleAsync();
        await ExecuteAsAdminAsync($"ALTER ROLE {Quote(role)} SET jit = off");
        var settingsBefore = await ReadRoleSettingsAsync(role);

        await ApplyAsync(role);

        Assert.Equal(settingsBefore, await ReadRoleSettingsAsync(role));
        Assert.DoesNotContain(_logger.Entries, e => e.Message.Contains("JIT", StringComparison.Ordinal));
    }

    [ProviderTheory(TestDbProvider.Postgres)]
    [InlineData("ALTER ROLE {role} IN DATABASE {database} SET jit = on")]
    [InlineData("ALTER ROLE {role} SET jit = on")]
    [InlineData("ALTER DATABASE {database} SET jit = on")]
    [InlineData("ALTER ROLE ALL IN DATABASE {database} SET jit = on")]
    [InlineData("ALTER ROLE ALL SET jit = on")]
    public async Task ApplyDatabaseSettings_JitIsSetOnByTheAdministrator_LeavesItOn(string statement)
    {
        var role = await CreateRoleAsync();
        await ExecuteAsAdminAsync(statement
            .Replace("{role}", Quote(role), StringComparison.Ordinal)
            .Replace("{database}", Quote(await ReadDatabaseNameAsync()), StringComparison.Ordinal));
        var settingsBefore = await ReadRoleSettingsAsync(role);

        await ApplyAsync(role);

        Assert.Equal("on", (await ReadSessionAsync(role)).Jit);
        Assert.Equal(settingsBefore, await ReadRoleSettingsAsync(role));
        var entry = Assert.Single(_logger.Entries, e => e.Message.Contains("JIT", StringComparison.Ordinal));
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains("as configured", entry.Message, StringComparison.Ordinal);
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task ApplyDatabaseSettings_JitIsTurnedOff_LogsTheCommandThatKeepsItOn()
    {
        var role = await CreateRoleAsync();
        var database = await ReadDatabaseNameAsync();

        await ApplyAsync(role);

        var entry = Assert.Single(_logger.Entries, e => e.Message.Contains("JIT", StringComparison.Ordinal));
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains($"ALTER ROLE {Quote(role)} IN DATABASE {Quote(database)} SET jit = on", entry.Message, StringComparison.Ordinal);
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task ApplyDatabaseSettings_AppliedTwice_AltersOnce()
    {
        var role = await CreateRoleAsync();

        await ApplyAsync(role);
        await ApplyAsync(role);

        Assert.Single(_logger.Entries, e => e.Message.Contains("Turned off JIT", StringComparison.Ordinal));
        Assert.Equal(["jit=off"], await ReadRoleSettingsAsync(role));
        Assert.Equal("off", (await ReadSessionAsync(role)).Jit);
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task ApplyDatabaseSettings_RoleMayNotAlterSettings_WarnsWithTheCommand()
    {
        var role = await CreateRoleAsync();
        await ExecuteAsAdminAsync($"ALTER ROLE {Quote(role)} SET default_transaction_read_only = on");
        var database = await ReadDatabaseNameAsync();

        await ApplyAsync(role);

        var entry = Assert.Single(_logger.Entries, e => e.Message.Contains("JIT", StringComparison.Ordinal));
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains($"ALTER ROLE {Quote(role)} IN DATABASE {Quote(database)} SET jit = off", entry.Message, StringComparison.Ordinal);
        Assert.Equal("on", (await ReadSessionAsync(role)).Jit);
        Assert.DoesNotContain(await ReadRoleSettingsAsync(role), e => e.StartsWith("jit=", StringComparison.Ordinal));
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task ApplyDatabaseSettings_RoleCannotConnect_WarnsAndDoesNotThrow()
    {
        var role = await CreateRoleAsync();
        await ExecuteAsAdminAsync($"ALTER ROLE {Quote(role)} NOLOGIN");

        await ApplyAsync(role);

        var entry = Assert.Single(_logger.Entries, e => e.Message.Contains("JIT", StringComparison.Ordinal));
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains("Could not", entry.Message, StringComparison.Ordinal);
        Assert.Empty(await ReadRoleSettingsAsync(role));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var role in _roles)
            {
                using var connection = new NpgsqlConnection(CreateRoleConnectionString(role));
                NpgsqlConnection.ClearPool(connection);
            }

            using var admin = CreateDbContext();
            admin.Database.ExecuteSqlRaw("ALTER ROLE ALL RESET jit");
            foreach (var role in _roles)
            {
                admin.Database.ExecuteSqlRaw(EscapeBraces($"DROP ROLE IF EXISTS {Quote(role)}"));
            }
        }

        base.Dispose(disposing);
    }

    private static string Quote(string identifier) => "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    private static string EscapeBraces(string statement)
        => statement.Replace("{", "{{", StringComparison.Ordinal).Replace("}", "}}", StringComparison.Ordinal);

    private async Task<string> CreateRoleAsync()
    {
        // Case, a space, a dot, a quote and braces in the name prove that no statement reads it back as an unquoted
        // identifier and that every statement that names the role is quoted and escaped.
        var role = $"JF jit.\"{{x}}{Guid.NewGuid():N}";
        await ExecuteAsAdminAsync($"CREATE ROLE {Quote(role)} LOGIN PASSWORD '{Password}'");
        _roles.Add(role);
        return role;
    }

    private async Task ExecuteAsAdminAsync(string statement)
    {
        await using var admin = CreateDbContext();
        await admin.Database.ExecuteSqlRawAsync(EscapeBraces(statement), TestContext.Current.CancellationToken);
    }

    private async Task<string> ReadDatabaseNameAsync()
    {
        await using var admin = CreateDbContext();
        return (await admin.Database.SqlQueryRaw<string>("SELECT current_database() AS \"Value\"").ToListAsync(TestContext.Current.CancellationToken)).Single();
    }

    private async Task<List<string>> ReadRoleSettingsAsync(string role)
    {
        await using var admin = CreateDbContext();
        return await admin.Database
            .SqlQueryRaw<string>(
                """
                SELECT c AS "Value"
                FROM pg_db_role_setting s
                CROSS JOIN unnest(s.setconfig) AS c
                WHERE s.setrole = (SELECT oid FROM pg_roles WHERE rolname = {0})
                ORDER BY 1
                """,
                role)
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    private string CreateRoleConnectionString(string role)
    {
        using var admin = CreateDbContext();
        var adminConnectionString = RelationalOptionsExtension.Extract(admin.GetService<IDbContextOptions>()).ConnectionString!;
        return new NpgsqlConnectionStringBuilder(adminConnectionString)
        {
            Username = role,
            Password = Password
        }.ConnectionString;
    }

    private JellyfinDbContext CreateRoleContext(string role)
    {
        var options = new DbContextOptionsBuilder<JellyfinDbContext>();
        _provider.Initialise(
            options,
            new DatabaseConfigurationOptions
            {
                DatabaseType = "Jellyfin-PostgreSQL",
                CustomProviderOptions = new CustomDatabaseOptions
                {
                    PluginName = string.Empty,
                    PluginAssembly = string.Empty,
                    ConnectionString = CreateRoleConnectionString(role)
                }
            });

        return new JellyfinDbContext(options.Options, NullLogger<JellyfinDbContext>.Instance, _provider, new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
    }

    private async Task ApplyAsync(string role)
    {
        await using var context = CreateRoleContext(role);
        await _provider.ApplyDatabaseSettingsAsync(context, TestContext.Current.CancellationToken);
    }

    private async Task<(string Jit, int BackendPid)> ReadSessionAsync(string role)
    {
        await using var context = CreateRoleContext(role);
        var session = await context.Database
            .SqlQueryRaw<SessionRow>("SELECT current_setting('jit') AS \"Jit\", pg_backend_pid() AS \"BackendPid\"")
            .ToListAsync(TestContext.Current.CancellationToken);
        return (session.Single().Jit, session.Single().BackendPid);
    }

    private sealed record SessionRow(string Jit, int BackendPid);
}
