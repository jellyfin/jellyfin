using System;
using System.Threading.Tasks;
using Jellyfin.Server.Implementations.Users;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Users
{
    public class UserManagerLockHelperTests
    {
        [Fact]
        public async Task LockAsync_WhenNested_DoesNotAcquireSecondLockAndRestoresStateOnDispose()
        {
            UserManager.LockHelper.IsNestedLock.Value = 0;
            using var helper = new UserManager.LockHelper();
            var key = Guid.NewGuid();

            Assert.True(helper.ShouldLock());

            var outerHandle = await helper.LockAsync(key);
            Assert.False(helper.ShouldLock());

            var innerHandle = await helper.LockAsync(key);
            Assert.False(helper.ShouldLock());

            innerHandle.Dispose();
            Assert.False(helper.ShouldLock());

            outerHandle.Dispose();
            Assert.True(helper.ShouldLock());
        }

        [Fact]
        public async Task LockAsync_WithSameKey_BlocksSecondLockUntilFirstIsReleased()
        {
            UserManager.LockHelper.IsNestedLock.Value = 0;
            using var helper = new UserManager.LockHelper();
            var key = Guid.NewGuid();

            var firstAcquired = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseFirst = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var secondEntered = false;

            var firstTask = Task.Run(async () =>
            {
                using var firstHandle = await helper.LockAsync(key);
                firstAcquired.SetResult(true);
                await releaseFirst.Task;
            });

            await firstAcquired.Task;

            var secondTask = Task.Run(async () =>
            {
                using var secondHandle = await helper.LockAsync(key);
                secondEntered = true;
            });

            await Task.Delay(100);
            Assert.False(secondEntered);

            releaseFirst.SetResult(true);

            await Task.WhenAll(firstTask, secondTask);
            Assert.True(secondEntered);
        }

        [Fact]
        public async Task LockAsync_WhenDisposed_ThrowsObjectDisposedException()
        {
            UserManager.LockHelper.IsNestedLock.Value = 0;
            using var helper = new UserManager.LockHelper();
            helper.Dispose();

            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await helper.LockAsync(Guid.NewGuid()));
        }

        [Fact]
        public void Dispose_WhenCalledMultipleTimes_DoesNotThrow()
        {
            UserManager.LockHelper.IsNestedLock.Value = 0;
            using var helper = new UserManager.LockHelper();

            helper.Dispose();
            var ex = Record.Exception(() => helper.Dispose());

            Assert.Null(ex);
        }
    }
}
