using System;
using System.Collections.Generic;
using MediaBrowser.Providers.Plugins.MusicBrainz;
using MetaBrainz.MusicBrainz.Interfaces.Entities;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.Music;

public static class MusicBrainzAlbumProviderTests
{
    [Theory]
    // The tags use the credited name.
    [InlineData("Trio X of Sweden", "Trio X", "Trio X of Sweden", "Trio X of Sweden")]
    // The tags use the artist's name.
    [InlineData("Beethoven", "Ludwig van Beethoven", "Ludwig van Beethoven", "Ludwig van Beethoven")]
    // The tags use neither.
    [InlineData("Beethoven", "Ludwig van Beethoven", "Glenn Gould", "Ludwig van Beethoven")]
    [InlineData("Beethoven", null, "Glenn Gould", "Beethoven")]
    public static void GetAlbumArtistName_MatchesTags(string credited, string? artistName, string tagged, string expected)
    {
        var artist = artistName is null ? null : Mock.Of<IArtist>(a => a.Name == artistName);
        var credit = Mock.Of<INameCredit>(c => c.Name == credited && c.Artist == artist);
        var tags = new HashSet<string>([tagged], StringComparer.OrdinalIgnoreCase);

        Assert.Equal(expected, MusicBrainzAlbumProvider.GetAlbumArtistName(credit, tags));
    }
}
