using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// The person NameStartsWith filter on a database whose <c>LIKE</c> is case-sensitive, as on PostgreSQL.
/// </summary>
public sealed class PeopleRepositoryCaseSensitiveLikeTests : SqliteDbTestFixture
{
    private readonly PeopleRepository _repository;

    public PeopleRepositoryCaseSensitiveLikeTests()
    {
        using (var context = CreateDbContext())
        {
            foreach (var name in new[] { "Bob", "bobby", "Rob", "ALICE", "bo", "Zoe" })
            {
                context.Peoples.Add(new People { Id = Guid.NewGuid(), Name = name, PersonType = "Actor" });
            }

            context.SaveChanges();
        }

        UseCaseSensitiveLike();
        _repository = new PeopleRepository(CreateDbContextFactory(), new ItemTypeLookup(), new Mock<IItemQueryHelpers>().Object);
    }

    [Theory]
    [InlineData("bo")]
    [InlineData("BO")]
    [InlineData("Bo")]
    public void GetPeople_NameStartsWith_IgnoresCase(string prefix)
    {
        Assert.Equal(["Bob", "bo", "bobby"], Names(new InternalPeopleQuery { NameStartsWith = prefix }));
    }

    /// <summary>
    /// Makes <c>LIKE</c> on this database case-sensitive, as it is on PostgreSQL.
    /// </summary>
    private void UseCaseSensitiveLike()
    {
        using var context = CreateDbContext();
        context.Database.ExecuteSqlRaw("PRAGMA case_sensitive_like = ON");
    }

    private string[] Names(InternalPeopleQuery query)
        => _repository.GetPeople(query).Items.Select(p => p.Name).Order(StringComparer.Ordinal).ToArray();
}
