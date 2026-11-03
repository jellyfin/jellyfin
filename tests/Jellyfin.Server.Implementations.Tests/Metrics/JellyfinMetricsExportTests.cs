using System.Net;
using System.Threading.Tasks;
using Jellyfin.Server.Implementations.Metrics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prometheus;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Metrics;

[Collection(MetricsTestCollection.Name)]
public class JellyfinMetricsExportTests
{
    [Fact]
    public async Task Export_ContainsMetricHelpTypesAndStableLabels()
    {
        JellyfinMetrics.ActiveStreams.WithLabels("transcode").Set(1);
        JellyfinMetrics.PlayMethodsTotal
            .WithLabels("transcode", "audio_codec_not_supported|video_codec_not_supported")
            .Inc();
        JellyfinMetrics.Libraries.WithLabels("movies").Set(1);

        using var host = await new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services => services.AddRouting());
                webHost.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapMetrics());
                });
            })
            .StartAsync(TestContext.Current.CancellationToken);
        using var client = host.GetTestClient();
        using var response = await client.GetAsync("/metrics", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        string[] gauges =
        [
            "jellyfin_user_accounts",
            "jellyfin_connected_sessions",
            "jellyfin_registered_devices",
            "jellyfin_active_streams",
            "jellyfin_active_transcodes",
            "jellyfin_libraries",
            "jellyfin_library_items",
            "jellyfin_media_streams",
            "jellyfin_users_with_permission",
            "jellyfin_users_authenticating_external"
        ];
        foreach (var gauge in gauges)
        {
            Assert.Contains($"# HELP {gauge} ", text, System.StringComparison.Ordinal);
            Assert.Contains($"# TYPE {gauge} gauge", text, System.StringComparison.Ordinal);
        }

        foreach (var counter in new[] { "jellyfin_play_methods_total", "jellyfin_transcodes_total" })
        {
            Assert.Contains($"# HELP {counter} ", text, System.StringComparison.Ordinal);
            Assert.Contains($"# TYPE {counter} counter", text, System.StringComparison.Ordinal);
        }

        Assert.Contains("play_method=\"transcode\"", text, System.StringComparison.Ordinal);
        Assert.Contains("transcode_reason=\"audio_codec_not_supported|video_codec_not_supported\"", text, System.StringComparison.Ordinal);
        Assert.Contains("type=\"movies\"", text, System.StringComparison.Ordinal);
    }
}
