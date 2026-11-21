using System;
using System.Linq;
using Jellyfin.Api.Controllers;
using Jellyfin.Api.Helpers;
using MediaBrowser.Common.Api;
using MediaBrowser.Model.Entities;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers;

public static class OfflineDownloadTracksTests
{
    [Theory]
    [InlineData("srt")]
    [InlineData("subrip")]
    [InlineData("ass")]
    [InlineData("ssa")]
    [InlineData("webvtt")]
    [InlineData("mov_text")]
    [InlineData("SRT")]
    public static void TextTracks_CanEmbedAndBurn(string codec)
    {
        var track = new MediaStream { Index = 4, Type = MediaStreamType.Subtitle, Codec = codec };
        Assert.True(OfflineDownloadTracks.CanEmbed(track));
        Assert.True(OfflineDownloadTracks.CanBurn(track));
        Assert.Null(OfflineDownloadTracks.Validate(new[] { track }, 4, "embed"));
        Assert.Null(OfflineDownloadTracks.Validate(new[] { track }, 4, "burn"));
    }

    [Theory]
    [InlineData("pgssub")]
    [InlineData("hdmv_pgs_subtitle")]
    [InlineData("dvdsub")]
    [InlineData("dvbsub")]
    public static void ImageTracks_RequireExplicitBurn(string codec)
    {
        var track = new MediaStream { Index = 7, Type = MediaStreamType.Subtitle, Codec = codec };
        Assert.False(OfflineDownloadTracks.CanEmbed(track));
        Assert.True(OfflineDownloadTracks.CanBurn(track));
        Assert.NotNull(OfflineDownloadTracks.Validate(new[] { track }, 7, "embed"));
        Assert.Null(OfflineDownloadTracks.Validate(new[] { track }, 7, "burn"));
    }

    [Theory]
    [InlineData("embed", null)]
    [InlineData("embed", -1)]
    [InlineData("burn", 88)]
    [InlineData("none", 2)]
    [InlineData("copy", 2)]
    [InlineData("embed&codec=unsafe", 2)]
    public static void InvalidSelections_AreRejected(string mode, int? index)
    {
        var streams = new[] { new MediaStream { Index = 2, Type = MediaStreamType.Subtitle, Codec = "srt" } };
        Assert.NotNull(OfflineDownloadTracks.Validate(streams, index, mode));
    }

    [Fact]
    public static void AudioCannotBeSelectedAsSubtitle()
    {
        var streams = new[] { new MediaStream { Index = 2, Type = MediaStreamType.Audio, Codec = "srt" } };
        Assert.NotNull(OfflineDownloadTracks.Validate(streams, 2, "embed"));
    }

    [Fact]
    public static void NoneWorksWithoutSubtitleTracks()
    {
        Assert.Null(OfflineDownloadTracks.Validate(Array.Empty<MediaStream>(), null, "none"));
        Assert.Null(OfflineDownloadTracks.Validate(Array.Empty<MediaStream>(), -1, "none"));
    }

    [Fact]
    public static void OptionsEndpoint_RequiresDownloadPolicy()
    {
        var method = typeof(VideosController).GetMethod(nameof(VideosController.GetOfflineDownloadOptions));
        Assert.NotNull(method);
        var attributes = method.GetCustomAttributes(typeof(AuthorizeAttribute), false).Cast<AuthorizeAttribute>();
        Assert.Contains(attributes, attribute => attribute.Policy == Policies.Download);
    }
}
