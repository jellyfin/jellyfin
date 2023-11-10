#pragma warning disable CA1307

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.MediaSegments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.MediaSegments
{
    /// <summary>
    /// Manages the creation and retrieval of <see cref="MediaSegment"/> instances.
    /// </summary>
    public class MediaSegmentsManager : IMediaSegmentsManager
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
        private readonly IUserManager _userManager;
        private readonly ILogger<MediaSegmentsManager> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaSegmentsManager"/> class.
        /// </summary>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="dbProvider">The database provider.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="logger">The logger.</param>
        public MediaSegmentsManager(
            ILibraryManager libraryManager,
            IDbContextFactory<JellyfinDbContext> dbProvider,
            IUserManager userManager,
            ILogger<MediaSegmentsManager> logger)
        {
            _libraryManager = libraryManager;
            _libraryManager.ItemRemoved += LibraryManagerItemRemoved;

            _dbProvider = dbProvider;
            _userManager = userManager;
            _logger = logger;
        }

        // <inheritdoc/>
        // public event EventHandler<GenericEventArgs<User>>? OnUserUpdated;

        /// <inheritdoc/>
        public async Task<MediaSegment> CreateMediaSegmentAsync(MediaSegment segment)
        {
            var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                ValidateSegment(segment);

                var found = dbContext.Segments.Where(s => s.ItemId.Equals(segment.ItemId) && s.Type.Equals(segment.Type) && s.TypeIndex.Equals(segment.TypeIndex)).FirstOrDefault();

                AddOrUpdateSegment(dbContext, segment, found);

                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }

            return segment;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<MediaSegment>> CreateMediaSegmentsAsync(IEnumerable<MediaSegment> segments)
        {
            var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                var foundSegments = dbContext.Segments.Select(s => s);

                foreach (var segment in segments)
                {
                    ValidateSegment(segment);

                    var found = foundSegments.Where(s => s.ItemId.Equals(segment.ItemId) && s.StreamIndex.Equals(segment.StreamIndex) && s.Type.Equals(segment.Type) && s.TypeIndex.Equals(segment.TypeIndex)).FirstOrDefault();

                    AddOrUpdateSegment(dbContext, segment, found);
                }

                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }

            return segments;
        }

        /// <inheritdoc/>
        public List<MediaSegment> GetAllMediaSegments(Guid itemId = default, int streamIndex = -1, int typeIndex = -1, MediaSegmentType? type = null)
        {
            var allSegments = new List<MediaSegment>();

            var dbContext = _dbProvider.CreateDbContext();
            using (dbContext)
            {
                IQueryable<MediaSegment> queryable = dbContext.Segments.Select(s => s);

                if (!itemId.Equals(default))
                {
                    queryable = queryable.Where(s => s.ItemId.Equals(itemId));
                }

                if (!streamIndex.Equals(-1))
                {
                    queryable = queryable.Where(s => s.StreamIndex.Equals(streamIndex));
                }

                if (!type.Equals(null))
                {
                    queryable = queryable.Where(s => s.Type.Equals(type));
                }

                if (typeIndex > -1)
                {
                    queryable = queryable.Where(s => s.TypeIndex.Equals(typeIndex));
                }

                allSegments = queryable.AsNoTracking().ToList();
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
        /// TODO: Do not block.
        /// </summary>
        /// <param name="sender">The sending entity.</param>
        /// <param name="itemChangeEventArgs">The <see cref="ItemChangeEventArgs"/>.</param>
        private void LibraryManagerItemRemoved(object? sender, ItemChangeEventArgs itemChangeEventArgs)
        {
            var task = Task.Run(async () => { await DeleteSegmentsAsync(itemChangeEventArgs.Item.Id).ConfigureAwait(false); });
            task.Wait();
        }

        /// <inheritdoc/>
        public async Task<List<MediaSegment>> DeleteSegmentsAsync(Guid itemId, int streamIndex = -1, int typeIndex = -1, MediaSegmentType? type = null)
        {
            var allSegments = new List<MediaSegment>();

            if (itemId.Equals(default))
            {
                throw new ArgumentException($"itemId is not set. Please provide one.");
            }

            var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                IQueryable<MediaSegment> queryable = dbContext.Segments.Where(s => s.ItemId.Equals(itemId));

                if (!streamIndex.Equals(-1))
                {
                    queryable = queryable.Where(s => s.StreamIndex.Equals(streamIndex));
                }

                if (!type.Equals(null))
                {
                    queryable = queryable.Where(s => s.Type.Equals(type));
                }

                if (typeIndex > -1)
                {
                    queryable = queryable.Where(s => s.TypeIndex.Equals(typeIndex));
                }

                allSegments = queryable.AsNoTracking().ToList();

                dbContext.Segments.RemoveRange(allSegments);
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }

            return allSegments;
        }
    }
}
