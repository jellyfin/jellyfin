using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoMoq;
using Castle.Components.DictionaryAdapter;
using Emby.Server.Implementations.IO;
using Emby.Server.Implementations.Library;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.MediaInfo;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library
{
    public class MediaSourceManagerTests
    {
        private readonly MediaSourceManager _mediaSourceManager;
        private readonly Mock<IUserDataManager> _mockUserDataManager;
        private readonly Mock<ILocalizationManager> _mockLocalizationManager;
        private Video _item;
        private User _user;

        public MediaSourceManagerTests()
        {
            IFixture fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
            fixture.Inject<IFileSystem>(fixture.Create<ManagedFileSystem>());

            _mockUserDataManager = fixture.Freeze<Mock<IUserDataManager>>();
            _mockUserDataManager.Setup(m => m.GetUserData(It.IsAny<User>(), It.IsAny<BaseItem>())).Returns(new UserItemData() { Key = "key" });

            _mockLocalizationManager = fixture.Create<Mock<ILocalizationManager>>();
            _mockLocalizationManager.Setup(m => m.FindLanguageInfo(It.IsAny<string>())).Returns((string s) => string.IsNullOrEmpty(s) ? null : new CultureDto(s, s, s, new EditableList<string> { s }));
            fixture.Inject(_mockLocalizationManager.Object);

            _mediaSourceManager = fixture.Create<MediaSourceManager>();

            _item = new Video { Id = Guid.NewGuid(), OwnerId = Guid.Empty, ParentId = Guid.Empty };

            _user = fixture.Create<User>();
        }

        [Theory]
        [InlineData(@"C:\mydir\myfile.ext", MediaProtocol.File)]
        [InlineData("/mydir/myfile.ext", MediaProtocol.File)]
        [InlineData("file:///mydir/myfile.ext", MediaProtocol.File)]
        [InlineData("http://example.com/stream.m3u8", MediaProtocol.Http)]
        [InlineData("https://example.com/stream.m3u8", MediaProtocol.Http)]
        [InlineData("rtsp://media.example.com:554/twister/audiotrack", MediaProtocol.Rtsp)]
        public void GetPathProtocol_ValidArg_Correct(string path, MediaProtocol expected)
            => Assert.Equal(expected, _mediaSourceManager.GetPathProtocol(path));

        [Theory]
        [InlineData(5, "eng", "eng", false, true)]
        [InlineData(5, "eng", "eng", true, true)]
        [InlineData(2, "ger", "eng", false, true)]
        [InlineData(2, "ger", "eng", true, true)]
        [InlineData(1, "fre", "eng", false, true)]
        [InlineData(2, "fre", "eng", true, true)]
        [InlineData(5, "OriginalLanguage", "eng", false, false)]
        [InlineData(4, "OriginalLanguage", "eng", false, true)]
        [InlineData(5, "OriginalLanguage", "eng", true, false)]
        [InlineData(5, "OriginalLanguage", "eng", true, true)]
        [InlineData(2, "OriginalLanguage", "jpn", true, true)]
        [InlineData(2, "OriginalLanguage", "jpn", false, true)]
        [InlineData(2, "OriginalLanguage", "jpn,eng", false, true)]
        [InlineData(4, "OriginalLanguage", null, false, true)]
        [InlineData(2, "OriginalLanguage", null, true, true)]
        [InlineData(4, "OriginalLanguage", "", false, true)]
        [InlineData(2, "OriginalLanguage", "", false, false)]
        [InlineData(2, "OriginalLanguage", "ger", false, true)]
        [InlineData(2, "OriginalLanguage", "ger", false, false)]
        [InlineData(1, "OriginalLanguage", "fre", false, false)]
        [InlineData(2, "OriginalLanguage", "fre", true, true)]
        [InlineData(2, "OriginalLanguage", "fre", true, false)]
        public void SetDefaultAudioStreamIndex_Index_Correct(
            int expectedIndex,
            string prefferedLanguage,
            string? originalLanguage,
            bool playDefault,
            bool originalExist)
        {
            var streams = new MediaStream[]
            {
                new()
                {
                    Index = 0,
                    Type = MediaStreamType.Video,
                    IsDefault = true
                },
                new()
                {
                    Index = 1,
                    Type = MediaStreamType.Audio,
                    Language = "fre",
                    IsDefault = false,
                    IsOriginal = false
                },
                new()
                {
                    Index = 2,
                    Type = MediaStreamType.Audio,
                    Language = "jpn",
                    IsDefault = true,
                    IsOriginal = false
                },
                new()
                {
                    Index = 3,
                    Type = MediaStreamType.Audio,
                    Language = "eng",
                    IsDefault = false,
                    IsOriginal = false
                },
                new()
                {
                    Index = 4,
                    Type = MediaStreamType.Audio,
                    Language = "eng",
                    IsDefault = false,
                    IsOriginal = originalExist,
                },
                new()
                {
                    Index = 5,
                    Type = MediaStreamType.Audio,
                    Language = "eng",
                    IsDefault = true,
                    IsOriginal = false,
                }
            };
            var mediaInfo = new MediaSourceInfo
            {
                MediaStreams = streams
            };
            _user.AudioLanguagePreference = prefferedLanguage;
            _user.PlayDefaultAudioTrack = playDefault;
            _item.OriginalLanguage = originalLanguage;

            _mediaSourceManager.SetDefaultAudioAndSubtitleStreamIndices(_item, mediaInfo, _user);
            Assert.Equal(expectedIndex, mediaInfo.DefaultAudioStreamIndex);
        }

        [Fact]
        public void SetDefaultAudioAndSubtitleStreamIndices_LiveTvChannel_RestoresRememberedSubtitleIndex()
        {
            var (manager, channel, user) = CreateLiveTvSubtitleTestContext(
                savedSubtitleStreamIndex: 3,
                configureUser: u =>
                {
                    u.RememberSubtitleSelections = true;
                    u.SubtitleMode = SubtitlePlaybackMode.Default;
                });

            Assert.True(channel.EnableRememberingTrackSelections);

            var mediaSource = CreateLiveTvMediaSourceWithSubtitles(
                new MediaStream { Index = 3, Type = MediaStreamType.Subtitle, Codec = "DVBSUB", Language = "rum" });

            manager.SetDefaultAudioAndSubtitleStreamIndices(channel, mediaSource, user);

            Assert.Equal(3, mediaSource.DefaultSubtitleStreamIndex);
        }

        [Fact]
        public void SetDefaultAudioAndSubtitleStreamIndices_LiveTvChannel_UsesGlobalLanguageWhenNoSavedSelection()
        {
            var (manager, channel, user) = CreateLiveTvSubtitleTestContext(
                savedSubtitleStreamIndex: null,
                configureUser: u =>
                {
                    u.RememberSubtitleSelections = true;
                    u.SubtitleLanguagePreference = "rum";
                    u.SubtitleMode = SubtitlePlaybackMode.Default;
                });

            var mediaSource = CreateLiveTvMediaSourceWithSubtitles(
                new MediaStream { Index = 3, Type = MediaStreamType.Subtitle, Codec = "DVBSUB", Language = "dut" },
                new MediaStream { Index = 4, Type = MediaStreamType.Subtitle, Codec = "DVBSUB", Language = "rum" });

            manager.SetDefaultAudioAndSubtitleStreamIndices(channel, mediaSource, user);

            Assert.Equal(4, mediaSource.DefaultSubtitleStreamIndex);
        }

        [Fact]
        public void SetDefaultAudioAndSubtitleStreamIndices_VodDefaultMode_DoesNotApplyPreferredLanguage()
        {
            var (manager, channel, user) = CreateLiveTvSubtitleTestContext(
                savedSubtitleStreamIndex: null,
                configureUser: u =>
                {
                    u.SubtitleLanguagePreference = "rum";
                    u.SubtitleMode = SubtitlePlaybackMode.Default;
                });

            var mediaSource = CreateLiveTvMediaSourceWithSubtitles(
                new MediaStream { Index = 3, Type = MediaStreamType.Subtitle, Codec = "DVBSUB", Language = "rum" });
            mediaSource.IsInfiniteStream = false;

            manager.SetDefaultAudioAndSubtitleStreamIndices(channel, mediaSource, user);

            Assert.Null(mediaSource.DefaultSubtitleStreamIndex);
        }

        [Fact]
        public void SetDefaultAudioAndSubtitleStreamIndices_LiveTvChannel_FallsBackWhenSavedIndexInvalid()
        {
            var (manager, channel, user) = CreateLiveTvSubtitleTestContext(
                savedSubtitleStreamIndex: 99,
                configureUser: u =>
                {
                    u.RememberSubtitleSelections = true;
                    u.SubtitleLanguagePreference = "rum";
                    u.SubtitleMode = SubtitlePlaybackMode.Always;
                });

            var mediaSource = CreateLiveTvMediaSourceWithSubtitles(
                new MediaStream { Index = 3, Type = MediaStreamType.Subtitle, Codec = "DVBSUB", Language = "dut" },
                new MediaStream { Index = 4, Type = MediaStreamType.Subtitle, Codec = "DVBSUB", Language = "rum" });

            manager.SetDefaultAudioAndSubtitleStreamIndices(channel, mediaSource, user);

            Assert.Equal(4, mediaSource.DefaultSubtitleStreamIndex);
        }

        [Fact]
        public void SetDefaultAudioAndSubtitleStreamIndices_LiveTvChannel_RememberedOff_DoesNotOverrideAlwaysWithLanguagePreference()
        {
            var (manager, channel, user) = CreateLiveTvSubtitleTestContext(
                savedSubtitleStreamIndex: -1,
                configureUser: u =>
                {
                    u.RememberSubtitleSelections = true;
                    u.SubtitleLanguagePreference = "rum";
                    u.SubtitleMode = SubtitlePlaybackMode.Always;
                });

            var mediaSource = CreateLiveTvMediaSourceWithSubtitles(
                new MediaStream { Index = 4, Type = MediaStreamType.Subtitle, Codec = "DVBSUB", Language = "rum" });

            manager.SetDefaultAudioAndSubtitleStreamIndices(channel, mediaSource, user);

            Assert.Equal(4, mediaSource.DefaultSubtitleStreamIndex);
        }

        [Fact]
        public void SetDefaultAudioAndSubtitleStreamIndices_LiveTvChannel_RememberedOff_StillHonoredWithoutLanguagePreference()
        {
            var (manager, channel, user) = CreateLiveTvSubtitleTestContext(
                savedSubtitleStreamIndex: -1,
                configureUser: u =>
                {
                    u.RememberSubtitleSelections = true;
                    u.SubtitleLanguagePreference = null;
                    u.SubtitleMode = SubtitlePlaybackMode.Always;
                });

            var mediaSource = CreateLiveTvMediaSourceWithSubtitles(
                new MediaStream { Index = 4, Type = MediaStreamType.Subtitle, Codec = "DVBSUB", Language = "rum" });

            manager.SetDefaultAudioAndSubtitleStreamIndices(channel, mediaSource, user);

            Assert.Equal(-1, mediaSource.DefaultSubtitleStreamIndex);
        }

        private static (MediaSourceManager Manager, LiveTvChannel Channel, User User) CreateLiveTvSubtitleTestContext(
            int? savedSubtitleStreamIndex,
            Action<User>? configureUser = null)
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
            fixture.Inject<IFileSystem>(fixture.Create<ManagedFileSystem>());

            var channel = new LiveTvChannel
            {
                Id = Guid.NewGuid(),
                Name = "TVR 1 HD",
                ChannelType = ChannelType.TV,
            };

            var user = fixture.Create<User>();
            configureUser?.Invoke(user);

            var userData = new UserItemData { Key = channel.Id.ToString("N") };
            if (savedSubtitleStreamIndex.HasValue)
            {
                userData.SubtitleStreamIndex = savedSubtitleStreamIndex.Value;
            }

            var userDataManager = fixture.Freeze<Mock<IUserDataManager>>();
            userDataManager.Setup(m => m.GetUserData(user, channel)).Returns(userData);

            var localizationManager = fixture.Freeze<Mock<ILocalizationManager>>();
            localizationManager
                .Setup(m => m.FindLanguageInfo(It.IsAny<string>()))
                .Returns((string s) => string.IsNullOrEmpty(s)
                    ? null
                    : new CultureDto(s, s, s, new EditableList<string> { s }));
            fixture.Inject(localizationManager.Object);

            return (fixture.Create<MediaSourceManager>(), channel, user);
        }

        private static MediaSourceInfo CreateLiveTvMediaSourceWithSubtitles(params MediaStream[] subtitleStreams)
        {
            var streams = new List<MediaStream>
            {
                new() { Index = 1, Type = MediaStreamType.Video, Codec = "h264" },
                new() { Index = 2, Type = MediaStreamType.Audio, Codec = "ac3", Language = "rum" },
            };
            streams.AddRange(subtitleStreams);

            return new MediaSourceInfo { IsInfiniteStream = true, MediaStreams = streams };
        }
    }
}
