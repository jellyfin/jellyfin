using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Naming.Tests.Video
{
    public class VideoResolverTests : BaseVideoTest
    {
        // FIXME
        // [Fact]
        public void TestSimpleFile()
        {
            var parser = GetParser();

            var result =
                parser.ResolveFile(@"/server/Movies/Brave (2007)/Brave (2006).mkv");

            Assert.Equal(2006, result.Year);
            Assert.False(result.IsStub);
            Assert.False(result.Is3D);
            Assert.Equal("Brave", result.Name);
            Assert.Null(result.ExtraType);
        }

        // FIXME
        // [Fact]
        public void TestSimpleFile2()
        {
            var parser = GetParser();

            var result =
                parser.ResolveFile(@"/server/Movies/Bad Boys (1995)/Bad Boys (1995).mkv");

            Assert.Equal(1995, result.Year);
            Assert.False(result.IsStub);
            Assert.False(result.Is3D);
            Assert.Equal("Bad Boys", result.Name);
            Assert.Null(result.ExtraType);
        }

        // FIXME
        // [Fact]
        public void TestSimpleFileWithNumericName()
        {
            var parser = GetParser();

            var result =
                parser.ResolveFile(@"/server/Movies/300 (2007)/300 (2006).mkv");

            Assert.Equal(2006, result.Year);
            Assert.False(result.IsStub);
            Assert.False(result.Is3D);
            Assert.Equal("300", result.Name);
            Assert.Null(result.ExtraType);
        }

        // FIXME
        // [Fact]
        public void TestExtra()
        {
            var parser = GetParser();

            var result =
                parser.ResolveFile(@"/server/Movies/Brave (2007)/Brave (2006)-trailer.mkv");

            Assert.Equal(2006, result.Year);
            Assert.False(result.IsStub);
            Assert.False(result.Is3D);
            Assert.Equal(ExtraType.Trailer, result.ExtraType);
            Assert.Equal("Brave (2006)-trailer", result.Name);
        }

        // FIXME
        // [Fact]
        public void TestExtraWithNumericName()
        {
            var parser = GetParser();

            var result =
                parser.ResolveFile(@"/server/Movies/300 (2007)/300 (2006)-trailer.mkv");

            Assert.Equal(2006, result.Year);
            Assert.False(result.IsStub);
            Assert.False(result.Is3D);
            Assert.Equal("300 (2006)-trailer", result.Name);
            Assert.Equal(ExtraType.Trailer, result.ExtraType);
        }

        // FIXME
        // [Fact]
        public void TestStubFileWithNumericName()
        {
            var parser = GetParser();

            var result =
                parser.ResolveFile(@"/server/Movies/300 (2007)/300 (2006).bluray.disc");

            Assert.Equal(2006, result.Year);
            Assert.True(result.IsStub);
            Assert.Equal("bluray", result.StubType);
            Assert.False(result.Is3D);
            Assert.Equal("300", result.Name);
            Assert.Null(result.ExtraType);
        }

        // FIXME
        // [Fact]
        public void TestStubFile()
        {
            var parser = GetParser();

            var result =
                parser.ResolveFile(@"/server/Movies/Brave (2007)/Brave (2006).bluray.disc");

            Assert.Equal(2006, result.Year);
            Assert.True(result.IsStub);
            Assert.Equal("bluray", result.StubType);
            Assert.False(result.Is3D);
            Assert.Equal("Brave", result.Name);
            Assert.Null(result.ExtraType);
        }

        // FIXME
        // [Fact]
        public void TestExtraStubWithNumericNameNotSupported()
        {
            var parser = GetParser();

            var result =
                parser.ResolveFile(@"/server/Movies/300 (2007)/300 (2006)-trailer.bluray.disc");

            Assert.Equal(2006, result.Year);
            Assert.True(result.IsStub);
            Assert.Equal("bluray", result.StubType);
            Assert.False(result.Is3D);
            Assert.Equal("300", result.Name);
            Assert.Null(result.ExtraType);
        }

        // FIXME
        // [Fact]
        public void TestExtraStubNotSupported()
        {
            // Using a stub for an extra is currently not supported
            var parser = GetParser();

            var result =
                parser.ResolveFile(@"/server/Movies/brave (2007)/brave (2006)-trailer.bluray.disc");

            Assert.Equal(2006, result.Year);
            Assert.True(result.IsStub);
            Assert.Equal("bluray", result.StubType);
            Assert.False(result.Is3D);
            Assert.Equal("brave", result.Name);
            Assert.Null(result.ExtraType);
        }

        // FIXME
        // [Fact]
        public void Test3DFileWithNumericName()
        {
            var parser = GetParser();

            var result =
                parser.ResolveFile(@"/server/Movies/300 (2007)/300 (2006).3d.sbs.mkv");

            Assert.Equal(2006, result.Year);
            Assert.False(result.IsStub);
            Assert.True(result.Is3D);
            Assert.Equal("sbs", result.Format3D);
            Assert.Equal("300", result.Name);
            Assert.Null(result.ExtraType);
        }

        // FIXME
        // [Fact]
        public void TestBad3DFileWithNumericName()
        {
            var parser = GetParser();

            var result =
                parser.ResolveFile(@"/server/Movies/300 (2007)/300 (2006).3d1.sbas.mkv");

            Assert.Equal(2006, result.Year);
            Assert.False(result.IsStub);
            Assert.False(result.Is3D);
            Assert.Equal("300", result.Name);
            Assert.Null(result.ExtraType);
            Assert.Null(result.Format3D);
        }

        // FIXME
        // [Fact]
        public void Test3DFile()
        {
            var parser = GetParser();

            var result =
                parser.ResolveFile(@"/server/Movies/brave (2007)/brave (2006).3d.sbs.mkv");

            Assert.Equal(2006, result.Year);
            Assert.False(result.IsStub);
            Assert.True(result.Is3D);
            Assert.Equal("sbs", result.Format3D);
            Assert.Equal("brave", result.Name);
            Assert.Null(result.ExtraType);
        }

        [Fact]
        public void TestNameWithoutDate()
        {
            var parser = GetParser();

            var result =
                parser.ResolveFile(@"/server/Movies/American Psycho/American.Psycho.mkv");

            Assert.Null(result.Year);
            Assert.False(result.IsStub);
            Assert.False(result.Is3D);
            Assert.Null(result.Format3D);
            Assert.Equal("American.Psycho", result.Name);
            Assert.Null(result.ExtraType);
        }

        // FIXME
        // [Fact]
        public void TestCleanDateAndStringsSequence()
        {
            var parser = GetParser();

            // In this test case, running CleanDateTime first produces no date, so it will attempt to run CleanString first and then CleanDateTime again
            var result =
                parser.ResolveFile(@"/server/Movies/3.Days.to.Kill/3.Days.to.Kill.2014.720p.BluRay.x264.YIFY.mkv");

            Assert.Equal(2014, result.Year);
            Assert.False(result.IsStub);
            Assert.False(result.Is3D);
            Assert.Null(result.Format3D);
            Assert.Equal("3.Days.to.Kill", result.Name);
            Assert.Null(result.ExtraType);
        }

        // FIXME
        // [Fact]
        public void TestCleanDateAndStringsSequence1()
        {
            var parser = GetParser();

            // In this test case, running CleanDateTime first produces no date, so it will attempt to run CleanString first and then CleanDateTime again
            var result =
                parser.ResolveFile(@"/server/Movies/3 days to kill (2005)/3 days to kill (2005).mkv");

            Assert.Equal(2005, result.Year);
            Assert.False(result.IsStub);
            Assert.False(result.Is3D);
            Assert.Null(result.Format3D);
            Assert.Equal("3 days to kill", result.Name);
            Assert.Null(result.ExtraType);
        }

        [Fact]
        public void TestFolderNameWithExtension()
        {
            var parser = GetParser();

            var result =
                parser.ResolveFile(@"/server/Movies/7 Psychos.mkv/7 Psychos.mkv");

            Assert.Null(result.Year);
            Assert.False(result.IsStub);
            Assert.False(result.Is3D);
            Assert.Equal("7 Psychos", result.Name);
            Assert.Null(result.ExtraType);
        }
    }
}
