using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

public sealed class ItemCountServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly IApplicationPaths _applicationPaths;
    private readonly ItemCountService _service;

    public ItemCountServiceTests()
    {
        _applicationPaths = new Mock<IApplicationPaths>().Object;

        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .Options;

        using (var context = CreateDbContext())
        {
            context.Database.EnsureCreated();
        }

        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(CreateDbContext);

        _service = new ItemCountService(
            factory.Object,
            new Mock<IItemTypeLookup>().Object,
            new Mock<IItemQueryHelpers>().Object);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public void GetChildCountBatch_LargeParentIdSet_DoesNotExceedSqliteVariableLimit()
    {
        var hierarchicalParentId = Guid.NewGuid();
        var linkedParentId = Guid.NewGuid();

        var hierarchicalChildId = Guid.NewGuid();
        var linkedChildId1 = Guid.NewGuid();
        var linkedChildId2 = Guid.NewGuid();

        using (var context = CreateDbContext())
        {
            context.BaseItems.AddRange(
                CreateItem(hierarchicalParentId),
                CreateItem(linkedParentId),
                CreateItem(hierarchicalChildId, hierarchicalParentId),
                CreateItem(linkedChildId1),
                CreateItem(linkedChildId2));

            context.LinkedChildren.AddRange(
                new LinkedChildEntity
                {
                    ParentId = linkedParentId,
                    ChildId = linkedChildId1,
                    ChildType = LinkedChildType.Manual,
                    SortOrder = 0
                },
                new LinkedChildEntity
                {
                    ParentId = linkedParentId,
                    ChildId = linkedChildId2,
                    ChildType = LinkedChildType.Manual,
                    SortOrder = 1
                });

            context.SaveChanges();
        }

        var parentIds = Enumerable.Range(0, 40_000)
            .Select(_ => Guid.NewGuid())
            .ToList();

        parentIds.Add(hierarchicalParentId);
        parentIds.Add(linkedParentId);

        var result = _service.GetChildCountBatch(parentIds, null);

        Assert.Equal(1, result[hierarchicalParentId]);
        Assert.Equal(2, result[linkedParentId]);
        Assert.Equal(parentIds.Count, result.Count);
    }

    private static BaseItemEntity CreateItem(Guid id, Guid? parentId = null)
    {
        return new BaseItemEntity
        {
            Id = id,
            Type = "Folder",
            ParentId = parentId,
            IsFolder = true
        };
    }

    private JellyfinDbContext CreateDbContext()
    {
        return new JellyfinDbContext(
            _dbOptions,
            NullLogger<JellyfinDbContext>.Instance,
            new SqliteDatabaseProvider(
                _applicationPaths,
                NullLogger<SqliteDatabaseProvider>.Instance),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
    }
}
