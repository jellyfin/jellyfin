using System;
using Jellyfin.LiveTv.Listings;
using MediaBrowser.Controller.LiveTv;
using Xunit;

namespace Jellyfin.LiveTv.Tests.Listings;

public class ProgramEtagTests
{
    [Fact]
    public void TryCreate_GenreOrderIsSignificant()
    {
        // GuideManager assigns item.Genres = info.Genres.ToArray() preserving order,
        // so the same genres in a different order is a real mapped-content change.
        var first = NewProgram();
        first.Genres = new() { "Drama", "Action" };

        var second = NewProgram();
        second.Genres = new() { "Action", "Drama" };

        Assert.True(ProgramEtag.TryCreate(first, out var firstEtag, out _));
        Assert.True(ProgramEtag.TryCreate(second, out var secondEtag, out _));
        Assert.NotEqual(firstEtag, secondEtag);
    }

    [Fact]
    public void MatchesStored_EqualOwnEtags_ReturnsTrue()
    {
        const string Etag = ProgramEtag.Prefix + "ABCDEF0123456789";
        Assert.True(ProgramEtag.MatchesStored(Etag, Etag));
    }

    [Fact]
    public void MatchesStored_DifferentOwnEtags_ReturnsFalse()
    {
        Assert.False(ProgramEtag.MatchesStored(
            ProgramEtag.Prefix + "AAAA",
            ProgramEtag.Prefix + "BBBB"));
    }

    [Fact]
    public void MatchesStored_EqualForeignEtags_ReturnsFalse()
    {
        // A provider's own etag scheme (e.g. Schedules Direct's) is not known to cover every field
        // GuideManager maps, so the IsProgramEtag gate must keep it on the field-by-field update
        // path even when the incoming and stored values happen to match exactly.
        const string Etag = "sd-abc123";
        Assert.False(ProgramEtag.MatchesStored(Etag, Etag));
    }

    [Fact]
    public void TryCreate_SameProgramTwice_ProducesMatchingEtag()
    {
        // The whole unchanged-program fast path rests on this being reproducible across refreshes,
        // including for providers that supply no etag of their own.
        Assert.True(ProgramEtag.TryCreate(NewProgram(), out var first, out _));
        Assert.True(ProgramEtag.TryCreate(NewProgram(), out var second, out _));

        Assert.Equal(first, second);
        Assert.True(ProgramEtag.IsProgramEtag(first));
        Assert.True(ProgramEtag.MatchesStored(second, first));
    }

    [Fact]
    public void TryCreate_ChangedImageUrl_ProducesDifferentEtag()
    {
        // Image URLs are mapped onto the item, so a change must not be hidden by the fast path.
        var first = NewProgram();
        first.ImageUrl = "https://example.com/a.jpg";

        var second = NewProgram();
        second.ImageUrl = "https://example.com/b.jpg";

        Assert.True(ProgramEtag.TryCreate(first, out var firstEtag, out _));
        Assert.True(ProgramEtag.TryCreate(second, out var secondEtag, out _));
        Assert.NotEqual(firstEtag, secondEtag);
    }

    [Theory]
    [InlineData("")]
    [InlineData("sd-abc123")]
    public void IsProgramEtag_ForeignOrEmptyEtag_ReturnsFalse(string etag)
        => Assert.False(ProgramEtag.IsProgramEtag(etag));

    private static ProgramInfo NewProgram() => new()
    {
        Id = "program-id",
        ChannelId = "channel-id",
        Name = "Program Name",
        StartDate = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
        EndDate = new DateTime(2026, 1, 1, 13, 0, 0, DateTimeKind.Utc),
    };
}
