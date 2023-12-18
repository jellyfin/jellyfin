using System;
using Emby.Server.Implementations.Sorting;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
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
