using System;
using Emby.Server.Implementations.Sorting;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Sorting
{
    public class AiredEpisodeOrderComparerTests
    {
        private readonly AiredEpisodeOrderComparer _cmp = new AiredEpisodeOrderComparer();

        [Theory]
        [ClassData(typeof(EpisodeBadData))]
        public void Compare_GivenNull_ThrowsArgumentNullException(BaseItem? x, BaseItem? y)
        {
            Assert.Throws<ArgumentNullException>(() => _cmp.Compare(x, y));
        }

        [Theory]
        [ClassData(typeof(EpisodeTestData))]
        public void AiredEpisodeOrderCompareTest(BaseItem x, BaseItem y, int expected)
        {
            Assert.Equal(expected, _cmp.Compare(x, y));
            Assert.Equal(-expected, _cmp.Compare(y, x));
        }

        [Fact]
        public void Compare_StackedEpisodeParts_SortsByAdditionalPartOrder()
        {
            var ownerId = Guid.NewGuid();
            var owner = new Episode
            {
                Id = ownerId,
                ParentIndexNumber = 1,
                IndexNumber = 1,
                Path = "/series/Season 01/Show - S01E01 - part1.mkv",
                AdditionalParts =
                [
                    "/series/Season 01/Show - S01E01 - part2.mkv",
                    "/series/Season 01/Show - S01E01 - part3.mkv"
                ]
            };
            var part2 = new Episode
            {
                OwnerId = ownerId,
                ParentIndexNumber = 1,
                IndexNumber = 1,
                Path = "/series/Season 01/Show - S01E01 - part2.mkv"
            };
            var part3 = new Episode
            {
                OwnerId = ownerId,
                ParentIndexNumber = 1,
                IndexNumber = 1,
                Path = "/series/Season 01/Show - S01E01 - part3.mkv"
            };

            var libraryManager = new Mock<ILibraryManager>();
            libraryManager.Setup(i => i.GetItemById(ownerId)).Returns(owner);
            var previousLibraryManager = BaseItem.LibraryManager;
            try
            {
                BaseItem.LibraryManager = libraryManager.Object;

                Assert.True(_cmp.Compare(owner, part2) < 0);
                Assert.True(_cmp.Compare(part2, part3) < 0);
                Assert.True(_cmp.Compare(part3, owner) > 0);
            }
            finally
            {
                BaseItem.LibraryManager = previousLibraryManager;
            }
        }

        [Theory]
        [InlineData(
            "/series/Season 01/Show S01E01-part-1 - Pilot.mkv",
            "/series/Season 01/Show S01E01-part-2 - Pilot.mkv")]
        [InlineData(
            "/series/Season 01/Show S01E01 pt A - Pilot.mkv",
            "/series/Season 01/Show S01E01 pt B - Pilot.mkv")]
        public void Compare_DuplicateEpisodeFilenameParts_SortsByPartNumber(string firstPartPath, string secondPartPath)
        {
            var firstPart = new Episode
            {
                ParentIndexNumber = 1,
                IndexNumber = 1,
                Path = firstPartPath,
                PremiereDate = new DateTime(2021, 09, 12, 0, 0, 0)
            };
            var secondPart = new Episode
            {
                ParentIndexNumber = 1,
                IndexNumber = 1,
                Path = secondPartPath,
                PremiereDate = new DateTime(2021, 09, 11, 0, 0, 0)
            };

            Assert.True(_cmp.Compare(firstPart, secondPart) < 0);
            Assert.True(_cmp.Compare(secondPart, firstPart) > 0);
        }

        private sealed class EpisodeBadData : TheoryData<BaseItem?, BaseItem?>
        {
            public EpisodeBadData()
            {
                Add(null, new Episode());
                Add(new Episode(), null);
            }
        }

        private sealed class EpisodeTestData : TheoryData<BaseItem, BaseItem, int>
        {
            public EpisodeTestData()
            {
                Add(
                    new Movie(),
                    new Movie(),
                    0);

                Add(
                    new Movie(),
                    new Episode(),
                    1);

                // Good cases
                Add(
                    new Episode(),
                    new Episode(),
                    0);

                Add(
                    new Episode { ParentIndexNumber = 1, IndexNumber = 1 },
                    new Episode { ParentIndexNumber = 1, IndexNumber = 1 },
                    0);

                Add(
                    new Episode { ParentIndexNumber = 1, IndexNumber = 2 },
                    new Episode { ParentIndexNumber = 1, IndexNumber = 1 },
                    1);

                Add(
                    new Episode { ParentIndexNumber = 2, IndexNumber = 1 },
                    new Episode { ParentIndexNumber = 1, IndexNumber = 1 },
                    1);

                // Good Specials
                Add(
                    new Episode { ParentIndexNumber = 0, IndexNumber = 1 },
                    new Episode { ParentIndexNumber = 0, IndexNumber = 1 },
                    0);

                Add(
                    new Episode { ParentIndexNumber = 0, IndexNumber = 2 },
                    new Episode { ParentIndexNumber = 0, IndexNumber = 1 },
                    1);

                // Specials to Episodes
                Add(
                    new Episode { ParentIndexNumber = 1, IndexNumber = 1 },
                    new Episode { ParentIndexNumber = 0, IndexNumber = 1 },
                    1);

                Add(
                    new Episode { ParentIndexNumber = 1, IndexNumber = 1 },
                    new Episode { ParentIndexNumber = 0, IndexNumber = 2 },
                    1);

                Add(
                    new Episode { ParentIndexNumber = 1, IndexNumber = 2 },
                    new Episode { ParentIndexNumber = 0, IndexNumber = 1 },
                    1);

                Add(
                    new Episode { ParentIndexNumber = 1, IndexNumber = 2 },
                    new Episode { ParentIndexNumber = 0, IndexNumber = 1 },
                    1);

                Add(
                    new Episode { ParentIndexNumber = 1, IndexNumber = 1 },
                    new Episode { ParentIndexNumber = 0, IndexNumber = 2 },
                    1);

                Add(
                    new Episode { ParentIndexNumber = 0, IndexNumber = 1, AirsAfterSeasonNumber = 1 },
                    new Episode { ParentIndexNumber = 1, IndexNumber = 1 },
                    1);

                Add(
                    new Episode { ParentIndexNumber = 3, IndexNumber = 1 },
                    new Episode { ParentIndexNumber = 0, IndexNumber = 1, AirsAfterSeasonNumber = 1 },
                    1);

                Add(
                    new Episode { ParentIndexNumber = 3, IndexNumber = 1 },
                    new Episode { ParentIndexNumber = 0, IndexNumber = 1, AirsAfterSeasonNumber = 1, AirsBeforeEpisodeNumber = 2 },
                    1);

                Add(
                    new Episode { ParentIndexNumber = 1, IndexNumber = 1 },
                    new Episode { ParentIndexNumber = 0, IndexNumber = 1, AirsBeforeSeasonNumber = 1 },
                    1);

                Add(
                    new Episode { ParentIndexNumber = 1, IndexNumber = 2 },
                    new Episode { ParentIndexNumber = 0, IndexNumber = 1, AirsBeforeSeasonNumber = 1, AirsBeforeEpisodeNumber = 2 },
                    1);

                Add(
                    new Episode { ParentIndexNumber = 1 },
                    new Episode { ParentIndexNumber = 0, IndexNumber = 1, AirsBeforeSeasonNumber = 1, AirsBeforeEpisodeNumber = 2 },
                    0);

                Add(
                    new Episode { ParentIndexNumber = 1, IndexNumber = 3 },
                    new Episode { ParentIndexNumber = 0, IndexNumber = 1, AirsBeforeSeasonNumber = 1, AirsBeforeEpisodeNumber = 2 },
                    1);

                // Premiere Date
                Add(
                    new Episode { ParentIndexNumber = 1, IndexNumber = 1, PremiereDate = new DateTime(2021, 09, 12, 0, 0, 0) },
                    new Episode { ParentIndexNumber = 1, IndexNumber = 1, PremiereDate = new DateTime(2021, 09, 12, 0, 0, 0) },
                    0);

                Add(
                    new Episode { ParentIndexNumber = 1, IndexNumber = 1, PremiereDate = new DateTime(2021, 09, 11, 0, 0, 0) },
                    new Episode { ParentIndexNumber = 1, IndexNumber = 1, PremiereDate = new DateTime(2021, 09, 12, 0, 0, 0) },
                    -1);

                Add(
                    new Episode { ParentIndexNumber = 1, IndexNumber = 1, PremiereDate = new DateTime(2021, 09, 12, 0, 0, 0) },
                    new Episode { ParentIndexNumber = 1, IndexNumber = 1, PremiereDate = new DateTime(2021, 09, 11, 0, 0, 0) },
                    1);
            }
        }
    }
}
