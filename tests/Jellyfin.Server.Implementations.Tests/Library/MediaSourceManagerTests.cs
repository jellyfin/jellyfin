using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoMoq;
using Castle.Components.DictionaryAdapter;
using Emby.Server.Implementations.IO;
using Emby.Server.Implementations.Library;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaSegments;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
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
        public void GetStaticMediaSources_PrimaryQueried_DefaultsToMostRecentlyPlayedVersion()
        {
            var (primary, alt1, alt2) = SetupVersionGroup();
            SetupUserDataBatch(new Dictionary<Guid, UserItemData>
            {
                [alt1.Id] = new UserItemData { Key = "alt1", PlaybackPositionTicks = 10, LastPlayedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                [alt2.Id] = new UserItemData { Key = "alt2", PlaybackPositionTicks = 20, LastPlayedDate = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc) }
            });

            var sources = _mediaSourceManager.GetStaticMediaSources(primary, false, _user);

            // The most recently played version is the default source, so resuming plays the right file.
            // Per-user positions live in each version's UserData, not on the source.
            Assert.Equal(alt2.Id.ToString("N"), sources[0].Id);
        }

        [Fact]
        public void GetStaticMediaSources_AlternateQueried_KeepsOwnSourceFirst()
        {
            var (primary, alt1, alt2) = SetupVersionGroup();
            SetupUserDataBatch(new Dictionary<Guid, UserItemData>
            {
                [alt2.Id] = new UserItemData { Key = "alt2", PlaybackPositionTicks = 20, LastPlayedDate = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc) }
            });

            var sources = _mediaSourceManager.GetStaticMediaSources(alt1, false, _user);

            // An explicitly opened version keeps its own source first, even when a sibling was
            // played more recently.
            Assert.Equal(alt1.Id.ToString("N"), sources[0].Id);
            Assert.Equal(3, sources.Count);
        }

        [Fact]
        public void GetStaticMediaSources_NoProgress_KeepsQueriedItemFirst()
        {
            var (primary, _, _) = SetupVersionGroup();
            SetupUserDataBatch([]);

            var sources = _mediaSourceManager.GetStaticMediaSources(primary, false, _user);

            Assert.Equal(primary.Id.ToString("N"), sources[0].Id);
        }

        [Fact]
        public void GetStaticMediaSources_NoUser_DoesNotTouchUserData()
        {
            var (primary, _, _) = SetupVersionGroup();

            var sources = _mediaSourceManager.GetStaticMediaSources(primary, false);

            Assert.Equal(primary.Id.ToString("N"), sources[0].Id);
            _mockUserDataManager.Verify(x => x.GetUserDataBatch(It.IsAny<IReadOnlyList<BaseItem>>(), It.IsAny<User>()), Times.Never);
        }

        private void SetupUserDataBatch(Dictionary<Guid, UserItemData> userData)
        {
            _mockUserDataManager
                .Setup(x => x.GetUserDataBatch(It.IsAny<IReadOnlyList<BaseItem>>(), It.IsAny<User>()))
                .Returns((IReadOnlyList<BaseItem> items, User _) => items
                    .Where(i => userData.ContainsKey(i.Id))
                    .ToDictionary(i => i.Id, i => userData[i.Id]));
        }

        private static (Video Primary, Video Alt1, Video Alt2) SetupVersionGroup()
        {
            var primary = new Video { Id = Guid.NewGuid(), Path = "/Movies/Movie/Movie.mkv" };
            var alt1 = new Video { Id = Guid.NewGuid(), Path = "/Movies/Movie/Movie - 1080p.mkv", PrimaryVersionId = primary.Id };
            var alt2 = new Video { Id = Guid.NewGuid(), Path = "/Movies/Movie/Movie - 4K.mkv", PrimaryVersionId = primary.Id };

            // BaseItem.GetMediaSources runs against the static service locators.
            var mediaSourceManager = new Mock<IMediaSourceManager>();
            mediaSourceManager.Setup(x => x.GetPathProtocol(It.IsAny<string>())).Returns(MediaProtocol.File);
            mediaSourceManager.Setup(x => x.GetMediaStreams(It.IsAny<Guid>())).Returns(new List<MediaStream>());
            mediaSourceManager.Setup(x => x.GetMediaAttachments(It.IsAny<Guid>())).Returns(new List<MediaAttachment>());

            var segmentManager = new Mock<IMediaSegmentManager>();
            segmentManager.Setup(x => x.IsTypeSupported(It.IsAny<BaseItem>())).Returns(false);

            var libraryManager = new Mock<ILibraryManager>();
            libraryManager.Setup(x => x.GetLinkedAlternateVersions(It.IsAny<Video>())).Returns(Array.Empty<Video>());
            libraryManager.Setup(x => x.GetLocalAlternateVersionIds(primary)).Returns(new[] { alt1.Id, alt2.Id });
            libraryManager.Setup(x => x.GetLocalAlternateVersionIds(alt1)).Returns(Array.Empty<Guid>());
            libraryManager.Setup(x => x.GetLocalAlternateVersionIds(alt2)).Returns(Array.Empty<Guid>());
            libraryManager.Setup(x => x.GetItemById(primary.Id)).Returns(primary);
            libraryManager.Setup(x => x.GetItemById(alt1.Id)).Returns(alt1);
            libraryManager.Setup(x => x.GetItemById(alt2.Id)).Returns(alt2);

            var recordingsManager = new Mock<IRecordingsManager>();
            recordingsManager.Setup(x => x.GetActiveRecordingInfo(It.IsAny<string>())).Returns((ActiveRecordingInfo?)null);

            BaseItem.MediaSegmentManager = segmentManager.Object;
            BaseItem.MediaSourceManager = mediaSourceManager.Object;
            BaseItem.LibraryManager = libraryManager.Object;
            Video.RecordingsManager = recordingsManager.Object;

            return (primary, alt1, alt2);
        }
    }
}
