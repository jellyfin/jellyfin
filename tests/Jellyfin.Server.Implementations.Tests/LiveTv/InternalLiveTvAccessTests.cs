using System;
using System.Net;
using Emby.Server.Implementations.LiveTv;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Net;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.LiveTv;

public sealed class InternalLiveTvAccessTests
{
    private const string LocalApi = "http://127.0.0.1:8096";
    private const string StreamPath = "/LiveTv/LiveStreamFiles/0123456789abcdef/stream.ts";

    [Theory]
    [InlineData("/LiveTv/LiveStreamFiles/0123456789abcdef/stream.ts")]
    [InlineData("/LiveTv/LiveRecordings/recording-42/stream")]
    public void GetInputUrl_LocalResourceProducesAValidCapability(string path)
    {
        var access = CreateAccess();

        var signed = access.GetInputUrl(LocalApi + path);

        Assert.NotEqual(LocalApi + path, signed);
        Assert.True(access.IsAuthorizedRequest(CreateRequest(signed)));
        Assert.Equal(signed, access.GetInputUrl(signed));
    }

    [Fact]
    public void IsAuthorizedRequest_BaseUrlAndPathBaseAreIncludedInTheCapability()
    {
        var access = CreateAccess(LocalApi + "/jellyfin");
        var signed = access.GetInputUrl(LocalApi + "/jellyfin" + StreamPath);
        var request = CreateRequest(signed);
        request.PathBase = "/jellyfin";
        request.Path = StreamPath;

        Assert.True(access.IsAuthorizedRequest(request));
        request.PathBase = "/other";
        Assert.False(access.IsAuthorizedRequest(request));
    }

    [Theory]
    [InlineData("http://other-server:8096/LiveTv/LiveStreamFiles/id/stream.ts")]
    [InlineData("https://127.0.0.1:8096/LiveTv/LiveStreamFiles/id/stream.ts")]
    [InlineData("http://127.0.0.1:8097/LiveTv/LiveStreamFiles/id/stream.ts")]
    [InlineData("http://user:password@127.0.0.1:8096/LiveTv/LiveStreamFiles/id/stream.ts")]
    [InlineData("http://127.0.0.1:8096/LiveTv/LiveStreamFiles/id/stream.ts?ApiKey=client")]
    [InlineData("http://127.0.0.1:8096/LiveTv/LiveStreamFiles/id/stream.ts#fragment")]
    [InlineData("http://127.0.0.1:8096/LiveTv/LiveStreamFiles/a%2Fb/stream.ts")]
    [InlineData("http://127.0.0.1:8096/LiveTv/LiveStreamFiles/id/other.ts")]
    [InlineData("http://127.0.0.1:8096/LiveTv/LiveStreamFiles/id/stream.ts/extra")]
    [InlineData("http://127.0.0.1:8096/LiveTv/LiveStreamFiles/id/../other/stream.ts")]
    [InlineData("http://127.0.0.1:8096/LiveTv//LiveStreamFiles/id/stream.ts")]
    [InlineData("http://127.0.0.1:8096/Items/id/File")]
    [InlineData("/LiveTv/LiveStreamFiles/id/stream.ts")]
    [InlineData("/recordings/recording.ts")]
    public void GetInputUrl_NonInternalOrAmbiguousAddressRemainsUnchanged(string address)
    {
        Assert.Equal(address, CreateAccess().GetInputUrl(address));
    }

    [Fact]
    public void IsAuthorizedRequest_CapabilityCannotBeTransferredToAnotherResource()
    {
        var access = CreateAccess();
        var signed = access.GetInputUrl(LocalApi + StreamPath);
        var request = CreateRequest(signed);
        request.Path = StreamPath.Replace("0123456789abcdef", "other-resource", StringComparison.Ordinal);

        Assert.False(access.IsAuthorizedRequest(request));
        request.Path = StreamPath.ToUpperInvariant();
        Assert.False(access.IsAuthorizedRequest(request));
    }

    [Fact]
    public void IsAuthorizedRequest_CapabilityDoesNotSurviveAnotherServerInstance()
    {
        var issuer = CreateAccess();
        var signed = issuer.GetInputUrl(LocalApi + StreamPath);

        Assert.True(issuer.IsAuthorizedRequest(CreateRequest(signed)));
        Assert.False(CreateAccess().IsAuthorizedRequest(CreateRequest(signed)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("?ApiKey=client-token")]
    [InlineData("?InternalLiveTvToken=invalid")]
    [InlineData("?InternalLiveTvToken=zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz")]
    [InlineData("?InternalLiveTvToken=0000000000000000000000000000000000000000000000000000000000000000")]
    public void IsAuthorizedRequest_ClientOrLocalNetworkDoesNotReplaceACapability(string query)
    {
        var context = new DefaultHttpContext();
        context.Connection.LocalIpAddress = IPAddress.Loopback;
        context.Connection.RemoteIpAddress = IPAddress.Loopback;
        context.Request.Path = StreamPath;
        context.Request.QueryString = new QueryString(query);
        context.Request.Headers.Authorization = "MediaBrowser Token=client-token";

        Assert.False(CreateAccess().IsAuthorizedRequest(context.Request));
    }

    [Fact]
    public void IsAuthorizedRequest_RepeatedTokenIsRejected()
    {
        var access = CreateAccess();
        var signed = access.GetInputUrl(LocalApi + StreamPath);
        var request = CreateRequest(signed);
        request.QueryString = new QueryString(request.QueryString.Value + "&" + IInternalLiveTvAccess.TokenQueryParameter + "=other");

        Assert.False(access.IsAuthorizedRequest(request));
    }

    [Fact]
    public void GetInputUrl_CanonicalizesExistingInternalCapabilities()
    {
        var access = CreateAccess();
        var expected = access.GetInputUrl(LocalApi + StreamPath);
        var invalid = LocalApi + StreamPath + "?InternalLiveTvToken=invalid&InternalLiveTvToken=other";

        Assert.Equal(expected, access.GetInputUrl(invalid));
        Assert.True(access.IsAuthorizedRequest(CreateRequest(access.GetInputUrl(invalid))));
    }

    [Fact]
    public void Redact_RemovesOnlyInternalCapabilityQueryValues()
    {
        const string original = "-i \"http://server/raw?InternalLiveTvToken=secret&other=value\" message http://server/raw?other=value&internallivetvtoken=another";
        const string expected = "-i \"http://server/raw?InternalLiveTvToken=[redacted]&other=value\" message http://server/raw?other=value&internallivetvtoken=[redacted]";

        Assert.Equal(expected, InternalLiveTvAccessToken.Redact(original));
    }

    private static InternalLiveTvAccess CreateAccess(string localApi = LocalApi)
    {
        var host = new Mock<IServerApplicationHost>();
        host.Setup(value => value.GetApiUrlForLocalAccess(null, true)).Returns(localApi);
        return new InternalLiveTvAccess(host.Object);
    }

    private static HttpRequest CreateRequest(string address)
    {
        var uri = new Uri(address);
        var context = new DefaultHttpContext();
        context.Request.Path = uri.AbsolutePath;
        context.Request.QueryString = new QueryString(uri.Query);
        return context.Request;
    }
}
