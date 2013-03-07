using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Server.Implementations.HttpServer;
using ServiceStack.ServiceHost;
using ServiceStack.Text.Controller;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MediaBrowser.Api.ScheduledTasks
{
    /// <summary>
    /// Class GetScheduledTask
    /// </summary>
    [Route("/ScheduledTasks/{Id}", "GET")]
    public class GetScheduledTask : IReturn<TaskInfo>
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// Class GetScheduledTasks
    /// </summary>
    [Route("/ScheduledTasks", "GET")]
    public class GetScheduledTasks : IReturn<List<TaskInfo>>
    {

    }

    /// <summary>
    /// Class StartScheduledTask
    /// </summary>
    [Route("/ScheduledTasks/Running/{Id}", "POST")]
    public class StartScheduledTask : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// Class StopScheduledTask
    /// </summary>
    [Route("/ScheduledTasks/Running/{Id}", "DELETE")]
    public class StopScheduledTask : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// Class UpdateScheduledTaskTriggers
    /// </summary>
    [Route("/ScheduledTasks/{Id}/Triggers", "POST")]
    public class UpdateScheduledTaskTriggers : List<TaskTriggerInfo>, IReturnVoid
    {
        /// <summary>
        /// Gets or sets the task id.
        /// </summary>
        /// <value>The task id.</value>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// Class ScheduledTasksService
    /// </summary>
    public class ScheduledTaskService : BaseRestService
    {
        /// <summary>
        /// Gets or sets the task manager.
        /// </summary>
        /// <value>The task manager.</value>
        private ITaskManager TaskManager { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledTaskService" /> class.
        /// </summary>
        /// <param name="taskManager">The task manager.</param>
        /// <exception cref="System.ArgumentNullException">taskManager</exception>
        public ScheduledTaskService(ITaskManager taskManager)
        {
            if (taskManager == null)
            {
                throw new ArgumentNullException("taskManager");
            }

            TaskManager = taskManager;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>IEnumerable{TaskInfo}.</returns>
        public object Get(GetScheduledTasks request)
        {
            var result = TaskManager.ScheduledTasks.OrderBy(i => i.Name)
                         .Select(ScheduledTaskHelpers.GetTaskInfo).ToList();

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>IEnumerable{TaskInfo}.</returns>
        /// <exception cref="MediaBrowser.Common.Extensions.ResourceNotFoundException">Task not found</exception>
        public object Get(GetScheduledTask request)
        {
            var task = TaskManager.ScheduledTasks.FirstOrDefault(i => i.Id == request.Id);

            if (task == null)
            {
                throw new ResourceNotFoundException("Task not found");
            }

            var result = ScheduledTaskHelpers.GetTaskInfo(task);

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <exception cref="MediaBrowser.Common.Extensions.ResourceNotFoundException">Task not found</exception>
        public void Post(StartScheduledTask request)
        {
            var task = TaskManager.ScheduledTasks.FirstOrDefault(i => i.Id == request.Id);

            if (task == null)
            {
                throw new ResourceNotFoundException("Task not found");
            }

            TaskManager.Execute(task);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <exception cref="MediaBrowser.Common.Extensions.ResourceNotFoundException">Task not found</exception>
        public void Delete(StopScheduledTask request)
        {
            var task = TaskManager.ScheduledTasks.FirstOrDefault(i => i.Id == request.Id);

            if (task == null)
            {
                throw new ResourceNotFoundException("Task not found");
            }

            TaskManager.Cancel(task);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <exception cref="MediaBrowser.Common.Extensions.ResourceNotFoundException">Task not found</exception>
        public void Post(UpdateScheduledTaskTriggers request)
        {
            // We need to parse this manually because we told service stack not to with IRequiresRequestStream
            // https://code.google.com/p/servicestack/source/browse/trunk/Common/ServiceStack.Text/ServiceStack.Text/Controller/PathInfo.cs
            var pathInfo = PathInfo.Parse(Request.PathInfo);
            var id = new Guid(pathInfo.GetArgumentValue<string>(1));

            var task = TaskManager.ScheduledTasks.FirstOrDefault(i => i.Id == id);

            if (task == null)
            {
                throw new ResourceNotFoundException("Task not found");
            }

            var triggerInfos = request;

            task.Triggers = triggerInfos.Select(ScheduledTaskHelpers.GetTrigger);
        }
    }
}
