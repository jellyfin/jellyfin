using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using Moq;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// The role of a credit is part of the key of the credit, so a role from a metadata provider is cut to the
/// length the key allows before credits are compared and saved. Credits whose roles are the same up to that
/// length are one credit.
/// </summary>
public sealed class PeopleRepositoryRoleLengthTests : JellyfinDbTestFixture
{
    private const int RoleLength = KeyNormalizer.PersonRoleMaxLength;

    private static readonly Guid _itemId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private readonly PeopleRepository _repository;

    public PeopleRepositoryRoleLengthTests()
    {
        var itemTypeLookup = new ItemTypeLookup();

        using (var ctx = CreateDbContext())
        {
            ctx.BaseItems.Add(new BaseItemEntity
            {
                Id = _itemId,
                Type = itemTypeLookup.BaseItemKindNames[BaseItemKind.Movie],
                Name = "Movie",
                MediaType = "Video",
                IsMovie = true,
                IsFolder = false,
                IsVirtualItem = false
            });
            ctx.SaveChanges();
        }

        _repository = new PeopleRepository(
            CreateDbContextFactory(),
            itemTypeLookup,
            new Mock<IItemQueryHelpers>().Object);
    }

    [Fact]
    public void UpdatePeople_RoleOverTheBound_IsStoredCut()
    {
        _repository.UpdatePeople(_itemId, [CreatePerson("Person A", new string('r', RoleLength + 50))]);

        Assert.Equal(new string('r', RoleLength), Assert.Single(StoredRoles()));
    }

    [Fact]
    public void UpdatePeople_RoleAtTheBound_IsStoredWhole()
    {
        var role = new string('r', RoleLength);

        _repository.UpdatePeople(_itemId, [CreatePerson("Person A", role)]);

        Assert.Equal(role, Assert.Single(StoredRoles()));
    }

    [Fact]
    public void UpdatePeople_RolesEqualAfterTheCut_AreOneCredit()
    {
        var prefix = new string('r', RoleLength);

        _repository.UpdatePeople(_itemId, [
            CreatePerson("Person A", prefix + "first"),
            CreatePerson("Person A", prefix + "second")
        ]);

        using var ctx = CreateDbContext();
        Assert.Single(ctx.Peoples);
        Assert.Equal(prefix, Assert.Single(ctx.PeopleBaseItemMap).Role);
    }

    [Fact]
    public void UpdatePeople_RolesEqualAfterTheCutExceptInCase_AreOneCredit()
    {
        var prefix = new string('r', RoleLength);

        _repository.UpdatePeople(_itemId, [
            CreatePerson("Person A", prefix + "first"),
            CreatePerson("person a", prefix.ToUpperInvariant() + "second")
        ]);

        using var ctx = CreateDbContext();
        Assert.Equal(prefix, Assert.Single(ctx.PeopleBaseItemMap).Role);
    }

    [Fact]
    public void UpdatePeople_RolesDifferingBeforeTheBound_AreKept()
    {
        var common = new string('r', RoleLength - 1);

        _repository.UpdatePeople(_itemId, [
            CreatePerson("Person A", common + "a" + "tail"),
            CreatePerson("Person A", common + "b" + "tail")
        ]);

        Assert.Equal([common + "a", common + "b"], StoredRoles());
    }

    [Fact]
    public void UpdatePeople_SurrogatePairAcrossTheBound_IsNotSplit()
    {
        var role = new string('r', RoleLength - 1) + "\U0001F600" + "tail";

        _repository.UpdatePeople(_itemId, [CreatePerson("Person A", role)]);

        var stored = Assert.Single(StoredRoles());
        Assert.Equal(new string('r', RoleLength - 1), stored);
    }

    [Fact]
    public void UpdatePeople_SurrogatePairEndingAtTheBound_IsKept()
    {
        var role = new string('r', RoleLength - 2) + "\U0001F600" + "tail";

        _repository.UpdatePeople(_itemId, [CreatePerson("Person A", role)]);

        Assert.Equal(new string('r', RoleLength - 2) + "\U0001F600", Assert.Single(StoredRoles()));
    }

    [Fact]
    public void UpdatePeople_CutRoleSavedAgain_KeepsTheCredit()
    {
        var people = new[] { CreatePerson("Person A", new string('r', RoleLength + 50)) };

        _repository.UpdatePeople(_itemId, people);
        _repository.UpdatePeople(_itemId, [CreatePerson("Person A", new string('r', RoleLength + 50))]);

        Assert.Equal(new string('r', RoleLength), Assert.Single(StoredRoles()));
    }

    [Fact]
    public void UpdatePeople_CutEndsInWhiteSpace_IsStoredWithoutIt()
    {
        var role = new string('r', RoleLength - 2) + "  " + "tail";

        _repository.UpdatePeople(_itemId, [CreatePerson("Person A", role)]);

        Assert.Equal(new string('r', RoleLength - 2), Assert.Single(StoredRoles()));
    }

    [Fact]
    public void UpdatePeople_RolesEqualOnlyAfterTheCutAndTrim_AreOneCredit()
    {
        var prefix = new string('r', RoleLength - 1);

        _repository.UpdatePeople(_itemId, [
            CreatePerson("Person A", prefix + " first"),
            CreatePerson("Person A", prefix + "  second")
        ]);

        Assert.Equal(prefix, Assert.Single(StoredRoles()));
    }

    private static PersonInfo CreatePerson(string name, string role)
        => new() { Name = name, Type = PersonKind.Actor, Role = role };

    private string[] StoredRoles()
    {
        using var ctx = CreateDbContext();
        return ctx.PeopleBaseItemMap.OrderBy(e => e.ListOrder).Select(e => e.Role ?? string.Empty).ToArray();
    }
}
