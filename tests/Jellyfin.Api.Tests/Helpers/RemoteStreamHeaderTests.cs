using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Helpers;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers;

public sealed class RemoteStreamHeaderTests
{
    [Fact]
    public async Task GetStaticRemoteStreamResult_ForwardsSourceCredentialsWithoutCallerCredentialsOrTransportHeaders()
    {
        using var state = CreateState();
        state.RemoteHttpHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Authorization"] = "Bearer upstream-secret",
            ["Referer"] = "https://origin.example/player",
            ["Cookie"] = "origin-session=secret",
            ["User-Agent"] = "Source player",
            ["Range"] = "bytes=0-100",
            ["Host"] = "different-host.example",
            ["Connection"] = "keep-alive, Source-Transport",
            ["Source-Transport"] = "must-not-forward",
            ["Transfer-Encoding"] = "chunked",
            ["Content-Length"] = "999"
        };
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "MediaBrowser Token=caller-jellyfin-token";
        context.Request.Headers.Range = "bytes=4-7";
        context.Request.QueryString = new QueryString("?ApiKey=caller-jellyfin-token");
        using var handler = new InspectingHandler(request =>
        {
            Assert.Equal("Bearer upstream-secret", request.Headers.Authorization?.ToString());
            Assert.Equal("https://origin.example/player", request.Headers.Referrer?.AbsoluteUri);
            Assert.Contains("origin-session=secret", request.Headers.GetValues("Cookie"));
            Assert.Equal("Source player", request.Headers.UserAgent.ToString());
            Assert.Equal("bytes=4-7", request.Headers.Range?.ToString());
            Assert.Null(request.Headers.Host);
            Assert.False(request.Headers.Contains("Connection"));
            Assert.False(request.Headers.Contains("Source-Transport"));
            Assert.False(request.Headers.Contains("Transfer-Encoding"));
            Assert.DoesNotContain("caller-jellyfin-token", request.ToString(), StringComparison.Ordinal);
            return new HttpResponseMessage(HttpStatusCode.PartialContent) { Content = new ByteArrayContent(new byte[] { 4, 5, 6, 7 }) };
        });
        using var client = new HttpClient(handler);

        var result = await FileStreamResponseHelpers.GetStaticRemoteStreamResult(state, client, context, TestContext.Current.CancellationToken);

        var file = Assert.IsType<FileStreamResult>(result);
        await using var stream = file.FileStream;
        Assert.Equal(206, context.Response.StatusCode);
        Assert.Equal(1, handler.Requests);
    }

    [Fact]
    public async Task GetStaticRemoteStreamResult_CanceledClientDoesNotStartAnUpstreamRequest()
    {
        using var state = CreateState();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        await cancellation.CancelAsync();
        var context = new DefaultHttpContext { RequestAborted = cancellation.Token };
        using var handler = new InspectingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(handler);

        // Exercise the default-token overload: cancellation must come from RequestAborted.
#pragma warning disable xUnit1051
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => FileStreamResponseHelpers.GetStaticRemoteStreamResult(state, client, context));
#pragma warning restore xUnit1051

        Assert.Equal(0, handler.Requests);
    }

    private static StreamState CreateState()
        => new(Mock.Of<IMediaSourceManager>(), TranscodingJobType.Progressive, Mock.Of<ITranscodeManager>())
        {
            MediaPath = "https://origin.example/video.mp4",
            MediaSource = new MediaSourceInfo(),
            Request = new StreamingRequestDto()
        };

    private sealed class InspectingHandler(Func<HttpRequestMessage, HttpResponseMessage> inspect) : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Requests++;
            return Task.FromResult(inspect(request));
        }
    }
}
