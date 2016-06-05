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
        private string _language = null;
        private Dictionary<string, List<string>> _categoryMappings = new Dictionary<string, List<string>>(){
            { "Movie", new List<String>() { "Movie", "Film" } },
            //{ "Sports", new List<String>() { "Sports", "Football", "Rugby", "Soccer" } },
            //{ "Kids", new List<String>() { "Childrens", "Children", "Kids", "Disney" } },
            //{ "News", new List<String>() { "News", "Journalism", "Documentary", "Current Affairs" } },
        };

        private Dictionary<string, string> _channelMappings = new Dictionary<string, string>(){
            { "1", "UK_RT_2667" },
            { "2", "UK_RT_116" },
            { "3", "UK_RT_2118" },
            { "4", "UK_RT_2056" },
            { "5", "UK_RT_134" }
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
            var reader = new XmlTvReader(info.Path, _language, null);
            string mappedChannel = channelNumber;

            if (_channelMappings.ContainsKey(channelNumber))
	        {
		        mappedChannel = _channelMappings[channelNumber];
	        }

            var results = reader.GetProgrammes(mappedChannel, startDateUtc, endDateUtc, cancellationToken);
            return Task.FromResult(results.Select(p => new ProgramInfo()
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
                IsKids = p.Categories.Any(info.KidsCategories.Contains),
                IsMovie = p.Categories.Any(info.MovieCategories.Contains),
                IsNews = p.Categories.Any(info.NewsCategories.Contains),
                IsSports = p.Categories.Any(info.SportsCategories.Contains),
                ImageUrl = p.Icon != null && !String.IsNullOrEmpty(p.Icon.Source) ? p.Icon.Source : null,
                HasImage = p.Icon != null && !String.IsNullOrEmpty(p.Icon.Source),
                OfficialRating = p.Rating != null && !String.IsNullOrEmpty(p.Rating.Value) ? p.Rating.Value : null,
                CommunityRating = p.StarRating.HasValue ? p.StarRating.Value : (float?)null
            }));
        }

        public async Task AddMetadata(ListingsProviderInfo info, List<ChannelInfo> channels, CancellationToken cancellationToken)
        {
            // Add the channel image url
            var reader = new XmlTvReader(info.Path, _language, null);
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
            if (!File.Exists(info.Path))
            {
                throw new FileNotFoundException("Could not find the XmlTv file specified:", info.Path);
            }
        }

        public Task<List<NameIdPair>> GetLineups(ListingsProviderInfo info, string country, string location)
        {
            // In theory this should never be called because there is always only one lineup
            var reader = new XmlTvReader(info.Path, _language, null);
            var results = reader.GetChannels();

            // Should this method be async?
            return Task.FromResult(results.Select(c => new NameIdPair() { Id = c.Id, Name = c.DisplayName }).ToList());
        }
    }
}