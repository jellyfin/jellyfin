using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Querying;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;
using System.Linq;
using MediaBrowser.Controller.Localization;
using System;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Activity;
using MediaBrowser.Controller.Activity;

namespace MediaBrowser.Api.Reports
{
    /// <summary> The reports service. </summary>
    /// <seealso cref="T:MediaBrowser.Api.BaseApiService"/>
    public class ReportsService : BaseApiService
    {
        #region [Constructors]

        /// <summary>
        /// Initializes a new instance of the MediaBrowser.Api.Reports.ReportsService class. </summary>
        /// <param name="userManager"> Manager for user. </param>
        /// <param name="libraryManager"> Manager for library. </param>
        /// <param name="localization"> The localization. </param>
        /// <param name="activityManager"> Manager for activity. </param>
        public ReportsService(IUserManager userManager, ILibraryManager libraryManager, ILocalizationManager localization, IActivityManager activityManager, IActivityRepository repo)
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
            _localization = localization;
            _activityManager = activityManager;
            _repo = repo;
        }

        #endregion

        #region [Private Fields]

        private readonly IActivityManager _activityManager; ///< Manager for activity

        /// <summary> Manager for library. </summary>
        private readonly ILibraryManager _libraryManager;   ///< Manager for library
        /// <summary> The localization. </summary>

        private readonly ILocalizationManager _localization;    ///< The localization

        private readonly IActivityRepository _repo;

        /// <summary> Manager for user. </summary>
        private readonly IUserManager _userManager; ///< Manager for user

        #endregion

        #region [Public Methods]

        /// <summary> Gets the given request. </summary>
        /// <param name="request"> The request. </param>
        /// <returns> A Task&lt;object&gt; </returns>
        public async Task<object> Get(GetActivityLogs request)
        {
            request.DisplayType = "Screen";
            ReportResult result = await GetReportActivities(request).ConfigureAwait(false);
            return ToOptimizedResult(result);
        }

        /// <summary> Gets the given request. </summary>
        /// <param name="request"> The request. </param>
        /// <returns> A Task&lt;object&gt; </returns>
        public async Task<object> Get(GetReportHeaders request)
        {
            if (string.IsNullOrEmpty(request.IncludeItemTypes))
                return null;

            request.DisplayType = "Screen";
            ReportViewType reportViewType = ReportHelper.GetReportViewType(request.ReportView);

            List<ReportHeader> result = new List<ReportHeader>();
            switch (reportViewType)
            {
                case ReportViewType.ReportData:
                    ReportBuilder dataBuilder = new ReportBuilder(_libraryManager);
                    result = dataBuilder.GetHeaders(request);
                    break;
                case ReportViewType.ReportStatistics:
                    break;
                case ReportViewType.ReportActivities:
                    ReportActivitiesBuilder activityBuilder = new ReportActivitiesBuilder(_libraryManager, _userManager);
                    result = activityBuilder.GetHeaders(request);
                    break;
            }

            return ToOptimizedResult(result);

        }

        /// <summary> Gets the given request. </summary>
        /// <param name="request"> The request. </param>
        /// <returns> A Task&lt;object&gt; </returns>
        public async Task<object> Get(GetItemReport request)
        {
            if (string.IsNullOrEmpty(request.IncludeItemTypes))
                return null;

            request.DisplayType = "Screen";
            var reportResult = await GetReportResult(request);

            return ToOptimizedResult(reportResult);
        }

        /// <summary> Gets the given request. </summary>
        /// <param name="request"> The request. </param>
        /// <returns> A Task&lt;object&gt; </returns>
        public async Task<object> Get(GetReportStatistics request)
        {
            if (string.IsNullOrEmpty(request.IncludeItemTypes))
                return null;
            request.DisplayType = "Screen";
            var reportResult = await GetReportStatistic(request);

            return ToOptimizedResult(reportResult);
        }

        /// <summary> Gets the given request. </summary>
        /// <param name="request"> The request. </param>
        /// <returns> A Task&lt;object&gt; </returns>
        public async Task<object> Get(GetReportDownload request)
        {
            if (string.IsNullOrEmpty(request.IncludeItemTypes))
                return null;

            request.DisplayType = "Export";
            ReportViewType reportViewType = ReportHelper.GetReportViewType(request.ReportView);
            var headers = new Dictionary<string, string>();
            string fileExtension = "csv";
            string contentType = "text/plain;charset='utf-8'";

            switch (request.ExportType)
            {
                case ReportExportType.CSV:
                    break;
                case ReportExportType.Excel:
                    contentType = "application/vnd.ms-excel";
                    fileExtension = "xls";
                    break;
            }

            var filename = "ReportExport." + fileExtension;
            headers["Content-Disposition"] = string.Format("attachment; filename=\"{0}\"", filename);
            headers["Content-Encoding"] = "UTF-8";

            ReportResult result = null;
            switch (reportViewType)
            {
                case ReportViewType.ReportStatistics:
                case ReportViewType.ReportData:
                    ReportIncludeItemTypes reportRowType = ReportHelper.GetRowType(request.IncludeItemTypes);
                    ReportBuilder dataBuilder = new ReportBuilder(_libraryManager);
                    QueryResult<BaseItem> queryResult = await GetQueryResult(request).ConfigureAwait(false);
                    result = dataBuilder.GetResult(queryResult.Items, request);
                    result.TotalRecordCount = queryResult.TotalRecordCount;
                    break;
                case ReportViewType.ReportActivities:
                    result = await GetReportActivities(request).ConfigureAwait(false);
                    break;
            }

            string returnResult = string.Empty;
            switch (request.ExportType)
            {
                case ReportExportType.CSV:
                    returnResult = new ReportExport().ExportToCsv(result);
                    break;
                case ReportExportType.Excel:
                    returnResult = new ReportExport().ExportToExcel(result);
                    break;
            }

            object ro = ResultFactory.GetResult(returnResult, contentType, headers);
            return ro;
        }

        #endregion

        #region [Private Methods]

        /// <summary> Gets items query. </summary>
        /// <param name="request"> The request. </param>
        /// <param name="user"> The user. </param>
        /// <returns> The items query. </returns>
        private InternalItemsQuery GetItemsQuery(BaseReportRequest request, User user)
        {
            var query = new InternalItemsQuery
            {
                User = user,
                IsPlayed = request.IsPlayed,
                MediaTypes = request.GetMediaTypes(),
                IncludeItemTypes = request.GetIncludeItemTypes(),
                ExcludeItemTypes = request.GetExcludeItemTypes(),
                Recursive = request.Recursive,
                SortBy = request.GetOrderBy(),
                SortOrder = request.SortOrder ?? SortOrder.Ascending,

                IsFavorite = request.IsFavorite,
                Limit = request.Limit,
                StartIndex = request.StartIndex,
                IsMissing = request.IsMissing,
                IsVirtualUnaired = request.IsVirtualUnaired,
                IsUnaired = request.IsUnaired,
                CollapseBoxSetItems = request.CollapseBoxSetItems,
                NameLessThan = request.NameLessThan,
                NameStartsWith = request.NameStartsWith,
                NameStartsWithOrGreater = request.NameStartsWithOrGreater,
                HasImdbId = request.HasImdbId,
                IsYearMismatched = request.IsYearMismatched,
                IsPlaceHolder = request.IsPlaceHolder,
                IsLocked = request.IsLocked,
                IsInBoxSet = request.IsInBoxSet,
                IsHD = request.IsHD,
                Is3D = request.Is3D,
                HasTvdbId = request.HasTvdbId,
                HasTmdbId = request.HasTmdbId,
                HasOverview = request.HasOverview,
                HasOfficialRating = request.HasOfficialRating,
                HasParentalRating = request.HasParentalRating,
                HasSpecialFeature = request.HasSpecialFeature,
                HasSubtitles = request.HasSubtitles,
                HasThemeSong = request.HasThemeSong,
                HasThemeVideo = request.HasThemeVideo,
                HasTrailer = request.HasTrailer,
                Tags = request.GetTags(),
                OfficialRatings = request.GetOfficialRatings(),
                Genres = request.GetGenres(),
                Studios = request.GetStudios(),
                StudioIds = request.GetStudioIds(),
                Person = request.Person,
                PersonIds = request.GetPersonIds(),
                PersonTypes = request.GetPersonTypes(),
                Years = request.GetYears(),
                ImageTypes = request.GetImageTypes().ToArray(),
                VideoTypes = request.GetVideoTypes().ToArray(),
                AdjacentTo = request.AdjacentTo,
                ItemIds = request.GetItemIds(),
                MinPlayers = request.MinPlayers,
                MaxPlayers = request.MaxPlayers,
                MinCommunityRating = request.MinCommunityRating,
                MinCriticRating = request.MinCriticRating,
                ParentIndexNumber = request.ParentIndexNumber,
                AiredDuringSeason = request.AiredDuringSeason,
                AlbumArtistStartsWithOrGreater = request.AlbumArtistStartsWithOrGreater
            };

            if (!string.IsNullOrWhiteSpace(request.Ids))
            {
                query.CollapseBoxSetItems = false;
            }

            foreach (var filter in request.GetFilters())
            {
                switch (filter)
                {
                    case ItemFilter.Dislikes:
                        query.IsLiked = false;
                        break;
                    case ItemFilter.IsFavorite:
                        query.IsFavorite = true;
                        break;
                    case ItemFilter.IsFavoriteOrLikes:
                        query.IsFavoriteOrLiked = true;
                        break;
                    case ItemFilter.IsFolder:
                        query.IsFolder = true;
                        break;
                    case ItemFilter.IsNotFolder:
                        query.IsFolder = false;
                        break;
                    case ItemFilter.IsPlayed:
                        query.IsPlayed = true;
                        break;
                    case ItemFilter.IsRecentlyAdded:
                        break;
                    case ItemFilter.IsResumable:
                        query.IsResumable = true;
                        break;
                    case ItemFilter.IsUnplayed:
                        query.IsPlayed = false;
                        break;
                    case ItemFilter.Likes:
                        query.IsLiked = true;
                        break;
                }
            }

            if (!string.IsNullOrEmpty(request.MinPremiereDate))
            {
                query.MinPremiereDate = DateTime.Parse(request.MinPremiereDate, null, DateTimeStyles.RoundtripKind).ToUniversalTime();
            }

            if (!string.IsNullOrEmpty(request.MaxPremiereDate))
            {
                query.MaxPremiereDate = DateTime.Parse(request.MaxPremiereDate, null, DateTimeStyles.RoundtripKind).ToUniversalTime();
            }

            // Filter by Series Status
            if (!string.IsNullOrEmpty(request.SeriesStatus))
            {
                query.SeriesStatuses = request.SeriesStatus.Split(',').Select(d => (SeriesStatus)Enum.Parse(typeof(SeriesStatus), d, true)).ToArray();
            }

            // Filter by Series AirDays
            if (!string.IsNullOrEmpty(request.AirDays))
            {
                query.AirDays = request.AirDays.Split(',').Select(d => (DayOfWeek)Enum.Parse(typeof(DayOfWeek), d, true)).ToArray();
            }

            // ExcludeLocationTypes
            if (!string.IsNullOrEmpty(request.ExcludeLocationTypes))
            {
                query.ExcludeLocationTypes = request.ExcludeLocationTypes.Split(',').Select(d => (LocationType)Enum.Parse(typeof(LocationType), d, true)).ToArray();
            }

            if (!string.IsNullOrEmpty(request.LocationTypes))
            {
                query.LocationTypes = request.LocationTypes.Split(',').Select(d => (LocationType)Enum.Parse(typeof(LocationType), d, true)).ToArray();
            }

            // Min official rating
            if (!string.IsNullOrWhiteSpace(request.MinOfficialRating))
            {
                query.MinParentalRating = _localization.GetRatingLevel(request.MinOfficialRating);
            }

            // Max official rating
            if (!string.IsNullOrWhiteSpace(request.MaxOfficialRating))
            {
                query.MaxParentalRating = _localization.GetRatingLevel(request.MaxOfficialRating);
            }

            // Artists
            if (!string.IsNullOrEmpty(request.ArtistIds))
            {
                var artistIds = request.ArtistIds.Split(new[] { '|', ',' });

                var artistItems = artistIds.Select(_libraryManager.GetItemById).Where(i => i != null).ToList();
                query.ArtistNames = artistItems.Select(i => i.Name).ToArray();
            }

            // Artists
            if (!string.IsNullOrEmpty(request.Artists))
            {
                query.ArtistNames = request.Artists.Split('|');
            }

            // Albums
            if (!string.IsNullOrEmpty(request.Albums))
            {
                query.AlbumNames = request.Albums.Split('|');
            }

            if (request.HasQueryLimit == false)
            {
                query.StartIndex = null;
                query.Limit = null;
            }

            return query;
        }

        /// <summary> Gets query result. </summary>
        /// <param name="request"> The request. </param>
        /// <returns> The query result. </returns>
        private async Task<QueryResult<BaseItem>> GetQueryResult(BaseReportRequest request)
        {
            // Placeholder in case needed later
            request.Recursive = true;
            var user = !string.IsNullOrWhiteSpace(request.UserId) ? _userManager.GetUserById(request.UserId) : null;
            request.Fields = "MediaSources,DateCreated,Settings,Studios,SyncInfo,ItemCounts";

            var parentItem = string.IsNullOrEmpty(request.ParentId) ?
                (user == null ? _libraryManager.RootFolder : user.RootFolder) :
                _libraryManager.GetItemById(request.ParentId);

            var item = string.IsNullOrEmpty(request.ParentId) ?
                user == null ? _libraryManager.RootFolder : user.RootFolder :
                parentItem;

            IEnumerable<BaseItem> items;

            if (request.Recursive)
            {
                var result = await ((Folder)item).GetItems(GetItemsQuery(request, user)).ConfigureAwait(false);
                return result;
            }
            else
            {
                if (user == null)
                {
                    var result = await ((Folder)item).GetItems(GetItemsQuery(request, null)).ConfigureAwait(false);
                    return result;
                }

                var userRoot = item as UserRootFolder;

                if (userRoot == null)
                {
                    var result = await ((Folder)item).GetItems(GetItemsQuery(request, user)).ConfigureAwait(false);

                    return result;
                }

                items = ((Folder)item).GetChildren(user, true);
            }

            return new QueryResult<BaseItem> { Items = items.ToArray() };

        }

        /// <summary> Gets report activities. </summary>
        /// <param name="request"> The request. </param>
        /// <returns> The report activities. </returns>
        private Task<ReportResult> GetReportActivities(IReportsDownload request)
        {
            return Task<ReportResult>.Run(() =>
            {
                DateTime? minDate = string.IsNullOrWhiteSpace(request.MinDate) ?
                (DateTime?)null :
                DateTime.Parse(request.MinDate, null, DateTimeStyles.RoundtripKind).ToUniversalTime();

                QueryResult<ActivityLogEntry> queryResult;
                 if (request.HasQueryLimit)   
                   queryResult = _repo.GetActivityLogEntries(minDate, request.StartIndex, request.Limit);
                 else
                     queryResult = _repo.GetActivityLogEntries(minDate, request.StartIndex, null);
                //var queryResult = _activityManager.GetActivityLogEntries(minDate, request.StartIndex, request.Limit);

                ReportActivitiesBuilder builder = new ReportActivitiesBuilder(_libraryManager, _userManager);
                var result = builder.GetResult(queryResult, request);
                result.TotalRecordCount = queryResult.TotalRecordCount;
                return result;

            });

        }

        /// <summary> Gets report result. </summary>
        /// <param name="request"> The request. </param>
        /// <returns> The report result. </returns>
        private async Task<ReportResult> GetReportResult(GetItemReport request)
        {
            ReportBuilder reportBuilder = new ReportBuilder(_libraryManager);
            QueryResult<BaseItem> queryResult = await GetQueryResult(request).ConfigureAwait(false);
            ReportResult reportResult = reportBuilder.GetResult(queryResult.Items, request);
            reportResult.TotalRecordCount = queryResult.TotalRecordCount;

            return reportResult;
        }

        /// <summary> Gets report statistic. </summary>
        /// <param name="request"> The request. </param>
        /// <returns> The report statistic. </returns>
        private async Task<ReportStatResult> GetReportStatistic(GetReportStatistics request)
        {
            ReportIncludeItemTypes reportRowType = ReportHelper.GetRowType(request.IncludeItemTypes);
            QueryResult<BaseItem> queryResult = await GetQueryResult(request).ConfigureAwait(false);

            ReportStatBuilder reportBuilder = new ReportStatBuilder(_libraryManager);
            ReportStatResult reportResult = reportBuilder.GetResult(queryResult.Items, ReportHelper.GetRowType(request.IncludeItemTypes), request.TopItems ?? 5);
            reportResult.TotalRecordCount = reportResult.Groups.Count();
            return reportResult;
        }

        #endregion

    }
}
