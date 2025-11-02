# Connection String Builder Implementation

## Overview

Fix the `NpgsqlConnectionStringBuilder` compilation issue and implement robust connection string handling for PostgreSQL.

**Priority**: HIGH
**Complexity**: LOW
**Effort**: 1-2 hours

## Current Issue

The `PostgresDatabaseProvider.cs` file references `NpgsqlConnectionStringBuilder` but doesn't have the proper using statement, causing compilation errors.

## Required Changes

### 1. Add Using Statement

**File**: `src/Jellyfin.Database/Jellyfin.Database.Providers.Postgres/PostgresDatabaseProvider.cs`

Add at the top of the file:
```csharp
using Npgsql;
```

### 2. Fix BuildConnectionString Method

**Current Code** (has issues):
```csharp
private string BuildConnectionString(DatabaseConfigurationOptions databaseConfiguration)
{
    // ... existing code ...

    var builder = new NpgsqlConnectionStringBuilder  // Missing import
    {
        Host = // ...
        SslMode = GetOption<bool?>(/* ... */)  // Wrong type - should be SslMode enum
    };
}
```

**Fixed Code**:
```csharp
private string BuildConnectionString(DatabaseConfigurationOptions databaseConfiguration)
{
    var connectionString = databaseConfiguration.CustomProviderOptions?.Options?
        .FirstOrDefault(o => o.Key.Equals("connectionstring", StringComparison.OrdinalIgnoreCase))?.Value;

    if (!string.IsNullOrEmpty(connectionString))
    {
        _logger.LogInformation("Using provided connection string");
        return connectionString;
    }

    // Build connection string from individual parameters
    var builder = new NpgsqlConnectionStringBuilder
    {
        Host = GetOption(databaseConfiguration.CustomProviderOptions?.Options,
            "host", s => s, () => "localhost"),
        Port = GetOption(databaseConfiguration.CustomProviderOptions?.Options,
            "port", int.Parse, () => 5432),
        Database = GetOption(databaseConfiguration.CustomProviderOptions?.Options,
            "database", s => s, () => "jellyfin"),
        Username = GetOption(databaseConfiguration.CustomProviderOptions?.Options,
            "username", s => s, () => "jellyfin"),
        Password = GetOption(databaseConfiguration.CustomProviderOptions?.Options,
            "password", s => s, () => string.Empty),

        // SSL/TLS Configuration
        SslMode = ParseSslMode(GetOption(databaseConfiguration.CustomProviderOptions?.Options,
            "sslmode", s => s, () => "Disable")),
        TrustServerCertificate = GetOption(databaseConfiguration.CustomProviderOptions?.Options,
            "trustservercertificate", bool.Parse, () => false),

        // Connection Pool Settings
        Pooling = GetOption(databaseConfiguration.CustomProviderOptions?.Options,
            "pooling", bool.Parse, () => true),
        MinPoolSize = GetOption(databaseConfiguration.CustomProviderOptions?.Options,
            "minpoolsize", int.Parse, () => 0),
        MaxPoolSize = GetOption(databaseConfiguration.CustomProviderOptions?.Options,
            "maxpoolsize", int.Parse, () => 100),
        ConnectionIdleLifetime = GetOption(databaseConfiguration.CustomProviderOptions?.Options,
            "connectionidlelifetime", int.Parse, () => 300),

        // Timeout Settings
        Timeout = GetOption(databaseConfiguration.CustomProviderOptions?.Options,
            "timeout", int.Parse, () => 30),
        CommandTimeout = GetOption(databaseConfiguration.CustomProviderOptions?.Options,
            "commandtimeout", int.Parse, () => 30),

        // Application Name for monitoring
        ApplicationName = GetOption(databaseConfiguration.CustomProviderOptions?.Options,
            "applicationname", s => s, () => "Jellyfin")
    };

    var result = builder.ToString();

    // Log connection string without password
    var safeBuilder = new NpgsqlConnectionStringBuilder(result)
    {
        Password = "***REDACTED***"
    };
    _logger.LogInformation("Built PostgreSQL connection string: {ConnectionString}", safeBuilder.ToString());

    return result;
}

private static SslMode ParseSslMode(string mode)
{
    return mode?.ToLowerInvariant() switch
    {
        "disable" => SslMode.Disable,
        "allow" => SslMode.Allow,
        "prefer" => SslMode.Prefer,
        "require" => SslMode.Require,
        "verifyca" => SslMode.VerifyCA,
        "verifyfull" => SslMode.VerifyFull,
        _ => SslMode.Disable
    };
}
```

## Configuration Options

### Supported Connection Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `connectionstring` | string | - | Full connection string (overrides all others) |
| `host` | string | localhost | PostgreSQL server hostname |
| `port` | int | 5432 | PostgreSQL server port |
| `database` | string | jellyfin | Database name |
| `username` | string | jellyfin | Database username |
| `password` | string | (empty) | Database password |
| `sslmode` | string | Disable | SSL mode (Disable/Allow/Prefer/Require/VerifyCA/VerifyFull) |
| `trustservercertificate` | bool | false | Trust server certificate without validation |
| `pooling` | bool | true | Enable connection pooling |
| `minpoolsize` | int | 0 | Minimum pool size |
| `maxpoolsize` | int | 100 | Maximum pool size |
| `connectionidlelifetime` | int | 300 | Idle connection lifetime (seconds) |
| `timeout` | int | 30 | Connection timeout (seconds) |
| `commandtimeout` | int | 30 | Command timeout (seconds) |
| `applicationname` | string | Jellyfin | Application name for monitoring |

### SSL/TLS Modes Explained

- **Disable**: No SSL (default for local development)
- **Allow**: Use SSL if server requires it
- **Prefer**: Try SSL first, fallback to non-SSL
- **Require**: Require SSL (fail if not available)
- **VerifyCA**: Require SSL and verify certificate authority
- **VerifyFull**: Require SSL and verify full certificate chain

## Configuration Examples

### Example 1: Local Development (Minimal)
```json
{
  "Database": {
    "Provider": "Jellyfin-PostgreSQL",
    "CustomProviderOptions": {
      "Options": [
        { "Key": "host", "Value": "localhost" },
        { "Key": "database", "Value": "jellyfin_dev" },
        { "Key": "username", "Value": "jellyfin" },
        { "Key": "password", "Value": "dev_password" }
      ]
    }
  }
}
```

### Example 2: Production with SSL
```json
{
  "Database": {
    "Provider": "Jellyfin-PostgreSQL",
    "CustomProviderOptions": {
      "Options": [
        { "Key": "host", "Value": "db.example.com" },
        { "Key": "port", "Value": "5432" },
        { "Key": "database", "Value": "jellyfin_prod" },
        { "Key": "username", "Value": "jellyfin_app" },
        { "Key": "password", "Value": "secure_password_here" },
        { "Key": "sslmode", "Value": "Require" },
        { "Key": "maxpoolsize", "Value": "50" }
      ]
    }
  }
}
```

### Example 3: Full Connection String
```json
{
  "Database": {
    "Provider": "Jellyfin-PostgreSQL",
    "CustomProviderOptions": {
      "Options": [
        {
          "Key": "connectionstring",
          "Value": "Host=db.example.com;Port=5432;Database=jellyfin;Username=jellyfin;Password=password;SSL Mode=Require;Pooling=true;Maximum Pool Size=50"
        }
      ]
    }
  }
}
```

### Example 4: Docker Compose Environment
```json
{
  "Database": {
    "Provider": "Jellyfin-PostgreSQL",
    "CustomProviderOptions": {
      "Options": [
        { "Key": "host", "Value": "postgres" },
        { "Key": "database", "Value": "jellyfin" },
        { "Key": "username", "Value": "jellyfin" },
        { "Key": "password", "Value": "${POSTGRES_PASSWORD}" }
      ]
    }
  }
}
```

## Testing

### Unit Tests

Create `PostgresDatabaseProviderTests.cs`:

```csharp
using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using Jellyfin.Database.Providers.Postgres;
using Jellyfin.Database.Implementations.DbConfiguration;
using MediaBrowser.Common.Configuration;

namespace Jellyfin.Database.Tests;

public class PostgresDatabaseProviderTests
{
    private readonly Mock<IApplicationPaths> _mockAppPaths;
    private readonly Mock<ILogger<PostgresDatabaseProvider>> _mockLogger;

    public PostgresDatabaseProviderTests()
    {
        _mockAppPaths = new Mock<IApplicationPaths>();
        _mockAppPaths.Setup(x => x.DataPath).Returns("/data");
        _mockLogger = new Mock<ILogger<PostgresDatabaseProvider>>();
    }

    [Fact]
    public void BuildConnectionString_WithMinimalConfig_ReturnsDefaultConnectionString()
    {
        // Arrange
        var provider = new PostgresDatabaseProvider(_mockAppPaths.Object, _mockLogger.Object);
        var config = new DatabaseConfigurationOptions
        {
            CustomProviderOptions = new CustomDatabaseOptions
            {
                Options = new List<CustomDatabaseOption>()
            }
        };

        // Act
        var result = InvokePrivateMethod(provider, "BuildConnectionString", config) as string;

        // Assert
        Assert.Contains("Host=localhost", result);
        Assert.Contains("Port=5432", result);
        Assert.Contains("Database=jellyfin", result);
    }

    [Fact]
    public void BuildConnectionString_WithCustomHost_UsesCustomHost()
    {
        // Arrange
        var provider = new PostgresDatabaseProvider(_mockAppPaths.Object, _mockLogger.Object);
        var config = new DatabaseConfigurationOptions
        {
            CustomProviderOptions = new CustomDatabaseOptions
            {
                Options = new List<CustomDatabaseOption>
                {
                    new CustomDatabaseOption { Key = "host", Value = "custom-host" }
                }
            }
        };

        // Act
        var result = InvokePrivateMethod(provider, "BuildConnectionString", config) as string;

        // Assert
        Assert.Contains("Host=custom-host", result);
    }

    [Fact]
    public void BuildConnectionString_WithSSLRequire_IncludesSSLMode()
    {
        // Arrange
        var provider = new PostgresDatabaseProvider(_mockAppPaths.Object, _mockLogger.Object);
        var config = new DatabaseConfigurationOptions
        {
            CustomProviderOptions = new CustomDatabaseOptions
            {
                Options = new List<CustomDatabaseOption>
                {
                    new CustomDatabaseOption { Key = "sslmode", Value = "Require" }
                }
            }
        };

        // Act
        var result = InvokePrivateMethod(provider, "BuildConnectionString", config) as string;

        // Assert
        Assert.Contains("SSL Mode=Require", result);
    }

    [Fact]
    public void BuildConnectionString_WithFullConnectionString_UsesProvidedString()
    {
        // Arrange
        var provider = new PostgresDatabaseProvider(_mockAppPaths.Object, _mockLogger.Object);
        var fullConnectionString = "Host=myhost;Database=mydb;Username=myuser;Password=mypass";
        var config = new DatabaseConfigurationOptions
        {
            CustomProviderOptions = new CustomDatabaseOptions
            {
                Options = new List<CustomDatabaseOption>
                {
                    new CustomDatabaseOption { Key = "connectionstring", Value = fullConnectionString }
                }
            }
        };

        // Act
        var result = InvokePrivateMethod(provider, "BuildConnectionString", config) as string;

        // Assert
        Assert.Equal(fullConnectionString, result);
    }

    private static object? InvokePrivateMethod(object obj, string methodName, params object[] parameters)
    {
        var method = obj.GetType().GetMethod(methodName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return method?.Invoke(obj, parameters);
    }
}
```

### Integration Tests

```csharp
[Fact]
public async Task ConnectionString_WithRealPostgreSQL_Connects()
{
    // Arrange
    var provider = new PostgresDatabaseProvider(_mockAppPaths.Object, _mockLogger.Object);
    var config = new DatabaseConfigurationOptions
    {
        CustomProviderOptions = new CustomDatabaseOptions
        {
            Options = new List<CustomDatabaseOption>
            {
                new CustomDatabaseOption { Key = "host", Value = "localhost" },
                new CustomDatabaseOption { Key = "username", Value = "postgres" },
                new CustomDatabaseOption { Key = "password", Value = "postgres" },
                new CustomDatabaseOption { Key = "database", Value = "postgres" }
            }
        }
    };

    var optionsBuilder = new DbContextOptionsBuilder<JellyfinDbContext>();

    // Act
    provider.Initialise(optionsBuilder, config);

    // Assert - Should not throw
    await using var context = new JellyfinDbContext(optionsBuilder.Options);
    var canConnect = await context.Database.CanConnectAsync();
    Assert.True(canConnect);
}
```

## Validation Checklist

- [ ] Code compiles without errors
- [ ] All unit tests pass
- [ ] Integration test with real PostgreSQL succeeds
- [ ] Connection string is properly redacted in logs
- [ ] All SSL modes work correctly
- [ ] Connection pooling parameters are respected
- [ ] Invalid configurations throw appropriate exceptions
- [ ] Documentation is complete

## Common Issues & Solutions

### Issue 1: "Unknown SSL mode"
**Cause**: Invalid sslmode value
**Solution**: Use one of: Disable, Allow, Prefer, Require, VerifyCA, VerifyFull

### Issue 2: "Password must be provided"
**Cause**: Empty password with some authentication methods
**Solution**: Provide password or configure trust authentication in pg_hba.conf

### Issue 3: Connection timeout
**Cause**: PostgreSQL not running or firewall blocking
**Solution**: Verify PostgreSQL is running and port 5432 is accessible

### Issue 4: SSL certificate validation fails
**Cause**: Self-signed certificate with VerifyFull mode
**Solution**: Use TrustServerCertificate=true or provide proper certificate

## Next Steps

After completing this implementation:
1. Test with various connection configurations
2. Move to `backup-restore-implementation.md`
3. Update checklist in `checklist.md`

---

**Status**: Ready to Implement
**Dependencies**: None
**Estimated Time**: 1-2 hours
