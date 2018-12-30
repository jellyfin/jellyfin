using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.XmlTv.Classes;
using Emby.XmlTv.Entities;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Progress;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.LiveTv;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.LiveTv.Listings
{
    public class XmlTvListingsProvider : IListingsProvider
    {
        private readonly IServerConfigurationManager _config;
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IZipClient _zipClient;

        public XmlTvListingsProvider(IServerConfigurationManager config, IHttpClient httpClient, ILogger logger, IFileSystem fileSystem, IZipClient zipClient)
        {
            _config = config;
            _httpClient = httpClient;
            _logger = logger;
            _fileSystem = fileSystem;
            _zipClient = zipClient;
        }

        public string Name
        {
            get { return "XmlTV"; }
        }

        public string Type
        {
            get { return "xmltv"; }
        }

        private string GetLanguage(ListingsProviderInfo info)
        {
            if (!string.IsNullOrWhiteSpace(info.PreferredLanguage))
            {
                return info.PreferredLanguage;
            }

            return _config.Configuration.PreferredMetadataLanguage;
        }

        private async Task<string> GetXml(string path, CancellationToken cancellationToken)
        {
            _logger.LogInformation("xmltv path: {path}", path);

            if (!path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return UnzipIfNeeded(path, path);
            }

            string cacheFilename = DateTime.UtcNow.DayOfYear.ToString(CultureInfo.InvariantCulture) + "-" + DateTime.UtcNow.Hour.ToString(CultureInfo.InvariantCulture) + ".xml";
            string cacheFile = Path.Combine(_config.ApplicationPaths.CachePath, "xmltv", cacheFilename);
            if (_fileSystem.FileExists(cacheFile))
            {
                return UnzipIfNeeded(path, cacheFile);
            }

            _logger.LogInformation("Downloading xmltv listings from {path}", path);

            string tempFile = await _httpClient.GetTempFile(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = path,
                Progress = new SimpleProgress<Double>(),
                DecompressionMethod = CompressionMethod.Gzip,

                // It's going to come back gzipped regardless of this value
                // So we need to make sure the decompression method is set to gzip
                EnableHttpCompression = true,

                UserAgent = "Emby/3.0"

            }).ConfigureAwait(false);

            _fileSystem.CreateDirectory(_fileSystem.GetDirectoryName(cacheFile));

            _fileSystem.CopyFile(tempFile, cacheFile, true);

            return UnzipIfNeeded(path, cacheFile);
        }

        private string UnzipIfNeeded(string originalUrl, string file)
        {
            string ext = Path.GetExtension(originalUrl.Split('?')[0]);

            if (string.Equals(ext, ".gz", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    string tempFolder = ExtractGz(file);
                    return FindXmlFile(tempFolder);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error extracting from gz file {file}", file);
                }

                try
                {
                    string tempFolder = ExtractFirstFileFromGz(file);
                    return FindXmlFile(tempFolder);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error extracting from zip file {file}", file);
                }
            }

            return file;
        }

        private string ExtractFirstFileFromGz(string file)
        {
            using (var stream = _fileSystem.OpenRead(file))
            {
                string tempFolder = Path.Combine(_config.ApplicationPaths.TempDirectory, Guid.NewGuid().ToString());
                _fileSystem.CreateDirectory(tempFolder);

                _zipClient.ExtractFirstFileFromGz(stream, tempFolder, "data.xml");

                return tempFolder;
            }
        }

        private string ExtractGz(string file)
        {
            using (var stream = _fileSystem.OpenRead(file))
            {
                string tempFolder = Path.Combine(_config.ApplicationPaths.TempDirectory, Guid.NewGuid().ToString());
                _fileSystem.CreateDirectory(tempFolder);

                _zipClient.ExtractAllFromGz(stream, tempFolder, true);

                return tempFolder;
            }
        }

        private string FindXmlFile(string directory)
        {
            return _fileSystem.GetFiles(directory, true)
                .Where(i => string.Equals(i.Extension, ".xml", StringComparison.OrdinalIgnoreCase))
                .Select(i => i.FullName)
                .FirstOrDefault();
        }

        public async Task<IEnumerable<ProgramInfo>> GetProgramsAsync(ListingsProviderInfo info, string channelId, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(channelId))
            {
                throw new ArgumentNullException("channelId");
            }

            /*
            if (!await EmbyTV.EmbyTVRegistration.Instance.EnableXmlTv().ConfigureAwait(false))
            {
                var length = endDateUtc - startDateUtc;
                if (length.TotalDays > 1)
                {
                    endDateUtc = startDateUtc.AddDays(1);
                }
            }*/

            _logger.LogDebug("Getting xmltv programs for channel {id}", channelId);

            string path = await GetXml(info.Path, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Opening XmlTvReader for {path}", path);
            var reader = new XmlTvReader(path, GetLanguage(info));

            return reader.GetProgrammes(channelId, startDateUtc, endDateUtc, cancellationToken)
                        .Select(p => GetProgramInfo(p, info));
        }

        private ProgramInfo GetProgramInfo(XmlTvProgram program, ListingsProviderInfo info)
        {
            string episodeTitle = program.Episode?.Title;

            var programInfo = new ProgramInfo
            {
                ChannelId = program.ChannelId,
                EndDate = program.EndDate.UtcDateTime,
                EpisodeNumber = program.Episode?.Episode,
                EpisodeTitle = episodeTitle,
                Genres = program.Categories,
                StartDate = program.StartDate.UtcDateTime,
                Name = program.Title,
                Overview = program.Description,
                ProductionYear = program.CopyrightDate?.Year,
                SeasonNumber = program.Episode?.Series,
                IsSeries = program.Episode != null,
                IsRepeat = program.IsPreviouslyShown && !program.IsNew,
                IsPremiere = program.Premiere != null,
                IsKids = program.Categories.Any(c => info.KidsCategories.Contains(c, StringComparer.OrdinalIgnoreCase)),
                IsMovie = program.Categories.Any(c => info.MovieCategories.Contains(c, StringComparer.OrdinalIgnoreCase)),
                IsNews = program.Categories.Any(c => info.NewsCategories.Contains(c, StringComparer.OrdinalIgnoreCase)),
                IsSports = program.Categories.Any(c => info.SportsCategories.Contains(c, StringComparer.OrdinalIgnoreCase)),
                ImageUrl = program.Icon != null && !String.IsNullOrEmpty(program.Icon.Source) ? program.Icon.Source : null,
                HasImage = program.Icon != null && !String.IsNullOrEmpty(program.Icon.Source),
                OfficialRating = program.Rating != null && !String.IsNullOrEmpty(program.Rating.Value) ? program.Rating.Value : null,
                CommunityRating = program.StarRating,
                SeriesId = program.Episode == null ? null : program.Title.GetMD5().ToString("N")
            };

            if (string.IsNullOrWhiteSpace(program.ProgramId))
            {
                string uniqueString = (program.Title ?? string.Empty) + (episodeTitle ?? string.Empty) /*+ (p.IceTvEpisodeNumber ?? string.Empty)*/;

                if (programInfo.SeasonNumber.HasValue)
                {
                    uniqueString = "-" + programInfo.SeasonNumber.Value.ToString(CultureInfo.InvariantCulture);
                }
                if (programInfo.EpisodeNumber.HasValue)
                {
                    uniqueString = "-" + programInfo.EpisodeNumber.Value.ToString(CultureInfo.InvariantCulture);
                }

                programInfo.ShowId = uniqueString.GetMD5().ToString("N");

                // If we don't have valid episode info, assume it's a unique program, otherwise recordings might be skipped
                if (programInfo.IsSeries
                    && !programInfo.IsRepeat
                    && (programInfo.EpisodeNumber ?? 0) == 0)
                {
                    programInfo.ShowId = programInfo.ShowId + programInfo.StartDate.Ticks.ToString(CultureInfo.InvariantCulture);
                }
            }
            else
            {
                programInfo.ShowId = program.ProgramId;
            }

            // Construct an id from the channel and start date
            programInfo.Id = String.Format("{0}_{1:O}", program.ChannelId, program.StartDate);

            if (programInfo.IsMovie)
            {
                programInfo.IsSeries = false;
                programInfo.EpisodeNumber = null;
                programInfo.EpisodeTitle = null;
            }

            return programInfo;
        }

        public Task Validate(ListingsProviderInfo info, bool validateLogin, bool validateListings)
        {
            // Assume all urls are valid. check files for existence
            if (!info.Path.StartsWith("http", StringComparison.OrdinalIgnoreCase) && !_fileSystem.FileExists(info.Path))
            {
                throw new FileNotFoundException("Could not find the XmlTv file specified:", info.Path);
            }

            return Task.CompletedTask;
        }

        public async Task<List<NameIdPair>> GetLineups(ListingsProviderInfo info, string country, string location)
        {
            // In theory this should never be called because there is always only one lineup
            string path = await GetXml(info.Path, CancellationToken.None).ConfigureAwait(false);
            _logger.LogDebug("Opening XmlTvReader for {path}", path);
            var reader = new XmlTvReader(path, GetLanguage(info));
            IEnumerable<XmlTvChannel> results = reader.GetChannels();

            // Should this method be async?
            return results.Select(c => new NameIdPair() { Id = c.Id, Name = c.DisplayName }).ToList();
        }

        public async Task<List<ChannelInfo>> GetChannels(ListingsProviderInfo info, CancellationToken cancellationToken)
        {
            // In theory this should never be called because there is always only one lineup
            string path = await GetXml(info.Path, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Opening XmlTvReader for {path}", path);
            var reader = new XmlTvReader(path, GetLanguage(info));
            IEnumerable<XmlTvChannel> results = reader.GetChannels();

            // Should this method be async?
            return results.Select(c => new ChannelInfo
            {
                Id = c.Id,
                Name = c.DisplayName,
                ImageUrl = c.Icon != null && !String.IsNullOrEmpty(c.Icon.Source) ? c.Icon.Source : null,
                Number = string.IsNullOrWhiteSpace(c.Number) ? c.Id : c.Number

            }).ToList();
        }
    }
}
