using MediaBrowser.Controller.Entities.Audio;
using Xunit;

namespace Jellyfin.Controller.Tests.Entities;

/// <summary>
/// Covers <see cref="MediaBrowser.Controller.Entities.Audio.Extensions.GetAllArtists{T}(T)"/>, an
/// extension method for classes implementing <see cref="IHasAlbumArtist"/> and <see cref="IHasArtist"/>.
/// It returns a distinct sequence of all non-blank artists from both interfaces.
/// </summary>
public class AudioExtensionsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void GetAllArtists_BlankAlbumArtists_ReturnsEmpty(string? artistName)
    {
        var album = new MusicAlbum { AlbumArtists = [artistName] };

        Assert.Empty(album.GetAllArtists());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void GetAllArtists_BlankArtists_ReturnsEmpty(string? artistName)
    {
        var album = new MusicAlbum { Artists = [artistName] };

        Assert.Empty(album.GetAllArtists());
    }

    [Fact]
    public void GetAllArtists_BlankAndValidArtists_ReturnsValidArtists()
    {
        var album = new MusicAlbum
        {
            AlbumArtists = ["Artist1", null, " "],
            Artists = ["Artist2", string.Empty, "\t"]
        };

        Assert.Equal(["Artist1", "Artist2"], album.GetAllArtists());
    }
}
