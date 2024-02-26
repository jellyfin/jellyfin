using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.MediaSegments;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations.MediaSegments
{
    /// <summary>
    /// Manages the creation and retrieval of <see cref="MediaSegment"/> instances.
    /// </summary>
    public class MediaSegmentsManager : IMediaSegmentsManager, IDisposable
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
        private bool _disposed;

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
        public async Task<IReadOnlyList<MediaSegment>> CreateMediaSegments(IReadOnlyList<MediaSegment> segments)
        {
            var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                foreach (var segment in segments)
                {
                    ValidateSegment(segment);

                    var found = await dbContext.Segments.FirstOrDefaultAsync(s => s.ItemId.Equals(segment.ItemId) && s.StreamIndex == segment.StreamIndex && s.Type == segment.Type && s.TypeIndex == segment.TypeIndex)
                        .ConfigureAwait(false);

                    AddOrUpdateSegment(dbContext, segment, found);
                }

                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }

            return segments;
        }

        /// <inheritdoc/>
        public async Task<List<MediaSegment>> GetAllMediaSegments(Guid itemId = default, int? streamIndex = null, int? typeIndex = null, MediaSegmentType? type = null)
        {
            List<MediaSegment> allSegments;

            var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                IQueryable<MediaSegment> queryable = dbContext.Segments.Select(s => s);

                if (!itemId.IsEmpty())
                {
                    queryable = queryable.Where(s => s.ItemId.Equals(itemId));
                }

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

                allSegments = await queryable.AsNoTracking().ToListAsync().ConfigureAwait(false);
            }

            return allSegments;
        }

        /// <summary>
        /// Add or Update a segment in db.
        /// <param name="dbContext">The db context.</param>
        /// <param name="segment">The segment.</param>
        /// <param name="found">The found segment.</param>
        /// </summary>
        private void AddOrUpdateSegment(JellyfinDbContext dbContext, MediaSegment segment, MediaSegment? found)
        {
            if (found != null)
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
            if (segment.ItemId.Equals(default))
            {
                throw new ArgumentException($"itemId is default: itemId={segment.ItemId} for segment with type '{segment.Type}.{segment.TypeIndex}'");
            }

            if (segment.StartTicks >= segment.EndTicks)
            {
                throw new ArgumentException($"start >= end: {segment.StartTicks}>={segment.EndTicks} for segment itemId '{segment.ItemId}' with type '{segment.Type}.{segment.TypeIndex}'");
            }
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
        public async Task<List<MediaSegment>> DeleteSegments(Guid itemId, int? streamIndex = null, int? typeIndex = null, MediaSegmentType? type = null)
        {
            List<MediaSegment> allSegments;

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

                allSegments = await queryable.ToListAsync().ConfigureAwait(false);

                dbContext.Segments.RemoveRange(allSegments);
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }

            return allSegments;
        }

        /// <summary>
        /// Dispose event.
        /// </summary>
        /// <param name="disposing">dispose.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _libraryManager.ItemRemoved -= LibraryManagerItemRemoved;
                }

                _disposed = true;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
