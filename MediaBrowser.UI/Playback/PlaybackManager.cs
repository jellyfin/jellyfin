using MediaBrowser.Common.Events;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Kernel;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.UI.Configuration;
using MediaBrowser.UI.Controller;
using MediaBrowser.UI.Playback.InternalPlayer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.UI.Playback
{
    /// <summary>
    /// Class PlaybackManager
    /// </summary>
    public class PlaybackManager : BaseManager<UIKernel>
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaybackManager" /> class.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        public PlaybackManager(UIKernel kernel, ILogger logger)
            : base(kernel)
        {
            _logger = logger;
        }

        #region PlaybackStarted Event
        /// <summary>
        /// Occurs when [playback started].
        /// </summary>
        public event EventHandler<PlaybackEventArgs> PlaybackStarted;

        /// <summary>
        /// Called when [playback started].
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="options">The options.</param>
        /// <param name="playerConfiguration">The player configuration.</param>
        private void OnPlaybackStarted(BaseMediaPlayer player, PlayOptions options, PlayerConfiguration playerConfiguration)
        {
            EventHelper.QueueEventIfNotNull(PlaybackStarted, this, new PlaybackEventArgs
            {
                Options = options,
                Player = player,
                PlayerConfiguration = playerConfiguration
            }, _logger);
        }
        #endregion

        #region PlaybackCompleted Event
        /// <summary>
        /// Occurs when [playback completed].
        /// </summary>
        public event EventHandler<PlaybackStopEventArgs> PlaybackCompleted;

        /// <summary>
        /// Called when [playback completed].
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="items">The items.</param>
        internal void OnPlaybackCompleted(BaseMediaPlayer player, List<BaseItemDto> items)
        {
            EventHelper.QueueEventIfNotNull(PlaybackCompleted, this, new PlaybackStopEventArgs
            {
                Items = items,
                Player = player
            }, _logger);
        }
        #endregion

        /// <summary>
        /// Gets the active players.
        /// </summary>
        /// <value>The active players.</value>
        public IEnumerable<BaseMediaPlayer> ActivePlayers
        {
            get
            {
                return Kernel.MediaPlayers.Where(m => m.PlayState != PlayState.Idle);
            }
        }

        /// <summary>
        /// Gets the active internal players.
        /// </summary>
        /// <value>The active internal players.</value>
        public IEnumerable<BaseMediaPlayer> ActiveInternalPlayers
        {
            get { return ActivePlayers.Where(p => p is BaseInternalMediaPlayer); }
        }

        /// <summary>
        /// Plays the specified items.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentException"></exception>
        /// <exception cref="System.InvalidOperationException"></exception>
        public async Task Play(PlayOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            if (options.Items == null || options.Items.Count == 0)
            {
                throw new ArgumentNullException("options");
            }

            var player = GetPlayer(options.Items);

            if (player != null)
            {
                await StopAllPlayback();

                await Play(player.Item1, options, player.Item2);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Plays the specified player.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="options">The options.</param>
        /// <param name="playerConfiguration">The player configuration.</param>
        /// <returns>Task.</returns>
        private async Task Play(BaseMediaPlayer player, PlayOptions options, PlayerConfiguration playerConfiguration)
        {
            if (options.Shuffle)
            {
                options.Items = options.Items.Shuffle().ToList();
            }

            var firstItem = options.Items[0];

            if (options.StartPositionTicks == 0 && player.SupportsMultiFilePlayback && firstItem.IsVideo && firstItem.LocationType == LocationType.FileSystem)
            {
                try
                {
                    var intros = await UIKernel.Instance.ApiClient.GetIntrosAsync(firstItem.Id, App.Instance.CurrentUser.Id);

                    options.Items.InsertRange(0, intros.Select(GetPlayableItem));
                }
                catch (HttpException ex)
                {
                    _logger.ErrorException("Error retrieving intros", ex);
                }
            }

            await player.Play(options, playerConfiguration);

            OnPlaybackStarted(player, options, playerConfiguration);
        }

        /// <summary>
        /// Gets the playable item.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>BaseItemDto.</returns>
        public BaseItemDto GetPlayableItem(string path)
        {
            return new BaseItemDto
            {
                Path = path,
                Name = Path.GetFileName(path),
                Type = "Video",
                VideoType = VideoType.VideoFile,
                IsFolder = false
            };
        }

        /// <summary>
        /// Gets the playable item.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="name">The name.</param>
        /// <returns>BaseItemDto.</returns>
        public BaseItemDto GetPlayableItem(Uri uri, string name)
        {
            return new BaseItemDto
            {
                Path = uri.ToString(),
                Name = name,
                Type = "Video",
                VideoType = VideoType.VideoFile,
                IsFolder = false,
                LocationType = LocationType.Remote
            };
        }

        /// <summary>
        /// Stops all playback.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task StopAllPlayback()
        {
            var tasks = Kernel.MediaPlayers.Where(p => p.PlayState == PlayState.Playing || p.PlayState == PlayState.Paused).Select(p => p.Stop());

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Gets the player.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <returns>BaseMediaPlayer.</returns>
        private Tuple<BaseMediaPlayer, PlayerConfiguration> GetPlayer(List<BaseItemDto> items)
        {
            var player = GetConfiguredPlayer(items);

            if (player != null)
            {
                return player;
            }

            // If there's no explicit configuration just find the first matching player
            var mediaPlayer = Kernel.MediaPlayers.OfType<BaseInternalMediaPlayer>().FirstOrDefault(p => items.All(p.CanPlay));

            if (mediaPlayer != null)
            {
                return new Tuple<BaseMediaPlayer, PlayerConfiguration>(mediaPlayer, null);
            }

            return null;
        }

        /// <summary>
        /// Gets the configured player.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <returns>BaseMediaPlayer.</returns>
        private Tuple<BaseMediaPlayer, PlayerConfiguration> GetConfiguredPlayer(List<BaseItemDto> items)
        {
            if (UIKernel.Instance.Configuration.MediaPlayers == null)
            {
                return null;
            }

            return UIKernel.Instance.Configuration.MediaPlayers.Where(p => IsConfiguredToPlay(p, items))
                           .Select(p => new Tuple<BaseMediaPlayer, PlayerConfiguration>(UIKernel.Instance.MediaPlayers.FirstOrDefault(m => m.Name.Equals(p.PlayerName, StringComparison.OrdinalIgnoreCase)), p))
                           .FirstOrDefault(p => p.Item1 != null);
        }

        /// <summary>
        /// Determines whether [is configured to play] [the specified configuration].
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="items">The items.</param>
        /// <returns><c>true</c> if [is configured to play] [the specified configuration]; otherwise, <c>false</c>.</returns>
        private bool IsConfiguredToPlay(PlayerConfiguration configuration, List<BaseItemDto> items)
        {
            if (configuration.ItemTypes != null && configuration.ItemTypes.Length > 0)
            {
                if (items.Any(i => !configuration.ItemTypes.Contains(i.Type, StringComparer.OrdinalIgnoreCase)))
                {
                    return false;
                }
            }

            if (configuration.FileExtensions != null && configuration.FileExtensions.Length > 0)
            {
                if (items.Any(i => !configuration.FileExtensions.Select(ext => ext.TrimStart('.')).Contains((Path.GetExtension(i.Path) ?? string.Empty).TrimStart('.'), StringComparer.OrdinalIgnoreCase)))
                {
                    return false;
                }
            }

            if (configuration.VideoTypes != null && configuration.VideoTypes.Length > 0)
            {
                if (items.Any(i => i.VideoType.HasValue && !configuration.VideoTypes.Contains(i.VideoType.Value)))
                {
                    return false;
                }
            }

            if (configuration.VideoFormats != null && configuration.VideoFormats.Length > 0)
            {
                if (items.Any(i => i.VideoFormat.HasValue && !configuration.VideoFormats.Contains(i.VideoFormat.Value)))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
