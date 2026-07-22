using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server
{
    /// <summary>
    /// <see cref="IHostedService"/> that pre-compiles the EF Core query plans for the
    /// ItemsByName endpoints (Artists/AlbumArtists/Studios/Genres/MusicGenres) combined with
    /// a user-data filter. These query shapes build large expression trees whose first-use
    /// compilation (Expression.Compile + JIT) can take tens of seconds on large libraries,
    /// blocking the first request (e.g. loading the Favorites view or logging in). Executing
    /// them once at startup on a background thread moves that one-time cost off the request path.
    /// This does not change any query behaviour; the results are discarded.
    /// </summary>
    public sealed class QueryPlanWarmupHost : IHostedService
    {
        private readonly ILogger<QueryPlanWarmupHost> _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryPlanWarmupHost"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger{TCategoryName}"/>.</param>
        /// <param name="libraryManager">The <see cref="ILibraryManager"/>.</param>
        /// <param name="userManager">The <see cref="IUserManager"/>.</param>
        public QueryPlanWarmupHost(
            ILogger<QueryPlanWarmupHost> logger,
            ILibraryManager libraryManager,
            IUserManager userManager)
        {
            _logger = logger;
            _libraryManager = libraryManager;
            _userManager = userManager;
        }

        /// <inheritdoc />
        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Fire-and-forget on a background thread; never block startup.
            _ = Task.Run(() => Warmup(cancellationToken), cancellationToken);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private void Warmup(CancellationToken cancellationToken)
        {
            var user = _userManager.GetUsers().FirstOrDefault();
            if (user is null)
            {
                return;
            }

            try
            {
                var start = DateTime.UtcNow;
                RunItemsByNameQueries(user);
                _logger.LogInformation(
                    "ItemsByName query plans warmed up in {Elapsed}",
                    DateTime.UtcNow - start);
            }
            catch (OperationCanceledException)
            {
                // Shutting down; ignore.
            }
            catch (Exception ex)
            {
                // Warm-up is best-effort and must never affect server startup.
                _logger.LogDebug(ex, "ItemsByName query plan warm-up failed");
            }
        }

        private void RunItemsByNameQueries(User user)
        {
            InternalItemsQuery Query() => new(user)
            {
                IsFavorite = true,
                Recursive = true,
                Limit = 1
            };

            // Each of these is a distinct query shape with its own one-time plan
            // compilation, and every ILibraryManager call opens its own DbContext,
            // so they can be compiled concurrently to reduce total warm-up time.
            Parallel.Invoke(
                () => _libraryManager.GetArtists(Query()),
                () => _libraryManager.GetAlbumArtists(Query()),
                () => _libraryManager.GetStudios(Query()),
                () => _libraryManager.GetGenres(Query()),
                () => _libraryManager.GetMusicGenres(Query()));
        }
    }
}
