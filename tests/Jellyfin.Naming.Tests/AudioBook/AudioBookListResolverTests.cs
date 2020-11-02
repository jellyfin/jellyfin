using System;
using System.Collections.Generic;
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
        public void TestAlternativeVersions()
        {
            var files = new[]
            {
                "Harry Potter and the Deathly Hallows/Chapter 1.ogg",
                "Harry Potter and the Deathly Hallows/Chapter 1.mp3",

                "Deadpool.ogg",
                "Deadpool.mp3",

                "Batman/Chapter 1.mp3"
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            })).ToList();

            Assert.Equal(3, result[0].Files.Count);
            Assert.NotEmpty(result[0].AlternateVersions);
            Assert.NotEmpty(result[1].AlternateVersions);
            Assert.Empty(result[2].AlternateVersions);
        }

        [Fact]
        public void TestNameYearExtraction()
        {
            var data = new[]
            {
                new NameYearPath
                {
                    Name = "Harry Potter and the Deathly Hallows",
                    Path = "Harry Potter and the Deathly Hallows (2007)/Chapter 1.ogg",
                    Year = 2007
                },
                new NameYearPath
                {
                    Name = "Batman",
                    Path = "Batman (2020).ogg",
                    Year = 2020
                },
                new NameYearPath
                {
                    Name = "Batman",
                    Path = "Batman( 2021 ).mp3",
                    Year = 2021
                },
                new NameYearPath
                {
                    Name = "Batman(*2021*)",
                    Path = "Batman(*2021*).mp3",
                    Year = null
                },
                new NameYearPath
                {
                    Name = "Batman",
                    Path = "Batman.mp3",
                    Year = null
                },
                new NameYearPath
                {
                    Name = "+ Batman .",
                    Path = " + Batman . .mp3",
                    Year = null
                },
                new NameYearPath
                {
                    Name = " ",
                    Path = " .mp3",
                    Year = null
                }
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(data.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i.Path
            })).ToList();

            Assert.Equal(data.Length, result.Count);

            for (int i = 0; i < data.Length; i++)
            {
                Assert.Equal(data[i].Name, result[i].Name);
                Assert.Equal(data[i].Year, result[i].Year);
            }
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

        [Fact]
        public void TestWithoutFolder()
        {
            var files = new[]
            {
                "Harry Potter and the Deathly Hallows trailer.mp3"
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            })).ToList();

            Assert.Single(result);
        }

        [Fact]
        public void TestEmpty()
        {
            var files = Array.Empty<string>();

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            })).ToList();

            Assert.Empty(result);
        }

        private AudioBookListResolver GetResolver()
        {
            return new AudioBookListResolver(_namingOptions);
        }

        internal struct NameYearPath
        {
            public string Name;
            public string Path;
            public int? Year;
        }
    }
}
