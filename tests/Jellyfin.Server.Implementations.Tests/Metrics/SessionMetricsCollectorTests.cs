using System;
using Jellyfin.Data.Queries;
using Jellyfin.Server.Implementations.Metrics;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Metrics;

[Collection(MetricsTestCollection.Name)]
public class SessionMetricsCollectorTests
{
    [Fact]
    public void RefreshGauges_TracksSessionsAndTranscodes()
    {
        var deviceType = "device-" + Guid.NewGuid();
        var client = "client-" + Guid.NewGuid();
        var sessionManager = new Mock<ISessionManager>();
        var deviceManager = new Mock<IDeviceManager>();
        deviceManager.Setup(x => x.GetDevices(It.IsAny<DeviceQuery>()))
            .Returns(new QueryResult<Jellyfin.Database.Implementations.Entities.Security.Device>(null, 8, []));
        SessionInfo[] sessions =
        [
            CreateSession(deviceType, client, PlayMethod.DirectPlay, null),
            CreateSession(deviceType, client, PlayMethod.Transcode, new TranscodingInfo
            {
                HardwareAccelerationType = HardwareAccelerationType.nvenc,
                VideoCodec = "h264",
                AudioCodec = "aac"
            }),
            CreateSession("other-device", "other-client", PlayMethod.DirectStream, null),
            CreateIdleSession(deviceType, client)
        ];
        sessionManager.SetupGet(x => x.Sessions).Returns(() => sessions);

        var collector = new SessionMetricsCollector(sessionManager.Object, deviceManager.Object);

        collector.RefreshGauges();

        Assert.Equal(3d, JellyfinMetrics.ConnectedSessions.WithLabels(deviceType, client).Value);
        Assert.Equal(8d, JellyfinMetrics.RegisteredDevices.Value);
        Assert.Equal(1d, JellyfinMetrics.ActiveStreams.WithLabels("direct_play").Value);
        Assert.Equal(1d, JellyfinMetrics.ActiveStreams.WithLabels("transcode").Value);
        Assert.Equal(1d, JellyfinMetrics.ActiveTranscodes.WithLabels("nvenc", "h264", "aac").Value);

        sessions = [];
        collector.RefreshGauges();

        Assert.Equal(0d, JellyfinMetrics.ConnectedSessions.WithLabels(deviceType, client).Value);
        Assert.Equal(0d, JellyfinMetrics.ActiveStreams.WithLabels("direct_play").Value);
        Assert.Equal(0d, JellyfinMetrics.ActiveTranscodes.WithLabels("nvenc", "h264", "aac").Value);
    }

    [Fact]
    public void OnPlaybackStarted_IncrementsCountersOnceForCombinedReasons()
    {
        var reasons = TranscodeReason.AudioCodecNotSupported | TranscodeReason.VideoCodecNotSupported;
        var session = CreateSession("device-" + Guid.NewGuid(), "client-" + Guid.NewGuid(), PlayMethod.Transcode, new TranscodingInfo
        {
            HardwareAccelerationType = HardwareAccelerationType.none,
            VideoCodec = "h264",
            AudioCodec = "aac",
            TranscodeReasons = reasons
        });

        const string playMethodLabel = "transcode";
        const string reasonLabel = "audio_codec_not_supported|video_codec_not_supported";

        var beforePlayMethods = JellyfinMetrics.PlayMethodsTotal.WithLabels(playMethodLabel, reasonLabel).Value;
        var beforeAudioReason = JellyfinMetrics.PlayMethodsTotal
            .WithLabels(playMethodLabel, "audio_codec_not_supported")
            .Value;
        var beforeVideoReason = JellyfinMetrics.PlayMethodsTotal
            .WithLabels(playMethodLabel, "video_codec_not_supported")
            .Value;
        var beforeTranscodes = JellyfinMetrics.TranscodesTotal.WithLabels("software").Value;

        SessionMetricsCollector.OnPlaybackStarted(new PlaybackStartEventArgs { Session = session });

        Assert.Equal(beforePlayMethods + 1, JellyfinMetrics.PlayMethodsTotal.WithLabels(playMethodLabel, reasonLabel).Value);
        Assert.Equal(beforeAudioReason, JellyfinMetrics.PlayMethodsTotal
            .WithLabels(playMethodLabel, "audio_codec_not_supported")
            .Value);
        Assert.Equal(beforeVideoReason, JellyfinMetrics.PlayMethodsTotal
            .WithLabels(playMethodLabel, "video_codec_not_supported")
            .Value);
        Assert.True(JellyfinMetrics.TranscodesTotal.WithLabels("software").Value > beforeTranscodes);
    }

    private static SessionInfo CreateIdleSession(string deviceType, string client)
    {
        return new SessionInfo(new Mock<ISessionManager>().Object, NullLogger.Instance)
        {
            DeviceType = deviceType,
            Client = client
        };
    }

    private static SessionInfo CreateSession(
        string deviceType,
        string client,
        PlayMethod playMethod,
        TranscodingInfo? transcodingInfo)
    {
        var session = new SessionInfo(new Mock<ISessionManager>().Object, NullLogger.Instance)
        {
            DeviceType = deviceType,
            Client = client,
            PlayState = new PlayerStateInfo { PlayMethod = playMethod },
            TranscodingInfo = transcodingInfo
        };

        return session;
    }
}
