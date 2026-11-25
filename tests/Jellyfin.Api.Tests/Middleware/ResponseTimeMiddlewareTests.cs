using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jellyfin.Api.Middleware;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Middleware;

public class ResponseTimeMiddlewareTests
{
    [Fact]
    public async Task SlowResponse_RedactsInternalCapabilityWithoutChangingTheRequest()
    {
        var logger = new CapturingLogger();
        var configuration = Mock.Of<IServerConfigurationManager>(manager => manager.Configuration == new ServerConfiguration
        {
            EnableSlowResponseWarning = true,
            SlowResponseThresholdMs = -1
        });
        using var host = await new HostBuilder().ConfigureWebHost(builder => builder.UseTestServer()
            .ConfigureServices(services => services
                .AddSingleton(configuration)
                .AddSingleton<ILogger<ResponseTimeMiddleware>>(logger))
            .Configure(app => app.UseMiddleware<ResponseTimeMiddleware>().Run(context =>
            {
                Assert.Equal("private-input-capability", context.Request.Query["InternalLiveTvToken"].ToString());
                return context.Response.WriteAsync("ok", context.RequestAborted);
            }))).StartAsync(TestContext.Current.CancellationToken);
        using var client = host.GetTestClient();

        using var response = await client.GetAsync("/LiveTv/LiveStreamFiles/input/stream.ts?InternalLiveTvToken=private-input-capability&Other=safe", TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var message = Assert.Single(logger.Messages);
        Assert.DoesNotContain("private-input-capability", message, StringComparison.Ordinal);
        Assert.Contains("InternalLiveTvToken=[redacted]", message, StringComparison.Ordinal);
        Assert.Contains("Other=safe", message, StringComparison.Ordinal);
    }

    private sealed class CapturingLogger : ILogger<ResponseTimeMiddleware>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Messages.Add(formatter(state, exception));
    }
}
