using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Jellyfin.Api.Constants;
using MediaBrowser.Common.Api;
using MediaBrowser.Model.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Scheduled Tasks Controller.
/// </summary>
[Authorize(Policy = Policies.RequiresElevation)]
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
    /// <response code="200">Scheduled tasks retrieved.</response>
    /// <returns>The list of scheduled tasks.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IEnumerable<TaskInfo> GetTasks(
        [FromQuery] bool? isHidden,
        [FromQuery] bool? isEnabled)
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

            yield return ScheduledTaskHelpers.GetTaskInfo(task);
        }
    }

    /// <summary>
    /// Get task by id.
    /// </summary>
    /// <param name="taskId">Task Id.</param>
    /// <response code="200">Task retrieved.</response>
    /// <response code="404">Task not found.</response>
    /// <returns>An <see cref="OkResult"/> containing the task on success, or a <see cref="NotFoundResult"/> if the task could not be found.</returns>
    [HttpGet("{taskId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<TaskInfo> GetTask([FromRoute, Required] string taskId)
    {
        var task = _taskManager.ScheduledTasks.FirstOrDefault(i =>
            string.Equals(i.Id, taskId, StringComparison.OrdinalIgnoreCase));

        if (task is null)
        {
            return NotFound();
        }

        return ScheduledTaskHelpers.GetTaskInfo(task);
    }

    /// <summary>
    /// Start specified task.
    /// </summary>
    /// <param name="taskId">Task Id.</param>
    /// <response code="204">Task started.</response>
    /// <response code="404">Task not found.</response>
    /// <returns>An <see cref="NoContentResult"/> on success, or a <see cref="NotFoundResult"/> if the file could not be found.</returns>
    [HttpPost("Running/{taskId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult StartTask([FromRoute, Required] string taskId)
    {
        var task = _taskManager.ScheduledTasks.FirstOrDefault(o =>
            o.Id.Equals(taskId, StringComparison.OrdinalIgnoreCase));

        if (task is null)
        {
            return NotFound();
        }

        _taskManager.Execute(task, new TaskOptions());
        return NoContent();
    }

    /// <summary>
    /// Stop specified task.
    /// </summary>
    /// <param name="taskId">Task Id.</param>
    /// <response code="204">Task stopped.</response>
    /// <response code="404">Task not found.</response>
    /// <returns>An <see cref="OkResult"/> on success, or a <see cref="NotFoundResult"/> if the file could not be found.</returns>
    [HttpDelete("Running/{taskId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult StopTask([FromRoute, Required] string taskId)
    {
        var task = _taskManager.ScheduledTasks.FirstOrDefault(o =>
            o.Id.Equals(taskId, StringComparison.OrdinalIgnoreCase));

        if (task is null)
        {
            return NotFound();
        }

        _taskManager.Cancel(task);
        return NoContent();
    }

    /// <summary>
    /// Update specified task triggers.
    /// </summary>
    /// <param name="taskId">Task Id.</param>
    /// <param name="triggerInfos">Triggers.</param>
    /// <response code="204">Task triggers updated.</response>
    /// <response code="404">Task not found.</response>
    /// <returns>An <see cref="OkResult"/> on success, or a <see cref="NotFoundResult"/> if the file could not be found.</returns>
    [HttpPost("{taskId}/Triggers")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult UpdateTask(
        [FromRoute, Required] string taskId,
        [FromBody, Required] TaskTriggerInfo[] triggerInfos)
    {
        var task = _taskManager.ScheduledTasks.FirstOrDefault(o =>
            o.Id.Equals(taskId, StringComparison.OrdinalIgnoreCase));
        if (task is null)
        {
            return NotFound();
        }

        task.Triggers = triggerInfos;
        return NoContent();
    }
}
