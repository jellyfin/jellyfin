using AutoFixture;
using AutoFixture.AutoMoq;
using Castle.Components.DictionaryAdapter;
using Emby.Server.Implementations.IO;
using Emby.Server.Implementations.Library;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
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

            _item = fixture.Create<Video>();
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
        [InlineData(1, "eng", "eng", true, true)]
        [InlineData(3, "eng", "eng", false, true)]
        [InlineData(1, "ger", "eng", false, true)]
        [InlineData(3, "OriginalLanguage", "eng", false, false)]
        [InlineData(4, "OriginalLanguage", "eng", false, true)]
        [InlineData(1, "OriginalLanguage", "jpn", true, true)]
        [InlineData(2, "OriginalLanguage", "jpn", false, false)]
        [InlineData(2, "OriginalLanguage", "jpn", false, true)]
        [InlineData(4, "OriginalLanguage", null, false, true)]
        [InlineData(4, "OriginalLanguage", "", false, true)]
        [InlineData(1, "OriginalLanguage", "ger", false, true)]
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
                    IsDefault = true,
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
    }
}
