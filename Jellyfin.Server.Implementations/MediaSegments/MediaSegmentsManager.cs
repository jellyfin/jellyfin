using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Entities.MediaSegment;
using Jellyfin.Data.Enums.MediaSegmentType;
using Jellyfin.Data.Events;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.MediaSegments;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations.MediaSegments;

/// <summary>
/// Manages the creation and retrieval of <see cref="MediaSegment"/> instances.
/// </summary>
public sealed class MediaSegmentsManager : IMediaSegmentsManager, IDisposable
{
    private readonly ILibraryManager _libraryManager;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MediaSegmentsManager"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="dbProvider">The database provider.</param>
    public MediaSegmentsManager(
        ILibraryManager libraryManager,
        IDbContextFactory<JellyfinDbContext> dbProvider)
    {
        _libraryManager = libraryManager;
        _libraryManager.ItemRemoved += LibraryManagerItemRemoved;
        _dbProvider = dbProvider;
    }

    /// <inheritdoc/>
    public event EventHandler<GenericEventArgs<Guid>>? SegmentsAddedOrUpdated;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<MediaSegment>> CreateMediaSegments(Guid itemId, IReadOnlyList<MediaSegment> segments)
    {
        var item = _libraryManager.GetItemById(itemId);
        if (item is null)
        {
            throw new InvalidOperationException("Item not found");
        }

        var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            foreach (var segment in segments)
            {
                segment.ItemId = itemId;
                ValidateSegment(segment);

                var found = await dbContext.Segments.FirstOrDefaultAsync(s => s.ItemId.Equals(segment.ItemId)
                && s.StreamIndex == segment.StreamIndex
                && s.Type == segment.Type
                && s.TypeIndex == segment.TypeIndex)
                .ConfigureAwait(false);

                AddOrUpdateSegment(dbContext, segment, found);
            }

            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        SegmentsAddedOrUpdated?.Invoke(this, new GenericEventArgs<Guid>(itemId));

        return segments;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<MediaSegment>> GetAllMediaSegments(Guid itemId, int? streamIndex = null, int? typeIndex = null, MediaSegmentType? type = null)
    {
        var item = _libraryManager.GetItemById(itemId);
        if (item is null)
        {
            throw new InvalidOperationException("Item not found");
        }

        var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            IQueryable<MediaSegment> queryable = dbContext.Segments.Where(s => s.ItemId.Equals(itemId));

            if (streamIndex is not null)
            {
                queryable = queryable.Where(s => s.StreamIndex == streamIndex.Value);
            }

            if (type is not null)
            {
                queryable = queryable.Where(s => s.Type == type.Value);
            }

            if (!typeIndex.Equals(null))
            {
                queryable = queryable.Where(s => s.TypeIndex == typeIndex.Value);
            }

            return await queryable.AsNoTracking().ToListAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Add or Update a segment in db.
    /// <param name="dbContext">The db context.</param>
    /// <param name="segment">The segment.</param>
    /// <param name="found">The found segment.</param>
    /// </summary>
    private void AddOrUpdateSegment(JellyfinDbContext dbContext, MediaSegment segment, MediaSegment? found)
    {
        if (found is not null)
        {
            found.StartTicks = segment.StartTicks;
            found.EndTicks = segment.EndTicks;
            found.Action = segment.Action;
            found.Comment = segment.Comment;
        }
        else
        {
            dbContext.Segments.Add(segment);
        }
    }

    /// <summary>
    /// Validate a segment: itemId, start >= end and type.
    /// </summary>
    /// <param name="segment">The segment to validate.</param>
    private void ValidateSegment(MediaSegment segment)
    {
        if (segment.ItemId.IsEmpty())
        {
            throw new ArgumentException($"itemId is default: itemId={segment.ItemId} for segment with type '{segment.Type}.{segment.TypeIndex}'");
        }

        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(segment.StartTicks, segment.EndTicks, $"itemId '{segment.ItemId}' with type '{segment.Type}.{segment.TypeIndex}'");
    }

    /// <summary>
    /// Delete all segments when itemid is deleted from library.
    /// </summary>
    /// <param name="sender">The sending entity.</param>
    /// <param name="itemChangeEventArgs">The <see cref="ItemChangeEventArgs"/>.</param>
    private async void LibraryManagerItemRemoved(object? sender, ItemChangeEventArgs itemChangeEventArgs)
    {
        await DeleteSegments(itemChangeEventArgs.Item.Id).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task DeleteSegments(Guid itemId, int? streamIndex = null, int? typeIndex = null, MediaSegmentType? type = null)
    {
        if (itemId.IsEmpty())
        {
            throw new ArgumentException("Default value provided", nameof(itemId));
        }

        var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            IQueryable<MediaSegment> queryable = dbContext.Segments.Where(s => s.ItemId.Equals(itemId));

            if (streamIndex is not null)
            {
                queryable = queryable.Where(s => s.StreamIndex == streamIndex);
            }

            if (type is not null)
            {
                queryable = queryable.Where(s => s.Type == type);
            }

            if (typeIndex is not null)
            {
                queryable = queryable.Where(s => s.TypeIndex == typeIndex);
            }

            await queryable.ExecuteDeleteAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _libraryManager.ItemRemoved -= LibraryManagerItemRemoved;
    }
}
