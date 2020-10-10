#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Emby.Dlna.Didl;
using Emby.Dlna.Net;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Events;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Session;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using NetworkCollection.Ssdp;
using Photo = MediaBrowser.Controller.Entities.Photo;

namespace Emby.Dlna.PlayTo
{
    public class PlayToController : ISessionController, IDisposable
    {
        private static readonly CultureInfo _usCulture = CultureInfo.ReadOnly(new CultureInfo("en-US"));

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
        private readonly IServerConfigurationManager _config;
        private readonly IMediaEncoder _mediaEncoder;

        private readonly ISsdpPlayToLocator _deviceDiscovery;
        private readonly string _serverAddress;
        private readonly string _accessToken;

        private readonly List<PlaylistItem> _playlist = new List<PlaylistItem>();
        private PlayToDevice _device;
        private int _currentPlaylistIndex = -1;

        private bool _disposed;

        public PlayToController(
            SessionInfo session,
            ISessionManager sessionManager,
            ILibraryManager libraryManager,
            ILogger logger,
            IDlnaManager dlnaManager,
            IUserManager userManager,
            IImageProcessor imageProcessor,
            string serverAddress,
            string accessToken,
            ISsdpPlayToLocator deviceDiscovery,
            IUserDataManager userDataManager,
            ILocalizationManager localization,
            IMediaSourceManager mediaSourceManager,
            IServerConfigurationManager config,
            IMediaEncoder mediaEncoder)
        {
            _session = session;
            _sessionManager = sessionManager;
            _libraryManager = libraryManager;
            _logger = logger;
            _dlnaManager = dlnaManager;
            _userManager = userManager;
            _imageProcessor = imageProcessor;
            _serverAddress = serverAddress;
            _accessToken = accessToken;
            _deviceDiscovery = deviceDiscovery;
            _userDataManager = userDataManager;
            _localization = localization;
            _mediaSourceManager = mediaSourceManager;
            _config = config;
            _mediaEncoder = mediaEncoder;
        }

        public bool IsSessionActive => !_disposed && _device != null;

        public bool SupportsMediaControl => IsSessionActive;

        public void Init(PlayToDevice device)
        {
            _device = device;
            _device.OnDeviceUnavailable = OnDeviceUnavailable;
            _device.PlaybackStart += OnDevicePlaybackStart;
            _device.PlaybackProgress += OnDevicePlaybackProgress;
            _device.PlaybackStopped += OnDevicePlaybackStopped;
            _device.MediaChanged += OnDeviceMediaChanged;

            _deviceDiscovery.DeviceLeft += OnDeviceDiscoveryDeviceLeft;
        }

        private void OnDeviceUnavailable()
        {
            try
            {
                _sessionManager.ReportSessionEnded(_session.Id);
                _ = _device?.DeviceUnavailable();
            }
            catch (Exception ex)
            {
                // Could throw if the session is already gone
                _logger.LogError(ex, "Error reporting the end of session {Id}", _session.Id);
            }
        }

        private void OnDeviceDiscoveryDeviceLeft(object sender, SsdpDeviceInfo e)
        {
            if (!_disposed
                && e.Headers.TryGetValue("USN", out string usn)
                && usn.IndexOf(_device.Properties.UUID, StringComparison.OrdinalIgnoreCase) != -1
                && (usn.IndexOf("MediaRenderer:", StringComparison.OrdinalIgnoreCase) != -1
                    || (e.Headers.TryGetValue("NT", out string nt)
                        && nt.IndexOf("MediaRenderer:", StringComparison.OrdinalIgnoreCase) != -1)))
            {
                OnDeviceUnavailable();
            }
        }

        private async void OnDeviceMediaChanged(object sender, MediaChangedEventArgs e)
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                var streamInfo = StreamParams.ParseFromUrl(e.OldMediaInfo.Url, _libraryManager, _mediaSourceManager);
                if (streamInfo.Item != null)
                {
                    var positionTicks = GetProgressPositionTicks(streamInfo);

                    await ReportPlaybackStopped(streamInfo, positionTicks).ConfigureAwait(false);
                }

                streamInfo = StreamParams.ParseFromUrl(e.NewMediaInfo.Url, _libraryManager, _mediaSourceManager);
                if (streamInfo.Item == null)
                {
                    return;
                }

                var newItemProgress = GetProgressInfo(streamInfo);

                await _sessionManager.OnPlaybackStart(newItemProgress).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting progress");
            }
        }

        private async void OnDevicePlaybackStopped(object sender, PlaybackEventArgs e)
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                var streamInfo = StreamParams.ParseFromUrl(e.MediaInfo.Url, _libraryManager, _mediaSourceManager);

                if (streamInfo.Item == null)
                {
                    return;
                }

                var positionTicks = GetProgressPositionTicks(streamInfo);

                await ReportPlaybackStopped(streamInfo, positionTicks).ConfigureAwait(false);

                var mediaSource = await streamInfo.GetMediaSource(CancellationToken.None).ConfigureAwait(false);

                var duration = mediaSource == null ?
                    (_device.Duration == null ? (long?)null : _device.Duration.Value.Ticks) :
                    mediaSource.RunTimeTicks;

                var playedToCompletion = positionTicks.HasValue && positionTicks.Value == 0;

                if (!playedToCompletion && duration.HasValue && positionTicks.HasValue)
                {
                    double percent = positionTicks.Value;
                    percent /= duration.Value;

                    playedToCompletion = Math.Abs(1 - percent) * 100 <= _config.Configuration.MaxResumePct;
                }

                if (playedToCompletion)
                {
                    await SetPlaylistIndex(_currentPlaylistIndex + 1).ConfigureAwait(false);
                }
                else
                {
                    _playlist.Clear();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting playback stopped");
            }
        }

        private async Task ReportPlaybackStopped(StreamParams streamInfo, long? positionTicks)
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
                _logger.LogError(ex, "Error reporting progress");
            }
        }

        private async void OnDevicePlaybackStart(object sender, PlaybackEventArgs e)
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                var info = StreamParams.ParseFromUrl(e.MediaInfo.Url, _libraryManager, _mediaSourceManager);

                if (info.Item != null)
                {
                    var progress = GetProgressInfo(info);

                    await _sessionManager.OnPlaybackStart(progress).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting progress");
            }
        }

        private async void OnDevicePlaybackProgress(object sender, PlaybackEventArgs e)
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                var mediaUrl = e.MediaInfo.Url;

                if (string.IsNullOrWhiteSpace(mediaUrl))
                {
                    return;
                }

                var info = StreamParams.ParseFromUrl(mediaUrl, _libraryManager, _mediaSourceManager);

                if (info.Item != null)
                {
                    var progress = GetProgressInfo(info);

                    await _sessionManager.OnPlaybackProgress(progress).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting progress");
            }
        }

        private long? GetProgressPositionTicks(StreamParams info)
        {
            var ticks = _device.Position.Ticks;

            if (!info.IsDirectStream)
            {
                ticks += info.StartPositionTicks;
            }

            return ticks;
        }

        private PlaybackStartInfo GetProgressInfo(StreamParams info)
        {
            return new PlaybackStartInfo
            {
                ItemId = info.ItemId,
                SessionId = _session.Id,
                PositionTicks = GetProgressPositionTicks(info),
                IsMuted = _device.IsMuted,
                IsPaused = _device.IsPaused,
                MediaSourceId = info.MediaSourceId,
                AudioStreamIndex = info.AudioStreamIndex,
                SubtitleStreamIndex = info.SubtitleStreamIndex,
                VolumeLevel = _device.Volume,

                // TODO
                CanSeek = true,

                PlayMethod = info.IsDirectStream ? PlayMethod.DirectStream : PlayMethod.Transcode
            };
        }

        /// <summary>
        /// Shuffles a list.
        /// https://stackoverflow.com/questions/273313/randomize-a-listt.
        /// </summary>
        private void ShufflePlaylist()
        {
            using RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
            int n = _playlist.Count;
            while (n > 1)
            {
                byte[] box = new byte[1];
                do
                {
                    provider.GetBytes(box);
                }
                while (!(box[0] < n * (byte.MaxValue / n)));

                int k = box[0] % n;
                n--;
                PlaylistItem value = _playlist[k];
                _playlist[k] = _playlist[n];
                _playlist[n] = value;
            }
        }

        public Task SendPlayCommand(PlayRequest command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            _logger.LogDebug("{0} - Received PlayRequest: {1}", this._session.DeviceName, command.PlayCommand);

            var user = command.ControllingUserId.Equals(Guid.Empty) ? null : _userManager.GetUserById(command.ControllingUserId);

            var items = new List<BaseItem>();
            foreach (var id in command.ItemIds)
            {
                AddItemFromId(id, items);
            }

            var startIndex = command.StartIndex ?? 0;
            if (startIndex > 0)
            {
                items = items.Skip(startIndex).ToList();
            }

            var playlist = new List<PlaylistItem>();
            var isFirst = true;

            var deviceInfo = _device.Properties;

            // Checking the profile once instead of on each iteration.
            var profile = _dlnaManager.GetProfile(deviceInfo) ??
                _dlnaManager.GetDefaultProfile(deviceInfo);

            foreach (var item in items)
            {
                if (isFirst && command.StartPositionTicks.HasValue)
                {
                    playlist.Add(CreatePlaylistItem(
                        item,
                        user,
                        command.StartPositionTicks.Value,
                        command.MediaSourceId,
                        command.AudioStreamIndex,
                        command.SubtitleStreamIndex,
                        profile));
                    isFirst = false;
                }
                else
                {
                    playlist.Add(CreatePlaylistItem(item, user, 0, null, null, null, profile));
                }
            }

            _logger.LogDebug("{0} - Playlist created", _session.DeviceName);

            // track offset./
            if (command.PlayCommand == PlayCommand.PlayNow)
            {
                // Reset playlist and re-add tracks.
                _playlist.Clear();
                _playlist.AddRange(playlist);
            }
            else if (command.PlayCommand == PlayCommand.PlayShuffle)
            {
                _logger.LogDebug("{0} - Shuffling playlist.", _session.DeviceName);
                // Will restart playback on a random item.
                ShufflePlaylist();
            }
            else if (command.PlayCommand == PlayCommand.PlayLast)
            {
                // Add to the end of the list.
                _playlist.AddRange(playlist);

                _logger.LogDebug("{0} - Adding {1} items to the end of the playlist.", _session.DeviceName, _playlist.Count);
                if (_device.IsPlaying)
                {
                    return Task.CompletedTask;
                }
            }
            else if (command.PlayCommand == PlayCommand.PlayNext)
            {
                // Insert into the next up.

                _logger.LogDebug("{0} - Inserting {1} items next in the playlist.", _session.DeviceName, _playlist.Count);
                if (_currentPlaylistIndex >= 0)
                {
                    _playlist.InsertRange(_currentPlaylistIndex, playlist);
                }
                else
                {
                    _playlist.AddRange(playlist);
                }

                if (_device.IsPlaying)
                {
                    return Task.CompletedTask;
                }
            }

            if (!command.ControllingUserId.Equals(Guid.Empty))
            {
                _sessionManager.LogSessionActivity(
                    _session.Client,
                    _session.ApplicationVersion,
                    _session.DeviceId,
                    _session.DeviceName,
                    _session.RemoteEndPoint,
                    user);
            }

            return PlayItems();
        }

        private Task SendPlaystateCommand(PlaystateRequest command)
        {
            switch (command.Command)
            {
                case PlaystateCommand.Stop:
                    _playlist.Clear();
                    return _device.Stop();

                case PlaystateCommand.Pause:
                    return _device.Pause();

                case PlaystateCommand.Unpause:
                    return _device.Play();

                case PlaystateCommand.PlayPause:
                    return _device.IsPaused ? _device.Play() : _device.Pause();

                case PlaystateCommand.Seek:
                    return Seek(command.SeekPositionTicks ?? 0);

                case PlaystateCommand.NextTrack:
                    return SetPlaylistIndex(_currentPlaylistIndex + 1);

                case PlaystateCommand.PreviousTrack:
                    return SetPlaylistIndex(_currentPlaylistIndex - 1);
            }

            return Task.CompletedTask;
        }

        private async Task Seek(long newPosition)
        {
            var media = _device.CurrentMediaInfo;

            if (media != null)
            {
                var info = StreamParams.ParseFromUrl(media.Url, _libraryManager, _mediaSourceManager);

                if (info.Item != null && !info.IsDirectStream)
                {
                    var user = !_session.UserId.Equals(Guid.Empty) ? _userManager.GetUserById(_session.UserId) : null;
                    var newItem = CreatePlaylistItem(info.Item, user, newPosition, info.MediaSourceId, info.AudioStreamIndex, info.SubtitleStreamIndex);

                    await _device.SetAvTransport(newItem.StreamInfo.MediaType, false, newItem.StreamUrl, GetDlnaHeaders(newItem), newItem.Didl).ConfigureAwait(false);
                    return;
                }

                await SeekAfterTransportChange(newPosition, CancellationToken.None).ConfigureAwait(false);
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

        private PlaylistItem CreatePlaylistItem(
            BaseItem item,
            User user,
            long startPostionTicks,
            string mediaSourceId,
            int? audioStreamIndex,
            int? subtitleStreamIndex,
            DeviceProfile profile = null)
        {
            var mediaSources = item is IHasMediaSources
                ? _mediaSourceManager.GetStaticMediaSources(item, true, user)
                : new List<MediaSourceInfo>();

            if (profile == null)
            {
                var deviceInfo = _device.Properties;
                profile = _dlnaManager.GetProfile(deviceInfo) ??
                        _dlnaManager.GetDefaultProfile(deviceInfo);
            }

            var playlistItem = GetPlaylistItem(item, mediaSources, profile, _session.DeviceId, mediaSourceId, audioStreamIndex, subtitleStreamIndex);
            playlistItem.StreamInfo.StartPositionTicks = startPostionTicks;

            playlistItem.StreamUrl = DidlBuilder.NormalizeDlnaMediaUrl(playlistItem.StreamInfo.ToUrl(_serverAddress, _accessToken));

            var itemXml = new DidlBuilder(
                profile,
                user,
                _imageProcessor,
                _serverAddress,
                _accessToken,
                _userDataManager,
                _localization,
                _mediaSourceManager,
                _logger,
                _mediaEncoder,
                _libraryManager)
                .GetItemDidl(item, user, null, _session.DeviceId, new Filter(), playlistItem.StreamInfo);

            playlistItem.Didl = itemXml;

            return playlistItem;
        }

        private static string GetDlnaHeaders(PlaylistItem item)
        {
            var profile = item.Profile;
            var streamInfo = item.StreamInfo;

            if (streamInfo.MediaType == DlnaProfileType.Audio)
            {
                return new ContentFeatureBuilder(profile)
                    .BuildAudioHeader(
                        streamInfo.Container,
                        streamInfo.TargetAudioCodec.FirstOrDefault(),
                        streamInfo.TargetAudioBitrate,
                        streamInfo.TargetAudioSampleRate,
                        streamInfo.TargetAudioChannels,
                        streamInfo.TargetAudioBitDepth,
                        streamInfo.IsDirectStream,
                        streamInfo.RunTimeTicks ?? 0,
                        streamInfo.TranscodeSeekInfo);
            }

            if (streamInfo.MediaType == DlnaProfileType.Video)
            {
                var list = new ContentFeatureBuilder(profile)
                    .BuildVideoHeader(
                        streamInfo.Container,
                        streamInfo.TargetVideoCodec.FirstOrDefault(),
                        streamInfo.TargetAudioCodec.FirstOrDefault(),
                        streamInfo.TargetWidth,
                        streamInfo.TargetHeight,
                        streamInfo.TargetVideoBitDepth,
                        streamInfo.TargetVideoBitrate,
                        streamInfo.TargetTimestamp,
                        streamInfo.IsDirectStream,
                        streamInfo.RunTimeTicks ?? 0,
                        streamInfo.TargetVideoProfile,
                        streamInfo.TargetVideoLevel,
                        streamInfo.TargetFramerate ?? 0,
                        streamInfo.TargetPacketLength,
                        streamInfo.TranscodeSeekInfo,
                        streamInfo.IsTargetAnamorphic,
                        streamInfo.IsTargetInterlaced,
                        streamInfo.TargetRefFrames,
                        streamInfo.TargetVideoStreamCount,
                        streamInfo.TargetAudioStreamCount,
                        streamInfo.TargetVideoCodecTag,
                        streamInfo.IsTargetAVC);

                return list.Count == 0 ? null : list[0];
            }

            return null;
        }

        private PlaylistItem GetPlaylistItem(BaseItem item, List<MediaSourceInfo> mediaSources, DeviceProfile profile, string deviceId, string mediaSourceId, int? audioStreamIndex, int? subtitleStreamIndex)
        {
            if (string.Equals(item.MediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase))
            {
                return new PlaylistItem
                {
                    StreamInfo = new StreamBuilder(_mediaEncoder, _logger).BuildVideoItem(new VideoOptions
                    {
                        ItemId = item.Id,
                        MediaSources = mediaSources.ToArray(),
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
                    StreamInfo = new StreamBuilder(_mediaEncoder, _logger).BuildAudioItem(new AudioOptions
                    {
                        ItemId = item.Id,
                        MediaSources = mediaSources.ToArray(),
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
        /// <returns><c>true</c> on success.</returns>
        private async Task<bool> PlayItems()
        {
            _logger.LogDebug("{0} - Playing {1} items", _session.DeviceName, _playlist.Count);

            await SetPlaylistIndex(0).ConfigureAwait(false);
            return true;
        }

        private async Task SetPlaylistIndex(int index)
        {
            if (index < 0 || index >= _playlist.Count)
            {
                _playlist.Clear();
                await _device.Stop().ConfigureAwait(false);
                return;
            }

            _currentPlaylistIndex = index;
            var currentitem = _playlist[index];

            await _device.SetAvTransport(currentitem.StreamInfo.MediaType, true, currentitem.StreamUrl, GetDlnaHeaders(currentitem), currentitem.Didl).ConfigureAwait(false);

            var streamInfo = currentitem.StreamInfo;
            if (streamInfo.StartPositionTicks > 0 && streamInfo.IsDirectStream)
            {
                await SeekAfterTransportChange(streamInfo.StartPositionTicks, CancellationToken.None).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Override this method and dispose any objects you own the lifetime of if disposing is true.
        /// </summary>
        /// <param name="disposing">True if managed objects should be disposed, if false, only unmanaged resources should be released.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _deviceDiscovery.DeviceLeft -= OnDeviceDiscoveryDeviceLeft;
                _device.PlaybackStart -= OnDevicePlaybackStart;
                _device.PlaybackProgress -= OnDevicePlaybackProgress;
                _device.PlaybackStopped -= OnDevicePlaybackStopped;
                _device.MediaChanged -= OnDeviceMediaChanged;
                _device.OnDeviceUnavailable = null;
                _device.Dispose();

                _disposed = true;
            }
        }

        private Task SendGeneralCommand(GeneralCommand command)
        {
            switch (command.Name)
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
                    if (command.Arguments.TryGetValue("Index", out string index))
                    {
                        if (int.TryParse(index, NumberStyles.Integer, _usCulture, out var val))
                        {
                            return SetAudioStreamIndex(val);
                        }

                        throw new ArgumentException("Unsupported SetAudioStreamIndex value supplied.");
                    }

                    throw new ArgumentException("SetAudioStreamIndex argument cannot be null");
                case GeneralCommandType.SetSubtitleStreamIndex:
                    if (command.Arguments.TryGetValue("Index", out index))
                    {
                        if (int.TryParse(index, NumberStyles.Integer, _usCulture, out var val))
                        {
                            return SetSubtitleStreamIndex(val);
                        }

                        throw new ArgumentException("Unsupported SetSubtitleStreamIndex value supplied.");
                    }

                    throw new ArgumentException("SetSubtitleStreamIndex argument cannot be null");
                case GeneralCommandType.SetVolume:
                    if (command.Arguments.TryGetValue("Volume", out string vol))
                    {
                        if (int.TryParse(vol, NumberStyles.Integer, _usCulture, out var volume))
                        {
                            _device.Volume = volume;
                            return Task.CompletedTask;
                        }

                        throw new ArgumentException("Unsupported volume value supplied.");
                    }

                    throw new ArgumentException("Volume argument cannot be null");
                default:
                    return Task.CompletedTask;
            }
        }

        private async Task SetAudioStreamIndex(int? newIndex)
        {
            var media = _device.CurrentMediaInfo;

            if (media != null)
            {
                var info = StreamParams.ParseFromUrl(media.Url, _libraryManager, _mediaSourceManager);

                if (info.Item != null)
                {
                    var newPosition = GetProgressPositionTicks(info) ?? 0;

                    var user = !_session.UserId.Equals(Guid.Empty) ? _userManager.GetUserById(_session.UserId) : null;
                    var newItem = CreatePlaylistItem(info.Item, user, newPosition, info.MediaSourceId, newIndex, info.SubtitleStreamIndex);

                    bool seekAfter = newItem.StreamInfo.IsDirectStream;

                    // Pass our intentions to the device, so that it doesn't restart at the beginning, only to then seek.
                    await _device.SetAvTransport(newItem.StreamInfo.MediaType, !seekAfter, newItem.StreamUrl, GetDlnaHeaders(newItem), newItem.Didl).ConfigureAwait(false);

                    if (seekAfter)
                    {
                        await SeekAfterTransportChange(newPosition, CancellationToken.None).ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task SetSubtitleStreamIndex(int? newIndex)
        {
            var media = _device.CurrentMediaInfo;

            if (media != null)
            {
                var info = StreamParams.ParseFromUrl(media.Url, _libraryManager, _mediaSourceManager);

                if (info.Item != null)
                {
                    var newPosition = GetProgressPositionTicks(info) ?? 0;

                    var user = !_session.UserId.Equals(Guid.Empty) ? _userManager.GetUserById(_session.UserId) : null;
                    var newItem = CreatePlaylistItem(info.Item, user, newPosition, info.MediaSourceId, info.AudioStreamIndex, newIndex);

                    bool seekAfter = newItem.StreamInfo.IsDirectStream && newPosition > 0;

                    // Pass our intentions to the device, so that it doesn't restart at the beginning, only to then seek.
                    await _device.SetAvTransport(newItem.StreamInfo.MediaType, !seekAfter, newItem.StreamUrl, GetDlnaHeaders(newItem), newItem.Didl).ConfigureAwait(false);

                    if (seekAfter)
                    {
                        await SeekAfterTransportChange(newPosition, CancellationToken.None).ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task SeekAfterTransportChange(long positionTicks, CancellationToken cancellationToken)
        {
            const int MaxWait = 15000000;
            const int Interval = 500;
            var currentWait = 0;

            while (!_device.IsPlaying && currentWait < MaxWait)
            {
                await Task.Delay(Interval, cancellationToken).ConfigureAwait(false);
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                currentWait += Interval;
            }

            await _device.Seek(TimeSpan.FromTicks(positionTicks)).ConfigureAwait(false);
        }

        private static int? GetIntValue(IReadOnlyDictionary<string, string> values, string name)
        {
            var value = values.GetValueOrDefault(name);

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }

            return null;
        }

        private static long GetLongValue(IReadOnlyDictionary<string, string> values, string name)
        {
            var value = values.GetValueOrDefault(name);

            if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }

            return 0;
        }

        /// <inheritdoc />
        public Task SendMessage<T>(SessionMessageType name, Guid messageId, T data, CancellationToken cancellationToken)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            if (_device == null)
            {
                return Task.CompletedTask;
            }

            // Ensure the device is initialised.
            _device.DeviceInitialise().ConfigureAwait(false);
            if (name == SessionMessageType.Play)
            {
                return SendPlayCommand(data as PlayRequest);
            }

            if (name == SessionMessageType.PlayState)
            {
                return SendPlaystateCommand(data as PlaystateRequest);
            }

            if (name == SessionMessageType.GeneralCommand)
            {
                return SendGeneralCommand(data as GeneralCommand);
            }

            // Not supported or needed right now
            return Task.CompletedTask;
        }

        private class StreamParams
        {
            private MediaSourceInfo _mediaSource;
            private IMediaSourceManager _mediaSourceManager;

            public Guid ItemId { get; set; }

            public bool IsDirectStream { get; set; }

            public long StartPositionTicks { get; set; }

            public int? AudioStreamIndex { get; set; }

            public int? SubtitleStreamIndex { get; set; }

            public string DeviceProfileId { get; set; }

            public string DeviceId { get; set; }

            public string MediaSourceId { get; set; }

            public string LiveStreamId { get; set; }

            public BaseItem Item { get; set; }

            public async Task<MediaSourceInfo> GetMediaSource(CancellationToken cancellationToken)
            {
                if (_mediaSource != null)
                {
                    return _mediaSource;
                }

                if (!(Item is IHasMediaSources))
                {
                    return null;
                }

                if (_mediaSourceManager != null)
                {
                    _mediaSource = await _mediaSourceManager.GetMediaSource(Item, MediaSourceId, LiveStreamId, false, cancellationToken).ConfigureAwait(false);
                }

                return _mediaSource;
            }

            private static Guid GetItemId(string url)
            {
                if (string.IsNullOrEmpty(url))
                {
                    throw new ArgumentNullException(nameof(url));
                }

                var parts = url.Split('/');

                for (var i = 0; i < parts.Length; i++)
                {
                    var part = parts[i];

                    if (string.Equals(part, "audio", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(part, "videos", StringComparison.OrdinalIgnoreCase))
                    {
                        if (parts.Length > i + 1)
                        {
                            return Guid.Parse(parts[i + 1]);
                        }
                    }
                }

                return Guid.Empty;
            }

            public static StreamParams ParseFromUrl(string url, ILibraryManager libraryManager, IMediaSourceManager mediaSourceManager)
            {
                if (string.IsNullOrEmpty(url))
                {
                    // This condition is met on the initial loading of media, when the last media url is null.
                    return new StreamParams
                    {
                        ItemId = Guid.Empty
                    };
                }

                var request = new StreamParams
                {
                    ItemId = GetItemId(url)
                };

                if (request.ItemId.Equals(Guid.Empty))
                {
                    return request;
                }

                var index = url.IndexOf('?', StringComparison.Ordinal);
                if (index == -1)
                {
                    return request;
                }

                var query = url.Substring(index + 1);
                Dictionary<string, string> values = QueryHelpers.ParseQuery(query).ToDictionary(kv => kv.Key, kv => kv.Value.ToString());

                request.DeviceProfileId = values.GetValueOrDefault("DeviceProfileId");
                request.DeviceId = values.GetValueOrDefault("DeviceId");
                request.MediaSourceId = values.GetValueOrDefault("MediaSourceId");
                request.LiveStreamId = values.GetValueOrDefault("LiveStreamId");
                request.IsDirectStream = string.Equals("true", values.GetValueOrDefault("Static"), StringComparison.OrdinalIgnoreCase);

                request.AudioStreamIndex = GetIntValue(values, "AudioStreamIndex");
                request.SubtitleStreamIndex = GetIntValue(values, "SubtitleStreamIndex");
                request.StartPositionTicks = GetLongValue(values, "StartPositionTicks");

                request.Item = libraryManager.GetItemById(request.ItemId);

                request._mediaSourceManager = mediaSourceManager;

                return request;
            }
        }
    }
}
