using System.Collections.Generic;
using MediaBrowser.Controller.Extensions;
using Microsoft.Extensions.Configuration;
using Xunit;

using JellyfinConfig = MediaBrowser.Controller.Extensions.ConfigurationExtensions;

namespace Jellyfin.Controller.Tests.Extensions;

public class ConfigurationExtensionsTests
{
    [Fact]
    public void GetFFmpegPlaybackProbeSize_NeitherKeySet_ReturnsNull()
    {
        Assert.Null(BuildConfig().GetFFmpegPlaybackProbeSize());
    }

    [Fact]
    public void GetFFmpegPlaybackProbeSize_OnlySharedKeySet_FallsBackToSharedValue()
    {
        var config = BuildConfig((JellyfinConfig.FfmpegProbeSizeKey, "1G"));

        Assert.Equal("1G", config.GetFFmpegPlaybackProbeSize());
    }

    [Fact]
    public void GetFFmpegPlaybackProbeSize_OnlyPlaybackKeySet_ReturnsPlaybackValue()
    {
        var config = BuildConfig((JellyfinConfig.FfmpegPlaybackProbeSizeKey, "50M"));

        Assert.Equal("50M", config.GetFFmpegPlaybackProbeSize());
    }

    [Fact]
    public void GetFFmpegPlaybackProbeSize_PlaybackKeyEmpty_FallsBackToSharedValue()
    {
        // An empty value is how a user clears an environment variable, so treat it as unset
        // rather than letting it suppress the probe size entirely.
        var config = BuildConfig(
            (JellyfinConfig.FfmpegProbeSizeKey, "1G"),
            (JellyfinConfig.FfmpegPlaybackProbeSizeKey, string.Empty));

        Assert.Equal("1G", config.GetFFmpegPlaybackProbeSize());
    }

    [Fact]
    public void GetFFmpegPlaybackProbeSize_BothKeysSet_PlaybackKeyWins()
    {
        var config = BuildConfig(
            (JellyfinConfig.FfmpegProbeSizeKey, "1G"),
            (JellyfinConfig.FfmpegPlaybackProbeSizeKey, "50M"));

        Assert.Equal("50M", config.GetFFmpegPlaybackProbeSize());
    }

    [Fact]
    public void GetFFmpegProbeSize_PlaybackKeySet_IsUnaffected()
    {
        var config = BuildConfig(
            (JellyfinConfig.FfmpegProbeSizeKey, "1G"),
            (JellyfinConfig.FfmpegPlaybackProbeSizeKey, "50M"));

        Assert.Equal("1G", config.GetFFmpegProbeSize());
    }

    private static IConfiguration BuildConfig(params (string Key, string? Value)[] values)
    {
        var items = new List<KeyValuePair<string, string?>>();
        foreach (var (key, value) in values)
        {
            items.Add(new KeyValuePair<string, string?>(key, value));
        }

        return new ConfigurationBuilder().AddInMemoryCollection(items).Build();
    }
}
