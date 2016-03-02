using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Tasks;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Configuration;

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
    public class GetScheduledTasks : IReturn<List<TaskInfo>>
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
        /// Gets or sets the task manager.
        /// </summary>
        /// <value>The task manager.</value>
        private ITaskManager TaskManager { get; set; }

        private readonly IServerConfigurationManager _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledTaskService" /> class.
        /// </summary>
        /// <param name="taskManager">The task manager.</param>
        /// <exception cref="ArgumentNullException">taskManager</exception>
        public ScheduledTaskService(ITaskManager taskManager, IServerConfigurationManager config)
        {
            if (taskManager == null)
            {
                throw new ArgumentNullException("taskManager");
            }

            TaskManager = taskManager;
            _config = config;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>IEnumerable{TaskInfo}.</returns>
        public object Get(GetScheduledTasks request)
        {
            IEnumerable<IScheduledTaskWorker> result = TaskManager.ScheduledTasks
                .OrderBy(i => i.Name);

            if (request.IsHidden.HasValue)
            {
                var val = request.IsHidden.Value;

                result = result.Where(i =>
                {
                    var isHidden = false;

                    var configurableTask = i.ScheduledTask as IConfigurableScheduledTask;

                    if (configurableTask != null)
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

                    var configurableTask = i.ScheduledTask as IConfigurableScheduledTask;

                    if (configurableTask != null)
                    {
                        isEnabled = configurableTask.IsEnabled;
                    }

                    return isEnabled == val;
                });
            }
            
            var infos = result
                .Select(ScheduledTaskHelpers.GetTaskInfo)
                .ToList();

            return ToOptimizedResult(infos);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>IEnumerable{TaskInfo}.</returns>
        /// <exception cref="MediaBrowser.Common.Extensions.ResourceNotFoundException">Task not found</exception>
        public object Get(GetScheduledTask request)
        {
            var task = TaskManager.ScheduledTasks.FirstOrDefault(i => string.Equals(i.Id, request.Id));

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
            var task = TaskManager.ScheduledTasks.FirstOrDefault(i => string.Equals(i.Id, request.Id));

            if (task == null)
            {
                throw new ResourceNotFoundException("Task not found");
            }

            var hasKey = task.ScheduledTask as IHasKey;
            if (hasKey != null)
            {
                if (string.Equals(hasKey.Key, "SystemUpdateTask", StringComparison.OrdinalIgnoreCase))
                {
                    // This is a hack for now just to get the update application function to work when auto-update is disabled
                    if (!_config.Configuration.EnableAutoUpdate)
                    {
                        _config.Configuration.EnableAutoUpdate = true;
                        _config.SaveConfiguration();
                    }
                }
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
            var task = TaskManager.ScheduledTasks.FirstOrDefault(i => string.Equals(i.Id, request.Id));

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
            var id = GetPathValue(1);

            var task = TaskManager.ScheduledTasks.FirstOrDefault(i => string.Equals(i.Id, id));

            if (task == null)
            {
                throw new ResourceNotFoundException("Task not found");
            }

            var triggerInfos = request;

            task.Triggers = triggerInfos.Select(ScheduledTaskHelpers.GetTrigger);
        }
    }
}
