using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.LiveTv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Emby.XmlTv.Classes;

namespace MediaBrowser.Server.Implementations.LiveTv.Listings
{
    public class XmlTvListingsProvider : IListingsProvider
    {
        private string _filePath = "C:\\Temp\\";
        private string _language = null;
        private Dictionary<string, List<string>> _categoryMappings = new Dictionary<string, List<string>>(){
            { "Movie", new List<String>() { "Movie", "Film" } },
            { "Sports", new List<String>() { "Sports", "Football", "Rugby", "Soccer" } },
            { "Kids", new List<String>() { "Childrens", "Children", "Kids", "Disney" } },
            { "News", new List<String>() { "News", "Journalism", "Documentary", "Current Affairs" } },
        };

        public string Name
        {
            get { return "XmlTV"; }
        }

        public string Type
        {
            get { return "xmltv"; }
        }

        // TODO: Should this method be async?
        public Task<IEnumerable<ProgramInfo>> GetProgramsAsync(ListingsProviderInfo info, string channelNumber, string channelName, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken)
        {
            var reader = new XmlTvReader(_filePath, _language, null);
            var results = reader.GetProgrammes(channelNumber, startDateUtc, endDateUtc, cancellationToken);
            return Task.FromResult(results.Select(p => new ProgramInfo()
            {
                ChannelId = p.ChannelId,
                //CommunityRating = p.Rating.,
                EndDate = p.EndDate,
                EpisodeNumber = p.Episode == null ? null : p.Episode.Episode,
                EpisodeTitle = p.Episode == null ? null : p.Episode.Title,
                Genres = p.Categories,
                Id = String.Format("{0}_{1:O}", p.ChannelId, p.StartDate), // Construct an id from the channel and start date,
                StartDate = p.StartDate,
                Name = p.Title,
                Overview = p.Description,
                // OfficialRating = p.OfficialRating,
                ShortOverview = p.Description,
                ProductionYear = !p.CopyrightDate.HasValue ? (int?)null : p.CopyrightDate.Value.Year,
                SeasonNumber = p.Episode == null ? null : p.Episode.Series,
                IsSeries = p.IsSeries,
                IsRepeat = p.IsRepeat,
                IsPremiere = !p.PreviouslyShown.HasValue,
                IsKids = p.Categories.Any(_categoryMappings["Kids"].Contains),
                IsMovie = p.Categories.Any(_categoryMappings["Movie"].Contains),
                IsNews = p.Categories.Any(_categoryMappings["News"].Contains),
                IsSports = p.Categories.Any(_categoryMappings["Sports"].Contains),
                ImageUrl = p.Icon != null && !String.IsNullOrEmpty(p.Icon.Source) ? p.Icon.Source : null,
                HasImage = p.Icon != null && !String.IsNullOrEmpty(p.Icon.Source),
            }));
        }

        public async Task AddMetadata(ListingsProviderInfo info, List<ChannelInfo> channels, CancellationToken cancellationToken)
        {
            // Add the channel image url
            var reader = new XmlTvReader(_filePath, _language, null);
            var results = reader.GetChannels().ToList();

            if (channels != null && channels.Count > 0)
	        {
                channels.ForEach(c => {
                    var match = results.FirstOrDefault(r => r.Id == c.Id);
                    if (match != null && match.Icon != null && !String.IsNullOrEmpty(match.Icon.Source))
                    {
                        c.ImageUrl = match.Icon.Source;
                    }
                });
	        }
        }

        public async Task Validate(ListingsProviderInfo info, bool validateLogin, bool validateListings)
        {
            // Check that the path or url is valid. If not, throw a file not found exception
            if (!File.Exists(_filePath))
            {
                throw new FileNotFoundException("Could not find the XmlTv file specified", _filePath);
            }
        }

        public Task<List<NameIdPair>> GetLineups(ListingsProviderInfo info, string country, string location)
        {
            // In theory this should never be called because there is always only one lineup
            var reader = new XmlTvReader(_filePath, _language, null);
            var results = reader.GetChannels();

            // Should this method be async?
            return Task.FromResult(results.Select(c => new NameIdPair() { Id = c.Id, Name = c.DisplayName }).ToList());
        }
    }
}