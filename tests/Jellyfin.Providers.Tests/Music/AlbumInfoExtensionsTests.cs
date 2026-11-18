using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Providers.Music;
using Xunit;

namespace Jellyfin.Providers.Tests.Music;

public static class AlbumInfoExtensionsTests
{
    private const string ExampleMbid = "59b5a40b-e2fd-3f18-a218-e8c9aae12ab5";
    private const string SongMbid = "6c301dbd-6ccb-3403-a6c4-6a22240a0297";

    [Theory]
    [InlineData(ExampleMbid, ExampleMbid)]
    // Another provider's id under a MusicBrainz key reads as no id, so the caller searches instead of
    // handing a value the MusicBrainz client throws on.
    [InlineData("111239", null)]
    [InlineData("", null)]
    public static void GetReleaseId_OnlyReturnsMbids(string id, string? expected)
    {
        var info = new AlbumInfo();
        info.ProviderIds[MetadataProvider.MusicBrainzAlbum.ToString()] = id;

        Assert.Equal(expected, info.GetReleaseId());
    }

    [Fact]
    public static void GetReleaseId_ForeignId_FallsBackToSongs()
    {
        var song = new SongInfo();
        song.ProviderIds[MetadataProvider.MusicBrainzAlbum.ToString()] = SongMbid;

        var info = new AlbumInfo { SongInfos = [song] };
        info.ProviderIds[MetadataProvider.MusicBrainzAlbum.ToString()] = "111239";

        Assert.Equal(SongMbid, info.GetReleaseId());
    }

    [Fact]
    public static void GetMusicBrainzArtistId_ForeignId_FallsBackToArtistIds()
    {
        var info = new AlbumInfo();
        info.ProviderIds[MetadataProvider.MusicBrainzAlbumArtist.ToString()] = "111239";
        info.ArtistProviderIds[MetadataProvider.MusicBrainzArtist.ToString()] = ExampleMbid;

        Assert.Equal(ExampleMbid, info.GetMusicBrainzArtistId());
    }

    [Theory]
    [InlineData(ExampleMbid, ExampleMbid)]
    [InlineData("111239", null)]
    public static void GetMusicBrainzArtistId_ArtistInfo_OnlyReturnsMbids(string id, string? expected)
    {
        var info = new ArtistInfo();
        info.ProviderIds[MetadataProvider.MusicBrainzArtist.ToString()] = id;

        Assert.Equal(expected, info.GetMusicBrainzArtistId());
    }
}
