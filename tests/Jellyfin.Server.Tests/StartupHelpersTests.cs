using System;
using System.IO;
using Emby.Server.Implementations;
using Jellyfin.Server.Helpers;
using Xunit;

namespace Jellyfin.Server.Tests;

[Collection(EnvironmentTestCollection.Name)]
public sealed class StartupHelpersTests : IDisposable
{
    private const string Secret = "Host=override;Username=jf;Password=env-secret";

    private readonly string _root;
    private readonly ServerApplicationPaths _paths;
    private readonly CapturingLoggerProvider _logs = new();

    public StartupHelpersTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "jellyfin-startup-helpers-tests", Guid.NewGuid().ToString("N"));
        _paths = new ServerApplicationPaths(
            Path.Combine(_root, "data"),
            Path.Combine(_root, "log"),
            Path.Combine(_root, "config"),
            Path.Combine(_root, "cache"),
            Path.Combine(_root, "web"));
    }

    [Theory]
    [InlineData("JELLYFIN_DATABASE__CONNECTIONSTRING")]
    [InlineData("jellyfin_database__connectionstring")]
    [InlineData("Jellyfin_Database__ConnectionString")]
    [InlineData("JELLYFIN_DATABASE:CONNECTIONSTRING")]
    [InlineData("jellyfin_Database:connectionString")]
    public void LogEnvironmentInfo_VariableMappingToTheOverrideKey_DoesNotLogItsValue(string variable)
    {
        using var scope = new EnvironmentVariableScope(variable, Secret);

        StartupHelpers.LogEnvironmentInfo(_logs.CreateLogger("test"), _paths);

        Assert.Contains(_logs.Entries, entry => entry.Message.Contains(variable, StringComparison.Ordinal));
        foreach (var logged in _logs.Entries)
        {
            Assert.DoesNotContain("env-secret", logged.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("env-secret", logged.State, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("JELLYFIN_DATABASE__CONNECTIONSTRINGS")]
    [InlineData("JELLYFIN_DATABASE__TIMEOUT")]
    [InlineData("JELLYFIN_OTHER__CONNECTIONSTRING")]
    public void LogEnvironmentInfo_UnrelatedVariable_StillLogsItsValue(string variable)
    {
        using var scope = new EnvironmentVariableScope(variable, "visible-value");

        StartupHelpers.LogEnvironmentInfo(_logs.CreateLogger("test"), _paths);

        Assert.Contains(_logs.Entries, entry => entry.Message.Contains("visible-value", StringComparison.Ordinal));
    }

    public void Dispose()
    {
        _logs.Dispose();
        try
        {
            Directory.Delete(_root, true);
        }
        catch (IOException)
        {
            // Nothing is created below the root, a missing directory is fine.
        }
    }
}
