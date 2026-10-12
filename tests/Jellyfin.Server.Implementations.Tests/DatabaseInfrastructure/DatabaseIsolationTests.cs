using System;
using System.Linq;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Testing;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;

public sealed class DatabaseIsolationTests : DatabaseTestFixture
{
    [Fact]
    public void Constructor_InstancesDoNotShareData()
    {
        using var other = new DatabaseIsolationTests();
        var id = AddItem();

        Assert.True(ContainsItem(id));
        Assert.False(other.ContainsItem(id));
    }

    [Fact]
    public void Constructor_DataDoesNotOutliveTheInstance()
    {
        Guid id;
        using (var first = new DatabaseIsolationTests())
        {
            id = first.AddItem();
            Assert.True(first.ContainsItem(id));
        }

        using var second = new DatabaseIsolationTests();
        Assert.False(second.ContainsItem(id));
    }

    [Fact]
    public void CreateDbContext_ContextsOfOneInstanceShareData()
    {
        var id = AddItem();

        Assert.True(ContainsItem(id));
    }

    private Guid AddItem()
    {
        var id = Guid.NewGuid();
        using var context = CreateDbContext();
        context.BaseItems.Add(new BaseItemEntity { Id = id, Type = "Test", IsFolder = false });
        context.SaveChanges();
        return id;
    }

    private bool ContainsItem(Guid id)
    {
        using var context = CreateDbContext();
        return context.BaseItems.Any(e => e.Id.Equals(id));
    }
}
