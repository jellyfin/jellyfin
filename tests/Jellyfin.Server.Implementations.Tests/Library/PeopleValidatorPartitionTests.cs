using System;
using System.Collections.Generic;
using Emby.Server.Implementations.Library.Validators;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library;

/// <summary>
/// Tests for how the people validator decides which credits need a person item and which person items
/// nothing credits any more. Keying either half on the item's name rather than its id put the two halves
/// in a loop that created, refreshed and deleted the same people on every run, so these pin the id.
/// </summary>
public class PeopleValidatorPartitionTests
{
    // Stands in for the real item-by-name id: derived from the credit name, case-insensitively, and
    // from nothing else. The property that matters is that it does not depend on the item's own name.
    private static Guid PersonId(string creditName)
    {
#pragma warning disable CA5351 // Do Not Use Broken Cryptographic Algorithms
        var hash = System.Security.Cryptography.MD5.HashData(
            System.Text.Encoding.Unicode.GetBytes(creditName.ToLowerInvariant()));
#pragma warning restore CA5351 // Do Not Use Broken Cryptographic Algorithms
        return new Guid(hash);
    }

    [Fact]
    public void PartitionCreditsByPersonId_ProviderRenamedThePerson_KeepsThemAndCreatesNothing()
    {
        // The credit still says "AURORA"; the item it made has been renamed to "Aurora" by the provider
        // that refreshed it. Nothing about the library changed, so nothing should be created or deleted.
        var credits = new[] { "AURORA" };
        var existing = new HashSet<Guid> { PersonId("AURORA") };

        var (newNames, deadIds) = PeopleValidator.PartitionCreditsByPersonId(credits, PersonId, existing);

        Assert.Empty(newNames);
        Assert.Empty(deadIds);
    }

    [Theory]
    // Every shape of rename seen in the wild on a real library.
    [InlineData("AURORA")]
    [InlineData("Amir AboulEla")]
    [InlineData("Miguel Ángel Fuentes")]
    [InlineData("a‐ha")]
    [InlineData("윤현민")]
    public void PartitionCreditsByPersonId_CreditWithAnItem_IsNeverBothCreatedAndDeleted(string creditName)
    {
        var existing = new HashSet<Guid> { PersonId(creditName) };

        var (newNames, deadIds) = PeopleValidator.PartitionCreditsByPersonId([creditName], PersonId, existing);

        Assert.Empty(newNames);
        Assert.Empty(deadIds);
    }

    [Fact]
    public void PartitionCreditsByPersonId_CreditWithNoItem_IsCreated()
    {
        var (newNames, deadIds) = PeopleValidator.PartitionCreditsByPersonId(
            ["Wanted Person"],
            PersonId,
            new HashSet<Guid>());

        Assert.Equal(["Wanted Person"], newNames);
        Assert.Empty(deadIds);
    }

    [Fact]
    public void PartitionCreditsByPersonId_ItemNoCreditNames_IsDead()
    {
        var orphan = PersonId("Nobody Credits Me");
        var existing = new HashSet<Guid> { PersonId("Credited"), orphan };

        var (newNames, deadIds) = PeopleValidator.PartitionCreditsByPersonId(["Credited"], PersonId, existing);

        Assert.Empty(newNames);
        Assert.Equal([orphan], deadIds);
    }

    [Fact]
    public void PartitionCreditsByPersonId_CreditsNormalizingOntoOneId_CreateOneItem()
    {
        // "AURORA" and "Aurora" are one person as far as the item-by-name id is concerned, so exactly
        // one of them should create the item and neither should end up dead.
        var (newNames, deadIds) = PeopleValidator.PartitionCreditsByPersonId(
            ["AURORA", "Aurora", "aurora"],
            PersonId,
            new HashSet<Guid>());

        Assert.Single(newNames);
        Assert.Empty(deadIds);
    }

    [Fact]
    public void PartitionCreditsByPersonId_SecondRunAfterCreating_AsksForNothingFurther()
    {
        // The churn showed up as a run that never settled, so drive two rounds: whatever round one
        // created must leave round two with nothing to do.
        string[] credits = ["AURORA", "Amir AboulEla", "Miguel Ángel Fuentes"];
        var existing = new HashSet<Guid>();

        var (firstNames, firstDead) = PeopleValidator.PartitionCreditsByPersonId(credits, PersonId, existing);
        Assert.Equal(3, firstNames.Count);
        Assert.Empty(firstDead);

        foreach (var created in firstNames)
        {
            existing.Add(PersonId(created));
        }

        var (secondNames, secondDead) = PeopleValidator.PartitionCreditsByPersonId(credits, PersonId, existing);

        Assert.Empty(secondNames);
        Assert.Empty(secondDead);
    }
}
