using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Providers;
using MediaBrowser.Providers.MediaInfo;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.MediaInfo;

public class SubtitleDownloaderTests
{
    public SubtitleDownloaderTests()
    {
        // Video.SourceType/IsCompleteMedia touch the static RecordingsManager
        // before the language guards are reached. Set a no-op instance so the
        // early-return guards don't throw NullReferenceException.
        var recordings = new Mock<IRecordingsManager>(MockBehavior.Strict);
        recordings.Setup(r => r.GetActiveRecordingInfo(It.IsAny<string>()))
            .Returns((ActiveRecordingInfo?)null);
        Video.RecordingsManager = recordings.Object;
    }

    // Real cultures resolve the same canonical Name regardless of whether the
    // caller supplies the ISO 639-2 ("en") or ISO 639-3 ("eng") code. The
    // pre-download guard relies on this so an existing external subtitle is
    // detected and the download (which consumes the OpenSubtitles daily quota)
    // is skipped instead of re-creating movie.en.0.srt, movie.en.1.srt, ...
    private static ILocalizationManager CreateLocalization()
    {
        var english = new CultureDto("en", "English", "en", ["eng"]);
        var spanish = new CultureDto("es", "Spanish", "es", ["spa"]);
        var cultures = new List<CultureDto> { english, spanish };

        var mock = new Mock<ILocalizationManager>(MockBehavior.Strict);
        mock.Setup(m => m.FindLanguageInfo(It.IsAny<string>()))
            .Returns<string>(lang =>
            {
                if (string.IsNullOrEmpty(lang))
                {
                    return null;
                }

                foreach (var c in cultures)
                {
                    if (lang.Equals(c.Name, System.StringComparison.OrdinalIgnoreCase)
                        || lang.Equals(c.DisplayName, System.StringComparison.OrdinalIgnoreCase)
                        || lang.Equals(c.TwoLetterISOLanguageName, System.StringComparison.OrdinalIgnoreCase)
                        || c.ThreeLetterISOLanguageNames.Contains(lang, StringComparer.OrdinalIgnoreCase))
                    {
                        return c;
                    }
                }

                return null;
            });
        return mock.Object;
    }

    [Theory]
    [InlineData("en", "eng", true)] // 2-letter request, 3-letter stream -> guard fires, search skipped
    [InlineData("eng", "en", true)] // 3-letter request, 2-letter stream -> guard fires, search skipped
    [InlineData("en", "es", false)] // different language -> guard passes, search runs
    [InlineData("en", "spa", false)] // different language (3-letter stream) -> guard passes, search runs
    public async Task DownloadSubtitles_NormalizesLanguageCodesBeforeGuard(string requested, string existing, bool expectSkip)
    {
        var video = new Movie { Path = "/movies/Film (2024)/Film (2024).mkv" };

        // External text subtitle for the "existing" language, stored the way
        // ExternalPathParser/FFprobe leaves it (often a 3-letter code).
        var streams = new List<MediaStream>
        {
            new()
            {
                Type = MediaStreamType.Subtitle,
                Language = existing,
                IsExternal = true,
                Codec = "srt"
            }
        };

        var subtitleManager = new Mock<ISubtitleManager>(MockBehavior.Strict);
        // When the guard is bypassed, DownloadSubtitles calls SearchSubtitles.
        // Return an empty result set so the download branch is never entered.
        subtitleManager
            .Setup(m => m.SearchSubtitles(It.IsAny<SubtitleSearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(System.Array.Empty<RemoteSubtitleInfo>());

        var downloader = new SubtitleDownloader(
            new NullLogger<SubtitleDownloader>(),
            subtitleManager.Object,
            CreateLocalization());

        await downloader.DownloadSubtitles(
            video,
            streams,
            skipIfEmbeddedSubtitlesPresent: false,
            skipIfAudioTrackMatches: false,
            requirePerfectMatch: false,
            requested,
            [],
            [],
            isAutomated: true,
            CancellationToken.None);

        var searchCalled = subtitleManager.Invocations.Count > 0;
        Assert.Equal(expectSkip, !searchCalled);
    }
}
