using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.LiveTv.TunerHosts;
using Jellyfin.LiveTv.TunerHosts.HdHomerun;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.LiveTv.Tests
{
    public class TunerHostStreamOrderTests
    {
        [Fact]
        public async Task GetChannelStream_OpensFirstMatch_WithoutContactingLaterHosts()
        {
            var host = CreateHost();

            var stream = await host.GetChannelStream("hdhr_5026", "native", new List<ILiveStream>(), CancellationToken.None);

            Assert.Equal("flex", stream.TunerHostId);
            Assert.Equal(new[] { "flex" }, host.ChannelLookups);
            Assert.Equal(new[] { "flex" }, host.Opened);
        }

        [Fact]
        public async Task GetChannelStream_FailedOpen_TriesNextHostThatHasTheChannel()
        {
            var host = CreateHost();
            host.FailOpen.Add("flex");

            var stream = await host.GetChannelStream("hdhr_5026", "native", new List<ILiveStream>(), CancellationToken.None);

            Assert.Equal("other", stream.TunerHostId);
            Assert.Equal(new[] { "flex", "other" }, host.Opened);
            Assert.DoesNotContain("dead", host.ChannelLookups);
        }

        [Fact]
        public async Task GetChannelStream_MediaSourceTarget_IsOpenedBeforeEarlierHosts()
        {
            var host = CreateHost();
            host.PreferredHostId = "other";

            var stream = await host.GetChannelStream("hdhr_5026", "preferred", new List<ILiveStream>(), CancellationToken.None);

            Assert.Equal("other", stream.TunerHostId);
            Assert.Equal(new[] { "other" }, host.ChannelLookups);
            Assert.Equal(new[] { "other" }, host.Opened);
        }

        [Fact]
        public async Task GetChannelStream_TargetLineupFails_FallsThroughToAnotherHostWithTheChannel()
        {
            var host = CreateHost();
            host.PreferredHostId = "dead";
            host.Channels.Remove("dead");

            var stream = await host.GetChannelStream("hdhr_1", "preferred", new List<ILiveStream>(), CancellationToken.None);

            Assert.Equal("flex", stream.TunerHostId);
            Assert.Equal(new[] { "dead", "flex" }, host.ChannelLookups);
        }

        [Fact]
        public void HdHomerun_StreamIdUrlHash_MatchesOnlyThatTuner()
        {
            var probe = new ProbeHdHomerunHost(
                Mock.Of<IServerConfigurationManager>(),
                Mock.Of<ILogger<HdHomerunHost>>(),
                Mock.Of<IFileSystem>(),
                Mock.Of<IHttpClientFactory>(),
                Mock.Of<IServerApplicationHost>(),
                Mock.Of<ISocketFactory>(),
                Mock.Of<IStreamHelper>());

            var flexUrl = new Uri("http://192.168.50.45").AbsoluteUri.TrimEnd('/');
            var streamId = "native_" + "hdhr_5026".GetMD5().ToString("N") + "_" + flexUrl.GetMD5().ToString("N");

            Assert.True(probe.Matches(new TunerHostInfo { Url = "http://192.168.50.45" }, streamId));
            Assert.False(probe.Matches(new TunerHostInfo { Url = "http://192.168.50.129:6081" }, streamId));
        }

        private static TestTunerHost CreateHost()
        {
            var host = new TestTunerHost();
            host.Hosts.Add(new TunerHostInfo { Id = "flex", Url = "http://flex" });
            host.Hosts.Add(new TunerHostInfo { Id = "other", Url = "http://other" });
            host.Hosts.Add(new TunerHostInfo { Id = "dead", Url = "http://dead" });
            host.Channels["flex"] = new List<ChannelInfo>
            {
                new ChannelInfo { Id = "hdhr_5026", TunerHostId = "flex" },
                new ChannelInfo { Id = "hdhr_1", TunerHostId = "flex" }
            };
            host.Channels["other"] = new List<ChannelInfo>
            {
                new ChannelInfo { Id = "hdhr_5026", TunerHostId = "other" }
            };
            host.Channels["dead"] = new List<ChannelInfo>
            {
                new ChannelInfo { Id = "hdhr_1", TunerHostId = "dead" }
            };
            return host;
        }

        private sealed class TestTunerHost : BaseTunerHost
        {
            public TestTunerHost()
                : base(Mock.Of<IServerConfigurationManager>(), Mock.Of<ILogger<BaseTunerHost>>(), Mock.Of<IFileSystem>())
            {
            }

            public List<TunerHostInfo> Hosts { get; } = new List<TunerHostInfo>();

            public Dictionary<string, List<ChannelInfo>> Channels { get; } = new Dictionary<string, List<ChannelInfo>>();

            public HashSet<string> FailOpen { get; } = new HashSet<string>();

            public List<string> ChannelLookups { get; } = new List<string>();

            public List<string> Opened { get; } = new List<string>();

            public string PreferredHostId { get; set; } = string.Empty;

            public override string Type => "hdhomerun";

            protected override string ChannelIdPrefix => "hdhr_";

            protected override IList<TunerHostInfo> GetTunerHosts() => Hosts;

            protected override bool IsStreamTarget(TunerHostInfo host, string streamId)
            {
                return string.Equals(streamId, "preferred", StringComparison.Ordinal)
                    && string.Equals(host.Id, PreferredHostId, StringComparison.Ordinal);
            }

            protected override Task<List<ChannelInfo>> GetChannelsInternal(TunerHostInfo tuner, CancellationToken cancellationToken)
            {
                ChannelLookups.Add(tuner.Id);
                if (!Channels.TryGetValue(tuner.Id, out var channels))
                {
                    throw new InvalidOperationException("tuner down");
                }

                return Task.FromResult(channels);
            }

            protected override Task<List<MediaSourceInfo>> GetChannelStreamMediaSources(TunerHostInfo tuner, ChannelInfo channel, CancellationToken cancellationToken)
            {
                return Task.FromResult(new List<MediaSourceInfo>());
            }

            protected override Task<ILiveStream> GetChannelStream(TunerHostInfo tunerHost, ChannelInfo channel, string streamId, IList<ILiveStream> currentLiveStreams, CancellationToken cancellationToken)
            {
                Opened.Add(tunerHost.Id);
                ILiveStream liveStream = new FakeLiveStream(tunerHost.Id, FailOpen.Contains(tunerHost.Id));
                return Task.FromResult(liveStream);
            }
        }

        private sealed class FakeLiveStream : ILiveStream
        {
            private readonly bool _failOpen;

            public FakeLiveStream(string tunerHostId, bool failOpen)
            {
                TunerHostId = tunerHostId;
                _failOpen = failOpen;
            }

            public int ConsumerCount { get; set; }

            public string OriginalStreamId { get; set; } = string.Empty;

            public string TunerHostId { get; }

            public bool EnableStreamSharing => false;

            public MediaSourceInfo MediaSource { get; set; } = new MediaSourceInfo();

            public string UniqueId { get; } = Guid.NewGuid().ToString("N");

            public Task Open(CancellationToken openCancellationToken)
            {
                if (_failOpen)
                {
                    throw new InvalidOperationException("open failed");
                }

                return Task.CompletedTask;
            }

            public Task Close() => Task.CompletedTask;

            public Stream GetStream() => Stream.Null;

            public void Dispose()
            {
            }
        }

        private sealed class ProbeHdHomerunHost : HdHomerunHost
        {
            public ProbeHdHomerunHost(
                IServerConfigurationManager config,
                ILogger<HdHomerunHost> logger,
                IFileSystem fileSystem,
                IHttpClientFactory httpClientFactory,
                IServerApplicationHost appHost,
                ISocketFactory socketFactory,
                IStreamHelper streamHelper)
                : base(config, logger, fileSystem, httpClientFactory, appHost, socketFactory, streamHelper)
            {
            }

            public bool Matches(TunerHostInfo host, string streamId) => IsStreamTarget(host, streamId);
        }
    }
}
