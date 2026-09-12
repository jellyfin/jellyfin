using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Session;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Session;

public class WebSocketControllerTests
{
    [Fact]
    public async Task DisposeAsync_SocketDisposalResumesOnAnotherThread_CompletesWithoutThrowing()
    {
        var gate = new TaskCompletionSource();
        var socket = new Mock<IWebSocketConnection>();
        socket.Setup(s => s.DisposeAsync()).Returns(() => new ValueTask(gate.Task));

        var controller = CreateController();
        controller.AddWebSocket(socket.Object);

        var disposing = controller.DisposeAsync();

        // Completing from another thread resumes the continuation there. A ReaderWriterLockSlim is
        // thread affine, so a lock held across the await cannot be released by that thread.
        await Task.Run(() => gate.SetResult(), TestContext.Current.CancellationToken);

        await disposing;
    }

    [Fact]
    public async Task DisposeAsync_CalledTwice_DisposesEachSocketOnce()
    {
        var socket = new Mock<IWebSocketConnection>();
        socket.Setup(s => s.DisposeAsync()).Returns(ValueTask.CompletedTask);

        var controller = CreateController();
        controller.AddWebSocket(socket.Object);

        await controller.DisposeAsync();
        await controller.DisposeAsync();

        socket.Verify(s => s.DisposeAsync(), Times.Once());
    }

    [Fact]
    public async Task OnConnectionClosed_AfterDispose_DoesNotThrow()
    {
        var socket = new Mock<IWebSocketConnection>();
        socket.Setup(s => s.DisposeAsync()).Returns(ValueTask.CompletedTask);

        var controller = CreateController();
        controller.AddWebSocket(socket.Object);
        await controller.DisposeAsync();

        var handler = typeof(WebSocketController)
            .GetMethod("OnConnectionClosed", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(handler);

        // The handler is async void, so a throw goes to the synchronization context rather than
        // to the caller. On the thread pool it would terminate the process.
        var context = new CapturingSynchronizationContext();
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            handler!.Invoke(controller, new object?[] { socket.Object, EventArgs.Empty });
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        Assert.Empty(context.Exceptions);
    }

    private static WebSocketController CreateController()
    {
        var sessionManager = Mock.Of<ISessionManager>();
        return new WebSocketController(
            NullLogger<WebSocketController>.Instance,
            new SessionInfo(sessionManager, NullLogger<SessionInfo>.Instance),
            sessionManager);
    }

    private sealed class CapturingSynchronizationContext : SynchronizationContext
    {
        public List<Exception> Exceptions { get; } = new List<Exception>();

        public override void Post(SendOrPostCallback d, object? state) => Run(d, state);

        public override void Send(SendOrPostCallback d, object? state) => Run(d, state);

        private void Run(SendOrPostCallback d, object? state)
        {
            try
            {
                d(state);
            }
            catch (Exception ex)
            {
                Exceptions.Add(ex);
            }
        }
    }
}
