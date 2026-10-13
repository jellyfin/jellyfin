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

        var floor = ServerVersionFloors.For(backend.Provider);

        Assert.True(backend.ServerVersion.Major > 0);
        Assert.True(backend.ServerVersion >= floor, $"{backend.Provider} {backend.ServerVersion} is older than the supported {floor}.");
    }
}
