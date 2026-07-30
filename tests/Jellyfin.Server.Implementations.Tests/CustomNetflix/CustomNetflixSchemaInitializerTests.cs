using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.Implementations.CustomNetflix;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.CustomNetflix;

public class CustomNetflixSchemaInitializerTests
{
    [Fact]
    public async Task StartAsync_RetriesWithoutFailingTheHost()
    {
        var repository = new Mock<ICustomNetflixRepository>();
        repository.SetupGet(mock => mock.IsEnabled).Returns(true);
        repository
            .SetupSequence(mock => mock.EnsureSchemaAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("PostgreSQL unavailable."))
            .Returns(Task.CompletedTask);
        var schemaState = new CustomNetflixSchemaState();
        using var initializer = new CustomNetflixSchemaInitializer(
            repository.Object,
            schemaState,
            NullLogger<CustomNetflixSchemaInitializer>.Instance);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));

        await initializer.StartAsync(timeout.Token);
        await schemaState.WaitUntilReadyAsync(timeout.Token);
        await initializer.StopAsync(timeout.Token);

        Assert.True(schemaState.IsReady);
        repository.Verify(
            mock => mock.EnsureSchemaAsync(It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(5, 16)]
    [InlineData(10, 30)]
    public void GetDelay_CapsExponentialBackoff(int failureCount, int expectedSeconds)
        => Assert.Equal(
            TimeSpan.FromSeconds(expectedSeconds),
            CustomNetflixRetryPolicy.GetDelay(failureCount, TimeSpan.FromSeconds(30)));
}
