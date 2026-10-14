using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.EfMigrations;

public class MigrationWorkflowTests
{
    private const string MigrationNamePlaceholder = "<Name>";

    public static TheoryData<string> ProviderNames() => new(MigrationProvider.All.Select(e => e.Name));

    public static TheoryData<string> MirroringProviderNames() => new(MigrationProvider.All.Where(e => !e.IsReference).Select(e => e.Name));

    [Theory]
    [MemberData(nameof(ProviderNames))]
    [Trait("Category", "Migrations")]
    public void Model_HasNoPendingChanges(string providerName)
    {
        var provider = MigrationProvider.Get(providerName);

        using var context = provider.CreateContext();

        Assert.False(
            context.Database.HasPendingModelChanges(),
            $"The EF Core model has changes that the {provider.Name} migrations do not contain. "
            + $"Run '{MigrationProvider.Command} {MigrationNamePlaceholder}' with a name that describes the change. "
            + "It generates the migrations of every provider with the same id. Review them and commit them with the change. "
            + $"If the {MigrationProvider.Reference.Name} migration of the change already exists, run the command with its name: "
            + "it skips the providers that have the migration and generates only the missing ones.");
    }

    [Theory]
    [MemberData(nameof(MirroringProviderNames))]
    [Trait("Category", "Migrations")]
    public void MigrationsAfterTheBaseline_AreThoseOfTheReferenceProviderAfterWhatItCovers(string providerName)
    {
        var provider = MigrationProvider.Get(providerName);
        if (provider.BaselineId is null || provider.CoversThroughReferenceId is null)
        {
            Assert.Fail(
                $"The provider {provider.Name} is not the reference provider, so its row in {nameof(MigrationProvider)}.{nameof(MigrationProvider.All)} "
                + $"needs a {nameof(MigrationProvider.BaselineId)} and a {nameof(MigrationProvider.CoversThroughReferenceId)}. "
                + "Without them its migrations cannot be compared with those of the reference provider.");
        }

        var expected = MigrationProvider.Reference.MigrationIds()
            .Where(id => string.CompareOrdinal(id, provider.CoversThroughReferenceId) > 0)
            .ToList();
        var actual = provider.MigrationIds().Where(id => id != provider.BaselineId).ToList();

        if (!expected.SequenceEqual(actual))
        {
            Assert.Fail(DescribeMismatch(MigrationProvider.Reference.Name, provider.Name, expected, actual));
        }
    }

    private static string DescribeMismatch(string referenceName, string providerName, List<string> expected, List<string> actual)
    {
        var lines = new List<string>
        {
            $"The {providerName} migrations after the baseline must have the same ids as the {referenceName} migrations."
        };

        lines.AddRange(expected.Except(actual).Select(id => $"  {id} exists for {referenceName} only."));
        lines.AddRange(actual.Except(expected).Select(id => $"  {id} exists for {providerName} only."));
        lines.Add(
            $"If the {referenceName} migration is correct, delete the {providerName} migration files, restore its JellyfinDbModelSnapshot.cs to the commit before the migration, "
            + $"and run '{MigrationProvider.Command} {MigrationNamePlaceholder}' with the name of the {referenceName} migration again; it generates only the missing providers. "
            + $"Otherwise delete the migration from every provider, restore their snapshots to the commit before it, and run '{MigrationProvider.Command} {MigrationNamePlaceholder}'.");
        return string.Join(Environment.NewLine, lines);
    }
}
