using System.Collections.Generic;
using Emby.Naming.Common;
using Emby.Naming.Video;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Naming.Tests.Video
{
    public class VideoResolverTests
    {
        private readonly NamingOptions _namingOptions = new NamingOptions();

        public static IEnumerable<object[]> GetResolveFileTestData()
        {
            yield return new object[]
            {
                new VideoFileInfo()
                {
                    Path = @"/server/Movies/7 Psychos.mkv/7 Psychos.mkv",
                    Container = "mkv",
                    Name = "7 Psychos"
                }
            };
            yield return new object[]
            {
                new VideoFileInfo()
                {
                    Path = @"/server/Movies/3 days to kill (2005)/3 days to kill (2005).mkv",
                    Container = "mkv",
                    Name = "3 days to kill",
                    Year = 2005
                }
            };
            yield return new object[]
            {
                new VideoFileInfo()
                {
                    Path = @"/server/Movies/American Psycho/American.Psycho.mkv",
                    Container = "mkv",
                    Name = "American.Psycho",
                }
            };
            yield return new object[]
            {
                new VideoFileInfo()
                {
                    Path = @"/server/Movies/brave (2007)/brave (2006).3d.sbs.mkv",
                    Container = "mkv",
                    Name = "brave",
                    Year = 2006,
                    Is3D = true,
                    Format3D = "sbs",
                }
            };
            yield return new object[]
            {
                new VideoFileInfo()
                {
                    Path = @"/server/Movies/300 (2007)/300 (2006).3d1.sbas.mkv",
                    Container = "mkv",
                    Name = "300",
                    Year = 2006
                }
            };
            yield return new object[]
            {
                new VideoFileInfo()
                {
                    Path = @"/server/Movies/300 (2007)/300 (2006).3d.sbs.mkv",
                    Container = "mkv",
                    Name = "300",
                    Year = 2006,
                    Is3D = true,
                    Format3D = "sbs",
                }
            };
            yield return new object[]
            {
                new VideoFileInfo()
                {
                    Path = @"/server/Movies/brave (2007)/brave (2006)-trailer.bluray.disc",
                    Container = "disc",
                    Name = "brave",
                    Year = 2006,
                    IsStub = true,
                    StubType = "bluray",
                }
            };
            yield return new object[]
            {
                new VideoFileInfo()
                {
                    Path = @"/server/Movies/300 (2007)/300 (2006)-trailer.bluray.disc",
                    Container = "disc",
                    Name = "300",
                    Year = 2006,
                    IsStub = true,
                    StubType = "bluray",
                }
            };
            yield return new object[]
            {
                new VideoFileInfo()
                {
                    Path = @"/server/Movies/Brave (2007)/Brave (2006).bluray.disc",
                    Container = "disc",
                    Name = "Brave",
                    Year = 2006,
                    IsStub = true,
                    StubType = "bluray",
                }
            };
            yield return new object[]
            {
                new VideoFileInfo()
                {
                    Path = @"/server/Movies/300 (2007)/300 (2006).bluray.disc",
                    Container = "disc",
                    Name = "300",
                    Year = 2006,
                    IsStub = true,
                    StubType = "bluray",
                }
            };
            yield return new object[]
            {
                new VideoFileInfo()
                {
                    Path = @"/server/Movies/300 (2007)/300 (2006)-trailer.mkv",
                    Container = "mkv",
                    Name = "300",
                    Year = 2006,
                    ExtraType = ExtraType.Trailer,
                }
            };
            yield return new object[]
            {
                new VideoFileInfo()
                {
                    Path = @"/server/Movies/Brave (2007)/Brave (2006)-trailer.mkv",
                    Container = "mkv",
                    Name = "Brave",
                    Year = 2006,
                    ExtraType = ExtraType.Trailer,
                }
            };
            yield return new object[]
            {
                new VideoFileInfo()
                {
                    Path = @"/server/Movies/300 (2007)/300 (2006).mkv",
                    Container = "mkv",
                    Name = "300",
                    Year = 2006
                }
            };
            yield return new object[]
            {
                new VideoFileInfo()
                {
                    Path = @"/server/Movies/Bad Boys (1995)/Bad Boys (1995).mkv",
                    Container = "mkv",
                    Name = "Bad Boys",
                    Year = 1995,
                }
            };
            yield return new object[]
            {
                new VideoFileInfo()
                {
                    Path = @"/server/Movies/Brave (2007)/Brave (2006).mkv",
                    Container = "mkv",
                    Name = "Brave",
                    Year = 2006,
                }
            };
        }

        [Theory]
        [MemberData(nameof(GetResolveFileTestData))]
        public void ResolveFile_ValidFileName_Success(VideoFileInfo expectedResult)
        {
            var result = new VideoResolver(_namingOptions).ResolveFile(expectedResult.Path);

            Assert.NotNull(result);
            Assert.Equal(result?.Path, expectedResult.Path);
            Assert.Equal(result?.Container, expectedResult.Container);
            Assert.Equal(result?.Name, expectedResult.Name);
            Assert.Equal(result?.Year, expectedResult.Year);
            Assert.Equal(result?.ExtraType, expectedResult.ExtraType);
            Assert.Equal(result?.Format3D, expectedResult.Format3D);
            Assert.Equal(result?.Is3D, expectedResult.Is3D);
            Assert.Equal(result?.IsStub, expectedResult.IsStub);
            Assert.Equal(result?.StubType, expectedResult.StubType);
            Assert.Equal(result?.IsDirectory, expectedResult.IsDirectory);
            Assert.Equal(result?.FileNameWithoutExtension, expectedResult.FileNameWithoutExtension);
        }
    }
}
