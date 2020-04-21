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
    [Authenticated]
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
        [ProducesResponseType(typeof(TaskInfo[]), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult GetTasks(
            [FromQuery] bool? isHidden = false,
            [FromQuery] bool? isEnabled = false)
        {
            IEnumerable<IScheduledTaskWorker> tasks = _taskManager.ScheduledTasks.OrderBy(o => o.Name);

            if (isHidden.HasValue)
            {
                var hiddenValue = isHidden.Value;
                tasks = tasks.Where(o =>
                {
                    var itemIsHidden = false;
                    if (o.ScheduledTask is IConfigurableScheduledTask configurableScheduledTask)
                    {
                        itemIsHidden = configurableScheduledTask.IsHidden;
                    }

                    return itemIsHidden == hiddenValue;
                });
            }

            if (isEnabled.HasValue)
            {
                var enabledValue = isEnabled.Value;
                tasks = tasks.Where(o =>
                {
                    var itemIsEnabled = false;
                    if (o.ScheduledTask is IConfigurableScheduledTask configurableScheduledTask)
                    {
                        itemIsEnabled = configurableScheduledTask.IsEnabled;
                    }

                    return itemIsEnabled == enabledValue;
                });
            }

            var taskInfos = tasks.Select(ScheduledTaskHelpers.GetTaskInfo);

            return Ok(taskInfos);
        }

        /// <summary>
        /// Get task by id.
        /// </summary>
        /// <param name="taskId">Task Id.</param>
        /// <returns>Task Info.</returns>
        [HttpGet("{TaskID}")]
        [ProducesResponseType(typeof(TaskInfo), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult GetTask([FromRoute] string taskId)
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
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult StartTask([FromRoute] string taskId)
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
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult StopTask([FromRoute] string taskId)
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
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult UpdateTask(
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
