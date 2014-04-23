using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.MediaEncoding.Encoder
{
    public class InternalEncodingTaskFactory
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILiveTvManager _liveTvManager;
        private readonly IItemRepository _itemRepo;
        private readonly IServerConfigurationManager _config;

        public InternalEncodingTaskFactory(ILibraryManager libraryManager, ILiveTvManager liveTvManager, IItemRepository itemRepo, IServerConfigurationManager config)
        {
            _libraryManager = libraryManager;
            _liveTvManager = liveTvManager;
            _itemRepo = itemRepo;
            _config = config;
        }

        public async Task<InternalEncodingTask> Create(EncodingOptions request, CancellationToken cancellationToken)
        {
            ValidateInput(request);

            var state = new InternalEncodingTask
            {
                Request = request
            };

            var item = string.IsNullOrEmpty(request.MediaSourceId) ?
                _libraryManager.GetItemById(new Guid(request.ItemId)) :
                _libraryManager.GetItemById(new Guid(request.MediaSourceId));

            if (item is ILiveTvRecording)
            {
                var recording = await _liveTvManager.GetInternalRecording(request.ItemId, cancellationToken).ConfigureAwait(false);

                if (string.Equals(recording.MediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase))
                {
                    state.InputVideoType = VideoType.VideoFile;
                }

                var path = recording.RecordingInfo.Path;
                var mediaUrl = recording.RecordingInfo.Url;

                if (string.IsNullOrWhiteSpace(path) && string.IsNullOrWhiteSpace(mediaUrl))
                {
                    var streamInfo = await _liveTvManager.GetRecordingStream(request.ItemId, cancellationToken).ConfigureAwait(false);

                    state.LiveTvStreamId = streamInfo.Id;

                    path = streamInfo.Path;
                    mediaUrl = streamInfo.Url;
                }

                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    state.MediaPath = path;
                    state.IsInputRemote = false;
                }
                else if (!string.IsNullOrEmpty(mediaUrl))
                {
                    state.MediaPath = mediaUrl;
                    state.IsInputRemote = true;
                }

                state.InputRunTimeTicks = recording.RunTimeTicks;
                if (recording.RecordingInfo.Status == RecordingStatus.InProgress && !state.IsInputRemote)
                {
                    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                }

                state.ReadInputAtNativeFramerate = recording.RecordingInfo.Status == RecordingStatus.InProgress;
                state.AudioSync = "1000";
                state.DeInterlace = true;
                state.InputVideoSync = "-1";
                state.InputAudioSync = "1";
            }
            else if (item is LiveTvChannel)
            {
                var channel = _liveTvManager.GetInternalChannel(request.ItemId);

                if (string.Equals(channel.MediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase))
                {
                    state.InputVideoType = VideoType.VideoFile;
                }

                var streamInfo = await _liveTvManager.GetChannelStream(request.ItemId, cancellationToken).ConfigureAwait(false);

                state.LiveTvStreamId = streamInfo.Id;

                if (!string.IsNullOrEmpty(streamInfo.Path) && File.Exists(streamInfo.Path))
                {
                    state.MediaPath = streamInfo.Path;
                    state.IsInputRemote = false;

                    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                }
                else if (!string.IsNullOrEmpty(streamInfo.Url))
                {
                    state.MediaPath = streamInfo.Url;
                    state.IsInputRemote = true;
                }

                state.ReadInputAtNativeFramerate = true;
                state.AudioSync = "1000";
                state.DeInterlace = true;
                state.InputVideoSync = "-1";
                state.InputAudioSync = "1";
            }
            else
            {
                state.MediaPath = item.Path;
                state.IsInputRemote = item.LocationType == LocationType.Remote;

                var video = item as Video;

                if (video != null)
                {
                    state.InputVideoType = video.VideoType;
                    state.IsoType = video.IsoType;

                    state.StreamFileNames = video.PlayableStreamFileNames.ToList();
                }

                state.InputRunTimeTicks = item.RunTimeTicks;
            }

            var videoRequest = request as VideoEncodingOptions;

            var mediaStreams = _itemRepo.GetMediaStreams(new MediaStreamQuery
            {
                ItemId = item.Id

            }).ToList();

            if (videoRequest != null)
            {
                state.VideoStream = GetMediaStream(mediaStreams, videoRequest.VideoStreamIndex, MediaStreamType.Video);
                state.SubtitleStream = GetMediaStream(mediaStreams, videoRequest.SubtitleStreamIndex, MediaStreamType.Subtitle, false);
                state.AudioStream = GetMediaStream(mediaStreams, videoRequest.AudioStreamIndex, MediaStreamType.Audio);

                if (state.VideoStream != null && state.VideoStream.IsInterlaced)
                {
                    state.DeInterlace = true;
                }
            }
            else
            {
                state.AudioStream = GetMediaStream(mediaStreams, null, MediaStreamType.Audio, true);
            }

            state.HasMediaStreams = mediaStreams.Count > 0;

            state.SegmentLength = state.ReadInputAtNativeFramerate ? 5 : 10;
            state.HlsListSize = state.ReadInputAtNativeFramerate ? 100 : 1440;

            state.QualitySetting = GetQualitySetting();

            ApplyDeviceProfileSettings(state);

            return state;
        }

        private void ValidateInput(EncodingOptions request)
        {
            if (string.IsNullOrWhiteSpace(request.ItemId))
            {
                throw new ArgumentException("ItemId is required.");
            }
            if (string.IsNullOrWhiteSpace(request.OutputPath))
            {
                throw new ArgumentException("OutputPath is required.");
            }
            if (string.IsNullOrWhiteSpace(request.Container))
            {
                throw new ArgumentException("Container is required.");
            }
            if (string.IsNullOrWhiteSpace(request.AudioCodec))
            {
                throw new ArgumentException("AudioCodec is required.");
            }

            var videoRequest = request as VideoEncodingOptions;

            if (videoRequest == null)
            {
                return;
            }
        }

        /// <summary>
        /// Determines which stream will be used for playback
        /// </summary>
        /// <param name="allStream">All stream.</param>
        /// <param name="desiredIndex">Index of the desired.</param>
        /// <param name="type">The type.</param>
        /// <param name="returnFirstIfNoIndex">if set to <c>true</c> [return first if no index].</param>
        /// <returns>MediaStream.</returns>
        private MediaStream GetMediaStream(IEnumerable<MediaStream> allStream, int? desiredIndex, MediaStreamType type, bool returnFirstIfNoIndex = true)
        {
            var streams = allStream.Where(s => s.Type == type).OrderBy(i => i.Index).ToList();

            if (desiredIndex.HasValue)
            {
                var stream = streams.FirstOrDefault(s => s.Index == desiredIndex.Value);

                if (stream != null)
                {
                    return stream;
                }
            }

            if (returnFirstIfNoIndex && type == MediaStreamType.Audio)
            {
                return streams.FirstOrDefault(i => i.Channels.HasValue && i.Channels.Value > 0) ??
                       streams.FirstOrDefault();
            }

            // Just return the first one
            return returnFirstIfNoIndex ? streams.FirstOrDefault() : null;
        }

        private void ApplyDeviceProfileSettings(InternalEncodingTask state)
        {
            var profile = state.Request.DeviceProfile;

            if (profile == null)
            {
                // Don't use settings from the default profile. 
                // Only use a specific profile if it was requested.
                return;
            }

            var container = state.Request.Container;

            var audioCodec = state.Request.AudioCodec;

            if (string.Equals(audioCodec, "copy", StringComparison.OrdinalIgnoreCase) && state.AudioStream != null)
            {
                audioCodec = state.AudioStream.Codec;
            }

            var videoCodec = state.VideoRequest == null ? null : state.VideoRequest.VideoCodec;

            if (string.Equals(videoCodec, "copy", StringComparison.OrdinalIgnoreCase) && state.VideoStream != null)
            {
                videoCodec = state.VideoStream.Codec;
            }

            //var mediaProfile = state.VideoRequest == null ?
            //    profile.GetAudioMediaProfile(container, audioCodec) :
            //    profile.GetVideoMediaProfile(container, audioCodec, videoCodec, state.AudioStream, state.VideoStream);

            //if (mediaProfile != null)
            //{
            //    state.MimeType = mediaProfile.MimeType;
            //    state.OrgPn = mediaProfile.OrgPn;
            //}

            //var transcodingProfile = state.VideoRequest == null ?
            //    profile.GetAudioTranscodingProfile(container, audioCodec) :
            //    profile.GetVideoTranscodingProfile(container, audioCodec, videoCodec);

            //if (transcodingProfile != null)
            //{
            //    //state.EstimateContentLength = transcodingProfile.EstimateContentLength;
            //    state.EnableMpegtsM2TsMode = transcodingProfile.EnableMpegtsM2TsMode;
            //    //state.TranscodeSeekInfo = transcodingProfile.TranscodeSeekInfo;

            //    if (state.VideoRequest != null && string.IsNullOrWhiteSpace(state.VideoRequest.VideoProfile))
            //    {
            //        state.VideoRequest.VideoProfile = transcodingProfile.VideoProfile;
            //    }
            //}
        }

        private EncodingQuality GetQualitySetting()
        {
            var quality = _config.Configuration.MediaEncodingQuality;

            if (quality == EncodingQuality.Auto)
            {
                var cpuCount = Environment.ProcessorCount;

                if (cpuCount >= 4)
                {
                    //return EncodingQuality.HighQuality;
                }

                return EncodingQuality.HighSpeed;
            }

            return quality;
        }
    }
}
