using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Session;
using MediaBrowser.Dlna.Didl;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.System;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.MediaEncoding;

namespace MediaBrowser.Dlna.PlayTo
{
    public class PlayToController : ISessionController, IDisposable
    {
        private Device _device;
        private readonly SessionInfo _session;
        private readonly ISessionManager _sessionManager;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly IDlnaManager _dlnaManager;
        private readonly IUserManager _userManager;
        private readonly IImageProcessor _imageProcessor;
        private readonly IUserDataManager _userDataManager;
        private readonly ILocalizationManager _localization;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly IConfigurationManager _config;
        private readonly IMediaEncoder _mediaEncoder;

        private readonly IDeviceDiscovery _deviceDiscovery;
        private readonly string _serverAddress;
        private readonly string _accessToken;
        private readonly DateTime _creationTime;

        public bool IsSessionActive
        {
            get
            {
                var lastDateKnownActivity = new[] { _creationTime, _device.DateLastActivity }.Max();

                if (DateTime.UtcNow >= lastDateKnownActivity.AddSeconds(120))
                {
                    try
                    {
                        // Session is inactive, mark it for Disposal and don't start the elapsed timer.
                        _sessionManager.ReportSessionEnded(_session.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error in ReportSessionEnded", ex);
                    }
                    return false;
                }

                return _device != null;
            }
        }

        public void OnActivity()
        {
        }

        public bool SupportsMediaControl
        {
            get { return IsSessionActive; }
        }

        public PlayToController(SessionInfo session, ISessionManager sessionManager, ILibraryManager libraryManager, ILogger logger, IDlnaManager dlnaManager, IUserManager userManager, IImageProcessor imageProcessor, string serverAddress, string accessToken, IDeviceDiscovery deviceDiscovery, IUserDataManager userDataManager, ILocalizationManager localization, IMediaSourceManager mediaSourceManager, IConfigurationManager config, IMediaEncoder mediaEncoder)
        {
            _session = session;
            _sessionManager = sessionManager;
            _libraryManager = libraryManager;
            _dlnaManager = dlnaManager;
            _userManager = userManager;
            _imageProcessor = imageProcessor;
            _serverAddress = serverAddress;
            _deviceDiscovery = deviceDiscovery;
            _userDataManager = userDataManager;
            _localization = localization;
            _mediaSourceManager = mediaSourceManager;
            _config = config;
            _mediaEncoder = mediaEncoder;
            _accessToken = accessToken;
            _logger = logger;
            _creationTime = DateTime.UtcNow;
        }

        public void Init(Device device)
        {
            _device = device;
            _device.OnDeviceUnavailable = OnDeviceUnavailable;
            _device.PlaybackStart += _device_PlaybackStart;
            _device.PlaybackProgress += _device_PlaybackProgress;
            _device.PlaybackStopped += _device_PlaybackStopped;
            _device.MediaChanged += _device_MediaChanged;

            _device.Start();

            _deviceDiscovery.DeviceLeft += _deviceDiscovery_DeviceLeft;
        }

        private void OnDeviceUnavailable()
        {
            try
            {
                _sessionManager.ReportSessionEnded(_session.Id);
            }
            catch
            {
                // Could throw if the session is already gone
            }
        }

        void _deviceDiscovery_DeviceLeft(object sender, SsdpMessageEventArgs e)
        {
            string nts;
            e.Headers.TryGetValue("NTS", out nts);

            string usn;
            if (!e.Headers.TryGetValue("USN", out usn)) usn = String.Empty;

            string nt;
            if (!e.Headers.TryGetValue("NT", out nt)) nt = String.Empty;

            if (usn.IndexOf(_device.Properties.UUID, StringComparison.OrdinalIgnoreCase) != -1 &&
                !_disposed)
            {
                if (usn.IndexOf("MediaRenderer:", StringComparison.OrdinalIgnoreCase) != -1 ||
                    nt.IndexOf("MediaRenderer:", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    OnDeviceUnavailable();
                }
            }
        }

        async void _device_MediaChanged(object sender, MediaChangedEventArgs e)
        {
            try
            {
                var streamInfo = await StreamParams.ParseFromUrl(e.OldMediaInfo.Url, _libraryManager, _mediaSourceManager).ConfigureAwait(false);
                if (streamInfo.Item != null)
                {
                    var progress = GetProgressInfo(e.OldMediaInfo, streamInfo);

                    var positionTicks = progress.PositionTicks;

                    ReportPlaybackStopped(e.OldMediaInfo, streamInfo, positionTicks);
                }

                streamInfo = await StreamParams.ParseFromUrl(e.NewMediaInfo.Url, _libraryManager, _mediaSourceManager).ConfigureAwait(false);
                if (streamInfo.Item == null) return;

                var newItemProgress = GetProgressInfo(e.NewMediaInfo, streamInfo);

                await _sessionManager.OnPlaybackStart(newItemProgress).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error reporting progress", ex);
            }
        }

        async void _device_PlaybackStopped(object sender, PlaybackStoppedEventArgs e)
        {
            try
            {
                var streamInfo = await StreamParams.ParseFromUrl(e.MediaInfo.Url, _libraryManager, _mediaSourceManager)
                            .ConfigureAwait(false);

                if (streamInfo.Item == null) return;

                var progress = GetProgressInfo(e.MediaInfo, streamInfo);

                var positionTicks = progress.PositionTicks;

                ReportPlaybackStopped(e.MediaInfo, streamInfo, positionTicks);

                var duration = streamInfo.MediaSource == null ?
                    (_device.Duration == null ? (long?)null : _device.Duration.Value.Ticks) :
                    streamInfo.MediaSource.RunTimeTicks;

                var playedToCompletion = (positionTicks.HasValue && positionTicks.Value == 0);

                if (!playedToCompletion && duration.HasValue && positionTicks.HasValue)
                {
                    double percent = positionTicks.Value;
                    percent /= duration.Value;

                    playedToCompletion = Math.Abs(1 - percent) <= .1;
                }

                if (playedToCompletion)
                {
                    await SetPlaylistIndex(_currentPlaylistIndex + 1).ConfigureAwait(false);
                }
                else
                {
                    Playlist.Clear();
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error reporting playback stopped", ex);
            }
        }

        private async void ReportPlaybackStopped(uBaseObject mediaInfo, StreamParams streamInfo, long? positionTicks)
        {
            try
            {
                await _sessionManager.OnPlaybackStopped(new PlaybackStopInfo
                {
                    ItemId = streamInfo.ItemId,
                    SessionId = _session.Id,
                    PositionTicks = positionTicks,
                    MediaSourceId = streamInfo.MediaSourceId

                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error reporting progress", ex);
            }
        }

        async void _device_PlaybackStart(object sender, PlaybackStartEventArgs e)
        {
            try
            {
                var info = await StreamParams.ParseFromUrl(e.MediaInfo.Url, _libraryManager, _mediaSourceManager).ConfigureAwait(false);

                if (info.Item != null)
                {
                    var progress = GetProgressInfo(e.MediaInfo, info);

                    await _sessionManager.OnPlaybackStart(progress).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error reporting progress", ex);
            }
        }

        async void _device_PlaybackProgress(object sender, PlaybackProgressEventArgs e)
        {
            try
            {
                var info = await StreamParams.ParseFromUrl(e.MediaInfo.Url, _libraryManager, _mediaSourceManager).ConfigureAwait(false);

                if (info.Item != null)
                {
                    var progress = GetProgressInfo(e.MediaInfo, info);

                    await _sessionManager.OnPlaybackProgress(progress).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error reporting progress", ex);
            }
        }

        private PlaybackStartInfo GetProgressInfo(uBaseObject mediaInfo, StreamParams info)
        {
            var ticks = _device.Position.Ticks;

            if (!EnableClientSideSeek(info))
            {
                ticks += info.StartPositionTicks;
            }

            return new PlaybackStartInfo
            {
                ItemId = info.ItemId,
                SessionId = _session.Id,
                PositionTicks = ticks,
                IsMuted = _device.IsMuted,
                IsPaused = _device.IsPaused,
                MediaSourceId = info.MediaSourceId,
                AudioStreamIndex = info.AudioStreamIndex,
                SubtitleStreamIndex = info.SubtitleStreamIndex,
                VolumeLevel = _device.Volume,

                CanSeek = info.MediaSource == null ? _device.Duration.HasValue : info.MediaSource.RunTimeTicks.HasValue,

                PlayMethod = info.IsDirectStream ? PlayMethod.DirectStream : PlayMethod.Transcode,
                QueueableMediaTypes = new List<string> { mediaInfo.MediaType }
            };
        }

        #region SendCommands

        public async Task SendPlayCommand(PlayRequest command, CancellationToken cancellationToken)
        {
            _logger.Debug("{0} - Received PlayRequest: {1}", this._session.DeviceName, command.PlayCommand);

            var user = String.IsNullOrEmpty(command.ControllingUserId) ? null : _userManager.GetUserById(command.ControllingUserId);

            var items = new List<BaseItem>();
            foreach (string id in command.ItemIds)
            {
                AddItemFromId(Guid.Parse(id), items);
            }

            var playlist = new List<PlaylistItem>();
            var isFirst = true;

            foreach (var item in items)
            {
                if (isFirst && command.StartPositionTicks.HasValue)
                {
                    playlist.Add(CreatePlaylistItem(item, user, command.StartPositionTicks.Value, null, null, null));
                    isFirst = false;
                }
                else
                {
                    playlist.Add(CreatePlaylistItem(item, user, 0, null, null, null));
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

            if (!String.IsNullOrWhiteSpace(command.ControllingUserId))
            {
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
                    {
                        return Seek(command.SeekPositionTicks ?? 0);
                    }

                case PlaystateCommand.NextTrack:
                    return SetPlaylistIndex(_currentPlaylistIndex + 1);

                case PlaystateCommand.PreviousTrack:
                    return SetPlaylistIndex(_currentPlaylistIndex - 1);
            }

            return Task.FromResult(true);
        }

        private async Task Seek(long newPosition)
        {
            var media = _device.CurrentMediaInfo;

            if (media != null)
            {
                var info = await StreamParams.ParseFromUrl(media.Url, _libraryManager, _mediaSourceManager).ConfigureAwait(false);

                if (info.Item != null && !EnableClientSideSeek(info))
                {
                    var user = _session.UserId.HasValue ? _userManager.GetUserById(_session.UserId.Value) : null;
                    var newItem = CreatePlaylistItem(info.Item, user, newPosition, info.MediaSourceId, info.AudioStreamIndex, info.SubtitleStreamIndex);

                    await _device.SetAvTransport(newItem.StreamUrl, GetDlnaHeaders(newItem), newItem.Didl).ConfigureAwait(false);
                    return;
                }
                await SeekAfterTransportChange(newPosition).ConfigureAwait(false);
            }
        }

        private bool EnableClientSideSeek(StreamParams info)
        {
            return info.IsDirectStream;
        }

        private bool EnableClientSideSeek(StreamInfo info)
        {
            return info.IsDirectStream;
        }

        public Task SendUserDataChangeInfo(UserDataChangeInfo info, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task SendRestartRequiredNotification(SystemInfo info, CancellationToken cancellationToken)
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

        #endregion

        #region Playlist

        private int _currentPlaylistIndex;
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
            if (item.MediaType == MediaType.Audio || item.MediaType == MediaType.Video)
            {
                list.Add(item);
            }
        }

        private PlaylistItem CreatePlaylistItem(BaseItem item, User user, long startPostionTicks, string mediaSourceId, int? audioStreamIndex, int? subtitleStreamIndex)
        {
            var deviceInfo = _device.Properties;

            var profile = _dlnaManager.GetProfile(deviceInfo.ToDeviceIdentification()) ??
                _dlnaManager.GetDefaultProfile();

            var hasMediaSources = item as IHasMediaSources;
            var mediaSources = hasMediaSources != null
                ? (_mediaSourceManager.GetStaticMediaSources(hasMediaSources, true, user)).ToList()
                : new List<MediaSourceInfo>();

            var playlistItem = GetPlaylistItem(item, mediaSources, profile, _session.DeviceId, mediaSourceId, audioStreamIndex, subtitleStreamIndex);
            playlistItem.StreamInfo.StartPositionTicks = startPostionTicks;

            playlistItem.StreamUrl = playlistItem.StreamInfo.ToDlnaUrl(_serverAddress, _accessToken);

            var itemXml = new DidlBuilder(profile, user, _imageProcessor, _serverAddress, _accessToken, _userDataManager, _localization, _mediaSourceManager, _logger, _libraryManager, _mediaEncoder)
                .GetItemDidl(_config.GetDlnaConfiguration(), item, null, _session.DeviceId, new Filter(), playlistItem.StreamInfo);

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
                    streamInfo.TargetAudioCodec,
                    streamInfo.TargetAudioBitrate,
                    streamInfo.TargetAudioSampleRate,
                    streamInfo.TargetAudioChannels,
                    streamInfo.IsDirectStream,
                    streamInfo.RunTimeTicks,
                    streamInfo.TranscodeSeekInfo);
            }

            if (streamInfo.MediaType == DlnaProfileType.Video)
            {
                var list = new ContentFeatureBuilder(profile)
                    .BuildVideoHeader(streamInfo.Container,
                    streamInfo.VideoCodec,
                    streamInfo.TargetAudioCodec,
                    streamInfo.TargetWidth,
                    streamInfo.TargetHeight,
                    streamInfo.TargetVideoBitDepth,
                    streamInfo.TargetVideoBitrate,
                    streamInfo.TargetTimestamp,
                    streamInfo.IsDirectStream,
                    streamInfo.RunTimeTicks,
                    streamInfo.TargetVideoProfile,
                    streamInfo.TargetVideoLevel,
                    streamInfo.TargetFramerate,
                    streamInfo.TargetPacketLength,
                    streamInfo.TranscodeSeekInfo,
                    streamInfo.IsTargetAnamorphic,
                    streamInfo.TargetRefFrames,
                    streamInfo.TargetVideoStreamCount,
                    streamInfo.TargetAudioStreamCount,
                    streamInfo.TargetVideoCodecTag);

                return list.FirstOrDefault();
            }

            return null;
        }

        private ILogger GetStreamBuilderLogger()
        {
            if (_config.GetDlnaConfiguration().EnableDebugLog)
            {
                return _logger;
            }

            return new NullLogger();
        }

        private PlaylistItem GetPlaylistItem(BaseItem item, List<MediaSourceInfo> mediaSources, DeviceProfile profile, string deviceId, string mediaSourceId, int? audioStreamIndex, int? subtitleStreamIndex)
        {
            if (string.Equals(item.MediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase))
            {
                return new PlaylistItem
                {
                    StreamInfo = new StreamBuilder(_mediaEncoder, GetStreamBuilderLogger()).BuildVideoItem(new VideoOptions
                    {
                        ItemId = item.Id.ToString("N"),
                        MediaSources = mediaSources,
                        Profile = profile,
                        DeviceId = deviceId,
                        MaxBitrate = profile.MaxStreamingBitrate,
                        MediaSourceId = mediaSourceId,
                        AudioStreamIndex = audioStreamIndex,
                        SubtitleStreamIndex = subtitleStreamIndex
                    }),

                    Profile = profile
                };
            }

            if (string.Equals(item.MediaType, MediaType.Audio, StringComparison.OrdinalIgnoreCase))
            {
                return new PlaylistItem
                {
                    StreamInfo = new StreamBuilder(_mediaEncoder, GetStreamBuilderLogger()).BuildAudioItem(new AudioOptions
                    {
                        ItemId = item.Id.ToString("N"),
                        MediaSources = mediaSources,
                        Profile = profile,
                        DeviceId = deviceId,
                        MaxBitrate = profile.MaxStreamingBitrate,
                        MediaSourceId = mediaSourceId
                    }),

                    Profile = profile
                };
            }

            if (string.Equals(item.MediaType, MediaType.Photo, StringComparison.OrdinalIgnoreCase))
            {
                return new PlaylistItemFactory().Create((Photo)item, profile);
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
            _logger.Debug("{0} - Playing {1} items", _session.DeviceName, Playlist.Count);

            await SetPlaylistIndex(0).ConfigureAwait(false);
            return true;
        }

        private async Task SetPlaylistIndex(int index)
        {
            if (index < 0 || index >= Playlist.Count)
            {
                Playlist.Clear();
                await _device.SetStop();
                return;
            }

            _currentPlaylistIndex = index;
            var currentitem = Playlist[index];

            await _device.SetAvTransport(currentitem.StreamUrl, GetDlnaHeaders(currentitem), currentitem.Didl);

            var streamInfo = currentitem.StreamInfo;
            if (streamInfo.StartPositionTicks > 0 && EnableClientSideSeek(streamInfo))
            {
                await SeekAfterTransportChange(streamInfo.StartPositionTicks).ConfigureAwait(false);
            }
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
                _device.MediaChanged -= _device_MediaChanged;
                _deviceDiscovery.DeviceLeft -= _deviceDiscovery_DeviceLeft;
                _device.OnDeviceUnavailable = null;

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
                    case GeneralCommandType.SetAudioStreamIndex:
                        {
                            string arg;

                            if (command.Arguments.TryGetValue("Index", out arg))
                            {
                                int val;

                                if (Int32.TryParse(arg, NumberStyles.Any, _usCulture, out val))
                                {
                                    return SetAudioStreamIndex(val);
                                }

                                throw new ArgumentException("Unsupported SetAudioStreamIndex value supplied.");
                            }

                            throw new ArgumentException("SetAudioStreamIndex argument cannot be null");
                        }
                    case GeneralCommandType.SetSubtitleStreamIndex:
                        {
                            string arg;

                            if (command.Arguments.TryGetValue("Index", out arg))
                            {
                                int val;

                                if (Int32.TryParse(arg, NumberStyles.Any, _usCulture, out val))
                                {
                                    return SetSubtitleStreamIndex(val);
                                }

                                throw new ArgumentException("Unsupported SetSubtitleStreamIndex value supplied.");
                            }

                            throw new ArgumentException("SetSubtitleStreamIndex argument cannot be null");
                        }
                    case GeneralCommandType.SetVolume:
                        {
                            string arg;

                            if (command.Arguments.TryGetValue("Volume", out arg))
                            {
                                int volume;

                                if (Int32.TryParse(arg, NumberStyles.Any, _usCulture, out volume))
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

        private async Task SetAudioStreamIndex(int? newIndex)
        {
            var media = _device.CurrentMediaInfo;

            if (media != null)
            {
                var info = await StreamParams.ParseFromUrl(media.Url, _libraryManager, _mediaSourceManager).ConfigureAwait(false);

                if (info.Item != null)
                {
                    var progress = GetProgressInfo(media, info);
                    var newPosition = progress.PositionTicks ?? 0;

                    var user = _session.UserId.HasValue ? _userManager.GetUserById(_session.UserId.Value) : null;
                    var newItem = CreatePlaylistItem(info.Item, user, newPosition, info.MediaSourceId, newIndex, info.SubtitleStreamIndex);

                    await _device.SetAvTransport(newItem.StreamUrl, GetDlnaHeaders(newItem), newItem.Didl).ConfigureAwait(false);

                    if (EnableClientSideSeek(newItem.StreamInfo))
                    {
                        await SeekAfterTransportChange(newPosition).ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task SetSubtitleStreamIndex(int? newIndex)
        {
            var media = _device.CurrentMediaInfo;

            if (media != null)
            {
                var info = await StreamParams.ParseFromUrl(media.Url, _libraryManager, _mediaSourceManager).ConfigureAwait(false);

                if (info.Item != null)
                {
                    var progress = GetProgressInfo(media, info);
                    var newPosition = progress.PositionTicks ?? 0;

                    var user = _session.UserId.HasValue ? _userManager.GetUserById(_session.UserId.Value) : null;
                    var newItem = CreatePlaylistItem(info.Item, user, newPosition, info.MediaSourceId, info.AudioStreamIndex, newIndex);

                    await _device.SetAvTransport(newItem.StreamUrl, GetDlnaHeaders(newItem), newItem.Didl).ConfigureAwait(false);

                    if (EnableClientSideSeek(newItem.StreamInfo) && newPosition > 0)
                    {
                        await SeekAfterTransportChange(newPosition).ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task SeekAfterTransportChange(long positionTicks)
        {
            const int maxWait = 15000000;
            const int interval = 500;
            var currentWait = 0;
            while (_device.TransportState != TRANSPORTSTATE.PLAYING && currentWait < maxWait)
            {
                await Task.Delay(interval).ConfigureAwait(false);
                currentWait += interval;
            }

            await _device.Seek(TimeSpan.FromTicks(positionTicks)).ConfigureAwait(false);
        }

        private class StreamParams
        {
            public string ItemId { get; set; }

            public bool IsDirectStream { get; set; }

            public long StartPositionTicks { get; set; }

            public int? AudioStreamIndex { get; set; }

            public int? SubtitleStreamIndex { get; set; }

            public string DeviceProfileId { get; set; }
            public string DeviceId { get; set; }

            public string MediaSourceId { get; set; }

            public BaseItem Item { get; set; }
            public MediaSourceInfo MediaSource { get; set; }

            private static string GetItemId(string url)
            {
                var parts = url.Split('/');

                for (var i = 0; i < parts.Length; i++)
                {
                    var part = parts[i];

                    if (string.Equals(part, "audio", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(part, "videos", StringComparison.OrdinalIgnoreCase))
                    {
                        if (parts.Length > i + 1)
                        {
                            return parts[i + 1];
                        }
                    }
                }

                return null;
            }

            public static async Task<StreamParams> ParseFromUrl(string url, ILibraryManager libraryManager, IMediaSourceManager mediaSourceManager)
            {
                var request = new StreamParams
                {
                    ItemId = GetItemId(url)
                };

                Guid parsedId;

                if (string.IsNullOrWhiteSpace(request.ItemId) || !Guid.TryParse(request.ItemId, out parsedId))
                {
                    return request;
                }

                const string srch = "params=";
                var index = url.IndexOf(srch, StringComparison.OrdinalIgnoreCase);

                if (index == -1) return request;

                var vals = url.Substring(index + srch.Length).Split(';');

                for (var i = 0; i < vals.Length; i++)
                {
                    var val = vals[i];

                    if (string.IsNullOrWhiteSpace(val))
                    {
                        continue;
                    }

                    if (i == 0)
                    {
                        request.DeviceProfileId = val;
                    }
                    else if (i == 1)
                    {
                        request.DeviceId = val;
                    }
                    else if (i == 2)
                    {
                        request.MediaSourceId = val;
                    }
                    else if (i == 3)
                    {
                        request.IsDirectStream = string.Equals("true", val, StringComparison.OrdinalIgnoreCase);
                    }
                    else if (i == 6)
                    {
                        request.AudioStreamIndex = int.Parse(val, CultureInfo.InvariantCulture);
                    }
                    else if (i == 7)
                    {
                        request.SubtitleStreamIndex = int.Parse(val, CultureInfo.InvariantCulture);
                    }
                    else if (i == 14)
                    {
                        request.StartPositionTicks = long.Parse(val, CultureInfo.InvariantCulture);
                    }
                }

                request.Item = string.IsNullOrWhiteSpace(request.ItemId)
                    ? null
                    : libraryManager.GetItemById(parsedId);

                var hasMediaSources = request.Item as IHasMediaSources;

                request.MediaSource = hasMediaSources == null
                    ? null
                    : (await mediaSourceManager.GetMediaSource(hasMediaSources, request.MediaSourceId, false).ConfigureAwait(false));

                return request;
            }
        }

        public Task SendMessage<T>(string name, T data, CancellationToken cancellationToken)
        {
            // Not supported or needed right now
            return Task.FromResult(true);
        }
    }
}
