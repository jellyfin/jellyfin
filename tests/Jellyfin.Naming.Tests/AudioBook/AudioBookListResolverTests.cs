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
                "Harry Potter and the Deathly Hallows/Extra.mp3",

                "Batman/Chapter 1.mp3",
                "Batman/Chapter 2.mp3",
                "Batman/Chapter 3.mp3",

                "Ready Player One (2020)/Ready Player One.mp3",
                "Ready Player One (2020)/extra.mp3",

                "Badman/audiobook.mp3",
                "Badman/extra.mp3",

                "Superman (2020)/book.mp3",
                "Superman (2020)/extra.mp3"
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            })).ToList();

            Assert.Equal(4, result[0].Files.Count);
            Assert.Single(result[0].Extras);
            Assert.Equal("Harry Potter and the Deathly Hallows", result[0].Name);

            Assert.Equal(3, result[1].Files.Count);
            Assert.Empty(result[1].Extras);
            Assert.Equal("Batman", result[1].Name);

            Assert.Equal(2, result[2].Files.Count);
            Assert.Single(result[2].Extras);
            Assert.Equal("Badman", result[2].Name);

            Assert.Equal(2, result[3].Files.Count);
            Assert.Single(result[3].Extras);
            Assert.Equal("Superman", result[3].Name);
        }

        [Fact]
        public void TestAlternativeVersions()
        {
            var files = new[]
            {
                "Harry Potter and the Deathly Hallows/Chapter 1.ogg",
                "Harry Potter and the Deathly Hallows/Chapter 1.mp3",

                "Aqua-man/book.mp3",

                "Deadpool.mp3",
                "Deadpool [HQ].mp3",

                "Superman/book.mp3",
                "Superman/audiobook.mp3",
                "Superman/Superman.mp3",
                "Superman/Superman [HQ].mp3",
                "Superman/extra.mp3",

                "Batman/ Chapter 1 .mp3",
                "Batman/Chapter 1[loss-less].mp3"
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            })).ToList();

            Assert.Equal(6, result[0].Files.Count);
            // HP - Same name so we don't care which file is alternative
            Assert.Single(result[0].AlternateVersions);
            // Aqua-man
            Assert.Empty(result[1].AlternateVersions);
            // DP
            Assert.Empty(result[2].AlternateVersions);
            // DP HQ (directory missing so we do not group deadpools together)
            Assert.Empty(result[3].AlternateVersions);
            // Superman
            // Priority:
            //  1. Name
            //  2. audiobook
            //  3. book
            //  4. Names with modifiers
            Assert.Equal(3, result[4].AlternateVersions.Count);
            Assert.Equal("Superman/audiobook.mp3", result[4].AlternateVersions[0].Path);
            Assert.Equal("Superman/book.mp3", result[4].AlternateVersions[1].Path);
            Assert.Equal("Superman/Superman [HQ].mp3", result[4].AlternateVersions[2].Path);
            // Batman
            Assert.Single(result[5].AlternateVersions);
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
