using System;
using System.Collections;
using System.Collections.Generic;
using Emby.Server.Implementations.Sorting;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Sorting
{
    public class AiredEpisodeOrderComparerTests
    {
        [Theory]
        [ClassData(typeof(EpisodeBadData))]
        public void AiredEpisodeOrderCompareErrorTest(BaseItem x, BaseItem y)
        {
            var cmp = new AiredEpisodeOrderComparer();
            Assert.Throws<ArgumentNullException>(() => cmp.Compare(x, y));
        }

        [Theory]
        [ClassData(typeof(EpisodeTestData))]
        public void AiredEpisodeOrderCompareTest(BaseItem x, BaseItem y, int expected)
        {
            var cmp = new AiredEpisodeOrderComparer();

            Assert.Equal(expected, cmp.Compare(x, y));
            if (expected == 1)
            {
                Assert.Equal(-expected, cmp.Compare(y, x));
            }
        }

        private class EpisodeBadData : IEnumerable<object?[]>
        {
            public IEnumerator<object?[]> GetEnumerator()
            {
                yield return new object?[] { null, new Episode() };
                yield return new object?[] { new Episode() };
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private class EpisodeTestData : IEnumerable<object?[]>
        {
            public IEnumerator<object?[]> GetEnumerator()
            {
                yield return new object?[] { new Movie(), new Movie(), 0 };
                yield return new object?[] { new Movie(), new Episode(), 1 };
                // Good cases
                yield return new object?[] { new Episode(), new Episode(), 0 };
                yield return new object?[] { new Episode { ParentIndexNumber = 1, IndexNumber = 1 }, new Episode { ParentIndexNumber = 1, IndexNumber = 1 }, 0 };
                yield return new object?[] { new Episode { ParentIndexNumber = 1, IndexNumber = 2 }, new Episode { ParentIndexNumber = 1, IndexNumber = 1 }, 1 };
                yield return new object?[] { new Episode { ParentIndexNumber = 2, IndexNumber = 1 }, new Episode { ParentIndexNumber = 1, IndexNumber = 1 }, 1 };
                // Good Specials
                yield return new object?[] { new Episode { ParentIndexNumber = 0, IndexNumber = 1 }, new Episode { ParentIndexNumber = 0, IndexNumber = 1 }, 0 };
                yield return new object?[] { new Episode { ParentIndexNumber = 0, IndexNumber = 2 }, new Episode { ParentIndexNumber = 0, IndexNumber = 1 }, 1 };

                // Specials to Episodes
                yield return new object?[] { new Episode { ParentIndexNumber = 1, IndexNumber = 1 }, new Episode { ParentIndexNumber = 0, IndexNumber = 1 }, 1 };
                yield return new object?[] { new Episode { ParentIndexNumber = 1, IndexNumber = 1 }, new Episode { ParentIndexNumber = 0, IndexNumber = 2 }, 1 };
                yield return new object?[] { new Episode { ParentIndexNumber = 1, IndexNumber = 2 }, new Episode { ParentIndexNumber = 0, IndexNumber = 1 }, 1 };

                yield return new object?[] { new Episode { ParentIndexNumber = 1, IndexNumber = 2 }, new Episode { ParentIndexNumber = 0, IndexNumber = 1 }, 1 };
                yield return new object?[] { new Episode { ParentIndexNumber = 1, IndexNumber = 1 }, new Episode { ParentIndexNumber = 0, IndexNumber = 2 }, 1 };

                yield return new object?[] { new Episode { ParentIndexNumber = 0, IndexNumber = 1, AirsAfterSeasonNumber = 1 }, new Episode { ParentIndexNumber = 1, IndexNumber = 1 }, 1 };
                yield return new object?[] { new Episode { ParentIndexNumber = 3, IndexNumber = 1 }, new Episode { ParentIndexNumber = 0, IndexNumber = 1, AirsAfterSeasonNumber = 1 }, 1 };

                yield return new object?[] { new Episode { ParentIndexNumber = 3, IndexNumber = 1 }, new Episode { ParentIndexNumber = 0, IndexNumber = 1, AirsAfterSeasonNumber = 1, AirsBeforeEpisodeNumber = 2 }, 1 };

                yield return new object?[] { new Episode { ParentIndexNumber = 1, IndexNumber = 1 }, new Episode { ParentIndexNumber = 0, IndexNumber = 1, AirsBeforeSeasonNumber = 1 }, 1 };
                yield return new object?[] { new Episode { ParentIndexNumber = 1, IndexNumber = 2 }, new Episode { ParentIndexNumber = 0, IndexNumber = 1, AirsBeforeSeasonNumber = 1, AirsBeforeEpisodeNumber = 2 }, 1 };
                yield return new object?[] { new Episode { ParentIndexNumber = 1 }, new Episode { ParentIndexNumber = 0, IndexNumber = 1, AirsBeforeSeasonNumber = 1, AirsBeforeEpisodeNumber = 2 }, 0 };
                yield return new object?[] { new Episode { ParentIndexNumber = 1, IndexNumber = 3 }, new Episode { ParentIndexNumber = 0, IndexNumber = 1, AirsBeforeSeasonNumber = 1, AirsBeforeEpisodeNumber = 2 }, 1 };
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
