using System;
using System.IO;
using Jellyfin.Api.Helpers;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers;

public class HlsHelpersTests
{
    private static readonly string _playlistPath = Path.Combine(Path.GetTempPath(), "owned-transcodes", "list.m3u8");

    [Theory]
    [InlineData("list0.ts")]
    [InlineData("hls/list/list12.ts")]
    [InlineData("list-1.mp4")]
    public void AuthenticatePlaylistSegments_AuthenticatesOnlyOwnedSegmentUris(string uri)
    {
        var output = HlsHelpers.AuthenticatePlaylistSegments(uri, _playlistPath, "owner-token");

        Assert.Equal(uri + "?ApiKey=owner-token", output);
    }

    [Fact]
    public void AuthenticatePlaylistSegments_AuthenticatesFmp4InitializationMap()
    {
        var output = HlsHelpers.AuthenticatePlaylistSegments("#EXT-X-MAP:URI=\"list-1.mp4\",BYTERANGE=\"100@0\"", _playlistPath, "owner-token");

        Assert.Equal("#EXT-X-MAP:URI=\"list-1.mp4?ApiKey=owner-token\",BYTERANGE=\"100@0\"", output);
    }

    [Theory]
    [InlineData("https://external.example/list0.ts")]
    [InlineData("//external.example/list0.ts")]
    [InlineData("../list0.ts")]
    [InlineData("hls/other/list0.ts")]
    [InlineData("other0.ts")]
    [InlineData("list-other0.ts")]
    [InlineData("hls/list/../list0.ts")]
    public void AuthenticatePlaylistSegments_NeverForwardsTokenToExternalOrUnrelatedPaths(string uri)
    {
        Assert.Equal(uri, HlsHelpers.AuthenticatePlaylistSegments(uri, _playlistPath, "private-token"));
    }

    [Fact]
    public void AuthenticatePlaylistSegments_ReplacesUnverifiedTokenWithoutDuplicateQueryValues()
    {
        var output = HlsHelpers.AuthenticatePlaylistSegments("list0.ts?ApiKey=other-token&option=1", _playlistPath, "owner+token");
        var query = QueryHelpers.ParseQuery(output[output.IndexOf('?', StringComparison.Ordinal)..]);

        Assert.Equal("owner+token", Assert.Single(query["ApiKey"]));
        Assert.Equal("1", query["option"].ToString());
    }

    [Fact]
    public void GetAuthenticatedPlaylistQuery_PropagatesResolvedAliasAndVerifiedToken()
    {
        var output = HlsHelpers.GetAuthenticatedPlaylistQuery("?ApiKey=unverified&PlaySessionId=obsolete&VideoCodec=copy", "resolved-encoding", "owner-token");
        var query = QueryHelpers.ParseQuery(output);

        Assert.Equal("owner-token", Assert.Single(query["ApiKey"]));
        Assert.Equal("resolved-encoding", Assert.Single(query["PlaySessionId"]));
        Assert.Equal("copy", query["VideoCodec"].ToString());
    }
}
