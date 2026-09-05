using System;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Jellyfin.MediaEncoding.Tests;

public class EncodingHelperTests
{
    private static EncodingHelper CreateEncodingHelper()
    {
        return new EncodingHelper(
            Mock.Of<IApplicationPaths>(),
            Mock.Of<IMediaEncoder>(),
            Mock.Of<ISubtitleEncoder>(),
            Mock.Of<IConfiguration>(),
            Mock.Of<MediaBrowser.Common.Configuration.IConfigurationManager>(),
            Mock.Of<IPathManager>());
    }

    private static EncodingJobInfo CreateJobInfo(MediaProtocol protocol, bool requiresLooping)
    {
        return new EncodingJobInfo(TranscodingJobType.Progressive)
        {
            BaseRequest = new BaseEncodingJobOptions(),
            InputProtocol = protocol,
            MediaSource = new MediaSourceInfo
            {
                Protocol = protocol,
                RequiresLooping = requiresLooping
            }
        };
    }

    [Fact]
    public void GetInputModifier_RemoteHttpSource_AddsReconnectWithoutLoopOrEofReconnect()
    {
        var helper = CreateEncodingHelper();
        var state = CreateJobInfo(MediaProtocol.Http, requiresLooping: false);

        var modifier = helper.GetInputModifier(state, new EncodingOptions(), null);

        Assert.Contains("-reconnect 1 -reconnect_delay_max 2", modifier, StringComparison.Ordinal);

        // VOD is finite and seekable: it must not loop, must not reconnect at a real EOF,
        // and must not restart a non-seekable stream from byte zero.
        Assert.DoesNotContain("-stream_loop", modifier, StringComparison.Ordinal);
        Assert.DoesNotContain("-reconnect_at_eof", modifier, StringComparison.Ordinal);
        Assert.DoesNotContain("-reconnect_streamed", modifier, StringComparison.Ordinal);
    }

    [Fact]
    public void GetInputModifier_LocalFileSource_DoesNotAddReconnectFlags()
    {
        var helper = CreateEncodingHelper();
        var state = CreateJobInfo(MediaProtocol.File, requiresLooping: false);

        var modifier = helper.GetInputModifier(state, new EncodingOptions(), null);

        Assert.DoesNotContain("-reconnect", modifier, StringComparison.Ordinal);
    }

    [Fact]
    public void GetInputModifier_RequiresLooping_KeepsLoopingFlagsAndSkipsVodBranch()
    {
        var helper = CreateEncodingHelper();
        var state = CreateJobInfo(MediaProtocol.Http, requiresLooping: true);

        var modifier = helper.GetInputModifier(state, new EncodingOptions(), null);

        Assert.Contains("-stream_loop -1 -reconnect_at_eof 1 -reconnect_streamed 1 -reconnect_delay_max 2", modifier, StringComparison.Ordinal);

        // An HTTP Live TV source must not also pick up the VOD reconnect flags via the else-if.
        Assert.DoesNotContain("-reconnect 1", modifier, StringComparison.Ordinal);
    }
}
