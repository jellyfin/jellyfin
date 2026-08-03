using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace Jellyfin.LiveTv.Tests;

public class LiveTvChannelImageHelperTests
{
    private const string IconUrl = "https://example.com/icon.png";

    [Fact]
    public async Task UpdateChannelImageIfNeeded_NoSource_DoesNotUpdate()
    {
        var channel = new LiveTvChannel { Name = "Test Channel" };

        var updated = await LiveTvChannelImageHelper.UpdateChannelImageIfNeededAsync(
            channel,
            null,
            null,
            CreateHttpClientFactory(_ => new HttpResponseMessage(HttpStatusCode.NotModified)),
            NullLogger.Instance,
            CancellationToken.None);

        Assert.Equal(ChannelImageUpdate.None, updated);
        Assert.False(channel.HasImage(ImageType.Primary));
    }

    [Fact]
    public async Task UpdateChannelImageIfNeeded_NewChannelWithUrl_AppliesUrl()
    {
        var channel = new LiveTvChannel { Name = "Test Channel" };

        var updated = await LiveTvChannelImageHelper.UpdateChannelImageIfNeededAsync(
            channel,
            null,
            IconUrl,
            CreateHttpClientFactory(_ => throw new InvalidOperationException("No request expected for a new channel")),
            NullLogger.Instance,
            CancellationToken.None);

        Assert.Equal(ChannelImageUpdate.ImageChanged, updated);
        Assert.True(channel.HasImage(ImageType.Primary));
        Assert.Equal(IconUrl, channel.GetImagePath(ImageType.Primary));
    }

    [Fact]
    public async Task UpdateChannelImageIfNeeded_ChangedUrl_Updates()
    {
        var channel = new LiveTvChannel { Name = "Test Channel" };
        SeedCachedIcon(channel, IconUrl);

        const string NewUrl = "https://example.com/new-icon.png";
        var updated = await LiveTvChannelImageHelper.UpdateChannelImageIfNeededAsync(
            channel,
            null,
            NewUrl,
            CreateHttpClientFactory(_ => throw new InvalidOperationException("No request expected when the URL changed")),
            NullLogger.Instance,
            CancellationToken.None);

        Assert.Equal(ChannelImageUpdate.ImageChanged, updated);
        Assert.Equal(NewUrl, channel.GetImagePath(ImageType.Primary));
    }

    [Fact]
    public async Task UpdateChannelImageIfNeeded_SameUrlNotModified_DoesNotUpdate()
    {
        var channel = new LiveTvChannel { Name = "Test Channel" };
        SeedCachedIcon(channel, IconUrl, etag: "\"cached\"");

        var updated = await LiveTvChannelImageHelper.UpdateChannelImageIfNeededAsync(
            channel,
            null,
            IconUrl,
            CreateHttpClientFactory(_ => new HttpResponseMessage(HttpStatusCode.NotModified)),
            NullLogger.Instance,
            CancellationToken.None);

        Assert.Equal(ChannelImageUpdate.None, updated);
    }

    [Fact]
    public async Task UpdateChannelImageIfNeeded_SameUrlChangedContent_Updates()
    {
        var channel = new LiveTvChannel { Name = "Test Channel" };
        SeedCachedIcon(channel, IconUrl, etag: "\"old\"");

        var updated = await LiveTvChannelImageHelper.UpdateChannelImageIfNeededAsync(
            channel,
            null,
            IconUrl,
            CreateHttpClientFactory(_ => CreateResponse(etag: "\"new\"")),
            NullLogger.Instance,
            CancellationToken.None);

        Assert.Equal(ChannelImageUpdate.ImageChanged, updated);
        Assert.Equal("\"new\"", channel.GetImageInfo(ImageType.Primary, 0)!.ETag);
    }

    [Fact]
    public async Task UpdateChannelImageIfNeeded_SameUrlStableETagFlakyLastModified_DoesNotUpdate()
    {
        // A stable strong ETag is authoritative even if Last-Modified differs (common on CDNs). Drift in
        // the already-stored Last-Modified must not even request a save, or a flaky origin would have
        // every channel written on every refresh.
        var channel = new LiveTvChannel { Name = "Test Channel" };
        SeedCachedIcon(channel, IconUrl, etag: "\"stable\"", lastModified: new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var updated = await LiveTvChannelImageHelper.UpdateChannelImageIfNeededAsync(
            channel,
            null,
            IconUrl,
            CreateHttpClientFactory(_ => CreateResponse(etag: "\"stable\"", lastModified: "Tue, 02 Jan 2024 00:00:00 GMT")),
            NullLogger.Instance,
            CancellationToken.None);

        Assert.Equal(ChannelImageUpdate.None, updated);
    }

    [Fact]
    public async Task UpdateChannelImageIfNeeded_SameUrlFirstValidatorSeen_RecordsValidatorsForSaving()
    {
        // The learned validators are useless unless they are persisted: without a save the next refresh
        // probes without them again and could never detect a change.
        var channel = new LiveTvChannel { Name = "Test Channel" };
        SeedCachedIcon(channel, IconUrl);

        var updated = await LiveTvChannelImageHelper.UpdateChannelImageIfNeededAsync(
            channel,
            null,
            IconUrl,
            CreateHttpClientFactory(_ => CreateResponse(etag: "\"first\"", lastModified: "Mon, 01 Jan 2024 00:00:00 GMT")),
            NullLogger.Instance,
            CancellationToken.None);

        Assert.Equal(ChannelImageUpdate.ValidatorsOnly, updated);

        var image = channel.GetImageInfo(ImageType.Primary, 0);
        Assert.NotNull(image);
        Assert.Equal("\"first\"", image.ETag);
        Assert.Equal(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), image.SourceLastModified);

        // The cached image itself is still current and must not have been reset to the remote URL.
        Assert.Equal(IconUrl, image.Path);
    }

    [Fact]
    public async Task UpdateChannelImageIfNeeded_SameUrlUnchangedValidators_DoesNotRequestSave()
    {
        // The steady state must not report a save, or every channel would be written on every refresh.
        var channel = new LiveTvChannel { Name = "Test Channel" };
        SeedCachedIcon(channel, IconUrl, etag: "\"same\"", lastModified: new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var updated = await LiveTvChannelImageHelper.UpdateChannelImageIfNeededAsync(
            channel,
            null,
            IconUrl,
            CreateHttpClientFactory(_ => CreateResponse(etag: "\"same\"", lastModified: "Mon, 01 Jan 2024 00:00:00 GMT")),
            NullLogger.Instance,
            CancellationToken.None);

        Assert.Equal(ChannelImageUpdate.None, updated);
    }

    [Fact]
    public async Task UpdateChannelImageIfNeeded_LastModifiedOnlyGainsETag_RecordsValidatorsForSaving()
    {
        // Last-Modified is unchanged so the icon is current, but the newly exposed ETag is worth keeping.
        var channel = new LiveTvChannel { Name = "Test Channel" };
        SeedCachedIcon(channel, IconUrl, lastModified: new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var updated = await LiveTvChannelImageHelper.UpdateChannelImageIfNeededAsync(
            channel,
            null,
            IconUrl,
            CreateHttpClientFactory(_ => CreateResponse(etag: "\"late\"", lastModified: "Mon, 01 Jan 2024 00:00:00 GMT")),
            NullLogger.Instance,
            CancellationToken.None);

        Assert.Equal(ChannelImageUpdate.ValidatorsOnly, updated);
        Assert.Equal("\"late\"", channel.GetImageInfo(ImageType.Primary, 0)!.ETag);
    }

    [Fact]
    public async Task UpdateChannelImageIfNeeded_SameUrlNoValidators_Updates()
    {
        var channel = new LiveTvChannel { Name = "Test Channel" };
        SeedCachedIcon(channel, IconUrl);

        var updated = await LiveTvChannelImageHelper.UpdateChannelImageIfNeededAsync(
            channel,
            null,
            IconUrl,
            CreateHttpClientFactory(_ => CreateResponse()),
            NullLogger.Instance,
            CancellationToken.None);

        Assert.Equal(ChannelImageUpdate.ImageChanged, updated);
    }

    [Fact]
    public async Task UpdateChannelImageIfNeeded_SameNonHttpSource_DoesNotProbeOrUpdate()
    {
        const string LocalPath = "/tuner/icons/channel.png";
        var channel = new LiveTvChannel { Name = "Test Channel" };
        SeedCachedIcon(channel, LocalPath);

        // A non-http source must not trigger an HTTP request.
        var updated = await LiveTvChannelImageHelper.UpdateChannelImageIfNeededAsync(
            channel,
            null,
            LocalPath,
            CreateHttpClientFactory(_ => throw new InvalidOperationException("No request expected for a non-http source")),
            NullLogger.Instance,
            CancellationToken.None);

        Assert.Equal(ChannelImageUpdate.None, updated);
    }

    [Fact]
    public async Task UpdateChannelImages_PartitionsChannelsByRequiredSave()
    {
        const string NewIcon = "https://example.com/new.png";
        const string LearnsValidators = "https://example.com/learns.png";
        const string Unchanged = "https://example.com/unchanged.png";

        var newChannel = new LiveTvChannel { Name = "New" };

        var learningChannel = new LiveTvChannel { Name = "Learning" };
        SeedCachedIcon(learningChannel, LearnsValidators);

        var unchangedChannel = new LiveTvChannel { Name = "Unchanged" };
        SeedCachedIcon(unchangedChannel, Unchanged, etag: "\"stable\"");

        var channels = new[]
        {
            (newChannel, new ChannelInfo { Id = "1", Name = "New", ImageUrl = NewIcon }),
            (learningChannel, new ChannelInfo { Id = "2", Name = "Learning", ImageUrl = LearnsValidators }),
            (unchangedChannel, new ChannelInfo { Id = "3", Name = "Unchanged", ImageUrl = Unchanged })
        };

        var (iconChanged, validatorsChanged) = await LiveTvChannelImageHelper.UpdateChannelImagesAsync(
            channels,
            CreateHttpClientFactory(request => request.RequestUri!.ToString() switch
            {
                Unchanged => new HttpResponseMessage(HttpStatusCode.NotModified),
                LearnsValidators => CreateResponse(etag: "\"learned\""),
                _ => throw new InvalidOperationException($"Unexpected request for {request.RequestUri}")
            }),
            NullLogger.Instance,
            new RecordingProgress(),
            CancellationToken.None);

        Assert.Equal(new[] { newChannel }, iconChanged);
        Assert.Equal(new[] { learningChannel }, validatorsChanged);
        Assert.Equal("\"learned\"", learningChannel.GetImageInfo(ImageType.Primary, 0)!.ETag);
    }

    [Fact]
    public async Task UpdateChannelImages_FailingChannelDoesNotStopTheRest()
    {
        const string Failing = "https://example.com/failing.png";
        const string Working = "https://example.com/working.png";

        var failingChannel = new LiveTvChannel { Name = "Failing" };
        SeedCachedIcon(failingChannel, Failing, etag: "\"old\"");

        var workingChannel = new LiveTvChannel { Name = "Working" };
        SeedCachedIcon(workingChannel, Working, etag: "\"old\"");

        var channels = new[]
        {
            (failingChannel, new ChannelInfo { Id = "1", ImageUrl = Failing }),
            (workingChannel, new ChannelInfo { Id = "2", ImageUrl = Working })
        };

        var (iconChanged, _) = await LiveTvChannelImageHelper.UpdateChannelImagesAsync(
            channels,
            CreateHttpClientFactory(request => request.RequestUri!.ToString() == Failing
                ? throw new HttpRequestException("boom")
                : CreateResponse(etag: "\"new\"")),
            NullLogger.Instance,
            new RecordingProgress(),
            CancellationToken.None);

        // The probe swallows network errors and keeps the cached icon, so only the working one changed.
        Assert.Equal(new[] { workingChannel }, iconChanged);
    }

    [Fact]
    public async Task UpdateChannelImages_ReportsProgress()
    {
        var channels = Enumerable.Range(0, 5)
            .Select(i =>
            {
                var url = $"https://example.com/icon{i}.png";
                var channel = new LiveTvChannel { Name = $"Channel {i}" };
                SeedCachedIcon(channel, url, etag: "\"stable\"");
                return (channel, new ChannelInfo { Id = i.ToString(CultureInfo.InvariantCulture), ImageUrl = url });
            })
            .ToArray();

        var progress = new RecordingProgress();

        await LiveTvChannelImageHelper.UpdateChannelImagesAsync(
            channels,
            CreateHttpClientFactory(_ => new HttpResponseMessage(HttpStatusCode.NotModified)),
            NullLogger.Instance,
            progress,
            CancellationToken.None);

        var reported = progress.Reported;
        Assert.Equal(channels.Length, reported.Count);
        Assert.Equal(1d, reported.Max());
    }

    [Fact]
    public async Task UpdateChannelImages_EmptyList_DoesNothing()
    {
        var (iconChanged, validatorsChanged) = await LiveTvChannelImageHelper.UpdateChannelImagesAsync(
            Array.Empty<(LiveTvChannel, ChannelInfo)>(),
            CreateHttpClientFactory(_ => throw new InvalidOperationException("No request expected")),
            NullLogger.Instance,
            new RecordingProgress(),
            CancellationToken.None);

        Assert.Empty(iconChanged);
        Assert.Empty(validatorsChanged);
    }

    private static void SeedCachedIcon(LiveTvChannel channel, string source, string? etag = null, DateTime? lastModified = null)
    {
        // Set the image directly (SetImagePath would resolve local paths against the static file system,
        // which is not initialized in unit tests).
        channel.SetImage(
            new ItemImageInfo
            {
                Path = source,
                Type = ImageType.Primary,
                Source = source,
                ETag = etag,
                SourceLastModified = lastModified
            },
            0);
    }

    private static HttpResponseMessage CreateResponse(string? etag = null, string? lastModified = null)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(Array.Empty<byte>())
        };

        if (etag is not null)
        {
            response.Headers.ETag = new EntityTagHeaderValue(etag);
        }

        if (lastModified is not null)
        {
            response.Content.Headers.LastModified = DateTimeOffset.Parse(lastModified, CultureInfo.InvariantCulture);
        }

        return response;
    }

    private static IHttpClientFactory CreateHttpClientFactory(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((request, _) => Task.FromResult(responder(request)));

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(handler.Object));

        return factory.Object;
    }

    /// <summary>
    /// Records progress synchronously; <see cref="Progress{T}"/> would marshal the callbacks through
    /// the synchronization context and arrive after the assertions.
    /// </summary>
    private sealed class RecordingProgress : IProgress<double>
    {
        private readonly List<double> _reported = new();

        public IReadOnlyList<double> Reported
        {
            get
            {
                lock (_reported)
                {
                    return _reported.ToList();
                }
            }
        }

        public void Report(double value)
        {
            lock (_reported)
            {
                _reported.Add(value);
            }
        }
    }
}
