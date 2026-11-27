using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Jellyfin.Data.Events;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Images
{
    /// <summary>
    /// Re-checks the library collages when an item is saved with a tag that some user has blocked, or when a
    /// user's blocked tags may have changed, so blocked items disappear from the collages right away.
    /// </summary>
    /// <remarks>
    /// A re-check is a plain metadata refresh of each library: it does not rescan the library contents, and
    /// <see cref="CollectionFolderImageProvider"/> only rebuilds a collage that pictures something now blocked.
    /// </remarks>
    public sealed class CollectionFolderImageBlockWatcher : BackgroundService
    {
        /// <summary>
        /// How long a re-check waits after the change that asked for it, so a burst of changes
        /// (metadata editor saves, library scans) leads to a single re-check.
        /// </summary>
        internal static readonly TimeSpan DefaultDelay = TimeSpan.FromSeconds(2);

        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IProviderManager _providerManager;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<CollectionFolderImageBlockWatcher> _logger;
        private readonly TimeSpan _delay;

        // Holds at most one request, so any number of changes made before a re-check starts lead to that one re-check.
        private readonly Channel<bool> _recheckRequests = Channel.CreateBounded<bool>(
            new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });

        private readonly Lock _blockedTagsLock = new();
        private string[]? _blockedTags;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionFolderImageBlockWatcher"/> class.
        /// </summary>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="providerManager">The provider manager.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="logger">The logger.</param>
        public CollectionFolderImageBlockWatcher(
            ILibraryManager libraryManager,
            IUserManager userManager,
            IProviderManager providerManager,
            IFileSystem fileSystem,
            ILogger<CollectionFolderImageBlockWatcher> logger)
            : this(libraryManager, userManager, providerManager, fileSystem, logger, DefaultDelay)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionFolderImageBlockWatcher"/> class.
        /// </summary>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="providerManager">The provider manager.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="delay">How long a re-check waits after the change that asked for it.</param>
        internal CollectionFolderImageBlockWatcher(
            ILibraryManager libraryManager,
            IUserManager userManager,
            IProviderManager providerManager,
            IFileSystem fileSystem,
            ILogger<CollectionFolderImageBlockWatcher> logger,
            TimeSpan delay)
        {
            _libraryManager = libraryManager;
            _userManager = userManager;
            _providerManager = providerManager;
            _fileSystem = fileSystem;
            _logger = logger;
            _delay = delay;
        }

        /// <inheritdoc />
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _libraryManager.ItemUpdated += OnItemUpdated;
            _userManager.OnUserUpdated += OnUserUpdated;
            return base.StartAsync(cancellationToken);
        }

        /// <inheritdoc />
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _libraryManager.ItemUpdated -= OnItemUpdated;
            _userManager.OnUserUpdated -= OnUserUpdated;

            // Waits for a re-check under way to wind down, so it doesn't outlive the services it uses.
            return base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// Gets whether saving an item may leave a collage picturing something blocked.
        /// </summary>
        /// <param name="item">The saved item.</param>
        /// <param name="blockedTags">The tags blocked by any user.</param>
        /// <returns><c>true</c> if the collages need a re-check.</returns>
        internal static bool MayAffectCollages(BaseItem item, string[] blockedTags)
        {
            // A library's own refresh saves the library, so reacting to that, the re-check's included, would re-check forever.
            return item is not (AggregateFolder or UserRootFolder or CollectionFolder)
                && CollectionFolderImageProvider.IsBlocked(item, blockedTags);
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (await _recheckRequests.Reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
                {
                    // The wait runs from the first change and later ones don't restart it, so a library scan that
                    // keeps saving blocked items can't hold the re-check off until the scan ends.
                    await Task.Delay(_delay, stoppingToken).ConfigureAwait(false);

                    // Taken only now, so this re-check covers the changes made while waiting, and a change made after
                    // this point asks for the next one.
                    _recheckRequests.Reader.TryRead(out _);

                    try
                    {
                        await RecheckAsync(stoppingToken).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                    {
                        // Leaving the loop would stop the server along with it.
                        _logger.LogError(ex, "Error re-checking the library images for blocked items");
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Shutting down.
            }
        }

        private void OnItemUpdated(object? sender, ItemChangeEventArgs e)
        {
            if (MayAffectCollages(e.Item, GetBlockedTags()))
            {
                RequestRecheck();
            }
        }

        private void OnUserUpdated(object? sender, GenericEventArgs<User> e)
        {
            // The user's blocked tags, or the libraries they can see, may have changed.
            ClearBlockedTags();
            RequestRecheck();
        }

        private void RequestRecheck() => _recheckRequests.Writer.TryWrite(true);

        // Item updates are frequent during a scan and reading the users hits the database, so the
        // blocked tags are cached until a user changes or a re-check runs.
        private string[] GetBlockedTags()
        {
            lock (_blockedTagsLock)
            {
                return _blockedTags ??= CollectionFolderImageProvider.GetBlockedTags(_userManager.GetUsers());
            }
        }

        private void ClearBlockedTags()
        {
            lock (_blockedTagsLock)
            {
                _blockedTags = null;
            }
        }

        private async Task RecheckAsync(CancellationToken cancellationToken)
        {
            // Reload the blocked tags from here on, so the tags of a deleted user stop triggering re-checks.
            ClearBlockedTags();

            var folders = _libraryManager.GetUserRootFolder().Children.OfType<CollectionFolder>().ToList();
            foreach (var folder in folders)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await _providerManager.RefreshSingleItem(
                        folder,
                        new MetadataRefreshOptions(new DirectoryService(_fileSystem)),
                        cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    // Includes a provider that gave up on its own, such as on an HTTP timeout, which must not keep
                    // the other libraries from being checked.
                    _logger.LogError(ex, "Error re-checking the image of library {Name}", folder.Name);
                }
            }
        }
    }
}
