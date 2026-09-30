using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Providers.Plugins.MusicBrainz;
using MetaBrainz.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Providers.Tests.Music;

public static class MusicBrainzQueryExtensionsTests
{
    [Fact]
    public static async Task RetryOnTransientErrorAsync_ServerBusy_RetriesAndSucceeds()
    {
        var attempts = 0;

        var result = await MusicBrainzQueryExtensions.RetryOnTransientErrorAsync(
            async _ =>
            {
                attempts++;
                if (attempts == 1)
                {
                    throw await CreateErrorAsync(HttpStatusCode.ServiceUnavailable, TimeSpan.Zero);
                }

                return "found";
            },
            "test",
            NullLogger.Instance,
            CancellationToken.None);

        Assert.Equal("found", result);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public static async Task RetryOnTransientErrorAsync_ServerStaysBusy_GivesUp()
    {
        var attempts = 0;

        var error = await Assert.ThrowsAsync<HttpError>(() => MusicBrainzQueryExtensions.RetryOnTransientErrorAsync<string>(
            async _ =>
            {
                attempts++;
                throw await CreateErrorAsync(HttpStatusCode.ServiceUnavailable, TimeSpan.Zero);
            },
            "test",
            NullLogger.Instance,
            CancellationToken.None));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, error.Status);
        Assert.Equal(3, attempts);
    }

    [Fact]
    public static async Task RetryOnTransientErrorAsync_NotFound_DoesNotRetry()
    {
        var attempts = 0;

        await Assert.ThrowsAsync<HttpError>(() => MusicBrainzQueryExtensions.RetryOnTransientErrorAsync<string>(
            async _ =>
            {
                attempts++;
                throw await CreateErrorAsync(HttpStatusCode.NotFound, null);
            },
            "test",
            NullLogger.Instance,
            CancellationToken.None));

        Assert.Equal(1, attempts);
    }

    [Fact]
    public static async Task GetRetryDelay_NoHint_BacksOffExponentially()
    {
        var error = await CreateErrorAsync(HttpStatusCode.ServiceUnavailable, null);

        Assert.Equal(TimeSpan.FromSeconds(1), MusicBrainzQueryExtensions.GetRetryDelay(error, 1));
        Assert.Equal(TimeSpan.FromSeconds(2), MusicBrainzQueryExtensions.GetRetryDelay(error, 2));
    }

    [Fact]
    public static async Task GetRetryDelay_RetryAfterZero_WaitsMinimum()
    {
        var error = await CreateErrorAsync(HttpStatusCode.ServiceUnavailable, TimeSpan.Zero);

        Assert.Equal(TimeSpan.FromSeconds(1), MusicBrainzQueryExtensions.GetRetryDelay(error, 1));
    }

    [Fact]
    public static async Task GetRetryDelay_RetryAfterLongerThanBackoff_UsesRetryAfter()
    {
        var error = await CreateErrorAsync(HttpStatusCode.ServiceUnavailable, TimeSpan.FromSeconds(10));

        Assert.Equal(TimeSpan.FromSeconds(10), MusicBrainzQueryExtensions.GetRetryDelay(error, 1));
    }

    [Fact]
    public static async Task GetRetryDelay_LongRetryAfter_IsCapped()
    {
        var error = await CreateErrorAsync(HttpStatusCode.ServiceUnavailable, TimeSpan.FromHours(1));

        Assert.Equal(TimeSpan.FromSeconds(15), MusicBrainzQueryExtensions.GetRetryDelay(error, 1));
    }

    [Fact]
    public static async Task GetRetryDelay_RateLimitWindow_WaitsForReset()
    {
        var error = await CreateErrorAsync(
            HttpStatusCode.ServiceUnavailable,
            null,
            headers => headers.TryAddWithoutValidation("X-RateLimit-Reset", DateTimeOffset.UtcNow.AddSeconds(8).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)));

        var delay = MusicBrainzQueryExtensions.GetRetryDelay(error, 1);

        Assert.InRange(delay, TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(8));
    }

    private static async Task<HttpError> CreateErrorAsync(HttpStatusCode status, TimeSpan? retryAfter, Action<HttpResponseHeaders>? configureHeaders = null)
    {
        using var response = new HttpResponseMessage(status);
        if (retryAfter is not null)
        {
            response.Headers.RetryAfter = new RetryConditionHeaderValue(retryAfter.Value);
        }

        configureHeaders?.Invoke(response.Headers);

        return await HttpError.FromResponseAsync(response);
    }
}
