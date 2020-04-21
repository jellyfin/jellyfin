#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// Scheduled Tasks Controller.
    /// </summary>
    // [Authenticated]
    public class ScheduledTasksController : BaseJellyfinApiController
    {
        private readonly ITaskManager _taskManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledTasksController"/> class.
        /// </summary>
        /// <param name="taskManager">Instance of the <see cref="ITaskManager"/> interface.</param>
        public ScheduledTasksController(ITaskManager taskManager)
        {
            _taskManager = taskManager;
        }

        /// <summary>
        /// Get tasks.
        /// </summary>
        /// <param name="isHidden">Optional filter tasks that are hidden, or not.</param>
        /// <param name="isEnabled">Optional filter tasks that are enabled, or not.</param>
        /// <returns>Task list.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IEnumerable<IScheduledTaskWorker> GetTasks(
            [FromQuery] bool? isHidden = false,
            [FromQuery] bool? isEnabled = false)
        {
            IEnumerable<IScheduledTaskWorker> tasks = _taskManager.ScheduledTasks.OrderBy(o => o.Name);

            foreach (var task in tasks)
            {
                if (task.ScheduledTask is IConfigurableScheduledTask scheduledTask)
                {
                    if (isHidden.HasValue && isHidden.Value != scheduledTask.IsHidden)
                    {
                        continue;
                    }

                    if (isEnabled.HasValue && isEnabled.Value != scheduledTask.IsEnabled)
                    {
                        continue;
                    }
                }

                yield return task;
            }
        }

        /// <summary>
        /// Get task by id.
        /// </summary>
        /// <param name="taskId">Task Id.</param>
        /// <returns>Task Info.</returns>
        [HttpGet("{TaskID}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<TaskInfo> GetTask([FromRoute] string taskId)
        {
            var task = _taskManager.ScheduledTasks.FirstOrDefault(i =>
                string.Equals(i.Id, taskId, StringComparison.OrdinalIgnoreCase));

            if (task == null)
            {
                return NotFound();
            }

            var result = ScheduledTaskHelpers.GetTaskInfo(task);
            return Ok(result);
        }

        /// <summary>
        /// Start specified task.
        /// </summary>
        /// <param name="taskId">Task Id.</param>
        /// <returns>Status.</returns>
        [HttpPost("Running/{TaskID}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult StartTask([FromRoute] string taskId)
        {
            var task = _taskManager.ScheduledTasks.FirstOrDefault(o =>
                o.Id.Equals(taskId, StringComparison.OrdinalIgnoreCase));

            if (task == null)
            {
                return NotFound();
            }

            _taskManager.Execute(task, new TaskOptions());
            return Ok();
        }

        /// <summary>
        /// Stop specified task.
        /// </summary>
        /// <param name="taskId">Task Id.</param>
        /// <returns>Status.</returns>
        [HttpDelete("Running/{TaskID}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult StopTask([FromRoute] string taskId)
        {
            var task = _taskManager.ScheduledTasks.FirstOrDefault(o =>
                o.Id.Equals(taskId, StringComparison.OrdinalIgnoreCase));

            if (task == null)
            {
                return NotFound();
            }

            _taskManager.Cancel(task);
            return Ok();
        }

        /// <summary>
        /// Update specified task triggers.
        /// </summary>
        /// <param name="taskId">Task Id.</param>
        /// <param name="triggerInfos">Triggers.</param>
        /// <returns>Status.</returns>
        [HttpPost("{TaskID}/Triggers")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateTask(
            [FromRoute] string taskId,
            [FromBody, BindRequired] TaskTriggerInfo[] triggerInfos)
        {
            var task = _taskManager.ScheduledTasks.FirstOrDefault(o =>
                o.Id.Equals(taskId, StringComparison.OrdinalIgnoreCase));
            if (task == null)
            {
                return NotFound();
            }

            task.Triggers = triggerInfos;
            return Ok();
        }
    }
}
