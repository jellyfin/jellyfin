using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Castle.Components.DictionaryAdapter;
using Emby.Server.Implementations.IO;
using Emby.Server.Implementations.Library;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaSegments;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library
{
    public class MediaSourceManagerTests
    {
        private readonly MediaSourceManager _mediaSourceManager;
        private readonly Mock<IUserDataManager> _mockUserDataManager;
        private readonly Mock<ILocalizationManager> _mockLocalizationManager;
        private readonly Mock<IMediaStreamRepository> _mockMediaStreamRepository;
        private readonly Mock<IMediaAttachmentRepository> _mockMediaAttachmentRepository;
        private readonly Mock<IProviderManager> _mockProviderManager;
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
            _mockMediaStreamRepository = fixture.Freeze<Mock<IMediaStreamRepository>>();
            _mockMediaAttachmentRepository = fixture.Freeze<Mock<IMediaAttachmentRepository>>();
            _mockProviderManager = new Mock<IProviderManager>(MockBehavior.Strict);

            _mediaSourceManager = fixture.Create<MediaSourceManager>();
            _mediaSourceManager.AddParts(Array.Empty<IMediaSourceProvider>());

            _item = new Video { Id = Guid.NewGuid(), OwnerId = Guid.Empty, ParentId = Guid.Empty };

            _user = fixture.Create<User>();

            var mediaSegmentManager = new Mock<IMediaSegmentManager>();
            mediaSegmentManager.Setup(m => m.IsTypeSupported(It.IsAny<BaseItem>())).Returns(false);

            BaseItem.MediaSourceManager = _mediaSourceManager;
            BaseItem.MediaSegmentManager = mediaSegmentManager.Object;
            BaseItem.ProviderManager = _mockProviderManager.Object;
            BaseItem.Logger = NullLogger<BaseItem>.Instance;

            _mockMediaAttachmentRepository.Setup(m => m.GetMediaAttachments(It.IsAny<MediaAttachmentQuery>())).Returns(Array.Empty<MediaAttachment>());
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
        public async Task GetPlaybackMediaSources_StrmWithPersistedVideoStreamMetadata_DoesNotRefreshMetadata()
        {
            var item = CreateShortcutVideo();
            var persistedStreams = new List<MediaStream>
            {
                new()
                {
                    Index = 0,
                    Type = MediaStreamType.Video,
                    Codec = "h264"
                }
            };

            _mockMediaStreamRepository.Setup(m => m.GetMediaStreams(It.Is<MediaStreamQuery>(q => q.ItemId.Equals(item.Id))))
                .Returns(persistedStreams);

            var result = await _mediaSourceManager.GetPlaybackMediaSources(item, _user, true, false, CancellationToken.None);

            Assert.Single(result);
            Assert.Single(result[0].MediaStreams);
            Assert.Equal("h264", result[0].MediaStreams[0].Codec);
            _mockProviderManager.Verify(
                m => m.RefreshSingleItem(It.IsAny<BaseItem>(), It.IsAny<MetadataRefreshOptions>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GetPlaybackMediaSources_StrmWithoutPersistedPrimaryStreamMetadata_RefreshesMetadata()
        {
            var item = CreateShortcutVideo();
            var persistedStreams = new List<MediaStream>
            {
                new()
                {
                    Index = 0,
                    Type = MediaStreamType.Subtitle,
                    Codec = "srt"
                }
            };

            _mockMediaStreamRepository.Setup(m => m.GetMediaStreams(It.Is<MediaStreamQuery>(q => q.ItemId.Equals(item.Id))))
                .Returns(persistedStreams);
            _mockProviderManager.Setup(m => m.RefreshSingleItem(
                    It.Is<BaseItem>(i => ReferenceEquals(i, item)),
                    It.Is<MetadataRefreshOptions>(o => o.EnableRemoteContentProbe && o.MetadataRefreshMode == MetadataRefreshMode.FullRefresh),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(ItemUpdateType.MetadataImport);

            await _mediaSourceManager.GetPlaybackMediaSources(item, _user, true, false, CancellationToken.None);

            _mockProviderManager.Verify(
                m => m.RefreshSingleItem(
                    It.Is<BaseItem>(i => ReferenceEquals(i, item)),
                    It.Is<MetadataRefreshOptions>(o => o.EnableRemoteContentProbe && o.MetadataRefreshMode == MetadataRefreshMode.FullRefresh),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private static Video CreateShortcutVideo()
            => new()
            {
                Id = Guid.NewGuid(),
                OwnerId = Guid.Empty,
                ParentId = Guid.Empty,
                Path = "/media/strm/movie.strm",
                IsShortcut = true,
                ShortcutPath = "https://example.com/movie.mp4",
                VideoType = VideoType.VideoFile
            };
    }
}
