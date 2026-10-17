using System;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Testing;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;

/// <summary>
/// Holds what a database provider says of the locking behaviours it supports, and what the server does with it when it
/// starts. Nothing here connects: the providers are created and, where a test needs the server's own registration, only
/// registered and resolved.
/// </summary>
public sealed class LockingBehaviourRegistrationTests
{
    public static TheoryData<TestDbProvider> Engines()
    {
        var engines = new TheoryData<TestDbProvider>();
        foreach (var provider in Enum.GetValues<TestDbProvider>())
        {
            engines.Add(provider);
        }

        return engines;
    }

    [Theory]
    [MemberData(nameof(Engines))]
    public void SupportsPessimisticLocking_IsTrueOnlyForSqlite(TestDbProvider engine)
    {
        var traits = EngineTraits.For(engine);

        var provider = CreateProvider(traits);

        // The embedded engine is the one with a single writer, which the process-wide lock of the behaviour stands in for.
        Assert.Equal(traits.IsEmbedded, provider.SupportsPessimisticLocking);
    }

    private static IJellyfinDatabaseProvider CreateProvider(EngineTraits traits)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(Mock.Of<IApplicationPaths>())
            .BuildServiceProvider();
        return (IJellyfinDatabaseProvider)ActivatorUtilities.CreateInstance(services, traits.ProviderType);
    }
}
