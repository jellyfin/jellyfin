using System;
using System.Linq;
using MediaBrowser.Common.Api;
using MediaBrowser.Model.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Local management controller. Only available to requests made from the server machine itself, so that
/// apps running next to the server can manage it without having to store credentials.
/// </summary>
[Route("LocalManagement")]
[Authorize(Policy = Policies.LoopbackAccess)]
public class LocalManagementController : BaseJellyfinApiController
{
    private const string RefreshLibraryTaskKey = "RefreshLibrary";

    private readonly ITaskManager _taskManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalManagementController"/> class.
    /// </summary>
    /// <param name="taskManager">Instance of the <see cref="ITaskManager"/> interface.</param>
    public LocalManagementController(ITaskManager taskManager)
    {
        _taskManager = taskManager;
    }

    /// <summary>
    /// Starts a library scan.
    /// </summary>
    /// <response code="204">Library scan started.</response>
    /// <response code="404">Library scan task not found.</response>
    /// <returns>A <see cref="NoContentResult"/>, or a <see cref="NotFoundResult"/> if the task does not exist.</returns>
    [HttpPost("Library/Refresh")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult RefreshLibrary()
    {
        var task = _taskManager.ScheduledTasks.FirstOrDefault(o =>
            o.ScheduledTask.Key.Equals(RefreshLibraryTaskKey, StringComparison.Ordinal));

        if (task is null)
        {
            return NotFound();
        }

        _taskManager.Execute(task, new TaskOptions());
        return NoContent();
    }
}
