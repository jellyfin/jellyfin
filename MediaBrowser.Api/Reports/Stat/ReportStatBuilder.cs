using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Api.Reports
{
    /// <summary> A report stat builder. </summary>
    /// <seealso cref="T:MediaBrowser.Api.Reports.ReportBuilderBase"/>
    public class ReportStatBuilder : ReportBuilderBase
    {
        #region [Constructors]

        /// <summary>
        /// Initializes a new instance of the MediaBrowser.Api.Reports.ReportStatBuilder class. </summary>
        /// <param name="libraryManager"> Manager for library. </param>
        public ReportStatBuilder(ILibraryManager libraryManager)
            : base(libraryManager)
        {
        }

        #endregion

        #region [Public Methods]

        /// <summary> Gets report stat result. </summary>
        /// <param name="items"> The items. </param>
        /// <param name="reportIncludeItemTypes"> List of types of the report include items. </param>
        /// <param name="topItem"> The top item. </param>
        /// <returns> The report stat result. </returns>
        public ReportStatResult GetResult(BaseItem[] items, ReportIncludeItemTypes reportIncludeItemTypes, int topItem = 5)
        {
            ReportStatResult result = new ReportStatResult();
            result = this.GetResultGenres(result, items, topItem);
            result = this.GetResultStudios(result, items, topItem);
            result = this.GetResultPersons(result, items, topItem);
            result = this.GetResultProductionYears(result, items, topItem);
            result = this.GetResulProductionLocations(result, items, topItem);
            result = this.GetResultCommunityRatings(result, items, topItem);
            result = this.GetResultParentalRatings(result, items, topItem);

            switch (reportIncludeItemTypes)
            {
                case ReportIncludeItemTypes.Season:
                case ReportIncludeItemTypes.Series:
                case ReportIncludeItemTypes.MusicAlbum:
                case ReportIncludeItemTypes.MusicArtist:
                case ReportIncludeItemTypes.Game:
                    break;
                case ReportIncludeItemTypes.Movie:
                case ReportIncludeItemTypes.BoxSet:

                    break;
                case ReportIncludeItemTypes.Book:
                case ReportIncludeItemTypes.Episode:
                case ReportIncludeItemTypes.Video:
                case ReportIncludeItemTypes.MusicVideo:
                case ReportIncludeItemTypes.Trailer:
                case ReportIncludeItemTypes.Audio:
                case ReportIncludeItemTypes.BaseItem:
                default:
                    break;
            }

            result.Groups = result.Groups.OrderByDescending(n => n.Items.Count()).ToList();

            return result;
        }

        #endregion

        #region [Protected Internal Methods]
        /// <summary> Gets the headers. </summary>
        /// <typeparam name="H"> Type of the header. </typeparam>
        /// <param name="request"> The request. </param>
        /// <returns> The headers. </returns>
        /// <seealso cref="M:MediaBrowser.Api.Reports.ReportBuilderBase.GetHeaders{H}(H)"/>
        protected internal override List<ReportHeader> GetHeaders<H>(H request)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region [Private Methods]

        /// <summary> Gets the groups. </summary>
        /// <param name="result"> The result. </param>
        /// <param name="header"> The header. </param>
        /// <param name="topItem"> The top item. </param>
        /// <param name="top"> The top. </param>
        private void GetGroups(ReportStatResult result, string header, int topItem, IEnumerable<ReportStatItem> top)
        {
            if (top != null && top.Count() > 0)
            {
                var group = new ReportStatGroup { Header = ReportStatGroup.FormatedHeader(header, topItem) };
                group.Items.AddRange(top);
                result.Groups.Add(group);
            }
        }

        /// <summary> Gets resul production locations. </summary>
        /// <param name="result"> The result. </param>
        /// <param name="items"> The items. </param>
        /// <param name="topItem"> The top item. </param>
        /// <returns> The resul production locations. </returns>
        private ReportStatResult GetResulProductionLocations(ReportStatResult result, BaseItem[] items, int topItem = 5)
        {
            this.GetGroups(result, GetLocalizedHeader(HeaderMetadata.Countries), topItem,
                        items.OfType<IHasProductionLocations>()
                        .Where(x => x.ProductionLocations != null)
                        .SelectMany(x => x.ProductionLocations)
                        .GroupBy(x => x)
                        .OrderByDescending(x => x.Count())
                        .Take(topItem)
                        .Select(x => new ReportStatItem
                        {
                            Name = x.Key.ToString(),
                            Value = x.Count().ToString()
                        })
            );

            return result;
        }

        /// <summary> Gets result community ratings. </summary>
        /// <param name="result"> The result. </param>
        /// <param name="items"> The items. </param>
        /// <param name="topItem"> The top item. </param>
        /// <returns> The result community ratings. </returns>
        private ReportStatResult GetResultCommunityRatings(ReportStatResult result, BaseItem[] items, int topItem = 5)
        {
            this.GetGroups(result, GetLocalizedHeader(HeaderMetadata.CommunityRating), topItem,
                       items.Where(x => x.CommunityRating != null && x.CommunityRating > 0)
                           .GroupBy(x => x.CommunityRating)
                           .OrderByDescending(x => x.Count())
                           .Take(topItem)
                           .Select(x => new ReportStatItem
                           {
                               Name = x.Key.ToString(),
                               Value = x.Count().ToString()
                           })
               );

            return result;
        }

        /// <summary> Gets result genres. </summary>
        /// <param name="result"> The result. </param>
        /// <param name="items"> The items. </param>
        /// <param name="topItem"> The top item. </param>
        /// <returns> The result genres. </returns>
        private ReportStatResult GetResultGenres(ReportStatResult result, BaseItem[] items, int topItem = 5)
        {
            this.GetGroups(result, GetLocalizedHeader(HeaderMetadata.Genres), topItem,
                            items.SelectMany(x => x.Genres)
                                .GroupBy(x => x)
                                .OrderByDescending(x => x.Count())
                                .Take(topItem)
                                .Select(x => new ReportStatItem
                                {
                                    Name = x.Key,
                                    Value = x.Count().ToString(),
                                    Id = GetGenreID(x.Key)
                                }));
            return result;

        }

        /// <summary> Gets result parental ratings. </summary>
        /// <param name="result"> The result. </param>
        /// <param name="items"> The items. </param>
        /// <param name="topItem"> The top item. </param>
        /// <returns> The result parental ratings. </returns>
        private ReportStatResult GetResultParentalRatings(ReportStatResult result, BaseItem[] items, int topItem = 5)
        {
            this.GetGroups(result, GetLocalizedHeader(HeaderMetadata.ParentalRatings), topItem,
                       items.Where(x => x.OfficialRating != null)
                           .GroupBy(x => x.OfficialRating)
                           .OrderByDescending(x => x.Count())
                           .Take(topItem)
                           .Select(x => new ReportStatItem
                           {
                               Name = x.Key.ToString(),
                               Value = x.Count().ToString()
                           })
               );

            return result;
        }

        /// <summary> Gets result persons. </summary>
        /// <param name="result"> The result. </param>
        /// <param name="items"> The items. </param>
        /// <param name="topItem"> The top item. </param>
        /// <returns> The result persons. </returns>
        private ReportStatResult GetResultPersons(ReportStatResult result, BaseItem[] items, int topItem = 5)
        {
            List<HeaderMetadata> t = new List<HeaderMetadata> 
            { 
                HeaderMetadata.Actor, 
                HeaderMetadata.Composer, 
                HeaderMetadata.Director, 
                HeaderMetadata.GuestStar, 
                HeaderMetadata.Producer,
                HeaderMetadata.Writer, 
                HeaderMetadata.Artist, 
                HeaderMetadata.AlbumArtist
            };
            foreach (var item in t)
            {
                var ps = items.SelectMany(x => _libraryManager.GetPeople(x))
                                .Where(n => n.Type == item.ToString())
                                .GroupBy(x => x.Name)
                                .OrderByDescending(x => x.Count())
                                .Take(topItem);
                if (ps != null && ps.Count() > 0)
                    this.GetGroups(result, GetLocalizedHeader(item), topItem,
                            ps.Select(x => new ReportStatItem
                                    {
                                        Name = x.Key,
                                        Value = x.Count().ToString(),
                                        Id = GetPersonID(x.Key)
                                    })
                    );
            }

            return result;
        }

        /// <summary> Gets result production years. </summary>
        /// <param name="result"> The result. </param>
        /// <param name="items"> The items. </param>
        /// <param name="topItem"> The top item. </param>
        /// <returns> The result production years. </returns>
        private ReportStatResult GetResultProductionYears(ReportStatResult result, BaseItem[] items, int topItem = 5)
        {
            this.GetGroups(result, GetLocalizedHeader(HeaderMetadata.Year), topItem,
                    items.Where(x => x.ProductionYear != null && x.ProductionYear > 0)
                        .GroupBy(x => x.ProductionYear)
                        .OrderByDescending(x => x.Count())
                        .Take(topItem)
                        .Select(x => new ReportStatItem
                        {
                            Name = x.Key.ToString(),
                            Value = x.Count().ToString()
                        })
            );

            return result;
        }

        /// <summary> Gets result studios. </summary>
        /// <param name="result"> The result. </param>
        /// <param name="items"> The items. </param>
        /// <param name="topItem"> The top item. </param>
        /// <returns> The result studios. </returns>
        private ReportStatResult GetResultStudios(ReportStatResult result, BaseItem[] items, int topItem = 5)
        {
            this.GetGroups(result, GetLocalizedHeader(HeaderMetadata.Studios), topItem,
                                    items.SelectMany(x => x.Studios)
                                        .GroupBy(x => x)
                                        .OrderByDescending(x => x.Count())
                                        .Take(topItem)
                                        .Select(x => new ReportStatItem
                                        {
                                            Name = x.Key,
                                            Value = x.Count().ToString(),
                                            Id = GetStudioID(x.Key)
                                        })
                    );

            return result;

        }

        #endregion

    }
}
