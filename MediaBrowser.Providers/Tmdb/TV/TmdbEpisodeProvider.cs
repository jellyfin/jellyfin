using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Tmdb.TV
{
    public class TmdbEpisodeProvider :
            TmdbEpisodeProviderBase,
            IRemoteMetadataProvider<Episode, EpisodeInfo>,
            IHasOrder
    {
        public TmdbEpisodeProvider(IHttpClient httpClient, IServerConfigurationManager configurationManager, IJsonSerializer jsonSerializer, IFileSystem fileSystem, ILocalizationManager localization, ILoggerFactory loggerFactory)
            : base(httpClient, configurationManager, jsonSerializer, fileSystem, localization, loggerFactory)
        { }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(EpisodeInfo searchInfo, CancellationToken cancellationToken)
        {
            var list = new List<RemoteSearchResult>();

            // The search query must either provide an episode number or date
            if (!searchInfo.IndexNumber.HasValue || !searchInfo.ParentIndexNumber.HasValue)
            {
                return list;
            }

            var metadataResult = await GetMetadata(searchInfo, cancellationToken);

            if (metadataResult.HasMetadata)
            {
                var item = metadataResult.Item;

                list.Add(new RemoteSearchResult
                {
                    IndexNumber = item.IndexNumber,
                    Name = item.Name,
                    ParentIndexNumber = item.ParentIndexNumber,
                    PremiereDate = item.PremiereDate,
                    ProductionYear = item.ProductionYear,
                    ProviderIds = item.ProviderIds,
                    SearchProviderName = Name,
                    IndexNumberEnd = item.IndexNumberEnd
                });
            }

            return list;
        }

        public async Task<MetadataResult<Episode>> GetMetadata(EpisodeInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Episode>();

            // Allowing this will dramatically increase scan times
            if (info.IsMissingEpisode)
            {
                return result;
            }

            info.SeriesProviderIds.TryGetValue(MetadataProviders.Tmdb.ToString(), out string seriesTmdbId);

            if (string.IsNullOrEmpty(seriesTmdbId))
            {
                return result;
            }

            var seasonNumber = info.ParentIndexNumber;
            var episodeNumber = info.IndexNumber;

            if (!seasonNumber.HasValue || !episodeNumber.HasValue)
            {
                return result;
            }

            try
            {
                var response = await GetEpisodeInfo(seriesTmdbId, seasonNumber.Value, episodeNumber.Value, info.MetadataLanguage, cancellationToken).ConfigureAwait(false);

                result.HasMetadata = true;
                result.QueriedById = true;

                if (!string.IsNullOrEmpty(response.Overview))
                {
                    // if overview is non-empty, we can assume that localized data was returned
                    result.ResultLanguage = info.MetadataLanguage;
                }

                var item = new Episode();
                result.Item = item;

                item.Name = info.Name;
                item.IndexNumber = info.IndexNumber;
                item.ParentIndexNumber = info.ParentIndexNumber;
                item.IndexNumberEnd = info.IndexNumberEnd;

                if (response.External_Ids.Tvdb_Id > 0)
                {
                    item.SetProviderId(MetadataProviders.Tvdb, response.External_Ids.Tvdb_Id.ToString(CultureInfo.InvariantCulture));
                }

                item.PremiereDate = response.Air_Date;
                item.ProductionYear = result.Item.PremiereDate.Value.Year;

                item.Name = response.Name;
                item.Overview = response.Overview;

                item.CommunityRating = (float)response.Vote_Average;

                if (response.Videos?.Results != null)
                {
                    foreach (var video in response.Videos.Results)
                    {
                        if (video.Type.Equals("trailer", System.StringComparison.OrdinalIgnoreCase)
                            || video.Type.Equals("clip", System.StringComparison.OrdinalIgnoreCase))
                        {
                            if (video.Site.Equals("youtube", System.StringComparison.OrdinalIgnoreCase))
                            {
                                var videoUrl = string.Format("http://www.youtube.com/watch?v={0}", video.Key);
                                item.AddTrailerUrl(videoUrl);
                            }
                        }
                    }
                }

                result.ResetPeople();

                var credits = response.Credits;
                if (credits != null)
                {
                    //Actors, Directors, Writers - all in People
                    //actors come from cast
                    if (credits.Cast != null)
                    {
                        foreach (var actor in credits.Cast.OrderBy(a => a.Order))
                        {
                            result.AddPerson(new PersonInfo { Name = actor.Name.Trim(), Role = actor.Character, Type = PersonType.Actor, SortOrder = actor.Order });
                        }
                    }

                    // guest stars
                    if (credits.Guest_Stars != null)
                    {
                        foreach (var guest in credits.Guest_Stars.OrderBy(a => a.Order))
                        {
                            result.AddPerson(new PersonInfo { Name = guest.Name.Trim(), Role = guest.Character, Type = PersonType.GuestStar, SortOrder = guest.Order });
                        }
                    }

                    //and the rest from crew
                    if (credits.Crew != null)
                    {
                        var keepTypes = new[]
                        {
                            PersonType.Director,
                            PersonType.Writer,
                            PersonType.Producer
                        };

                        foreach (var person in credits.Crew)
                        {
                            // Normalize this
                            var type = TmdbUtils.MapCrewToPersonType(person);

                            if (!keepTypes.Contains(type, StringComparer.OrdinalIgnoreCase) &&
                                !keepTypes.Contains(person.Job ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            result.AddPerson(new PersonInfo { Name = person.Name.Trim(), Role = person.Job, Type = type });
                        }
                    }
                }
            }
            catch (HttpException ex)
            {
                if (ex.StatusCode.HasValue && ex.StatusCode.Value == HttpStatusCode.NotFound)
                {
                    return result;
                }

                throw;
            }

            return result;
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return GetResponse(url, cancellationToken);
        }
        // After TheTvDb
        public int Order => 1;

        public string Name => TmdbUtils.ProviderName;
    }
}
