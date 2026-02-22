using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Api.Controllers;
using Jellyfin.Api.Models.HomeSectionDto;
using Jellyfin.Api.Results;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers
{
    public class HomeSectionControllerTests
    {
        private readonly Mock<IHomeSectionManager> _mockHomeSectionManager;
        private readonly Mock<IUserManager> _mockUserManager;
        private readonly Mock<ILibraryManager> _mockLibraryManager;
        private readonly Mock<IDtoService> _mockDtoService;
        private readonly HomeSectionController _controller;
        private readonly Guid _userId = Guid.NewGuid();

        public HomeSectionControllerTests()
        {
            _mockHomeSectionManager = new Mock<IHomeSectionManager>();
            _mockUserManager = new Mock<IUserManager>();
            _mockLibraryManager = new Mock<ILibraryManager>();
            _mockDtoService = new Mock<IDtoService>();

            _controller = new HomeSectionController(
                _mockHomeSectionManager.Object,
                _mockUserManager.Object,
                _mockLibraryManager.Object,
                _mockDtoService.Object);

            // Setup user manager to return a non-null user for the test user ID
            var mockUser = new Mock<User>();
            _mockUserManager.Setup(m => m.GetUserById(_userId))
                .Returns(mockUser.Object);
        }

        [Fact]
        public void GetHomeSections_ReturnsOkResult_WithListOfSections()
        {
            // Arrange
            var sections = new List<HomeSectionOptions>
            {
                new HomeSectionOptions
                {
                    Name = "Test Section 1",
                    SectionType = HomeSectionType.LatestMedia,
                    Priority = 1,
                    MaxItems = 10,
                    SortOrder = SortOrder.Descending,
                    SortBy = SortOrder.Ascending
                },
                new HomeSectionOptions
                {
                    Name = "Test Section 2",
                    SectionType = HomeSectionType.NextUp,
                    Priority = 2,
                    MaxItems = 5,
                    SortOrder = SortOrder.Ascending,
                    SortBy = SortOrder.Descending
                }
            };

            _mockHomeSectionManager.Setup(m => m.GetHomeSections(_userId))
                .Returns(sections);

            // Act
            var result = _controller.GetHomeSections(_userId);

            // Assert
            var okResult = Assert.IsAssignableFrom<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<List<EnrichedHomeSectionDto>>(okResult.Value);
            Assert.Equal(2, returnValue.Count);
            Assert.Equal("Test Section 1", returnValue[0].SectionOptions.Name);
            Assert.Equal("Test Section 2", returnValue[1].SectionOptions.Name);
        }

        [Fact]
        public void GetHomeSection_WithValidId_ReturnsOkResult()
        {
            // Arrange
            var sectionId = Guid.NewGuid();
            var section = new HomeSectionOptions
            {
                Name = "Test Section",
                SectionType = HomeSectionType.LatestMedia,
                Priority = 1,
                MaxItems = 10,
                SortOrder = SortOrder.Descending,
                SortBy = SortOrder.Ascending
            };

            _mockHomeSectionManager.Setup(m => m.GetHomeSection(_userId, sectionId))
                .Returns(section);

            // Act
            var result = _controller.GetHomeSection(_userId, sectionId);

            // Assert
            var okResult = Assert.IsAssignableFrom<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<EnrichedHomeSectionDto>(okResult.Value);
            Assert.Equal("Test Section", returnValue.SectionOptions.Name);
            Assert.Equal(sectionId, returnValue.Id);
        }

        [Fact]
        public void GetHomeSection_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var sectionId = Guid.NewGuid();
            _mockHomeSectionManager.Setup(m => m.GetHomeSection(_userId, sectionId))
                .Returns((HomeSectionOptions?)null);

            // Act
            var result = _controller.GetHomeSection(_userId, sectionId);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public void CreateHomeSection_ReturnsCreatedAtAction()
        {
            // Arrange
            var sectionId = Guid.NewGuid();
            var dto = new HomeSectionDto
            {
                SectionOptions = new HomeSectionOptions
                {
                    Name = "New Section",
                    SectionType = HomeSectionType.LatestMedia,
                    Priority = 3,
                    MaxItems = 15,
                    SortOrder = SortOrder.Ascending,
                    SortBy = SortOrder.Ascending
                }
            };

            _mockHomeSectionManager.Setup(m => m.CreateHomeSection(_userId, dto.SectionOptions))
                .Returns(sectionId);

            // Act
            var result = _controller.CreateHomeSection(_userId, dto);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnValue = Assert.IsType<HomeSectionDto>(createdAtActionResult.Value);
            Assert.Equal("New Section", returnValue.SectionOptions.Name);
            Assert.Equal(sectionId, returnValue.Id);
            Assert.Equal("GetHomeSection", createdAtActionResult.ActionName);

            // Check if RouteValues is not null before accessing its elements
            Assert.NotNull(createdAtActionResult.RouteValues);
            if (createdAtActionResult.RouteValues != null)
            {
                Assert.Equal(_userId, createdAtActionResult.RouteValues["userId"]);
                Assert.Equal(sectionId, createdAtActionResult.RouteValues["sectionId"]);
            }

            _mockHomeSectionManager.Verify(m => m.SaveChanges(), Times.Once);
        }

        [Fact]
        public void UpdateHomeSection_WithValidId_ReturnsNoContent()
        {
            // Arrange
            var sectionId = Guid.NewGuid();
            var dto = new HomeSectionDto
            {
                SectionOptions = new HomeSectionOptions
                {
                    Name = "Updated Section",
                    SectionType = HomeSectionType.LatestMedia,
                    Priority = 3,
                    MaxItems = 15,
                    SortOrder = SortOrder.Ascending,
                    SortBy = SortOrder.Ascending
                }
            };

            _mockHomeSectionManager.Setup(m => m.UpdateHomeSection(_userId, sectionId, dto.SectionOptions))
                .Returns(true);

            // Act
            var result = _controller.UpdateHomeSection(_userId, sectionId, dto);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockHomeSectionManager.Verify(m => m.SaveChanges(), Times.Once);
        }

        [Fact]
        public void UpdateHomeSection_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var sectionId = Guid.NewGuid();
            var dto = new HomeSectionDto
            {
                SectionOptions = new HomeSectionOptions
                {
                    Name = "Updated Section",
                    SectionType = HomeSectionType.LatestMedia,
                    Priority = 3,
                    MaxItems = 15,
                    SortOrder = SortOrder.Ascending,
                    SortBy = SortOrder.Ascending
                }
            };

            _mockHomeSectionManager.Setup(m => m.UpdateHomeSection(_userId, sectionId, dto.SectionOptions))
                .Returns(false);

            // Act
            var result = _controller.UpdateHomeSection(_userId, sectionId, dto);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockHomeSectionManager.Verify(m => m.SaveChanges(), Times.Never);
        }

        [Fact]
        public void DeleteHomeSection_WithValidId_ReturnsNoContent()
        {
            // Arrange
            var sectionId = Guid.NewGuid();
            _mockHomeSectionManager.Setup(m => m.DeleteHomeSection(_userId, sectionId))
                .Returns(true);

            // Act
            var result = _controller.DeleteHomeSection(_userId, sectionId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockHomeSectionManager.Verify(m => m.SaveChanges(), Times.Once);
        }

        [Fact]
        public void DeleteHomeSection_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var sectionId = Guid.NewGuid();
            _mockHomeSectionManager.Setup(m => m.DeleteHomeSection(_userId, sectionId))
                .Returns(false);

            // Act
            var result = _controller.DeleteHomeSection(_userId, sectionId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockHomeSectionManager.Verify(m => m.SaveChanges(), Times.Never);
        }
    }
}
