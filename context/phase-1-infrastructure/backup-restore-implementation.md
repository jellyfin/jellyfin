# Backup/Restore Implementation Guide

## Overview

Implement PostgreSQL database backup and restore functionality using pg_dump and pg_restore command-line tools.

**Priority**: CRITICAL
**Complexity**: HIGH
**Effort**: 20-30 hours

## Challenge

Unlike SQLite where you can simply copy a file, PostgreSQL requires executing external processes (pg_dump/pg_restore) to perform backups and restores. This is significantly more complex.

## Architecture

### Backup Flow
```
User Request → MigrationBackupFast()
    ↓
Find pg_dump executable
    ↓
Build pg_dump command arguments
    ↓
Execute pg_dump process
    ↓
Monitor output/errors
    ↓
Verify backup file created
    ↓
Return backup key
```

### Restore Flow
```
User Request → RestoreBackupFast(key)
    ↓
Verify backup file exists
    ↓
Close all database connections
    ↓
Find pg_restore executable
    ↓
Build pg_restore command arguments
    ↓
Execute pg_restore process
    ↓
Monitor output/errors
    ↓
Verify restore completed
    ↓
Reconnect database
```

## Implementation

### 1. MigrationBackupFast Implementation

```csharp
/// <inheritdoc />
public async Task<string> MigrationBackupFast(CancellationToken cancellationToken)
{
    try
    {
        // Generate unique backup key with timestamp
        var key = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

        // Ensure backup directory exists
        var backupDir = Path.Combine(_applicationPaths.DataPath, BackupFolderName);
        Directory.CreateDirectory(backupDir);

        // Build backup file path
        var backupFile = Path.Combine(backupDir, $"{key}_jellyfin.backup");

        _logger.LogInformation("Starting PostgreSQL backup to {BackupFile}", backupFile);

        // Get connection string details
        var connectionString = GetConnectionString();
        var connBuilder = new NpgsqlConnectionStringBuilder(connectionString);

        // Find pg_dump executable
        var pgDumpPath = FindPgDumpExecutable();
        if (string.IsNullOrEmpty(pgDumpPath))
        {
            throw new FileNotFoundException(
                "pg_dump executable not found. Ensure PostgreSQL client tools are installed and in PATH.");
        }

        _logger.LogDebug("Using pg_dump at: {PgDumpPath}", pgDumpPath);

        // Build pg_dump arguments
        var arguments = BuildPgDumpArguments(connBuilder, backupFile);

        // Execute pg_dump
        var exitCode = await ExecuteProcessAsync(
            pgDumpPath,
            arguments,
            connBuilder.Password,
            cancellationToken);

        if (exitCode != 0)
        {
            throw new InvalidOperationException(
                $"pg_dump failed with exit code {exitCode}. Check logs for details.");
        }

        // Verify backup file was created
        if (!File.Exists(backupFile))
        {
            throw new InvalidOperationException(
                "Backup file was not created despite pg_dump reporting success.");
        }

        var fileInfo = new FileInfo(backupFile);
        _logger.LogInformation(
            "PostgreSQL backup completed successfully. File size: {Size:N0} bytes",
            fileInfo.Length);

        return key;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to create PostgreSQL backup");
        throw;
    }
}

private string GetConnectionString()
{
    // Get connection string from current configuration
    // This needs to be stored during Initialise()
    if (_connectionString == null)
    {
        throw new InvalidOperationException(
            "Connection string not available. Ensure Initialise() was called.");
    }
    return _connectionString;
}

private static string FindPgDumpExecutable()
{
    // Try common locations
    var candidates = new[]
    {
        "pg_dump", // In PATH
        "/usr/bin/pg_dump", // Linux
        "/usr/local/bin/pg_dump", // Linux/macOS
        "/opt/homebrew/bin/pg_dump", // macOS Homebrew
        @"C:\Program Files\PostgreSQL\16\bin\pg_dump.exe", // Windows
        @"C:\Program Files\PostgreSQL\15\bin\pg_dump.exe",
        @"C:\Program Files\PostgreSQL\14\bin\pg_dump.exe",
    };

    foreach (var candidate in candidates)
    {
        if (File.Exists(candidate))
        {
            return candidate;
        }

        // Try to find in PATH
        if (candidate == "pg_dump")
        {
            var path = FindInPath("pg_dump");
            if (path != null)
            {
                return path;
            }
        }
    }

    return null;
}

private static string? FindInPath(string fileName)
{
    var paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator);
    if (paths == null) return null;

    foreach (var path in paths)
    {
        var fullPath = Path.Combine(path, fileName);
        if (File.Exists(fullPath))
        {
            return fullPath;
        }

        // Try with .exe extension on Windows
        if (OperatingSystem.IsWindows())
        {
            fullPath += ".exe";
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }
    }

    return null;
}

private static string BuildPgDumpArguments(
    NpgsqlConnectionStringBuilder connBuilder,
    string outputFile)
{
    var args = new List<string>
    {
        $"--host={connBuilder.Host}",
        $"--port={connBuilder.Port}",
        $"--username={connBuilder.Username}",
        $"--dbname={connBuilder.Database}",
        $"--file={outputFile}",
        "--format=custom", // Custom format for pg_restore
        "--compress=9", // Maximum compression
        "--verbose", // Detailed output
        "--no-password" // Use PGPASSWORD environment variable
    };

    return string.Join(" ", args.Select(QuoteArgument));
}

private static string QuoteArgument(string arg)
{
    if (arg.Contains(' ') || arg.Contains('"'))
    {
        return $"\"{arg.Replace("\"", "\\\"")}\"";
    }
    return arg;
}

private async Task<int> ExecuteProcessAsync(
    string fileName,
    string arguments,
    string password,
    CancellationToken cancellationToken)
{
    var startInfo = new ProcessStartInfo
    {
        FileName = fileName,
        Arguments = arguments,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    };

    // Set password via environment variable (more secure than command line)
    if (!string.IsNullOrEmpty(password))
    {
        startInfo.Environment["PGPASSWORD"] = password;
    }

    using var process = new Process { StartInfo = startInfo };

    var outputBuilder = new StringBuilder();
    var errorBuilder = new StringBuilder();

    process.OutputDataReceived += (sender, args) =>
    {
        if (!string.IsNullOrEmpty(args.Data))
        {
            outputBuilder.AppendLine(args.Data);
            _logger.LogDebug("pg_dump output: {Output}", args.Data);
        }
    };

    process.ErrorDataReceived += (sender, args) =>
    {
        if (!string.IsNullOrEmpty(args.Data))
        {
            errorBuilder.AppendLine(args.Data);
            _logger.LogWarning("pg_dump error: {Error}", args.Data);
        }
    };

    process.Start();
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();

    await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

    if (process.ExitCode != 0)
    {
        _logger.LogError(
            "pg_dump failed with exit code {ExitCode}. Output: {Output}. Errors: {Errors}",
            process.ExitCode,
            outputBuilder.ToString(),
            errorBuilder.ToString());
    }

    return process.ExitCode;
}
```

### 2. RestoreBackupFast Implementation

```csharp
/// <inheritdoc />
public async Task RestoreBackupFast(string key, CancellationToken cancellationToken)
{
    try
    {
        var backupFile = Path.Combine(_applicationPaths.DataPath, BackupFolderName, $"{key}_jellyfin.backup");

        if (!File.Exists(backupFile))
        {
            throw new FileNotFoundException($"Backup file not found: {backupFile}");
        }

        _logger.LogInformation("Starting PostgreSQL restore from {BackupFile}", backupFile);

        // Get connection string details
        var connectionString = GetConnectionString();
        var connBuilder = new NpgsqlConnectionStringBuilder(connectionString);

        // Close all active connections
        await CloseAllConnectionsAsync(connBuilder, cancellationToken).ConfigureAwait(false);

        // Drop and recreate database
        await DropAndRecreateDatabase(connBuilder, cancellationToken).ConfigureAwait(false);

        // Find pg_restore executable
        var pgRestorePath = FindPgRestoreExecutable();
        if (string.IsNullOrEmpty(pgRestorePath))
        {
            throw new FileNotFoundException(
                "pg_restore executable not found. Ensure PostgreSQL client tools are installed and in PATH.");
        }

        _logger.LogDebug("Using pg_restore at: {PgRestorePath}", pgRestorePath);

        // Build pg_restore arguments
        var arguments = BuildPgRestoreArguments(connBuilder, backupFile);

        // Execute pg_restore
        var exitCode = await ExecuteProcessAsync(
            pgRestorePath,
            arguments,
            connBuilder.Password,
            cancellationToken);

        if (exitCode != 0)
        {
            throw new InvalidOperationException(
                $"pg_restore failed with exit code {exitCode}. Check logs for details.");
        }

        _logger.LogInformation("PostgreSQL restore completed successfully");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to restore PostgreSQL backup");
        throw;
    }
}

private static string FindPgRestoreExecutable()
{
    // Similar to FindPgDumpExecutable, but for pg_restore
    var candidates = new[]
    {
        "pg_restore",
        "/usr/bin/pg_restore",
        "/usr/local/bin/pg_restore",
        "/opt/homebrew/bin/pg_restore",
        @"C:\Program Files\PostgreSQL\16\bin\pg_restore.exe",
        @"C:\Program Files\PostgreSQL\15\bin\pg_restore.exe",
        @"C:\Program Files\PostgreSQL\14\bin\pg_restore.exe",
    };

    foreach (var candidate in candidates)
    {
        if (File.Exists(candidate))
        {
            return candidate;
        }

        if (candidate == "pg_restore")
        {
            var path = FindInPath("pg_restore");
            if (path != null)
            {
                return path;
            }
        }
    }

    return null;
}

private static string BuildPgRestoreArguments(
    NpgsqlConnectionStringBuilder connBuilder,
    string inputFile)
{
    var args = new List<string>
    {
        $"--host={connBuilder.Host}",
        $"--port={connBuilder.Port}",
        $"--username={connBuilder.Username}",
        $"--dbname={connBuilder.Database}",
        $"--file={inputFile}",
        "--verbose",
        "--no-password",
        "--clean", // Clean (drop) database objects before recreating
        "--if-exists", // Don't error if objects don't exist
        "--no-owner", // Don't restore ownership
        "--no-privileges" // Don't restore privileges
    };

    return string.Join(" ", args.Select(QuoteArgument));
}

private async Task CloseAllConnectionsAsync(
    NpgsqlConnectionStringBuilder connBuilder,
    CancellationToken cancellationToken)
{
    // Connect to postgres database to close connections to target database
    var adminConnBuilder = new NpgsqlConnectionStringBuilder(connBuilder.ToString())
    {
        Database = "postgres"
    };

    await using var conn = new NpgsqlConnection(adminConnBuilder.ToString());
    await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

    // Terminate all connections to the target database
    var sql = $@"
        SELECT pg_terminate_backend(pg_stat_activity.pid)
        FROM pg_stat_activity
        WHERE pg_stat_activity.datname = '{connBuilder.Database}'
          AND pid <> pg_backend_pid();";

    await using var cmd = new NpgsqlCommand(sql, conn);
    await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

    _logger.LogInformation("Closed all connections to database {Database}", connBuilder.Database);
}

private async Task DropAndRecreateDatabase(
    NpgsqlConnectionStringBuilder connBuilder,
    CancellationToken cancellationToken)
{
    // Connect to postgres database
    var adminConnBuilder = new NpgsqlConnectionStringBuilder(connBuilder.ToString())
    {
        Database = "postgres"
    };

    await using var conn = new NpgsqlConnection(adminConnBuilder.ToString());
    await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

    // Drop database
    var dropSql = $"DROP DATABASE IF EXISTS \"{connBuilder.Database}\";";
    await using (var cmd = new NpgsqlCommand(dropSql, conn))
    {
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    _logger.LogInformation("Dropped database {Database}", connBuilder.Database);

    // Create database
    var createSql = $"CREATE DATABASE \"{connBuilder.Database}\" WITH ENCODING 'UTF8';";
    await using (var cmd = new NpgsqlCommand(createSql, conn))
    {
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    _logger.LogInformation("Created database {Database}", connBuilder.Database);
}
```

### 3. DeleteBackup Implementation

```csharp
/// <inheritdoc />
public Task DeleteBackup(string key)
{
    try
    {
        var backupFile = Path.Combine(_applicationPaths.DataPath, BackupFolderName, $"{key}_jellyfin.backup");

        if (!File.Exists(backupFile))
        {
            _logger.LogWarning("Backup file not found, cannot delete: {BackupFile}", backupFile);
            return Task.CompletedTask;
        }

        File.Delete(backupFile);
        _logger.LogInformation("Deleted backup file: {BackupFile}", backupFile);

        return Task.CompletedTask;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to delete backup with key {Key}", key);
        throw;
    }
}
```

### 4. Store Connection String

Add field to store connection string:

```csharp
public sealed class PostgresDatabaseProvider : IJellyfinDatabaseProvider
{
    private const string BackupFolderName = "PostgreSQLBackups";
    private readonly IApplicationPaths _applicationPaths;
    private readonly ILogger<PostgresDatabaseProvider> _logger;
    private string? _connectionString; // Add this field

    // ... existing code ...

    public void Initialise(DbContextOptionsBuilder options, DatabaseConfigurationOptions databaseConfiguration)
    {
        // Build connection string
        var connectionString = BuildConnectionString(databaseConfiguration);
        _connectionString = connectionString; // Store it

        // ... rest of Initialise code ...
    }
}
```

## Testing

### Unit Tests

```csharp
[Fact]
public async Task MigrationBackupFast_CreatesBackupFile()
{
    // Arrange
    var provider = new PostgresDatabaseProvider(_mockAppPaths.Object, _mockLogger.Object);
    // Initialize with test connection

    // Act
    var key = await provider.MigrationBackupFast(CancellationToken.None);

    // Assert
    Assert.NotNull(key);
    Assert.Matches(@"\d{14}", key); // yyyyMMddHHmmss format

    var backupFile = Path.Combine(_mockAppPaths.Object.DataPath, "PostgreSQLBackups", $"{key}_jellyfin.backup");
    Assert.True(File.Exists(backupFile));
}

[Fact]
public async Task RestoreBackupFast_RestoresFromBackup()
{
    // Arrange
    var provider = new PostgresDatabaseProvider(_mockAppPaths.Object, _mockLogger.Object);
    var key = await provider.MigrationBackupFast(CancellationToken.None);

    // Act
    await provider.RestoreBackupFast(key, CancellationToken.None);

    // Assert - database should be restored
    // Verify by checking data exists
}

[Fact]
public async Task DeleteBackup_RemovesBackupFile()
{
    // Arrange
    var provider = new PostgresDatabaseProvider(_mockAppPaths.Object, _mockLogger.Object);
    var key = await provider.MigrationBackupFast(CancellationToken.None);

    // Act
    await provider.DeleteBackup(key);

    // Assert
    var backupFile = Path.Combine(_mockAppPaths.Object.DataPath, "PostgreSQLBackups", $"{key}_jellyfin.backup");
    Assert.False(File.Exists(backupFile));
}
```

## Platform-Specific Considerations

### Windows
- pg_dump.exe and pg_restore.exe in `C:\Program Files\PostgreSQL\<version>\bin\`
- Requires proper PATH configuration
- May need administrator privileges

### Linux
- Usually in `/usr/bin/` or `/usr/local/bin/`
- Installed via package manager (apt, yum, etc.)
- Check with: `which pg_dump`

### macOS
- Homebrew: `/opt/homebrew/bin/` (Apple Silicon) or `/usr/local/bin/` (Intel)
- Postgres.app: `/Applications/Postgres.app/Contents/Versions/latest/bin/`
- Check with: `which pg_dump`

## Performance Considerations

### Backup Size
- Custom format with compression reduces size by ~70%
- 100K items: ~50-100 MB backup file
- 1M items: ~500 MB - 1 GB backup file

### Backup Time
- Depends on database size and CPU
- 100K items: 2-5 minutes
- 1M items: 10-20 minutes

### Restore Time
- Typically 1.5-2x backup time
- Includes database recreation overhead

## Error Handling

Common errors and solutions:

| Error | Cause | Solution |
|-------|-------|----------|
| pg_dump not found | Not in PATH | Install PostgreSQL client tools |
| Permission denied | Insufficient privileges | Grant backup privileges |
| Connection refused | Database not running | Start PostgreSQL service |
| Disk full | Insufficient space | Free up disk space |
| Authentication failed | Wrong credentials | Verify username/password |

## Security Considerations

1. **Password Handling**
   - Use PGPASSWORD environment variable
   - Never log passwords
   - Clear environment after use

2. **Backup File Security**
   - Store in secure location
   - Set appropriate file permissions
   - Consider encryption for sensitive data

3. **Privilege Requirements**
   - User needs SELECT on all tables for backup
   - User needs CREATE DATABASE for restore
   - Consider separate backup user

## Next Steps

1. Implement all methods as shown
2. Test with real PostgreSQL database
3. Handle edge cases (large databases, network issues)
4. Move to `purge-database-implementation.md`

---

**Status**: Ready to Implement
**Estimated Time**: 20-30 hours
