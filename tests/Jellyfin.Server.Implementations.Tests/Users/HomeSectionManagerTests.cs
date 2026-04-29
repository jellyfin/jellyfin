using System;
using System.IO;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Server.Implementations.Users;
using MediaBrowser.Model.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Users;

public sealed class HomeSectionManagerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IDbContextFactory<JellyfinDbContext> _dbContextFactory;
    private readonly HomeSectionManager _manager;
    private readonly Guid _userId = Guid.NewGuid();

    private readonly string _dbPath;

    public HomeSectionManagerTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"homesection_test_{Guid.NewGuid():N}.db");
        _connection = new SqliteConnection($"DataSource={_dbPath}");
        _connection.Open();

        var options = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite($"DataSource={_dbPath}")
            .Options;

        var mockLocking = new Mock<IEntityFrameworkCoreLockingBehavior>();
        mockLocking.Setup(l => l.OnSaveChanges(It.IsAny<JellyfinDbContext>(), It.IsAny<Action>()))
            .Callback<JellyfinDbContext, Action>((_, save) => save());
        var mockProvider = new Mock<IJellyfinDatabaseProvider>();

        using (var initCtx = new JellyfinDbContext(
            options,
            NullLogger<JellyfinDbContext>.Instance,
            mockProvider.Object,
            mockLocking.Object))
        {
            initCtx.Database.EnsureCreated();
        }

        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(() =>
            new JellyfinDbContext(
                options,
                NullLogger<JellyfinDbContext>.Instance,
                mockProvider.Object,
                mockLocking.Object));

        _dbContextFactory = factory.Object;
        _manager = new HomeSectionManager(_dbContextFactory);
    }

    [Fact]
    public void GetHomeSections_ReturnsAllSectionsForUser()
    {
        // Arrange
        _manager.CreateHomeSection(_userId, new HomeSectionOptions
        {
            Name = "Test Section 1",
            SectionType = HomeSectionType.LatestMedia,
            Priority = 1,
            MaxItems = 10
        });

        _manager.CreateHomeSection(_userId, new HomeSectionOptions
        {
            Name = "Test Section 2",
            SectionType = HomeSectionType.NextUp,
            Priority = 2,
            MaxItems = 5
        });

        // Different user
        _manager.CreateHomeSection(Guid.NewGuid(), new HomeSectionOptions
        {
            Name = "Other User Section",
            SectionType = HomeSectionType.LatestMedia,
            Priority = 1
        });

        // Act
        var result = _manager.GetHomeSections(_userId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Test Section 1", result[0].Name);
        Assert.Equal("Test Section 2", result[1].Name);
    }

    [Fact]
    public void GetHomeSection_WithValidId_ReturnsSection()
    {
        // Arrange
        var sectionId = _manager.CreateHomeSection(_userId, new HomeSectionOptions
        {
            Name = "Test Section",
            SectionType = HomeSectionType.LatestMedia,
            Priority = 1,
            MaxItems = 10,
            SortOrder = SortOrder.Descending,
            SortBy = SortOrder.Ascending
        });

        // Act
        var result = _manager.GetHomeSection(_userId, sectionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Section", result.Name);
        Assert.Equal(HomeSectionType.LatestMedia, result.SectionType);
        Assert.Equal(1, result.Priority);
        Assert.Equal(10, result.MaxItems);
        Assert.Equal(SortOrder.Descending, result.SortOrder);
        Assert.Equal(SortOrder.Ascending, result.SortBy);
    }

    [Fact]
    public void GetHomeSection_WithInvalidId_ReturnsNull()
    {
        var result = _manager.GetHomeSection(_userId, Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public void CreateHomeSection_AddsNewSection()
    {
        // Act
        var sectionId = _manager.CreateHomeSection(_userId, new HomeSectionOptions
        {
            Name = "New Section",
            SectionType = HomeSectionType.LatestMedia,
            Priority = 3,
            MaxItems = 15
        });

        // Assert
        Assert.NotEqual(Guid.Empty, sectionId);
        var section = _manager.GetHomeSection(_userId, sectionId);
        Assert.NotNull(section);
        Assert.Equal("New Section", section.Name);
    }

    [Fact]
    public void UpdateHomeSection_WithValidId_UpdatesSection()
    {
        // Arrange
        var sectionId = _manager.CreateHomeSection(_userId, new HomeSectionOptions
        {
            Name = "Original",
            SectionType = HomeSectionType.LatestMedia,
            Priority = 1,
            MaxItems = 10
        });

        // Act
        var result = _manager.UpdateHomeSection(_userId, sectionId, new HomeSectionOptions
        {
            Name = "Updated",
            SectionType = HomeSectionType.NextUp,
            Priority = 3,
            MaxItems = 15,
            SortOrder = SortOrder.Descending,
            SortBy = SortOrder.Descending
        });

        // Assert
        Assert.True(result);
        var section = _manager.GetHomeSection(_userId, sectionId);
        Assert.NotNull(section);
        Assert.Equal("Updated", section.Name);
        Assert.Equal(HomeSectionType.NextUp, section.SectionType);
        Assert.Equal(3, section.Priority);
        Assert.Equal(15, section.MaxItems);
    }

    [Fact]
    public void UpdateHomeSection_WithInvalidId_ReturnsFalse()
    {
        var result = _manager.UpdateHomeSection(_userId, Guid.NewGuid(), new HomeSectionOptions { Name = "X" });
        Assert.False(result);
    }

    [Fact]
    public void DeleteHomeSection_WithValidId_RemovesSection()
    {
        // Arrange
        var sectionId = _manager.CreateHomeSection(_userId, new HomeSectionOptions
        {
            Name = "To Delete",
            SectionType = HomeSectionType.LatestMedia,
            Priority = 1,
            MaxItems = 10
        });

        // Act
        var result = _manager.DeleteHomeSection(_userId, sectionId);

        // Assert
        Assert.True(result);
        Assert.Null(_manager.GetHomeSection(_userId, sectionId));
    }

    [Fact]
    public void DeleteHomeSection_WithInvalidId_ReturnsFalse()
    {
        var result = _manager.DeleteHomeSection(_userId, Guid.NewGuid());
        Assert.False(result);
    }

    public void Dispose()
    {
        _connection.Dispose();
        try
        {
            File.Delete(_dbPath);
        }
        catch (IOException)
        {
            // Best effort cleanup
        }
    }
}
