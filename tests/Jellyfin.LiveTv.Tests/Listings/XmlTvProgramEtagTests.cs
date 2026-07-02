using System;
using Jellyfin.LiveTv.Listings;
using MediaBrowser.Controller.LiveTv;
using Xunit;

namespace Jellyfin.LiveTv.Tests.Listings;

public class XmlTvProgramEtagTests
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

        Assert.True(XmlTvProgramEtag.TryCreate(first, out var firstEtag, out _));
        Assert.True(XmlTvProgramEtag.TryCreate(second, out var secondEtag, out _));
        Assert.NotEqual(firstEtag, secondEtag);
    }

    [Fact]
    public void MatchesStored_EqualXmlTvEtags_ReturnsTrue()
    {
        const string Etag = XmlTvProgramEtag.Prefix + "ABCDEF0123456789";
        Assert.True(XmlTvProgramEtag.MatchesStored(Etag, Etag));
    }

    [Fact]
    public void MatchesStored_DifferentXmlTvEtags_ReturnsFalse()
    {
        Assert.False(XmlTvProgramEtag.MatchesStored(
            XmlTvProgramEtag.Prefix + "AAAA",
            XmlTvProgramEtag.Prefix + "BBBB"));
    }

    [Fact]
    public void MatchesStored_EqualNonXmlTvEtags_ReturnsFalse()
    {
        // Other providers (e.g. Schedules Direct) use their own etag schemes.
        // The IsXmlTvEtag gate must keep them on the field-by-field update path
        // even when their incoming and stored values happen to match exactly.
        const string Etag = "sd-abc123";
        Assert.False(XmlTvProgramEtag.MatchesStored(Etag, Etag));
    }

    private static ProgramInfo NewProgram() => new()
    {
        Id = "program-id",
        ChannelId = "channel-id",
        Name = "Program Name",
        StartDate = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
        EndDate = new DateTime(2026, 1, 1, 13, 0, 0, DateTimeKind.Utc),
    };
}
