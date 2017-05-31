using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using MediaBrowser.Api.Playback.Hls;
using MediaBrowser.Api.Playback.Progressive;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.System;

namespace MediaBrowser.Api.Playback
{
    public class BaseUniversalRequest
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "MediaSourceId", Description = "The media version id, if playing an alternate version", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string MediaSourceId { get; set; }

        [ApiMember(Name = "DeviceId", Description = "The device id of the client requesting. Used to stop encoding processes when needed.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string DeviceId { get; set; }

        public string UserId { get; set; }
        public string AudioCodec { get; set; }
        public string Container { get; set; }

        public int? MaxAudioChannels { get; set; }
        public int? TranscodingAudioChannels { get; set; }

        public long? MaxStreamingBitrate { get; set; }

        [ApiMember(Name = "StartTimeTicks", Description = "Optional. Specify a starting offset, in ticks. 1 tick = 10000 ms", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public long? StartTimeTicks { get; set; }

        public string TranscodingContainer { get; set; }
        public string TranscodingProtocol { get; set; }
        public int? MaxAudioSampleRate { get; set; }

        public bool EnableRedirection { get; set; }
        public bool EnableRemoteMedia { get; set; }
        public bool BreakOnNonKeyFrames { get; set; }

        public BaseUniversalRequest()
        {
            EnableRedirection = true;
        }
    }

    [Route("/Audio/{Id}/universal.{Container}", "GET", Summary = "Gets an audio stream")]
    [Route("/Audio/{Id}/universal", "GET", Summary = "Gets an audio stream")]
    [Route("/Audio/{Id}/universal.{Container}", "HEAD", Summary = "Gets an audio stream")]
    [Route("/Audio/{Id}/universal", "HEAD", Summary = "Gets an audio stream")]
    public class GetUniversalAudioStream : BaseUniversalRequest
    {
    }

    [Authenticated]
    public class UniversalAudioService : BaseApiService
    {
        public UniversalAudioService(IServerConfigurationManager serverConfigurationManager, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager, IMediaEncoder mediaEncoder, IFileSystem fileSystem, IDlnaManager dlnaManager, IDeviceManager deviceManager, ISubtitleEncoder subtitleEncoder, IMediaSourceManager mediaSourceManager, IZipClient zipClient, IJsonSerializer jsonSerializer, IAuthorizationContext authorizationContext, IImageProcessor imageProcessor, INetworkManager networkManager, IEnvironmentInfo environmentInfo)
        {
            ServerConfigurationManager = serverConfigurationManager;
            UserManager = userManager;
            LibraryManager = libraryManager;
            IsoManager = isoManager;
            MediaEncoder = mediaEncoder;
            FileSystem = fileSystem;
            DlnaManager = dlnaManager;
            DeviceManager = deviceManager;
            SubtitleEncoder = subtitleEncoder;
            MediaSourceManager = mediaSourceManager;
            ZipClient = zipClient;
            JsonSerializer = jsonSerializer;
            AuthorizationContext = authorizationContext;
            ImageProcessor = imageProcessor;
            NetworkManager = networkManager;
            EnvironmentInfo = environmentInfo;
        }

        protected IServerConfigurationManager ServerConfigurationManager { get; private set; }
        protected IUserManager UserManager { get; private set; }
        protected ILibraryManager LibraryManager { get; private set; }
        protected IIsoManager IsoManager { get; private set; }
        protected IMediaEncoder MediaEncoder { get; private set; }
        protected IFileSystem FileSystem { get; private set; }
        protected IDlnaManager DlnaManager { get; private set; }
        protected IDeviceManager DeviceManager { get; private set; }
        protected ISubtitleEncoder SubtitleEncoder { get; private set; }
        protected IMediaSourceManager MediaSourceManager { get; private set; }
        protected IZipClient ZipClient { get; private set; }
        protected IJsonSerializer JsonSerializer { get; private set; }
        protected IAuthorizationContext AuthorizationContext { get; private set; }
        protected IImageProcessor ImageProcessor { get; private set; }
        protected INetworkManager NetworkManager { get; private set; }
        protected IEnvironmentInfo EnvironmentInfo { get; private set; }

        public Task<object> Get(GetUniversalAudioStream request)
        {
            return GetUniversalStream(request, false);
        }

        public Task<object> Head(GetUniversalAudioStream request)
        {
            return GetUniversalStream(request, true);
        }

        private DeviceProfile GetDeviceProfile(GetUniversalAudioStream request)
        {
            var deviceProfile = new DeviceProfile();

            var directPlayProfiles = new List<DirectPlayProfile>();

            directPlayProfiles.Add(new DirectPlayProfile
            {
                Type = DlnaProfileType.Audio,
                Container = request.Container
            });

            deviceProfile.DirectPlayProfiles = directPlayProfiles.ToArray();

            deviceProfile.TranscodingProfiles = new[]
            {
                new TranscodingProfile
                {
                    Type = DlnaProfileType.Audio,
                    Context = EncodingContext.Streaming,
                    Container = request.TranscodingContainer,
                    AudioCodec = request.AudioCodec,
                    Protocol = request.TranscodingProtocol,
                    BreakOnNonKeyFrames = request.BreakOnNonKeyFrames,
                    MaxAudioChannels = request.TranscodingAudioChannels.HasValue ? request.TranscodingAudioChannels.Value.ToString(CultureInfo.InvariantCulture) : null
                }
            };

            var codecProfiles = new List<CodecProfile>();
            var conditions = new List<ProfileCondition>();

            if (request.MaxAudioSampleRate.HasValue)
            {
                // codec profile
                conditions.Add(new ProfileCondition
                {
                    Condition = ProfileConditionType.LessThanEqual,
                    IsRequired = false,
                    Property = ProfileConditionValue.AudioSampleRate,
                    Value = request.MaxAudioSampleRate.Value.ToString(CultureInfo.InvariantCulture)
                });
            }

            if (request.MaxAudioChannels.HasValue)
            {
                // codec profile
                conditions.Add(new ProfileCondition
                {
                    Condition = ProfileConditionType.LessThanEqual,
                    IsRequired = false,
                    Property = ProfileConditionValue.AudioChannels,
                    Value = request.MaxAudioChannels.Value.ToString(CultureInfo.InvariantCulture)
                });
            }

            if (conditions.Count > 0)
            {
                // codec profile
                codecProfiles.Add(new CodecProfile
                {
                    Type = CodecType.Audio,
                    Container = request.Container,
                    Conditions = conditions.ToArray()
                });
            }

            deviceProfile.CodecProfiles = codecProfiles.ToArray();

            return deviceProfile;
        }

        private async Task<object> GetUniversalStream(GetUniversalAudioStream request, bool isHeadRequest)
        {
            var deviceProfile = GetDeviceProfile(request);

            AuthorizationContext.GetAuthorizationInfo(Request).DeviceId = request.DeviceId;

            var mediaInfoService = new MediaInfoService(MediaSourceManager, DeviceManager, LibraryManager, ServerConfigurationManager, NetworkManager, MediaEncoder, UserManager, JsonSerializer, AuthorizationContext)
            {
                Request = Request
            };

            var playbackInfoResult = await mediaInfoService.GetPlaybackInfo(new GetPostedPlaybackInfo
            {
                Id = request.Id,
                MaxAudioChannels = request.MaxAudioChannels,
                MaxStreamingBitrate = request.MaxStreamingBitrate,
                StartTimeTicks = request.StartTimeTicks,
                UserId = request.UserId,
                DeviceProfile = deviceProfile,
                MediaSourceId = request.MediaSourceId

            }).ConfigureAwait(false);

            var mediaSource = playbackInfoResult.MediaSources[0];

            if (mediaSource.SupportsDirectPlay && mediaSource.Protocol == MediaProtocol.Http)
            {
                if (request.EnableRedirection)
                {
                    if (mediaSource.IsRemote && request.EnableRemoteMedia)
                    {
                        return ResultFactory.GetRedirectResult(mediaSource.Path);
                    }
                }
            }

            var isStatic = mediaSource.SupportsDirectStream;

            if (!isStatic && string.Equals(mediaSource.TranscodingSubProtocol, "hls", StringComparison.OrdinalIgnoreCase))
            {
                var service = new DynamicHlsService(ServerConfigurationManager,
                  UserManager,
                  LibraryManager,
                  IsoManager,
                  MediaEncoder,
                  FileSystem,
                  DlnaManager,
                  SubtitleEncoder,
                  DeviceManager,
                  MediaSourceManager,
                  ZipClient,
                  JsonSerializer,
                  AuthorizationContext,
                  NetworkManager)
                {
                    Request = Request
                };

                var transcodingProfile = deviceProfile.TranscodingProfiles[0];

                var newRequest = new GetMasterHlsAudioPlaylist
                {
                    AudioBitRate = isStatic ? (int?)null : Convert.ToInt32(Math.Min(request.MaxStreamingBitrate ?? 192000, int.MaxValue)),
                    AudioCodec = transcodingProfile.AudioCodec,
                    Container = ".m3u8",
                    DeviceId = request.DeviceId,
                    Id = request.Id,
                    MaxAudioChannels = request.MaxAudioChannels,
                    MediaSourceId = mediaSource.Id,
                    PlaySessionId = playbackInfoResult.PlaySessionId,
                    StartTimeTicks = request.StartTimeTicks,
                    Static = isStatic,
                    SegmentContainer = request.TranscodingContainer,
                    AudioSampleRate = request.MaxAudioSampleRate,
                    BreakOnNonKeyFrames = transcodingProfile.BreakOnNonKeyFrames
                };

                if (isHeadRequest)
                {
                    return await service.Head(newRequest).ConfigureAwait(false);
                }
                return await service.Get(newRequest).ConfigureAwait(false);
            }
            else
            {
                var service = new AudioService(ServerConfigurationManager,
                    UserManager,
                    LibraryManager,
                    IsoManager,
                    MediaEncoder,
                    FileSystem,
                    DlnaManager,
                    SubtitleEncoder,
                    DeviceManager,
                    MediaSourceManager,
                    ZipClient,
                    JsonSerializer,
                    AuthorizationContext,
                    ImageProcessor,
                    EnvironmentInfo)
                {
                    Request = Request
                };

                var newRequest = new GetAudioStream
                {
                    AudioBitRate = isStatic ? (int?)null : Convert.ToInt32(Math.Min(request.MaxStreamingBitrate ?? 192000, int.MaxValue)),
                    AudioCodec = request.AudioCodec,
                    Container = isStatic ? null : ("." + mediaSource.TranscodingContainer),
                    DeviceId = request.DeviceId,
                    Id = request.Id,
                    MaxAudioChannels = request.MaxAudioChannels,
                    MediaSourceId = mediaSource.Id,
                    PlaySessionId = playbackInfoResult.PlaySessionId,
                    StartTimeTicks = request.StartTimeTicks,
                    Static = isStatic,
                    AudioSampleRate = request.MaxAudioSampleRate
                };

                if (isHeadRequest)
                {
                    return await service.Head(newRequest).ConfigureAwait(false);
                }
                return await service.Get(newRequest).ConfigureAwait(false);
            }
        }
    }
}
