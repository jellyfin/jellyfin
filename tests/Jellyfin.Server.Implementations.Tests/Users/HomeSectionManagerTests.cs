using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Server.Implementations.Users;
using MediaBrowser.Model.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Users
{
    public sealed class HomeSectionManagerTests : IAsyncDisposable
    {
        private readonly Mock<IDbContextFactory<JellyfinDbContext>> _mockDbContextFactory;
        private readonly Mock<JellyfinDbContext> _mockDbContext;
        private readonly HomeSectionManager _manager;
        private readonly Guid _userId = Guid.NewGuid();
        private readonly List<UserHomeSection> _homeSections;
        private readonly Mock<DbSet<UserHomeSection>> _mockDbSet;

        public HomeSectionManagerTests()
        {
            _homeSections = new List<UserHomeSection>();

            // Setup mock DbSet for UserHomeSections
            _mockDbSet = CreateMockDbSet(_homeSections);

            // Setup mock DbContext
            var mockLogger = new Mock<ILogger<JellyfinDbContext>>();
            var mockProvider = new Mock<IJellyfinDatabaseProvider>();
            _mockDbContext = new Mock<JellyfinDbContext>(
                new DbContextOptions<JellyfinDbContext>(),
                mockLogger.Object,
                mockProvider.Object);

            // Setup the property to return our mock DbSet
            _mockDbContext.Setup(c => c.Set<UserHomeSection>()).Returns(_mockDbSet.Object);

            // Setup mock DbContextFactory
            _mockDbContextFactory = new Mock<IDbContextFactory<JellyfinDbContext>>();
            _mockDbContextFactory.Setup(f => f.CreateDbContext()).Returns(_mockDbContext.Object);

            _manager = new HomeSectionManager(_mockDbContextFactory.Object);
        }

        [Fact]
        public void GetHomeSections_ReturnsAllSectionsForUser()
        {
            // Arrange
            var sectionId1 = Guid.NewGuid();
            var sectionId2 = Guid.NewGuid();

            _homeSections.AddRange(new[]
            {
                new UserHomeSection
                {
                    UserId = _userId,
                    SectionId = sectionId1,
                    Name = "Test Section 1",
                    SectionType = HomeSectionType.LatestMedia,
                    Priority = 1,
                    MaxItems = 10,
                    SortOrder = SortOrder.Descending,
                    SortBy = (int)SortOrder.Ascending
                },
                new UserHomeSection
                {
                    UserId = _userId,
                    SectionId = sectionId2,
                    Name = "Test Section 2",
                    SectionType = HomeSectionType.NextUp,
                    Priority = 2,
                    MaxItems = 5,
                    SortOrder = SortOrder.Ascending,
                    SortBy = (int)SortOrder.Descending
                },
                new UserHomeSection
                {
                    UserId = Guid.NewGuid(), // Different user
                    SectionId = Guid.NewGuid(),
                    Name = "Other User Section",
                    SectionType = HomeSectionType.LatestMedia,
                    Priority = 1,
                    MaxItems = 15,
                    SortOrder = SortOrder.Ascending,
                    SortBy = (int)SortOrder.Ascending
                }
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
            var sectionId = Guid.NewGuid();

            _homeSections.Add(new UserHomeSection
            {
                UserId = _userId,
                SectionId = sectionId,
                Name = "Test Section",
                SectionType = HomeSectionType.LatestMedia,
                Priority = 1,
                MaxItems = 10,
                SortOrder = SortOrder.Descending,
                SortBy = (int)SortOrder.Ascending
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
            // Arrange
            var sectionId = Guid.NewGuid();

            // Act
            var result = _manager.GetHomeSection(_userId, sectionId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void CreateHomeSection_AddsNewSectionToDatabase()
        {
            // Arrange
            var options = new HomeSectionOptions
            {
                Name = "New Section",
                SectionType = HomeSectionType.LatestMedia,
                Priority = 3,
                MaxItems = 15,
                SortOrder = SortOrder.Ascending,
                SortBy = SortOrder.Ascending
            };

            // Act
            var sectionId = _manager.CreateHomeSection(_userId, options);

            // Assert
            _mockDbSet.Verify(
                m => m.Add(
                    It.Is<UserHomeSection>(s =>
                        s.UserId.Equals(_userId) &&
                        s.SectionId.Equals(sectionId) &&
                        s.Name == "New Section" &&
                        s.SectionType == HomeSectionType.LatestMedia &&
                        s.Priority == 3 &&
                        s.MaxItems == 15 &&
                        s.SortOrder == SortOrder.Ascending &&
                        s.SortBy == (int)SortOrder.Ascending)),
                Times.Once);
        }

        [Fact]
        public void UpdateHomeSection_WithValidId_UpdatesSection()
        {
            // Arrange
            var sectionId = Guid.NewGuid();

            _homeSections.Add(new UserHomeSection
            {
                UserId = _userId,
                SectionId = sectionId,
                Name = "Original Section",
                SectionType = HomeSectionType.LatestMedia,
                Priority = 1,
                MaxItems = 10,
                SortOrder = SortOrder.Descending,
                SortBy = (int)SortOrder.Ascending
            });

            var options = new HomeSectionOptions
            {
                Name = "Updated Section",
                SectionType = HomeSectionType.LatestMedia,
                Priority = 3,
                MaxItems = 15,
                SortOrder = SortOrder.Ascending,
                SortBy = SortOrder.Descending
            };

            // Act
            var result = _manager.UpdateHomeSection(_userId, sectionId, options);

            // Assert
            Assert.True(result);
            var section = _homeSections.First(s => s.SectionId.Equals(sectionId));
            Assert.Equal("Updated Section", section.Name);
            Assert.Equal(HomeSectionType.LatestMedia, section.SectionType);
            Assert.Equal(3, section.Priority);
            Assert.Equal(15, section.MaxItems);
            Assert.Equal(SortOrder.Ascending, section.SortOrder);
            Assert.Equal((int)SortOrder.Descending, section.SortBy);
        }

        [Fact]
        public void UpdateHomeSection_WithInvalidId_ReturnsFalse()
        {
            // Arrange
            var sectionId = Guid.NewGuid();

            var options = new HomeSectionOptions
            {
                Name = "Updated Section",
                SectionType = HomeSectionType.LatestMedia,
                Priority = 3,
                MaxItems = 15,
                SortOrder = SortOrder.Ascending,
                SortBy = SortOrder.Descending
            };

            // Act
            var result = _manager.UpdateHomeSection(_userId, sectionId, options);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void DeleteHomeSection_WithValidId_RemovesSection()
        {
            // Arrange
            var sectionId = Guid.NewGuid();

            var section = new UserHomeSection
            {
                UserId = _userId,
                SectionId = sectionId,
                Name = "Section to Delete",
                SectionType = HomeSectionType.LatestMedia,
                Priority = 1,
                MaxItems = 10,
                SortOrder = SortOrder.Descending,
                SortBy = (int)SortOrder.Ascending
            };

            _homeSections.Add(section);

            // Act
            var result = _manager.DeleteHomeSection(_userId, sectionId);

            // Assert
            Assert.True(result);
            _mockDbSet.Verify(m => m.Remove(section), Times.Once);
        }

        [Fact]
        public void DeleteHomeSection_WithInvalidId_ReturnsFalse()
        {
            // Arrange
            var sectionId = Guid.NewGuid();

            // Act
            var result = _manager.DeleteHomeSection(_userId, sectionId);

            // Assert
            Assert.False(result);
            _mockDbSet.Verify(m => m.Remove(It.IsAny<UserHomeSection>()), Times.Never);
        }

        [Fact]
        public void SaveChanges_CallsSaveChangesOnDbContext()
        {
            // Act
            _manager.SaveChanges();

            // Assert
            _mockDbContext.Verify(c => c.SaveChanges(), Times.Once);
        }

        private static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data)
            where T : class
        {
            var queryable = data.AsQueryable();
            var mockDbSet = new Mock<DbSet<T>>();

            mockDbSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            mockDbSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockDbSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockDbSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());

            mockDbSet.Setup(m => m.Add(It.IsAny<T>())).Callback<T>(data.Add);
            mockDbSet.Setup(m => m.Remove(It.IsAny<T>())).Callback<T>(item => data.Remove(item));

            return mockDbSet;
        }

        public async ValueTask DisposeAsync()
        {
            await _manager.DisposeAsync().ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }
    }
}
