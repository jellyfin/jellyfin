using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Conventions;
using Jellyfin.Database.Implementations.DbConfiguration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;
using Microting.EntityFrameworkCore.MySql.Infrastructure;
using MySqlConnector;

namespace Jellyfin.Database.Providers.MySql;

/// <summary>
/// Configures jellyfin to use a MySQL or a MariaDB database.
/// </summary>
/// <remarks>
/// One provider serves both servers. The server is named in the <c>server-version</c> option, for example
/// <c>8.4-mysql</c> or <c>11.4.7-mariadb</c>, and is asked for its kind and version when the option is missing.
/// </remarks>
[JellyfinDatabaseProviderKey("Jellyfin-MySQL")]
public sealed class MySqlDatabaseProvider : IJellyfinDatabaseProvider
{
    /// <summary>
    /// The collation of every text column. It compares the code points and is the same on both servers.
    /// </summary>
    internal const string Collation = "utf8mb4_0900_bin";

    private const string ConnectionCharacterSet = "utf8mb4";
    private const int DefaultCommandTimeout = 60;
    private const int MaxIdentifierLength = 64;

    private const string ConnectionStringGuidance =
        "Set CustomProviderOptions/ConnectionString in database.xml, "
        + "or the database:ConnectionString configuration key (environment variable JELLYFIN_DATABASE__CONNECTIONSTRING).";

    private static readonly TimeSpan _maintenanceCommandTimeout = TimeSpan.FromHours(1);

    // The server builds a provider for the migrations that run before it starts and another for the running server, and
    // each of them is set up once. The warning is given once for each database in the process, so a start shows it once.
    private static readonly ConcurrentDictionary<string, bool> _databasesWarnedOf = new(StringComparer.Ordinal);

    private readonly ILogger<MySqlDatabaseProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlDatabaseProvider"/> class.
    /// </summary>
    /// <param name="logger">A logger.</param>
    public MySqlDatabaseProvider(ILogger<MySqlDatabaseProvider> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public IDbContextFactory<JellyfinDbContext>? DbContextFactory { get; set; }

    /// <inheritdoc/>
    public bool SupportsTransactionalMigrations => false;

    /// <inheritdoc/>
    public void Initialise(DbContextOptionsBuilder options, DatabaseConfigurationOptions databaseConfiguration)
    {
        var customOptions = databaseConfiguration.CustomProviderOptions;
        if (string.IsNullOrWhiteSpace(customOptions?.ConnectionString))
        {
            throw new InvalidOperationException("No MySQL or MariaDB connection string is configured. " + ConnectionStringGuidance);
        }

        var providerOptions = customOptions.Options;
        MySqlConnectionStringBuilder connectionStringBuilder;
        try
        {
            connectionStringBuilder = new MySqlConnectionStringBuilder(customOptions.ConnectionString);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException)
        {
            // Neither the connection string nor the exception is passed on, as the messages can repeat keywords and
            // values of the connection string, and it can hold a password.
            throw new InvalidOperationException(
                "The configured MySQL or MariaDB connection string is not valid. It is not shown, as it may contain a password. "
                + ConnectionStringGuidance);
        }

        WarnOnceThatTheProviderIsExperimental(connectionStringBuilder);

        var commandTimeout = GetOption<int?>(providerOptions, "command-timeout", e => int.Parse(e, CultureInfo.InvariantCulture));
        if (commandTimeout is not null)
        {
            connectionStringBuilder.DefaultCommandTimeout = (uint)commandTimeout.Value;
        }
        else if (!SpecifiesCommandTimeout(customOptions.ConnectionString))
        {
            connectionStringBuilder.DefaultCommandTimeout = DefaultCommandTimeout;
        }

        var maxPoolSize = GetOption<int?>(providerOptions, "max-pool-size", e => int.Parse(e, CultureInfo.InvariantCulture));
        if (maxPoolSize is not null)
        {
            connectionStringBuilder.MaximumPoolSize = (uint)maxPoolSize.Value;
        }

        // Ids are stored as 16 bytes and not as text, and a statement can only bind the values it is given.
        connectionStringBuilder.GuidFormat = MySqlGuidFormat.Binary16;
        connectionStringBuilder.AllowUserVariables = false;
        RequireUtf8Mb4(connectionStringBuilder);

        var connectionString = connectionStringBuilder.ConnectionString;
        var configuredServer = GetOption(providerOptions, "server-version", e => e);
        var server = ResolveServer(configuredServer, connectionString);

        var loggable = new MySqlConnectionStringBuilder(connectionString) { Password = string.Empty };
        _logger.LogInformation(
            "{Flavor} {Version} connection string: {ConnectionString}",
            server.Flavor == ServerFlavor.MariaDb ? "MariaDB" : "MySQL",
            server.Version,
            loggable.ConnectionString);

        var version = new Version(server.Version.Major, server.Version.Minor, Math.Max(server.Version.Build, 0));
        ServerVersion serverVersion = server.Flavor == ServerFlavor.MariaDb ? new MariaDbServerVersion(version) : new MySqlServerVersion(version);
        options
            .UseMySql(
                connectionString,
                serverVersion,
                mySqlOptions => mySqlOptions
                    .MigrationsAssembly(GetType().Assembly)
                    .UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery)
                    .UseParameterizedCollectionMode(ParameterTranslationMode.MultipleParameters))
            .AddInterceptors(
                string.IsNullOrWhiteSpace(configuredServer) ? SessionSettingsInterceptor.Instance : SessionSettingsInterceptor.ForConfiguredServer,
                CollectionParameterInterceptor.Instance);

        if (server.Flavor == ServerFlavor.MariaDb)
        {
            options.AddInterceptors(UnionAggregateInterceptor.Instance);
        }

        var enableSensitiveDataLogging = GetOption(providerOptions, "EnableSensitiveDataLogging", e => e.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase));
        if (enableSensitiveDataLogging)
        {
            options.EnableSensitiveDataLogging();
            _logger.LogInformation("EnableSensitiveDataLogging is enabled on MySQL connection");
        }
    }

    /// <inheritdoc/>
    public void OnModelCreating(ModelBuilder modelBuilder)
    {
        AsciiCaseInsensitiveLike.Register(modelBuilder);
    }

    /// <inheritdoc/>
    public void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<string>()
            .UseCollation(Collation);

        // The converter of the shared configuration is replaced, a property has only one.
        configurationBuilder
            .Properties<DateTime>()
            .HaveConversion<ClampedUtcDateTimeConverter>();

        configurationBuilder.Conventions.Add(_ => new IdentifierLengthConvention(MaxIdentifierLength));
        configurationBuilder.Conventions.Add(_ => new IndexPrefixLengthConvention());
        configurationBuilder.Conventions.Add(_ => new UnindexedTextConvention());
        configurationBuilder.Conventions.Add(_ => new IndexFilterConvention());
    }

    /// <inheritdoc/>
    public bool IsRetryableWriteConflict(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry or MySqlErrorCode.LockDeadlock or MySqlErrorCode.LockWaitTimeout })
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public Task RunScheduledOptimisation(CancellationToken cancellationToken)
    {
        return AnalyzeAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task RefreshStatistics(CancellationToken cancellationToken)
    {
        return AnalyzeAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RunShutdownTask(CancellationToken cancellationToken)
    {
        await MySqlConnection.ClearAllPoolsAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<string> MigrationBackupFast(CancellationToken cancellationToken)
    {
        if (await HasAppliedMigrationsAsync(cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning(
                "Migrations on MySQL and MariaDB are applied one statement at a time and cannot be rolled back as a whole. "
                + "Take a dump of the database with mysqldump or mariadb-dump before upgrading, it is the only way to return "
                + "to the state before a failed upgrade.");
        }

        // The server has nothing to snapshot, the empty key says that no snapshot was taken.
        return string.Empty;
    }

    /// <inheritdoc/>
    public async Task RestoreBackupFast(string key, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(key))
        {
            _logger.LogCritical("Tried to restore a backup that does not exist: {Key}", key);
            return;
        }

        var lastApplied = await GetLastAppliedMigrationAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogCritical(
            "There is no snapshot to restore on MySQL and MariaDB. The database was left at the last migration that applied "
            + "completely ({Migration}). Restore the dump taken before the upgrade to return to the previous version.",
            lastApplied ?? "none");
    }

    /// <inheritdoc/>
    public Task DeleteBackup(string key)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task PurgeDatabase(JellyfinDbContext dbContext, IEnumerable<string>? tableNames)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(tableNames);

        var tables = tableNames.ToList();
        if (tables.Count == 0)
        {
            return;
        }

        // Rows are deleted, as a table that other tables reference cannot be truncated. A delete leaves the counter of
        // the ids where it was, so the counters are reset afterwards.
        if (dbContext.Database.CurrentTransaction is not null)
        {
            // Changing a counter commits the transaction it runs in, so it is left alone while the caller holds one. The
            // caller then inserts rows with ids of its own, which move the counters past them.
            await DeleteRowsAsync(dbContext, tables).ConfigureAwait(false);
            return;
        }

        var transaction = await dbContext.Database.BeginTransactionAsync().ConfigureAwait(false);
        await using (transaction.ConfigureAwait(false))
        {
            await DeleteRowsAsync(dbContext, tables).ConfigureAwait(false);
            await transaction.CommitAsync().ConfigureAwait(false);
        }

        await ResetIdentityCountersAsync(dbContext, tables).ConfigureAwait(false);
    }

    private static T? GetOption<T>(ICollection<CustomDatabaseOption>? options, string key, Func<string, T> converter)
    {
        var value = options?.FirstOrDefault(e => e.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        return value is null ? default : converter(value.Value);
    }

    /// <summary>
    /// Reads the version a server reported when it was asked, for a server that is not named in the configuration.
    /// </summary>
    /// <param name="reported">The version string of the connection.</param>
    /// <returns>The server.</returns>
    internal static ServerInfo ReadDetected(string? reported)
    {
        return ServerInfo.TryParseReported(reported, out var server)
            ? server!
            : throw new InvalidOperationException(ServerInfo.UnreadableMessage(reported));
    }

    private static bool SpecifiesCommandTimeout(string connectionString)
    {
        var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
        return builder.ContainsKey("Default Command Timeout")
            || builder.ContainsKey("DefaultCommandTimeout")
            || builder.ContainsKey("Command Timeout")
            || builder.ContainsKey("CommandTimeout");
    }

    private static string QuoteIdentifier(string identifier)
    {
        return $"`{identifier.Replace("`", "``", StringComparison.Ordinal)}`";
    }

    private static Task<bool> HasLibraryItemsAsync(JellyfinDbContext context, CancellationToken cancellationToken)
    {
        // Folders and the seeded placeholder exist before any library has been scanned.
        return context.BaseItems.AnyAsync(e => !e.IsFolder && e.Type != "PLACEHOLDER", cancellationToken);
    }

    private void WarnOnceThatTheProviderIsExperimental(MySqlConnectionStringBuilder builder)
    {
        // The password is not part of the key, as the key is kept for the life of the process.
        var database = string.Create(CultureInfo.InvariantCulture, $"{builder.Server}:{builder.Port}/{builder.Database}");
        if (_databasesWarnedOf.TryAdd(database, true))
        {
            _logger.LogWarning("The MySQL and MariaDB database provider is experimental. Back up the data before using it with a library you cannot rebuild.");
        }
    }

    private void RequireUtf8Mb4(MySqlConnectionStringBuilder builder)
    {
        // The collation of every text column, utf8mb4_0900_bin, is not valid on a connection in another character set.
        if (!string.IsNullOrWhiteSpace(builder.CharacterSet)
            && !builder.CharacterSet.Equals(ConnectionCharacterSet, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "The connection string sets the character set {CharacterSet}. It is replaced by {Required}, which the collation of the tables needs",
                builder.CharacterSet,
                ConnectionCharacterSet);
        }

        builder.CharacterSet = ConnectionCharacterSet;
    }

    private ServerInfo ResolveServer(string? configured, string connectionString)
    {
        ServerInfo? server;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            if (!ServerInfo.TryParseConfigured(configured, out server))
            {
                throw new InvalidOperationException(
                    $"The server-version option '{configured}' is not valid. Use a version followed by the kind of server, for example 8.4-mysql or 11.4-mariadb.");
            }
        }
        else
        {
            try
            {
                using var connection = new MySqlConnection(connectionString);
                connection.Open();
                server = ReadDetected(connection.ServerVersion);
            }
            catch (MySqlException ex)
            {
                throw new InvalidOperationException(
                    "The database server could not be asked for its version. Check that it is running and that the connection string is correct, "
                    + "or name the server in the server-version option of database.xml, for example 8.4-mysql or 11.4-mariadb.",
                    ex);
            }
        }

        return server!.MeetsFloor ? server : throw new InvalidOperationException(server.UnsupportedMessage);
    }

    private async Task AnalyzeAsync(CancellationToken cancellationToken)
    {
        if (DbContextFactory is null)
        {
            return;
        }

        var context = await DbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            context.Database.SetCommandTimeout(_maintenanceCommandTimeout);

            if (!await HasLibraryItemsAsync(context, cancellationToken).ConfigureAwait(false))
            {
                _logger.LogInformation("Not analyzing the database, the library holds no items yet");
                return;
            }

            var tables = context.Model.GetEntityTypes()
                .Select(e => e.GetTableName())
                .OfType<string>()
                .Distinct(StringComparer.Ordinal)
                .Select(QuoteIdentifier);
            _logger.LogInformation("Analyzing the database");
            var analyze = "ANALYZE TABLE " + string.Join(", ", tables);
            await context.Database.ExecuteSqlRawAsync(analyze, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task DeleteRowsAsync(JellyfinDbContext dbContext, List<string> tables)
    {
        await dbContext.Database.ExecuteSqlRawAsync("SET SESSION foreign_key_checks = 0").ConfigureAwait(false);
        try
        {
            foreach (var table in tables)
            {
                var delete = $"DELETE FROM {QuoteIdentifier(table)}";
                await dbContext.Database.ExecuteSqlRawAsync(delete).ConfigureAwait(false);
            }
        }
        finally
        {
            await RestoreForeignKeyChecksAsync(dbContext).ConfigureAwait(false);
        }
    }

    private async Task RestoreForeignKeyChecksAsync(JellyfinDbContext dbContext)
    {
        try
        {
            await dbContext.Database.ExecuteSqlRawAsync("SET SESSION foreign_key_checks = 1").ConfigureAwait(false);
        }
        catch (DbException ex)
        {
            // The connection is unusable, and a pooled connection is reset to the defaults when it is returned.
            _logger.LogWarning(ex, "Could not turn the foreign key checks of the session back on");
        }
    }

    private async Task ResetIdentityCountersAsync(JellyfinDbContext dbContext, List<string> tables)
    {
        // The counter a table reports is cached by the server and can be stale after the rows were deleted, so the tables
        // are chosen by whether they have a counter, not by how far it has run.
        var withCounter = await dbContext.Database
            .SqlQueryRaw<string>("SELECT DISTINCT CAST(TABLE_NAME AS CHAR) AS `Value` FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND EXTRA LIKE '%auto_increment%'")
            .ToListAsync()
            .ConfigureAwait(false);
        foreach (var table in withCounter.Where(u => tables.Contains(u, StringComparer.OrdinalIgnoreCase)))
        {
            var alter = $"ALTER TABLE {QuoteIdentifier(table)} AUTO_INCREMENT = 1";
            await dbContext.Database.ExecuteSqlRawAsync(alter).ConfigureAwait(false);
        }
    }

    private async Task<bool> HasAppliedMigrationsAsync(CancellationToken cancellationToken)
    {
        if (DbContextFactory is null)
        {
            // Without a way to look, assume there is something to lose.
            return true;
        }

        var context = await DbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            // The history also holds the code routines, which a fresh install records before the schema exists.
            var schemaMigrations = context.GetService<IMigrationsAssembly>().Migrations.Keys;
            return (await context.Database.GetAppliedMigrationsAsync(cancellationToken).ConfigureAwait(false))
                .Any(id => schemaMigrations.Contains(id));
        }
    }

    private async Task<string?> GetLastAppliedMigrationAsync(CancellationToken cancellationToken)
    {
        if (DbContextFactory is null)
        {
            return null;
        }

        try
        {
            var context = await DbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using (context.ConfigureAwait(false))
            {
                return (await context.Database.GetAppliedMigrationsAsync(cancellationToken).ConfigureAwait(false)).LastOrDefault();
            }
        }
        catch (DbException ex)
        {
            // This runs because a migration just failed, so the database may well be unreachable.
            _logger.LogError(ex, "Could not read the applied migrations");
            return null;
        }
    }
}
