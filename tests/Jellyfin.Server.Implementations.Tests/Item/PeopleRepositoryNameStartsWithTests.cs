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
/// <c>NameStartsWith</c> filters the person list by the first letters of a name. Names keep the case a metadata
/// provider wrote them in, so the filter folds the name as well as the letters, as the name range filters do.
/// </summary>
public sealed class PeopleRepositoryNameStartsWithTests : SqliteDbTestFixture
{
    private readonly PeopleRepository _repository;

    public PeopleRepositoryNameStartsWithTests()
    {
        using (var context = CreateDbContext())
        {
            // SQLite's LIKE ignores ASCII case by default; match the case-sensitive LIKE of other providers.
            context.Database.ExecuteSqlRaw("PRAGMA case_sensitive_like = ON");

            foreach (var name in new[] { "50 Cent", "Brad Pitt", "bob dylan", "émilie dequenne", "Zoe Saldana" })
            {
                context.Peoples.Add(new People { Id = Guid.NewGuid(), Name = name, PersonType = "Actor" });
            }

            context.SaveChanges();
        }

        _repository = new PeopleRepository(CreateDbContextFactory(), new ItemTypeLookup(), new Mock<IItemQueryHelpers>().Object);
    }

    [Theory]
    [InlineData("B", new[] { "bob dylan", "Brad Pitt" })]
    [InlineData("b", new[] { "bob dylan", "Brad Pitt" })]
    [InlineData("Br", new[] { "Brad Pitt" })]
    [InlineData("z", new[] { "Zoe Saldana" })]
    [InlineData("É", new[] { "émilie dequenne" })]
    public void GetPeople_NameStartsWith_IgnoresCase(string nameStartsWith, string[] expected)
    {
        var result = _repository.GetPeople(new InternalPeopleQuery
        {
            NameStartsWith = nameStartsWith
        });

        Assert.Equal(expected, result.Items.Select(p => p.Name));
        Assert.Equal(expected.Length, result.TotalRecordCount);
    }
}
