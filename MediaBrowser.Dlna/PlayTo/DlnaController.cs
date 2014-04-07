using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Dlna.PlayTo
{
    public class PlayToController : ISessionController, IDisposable
    {
        private Device _device;
        private BaseItem _currentItem;
        private readonly SessionInfo _session;
        private readonly ISessionManager _sessionManager;
        private readonly IItemRepository _itemRepository;
        private readonly ILibraryManager _libraryManager;
        private readonly INetworkManager _networkManager;
        private readonly ILogger _logger;
        private readonly IDlnaManager _dlnaManager;
        private readonly IUserManager _userManager;
        private readonly IServerApplicationHost _appHost;
        private bool _playbackStarted;

        private const int UpdateTimerIntervalMs = 1000;

        public bool SupportsMediaRemoteControl
        {
            get { return true; }
        }

        public bool IsSessionActive
        {
            get
            {
                if (_device == null || _device.UpdateTime == default(DateTime))
                    return false;

                return DateTime.UtcNow <= _device.UpdateTime.AddSeconds(30);
            }
        }

        public PlayToController(SessionInfo session, ISessionManager sessionManager, IItemRepository itemRepository, ILibraryManager libraryManager, ILogger logger, INetworkManager networkManager, IDlnaManager dlnaManager, IUserManager userManager, IServerApplicationHost appHost)
        {
            _session = session;
            _itemRepository = itemRepository;
            _sessionManager = sessionManager;
            _libraryManager = libraryManager;
            _networkManager = networkManager;
            _dlnaManager = dlnaManager;
            _userManager = userManager;
            _appHost = appHost;
            _logger = logger;
        }

        public void Init(Device device)
        {
            _device = device;
            _device.PlaybackChanged += Device_PlaybackChanged;
            _device.CurrentIdChanged += Device_CurrentIdChanged;
            _device.Start();

            _updateTimer = new Timer(updateTimer_Elapsed, null, UpdateTimerIntervalMs, UpdateTimerIntervalMs);
        }

        #region Device EventHandlers & Update Timer

        Timer _updateTimer;

        async void Device_PlaybackChanged(object sender, TransportStateEventArgs e)
        {
            if (_currentItem == null)
                return;

            if (e.Stopped == false)
                await ReportProgress().ConfigureAwait(false);

            else if (e.Stopped && _playbackStarted)
            {
                _playbackStarted = false;

                await _sessionManager.OnPlaybackStopped(new Controller.Session.PlaybackStopInfo
                {
                    Item = _currentItem,
                    SessionId = _session.Id,
                    PositionTicks = _device.Position.Ticks

                }).ConfigureAwait(false);

                await SetNext().ConfigureAwait(false);
            }
        }

        async void Device_CurrentIdChanged(object sender, CurrentIdEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Id))
            {
                Guid guid;

                if (Guid.TryParse(e.Id, out guid))
                {
                    if (_currentItem != null && _currentItem.Id == guid)
                    {
                        return;
                    }

                    var item = _libraryManager.GetItemById(guid);

                    if (item != null)
                    {
                        _logger.Debug("{0} - CurrentId {1}", _session.DeviceName, item.Id);
                        _currentItem = item;
                        _playbackStarted = false;

                        await ReportProgress().ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>
        /// Handles the Elapsed event of the updateTimer control.
        /// </summary>
        /// <param name="state">The state.</param>
        private async void updateTimer_Elapsed(object state)
        {
            if (_disposed)
                return;

            if (IsSessionActive)
            {
                await ReportProgress().ConfigureAwait(false);
            }
            else
            {
                _updateTimer.Change(Timeout.Infinite, Timeout.Infinite);

                try
                {
                    // Session is inactive, mark it for Disposal and don't start the elapsed timer.
                    await _sessionManager.ReportSessionEnded(_session.Id);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error in ReportSessionEnded", ex);
                }
            }
        }

        /// <summary>
        /// Reports the playback progress.
        /// </summary>
        /// <returns></returns>
        private async Task ReportProgress()
        {
            if (_currentItem == null || _device.IsStopped)
                return;

            var playlistItem = Playlist.FirstOrDefault(p => p.PlayState == 1);
            
            if (playlistItem != null)
            {
                if (!_playbackStarted)
                {
                    await _sessionManager.OnPlaybackStart(new PlaybackInfo
                    {
                        Item = _currentItem,
                        SessionId = _session.Id,
                        CanSeek = true,
                        QueueableMediaTypes = new List<string> { _currentItem.MediaType },
                        MediaSourceId = playlistItem.MediaSourceId,
                        AudioStreamIndex = playlistItem.AudioStreamIndex,
                        SubtitleStreamIndex = playlistItem.SubtitleStreamIndex

                    }).ConfigureAwait(false);

                    _playbackStarted = true;
                }

                if ((_device.IsPlaying || _device.IsPaused))
                {
                    var ticks = _device.Position.Ticks;

                    if (playlistItem.Transcode)
                    {
                        ticks += playlistItem.StartPositionTicks;
                    }

                    await _sessionManager.OnPlaybackProgress(new Controller.Session.PlaybackProgressInfo
                    {
                        Item = _currentItem,
                        SessionId = _session.Id,
                        PositionTicks = ticks,
                        IsMuted = _device.IsMuted,
                        IsPaused = _device.IsPaused,
                        MediaSourceId = playlistItem.MediaSourceId,
                        AudioStreamIndex = playlistItem.AudioStreamIndex,
                        SubtitleStreamIndex = playlistItem.SubtitleStreamIndex

                    }).ConfigureAwait(false);
                }
            }
        }

        #endregion

        #region SendCommands

        public async Task SendPlayCommand(PlayRequest command, CancellationToken cancellationToken)
        {
            _logger.Debug("{0} - Received PlayRequest: {1}", this._session.DeviceName, command.PlayCommand);

            var items = new List<BaseItem>();
            foreach (string id in command.ItemIds)
            {
                AddItemFromId(Guid.Parse(id), items);
            }

            var playlist = new List<PlaylistItem>();
            var isFirst = true;

            var serverAddress = GetServerAddress();

            foreach (var item in items)
            {
                if (isFirst && command.StartPositionTicks.HasValue)
                {
                    playlist.Add(CreatePlaylistItem(item, command.StartPositionTicks.Value, serverAddress));
                    isFirst = false;
                }
                else
                {
                    playlist.Add(CreatePlaylistItem(item, 0, serverAddress));
                }
            }

            _logger.Debug("{0} - Playlist created", _session.DeviceName);

            if (command.PlayCommand == PlayCommand.PlayLast)
            {
                AddItemsToPlaylist(playlist);
            }
            if (command.PlayCommand == PlayCommand.PlayNext)
            {
                AddItemsToPlaylist(playlist);
            }

            _logger.Debug("{0} - Playing {1} items", _session.DeviceName, playlist.Count);

            if (!string.IsNullOrWhiteSpace(command.ControllingUserId))
            {
                var userId = new Guid(command.ControllingUserId);

                var user = _userManager.GetUserById(userId);

                await _sessionManager.LogSessionActivity(_session.Client, _session.ApplicationVersion, _session.DeviceId,
                        _session.DeviceName, _session.RemoteEndPoint, user).ConfigureAwait(false);
            }

            await PlayItems(playlist).ConfigureAwait(false);
        }

        public Task SendPlaystateCommand(PlaystateRequest command, CancellationToken cancellationToken)
        {
            switch (command.Command)
            {
                case PlaystateCommand.Stop:
                    Playlist.Clear();
                    return _device.SetStop();

                case PlaystateCommand.Pause:
                    return _device.SetPause();

                case PlaystateCommand.Unpause:
                    return _device.SetPlay();

                case PlaystateCommand.Seek:
                    //var playlistItem = Playlist.FirstOrDefault(p => p.PlayState == 1);
                    //if (playlistItem != null && playlistItem.Transcode && _currentItem != null)
                    //{
                    //    var newItem = CreatePlaylistItem(_currentItem, command.SeekPositionTicks ?? 0, GetServerAddress());
                    //    playlistItem.StartPositionTicks = newItem.StartPositionTicks;
                    //    playlistItem.StreamUrl = newItem.StreamUrl;
                    //    playlistItem.Didl = newItem.Didl;
                    //    return _device.SetAvTransport(playlistItem.StreamUrl, GetDlnaHeaders(playlistItem), playlistItem.Didl);

                    //}
                    return _device.Seek(TimeSpan.FromTicks(command.SeekPositionTicks ?? 0));


                case PlaystateCommand.NextTrack:
                    _currentItem = null;
                    return SetNext();

                case PlaystateCommand.PreviousTrack:
                    _currentItem = null;
                    return SetPrevious();
            }

            return Task.FromResult(true);
        }

        public Task SendUserDataChangeInfo(UserDataChangeInfo info, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task SendRestartRequiredNotification(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task SendServerRestartNotification(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task SendSessionEndedNotification(SessionInfoDto sessionInfo, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task SendServerShutdownNotification(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task SendBrowseCommand(BrowseRequest command, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task SendLibraryUpdateInfo(LibraryUpdateInfo info, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task SendMessageCommand(MessageCommand command, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        #endregion

        #region Playlist

        private List<PlaylistItem> _playlist = new List<PlaylistItem>();

        private List<PlaylistItem> Playlist
        {
            get
            {
                return _playlist;
            }
            set
            {
                _playlist = value;
            }
        }

        private void AddItemFromId(Guid id, List<BaseItem> list)
        {
            var item = _libraryManager.GetItemById(id);
            if (item.IsFolder)
            {
                foreach (var childId in _itemRepository.GetChildren(item.Id))
                {
                    AddItemFromId(childId, list);
                }
            }
            else
            {
                if (item.MediaType == MediaType.Audio || item.MediaType == MediaType.Video)
                {
                    list.Add(item);
                }
            }
        }

        private string GetServerAddress()
        {
            return string.Format("{0}://{1}:{2}/mediabrowser",

                "http",
                _networkManager.GetLocalIpAddresses().FirstOrDefault() ?? "localhost",
                _appHost.HttpServerPort
                );
        }

        private PlaylistItem CreatePlaylistItem(BaseItem item, long startPostionTicks, string serverAddress)
        {
            var streams = _itemRepository.GetMediaStreams(new MediaStreamQuery
            {
                ItemId = item.Id

            }).ToList();

            var deviceInfo = _device.Properties;

            var profile = _dlnaManager.GetProfile(deviceInfo.ToDeviceIdentification()) ??
                _dlnaManager.GetDefaultProfile();

            var playlistItem = GetPlaylistItem(item, streams, profile);
            playlistItem.StartPositionTicks = startPostionTicks;
            playlistItem.DeviceProfileId = profile.Id;

            if (playlistItem.MediaType == DlnaProfileType.Audio)
            {
                playlistItem.StreamUrl = StreamHelper.GetAudioUrl(deviceInfo, playlistItem, streams, serverAddress);
            }
            else
            {
                playlistItem.StreamUrl = StreamHelper.GetVideoUrl(_device.Properties, playlistItem, streams, serverAddress);
            }

            playlistItem.Didl = DidlBuilder.Build(item, _session.UserId.ToString(), serverAddress, playlistItem.StreamUrl, streams, profile.EnableAlbumArtInDidl);

            return playlistItem;
        }

        private string GetDlnaHeaders(PlaylistItem item)
        {
            var orgOp = item.Transcode ? ";DLNA.ORG_OP=00" : ";DLNA.ORG_OP=01";

            var orgCi = item.Transcode ? ";DLNA.ORG_CI=0" : ";DLNA.ORG_CI=1";

            const string dlnaflags = ";DLNA.ORG_FLAGS=01500000000000000000000000000000";

            string contentFeatures;

            var container = item.Container.TrimStart('.');

            if (string.Equals(container, "mp3", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_PN=MP3";
            }
            else if (string.Equals(container, "wma", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_PN=WMABASE";
            }
            else if (string.Equals(container, "wmw", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_PN=WMVMED_BASE";
            }
            else if (string.Equals(container, "asf", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_PN=WMVMED_BASE";
            }
            else if (string.Equals(container, "avi", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_PN=AVI";
            }
            else if (string.Equals(container, "mkv", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_PN=MATROSKA";
            }
            else if (string.Equals(container, "mp4", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_PN=AVC_MP4_MP_HD_720p_AAC";
            }
            else if (string.Equals(container, "mpeg", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_PN=MPEG_PS_PAL";
            }
            else if (string.Equals(container, "ts", StringComparison.OrdinalIgnoreCase))
            {
                contentFeatures = "DLNA.ORG_PN=MPEG_PS_PAL";
            }
            else if (item.MediaType == DlnaProfileType.Video)
            {
                // Default to AVI for video
                contentFeatures = "DLNA.ORG_PN=AVI";
            }
            else
            {
                // Default to MP3 for audio
                contentFeatures = "DLNA.ORG_PN=MP3";
            }

            return (contentFeatures + orgOp + orgCi + dlnaflags).Trim(';');
        }

        private PlaylistItem GetPlaylistItem(BaseItem item, List<MediaStream> mediaStreams, DeviceProfile profile)
        {
            var video = item as Video;

            if (video != null)
            {
                return new PlaylistItemFactory().Create(video, mediaStreams, profile);
            }

            var audio = item as Audio;

            if (audio != null)
            {
                return new PlaylistItemFactory().Create(audio, mediaStreams, profile);
            }

            var photo = item as Photo;

            if (photo != null)
            {
                return new PlaylistItemFactory().Create(photo, profile);
            }

            throw new ArgumentException("Unrecognized item type.");
        }

        /// <summary>
        /// Plays the items.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <returns></returns>
        private async Task<bool> PlayItems(IEnumerable<PlaylistItem> items)
        {
            Playlist.Clear();
            Playlist.AddRange(items);
            await SetNext();
            return true;
        }

        /// <summary>
        /// Adds the items to playlist.
        /// </summary>
        /// <param name="items">The items.</param>
        private void AddItemsToPlaylist(IEnumerable<PlaylistItem> items)
        {
            Playlist.AddRange(items);
        }

        private async Task<bool> SetNext()
        {
            if (!Playlist.Any() || Playlist.All(i => i.PlayState != 0))
            {
                return true;
            }
            var currentitem = Playlist.FirstOrDefault(i => i.PlayState == 1);

            if (currentitem != null)
            {
                currentitem.PlayState = 2;
            }

            var nextTrack = Playlist.FirstOrDefault(i => i.PlayState == 0);
            if (nextTrack == null)
            {
                await _device.SetStop();
                return true;
            }

            nextTrack.PlayState = 1;

            var dlnaheaders = GetDlnaHeaders(nextTrack);

            _logger.Debug("{0} - SetAvTransport Uri: {1} DlnaHeaders: {2}", _device.Properties.Name, nextTrack.StreamUrl, dlnaheaders);

            await _device.SetAvTransport(nextTrack.StreamUrl, dlnaheaders, nextTrack.Didl);

            if (nextTrack.StartPositionTicks > 0 && !nextTrack.Transcode)
                await _device.Seek(TimeSpan.FromTicks(nextTrack.StartPositionTicks));

            return true;
        }

        public Task<bool> SetPrevious()
        {
            if (!Playlist.Any() || Playlist.All(i => i.PlayState != 2))
                return Task.FromResult(false);

            var currentitem = Playlist.FirstOrDefault(i => i.PlayState == 1);

            var prevTrack = Playlist.LastOrDefault(i => i.PlayState == 2);

            if (currentitem != null)
            {
                currentitem.PlayState = 0;
            }

            if (prevTrack == null)
                return Task.FromResult(false);

            prevTrack.PlayState = 1;
            return _device.SetAvTransport(prevTrack.StreamUrl, GetDlnaHeaders(prevTrack), prevTrack.Didl);
        }

        #endregion

        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _updateTimer.Dispose();
                _device.Dispose();
                _logger.Log(LogSeverity.Debug, "Controller disposed");
            }
        }

        public Task SendGeneralCommand(GeneralCommand command, CancellationToken cancellationToken)
        {
            GeneralCommandType commandType;

            if (!Enum.TryParse(command.Name, true, out commandType))
            {
                switch (commandType)
                {
                    case GeneralCommandType.VolumeDown:
                        return _device.VolumeDown();
                    case GeneralCommandType.VolumeUp:
                        return _device.VolumeUp();
                    case GeneralCommandType.Mute:
                        return _device.VolumeDown(true);
                    case GeneralCommandType.Unmute:
                        return _device.VolumeUp(true);
                    case GeneralCommandType.ToggleMute:
                        return _device.ToggleMute();
                    default:
                        return Task.FromResult(true);
                }
            }

            return Task.FromResult(true);
        }
    }
}
