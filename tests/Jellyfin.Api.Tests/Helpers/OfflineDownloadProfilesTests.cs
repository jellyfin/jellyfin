using System;
using System.Linq;
using Jellyfin.Api.Controllers;
using Jellyfin.Api.Helpers;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers;

public static class OfflineDownloadProfilesTests
{
    [Theory]
    [InlineData("540p", 960, 540, 600000, 96000)]
    [InlineData("720p", 1280, 720, 1000000, 96000)]
    [InlineData("1080p", 1920, 1080, 2000000, 128000)]
    public static void Get_ReturnsBoundedPreset(string name, int width, int height, int video, int audio)
    {
        var preset = OfflineDownloadProfiles.Get(name);
        Assert.NotNull(preset);
        Assert.Equal(width, preset.MaxWidth);
        Assert.Equal(height, preset.MaxHeight);
        Assert.Equal(video, preset.VideoBitRate);
        Assert.Equal(audio, preset.AudioBitRate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("original")]
    [InlineData("720P")]
    [InlineData("720p&videoBitRate=100000000")]
    public static void Get_RejectsUnknownPreset(string? name)
    {
        Assert.Null(OfflineDownloadProfiles.Get(name));
    }

    [Theory]
    [InlineData("/data/tv/episode.mkv", "episode - 720p.mp4")]
    [InlineData("C:\\private\\episode.mkv", "episode - 720p.mp4")]
    [InlineData("/data/a\r\n\"b?.mkv", "a___b_ - 720p.mp4")]
    [InlineData("/data/.mkv", "video - 720p.mp4")]
    public static void CreateFileName_DropsPathsAndUnsafeCharacters(string input, string expected)
    {
        Assert.Equal(expected, OfflineDownloadProfiles.CreateFileName(input, "720p"));
    }

    [Fact]
    public static void CreateFileName_RejectsUnknownPreset()
    {
        Assert.Throws<ArgumentException>(() => OfflineDownloadProfiles.CreateFileName("episode.mkv", "original"));
    }

    [Fact]
    public static void DownloadEndpoint_RequiresExistingDownloadPolicy()
    {
        var method = typeof(VideosController).GetMethod(nameof(VideosController.DownloadTranscoded));
        Assert.NotNull(method);
        var attributes = method.GetCustomAttributes(typeof(AuthorizeAttribute), false).Cast<AuthorizeAttribute>();
        Assert.Contains(attributes, attribute => attribute.Policy == Policies.Download);
    }
}
