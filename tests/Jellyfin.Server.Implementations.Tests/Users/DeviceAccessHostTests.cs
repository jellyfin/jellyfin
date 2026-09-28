using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Events;
using Jellyfin.Data.Queries;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Entities.Security;
using Jellyfin.Server.Implementations.Users;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Users;

public class DeviceAccessHostTests
{
    [Fact]
    public async Task OnUserUpdated_LogoutThrows_DoesNotEscapeToThreadPool()
    {
        var user = new User("test", "default", "default");
        var device = new Device(user.Id, "app", "1.0", "device", "device-id");

        var deviceManager = new Mock<IDeviceManager>();
        deviceManager.Setup(d => d.GetDevices(It.IsAny<DeviceQuery>()))
            .Returns(new QueryResult<Device>(new[] { device }));
        deviceManager.Setup(d => d.CanAccessDevice(user, device.DeviceId)).Returns(false);

        var sessionManager = new Mock<ISessionManager>();
        sessionManager.Setup(s => s.Logout(It.IsAny<Device>()))
            .ThrowsAsync(new ObjectDisposedException(nameof(ISessionManager)));

        var userManager = new Mock<IUserManager>();
        var host = new DeviceAccessHost(
            userManager.Object,
            deviceManager.Object,
            sessionManager.Object,
            NullLogger<DeviceAccessHost>.Instance);
        await host.StartAsync(TestContext.Current.CancellationToken);

        var context = new CapturingSynchronizationContext();
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            userManager.Raise(m => m.OnUserUpdated += null, userManager.Object, new GenericEventArgs<User>(user));
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        Assert.Empty(context.Exceptions);
    }

    [Fact]
    public async Task OnUserUpdated_DeviceNoLongerAllowed_LogsOutDevice()
    {
        var user = new User("test", "default", "default");
        var device = new Device(user.Id, "app", "1.0", "device", "device-id");

        var deviceManager = new Mock<IDeviceManager>();
        deviceManager.Setup(d => d.GetDevices(It.IsAny<DeviceQuery>()))
            .Returns(new QueryResult<Device>(new[] { device }));
        deviceManager.Setup(d => d.CanAccessDevice(user, device.DeviceId)).Returns(false);

        var loggedOut = new TaskCompletionSource();
        var sessionManager = new Mock<ISessionManager>();
        sessionManager.Setup(s => s.Logout(It.IsAny<Device>()))
            .Callback(() => loggedOut.TrySetResult())
            .Returns(Task.CompletedTask);

        var userManager = new Mock<IUserManager>();
        var host = new DeviceAccessHost(
            userManager.Object,
            deviceManager.Object,
            sessionManager.Object,
            NullLogger<DeviceAccessHost>.Instance);
        await host.StartAsync(TestContext.Current.CancellationToken);

        userManager.Raise(m => m.OnUserUpdated += null, userManager.Object, new GenericEventArgs<User>(user));

        await loggedOut.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        sessionManager.Verify(s => s.Logout(device), Times.Once);
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
