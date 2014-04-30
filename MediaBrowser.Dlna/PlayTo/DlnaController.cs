using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Session;
using MediaBrowser.Dlna.Didl;
using MediaBrowser.Dlna.Ssdp;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Session;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Dlna.PlayTo
{
    public class PlayToController : ISessionController, IDisposable
    {
        private Device _device;
        private readonly SessionInfo _session;
        private readonly ISessionManager _sessionManager;
        private readonly IItemRepository _itemRepository;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly IDlnaManager _dlnaManager;
        private readonly IUserManager _userManager;
        private readonly IDtoService _dtoService;
        private readonly IImageProcessor _imageProcessor;

        private readonly SsdpHandler _ssdpHandler;
        private readonly string _serverAddress;

        public bool SupportsMediaRemoteControl
        {
            get { return true; }
        }

        public bool IsSessionActive
        {
            get
            {
                return _device != null;
            }
        }

        public PlayToController(SessionInfo session, ISessionManager sessionManager, IItemRepository itemRepository, ILibraryManager libraryManager, ILogger logger, IDlnaManager dlnaManager, IUserManager userManager, IDtoService dtoService, IImageProcessor imageProcessor, SsdpHandler ssdpHandler, string serverAddress)
        {
            _session = session;
            _itemRepository = itemRepository;
            _sessionManager = sessionManager;
            _libraryManager = libraryManager;
            _dlnaManager = dlnaManager;
            _userManager = userManager;
            _dtoService = dtoService;
            _imageProcessor = imageProcessor;
            _ssdpHandler = ssdpHandler;
            _serverAddress = serverAddress;
            _logger = logger;
        }

        public void Init(Device device)
        {
            _device = device;
            _device.PlaybackStart += _device_PlaybackStart;
            _device.PlaybackProgress += _device_PlaybackProgress;
            _device.PlaybackStopped += _device_PlaybackStopped;
            _device.Start();

            _ssdpHandler.MessageReceived += _SsdpHandler_MessageReceived;
        }

        private string GetServerAddress()
        {
            return _serverAddress;
        }

        async void _SsdpHandler_MessageReceived(object sender, SsdpMessageEventArgs e)
        {
            string nts;
            e.Headers.TryGetValue("NTS", out nts);

            string usn;
            if (!e.Headers.TryGetValue("USN", out usn)) usn = string.Empty;

            string nt;
            if (!e.Headers.TryGetValue("NT", out nt)) nt = string.Empty;
            
            if (string.Equals(e.Method, "NOTIFY", StringComparison.OrdinalIgnoreCase) && 
                string.Equals(nts, "ssdp:byebye", StringComparison.OrdinalIgnoreCase) &&
                usn.IndexOf(_device.Properties.UUID, StringComparison.OrdinalIgnoreCase) != -1 &&
                !_disposed)
            {
                if (usn.IndexOf("MediaRenderer:", StringComparison.OrdinalIgnoreCase) != -1 ||
                    nt.IndexOf("MediaRenderer:", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    try
                    {
                        await _sessionManager.ReportSessionEnded(_session.Id).ConfigureAwait(false);
                    }
                    catch
                    {
                        // Could throw if the session is already gone
                    }
                }
            }
        }

        async void _device_PlaybackStopped(object sender, PlaybackStoppedEventArgs e)
        {
            try
            {
                await _sessionManager.OnPlaybackStopped(new PlaybackStopInfo
                {
                    ItemId = e.MediaInfo.Id,
                    SessionId = _session.Id,
                    PositionTicks = _device.Position.Ticks

                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error reporting progress", ex);
            }

            await SetNext().ConfigureAwait(false);
        }

        async void _device_PlaybackStart(object sender, PlaybackStartEventArgs e)
        {
            var playlistItem = Playlist.FirstOrDefault(p => p.PlayState == 1);

            if (playlistItem != null)
            {
                var streamInfo = playlistItem.StreamInfo;

                var info = GetProgressInfo(streamInfo, e.MediaInfo);

                try
                {
                    await _sessionManager.OnPlaybackStart(info).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error reporting progress", ex);
                }
            }
        }

        async void _device_PlaybackProgress(object sender, PlaybackProgressEventArgs e)
        {
            var playlistItem = Playlist.FirstOrDefault(p => p.PlayState == 1);

            if (playlistItem != null)
            {
                var streamInfo = playlistItem.StreamInfo;

                var info = GetProgressInfo(streamInfo, e.MediaInfo);

                try
                {
                    await _sessionManager.OnPlaybackProgress(info).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error reporting progress", ex);
                }
            }
        }

        private PlaybackStartInfo GetProgressInfo(StreamInfo streamInfo, uBaseObject mediaInfo)
        {
            var ticks = _device.Position.Ticks;

            if (!streamInfo.IsDirectStream)
            {
                ticks += streamInfo.StartPositionTicks;
            }

            return new PlaybackStartInfo
            {
                ItemId = mediaInfo.Id,
                SessionId = _session.Id,
                PositionTicks = ticks,
                IsMuted = _device.IsMuted,
                IsPaused = _device.IsPaused,
                MediaSourceId = streamInfo.MediaSourceId,
                AudioStreamIndex = streamInfo.AudioStreamIndex,
                SubtitleStreamIndex = streamInfo.SubtitleStreamIndex,
                VolumeLevel = _device.Volume,
                CanSeek = streamInfo.RunTimeTicks.HasValue,
                PlayMethod = streamInfo.IsDirectStream ? PlayMethod.DirectStream : PlayMethod.Transcode,
                QueueableMediaTypes = new List<string> { mediaInfo.MediaType }
            };
        }

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
                Playlist.AddRange(playlist);
            }
            if (command.PlayCommand == PlayCommand.PlayNext)
            {
                Playlist.AddRange(playlist);
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
                    return SetNext();

                case PlaystateCommand.PreviousTrack:
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

        public Task SendPlaybackStartNotification(SessionInfoDto sessionInfo, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task SendPlaybackStoppedNotification(SessionInfoDto sessionInfo, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task SendServerShutdownNotification(CancellationToken cancellationToken)
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

        private readonly List<PlaylistItem> _playlist = new List<PlaylistItem>();
        private List<PlaylistItem> Playlist
        {
            get
            {
                return _playlist;
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

        private PlaylistItem CreatePlaylistItem(BaseItem item, long startPostionTicks, string serverAddress)
        {
            var deviceInfo = _device.Properties;

            var profile = _dlnaManager.GetProfile(deviceInfo.ToDeviceIdentification()) ??
                _dlnaManager.GetDefaultProfile();

            var mediaSources = item is Audio || item is Video
                ? _dtoService.GetMediaSources(item)
                : new List<MediaSourceInfo>();

            var playlistItem = GetPlaylistItem(item, mediaSources, profile, _session.DeviceId);
            playlistItem.StreamInfo.StartPositionTicks = startPostionTicks;

            playlistItem.StreamUrl = playlistItem.StreamInfo.ToUrl(serverAddress);

            var itemXml =
                new DidlBuilder(profile, _imageProcessor, serverAddress, _dtoService).GetItemDidl(item, _session.DeviceId,
                    new Filter());

            playlistItem.Didl = itemXml;

            return playlistItem;
        }

        private string GetDlnaHeaders(PlaylistItem item)
        {
            var profile = item.Profile;
            var streamInfo = item.StreamInfo;

            if (streamInfo.MediaType == DlnaProfileType.Audio)
            {
                return new ContentFeatureBuilder(profile)
                    .BuildAudioHeader(streamInfo.Container,
                    streamInfo.AudioCodec,
                    streamInfo.TargetAudioBitrate,
                    streamInfo.TargetAudioSampleRate,
                    streamInfo.TargetAudioChannels,
                    streamInfo.IsDirectStream,
                    streamInfo.RunTimeTicks,
                    streamInfo.TranscodeSeekInfo);
            }

            if (streamInfo.MediaType == DlnaProfileType.Video)
            {
                return new ContentFeatureBuilder(profile)
                    .BuildVideoHeader(streamInfo.Container,
                    streamInfo.VideoCodec,
                    streamInfo.AudioCodec,
                    streamInfo.TargetWidth,
                    streamInfo.TargetHeight,
                    streamInfo.TargetVideoBitDepth,
                    streamInfo.TargetVideoBitrate,
                    streamInfo.TargetAudioChannels,
                    streamInfo.TargetAudioBitrate,
                    streamInfo.TargetTimestamp,
                    streamInfo.IsDirectStream,
                    streamInfo.RunTimeTicks,
                    streamInfo.TargetVideoProfile,
                    streamInfo.TargetVideoLevel,
                    streamInfo.TargetFramerate,
                    streamInfo.TargetPacketLength,
                    streamInfo.TranscodeSeekInfo);
            }

            return null;
        }

        private PlaylistItem GetPlaylistItem(BaseItem item, List<MediaSourceInfo> mediaSources, DeviceProfile profile, string deviceId)
        {
            var video = item as Video;

            if (video != null)
            {
                return new PlaylistItem
                {
                    StreamInfo = new StreamBuilder().BuildVideoItem(new VideoOptions
                    {
                        ItemId = item.Id.ToString("N"),
                        MediaSources = mediaSources,
                        Profile = profile,
                        DeviceId = deviceId
                    }),

                    Profile = profile
                };
            }

            var audio = item as Audio;

            if (audio != null)
            {
                return new PlaylistItem
                {
                    StreamInfo = new StreamBuilder().BuildAudioItem(new AudioOptions
                    {
                        ItemId = item.Id.ToString("N"),
                        MediaSources = mediaSources,
                        Profile = profile,
                        DeviceId = deviceId
                    }),

                    Profile = profile
                };
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

        private async Task SetNext()
        {
            if (!Playlist.Any() || Playlist.All(i => i.PlayState != 0))
            {
                return;
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
                return;
            }

            nextTrack.PlayState = 1;

            var dlnaheaders = GetDlnaHeaders(nextTrack);

            _logger.Debug("{0} - SetAvTransport Uri: {1} DlnaHeaders: {2}", _device.Properties.Name, nextTrack.StreamUrl, dlnaheaders);

            await _device.SetAvTransport(nextTrack.StreamUrl, dlnaheaders, nextTrack.Didl);

            var streamInfo = nextTrack.StreamInfo;
            if (streamInfo.StartPositionTicks > 0 && streamInfo.IsDirectStream)
                await _device.Seek(TimeSpan.FromTicks(streamInfo.StartPositionTicks));
        }

        public Task SetPrevious()
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
            return _device.SetAvTransport(prevTrack.StreamInfo.ToDlnaUrl(GetServerAddress()), GetDlnaHeaders(prevTrack), prevTrack.Didl);
        }

        #endregion

        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                _device.PlaybackStart -= _device_PlaybackStart;
                _device.PlaybackProgress -= _device_PlaybackProgress;
                _device.PlaybackStopped -= _device_PlaybackStopped;
                _ssdpHandler.MessageReceived -= _SsdpHandler_MessageReceived;

                _device.Dispose();
            }
        }

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public Task SendGeneralCommand(GeneralCommand command, CancellationToken cancellationToken)
        {
            GeneralCommandType commandType;

            if (Enum.TryParse(command.Name, true, out commandType))
            {
                switch (commandType)
                {
                    case GeneralCommandType.VolumeDown:
                        return _device.VolumeDown();
                    case GeneralCommandType.VolumeUp:
                        return _device.VolumeUp();
                    case GeneralCommandType.Mute:
                        return _device.Mute();
                    case GeneralCommandType.Unmute:
                        return _device.Unmute();
                    case GeneralCommandType.ToggleMute:
                        return _device.ToggleMute();
                    case GeneralCommandType.SetVolume:
                        {
                            string volumeArg;

                            if (command.Arguments.TryGetValue("Volume", out volumeArg))
                            {
                                int volume;

                                if (int.TryParse(volumeArg, NumberStyles.Any, _usCulture, out volume))
                                {
                                    return _device.SetVolume(volume);
                                }

                                throw new ArgumentException("Unsupported volume value supplied.");
                            }

                            throw new ArgumentException("Volume argument cannot be null");
                        }
                    default:
                        return Task.FromResult(true);
                }
            }

            return Task.FromResult(true);
        }
    }
}
