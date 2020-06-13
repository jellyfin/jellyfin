using System.Linq;
using Emby.Naming.Common;
using Emby.Naming.Video;
using MediaBrowser.Model.IO;
using Xunit;

namespace Jellyfin.Naming.Tests.Video
{
    public class StackTests
    {
        private readonly NamingOptions _namingOptions = new NamingOptions();

        [Fact]
        public void TestSimpleStack()
        {
            var files = new[]
            {
                "Bad Boys (2006) part1.mkv",
                "Bad Boys (2006) part2.mkv",
                "Bad Boys (2006) part3.mkv",
                "Bad Boys (2006) part4.mkv",
                "Bad Boys (2006)-trailer.mkv"
            };

            var resolver = GetResolver();

            var result = resolver.ResolveFiles(files).ToList();

            Assert.Single(result);
            TestStackInfo(result[0], "Bad Boys (2006)", 4);
        }

        [Fact]
        public void TestFalsePositives()
        {
            var files = new[]
            {
                "Bad Boys (2006).mkv",
                "Bad Boys (2007).mkv"
            };

            var resolver = GetResolver();

            var result = resolver.ResolveFiles(files).ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void TestFalsePositives2()
        {
            var files = new[]
            {
                "Bad Boys 2006.mkv",
                "Bad Boys 2007.mkv"
            };

            var resolver = GetResolver();

            var result = resolver.ResolveFiles(files).ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void TestFalsePositives3()
        {
            var files = new[]
            {
                "300 (2006).mkv",
                "300 (2007).mkv"
            };

            var resolver = GetResolver();

            var result = resolver.ResolveFiles(files).ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void TestFalsePositives4()
        {
            var files = new[]
            {
                "300 2006.mkv",
                "300 2007.mkv"
            };

            var resolver = GetResolver();

            var result = resolver.ResolveFiles(files).ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void TestFalsePositives5()
        {
            var files = new[]
            {
                "Star Trek 1 - The motion picture.mkv",
                "Star Trek 2- The wrath of khan.mkv"
            };

            var resolver = GetResolver();

            var result = resolver.ResolveFiles(files).ToList();
            Assert.Empty(result);
        }

        [Fact]
        public void TestFalsePositives6()
        {
            var files = new[]
            {
                "Red Riding in the Year of Our Lord 1983 (2009).mkv",
                "Red Riding in the Year of Our Lord 1980 (2009).mkv",
                "Red Riding in the Year of Our Lord 1974 (2009).mkv"
            };

            var resolver = GetResolver();

            var result = resolver.ResolveFiles(files).ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void TestStackName()
        {
            var files = new[]
            {
                "d:/movies/300 2006 part1.mkv",
                "d:/movies/300 2006 part2.mkv"
            };

            var resolver = GetResolver();

            var result = resolver.ResolveFiles(files).ToList();

            Assert.Single(result);
            TestStackInfo(result[0], "300 2006", 2);
        }

        [Fact]
        public void TestDirtyNames()
        {
            var files = new[]
            {
                "Bad Boys (2006).part1.stv.unrated.multi.1080p.bluray.x264-rough.mkv",
                "Bad Boys (2006).part2.stv.unrated.multi.1080p.bluray.x264-rough.mkv",
                "Bad Boys (2006).part3.stv.unrated.multi.1080p.bluray.x264-rough.mkv",
                "Bad Boys (2006).part4.stv.unrated.multi.1080p.bluray.x264-rough.mkv",
                "Bad Boys (2006)-trailer.mkv"
            };

            var resolver = GetResolver();

            var result = resolver.ResolveFiles(files).ToList();

            Assert.Single(result);
            TestStackInfo(result[0], "Bad Boys (2006).stv.unrated.multi.1080p.bluray.x264-rough", 4);
        }

        [Fact]
        public void TestNumberedFiles()
        {
            var files = new[]
            {
                "Bad Boys (2006).mkv",
                "Bad Boys (2006) 1.mkv",
                "Bad Boys (2006) 2.mkv",
                "Bad Boys (2006) 3.mkv",
                "Bad Boys (2006)-trailer.mkv"
            };

            var resolver = GetResolver();

            var result = resolver.ResolveFiles(files).ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void TestSimpleStackWithNumericName()
        {
            var files = new[]
            {
                "300 (2006) part1.mkv",
                "300 (2006) part2.mkv",
                "300 (2006) part3.mkv",
                "300 (2006) part4.mkv",
                "300 (2006)-trailer.mkv"
            };

            var resolver = GetResolver();

            var result = resolver.ResolveFiles(files).ToList();

            Assert.Single(result);
            TestStackInfo(result[0], "300 (2006)", 4);
        }

        [Fact]
        public void TestMixedExpressionsNotAllowed()
        {
            var files = new[]
            {
                "Bad Boys (2006) part1.mkv",
                "Bad Boys (2006) part2.mkv",
                "Bad Boys (2006) part3.mkv",
                "Bad Boys (2006) parta.mkv",
                "Bad Boys (2006)-trailer.mkv"
            };

            var resolver = GetResolver();

            var result = resolver.ResolveFiles(files).ToList();

            Assert.Single(result);
            TestStackInfo(result[0], "Bad Boys (2006)", 3);
        }

        [Fact]
        public void TestDualStacks()
        {
            var files = new[]
            {
                "Bad Boys (2006) part1.mkv",
                "Bad Boys (2006) part2.mkv",
                "Bad Boys (2006) part3.mkv",
                "Bad Boys (2006) part4.mkv",
                "Bad Boys (2006)-trailer.mkv",
                "300 (2006) part1.mkv",
                "300 (2006) part2.mkv",
                "300 (2006) part3.mkv",
                "300 (2006)-trailer.mkv"
            };

            var resolver = GetResolver();

            var result = resolver.ResolveFiles(files).ToList();

            Assert.Equal(2, result.Count);
            TestStackInfo(result[1], "Bad Boys (2006)", 4);
            TestStackInfo(result[0], "300 (2006)", 3);
        }

        [Fact]
        public void TestDirectories()
        {
            var files = new[]
            {
                "blah blah - cd 1",
                "blah blah - cd 2"
            };

            var resolver = GetResolver();

            var result = resolver.ResolveDirectories(files).ToList();

            Assert.Single(result);
            TestStackInfo(result[0], "blah blah", 2);
        }

        [Fact]
        public void TestFalsePositive()
        {
            var files = new[]
            {
                "300a.mkv",
                "300b.mkv",
                "300c.mkv",
                "300-trailer.mkv"
            };

            var resolver = GetResolver();

            var result = resolver.ResolveFiles(files).ToList();

            Assert.Single(result);

            TestStackInfo(result[0], "300", 3);
        }

        [Fact]
        public void TestFailSequence()
        {
            var files = new[]
            {
                "300 part1.mkv",
                "300 part2.mkv",
                "Avatar",
                "Avengers part1.mkv",
                "Avengers part2.mkv",
                "Avengers part3.mkv"
            };

            var resolver = GetResolver();

            var result = resolver.ResolveFiles(files).ToList();

            Assert.Equal(2, result.Count);

            TestStackInfo(result[0], "300", 2);
            TestStackInfo(result[1], "Avengers", 3);
        }

        [Fact]
        public void TestMixedExpressions()
        {
            var files = new[]
            {
                "Bad Boys (2006) part1.mkv",
                "Bad Boys (2006) part2.mkv",
                "Bad Boys (2006) part3.mkv",
                "Bad Boys (2006) part4.mkv",
                "Bad Boys (2006)-trailer.mkv",
                "300 (2006) parta.mkv",
                "300 (2006) partb.mkv",
                "300 (2006) partc.mkv",
                "300 (2006) partd.mkv",
                "300 (2006)-trailer.mkv",
                "300a.mkv",
                "300b.mkv",
                "300c.mkv",
                "300-trailer.mkv"
            };

            var resolver = GetResolver();

            var result = resolver.ResolveFiles(files).ToList();

            Assert.Equal(3, result.Count);

            TestStackInfo(result[0], "300 (2006)", 4);
            TestStackInfo(result[1], "300", 3);
            TestStackInfo(result[2], "Bad Boys (2006)", 4);
        }

        [Fact]
        public void TestAlphaLimitOfFour()
        {
            var files = new[]
            {
                "300 (2006) parta.mkv",
                "300 (2006) partb.mkv",
                "300 (2006) partc.mkv",
                "300 (2006) partd.mkv",
                "300 (2006) parte.mkv",
                "300 (2006) partf.mkv",
                "300 (2006) partg.mkv",
                "300 (2006)-trailer.mkv"
            };

            var resolver = GetResolver();

            var result = resolver.ResolveFiles(files).ToList();

            Assert.Single(result);

            TestStackInfo(result[0], "300 (2006)", 4);
        }

        [Fact]
        public void TestMixed()
        {
            var files = new[]
            {
                new FileSystemMetadata { FullName = "Bad Boys (2006) part1.mkv", IsDirectory = false },
                new FileSystemMetadata { FullName = "Bad Boys (2006) part2.mkv", IsDirectory = false },
                new FileSystemMetadata { FullName = "300 (2006) part2", IsDirectory = true },
                new FileSystemMetadata { FullName = "300 (2006) part3", IsDirectory = true },
                new FileSystemMetadata { FullName = "300 (2006) part1", IsDirectory = true }
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files).ToList();

            Assert.Equal(2, result.Count);
            TestStackInfo(result[0], "300 (2006)", 3);
            TestStackInfo(result[1], "Bad Boys (2006)", 2);
        }

        [Fact]
        public void TestNamesWithoutParts()
        {
            // No stacking here because there is no part/disc/etc
            var files = new[]
            {
                "Harry Potter and the Deathly Hallows.mkv",
                "Harry Potter and the Deathly Hallows 1.mkv",
                "Harry Potter and the Deathly Hallows 2.mkv",
                "Harry Potter and the Deathly Hallows 3.mkv",
                "Harry Potter and the Deathly Hallows 4.mkv"
            };

            var resolver = GetResolver();

            var result = resolver.ResolveFiles(files).ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void TestNumbersAppearingBeforePartNumber()
        {
            // No stacking here because there is no part/disc/etc
            var files = new[]
            {
                "Neverland (2011)[720p][PG][Voted 6.5][Family-Fantasy]part1.mkv",
                "Neverland (2011)[720p][PG][Voted 6.5][Family-Fantasy]part2.mkv"
            };

            var resolver = GetResolver();

            var result = resolver.ResolveFiles(files).ToList();

            Assert.Single(result);
            Assert.Equal(2, result[0].Files.Count);
        }

        [Fact]
        public void TestMultiDiscs()
        {
            // No stacking here because there is no part/disc/etc
            var files = new[]
            {
                @"M:/Movies (DVD)/Movies (Musical)/The Sound of Music/The Sound of Music (1965) (Disc 01)",
                @"M:/Movies (DVD)/Movies (Musical)/The Sound of Music/The Sound of Music (1965) (Disc 02)"
            };

            var resolver = GetResolver();

            var result = resolver.ResolveDirectories(files).ToList();

            Assert.Single(result);
            Assert.Equal(2, result[0].Files.Count);
        }

        private void TestStackInfo(FileStack stack, string name, int fileCount)
        {
            Assert.Equal(fileCount, stack.Files.Count);
            Assert.Equal(name, stack.Name);
        }

        private StackResolver GetResolver()
        {
            return new StackResolver(_namingOptions);
        }
    }
}
