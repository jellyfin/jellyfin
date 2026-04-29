using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Manager;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.Manager
{
    public class MetadataServiceTests
    {
        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void MergeBaseItemData_MergeMetadataSettings_MergesWhenSet(bool mergeMetadataSettings, bool defaultDate)
        {
            var newLocked = new[] { MetadataField.Genres, MetadataField.Cast };
            var newString = "new";
            var newDate = DateTime.UtcNow;

            var oldLocked = new[] { MetadataField.Genres };
            var oldString = "old";
            var oldDate = DateTime.UnixEpoch;

            var source = new MetadataResult<Movie>
            {
                Item = new Movie
                {
                    LockedFields = newLocked,
                    IsLocked = true,
                    PreferredMetadataCountryCode = newString,
                    PreferredMetadataLanguage = newString,
                    DateCreated = newDate
                }
            };

            if (defaultDate)
            {
                source.Item.DateCreated = default;
            }

            var target = new MetadataResult<Movie>
            {
                Item = new Movie
                {
                    LockedFields = oldLocked,
                    IsLocked = false,
                    PreferredMetadataCountryCode = oldString,
                    PreferredMetadataLanguage = oldString,
                    DateCreated = oldDate
                }
            };

            MetadataService<Movie, MovieInfo>.MergeBaseItemData(source, target, Array.Empty<MetadataField>(), true, mergeMetadataSettings);

            if (mergeMetadataSettings)
            {
                Assert.Equal(newLocked, target.Item.LockedFields);
                Assert.True(target.Item.IsLocked);
                Assert.Equal(newString, target.Item.PreferredMetadataCountryCode);
                Assert.Equal(newString, target.Item.PreferredMetadataLanguage);
                Assert.Equal(defaultDate ? oldDate : newDate, target.Item.DateCreated);
            }
            else
            {
                Assert.Equal(oldLocked, target.Item.LockedFields);
                Assert.False(target.Item.IsLocked);
                Assert.Equal(oldString, target.Item.PreferredMetadataCountryCode);
                Assert.Equal(oldString, target.Item.PreferredMetadataLanguage);
                Assert.Equal(oldDate, target.Item.DateCreated);
            }
        }

        [Fact]
        public void MergeBaseItemData_ReloadMetadata_OrphanedSource_DoesNotOverwriteEditedFields()
        {
            var target = new MetadataResult<Movie>
            {
                Item = new Movie
                {
                    Name = "Edited name",
                    OfficialRating = "TV-14",
                    CustomRating = "Edited custom",
                    Overview = "Edited overview",
                    ProductionYear = 2024,
                    CommunityRating = 8.5f,
                    CriticRating = 88f,
                    Genres = new[] { "Drama" },
                    Studios = new[] { "Edited Studio" },
                    Tags = new[] { "Edited Tag" },
                    ProductionLocations = new[] { "PT" },
                    ForcedSortName = "Edited sort"
                }
            };

            // Simulate manual edits already stored in library item.
            var expectedName = target.Item.Name;
            var expectedOfficialRating = target.Item.OfficialRating;
            var expectedCustomRating = target.Item.CustomRating;
            var expectedOverview = target.Item.Overview;
            var expectedProductionYear = target.Item.ProductionYear;
            var expectedCommunityRating = target.Item.CommunityRating;
            var expectedCriticRating = target.Item.CriticRating;
            var expectedGenres = target.Item.Genres.ToArray();
            var expectedStudios = target.Item.Studios.ToArray();
            var expectedTags = target.Item.Tags.ToArray();
            var expectedProductionLocations = target.Item.ProductionLocations.ToArray();
            var expectedForcedSortName = target.Item.ForcedSortName;

            var source = new MetadataResult<Movie>
            {
                // Simulate reload returning no content metadata and no provider ids (orphan).
                Item = new Movie()
            };

            MetadataService<Movie, MovieInfo>.MergeBaseItemData(source, target, Array.Empty<MetadataField>(), true, true);

            Assert.Equal(expectedName, target.Item.Name);
            Assert.Equal(expectedOfficialRating, target.Item.OfficialRating);
            Assert.Equal(expectedCustomRating, target.Item.CustomRating);
            Assert.Equal(expectedOverview, target.Item.Overview);
            Assert.Equal(expectedProductionYear, target.Item.ProductionYear);
            Assert.Equal(expectedCommunityRating, target.Item.CommunityRating);
            Assert.Equal(expectedCriticRating, target.Item.CriticRating);
            Assert.Equal(expectedGenres, target.Item.Genres);
            Assert.Equal(expectedStudios, target.Item.Studios);
            Assert.Equal(expectedTags, target.Item.Tags);
            Assert.Equal(expectedProductionLocations, target.Item.ProductionLocations);
            Assert.Equal(expectedForcedSortName, target.Item.ForcedSortName);
        }

        [Fact]
        public void MergeBaseItemData_ReloadMetadata_EmptyProviderData_DoesNotOverwriteEditedRatings()
        {
            var target = new MetadataResult<Movie>
            {
                Item = new Movie
                {
                    OfficialRating = "PG-13",
                    CustomRating = "Edited custom",
                    ProductionYear = 2022,
                    CriticRating = 75f,
                    CommunityRating = 7.4f
                }
            };

            // Source has provider ids so it is not orphaned, but content fields are empty/default.
            var source = new MetadataResult<Movie>
            {
                Item = new Movie
                {
                    OfficialRating = string.Empty,
                    CustomRating = string.Empty,
                    ProductionYear = 0,
                    CriticRating = null,
                    CommunityRating = null
                }
            };
            source.Item.ProviderIds["tmdb"] = "12345";

            MetadataService<Movie, MovieInfo>.MergeBaseItemData(source, target, Array.Empty<MetadataField>(), true, false);

            Assert.Equal("PG-13", target.Item.OfficialRating);
            Assert.Equal("Edited custom", target.Item.CustomRating);
            Assert.Equal(2022, target.Item.ProductionYear);
            Assert.Equal(75f, target.Item.CriticRating);
            Assert.Equal(7.4f, target.Item.CommunityRating);
        }

        [Fact]
        public void MergeBaseItemData_ReloadMetadata_ReplaceAllWithSettings_PreservesEditedContentFields()
        {
            var target = new MetadataResult<Movie>
            {
                Item = new Movie
                {
                    Name = "Edited name",
                    OfficialRating = "TV-MA",
                    CustomRating = "Edited custom",
                    Overview = "Edited overview",
                    Genres = new[] { "Drama" },
                    Studios = new[] { "Edited Studio" },
                    Tags = new[] { "Edited Tag" },
                    ProductionLocations = new[] { "PT" },
                    IsLocked = false,
                    PreferredMetadataCountryCode = "pt",
                    PreferredMetadataLanguage = "pt-PT"
                }
            };

            var source = new MetadataResult<Movie>
            {
                // Simulates refresh result where provider id exists but field payload is empty/default.
                Item = new Movie
                {
                    Name = string.Empty,
                    OfficialRating = string.Empty,
                    CustomRating = string.Empty,
                    Overview = string.Empty,
                    Genres = Array.Empty<string>(),
                    Studios = Array.Empty<string>(),
                    Tags = Array.Empty<string>(),
                    ProductionLocations = Array.Empty<string>(),
                    IsLocked = true,
                    PreferredMetadataCountryCode = "en",
                    PreferredMetadataLanguage = "en-US"
                }
            };
            source.Item.ProviderIds["tmdb"] = "12345";

            // Runtime-equivalent merge configuration for full replace plus metadata settings update.
            MetadataService<Movie, MovieInfo>.MergeBaseItemData(source, target, Array.Empty<MetadataField>(), true, true);

            Assert.Equal("Edited name", target.Item.Name);
            Assert.Equal("TV-MA", target.Item.OfficialRating);
            Assert.Equal("Edited custom", target.Item.CustomRating);
            Assert.Equal("Edited overview", target.Item.Overview);
            Assert.Equal(new[] { "Drama" }, target.Item.Genres);
            Assert.Equal(new[] { "Edited Studio" }, target.Item.Studios);
            Assert.Equal(new[] { "Edited Tag" }, target.Item.Tags);
            Assert.Equal(new[] { "PT" }, target.Item.ProductionLocations);

            // Metadata settings should still follow the source in this mode.
            Assert.True(target.Item.IsLocked);
            Assert.Equal("en", target.Item.PreferredMetadataCountryCode);
            Assert.Equal("en-US", target.Item.PreferredMetadataLanguage);
        }

        [Fact]
        public async Task RefreshWithProviders_ReplaceAllRemoveOld_EmptyLocalMetadataWithProviderId_DoesNotOverwriteEditedFields()
        {
            var service = CreateTestService();

            var metadata = new MetadataResult<Movie>
            {
                Item = new Movie
                {
                    Name = "Edited name",
                    OfficialRating = "PG-13",
                    CustomRating = "Edited custom",
                    Overview = "Edited overview",
                    Genres = new[] { "Drama" },
                    ProductionYear = 2022
                }
            };

            var localProviderResult = new MetadataResult<Movie>
            {
                Item = new Movie
                {
                    Name = string.Empty,
                    OfficialRating = string.Empty,
                    CustomRating = string.Empty,
                    Overview = string.Empty,
                    Genres = Array.Empty<string>(),
                    ProductionYear = 0
                },
                HasMetadata = true
            };
            localProviderResult.Item.ProviderIds["tmdb"] = "123";

            var localProvider = new Mock<ILocalMetadataProvider<Movie>>(MockBehavior.Strict);
            localProvider.SetupGet(p => p.Name).Returns("local-test");
            localProvider
                .Setup(p => p.GetMetadata(It.IsAny<ItemInfo>(), It.IsAny<IDirectoryService>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(localProviderResult);

            var options = new MetadataRefreshOptions(Mock.Of<IDirectoryService>())
            {
                MetadataRefreshMode = MetadataRefreshMode.FullRefresh,
                ReplaceAllMetadata = true,
                RemoveOldMetadata = true
            };

            var id = new MovieInfo();
            id.ProviderIds["tmdb"] = "123";

            var result = await service.RunRefreshWithProviders(
                metadata,
                id,
                options,
                new List<IMetadataProvider> { localProvider.Object },
                false,
                CancellationToken.None);

            Assert.True((result.UpdateType & ItemUpdateType.MetadataImport) > ItemUpdateType.None);
            Assert.Equal("Edited name", metadata.Item.Name);
            Assert.Equal("PG-13", metadata.Item.OfficialRating);
            Assert.Equal("Edited custom", metadata.Item.CustomRating);
            Assert.Equal("Edited overview", metadata.Item.Overview);
            Assert.Equal(new[] { "Drama" }, metadata.Item.Genres);
            Assert.Equal(2022, metadata.Item.ProductionYear);
        }

        [Fact]
        public async Task RefreshWithProviders_ReplaceAllRemoveOld_EmptyLocalMetadataWithoutProviderId_PreservesEditedFields()
        {
            var service = CreateTestService();

            var metadata = new MetadataResult<Movie>
            {
                Item = new Movie
                {
                    OfficialRating = "TV-14",
                    Overview = "Edited overview",
                    Genres = new[] { "Sci-Fi" }
                }
            };

            var localProviderResult = new MetadataResult<Movie>
            {
                Item = new Movie
                {
                    OfficialRating = string.Empty,
                    Overview = string.Empty,
                    Genres = Array.Empty<string>()
                },
                HasMetadata = true
            };

            var localProvider = new Mock<ILocalMetadataProvider<Movie>>(MockBehavior.Strict);
            localProvider.SetupGet(p => p.Name).Returns("local-test-no-id");
            localProvider
                .Setup(p => p.GetMetadata(It.IsAny<ItemInfo>(), It.IsAny<IDirectoryService>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(localProviderResult);

            var options = new MetadataRefreshOptions(Mock.Of<IDirectoryService>())
            {
                MetadataRefreshMode = MetadataRefreshMode.FullRefresh,
                ReplaceAllMetadata = true,
                RemoveOldMetadata = true
            };

            var result = await service.RunRefreshWithProviders(
                metadata,
                new MovieInfo(),
                options,
                new List<IMetadataProvider> { localProvider.Object },
                false,
                CancellationToken.None);

            Assert.True((result.UpdateType & ItemUpdateType.MetadataImport) > ItemUpdateType.None);
            Assert.Equal("TV-14", metadata.Item.OfficialRating);
            Assert.Equal("Edited overview", metadata.Item.Overview);
            Assert.Equal(new[] { "Sci-Fi" }, metadata.Item.Genres);
        }

        [Fact]
        public async Task RefreshWithProviders_NoExternalMetadata_CustomProviderChanges_AreRevertedForMenuFields()
        {
            var service = CreateTestService();

            var metadata = new MetadataResult<Movie>
            {
                Item = new Movie
                {
                    Name = "Edited name",
                    Overview = "Edited overview",
                    OfficialRating = "TV-14",
                    Genres = new[] { "Drama" },
                    Studios = new[] { "Edited Studio" },
                    Tags = new[] { "Edited Tag" },
                    ProductionLocations = new[] { "PT" }
                }
            };

            var customProvider = new Mock<ICustomMetadataProvider<Movie>>(MockBehavior.Strict);
            customProvider.SetupGet(p => p.Name).Returns("custom-no-remote");
            customProvider
                .Setup(p => p.FetchAsync(It.IsAny<Movie>(), It.IsAny<MetadataRefreshOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Movie item, MetadataRefreshOptions _, CancellationToken _) =>
                {
                    item.Name = "Changed name";
                    item.Overview = "Changed overview";
                    item.OfficialRating = "R";
                    item.Genres = new[] { "Action" };
                    item.Studios = new[] { "Changed Studio" };
                    item.Tags = new[] { "Changed Tag" };
                    item.ProductionLocations = new[] { "US" };
                    return ItemUpdateType.MetadataImport;
                });

            var options = new MetadataRefreshOptions(Mock.Of<IDirectoryService>())
            {
                MetadataRefreshMode = MetadataRefreshMode.FullRefresh,
                ReplaceAllMetadata = true,
                RemoveOldMetadata = true
            };

            var result = await service.RunRefreshWithProviders(
                metadata,
                new MovieInfo(),
                options,
                new List<IMetadataProvider> { customProvider.Object },
                false,
                CancellationToken.None);

            Assert.True((result.UpdateType & ItemUpdateType.MetadataImport) > ItemUpdateType.None);
            Assert.Equal("Edited name", metadata.Item.Name);
            Assert.Equal("Edited overview", metadata.Item.Overview);
            Assert.Equal("TV-14", metadata.Item.OfficialRating);
            Assert.Equal(new[] { "Drama" }, metadata.Item.Genres);
            Assert.Equal(new[] { "Edited Studio" }, metadata.Item.Studios);
            Assert.Equal(new[] { "Edited Tag" }, metadata.Item.Tags);
            Assert.Equal(new[] { "PT" }, metadata.Item.ProductionLocations);
        }

        [Theory]
        [InlineData("Name", MetadataField.Name, false)]
        [InlineData("OriginalTitle", null, false)]
        [InlineData("OfficialRating", MetadataField.OfficialRating, false)]
        [InlineData("CustomRating", null, false)]
        [InlineData("Tagline", null, false)]
        [InlineData("Overview", MetadataField.Overview, false)]
        [InlineData("DisplayOrder", null, false)]
        [InlineData("ForcedSortName", null, false)]
        public void MergeBaseItemData_StringField_ReplacesAppropriately(string propName, MetadataField? lockField = null, bool replacesWithEmpty = true)
        {
            var oldValue = "Old";
            var newValue = "New";

            // Use type Series to hit DisplayOrder
            Assert.False(TestMergeBaseItemData<Series, SeriesInfo>(propName, oldValue, newValue, null, false, out _));
            if (lockField is not null)
            {
                Assert.False(TestMergeBaseItemData<Series, SeriesInfo>(propName, oldValue, newValue, lockField, true, out _));
                Assert.False(TestMergeBaseItemData<Series, SeriesInfo>(propName, null, newValue, lockField, false, out _));
                Assert.False(TestMergeBaseItemData<Series, SeriesInfo>(propName, string.Empty, newValue, lockField, false, out _));
            }

            Assert.True(TestMergeBaseItemData<Series, SeriesInfo>(propName, oldValue, newValue, null, true, out _));
            Assert.True(TestMergeBaseItemData<Series, SeriesInfo>(propName, null, newValue, null, false, out _));
            Assert.True(TestMergeBaseItemData<Series, SeriesInfo>(propName, string.Empty, newValue, null, false, out _));

            var replacedWithEmpty = TestMergeBaseItemData<Series, SeriesInfo>(propName, oldValue, string.Empty, null, true, out _);
            Assert.Equal(replacesWithEmpty, replacedWithEmpty);
        }

        [Theory]
        [InlineData("Genres", MetadataField.Genres)]
        [InlineData("Studios", MetadataField.Studios)]
        [InlineData("Tags", MetadataField.Tags)]
        [InlineData("ProductionLocations", MetadataField.ProductionLocations)]
        [InlineData("AlbumArtists")]
        public void MergeBaseItemData_StringArrayField_ReplacesAppropriately(string propName, MetadataField? lockField = null)
        {
            // Note that arrays are replaced, not merged
            var oldValue = new[] { "Old" };
            var newValue = new[] { "New" };

            // Use type Audio to hit AlbumArtists
            Assert.False(TestMergeBaseItemData<Audio, SongInfo>(propName, oldValue, newValue, null, false, out _));
            if (lockField is not null)
            {
                Assert.False(TestMergeBaseItemData<Audio, SongInfo>(propName, oldValue, newValue, lockField, true, out _));
                Assert.False(TestMergeBaseItemData<Audio, SongInfo>(propName, Array.Empty<string>(), newValue, lockField, false, out _));
            }

            Assert.True(TestMergeBaseItemData<Audio, SongInfo>(propName, oldValue, newValue, null, true, out _));
            Assert.True(TestMergeBaseItemData<Audio, SongInfo>(propName, Array.Empty<string>(), newValue, null, false, out _));

            Assert.False(TestMergeBaseItemData<Audio, SongInfo>(propName, oldValue, Array.Empty<string>(), null, true, out _));
        }

        public static TheoryData<string, object, object> MergeBaseItemData_SimpleField_ReplacesAppropriately_TestData()
            => new()
            {
                { "IndexNumber", 1, 2 },
                { "ParentIndexNumber", 1, 2 },
                { "ProductionYear", 1, 2 },
                { "CommunityRating", 1.0f, 2.0f },
                { "CriticRating", 1.0f, 2.0f },
                { "EndDate", DateTime.UnixEpoch, DateTime.UtcNow },
                { "PremiereDate", DateTime.UnixEpoch, DateTime.UtcNow },
                { "Video3DFormat", Video3DFormat.HalfSideBySide, Video3DFormat.FullSideBySide }
            };

        [Theory]
        [MemberData(nameof(MergeBaseItemData_SimpleField_ReplacesAppropriately_TestData))]
        public void MergeBaseItemData_SimpleField_ReplacesAppropriately(string propName, object oldValue, object newValue)
        {
            // Use type Movie to allow testing of Video3DFormat
            Assert.False(TestMergeBaseItemData<Movie, MovieInfo>(propName, oldValue, newValue, null, false, out _));

            Assert.True(TestMergeBaseItemData<Movie, MovieInfo>(propName, oldValue, newValue, null, true, out _));
            Assert.True(TestMergeBaseItemData<Movie, MovieInfo>(propName, null, newValue, null, false, out _));

            Assert.False(
                TestMergeBaseItemData<Movie, MovieInfo>(propName, oldValue, null, null, true, out _));
        }

        [Fact]
        public void MergeBaseItemData_MergeTrailers_ReplacesAppropriately()
        {
            string propName = "RemoteTrailers";
            var oldValue = new[]
            {
                new MediaUrl
                {
                    Name = "Name 1",
                    Url = "URL 1"
                }
            };
            var newValue = new[]
            {
                new MediaUrl
                {
                    Name = "Name 2",
                    Url = "URL 2"
                }
            };

            Assert.False(TestMergeBaseItemData<Movie, MovieInfo>(propName, oldValue, newValue, null, false, out _));

            Assert.True(TestMergeBaseItemData<Movie, MovieInfo>(propName, oldValue, newValue, null, true, out _));
            Assert.True(TestMergeBaseItemData<Movie, MovieInfo>(propName, Array.Empty<MediaUrl>(), newValue, null, false, out _));

            Assert.False(TestMergeBaseItemData<Movie, MovieInfo>(propName, oldValue, Array.Empty<MediaUrl>(), null, true, out _));
        }

        [Fact]
        public void MergeBaseItemData_ProviderIds_MergesAppropriately()
        {
            var propName = "ProviderIds";
            var oldValue = new Dictionary<string, string>
            {
                { "provider 1", "id 1" }
            };

            // overwrite provider id
            var overwriteNewValue = new Dictionary<string, string>
            {
                { "provider 1", "id 2" }
            };
            Assert.False(TestMergeBaseItemData<Movie, MovieInfo>(propName, new Dictionary<string, string>(oldValue), overwriteNewValue, null, false, out _));
            TestMergeBaseItemData<Movie, MovieInfo>(propName, new Dictionary<string, string>(oldValue), overwriteNewValue, null, true, out var overwritten);
            Assert.Equal(overwriteNewValue, overwritten);

            // merge without overwriting
            var mergeNewValue = new Dictionary<string, string>
            {
                { "provider 1", "id 2" },
                { "provider 2", "id 3" }
            };
            TestMergeBaseItemData<Movie, MovieInfo>(propName, new Dictionary<string, string>(oldValue), mergeNewValue, null, false, out var merged);
            var actual = (Dictionary<string, string>)merged!;
            Assert.Equal("id 1", actual["provider 1"]);
            Assert.Equal("id 3", actual["provider 2"]);

            // empty source results in no change
            TestMergeBaseItemData<Movie, MovieInfo>(propName, new Dictionary<string, string>(oldValue), new Dictionary<string, string>(), null, true, out var notOverwritten);
            Assert.Equal(oldValue, notOverwritten);
        }

        [Fact]
        public void MergeBaseItemData_MergePeople_MergesAppropriately()
        {
            // PersonInfo in list is changed by merge, create new for every call
            List<PersonInfo> GetOldValue()
                => new()
                {
                    new PersonInfo
                    {
                        Name = "Name 1",
                        ProviderIds = new Dictionary<string, string>
                        {
                            { "Provider 1", "1234" }
                        }
                    }
                };

            // overwrite provider id
            var overwriteNewValue = new List<PersonInfo>
            {
                new()
                {
                    Name = "Name 2"
                }
            };
            Assert.False(TestMergeBaseItemDataPerson(GetOldValue(), overwriteNewValue, null, false, out var result));
            // People not already in target are not merged into it from source
            List<PersonInfo> actual = (List<PersonInfo>)result!;
            Assert.Single(actual);
            Assert.Equal("Name 1", actual[0].Name);

            Assert.True(TestMergeBaseItemDataPerson(GetOldValue(), overwriteNewValue, null, true, out _));
            Assert.True(TestMergeBaseItemDataPerson(new List<PersonInfo>(), overwriteNewValue, null, false, out _));
            Assert.True(TestMergeBaseItemDataPerson(null, overwriteNewValue, null, false, out _));

            Assert.False(TestMergeBaseItemDataPerson(GetOldValue(), overwriteNewValue, MetadataField.Cast, true, out _));

            // providers merge but don't overwrite existing keys
            var mergeNewValue = new List<PersonInfo>
            {
                new()
                {
                    Name = "Name 1",
                    ProviderIds = new Dictionary<string, string>
                    {
                        { "Provider 1", "5678" },
                        { "Provider 2", "5678" }
                    }
                }
            };
            TestMergeBaseItemDataPerson(GetOldValue(), mergeNewValue, null, false, out result);
            actual = (List<PersonInfo>)result!;
            Assert.Single(actual);
            Assert.Equal("Name 1", actual[0].Name);
            Assert.Equal(2, actual[0].ProviderIds.Count);
            Assert.Equal("1234", actual[0].ProviderIds["Provider 1"]);
            Assert.Equal("5678", actual[0].ProviderIds["Provider 2"]);

            // picture adds if missing but won't overwrite (forcing overwrites entire list, not entries in merged PersonInfo)
            var mergePicture1 = new List<PersonInfo>
            {
                new()
                {
                    Name = "Name 1",
                    ImageUrl = "URL 1"
                }
            };
            TestMergeBaseItemDataPerson(GetOldValue(), mergePicture1, null, false, out result);
            actual = (List<PersonInfo>)result!;
            Assert.Single(actual);
            Assert.Equal("Name 1", actual[0].Name);
            Assert.Equal("URL 1", actual[0].ImageUrl);
            var mergePicture2 = new List<PersonInfo>
            {
                new()
                {
                    Name = "Name 1",
                    ImageUrl = "URL 2"
                }
            };
            TestMergeBaseItemDataPerson(mergePicture1, mergePicture2, null, false, out result);
            actual = (List<PersonInfo>)result!;
            Assert.Single(actual);
            Assert.Equal("Name 1", actual[0].Name);
            Assert.Equal("URL 1", actual[0].ImageUrl);

            // Empty source no longer overwrites existing cast data.
            Assert.False(TestMergeBaseItemDataPerson(GetOldValue(), new List<PersonInfo>(), null, true, out _));
        }

        private static bool TestMergeBaseItemDataPerson(List<PersonInfo>? oldValue, List<PersonInfo>? newValue, MetadataField? lockField, bool replaceData, out object? actualValue)
        {
            var source = new MetadataResult<Movie>
            {
                Item = new Movie(),
                People = newValue
            };
            source.Item.ProviderIds["test"] = "1";

            var target = new MetadataResult<Movie>
            {
                Item = new Movie(),
                People = oldValue
            };

            var lockedFields = lockField is null ? Array.Empty<MetadataField>() : new[] { (MetadataField)lockField };
            MetadataService<Movie, MovieInfo>.MergeBaseItemData(source, target, lockedFields, replaceData, false);

            actualValue = target.People;
            return newValue?.SequenceEqual((IEnumerable<PersonInfo>)actualValue!) ?? actualValue is null;
        }

        /// <summary>
        /// Makes a call to <see cref="MetadataService{TItemType,TIdType}.MergeBaseItemData"/> with the provided parameters and returns whether the target changed or not.
        ///
        /// Reflection is used to allow testing of all fields using the same logic, rather than relying on copy/pasting test code for each field.
        /// </summary>
        /// <param name="propName">The property to test.</param>
        /// <param name="oldValue">The initial value in the target object.</param>
        /// <param name="newValue">The initial value in the source object.</param>
        /// <param name="lockField">The metadata field that locks this property if the field should be locked, or <c>null</c> to leave unlocked.</param>
        /// <param name="replaceData">Passed through to <see cref="MetadataService{TItemType,TIdType}.MergeBaseItemData"/>.</param>
        /// <param name="actualValue">The resulting value set to the target.</param>
        /// <typeparam name="TItemType">The <see cref="BaseItem"/> type to test on.</typeparam>
        /// <typeparam name="TIdType">The <see cref="BaseItem"/> info type.</typeparam>
        /// <returns><c>true</c> if the property on the target updates to match the source value when<see cref="MetadataService{TItemType,TIdType}.MergeBaseItemData"/> is called.</returns>
        private static bool TestMergeBaseItemData<TItemType, TIdType>(string propName, object? oldValue, object? newValue, MetadataField? lockField, bool replaceData, out object? actualValue)
            where TItemType : BaseItem, IHasLookupInfo<TIdType>, new()
            where TIdType : ItemLookupInfo, new()
        {
            var property = typeof(TItemType).GetProperty(propName)!;

            var source = new MetadataResult<TItemType>
            {
                Item = new TItemType()
            };
            property.SetValue(source.Item, newValue);
            if (!string.Equals(propName, "ProviderIds", StringComparison.Ordinal) && source.Item.ProviderIds.Count == 0)
            {
                source.Item.ProviderIds["test"] = "1";
            }

            var target = new MetadataResult<TItemType>
            {
                Item = new TItemType()
            };
            property.SetValue(target.Item, oldValue);

            var lockedFields = lockField is null ? Array.Empty<MetadataField>() : new[] { (MetadataField)lockField };
            // generic type doesn't actually matter to call the static method, just has to be filled in
            MetadataService<TItemType, TIdType>.MergeBaseItemData(source, target, lockedFields, replaceData, false);

            actualValue = property.GetValue(target.Item);
            return newValue?.Equals(actualValue) ?? actualValue is null;
        }

        private static TestMovieMetadataService CreateTestService()
        {
            return new TestMovieMetadataService(
                Mock.Of<IServerConfigurationManager>(),
                Mock.Of<ILogger<MetadataService<Movie, MovieInfo>>>(),
                Mock.Of<IProviderManager>(),
                Mock.Of<IFileSystem>(),
                Mock.Of<ILibraryManager>(),
                Mock.Of<IExternalDataManager>(),
                Mock.Of<IItemRepository>());
        }

        private sealed class TestMovieMetadataService : MetadataService<Movie, MovieInfo>
        {
            public TestMovieMetadataService(
                IServerConfigurationManager serverConfigurationManager,
                ILogger<MetadataService<Movie, MovieInfo>> logger,
                IProviderManager providerManager,
                IFileSystem fileSystem,
                ILibraryManager libraryManager,
                IExternalDataManager externalDataManager,
                IItemRepository itemRepository)
                : base(serverConfigurationManager, logger, providerManager, fileSystem, libraryManager, externalDataManager, itemRepository)
            {
            }

            public Task<RefreshResult> RunRefreshWithProviders(
                MetadataResult<Movie> metadata,
                MovieInfo id,
                MetadataRefreshOptions options,
                ICollection<IMetadataProvider> providers,
                bool isSavingMetadata,
                CancellationToken cancellationToken)
            {
                return RefreshWithProviders(metadata, id, options, providers, ImageProvider, isSavingMetadata, cancellationToken);
            }
        }
    }
}
