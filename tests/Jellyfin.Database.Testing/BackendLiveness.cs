using System;
using System.Linq;
using Xunit;

namespace Jellyfin.Database.Testing;

public static class BackendLiveness
{
    public const string ExpectedProviderVariable = "JELLYFIN_TEST_EXPECTED_PROVIDER";

    public static void VerifyProvider(string? expectedProvider, TestDbProvider actual)
    {
        if (string.IsNullOrWhiteSpace(expectedProvider))
        {
            return;
        }

        var expected = Enum.GetValues<TestDbProvider>()
            .Cast<TestDbProvider?>()
            .FirstOrDefault(p => string.Equals(p.ToString(), expectedProvider.Trim(), StringComparison.OrdinalIgnoreCase));
        if (expected is null)
        {
            Assert.Fail($"{ExpectedProviderVariable} is set to '{expectedProvider}', expected one of: {string.Join(", ", Enum.GetNames<TestDbProvider>())}.");
        }

        Assert.True(
            expected == actual,
            $"The job asked for {expected} ({ExpectedProviderVariable}), but the tests ran on {actual}.");
    }

    public static void Verify(string? expectedProvider, ITestDatabaseBackend backend)
    {
        VerifyProvider(expectedProvider, backend.Provider);

        // A server of another engine is told first, as its version would be compared with the floor of the wrong engine.
        VerifyServer(backend.Provider, backend.ReportedServerVersion);

        var floor = ServerVersionFloors.For(backend.Provider);

        Assert.True(backend.ServerVersion.Major > 0);
        Assert.True(backend.ServerVersion >= floor, $"{backend.Provider} {backend.ServerVersion} is older than the supported {floor}.");
    }

    /// <summary>
    /// Checks that the server is of the engine that was selected, by the version it reports: the version holds the
    /// marker of the selected engine, if it has one, and the marker of no other engine. A server of another engine
    /// that speaks the same protocol, such as MariaDB for MySQL, would otherwise pass.
    /// </summary>
    /// <param name="selected">The engine that was selected.</param>
    /// <param name="reportedVersion">The version as the server reports it.</param>
    public static void VerifyServer(TestDbProvider selected, string reportedVersion)
    {
        var own = EngineTraits.For(selected).ServerMarker;
        Assert.True(
            own is null || reportedVersion.Contains(own, StringComparison.OrdinalIgnoreCase),
            $"The tests ran on {selected}, but the server reports the version '{reportedVersion}', which does not name {own}.");

        var other = Enum.GetValues<TestDbProvider>()
            .Select(EngineTraits.For)
            .FirstOrDefault(traits => traits.ServerMarker is { } marker
                && !string.Equals(marker, own, StringComparison.OrdinalIgnoreCase)
                && reportedVersion.Contains(marker, StringComparison.OrdinalIgnoreCase));
        Assert.True(
            other is null,
            $"The tests ran on {selected}, but the server reports the version '{reportedVersion}', which is a server of {other?.Provider}.");
    }
}
