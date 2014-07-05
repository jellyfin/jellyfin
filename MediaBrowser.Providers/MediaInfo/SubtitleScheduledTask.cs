using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.MediaInfo
{
    public class SubtitleScheduledTask : IScheduledTask
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IServerConfigurationManager _config;
        private readonly ISubtitleManager _subtitleManager;
        private readonly ILogger _logger;

        public SubtitleScheduledTask(ILibraryManager libraryManager, IServerConfigurationManager config, ISubtitleManager subtitleManager, ILogger logger)
        {
            _libraryManager = libraryManager;
            _config = config;
            _subtitleManager = subtitleManager;
            _logger = logger;
        }

        public string Name
        {
            get { return "Download missing subtitles"; }
        }

        public string Description
        {
            get { return "Searches the internet for missing subtitles based on metadata configuration."; }
        }

        public string Category
        {
            get { return "Library"; }
        }

        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var videos = _libraryManager.RootFolder
                .RecursiveChildren
                .OfType<Video>()
                .Where(i =>
                {
                    if (i.LocationType == LocationType.Remote || i.LocationType == LocationType.Virtual)
                    {
                        return false;
                    }

                    return (_config.Configuration.SubtitleOptions.DownloadEpisodeSubtitles &&
                            i is Episode) ||
                           (_config.Configuration.SubtitleOptions.DownloadMovieSubtitles &&
                            i is Movie);
                })
                .ToList();

            var numComplete = 0;

            foreach (var video in videos)
            {
                try
                {
                    await DownloadSubtitles(video, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error downloading subtitles for {0}", ex, video.Path);
                }

                // Update progress
                numComplete++;
                double percent = numComplete;
                percent /= videos.Count;

                progress.Report(100 * percent);
            }
        }

        private async Task DownloadSubtitles(Video video, CancellationToken cancellationToken)
        {
            if ((_config.Configuration.SubtitleOptions.DownloadEpisodeSubtitles &&
                video is Episode) ||
                (_config.Configuration.SubtitleOptions.DownloadMovieSubtitles &&
                video is Movie))
            {
                var mediaStreams = video.GetMediaSources(false).First().MediaStreams;

                var externalSubtitleStreams = mediaStreams.Where(i => i.Type == MediaStreamType.Subtitle && i.IsExternal).ToList();
                var currentStreams = mediaStreams.Except(externalSubtitleStreams).ToList();

                var downloadedLanguages = await new SubtitleDownloader(_logger,
                    _subtitleManager)
                    .DownloadSubtitles(video,
                    currentStreams,
                    externalSubtitleStreams,
                    _config.Configuration.SubtitleOptions.SkipIfGraphicalSubtitlesPresent,
                    _config.Configuration.SubtitleOptions.SkipIfAudioTrackMatches,
                    _config.Configuration.SubtitleOptions.DownloadLanguages,
                    cancellationToken).ConfigureAwait(false);

                // Rescan
                if (downloadedLanguages.Count > 0)
                {
                    await video.RefreshMetadata(cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public IEnumerable<ITaskTrigger> GetDefaultTriggers()
        {
            return new ITaskTrigger[]
                {
                    new DailyTrigger { TimeOfDay = TimeSpan.FromHours(3) },
                };
        }
    }
}
