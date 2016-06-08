using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.LiveTv;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Emby.XmlTv.Classes;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;

namespace MediaBrowser.Server.Implementations.LiveTv.Listings
{
    public class XmlTvListingsProvider : IListingsProvider
    {
        private readonly IServerConfigurationManager _config;
        private readonly IHttpClient _httpClient;

        public XmlTvListingsProvider(IServerConfigurationManager config, IHttpClient httpClient)
        {
            _config = config;
            _httpClient = httpClient;
        }

        public string Name
        {
            get { return "XmlTV"; }
        }

        public string Type
        {
            get { return "xmltv"; }
        }

        private string GetLanguage()
        {
            return _config.Configuration.PreferredMetadataLanguage;
        }

        private async Task<string> GetXml(string path, CancellationToken cancellationToken)
        {
            if (!path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            var cacheFilename = DateTime.UtcNow.DayOfYear.ToString(CultureInfo.InvariantCulture) + "_" + DateTime.UtcNow.Hour.ToString(CultureInfo.InvariantCulture) + ".xml";
            var cacheFile = Path.Combine(_config.ApplicationPaths.CachePath, "xmltv", cacheFilename);
            if (File.Exists(cacheFile))
            {
                return cacheFile;
            }

            var tempFile = await _httpClient.GetTempFile(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = path

            }).ConfigureAwait(false);
            File.Copy(tempFile, cacheFile, true);

            return cacheFile;
        }

        // TODO: Should this method be async?
        public async Task<IEnumerable<ProgramInfo>> GetProgramsAsync(ListingsProviderInfo info, string channelNumber, string channelName, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken)
        {
            var path = await GetXml(info.Path, cancellationToken).ConfigureAwait(false);
            var reader = new XmlTvReader(path, GetLanguage(), null);

            var results = reader.GetProgrammes(channelNumber, startDateUtc, endDateUtc, cancellationToken);
            return results.Select(p => new ProgramInfo()
            {
                ChannelId = p.ChannelId,
                EndDate = p.EndDate,
                EpisodeNumber = p.Episode == null ? null : p.Episode.Episode,
                EpisodeTitle = p.Episode == null ? null : p.Episode.Title,
                Genres = p.Categories,
                Id = String.Format("{0}_{1:O}", p.ChannelId, p.StartDate), // Construct an id from the channel and start date,
                StartDate = p.StartDate,
                Name = p.Title,
                Overview = p.Description,
                ShortOverview = p.Description,
                ProductionYear = !p.CopyrightDate.HasValue ? (int?)null : p.CopyrightDate.Value.Year,
                SeasonNumber = p.Episode == null ? null : p.Episode.Series,
                IsSeries = p.IsSeries,
                IsRepeat = p.IsRepeat,
                // IsPremiere = !p.PreviouslyShown.HasValue,
                IsKids = p.Categories.Any(c => info.KidsCategories.Contains(c, StringComparer.InvariantCultureIgnoreCase)),
                IsMovie = p.Categories.Any(c => info.MovieCategories.Contains(c, StringComparer.InvariantCultureIgnoreCase)),
                IsNews = p.Categories.Any(c => info.NewsCategories.Contains(c, StringComparer.InvariantCultureIgnoreCase)),
                IsSports = p.Categories.Any(c => info.SportsCategories.Contains(c, StringComparer.InvariantCultureIgnoreCase)),
                ImageUrl = p.Icon != null && !String.IsNullOrEmpty(p.Icon.Source) ? p.Icon.Source : null,
                HasImage = p.Icon != null && !String.IsNullOrEmpty(p.Icon.Source),
                OfficialRating = p.Rating != null && !String.IsNullOrEmpty(p.Rating.Value) ? p.Rating.Value : null,
                CommunityRating = p.StarRating.HasValue ? p.StarRating.Value : (float?)null
            });
        }

        public Task AddMetadata(ListingsProviderInfo info, List<ChannelInfo> channels, CancellationToken cancellationToken)
        {
            // Add the channel image url
            var reader = new XmlTvReader(info.Path, GetLanguage(), null);
            var results = reader.GetChannels().ToList();

            if (channels != null)
            {
                channels.ForEach(c =>
                {
                    var channelNumber = info.GetMappedChannel(c.Number);
                    var match = results.FirstOrDefault(r => string.Equals(r.Id, channelNumber, StringComparison.OrdinalIgnoreCase));

                    if (match != null && match.Icon != null && !String.IsNullOrEmpty(match.Icon.Source))
                    {
                        c.ImageUrl = match.Icon.Source;
                    }
                });
            }

            return Task.FromResult(true);
        }

        public Task Validate(ListingsProviderInfo info, bool validateLogin, bool validateListings)
        {
            // Assume all urls are valid. check files for existence
            if (!info.Path.StartsWith("http", StringComparison.OrdinalIgnoreCase) && !File.Exists(info.Path))
            {
                throw new FileNotFoundException("Could not find the XmlTv file specified:", info.Path);
            }

            return Task.FromResult(true);
        }

        public Task<List<NameIdPair>> GetLineups(ListingsProviderInfo info, string country, string location)
        {
            // In theory this should never be called because there is always only one lineup
            var reader = new XmlTvReader(info.Path, GetLanguage(), null);
            var results = reader.GetChannels();

            // Should this method be async?
            return Task.FromResult(results.Select(c => new NameIdPair() { Id = c.Id, Name = c.DisplayName }).ToList());
        }

        public async Task<List<ChannelInfo>> GetChannels(ListingsProviderInfo info, CancellationToken cancellationToken)
        {
            return new List<ChannelInfo>();
        }
    }
}