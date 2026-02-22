using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Jellyfin.Api.Models.HomeSectionDto;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Model.Configuration;
using Xunit;

namespace Jellyfin.Api.Tests.Models.HomeSectionDto
{
    public class HomeSectionDtoTests
    {
        [Fact]
        public void HomeSectionDto_DefaultConstructor_InitializesProperties()
        {
            // Act
            var dto = new Jellyfin.Api.Models.HomeSectionDto.HomeSectionDto();

            // Assert
            Assert.Null(dto.Id);
            Assert.NotNull(dto.SectionOptions);
        }

        [Fact]
        public void HomeSectionDto_WithValues_StoresCorrectly()
        {
            // Arrange
            var id = Guid.NewGuid();
            var options = new HomeSectionOptions
            {
                Name = "Test Section",
                SectionType = HomeSectionType.LatestMedia,
                Priority = 1,
                MaxItems = 10,
                SortOrder = SortOrder.Descending,
                SortBy = SortOrder.Ascending
            };

            // Act
            var dto = new Jellyfin.Api.Models.HomeSectionDto.HomeSectionDto
            {
                Id = id,
                SectionOptions = options
            };

            // Assert
            Assert.Equal(id, dto.Id);
            Assert.Same(options, dto.SectionOptions);
            Assert.Equal("Test Section", dto.SectionOptions.Name);
            Assert.Equal(HomeSectionType.LatestMedia, dto.SectionOptions.SectionType);
            Assert.Equal(1, dto.SectionOptions.Priority);
            Assert.Equal(10, dto.SectionOptions.MaxItems);
            Assert.Equal(SortOrder.Descending, dto.SectionOptions.SortOrder);
            Assert.Equal(SortOrder.Ascending, dto.SectionOptions.SortBy);
        }

        [Fact]
        public void HomeSectionDto_SectionOptionsRequired_ValidationFails()
        {
            // Arrange
            var dto = new Jellyfin.Api.Models.HomeSectionDto.HomeSectionDto
            {
                Id = Guid.NewGuid(),
                SectionOptions = new HomeSectionOptions() // Use empty options instead of null
            };

            // Set SectionOptions to null for validation test
            // This is a workaround for non-nullable reference types
            var validationContext = new ValidationContext(dto);
            var validationResults = new List<ValidationResult>();

            // Use reflection to set the SectionOptions to null for validation testing
            var propertyInfo = dto.GetType().GetProperty("SectionOptions");
            propertyInfo?.SetValue(dto, null);

            // Act
            var isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.Single(validationResults);
            Assert.Contains("SectionOptions", validationResults[0].MemberNames);
        }

        [Fact]
        public void HomeSectionOptions_DefaultConstructor_InitializesProperties()
        {
            // Act
            var options = new HomeSectionOptions();

            // Assert
            Assert.Equal(string.Empty, options.Name);
            Assert.Equal(HomeSectionType.None, options.SectionType);
            Assert.Equal(0, options.Priority);
            Assert.Equal(10, options.MaxItems);
            Assert.Equal(SortOrder.Ascending, options.SortOrder);
            Assert.Equal(SortOrder.Ascending, options.SortBy);
        }

        [Fact]
        public void HomeSectionOptions_WithValues_StoresCorrectly()
        {
            // Act
            var options = new HomeSectionOptions
            {
                Name = "Custom Section",
                SectionType = HomeSectionType.LatestMedia,
                Priority = 5,
                MaxItems = 20,
                SortOrder = SortOrder.Descending,
                SortBy = SortOrder.Descending
            };

            // Assert
            Assert.Equal("Custom Section", options.Name);
            Assert.Equal(HomeSectionType.LatestMedia, options.SectionType);
            Assert.Equal(5, options.Priority);
            Assert.Equal(20, options.MaxItems);
            Assert.Equal(SortOrder.Descending, options.SortOrder);
            Assert.Equal(SortOrder.Descending, options.SortBy);
        }
    }
}
