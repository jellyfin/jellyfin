using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Jellyfin.Api.Models.HomeSectionDto;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Model.Configuration;
using Xunit;

namespace Jellyfin.Api.Tests.Integration
{
    /// <summary>
    /// Integration tests for the Home Section API.
    /// These tests require a running Jellyfin server and should be run in a controlled environment.
    /// </summary>
    public sealed class HomeSectionApiTests : IDisposable
    {
        private readonly HttpClient _client;
        private readonly Guid _userId = Guid.Parse("38a9e9be-b2a6-4790-85a3-62a01ca06dec"); // Test user ID
        private readonly List<Guid> _createdSectionIds = new List<Guid>();

        public HomeSectionApiTests()
        {
            // Setup HttpClient with base address pointing to your test server
            _client = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:8096/")
            };
        }

        [Fact(Skip = "Integration test - requires running server")]
        public async Task GetHomeSections_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync($"Users/{_userId}/HomeSections");

            // Assert
            response.EnsureSuccessStatusCode();
            var sections = await response.Content.ReadFromJsonAsync<List<HomeSectionDto>>();
            Assert.NotNull(sections);
        }

        [Fact(Skip = "Integration test - requires running server")]
        public async Task CreateAndGetHomeSection_ReturnsCreatedSection()
        {
            // Arrange
            var newSection = new HomeSectionDto
            {
                SectionOptions = new HomeSectionOptions
                {
                    Name = "Integration Test Section",
                    SectionType = HomeSectionType.LatestMedia,
                    Priority = 100,
                    MaxItems = 8,
                    SortOrder = SortOrder.Descending,
                    SortBy = SortOrder.Ascending
                }
            };

            // Act - Create
            var createResponse = await _client.PostAsJsonAsync($"Users/{_userId}/HomeSections", newSection);

            // Assert - Create
            createResponse.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

            var createdSection = await createResponse.Content.ReadFromJsonAsync<HomeSectionDto>();
            Assert.NotNull(createdSection);
            Assert.NotNull(createdSection.Id);
            _createdSectionIds.Add(createdSection.Id.Value);

            // Act - Get
            var getResponse = await _client.GetAsync($"Users/{_userId}/HomeSections/{createdSection.Id}");

            // Assert - Get
            getResponse.EnsureSuccessStatusCode();
            var retrievedSection = await getResponse.Content.ReadFromJsonAsync<HomeSectionDto>();

            Assert.NotNull(retrievedSection);
            Assert.Equal(createdSection.Id, retrievedSection.Id);
            Assert.Equal("Integration Test Section", retrievedSection.SectionOptions.Name);
            Assert.Equal(HomeSectionType.LatestMedia, retrievedSection.SectionOptions.SectionType);
            Assert.Equal(100, retrievedSection.SectionOptions.Priority);
            Assert.Equal(8, retrievedSection.SectionOptions.MaxItems);
            Assert.Equal(SortOrder.Descending, retrievedSection.SectionOptions.SortOrder);
            Assert.Equal(SortOrder.Ascending, retrievedSection.SectionOptions.SortBy);
        }

        [Fact(Skip = "Integration test - requires running server")]
        public async Task UpdateHomeSection_ReturnsNoContent()
        {
            // Arrange - Create a section first
            var newSection = new HomeSectionDto
            {
                SectionOptions = new HomeSectionOptions
                {
                    Name = "Section To Update",
                    SectionType = HomeSectionType.NextUp,
                    Priority = 50,
                    MaxItems = 5,
                    SortOrder = SortOrder.Ascending,
                    SortBy = SortOrder.Ascending
                }
            };

            var createResponse = await _client.PostAsJsonAsync($"Users/{_userId}/HomeSections", newSection);
            createResponse.EnsureSuccessStatusCode();
            var createdSection = await createResponse.Content.ReadFromJsonAsync<HomeSectionDto>();
            Assert.NotNull(createdSection);
            Assert.NotNull(createdSection.Id);
            _createdSectionIds.Add(createdSection.Id.Value);

            // Arrange - Update data
            var updateSection = new HomeSectionDto
            {
                SectionOptions = new HomeSectionOptions
                {
                    Name = "Updated Section Name",
                    SectionType = HomeSectionType.LatestMedia,
                    Priority = 25,
                    MaxItems = 12,
                    SortOrder = SortOrder.Descending,
                    SortBy = SortOrder.Descending
                }
            };

            // Act
            var updateResponse = await _client.PutAsJsonAsync($"Users/{_userId}/HomeSections/{createdSection.Id}", updateSection);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

            // Verify the update
            var getResponse = await _client.GetAsync($"Users/{_userId}/HomeSections/{createdSection.Id}");
            getResponse.EnsureSuccessStatusCode();
            var retrievedSection = await getResponse.Content.ReadFromJsonAsync<HomeSectionDto>();

            Assert.NotNull(retrievedSection);
            Assert.Equal("Updated Section Name", retrievedSection.SectionOptions.Name);
            Assert.Equal(HomeSectionType.LatestMedia, retrievedSection.SectionOptions.SectionType);
            Assert.Equal(25, retrievedSection.SectionOptions.Priority);
            Assert.Equal(12, retrievedSection.SectionOptions.MaxItems);
            Assert.Equal(SortOrder.Descending, retrievedSection.SectionOptions.SortOrder);
            Assert.Equal(SortOrder.Descending, retrievedSection.SectionOptions.SortBy);
        }

        [Fact(Skip = "Integration test - requires running server")]
        public async Task DeleteHomeSection_ReturnsNoContent()
        {
            // Arrange - Create a section first
            var newSection = new HomeSectionDto
            {
                SectionOptions = new HomeSectionOptions
                {
                    Name = "Section To Delete",
                    SectionType = HomeSectionType.LatestMedia,
                    Priority = 75,
                    MaxItems = 3,
                    SortOrder = SortOrder.Ascending,
                    SortBy = SortOrder.Descending
                }
            };

            var createResponse = await _client.PostAsJsonAsync($"Users/{_userId}/HomeSections", newSection);
            createResponse.EnsureSuccessStatusCode();
            var createdSection = await createResponse.Content.ReadFromJsonAsync<HomeSectionDto>();
            Assert.NotNull(createdSection);
            Assert.NotNull(createdSection.Id);

            // Act
            var deleteResponse = await _client.DeleteAsync($"Users/{_userId}/HomeSections/{createdSection.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            // Verify it's gone
            var getResponse = await _client.GetAsync($"Users/{_userId}/HomeSections/{createdSection.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        public void Dispose()
        {
            // Clean up any sections created during tests
            foreach (var sectionId in _createdSectionIds)
            {
                try
                {
                    _client.DeleteAsync($"Users/{_userId}/HomeSections/{sectionId}").Wait();
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }

            _client.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
