using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Playback activity controller.
/// </summary>
[ApiController]
[Route("Users/{userId}/PlaybackStats")]
public class PlaybackActivityController : BaseJellyfinApiController
{
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="PlaybackActivityController"/> class.
    /// </summary>
    /// <param name="dbProvider">Instance of
    /// <see cref="IDbContextFactory{JellyfinDbContext}"/>.</param>
    public PlaybackActivityController(
        IDbContextFactory<JellyfinDbContext> dbProvider)
    {
        _dbProvider = dbProvider;
    }

    /// <summary>
    /// Gets playback statistics for a user, grouped by media type.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <response code="200">Playback stats returned.</response>
    /// <returns>Playback stats grouped by media type.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlaybackStats([FromRoute] Guid userId)
    {
        using var dbContext = await _dbProvider.CreateDbContextAsync(CancellationToken.None).ConfigureAwait(false);

        var stats = await dbContext.PlaybackActivity
            .Where(p => p.UserId.Equals(userId))
            .GroupBy(p => p.MediaType)
            .Select(g => new
            {
                MediaType = g.Key,
                TotalTicks = g.Sum(p => p.PlayedTicks),
                PlayCount = g.Count()
            })
            .ToListAsync()
            .ConfigureAwait(false);

        return new OkObjectResult(stats);
    }

    /// <summary>
    /// Gets playback statistics for a user, filtered by media type.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="mediaType">The media type to filter by.</param>
    /// <response code="200">Playback stats returned.</response>
    /// <returns>Playback stats filtered by media type.</returns>
    [HttpGet("{mediaType}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlaybackStatsByType(
        [FromRoute] Guid userId,
        [FromRoute] string mediaType)
    {
        using var dbContext = await _dbProvider.CreateDbContextAsync(CancellationToken.None).ConfigureAwait(false);

        var stats = await dbContext.PlaybackActivity
            .Where(p => p.UserId.Equals(userId) && p.MediaType == mediaType)
            .GroupBy(p => p.ItemSubGroup)
            .Select(g => new
            {
                SubGroup = g.Key,
                TotalTicks = g.Sum(p => p.PlayedTicks),
                PlayCount = g.Count()
            })
            .ToListAsync(CancellationToken.None)
            .ConfigureAwait(false);

        return new OkObjectResult(stats);
    }
}
