using System.Collections.Generic;
using Jellyfin.Api.Controllers;
using MediaBrowser.Model.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers
{
    public class LocalManagementControllerTests
    {
        [Fact]
        public void RefreshLibrary_TaskExists_StartsTask()
        {
            var scheduledTask = new Mock<IScheduledTask>();
            scheduledTask.Setup(t => t.Key).Returns("RefreshLibrary");
            var worker = new Mock<IScheduledTaskWorker>();
            worker.Setup(w => w.ScheduledTask).Returns(scheduledTask.Object);
            var taskManager = new Mock<ITaskManager>();
            taskManager.Setup(m => m.ScheduledTasks).Returns(new List<IScheduledTaskWorker> { worker.Object });

            var result = new LocalManagementController(taskManager.Object).RefreshLibrary();

            Assert.IsType<NoContentResult>(result);
            taskManager.Verify(m => m.Execute(worker.Object, It.IsAny<TaskOptions>()), Times.Once());
        }

        [Fact]
        public void RefreshLibrary_TaskMissing_ReturnsNotFound()
        {
            var taskManager = new Mock<ITaskManager>();
            taskManager.Setup(m => m.ScheduledTasks).Returns(new List<IScheduledTaskWorker>());

            var result = new LocalManagementController(taskManager.Object).RefreshLibrary();

            Assert.IsType<NotFoundResult>(result);
            taskManager.Verify(m => m.Execute(It.IsAny<IScheduledTaskWorker>(), It.IsAny<TaskOptions>()), Times.Never());
        }
    }
}
