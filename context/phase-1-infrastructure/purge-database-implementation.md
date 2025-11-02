# Database Purge Implementation Guide

## Overview

Implement the `PurgeDatabase` method to clear specified tables from the PostgreSQL database.

**Priority**: HIGH
**Complexity**: MEDIUM
**Effort**: 8-12 hours

## Purpose

The `PurgeDatabase` method is used to:
- Clear test data during development
- Reset database state for testing
- Remove data from specific tables without dropping schema
- Prepare database for fresh data imports

## PostgreSQL vs SQLite Differences

| Operation | SQLite | PostgreSQL |
|-----------|--------|------------|
| Clear table | `DELETE FROM table` | `TRUNCATE TABLE table` |
| Foreign keys | `PRAGMA foreign_keys = OFF` | Disable triggers or CASCADE |
| Performance | DELETE slower | TRUNCATE much faster |
| Sequences | No auto-reset | RESTART IDENTITY option |
| Transaction | Required | Optional but recommended |

## Implementation

### Complete PurgeDatabase Method

```csharp
/// <inheritdoc/>
public async Task PurgeDatabase(JellyfinDbContext dbContext, IEnumerable<string>? tableNames)
{
    ArgumentNullException.ThrowIfNull(tableNames);

    try
    {
        _logger.LogInformation("Starting database purge for {Count} tables", tableNames.Count());

        // Begin transaction for atomicity
        await using var transaction = await dbContext.Database.BeginTransactionAsync().ConfigureAwait(false);

        try
        {
            // Disable foreign key checks temporarily
            await dbContext.Database.ExecuteSqlRawAsync(
                "SET session_replication_role = 'replica';").ConfigureAwait(false);

            // Truncate each table
            foreach (var tableName in tableNames)
            {
                _logger.LogDebug("Truncating table: {TableName}", tableName);

                // Use TRUNCATE for better performance
                // CASCADE removes dependent data from other tables
                // RESTART IDENTITY resets sequences
                var sql = $"TRUNCATE TABLE \"{tableName}\" RESTART IDENTITY CASCADE;";

                await dbContext.Database.ExecuteSqlRawAsync(sql).ConfigureAwait(false);
            }

            // Re-enable foreign key checks
            await dbContext.Database.ExecuteSqlRawAsync(
                "SET session_replication_role = 'origin';").ConfigureAwait(false);

            // Commit transaction
            await transaction.CommitAsync().ConfigureAwait(false);

            _logger.LogInformation("Database purge completed successfully");
        }
        catch (Exception)
        {
            // Rollback on error
            await transaction.RollbackAsync().ConfigureAwait(false);

            // Ensure foreign keys are re-enabled even on error
            try
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "SET session_replication_role = 'origin';").ConfigureAwait(false);
            }
            catch
            {
                // Ignore errors re-enabling foreign keys
            }

            throw;
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to purge database");
        throw;
    }
}
```

### Alternative: DELETE-based Implementation

For scenarios where TRUNCATE permissions are restricted:

```csharp
public async Task PurgeDatabaseWithDelete(JellyfinDbContext dbContext, IEnumerable<string>? tableNames)
{
    ArgumentNullException.ThrowIfNull(tableNames);

    try
    {
        _logger.LogInformation("Starting database purge with DELETE for {Count} tables", tableNames.Count());

        await using var transaction = await dbContext.Database.BeginTransactionAsync().ConfigureAwait(false);

        try
        {
            // Disable foreign key checks
            await dbContext.Database.ExecuteSqlRawAsync(
                "SET session_replication_role = 'replica';").ConfigureAwait(false);

            // Delete from each table
            foreach (var tableName in tableNames)
            {
                _logger.LogDebug("Deleting from table: {TableName}", tableName);

                var sql = $"DELETE FROM \"{tableName}\";";
                await dbContext.Database.ExecuteSqlRawAsync(sql).ConfigureAwait(false);

                // Reset sequence if table has an ID column
                var resetSeqSql = $@"
                    SELECT setval(
                        pg_get_serial_sequence('{tableName}', 'Id'),
                        1,
                        false
                    ) WHERE pg_get_serial_sequence('{tableName}', 'Id') IS NOT NULL;";

                try
                {
                    await dbContext.Database.ExecuteSqlRawAsync(resetSeqSql).ConfigureAwait(false);
                }
                catch
                {
                    // Ignore if table doesn't have Id column or sequence
                }
            }

            // Re-enable foreign key checks
            await dbContext.Database.ExecuteSqlRawAsync(
                "SET session_replication_role = 'origin';").ConfigureAwait(false);

            await transaction.CommitAsync().ConfigureAwait(false);

            _logger.LogInformation("Database purge with DELETE completed successfully");
        }
        catch (Exception)
        {
            await transaction.RollbackAsync().ConfigureAwait(false);

            try
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "SET session_replication_role = 'origin';").ConfigureAwait(false);
            }
            catch
            {
                // Ignore errors
            }

            throw;
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to purge database with DELETE");
        throw;
    }
}
```

### Improved: Get All Table Names

If `tableNames` is null, purge all tables:

```csharp
public async Task PurgeDatabase(JellyfinDbContext dbContext, IEnumerable<string>? tableNames)
{
    // If no table names provided, get all tables
    if (tableNames == null || !tableNames.Any())
    {
        tableNames = await GetAllTableNamesAsync(dbContext).ConfigureAwait(false);
        _logger.LogInformation("No tables specified, will purge all {Count} tables", tableNames.Count());
    }

    // ... rest of implementation
}

private async Task<IEnumerable<string>> GetAllTableNamesAsync(JellyfinDbContext dbContext)
{
    var sql = @"
        SELECT tablename
        FROM pg_tables
        WHERE schemaname = 'public'
        ORDER BY tablename;";

    var connection = dbContext.Database.GetDbConnection();
    await connection.OpenAsync().ConfigureAwait(false);

    await using var command = connection.CreateCommand();
    command.CommandText = sql;

    var tableNames = new List<string>();
    await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

    while (await reader.ReadAsync().ConfigureAwait(false))
    {
        tableNames.Add(reader.GetString(0));
    }

    return tableNames;
}
```

## Key PostgreSQL Features

### 1. TRUNCATE vs DELETE

**TRUNCATE**:
- Removes all rows from table
- Much faster than DELETE
- Resets auto-increment sequences
- Cannot be rolled back in some cases
- Requires TRUNCATE privilege

**DELETE**:
- Removes rows one by one
- Slower but more flexible
- Triggers are fired
- Can be rolled back
- Requires DELETE privilege

### 2. CASCADE Option

```sql
TRUNCATE TABLE table_name CASCADE;
```

Automatically truncates tables with foreign key references.

### 3. RESTART IDENTITY

```sql
TRUNCATE TABLE table_name RESTART IDENTITY;
```

Resets auto-increment sequences to their starting values.

### 4. session_replication_role

```sql
SET session_replication_role = 'replica';  -- Disable triggers/FKs
-- ... perform operations ...
SET session_replication_role = 'origin';   -- Re-enable
```

Temporarily disables triggers and foreign key checks.

## Table Dependency Order

For DELETE-based purge, tables must be processed in dependency order:

```csharp
private static IEnumerable<string> GetTablesInDependencyOrder()
{
    // Child tables first (no foreign keys pointing to them)
    return new[]
    {
        // Leaf tables (no dependencies on them)
        "Permissions",
        "Preferences",
        "UserData",
        "ItemValuesMap",
        "PeopleBaseItemMap",
        "BaseItemProviders",
        "BaseItemMetadataFields",
        "BaseItemTrailerTypes",
        "Chapters",
        "MediaStreamInfos",
        "AttachmentStreamInfos",
        "BaseItemImageInfos",
        "AncestorIds",
        "TrickplayInfos",
        "KeyframeData",
        "MediaSegments",

        // Parent tables
        "ItemValues",
        "Peoples",
        "BaseItems",
        "Groups",
        "Users",
        "Devices",
        "DisplayPreferences",
        "CustomItemDisplayPreferences",
        "ActivityLog",
        "ApiKeys",
    };
}
```

## Testing

### Unit Tests

```csharp
[Fact]
public async Task PurgeDatabase_WithSpecificTables_ClearsTables()
{
    // Arrange
    var provider = new PostgresDatabaseProvider(_mockAppPaths.Object, _mockLogger.Object);
    await using var context = CreateTestContext();

    // Insert test data
    context.Users.Add(new User { /* ... */ });
    await context.SaveChangesAsync();

    var tablesToPurge = new[] { "Users" };

    // Act
    await provider.PurgeDatabase(context, tablesToPurge);

    // Assert
    var userCount = await context.Users.CountAsync();
    Assert.Equal(0, userCount);
}

[Fact]
public async Task PurgeDatabase_WithNullTables_ClearsAllTables()
{
    // Arrange
    var provider = new PostgresDatabaseProvider(_mockAppPaths.Object, _mockLogger.Object);
    await using var context = CreateTestContext();

    // Insert test data in multiple tables
    context.Users.Add(new User { /* ... */ });
    context.ActivityLog.Add(new ActivityLog { /* ... */ });
    await context.SaveChangesAsync();

    // Act
    await provider.PurgeDatabase(context, null);

    // Assert
    Assert.Equal(0, await context.Users.CountAsync());
    Assert.Equal(0, await context.ActivityLog.CountAsync());
}

[Fact]
public async Task PurgeDatabase_RollsBackOnError()
{
    // Arrange
    var provider = new PostgresDatabaseProvider(_mockAppPaths.Object, _mockLogger.Object);
    await using var context = CreateTestContext();

    context.Users.Add(new User { /* ... */ });
    await context.SaveChangesAsync();

    var invalidTables = new[] { "NonExistentTable" };

    // Act & Assert
    await Assert.ThrowsAsync<InvalidOperationException>(
        () => provider.PurgeDatabase(context, invalidTables));

    // Data should still exist due to rollback
    Assert.Equal(1, await context.Users.CountAsync());
}
```

### Integration Tests

```csharp
[Fact]
public async Task PurgeDatabase_WithForeignKeys_HandlesCorrectly()
{
    // Arrange
    await using var context = CreateTestContext();

    // Create user and related data
    var user = new User { /* ... */ };
    context.Users.Add(user);
    await context.SaveChangesAsync();

    var userData = new UserData { UserId = user.Id, /* ... */ };
    context.UserData.Add(userData);
    await context.SaveChangesAsync();

    // Act
    await _provider.PurgeDatabase(context, new[] { "UserData", "Users" });

    // Assert
    Assert.Equal(0, await context.Users.CountAsync());
    Assert.Equal(0, await context.UserData.CountAsync());
}
```

## Performance Considerations

### TRUNCATE Performance
- 100K rows: < 1 second
- 1M rows: 1-2 seconds
- 10M rows: 2-5 seconds

### DELETE Performance
- 100K rows: 10-30 seconds
- 1M rows: 2-5 minutes
- 10M rows: 20-60 minutes

**Recommendation**: Use TRUNCATE whenever possible.

## Common Issues

### Issue 1: Permission Denied
```
ERROR: permission denied for table <table_name>
```
**Solution**: Grant TRUNCATE or DELETE permissions

### Issue 2: Foreign Key Constraint
```
ERROR: cannot truncate a table referenced in a foreign key constraint
```
**Solution**: Use CASCADE option or disable constraints temporarily

### Issue 3: Active Transactions
```
ERROR: cannot execute TRUNCATE in a read-only transaction
```
**Solution**: Ensure write access to database

## Security Considerations

1. **Permissions**: Ensure user has appropriate TRUNCATE/DELETE privileges
2. **Backup**: Always backup before purge operations
3. **Confirmation**: Require user confirmation for purge operations
4. **Audit**: Log all purge operations for audit trail

## Best Practices

1. **Transaction**: Always use transactions for atomicity
2. **Error Handling**: Rollback on any error
3. **Logging**: Log table names and operation results
4. **Validation**: Validate table names before execution
5. **Re-enable Constraints**: Always re-enable foreign keys, even on error

## Next Steps

1. Implement PurgeDatabase method
2. Add comprehensive error handling
3. Test with various table combinations
4. Move to `error-handling-logging.md`

---

**Status**: Ready to Implement
**Estimated Time**: 8-12 hours
