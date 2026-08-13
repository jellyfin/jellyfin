using System;
using Emby.Server.Implementations.Library;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library;

public class MediaStreamSelectorTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GetDefaultAudioStreamIndex_EmptyStreams_Null(bool preferDefaultTrack)
    {
        Assert.Null(MediaStreamSelector.GetDefaultAudioStreamIndex(Array.Empty<MediaStream>(), Array.Empty<string>(), preferDefaultTrack));
    }

    [Theory]
    [InlineData(new string[0], false, 1)]
    [InlineData(new string[0], true, 1)]
    [InlineData(new[] { "eng" }, false, 2)]
    [InlineData(new[] { "eng" }, true, 1)]
    [InlineData(new[] { "eng", "fre" }, false, 2)]
    [InlineData(new[] { "fre", "eng" }, false, 1)]
    [InlineData(new[] { "eng", "fre" }, true, 1)]
    public void GetDefaultAudioStreamIndex_PreferredLanguage_SelectsCorrect(string[] preferredLanguages, bool preferDefaultTrack, int expectedIndex)
    {
        var streams = new MediaStream[]
        {
            new()
            {
                Index = 0,
                Type = MediaStreamType.Video,
                IsDefault = true
            },
            new()
            {
                Index = 1,
                Type = MediaStreamType.Audio,
                Language = "fre",
                IsDefault = true
            },
            new()
            {
                Index = 2,
                Type = MediaStreamType.Audio,
                Language = "eng",
                IsDefault = false
            }
        };

        Assert.Equal(expectedIndex, MediaStreamSelector.GetDefaultAudioStreamIndex(streams, preferredLanguages, preferDefaultTrack));
    }

    public static TheoryData<MediaStream, int> GetStreamScore_MediaStream_TestData()
    {
        var data = new TheoryData<MediaStream, int>();

        data.Add(new MediaStream(), 111111);
        data.Add(
            new MediaStream()
            {
                Language = "eng"
            },
            10111111);
        data.Add(
            new MediaStream()
            {
                Language = "fre"
            },
            10011111);
        data.Add(
            new MediaStream()
            {
                IsForced = true
            },
            121111);
        data.Add(
            new MediaStream()
            {
                IsDefault = true
            },
            112111);
        data.Add(
            new MediaStream()
            {
                SupportsExternalStream = true
            },
            111211);
        data.Add(
            new MediaStream()
            {
                IsExternal = true
            },
            111112);
        data.Add(
            new MediaStream()
            {
                Language = "eng",
                IsForced = true,
                IsDefault = true,
                SupportsExternalStream = true,
                IsExternal = true
            },
            10122212);

        return data;
    }

    [Theory]
    [MemberData(nameof(GetStreamScore_MediaStream_TestData))]
    public void GetStreamScore_MediaStream_CorrectScore(MediaStream stream, int expectedScore)
    {
        var languagePref = new[] { "eng", "fre" };

        Assert.Equal(expectedScore, MediaStreamSelector.GetStreamScore(stream, languagePref));
    }

    [Theory]
    [InlineData(new[] { "eng", "swe" }, 2)] // eng preferred when both present
    [InlineData(new[] { "swe", "eng" }, 1)] // swe preferred when both present
    public void GetDefaultSubtitleStreamIndex_Always_PrefersEarlierLanguage(
        string[] preferredLanguages,
        int expectedIndex)
    {
        var streams = new MediaStream[]
        {
            new() { Index = 0, Type = MediaStreamType.Video },
            new() { Index = 1, Type = MediaStreamType.Subtitle, Language = "swe", IsForced = false },
            new() { Index = 2, Type = MediaStreamType.Subtitle, Language = "eng", IsForced = false },
        };

        Assert.Equal(
            expectedIndex,
            MediaStreamSelector.GetDefaultSubtitleStreamIndex(
                streams,
                preferredLanguages,
                SubtitlePlaybackMode.Always,
                "jpn"));
    }

    [Fact]
    public void GetDefaultSubtitleStreamIndex_Always_FallsBackWhenPreferredMissing()
    {
        var streams = new MediaStream[]
        {
            new() { Index = 0, Type = MediaStreamType.Video },
            new() { Index = 1, Type = MediaStreamType.Subtitle, Language = "swe", IsForced = false },
        };

        Assert.Equal(
            1,
            MediaStreamSelector.GetDefaultSubtitleStreamIndex(
                streams,
                new[] { "eng", "swe" },
                SubtitlePlaybackMode.Always,
                "jpn"));
    }

    [Fact]
    public void GetDefaultSubtitleStreamIndex_Always_FallsBackToAnyNonForcedWhenPreferredAbsent()
    {
        // Single preferred language missing → still enable a full track (not OnlyForced/none).
        var streams = new MediaStream[]
        {
            new() { Index = 0, Type = MediaStreamType.Video },
            new() { Index = 1, Type = MediaStreamType.Subtitle, Language = "eng", IsForced = false },
            new() { Index = 2, Type = MediaStreamType.Subtitle, Language = "eng", IsForced = true },
        };

        Assert.Equal(
            1,
            MediaStreamSelector.GetDefaultSubtitleStreamIndex(
                streams,
                new[] { "swe" },
                SubtitlePlaybackMode.Always,
                "jpn"));
    }

    [Fact]
    public void GetDefaultSubtitleStreamIndex_Smart_FallsBackWhenPreferredMissing()
    {
        var streams = new MediaStream[]
        {
            new() { Index = 0, Type = MediaStreamType.Video },
            new() { Index = 1, Type = MediaStreamType.Subtitle, Language = "swe", IsForced = false },
        };

        Assert.Equal(
            1,
            MediaStreamSelector.GetDefaultSubtitleStreamIndex(
                streams,
                new[] { "eng", "swe" },
                SubtitlePlaybackMode.Smart,
                "jpn"));
    }
}
