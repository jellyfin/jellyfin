using System;
using Jellyfin.Database.Testing;
using Xunit;

namespace Jellyfin.Server.Tests;

/// <summary>
/// The sentinel of a provider run of this project: it fails when the backend is not the one the job asked for, or
/// cannot be reached, and prints the backend and its server version.
/// </summary>
public class ProviderBackendTests(ITestOutputHelper output)
{
    [Fact]
    public void ProviderBackend_IsLive()
    {
        var backend = TestDatabaseEnvironment.Backend;

        BackendLiveness.Verify(Environment.GetEnvironmentVariable(BackendLiveness.ExpectedProviderVariable), backend);

        output.WriteLine($"Database backend: {backend.Provider} {backend.ServerVersion}");
    }
}
