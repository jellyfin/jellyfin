using System.Linq;
using Emby.Naming.AudioBook;
using Emby.Naming.Common;
using MediaBrowser.Model.IO;
using Xunit;

namespace Jellyfin.Naming.Tests.AudioBook
{
    public class AudioBookListResolverTests
    {
        private readonly NamingOptions _namingOptions = new NamingOptions();

        [Fact]
        public void TestStackAndExtras()
        {
            // No stacking here because there is no part/disc/etc
            var files = new[]
            {
                "Harry Potter and the Deathly Hallows/Part 1.mp3",
                "Harry Potter and the Deathly Hallows/Part 2.mp3",
                "Harry Potter and the Deathly Hallows/book.nfo",

                "Batman/Chapter 1.mp3",
                "Batman/Chapter 2.mp3",
                "Batman/Chapter 3.mp3",
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            })).ToList();

            Assert.Equal(2, result[0].Files.Count);
            // Assert.Empty(result[0].Extras); FIXME: AudioBookListResolver should resolve extra files properly
            Assert.Equal("Harry Potter and the Deathly Hallows", result[0].Name);

            Assert.Equal(3, result[1].Files.Count);
            Assert.Empty(result[1].Extras);
            Assert.Equal("Batman", result[1].Name);
        }

        [Fact]
        public void TestWithMetadata()
        {
            var files = new[]
            {
                "Harry Potter and the Deathly Hallows/Chapter 1.ogg",
                "Harry Potter and the Deathly Hallows/Harry Potter and the Deathly Hallows.nfo"
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            }));

            Assert.Single(result);
        }

        [Fact]
        public void TestWithExtra()
        {
            var files = new[]
            {
                "Harry Potter and the Deathly Hallows/Chapter 1.mp3",
                "Harry Potter and the Deathly Hallows/Harry Potter and the Deathly Hallows trailer.mp3"
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            })).ToList();

            Assert.Single(result);
        }

        private AudioBookListResolver GetResolver()
        {
            return new AudioBookListResolver(_namingOptions);
        }
    }
}
