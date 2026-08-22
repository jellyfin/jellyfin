using MediaBrowser.Providers.MediaInfo;
using Xunit;

namespace Jellyfin.Providers.Tests.MediaInfo;

public class AudioFileProberTests
{
    private const char Sep = '';

    [Fact]
    public void AlignMusicBrainzIds_OneIdPerArtist_AlignsThem()
    {
        var ids = AudioFileProber.AlignMusicBrainzIds($"id-a{Sep}id-b", 2, null, []);

        Assert.NotNull(ids);
        Assert.Equal(["id-a", "id-b"], ids);
    }

    [Fact]
    public void AlignMusicBrainzIds_SingleArtist_AlignsIt()
    {
        var ids = AudioFileProber.AlignMusicBrainzIds("id-a", 1, null, []);

        Assert.NotNull(ids);
        Assert.Equal(["id-a"], ids);
    }

    [Theory]
    // Taking the first id for everyone would merge the whole credit list into one artist.
    [InlineData("id-a", 2)]
    [InlineData("id-aid-b", 3)]
    [InlineData("id-aid-bid-c", 2)]
    public void AlignMusicBrainzIds_CountsDisagree_AlignsNothing(string tag, int artistCount)
    {
        Assert.Null(AudioFileProber.AlignMusicBrainzIds(tag, artistCount, null, []));
    }

    [Fact]
    public void AlignMusicBrainzIds_BlankEntry_AlignsNothing()
    {
        Assert.Null(AudioFileProber.AlignMusicBrainzIds($"id-a{Sep} ", 2, null, []));
    }

    [Fact]
    public void AlignMusicBrainzIds_NoArtists_AlignsNothing()
    {
        Assert.Null(AudioFileProber.AlignMusicBrainzIds("id-a", 0, null, []));
    }

    [Fact]
    public void AlignMusicBrainzIds_CustomDelimiter_SplitsBeforeCounting()
    {
        var ids = AudioFileProber.AlignMusicBrainzIds("id-a;id-b", 2, [';'], []);

        Assert.NotNull(ids);
        Assert.Equal(["id-a", "id-b"], ids);
    }
}
