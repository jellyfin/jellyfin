using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Results;
using MediaBrowser.Controller.Session;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Results;

public sealed class VideoPlaybackResultTests
{
    [Fact]
    public async Task ExecuteResultAsync_BodyStillExecuting_HoldsOperation()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var complete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var request = new Mock<IVideoPlaybackRequest>(MockBehavior.Strict);
        request.Setup(r => r.Acknowledge());
        request.Setup(r => r.Dispose());
        var inner = new DelegateResult(async _ =>
        {
            entered.SetResult();
            await complete.Task.ConfigureAwait(true);
        });
        var result = new VideoPlaybackResult(inner, request.Object);

        var execution = result.ExecuteResultAsync(CreateContext());
        await entered.Task.ConfigureAwait(true);
        try
        {
            request.Verify(r => r.Acknowledge(), Times.Never);
            request.Verify(r => r.Dispose(), Times.Never);
            Assert.False(execution.IsCompleted);
        }
        finally
        {
            complete.SetResult();
        }

        await execution.ConfigureAwait(true);
        request.Verify(r => r.Acknowledge(), Times.Once);
        request.Verify(r => r.Dispose(), Times.Once);
    }

    [Theory]
    [InlineData(StatusCodes.Status200OK)]
    [InlineData(StatusCodes.Status206PartialContent)]
    [InlineData(StatusCodes.Status302Found)]
    public async Task ExecuteResultAsync_Success_ConfirmsBeforeDisposing(int statusCode)
    {
        var request = new Mock<IVideoPlaybackRequest>(MockBehavior.Strict);
        var sequence = new MockSequence();
        request.InSequence(sequence).Setup(r => r.Acknowledge());
        request.InSequence(sequence).Setup(r => r.Dispose());
        var inner = new DelegateResult(context =>
        {
            context.HttpContext.Response.StatusCode = statusCode;
            return Task.CompletedTask;
        });
        var result = new VideoPlaybackResult(inner, request.Object);

        await result.ExecuteResultAsync(CreateContext()).ConfigureAwait(true);

        request.Verify(r => r.Acknowledge(), Times.Once);
        request.Verify(r => r.Dispose(), Times.Once);
    }

    [Theory]
    [InlineData(StatusCodes.Status400BadRequest)]
    [InlineData(StatusCodes.Status404NotFound)]
    [InlineData(StatusCodes.Status500InternalServerError)]
    public async Task ExecuteResultAsync_ErrorResponse_DoesNotConfirm(int statusCode)
    {
        var request = CreateUnconfirmedRequest();
        var inner = new DelegateResult(context =>
        {
            context.HttpContext.Response.StatusCode = statusCode;
            return Task.CompletedTask;
        });
        var result = new VideoPlaybackResult(inner, request.Object);

        await result.ExecuteResultAsync(CreateContext()).ConfigureAwait(true);

        request.Verify(r => r.Acknowledge(), Times.Never);
        request.Verify(r => r.Dispose(), Times.Once);
    }

    [Fact]
    public async Task ExecuteResultAsync_Throws_DisposesWithoutConfirming()
    {
        var request = CreateUnconfirmedRequest();
        var failure = new InvalidOperationException("The response could not be written.");
        var result = new VideoPlaybackResult(new DelegateResult(_ => Task.FromException(failure)), request.Object);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => result.ExecuteResultAsync(CreateContext())).ConfigureAwait(true);

        Assert.Same(failure, actual);
        request.Verify(r => r.Acknowledge(), Times.Never);
        request.Verify(r => r.Dispose(), Times.Once);
    }

    [Fact]
    public async Task ExecuteResultAsync_RequestAborted_DisposesWithoutConfirming()
    {
        using var cancellation = new CancellationTokenSource();
        var context = CreateContext();
        context.HttpContext.RequestAborted = cancellation.Token;
        var request = CreateUnconfirmedRequest();
        var inner = new DelegateResult(async _ =>
        {
            await cancellation.CancelAsync().ConfigureAwait(true);
            await Task.FromCanceled(cancellation.Token).ConfigureAwait(true);
        });
        var result = new VideoPlaybackResult(inner, request.Object);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => result.ExecuteResultAsync(context)).ConfigureAwait(true);

        request.Verify(r => r.Acknowledge(), Times.Never);
        request.Verify(r => r.Dispose(), Times.Once);
    }

    [Fact]
    public async Task ExecuteResultAsync_AbortedResultReturnsNormally_DoesNotConfirm()
    {
        using var cancellation = new CancellationTokenSource();
        var context = CreateContext();
        context.HttpContext.RequestAborted = cancellation.Token;
        var request = CreateUnconfirmedRequest();
        var inner = new DelegateResult(async _ =>
        {
            await cancellation.CancelAsync().ConfigureAwait(true);
        });
        var result = new VideoPlaybackResult(inner, request.Object);

        await result.ExecuteResultAsync(context).ConfigureAwait(true);

        request.Verify(r => r.Acknowledge(), Times.Never);
        request.Verify(r => r.Dispose(), Times.Once);
    }

    private static ActionContext CreateContext()
        => new() { HttpContext = new DefaultHttpContext() };

    private static Mock<IVideoPlaybackRequest> CreateUnconfirmedRequest()
    {
        var request = new Mock<IVideoPlaybackRequest>(MockBehavior.Strict);
        request.Setup(r => r.Dispose());
        return request;
    }

    private sealed class DelegateResult : ActionResult
    {
        private readonly Func<ActionContext, Task> _execute;

        public DelegateResult(Func<ActionContext, Task> execute)
        {
            _execute = execute;
        }

        public override Task ExecuteResultAsync(ActionContext context)
            => _execute(context);
    }
}
