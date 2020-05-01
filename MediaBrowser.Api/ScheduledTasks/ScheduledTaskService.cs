using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api.ScheduledTasks
{
    /// <summary>
    /// Class GetScheduledTask
    /// </summary>
    [Route("/ScheduledTasks/{Id}", "GET", Summary = "Gets a scheduled task, by Id")]
    public class GetScheduledTask : IReturn<TaskInfo>
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    /// <summary>
    /// Class GetScheduledTasks
    /// </summary>
    [Route("/ScheduledTasks", "GET", Summary = "Gets scheduled tasks")]
    public class GetScheduledTasks : IReturn<TaskInfo[]>
    {
        [ApiMember(Name = "IsHidden", Description = "Optional filter tasks that are hidden, or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsHidden { get; set; }

        [ApiMember(Name = "IsEnabled", Description = "Optional filter tasks that are enabled, or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsEnabled { get; set; }
    }

    /// <summary>
    /// Class StartScheduledTask
    /// </summary>
    [Route("/ScheduledTasks/Running/{Id}", "POST", Summary = "Starts a scheduled task")]
    public class StartScheduledTask : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }
    }

    /// <summary>
    /// Class StopScheduledTask
    /// </summary>
    [Route("/ScheduledTasks/Running/{Id}", "DELETE", Summary = "Stops a scheduled task")]
    public class StopScheduledTask : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Id { get; set; }
    }

    /// <summary>
    /// Class UpdateScheduledTaskTriggers
    /// </summary>
    [Route("/ScheduledTasks/{Id}/Triggers", "POST", Summary = "Updates the triggers for a scheduled task")]
    public class UpdateScheduledTaskTriggers : List<TaskTriggerInfo>, IReturnVoid
    {
        /// <summary>
        /// Gets or sets the task id.
        /// </summary>
        /// <value>The task id.</value>
        [ApiMember(Name = "Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }
    }

    /// <summary>
    /// Class ScheduledTasksService
    /// </summary>
    [Authenticated(Roles = "Admin")]
    public class ScheduledTaskService : BaseApiService
    {
        /// <summary>
        /// The task manager.
        /// </summary>
        private readonly ITaskManager _taskManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledTaskService" /> class.
        /// </summary>
        /// <param name="taskManager">The task manager.</param>
        /// <exception cref="ArgumentNullException">taskManager</exception>
        public ScheduledTaskService(
            ILogger<ScheduledTaskService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            ITaskManager taskManager)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _taskManager = taskManager;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>IEnumerable{TaskInfo}.</returns>
        public object Get(GetScheduledTasks request)
        {
            IEnumerable<IScheduledTaskWorker> result = _taskManager.ScheduledTasks
                .OrderBy(i => i.Name);

            if (request.IsHidden.HasValue)
            {
                var val = request.IsHidden.Value;

                result = result.Where(i =>
                {
                    var isHidden = false;

                    if (i.ScheduledTask is IConfigurableScheduledTask configurableTask)
                    {
                        isHidden = configurableTask.IsHidden;
                    }

                    return isHidden == val;
                });
            }

            if (request.IsEnabled.HasValue)
            {
                var val = request.IsEnabled.Value;

                result = result.Where(i =>
                {
                    var isEnabled = true;

                    if (i.ScheduledTask is IConfigurableScheduledTask configurableTask)
                    {
                        isEnabled = configurableTask.IsEnabled;
                    }

                    return isEnabled == val;
                });
            }

            var infos = result
                .Select(ScheduledTaskHelpers.GetTaskInfo)
                .ToArray();

            return ToOptimizedResult(infos);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>IEnumerable{TaskInfo}.</returns>
        /// <exception cref="ResourceNotFoundException">Task not found</exception>
        public object Get(GetScheduledTask request)
        {
            var task = _taskManager.ScheduledTasks.FirstOrDefault(i => string.Equals(i.Id, request.Id));

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
        /// <exception cref="ResourceNotFoundException">Task not found</exception>
        public void Post(StartScheduledTask request)
        {
            var task = _taskManager.ScheduledTasks.FirstOrDefault(i => string.Equals(i.Id, request.Id));

            if (task == null)
            {
                throw new ResourceNotFoundException("Task not found");
            }

            _taskManager.Execute(task, new TaskOptions());
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <exception cref="ResourceNotFoundException">Task not found</exception>
        public void Delete(StopScheduledTask request)
        {
            var task = _taskManager.ScheduledTasks.FirstOrDefault(i => string.Equals(i.Id, request.Id));

            if (task == null)
            {
                throw new ResourceNotFoundException("Task not found");
            }

            _taskManager.Cancel(task);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <exception cref="ResourceNotFoundException">Task not found</exception>
        public void Post(UpdateScheduledTaskTriggers request)
        {
            // We need to parse this manually because we told service stack not to with IRequiresRequestStream
            // https://code.google.com/p/servicestack/source/browse/trunk/Common/ServiceStack.Text/ServiceStack.Text/Controller/PathInfo.cs
            var id = GetPathValue(1).ToString();

            var task = _taskManager.ScheduledTasks.FirstOrDefault(i => string.Equals(i.Id, id, StringComparison.Ordinal));

            if (task == null)
            {
                throw new ResourceNotFoundException("Task not found");
            }

            task.Triggers = request.ToArray();
        }
    }
}
