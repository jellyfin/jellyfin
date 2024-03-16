using System;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Filters;

/// <summary>
/// Action filter to assess whether a user can access an item.
/// </summary>
public sealed class ItemAccessFilter : IActionFilter
{
    private const string ItemIdArgument = "itemId";
    private const string UserIdArgument = "userId";

    private readonly ILogger<ItemAccessFilter> _logger;
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemAccessFilter"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="userManager">The user manager.</param>
    public ItemAccessFilter(
        ILogger<ItemAccessFilter> logger,
        ILibraryManager libraryManager,
        IUserManager userManager)
    {
        _logger = logger;
        _libraryManager = libraryManager;
        _userManager = userManager;
    }

    /// <inheritdoc />
    public void OnActionExecuting(ActionExecutingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            // Non-authenticated request.
            return;
        }

        if (context.HttpContext.User.GetIsApiKey())
        {
            _ = GetItem(context);
            return;
        }

        Guid userId;
        if (context.ActionArguments.TryGetValue(UserIdArgument, out var userIdObj))
        {
            var requestUserId = userIdObj as Guid?;
            userId = RequestHelpers.GetUserId(context.HttpContext.User, requestUserId);
        }
        else
        {
            userId = context.HttpContext.User.GetUserId();
        }

        var user = _userManager.GetUserById(userId);
        if (user is null)
        {
            context.Result = new NotFoundResult();
            return;
        }

        var item = GetItem(context);
        if (item is null)
        {
            return;
        }

        if (!item.IsVisibleStandalone(user))
        {
            _logger.LogWarning("User {UserId} attempted to access {ItemId}", userId, item.Id);
            context.Result = new ForbidResult();
        }
    }

    /// <inheritdoc />
    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    private BaseItem? GetItem(ActionExecutingContext context)
    {
        if (!context.ActionArguments.TryGetValue(ItemIdArgument, out var itemIdObj)
            || itemIdObj is not Guid itemId)
        {
            // No item id in route or query.
            return null;
        }

        var item = itemId.IsEmpty()
            ? _libraryManager.GetUserRootFolder()
            : _libraryManager.GetItemById(itemId);

        if (item is not null)
        {
            return item;
        }

        context.Result = new NotFoundResult();
        return null;
    }
}
