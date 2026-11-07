using System;
using System.Globalization;
using System.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Controller.Tests.Library;

public sealed class DiscReferenceTests : IDisposable
{
    private readonly string _root;
    private readonly string _movieFolder;
    private readonly string _strmPath;

    public DiscReferenceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "jf-discref-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        _movieFolder = Path.Combine(_root, "Movies", "Movie (2000)");
        Directory.CreateDirectory(_movieFolder);
        _strmPath = Path.Combine(_movieFolder, "Movie (2000) - Director's Cut.strm");
    }

    private string[] Roots => [Path.Combine(_root, "Movies")];

    public void Dispose()
    {
        Directory.Delete(_root, true);
    }

    [Theory]
    [InlineData("dvd://./MyMovie.iso?index=2", IsoType.Dvd)]
    [InlineData("DVD://MyMovie.iso?title=2", IsoType.Dvd)]
    [InlineData("bluray://./MyMovie.iso?playlist=2", IsoType.BluRay)]
    [InlineData("BluRay://MyMovie.iso?INDEX=2", IsoType.BluRay)]
    public void TryParse_RelativeIso_ResolvesAgainstStrmFolder(string contents, IsoType expectedType)
    {
        var iso = CreateFile(_movieFolder, "MyMovie.iso");

        var reference = AssertDiscReference(contents);

        Assert.NotNull(reference);
        Assert.Equal(iso, reference.SourcePath);
        Assert.Equal(VideoType.Iso, reference.VideoType);
        Assert.Equal(expectedType, reference.IsoType);
        Assert.Equal(2, reference.Index);
    }

    [Fact]
    public void TryParse_AbsolutePath_IsUsedAsIs()
    {
        var iso = CreateFile(_movieFolder, "MyMovie.iso");

        // dvd:///mnt/x.iso on Linux, dvd://C:\...\x.iso on Windows.
        var reference = AssertDiscReference("dvd://" + iso + "?index=1");

        Assert.Equal(iso, reference?.SourcePath);
    }

    [Theory]
    [InlineData("http://example.com/stream.m3u8")]
    [InlineData("https://example.com/movie.mkv?index=1")]
    [InlineData("/media/Movie.mkv")]
    [InlineData("")]
    [InlineData("# only a comment\n\n")]
    public void TryParse_OtherStrm_IsNotADiscReference(string contents)
    {
        Assert.False(DiscReference.TryParse(_strmPath, contents, Roots, NullLogger.Instance, out var reference));
        Assert.Null(reference);
    }

    [Fact]
    public void TryParse_CommentsBlankLinesAndBom_AreSkipped()
    {
        CreateFile(_movieFolder, "MyMovie.iso");

        var reference = AssertDiscReference("\uFEFF# Extended cut\r\n\r\n  bluray://MyMovie.iso?playlist=800  \r\n# trailing comment\r\n");

        Assert.Equal(800, reference?.Index);
    }

    [Theory]
    [InlineData("dvd://MyMovie.iso")]
    [InlineData("dvd://MyMovie.iso?")]
    [InlineData("dvd://MyMovie.iso?angle=2")]
    public void TryParse_NoIndex_UsesLongestTitle(string contents)
    {
        CreateFile(_movieFolder, "MyMovie.iso");

        var reference = AssertDiscReference(contents);

        Assert.NotNull(reference);
        Assert.Null(reference.Index);
    }

    [Fact]
    public void TryParse_PercentEncodedValue_IsDecoded()
    {
        CreateFile(_movieFolder, "MyMovie.iso");

        Assert.Equal(12, AssertDiscReference("dvd://MyMovie.iso?title=%312")?.Index);
    }

    [Fact]
    public void TryParse_QuestionMarkInFileName_IsPartOfPath()
    {
        var iso = CreateFile(_movieFolder, "Who.iso");

        // "?" is not a valid file name character on Windows, so only check the splitting rule there.
        if (OperatingSystem.IsWindows())
        {
            Assert.Equal(iso, AssertDiscReference("dvd://Who.iso?index=1")?.SourcePath);
            return;
        }

        var odd = CreateFile(_movieFolder, "Who?.iso");
        Assert.Equal(odd, AssertDiscReference("dvd://Who?.iso")?.SourcePath);
        Assert.Equal(odd, AssertDiscReference("dvd://Who?.iso?index=1")?.SourcePath);
        Assert.Equal(iso, AssertDiscReference("dvd://Who.iso?index=1")?.SourcePath);
    }

    [Theory]
    [InlineData("dvd", "VIDEO_TS", VideoType.Dvd, false)]
    [InlineData("dvd", "VIDEO_TS", VideoType.Dvd, true)]
    [InlineData("bluray", "BDMV", VideoType.BluRay, false)]
    [InlineData("bluray", "BDMV", VideoType.BluRay, true)]
    public void TryParse_DiscFolder_UsesFolderContainingDiscStructure(string scheme, string discFolder, VideoType expectedType, bool pointAtDiscFolder)
    {
        // A shared disc folder elsewhere in the same library.
        var disc = Path.Combine(_root, "Movies", "Discs", "Disc 1");
        Directory.CreateDirectory(Path.Combine(disc, discFolder));
        var source = pointAtDiscFolder ? $"../Discs/Disc 1/{discFolder}/" : "../Discs/Disc 1";

        var reference = AssertDiscReference(scheme + "://" + source + "?index=3");

        Assert.NotNull(reference);
        Assert.Equal(disc, reference.SourcePath);
        Assert.Equal(expectedType, reference.VideoType);
        Assert.Equal(3, reference.Index);
    }

    [Theory]
    [InlineData("dvd://")]
    [InlineData("dvd://?index=1")]
    [InlineData("dvd://MyMovie.iso?index=-1")]
    [InlineData("dvd://MyMovie.iso?title=one")]
    [InlineData("dvd://MyMovie.iso?playlist=")]
    [InlineData("dvd://Missing.iso")]
    [InlineData("dvd://Movie.mkv")]
    [InlineData("dvd://EmptyFolder")]
    [InlineData("dvd://BluRayFolder")]
    [InlineData("bluray://DvdFolder")]
    [InlineData("dvd://../../Outside/Secret.iso")]
    [InlineData("dvd://../../MoviesOther/Disc.iso")]
    public void TryParse_InvalidDiscReference_IsDiscReferenceWithoutResult(string contents)
    {
        CreateFile(_movieFolder, "MyMovie.iso");
        CreateFile(_movieFolder, "Movie.mkv");
        Directory.CreateDirectory(Path.Combine(_movieFolder, "EmptyFolder"));
        Directory.CreateDirectory(Path.Combine(_movieFolder, "BluRayFolder", "BDMV"));
        Directory.CreateDirectory(Path.Combine(_movieFolder, "DvdFolder", "VIDEO_TS"));

        // Outside every library folder, including one that merely shares the library folder's name as a prefix.
        Directory.CreateDirectory(Path.Combine(_root, "Outside"));
        CreateFile(Path.Combine(_root, "Outside"), "Secret.iso");
        Directory.CreateDirectory(Path.Combine(_root, "MoviesOther"));
        CreateFile(Path.Combine(_root, "MoviesOther"), "Disc.iso");

        Assert.Null(AssertDiscReference(contents));
    }

    [Fact]
    public void TryRead_File_ParsesContents()
    {
        CreateFile(_movieFolder, "MyMovie.iso");
        File.WriteAllText(_strmPath, "dvd://MyMovie.iso?title=1\n");

        Assert.True(DiscReference.TryRead(_strmPath, Roots, NullLogger.Instance, out var reference));
        Assert.Equal(1, reference?.Index);
    }

    [Fact]
    public void TryRead_TooLarge_IsNotADiscReference()
    {
        File.WriteAllText(_strmPath, "dvd://MyMovie.iso" + new string('\n', DiscReference.MaxFileSize));

        Assert.False(DiscReference.TryRead(_strmPath, Roots, NullLogger.Instance, out _));
    }

    [Theory]
    [InlineData("/media/Movie.strm", true)]
    [InlineData("/media/Movie.STRM", true)]
    [InlineData("/media/Movie.iso", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsStrmPath_MatchesExtension(string? path, bool expected)
    {
        Assert.Equal(expected, DiscReference.IsStrmPath(path));
    }

    private DiscReference? AssertDiscReference(string contents)
    {
        Assert.True(DiscReference.TryParse(_strmPath, contents, Roots, NullLogger.Instance, out var reference));
        return reference;
    }

    private static string CreateFile(string folder, string name)
    {
        var path = Path.Combine(folder, name);
        File.WriteAllBytes(path, [0]);
        return path;
    }
}
