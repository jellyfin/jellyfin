using System;
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

    private static WebSocketController CreateController()
    {
        var sessionManager = Mock.Of<ISessionManager>();
        return new WebSocketController(
            NullLogger<WebSocketController>.Instance,
            new SessionInfo(sessionManager, NullLogger<SessionInfo>.Instance),
            sessionManager);
    }
}
