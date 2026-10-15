using System;
using Jellyfin.Database.Testing;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;
using Xunit.Sdk;

namespace Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;

public class BackendLivenessTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Verify_NoProviderExpected_AcceptsTheBackend(string? expected)
    {
        BackendLiveness.Verify(expected, new StubBackend(TestDbProvider.Sqlite, new Version(3, 53)));
    }

    [Theory]
    [InlineData("Sqlite", TestDbProvider.Sqlite)]
    [InlineData("sqlite", TestDbProvider.Sqlite)]
    [InlineData(" Postgres ", TestDbProvider.Postgres)]
    [InlineData("POSTGRES", TestDbProvider.Postgres)]
    public void Verify_ExpectedProviderIsTheBackend_Passes(string expected, TestDbProvider provider)
    {
        BackendLiveness.Verify(expected, new StubBackend(provider, new Version(16, 15)));
    }

    [Theory]
    [InlineData("Postgres", TestDbProvider.Sqlite)]
    [InlineData("Sqlite", TestDbProvider.Postgres)]
    public void Verify_ExpectedProviderIsNotTheBackend_Fails(string expected, TestDbProvider provider)
    {
        var ex = Assert.ThrowsAny<XunitException>(() => BackendLiveness.Verify(expected, new StubBackend(provider, new Version(16, 15))));

        Assert.Contains(expected, ex.Message, StringComparison.Ordinal);
        Assert.Contains(provider.ToString(), ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("oracle")]
    [InlineData("42")]
    [InlineData("Sqlite,Postgres")]
    public void Verify_ExpectedProviderIsUnknown_Fails(string expected)
    {
        var ex = Assert.ThrowsAny<XunitException>(() => BackendLiveness.Verify(expected, new StubBackend(TestDbProvider.Sqlite, new Version(3, 53))));

        Assert.Contains(expected, ex.Message, StringComparison.Ordinal);
        Assert.Contains(BackendLiveness.ExpectedProviderVariable, ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void VerifyProvider_NoProviderExpected_AcceptsAnyProvider(string? expected)
    {
        BackendLiveness.VerifyProvider(expected, TestDbProvider.Sqlite);
        BackendLiveness.VerifyProvider(expected, TestDbProvider.Postgres);
    }

    [Theory]
    [InlineData("Postgres", TestDbProvider.Postgres)]
    [InlineData(" sqlite ", TestDbProvider.Sqlite)]
    public void VerifyProvider_ExpectedProviderIsTheActualOne_Passes(string expected, TestDbProvider actual)
    {
        BackendLiveness.VerifyProvider(expected, actual);
    }

    [Theory]
    [InlineData("Postgres", TestDbProvider.Sqlite)]
    [InlineData("Sqlite", TestDbProvider.Postgres)]
    [InlineData("oracle", TestDbProvider.Sqlite)]
    public void VerifyProvider_ExpectedProviderIsNotTheActualOne_Fails(string expected, TestDbProvider actual)
    {
        var ex = Assert.ThrowsAny<XunitException>(() => BackendLiveness.VerifyProvider(expected, actual));

        Assert.Contains(expected, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Verify_ServerOlderThanTheFloor_Fails()
    {
        var floor = ServerVersionFloors.For(TestDbProvider.Postgres);

        Assert.ThrowsAny<XunitException>(() => BackendLiveness.Verify(null, new StubBackend(TestDbProvider.Postgres, new Version(floor.Major - 1, 9))));
    }

    [Fact]
    public void Verify_ServerAtTheFloor_Passes()
    {
        BackendLiveness.Verify(null, new StubBackend(TestDbProvider.Postgres, ServerVersionFloors.For(TestDbProvider.Postgres)));
    }

    [Theory]
    [InlineData(TestDbProvider.MySql, "8.3.99")]
    [InlineData(TestDbProvider.MariaDb, "11.4.6")]
    [InlineData(TestDbProvider.MariaDb, "10.11.9")]
    public void Verify_MySqlFamilyServerOlderThanTheFloor_Fails(TestDbProvider provider, string version)
    {
        Assert.ThrowsAny<XunitException>(() => BackendLiveness.Verify(null, new StubBackend(provider, Version.Parse(version))));
    }

    [Theory]
    [InlineData(TestDbProvider.MySql, "8.4.11")]
    [InlineData(TestDbProvider.MySql, "9.1.0")]
    [InlineData(TestDbProvider.MariaDb, "11.4.7")]
    [InlineData(TestDbProvider.MariaDb, "11.8.0")]
    public void Verify_MySqlFamilyServerAtOrAboveTheFloor_Passes(TestDbProvider provider, string version)
    {
        BackendLiveness.Verify(null, new StubBackend(provider, Version.Parse(version)));
    }

    [Theory]
    [InlineData(TestDbProvider.MySql, "MariaDb")]
    [InlineData(TestDbProvider.MariaDb, "MySql")]
    public void VerifyProvider_OtherEngineOfTheMySqlFamily_Fails(TestDbProvider actual, string expected)
    {
        Assert.ThrowsAny<XunitException>(() => BackendLiveness.VerifyProvider(expected, actual));
    }

    [Fact]
    public void Verify_ExpectedProviderIsNotTheBackend_FailsBeforeReachingTheServer()
    {
        var backend = new StubBackend(TestDbProvider.Sqlite, null);

        Assert.ThrowsAny<XunitException>(() => BackendLiveness.Verify("Postgres", backend));
    }

    [Fact]
    public void Verify_ServerVersionUnavailable_Propagates()
    {
        var backend = new StubBackend(TestDbProvider.Postgres, null);

        Assert.Throws<InvalidOperationException>(() => BackendLiveness.Verify("Postgres", backend));
    }

    [Theory]
    [InlineData(TestDbProvider.Sqlite, "3.53.3")]
    [InlineData(TestDbProvider.Postgres, "PostgreSQL 16.15 (Debian 16.15-1.pgdg13+1) on x86_64-pc-linux-gnu, compiled by gcc (Debian 14.2.0-19) 14.2.0, 64-bit")]
    [InlineData(TestDbProvider.MySql, "8.4.11")]
    [InlineData(TestDbProvider.MySql, "9.7.2")]
    [InlineData(TestDbProvider.MariaDb, "11.4.13-MariaDB-ubu2404")]
    [InlineData(TestDbProvider.MariaDb, "12.3.3-MariaDB-ubu2404")]
    [InlineData(TestDbProvider.MariaDb, "11.8.9-mariadb")]
    [InlineData(TestDbProvider.MariaDb, "5.5.5-11.4.13-MariaDB")]
    [InlineData(TestDbProvider.MariaDb, "5.5.5-10.11.9-MariaDB-ubu2204-log")]
    public void VerifyServer_ServerOfTheSelectedEngine_Passes(TestDbProvider selected, string reported)
    {
        BackendLiveness.VerifyServer(selected, reported);
    }

    [Theory]
    [InlineData(TestDbProvider.MySql, "11.4.13-MariaDB-ubu2404", "MariaDb")]
    [InlineData(TestDbProvider.MySql, "12.3.3-MariaDB-ubu2404", "MariaDb")]
    [InlineData(TestDbProvider.MySql, "5.5.5-11.4.13-MariaDB", "MariaDb")]
    [InlineData(TestDbProvider.MySql, "PostgreSQL 16.15 (Debian 16.15-1.pgdg13+1)", "Postgres")]
    [InlineData(TestDbProvider.MariaDb, "8.4.11", "MariaDB")]
    [InlineData(TestDbProvider.MariaDb, "9.7.2", "MariaDB")]
    [InlineData(TestDbProvider.Postgres, "8.4.11", "PostgreSQL")]
    [InlineData(TestDbProvider.Sqlite, "11.4.13-MariaDB-ubu2404", "MariaDb")]
    public void VerifyServer_ServerOfAnotherEngine_Fails(TestDbProvider selected, string reported, string named)
    {
        var ex = Assert.ThrowsAny<XunitException>(() => BackendLiveness.VerifyServer(selected, reported));

        Assert.Contains(reported, ex.Message, StringComparison.Ordinal);
        Assert.Contains(named, ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("MySql", TestDbProvider.MySql, "11.4.13-MariaDB-ubu2404")]
    [InlineData("MariaDb", TestDbProvider.MariaDb, "8.4.11")]
    public void Verify_ExpectedProviderIsTheBackendButItsServerIsOfAnotherEngine_Fails(string expected, TestDbProvider provider, string reported)
    {
        var backend = new StubBackend(provider, new Version(11, 4, 13), reported);

        Assert.ThrowsAny<XunitException>(() => BackendLiveness.Verify(expected, backend));
    }

    private sealed class StubBackend(TestDbProvider provider, Version? serverVersion, string? reportedServerVersion = null) : ITestDatabaseBackend
    {
        public TestDbProvider Provider => provider;

        public Version ServerVersion => serverVersion ?? throw new InvalidOperationException("The server cannot be reached.");

        // A version as a server of the engine reports it, unless the test gives one.
        public string ReportedServerVersion => reportedServerVersion ?? provider switch
        {
            TestDbProvider.Postgres => $"PostgreSQL {ServerVersion}",
            TestDbProvider.MariaDb => $"{ServerVersion}-MariaDB",
            _ => ServerVersion.ToString()
        };

        public TestDatabase Create(TestSchemaSource source, IInterceptor[] interceptors, bool fresh = false) => throw new NotSupportedException();

        public TestDatabase CreateEmpty() => throw new NotSupportedException();
    }
}
