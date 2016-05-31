using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.LiveTv;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Emby.XmlTv.Classes;
using System.IO;

namespace MediaBrowser.Server.Implementations.LiveTv.Listings
{
    public class XmlTvListingsProvider : IListingsProvider
    {
        private string _filePath = "C:\\Temp\\";
        private string _language = null;

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
                    if (match != null)
                    {
                        // c.ImageUrl = match.Url;
                        // TODO: Add support for the channel logo to the XMLTv Component
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