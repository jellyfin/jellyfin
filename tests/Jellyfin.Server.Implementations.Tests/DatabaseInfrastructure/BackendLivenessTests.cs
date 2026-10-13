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

    private sealed class StubBackend(TestDbProvider provider, Version? serverVersion) : ITestDatabaseBackend
    {
        public TestDbProvider Provider => provider;

        public Version ServerVersion => serverVersion ?? throw new InvalidOperationException("The server cannot be reached.");

        public TestDatabase Create(TestSchemaSource source, IInterceptor[] interceptors, bool fresh = false) => throw new NotSupportedException();

        public TestDatabase CreateEmpty() => throw new NotSupportedException();
    }
}
