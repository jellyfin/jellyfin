using MediaBrowser.Api.UserLibrary;
using MediaBrowser.Model.Entities;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Api.Reports
{
    public interface IReportsDownload : IReportsQuery
    {
        /// <summary> Gets or sets the minimum date. </summary>
        /// <value> The minimum date. </value>
        string MinDate { get; set; }
    }

    /// <summary> Interface for reports query. </summary>
    public interface IReportsQuery : IReportsHeader
    {
        /// <summary>
        /// Gets or sets a value indicating whether this MediaBrowser.Api.Reports.GetActivityLogs has
        /// query limit. </summary>
        /// <value>
        /// true if this MediaBrowser.Api.Reports.GetActivityLogs has query limit, false if not. </value>
        bool HasQueryLimit { get; set; }
        /// <summary> Gets or sets who group this MediaBrowser.Api.Reports.GetActivityLogs. </summary>
        /// <value> Describes who group this MediaBrowser.Api.Reports.GetActivityLogs. </value>
        string GroupBy { get; set; }

        /// <summary>
        /// Skips over a given number of items within the results. Use for paging.
        /// </summary>
        /// <value>The start index.</value>
        int? StartIndex { get; set; }
        /// <summary>
        /// The maximum number of items to return
        /// </summary>
        /// <value>The limit.</value>
        int? Limit { get; set; }

    }
    public interface IReportsHeader
    {
        /// <summary> Gets or sets the report view. </summary>
        /// <value> The report view. </value>
        string ReportView { get; set; }

        /// <summary> Gets or sets the report columns. </summary>
        /// <value> The report columns. </value>
        string ReportColumns { get; set; }

        /// <summary> Gets or sets a list of types of the include items. </summary>
        /// <value> A list of types of the include items. </value>
        string IncludeItemTypes { get; set; }

    }

    public class BaseReportRequest : BaseItemsRequest, IReportsQuery
    {
        /// <summary> Gets or sets the report view. </summary>
        /// <value> The report view. </value>
        [ApiMember(Name = "ReportView", Description = "The report view. Values (ReportData, ReportStatistics, ReportActivities)", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ReportView { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this MediaBrowser.Api.Reports.BaseReportRequest has
        /// query limit. </summary>
        /// <value>
        /// true if this MediaBrowser.Api.Reports.BaseReportRequest has query limit, false if not. </value>
        [ApiMember(Name = "HasQueryLimit", Description = "Optional. If specified, results will include all records.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool HasQueryLimit { get; set; }

        /// <summary>
        /// Gets or sets who group this MediaBrowser.Api.Reports.BaseReportRequest. </summary>
        /// <value> Describes who group this MediaBrowser.Api.Reports.BaseReportRequest. </value>
        [ApiMember(Name = "GroupBy", Description = "Optional. If specified, results will include grouped records.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string GroupBy { get; set; }

        /// <summary> Gets or sets the report columns. </summary>
        /// <value> The report columns. </value>
        [ApiMember(Name = "ReportColumns", Description = "Optional. The columns to show.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ReportColumns { get; set; }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = false, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UserId { get; set; }

        /// <summary>
        /// Limit results to items containing a specific person
        /// </summary>
        /// <value>The person.</value>
        [ApiMember(Name = "Person", Description = "Optional. If specified, results will be filtered to include only those containing the specified person.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Person { get; set; }

        [ApiMember(Name = "PersonIds", Description = "Optional. If specified, results will be filtered to include only those containing the specified person.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string PersonIds { get; set; }

        /// <summary>
        /// If the Person filter is used, this can also be used to restrict to a specific person type
        /// </summary>
        /// <value>The type of the person.</value>
        [ApiMember(Name = "PersonTypes", Description = "Optional. If specified, along with Person, results will be filtered to include only those containing the specified person and PersonType. Allows multiple, comma-delimited", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string PersonTypes { get; set; }

        /// <summary>
        /// Limit results to items containing specific studios
        /// </summary>
        /// <value>The studios.</value>
        [ApiMember(Name = "Studios", Description = "Optional. If specified, results will be filtered based on studio. This allows multiple, pipe delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Studios { get; set; }

        [ApiMember(Name = "StudioIds", Description = "Optional. If specified, results will be filtered based on studio. This allows multiple, pipe delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string StudioIds { get; set; }

        /// <summary>
        /// Gets or sets the studios.
        /// </summary>
        /// <value>The studios.</value>
        [ApiMember(Name = "Artists", Description = "Optional. If specified, results will be filtered based on artist. This allows multiple, pipe delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Artists { get; set; }

        [ApiMember(Name = "ArtistIds", Description = "Optional. If specified, results will be filtered based on artist. This allows multiple, pipe delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string ArtistIds { get; set; }

        [ApiMember(Name = "Albums", Description = "Optional. If specified, results will be filtered based on album. This allows multiple, pipe delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Albums { get; set; }

        /// <summary>
        /// Gets or sets the item ids.
        /// </summary>
        /// <value>The item ids.</value>
        [ApiMember(Name = "Ids", Description = "Optional. If specific items are needed, specify a list of item id's to retrieve. This allows multiple, comma delimited.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Ids { get; set; }
        
        /// <summary>
        /// Gets or sets the video types.
        /// </summary>
        /// <value>The video types.</value>
        [ApiMember(Name = "VideoTypes", Description = "Optional filter by VideoType (videofile, dvd, bluray, iso). Allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string VideoTypes { get; set; }

        /// <summary>
        /// Gets or sets the video formats.
        /// </summary>
        /// <value>The video formats.</value>
        [ApiMember(Name = "Is3D", Description = "Optional filter by items that are 3D, or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? Is3D { get; set; }

        /// <summary>
        /// Gets or sets the series status.
        /// </summary>
        /// <value>The series status.</value>
        [ApiMember(Name = "SeriesStatus", Description = "Optional filter by Series Status. Allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string SeriesStatus { get; set; }

        [ApiMember(Name = "NameStartsWithOrGreater", Description = "Optional filter by items whose name is sorted equally or greater than a given input string.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string NameStartsWithOrGreater { get; set; }

        [ApiMember(Name = "NameStartsWith", Description = "Optional filter by items whose name is sorted equally than a given input string.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string NameStartsWith { get; set; }

        [ApiMember(Name = "NameLessThan", Description = "Optional filter by items whose name is equally or lesser than a given input string.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string NameLessThan { get; set; }

        [ApiMember(Name = "AlbumArtistStartsWithOrGreater", Description = "Optional filter by items whose album artist is sorted equally or greater than a given input string.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string AlbumArtistStartsWithOrGreater { get; set; }

        /// <summary>
        /// Gets or sets the air days.
        /// </summary>
        /// <value>The air days.</value>
        [ApiMember(Name = "AirDays", Description = "Optional filter by Series Air Days. Allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string AirDays { get; set; }

        /// <summary>
        /// Gets or sets the min offical rating.
        /// </summary>
        /// <value>The min offical rating.</value>
        [ApiMember(Name = "MinOfficialRating", Description = "Optional filter by minimum official rating (PG, PG-13, TV-MA, etc).", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string MinOfficialRating { get; set; }

        /// <summary>
        /// Gets or sets the max offical rating.
        /// </summary>
        /// <value>The max offical rating.</value>
        [ApiMember(Name = "MaxOfficialRating", Description = "Optional filter by maximum official rating (PG, PG-13, TV-MA, etc).", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string MaxOfficialRating { get; set; }

        [ApiMember(Name = "HasThemeSong", Description = "Optional filter by items with theme songs.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public bool? HasThemeSong { get; set; }

        [ApiMember(Name = "HasThemeVideo", Description = "Optional filter by items with theme videos.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public bool? HasThemeVideo { get; set; }

        [ApiMember(Name = "HasSubtitles", Description = "Optional filter by items with subtitles.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public bool? HasSubtitles { get; set; }

        [ApiMember(Name = "HasSpecialFeature", Description = "Optional filter by items with special features.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public bool? HasSpecialFeature { get; set; }

        [ApiMember(Name = "HasTrailer", Description = "Optional filter by items with trailers.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public bool? HasTrailer { get; set; }

        [ApiMember(Name = "AdjacentTo", Description = "Optional. Return items that are siblings of a supplied item.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string AdjacentTo { get; set; }

        [ApiMember(Name = "MinIndexNumber", Description = "Optional filter by minimum index number.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? MinIndexNumber { get; set; }

        [ApiMember(Name = "MinPlayers", Description = "Optional filter by minimum number of game players.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? MinPlayers { get; set; }

        [ApiMember(Name = "MaxPlayers", Description = "Optional filter by maximum number of game players.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? MaxPlayers { get; set; }

        [ApiMember(Name = "ParentIndexNumber", Description = "Optional filter by parent index number.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? ParentIndexNumber { get; set; }

        [ApiMember(Name = "HasParentalRating", Description = "Optional filter by items that have or do not have a parental rating", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? HasParentalRating { get; set; }

        [ApiMember(Name = "IsHD", Description = "Optional filter by items that are HD or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsHD { get; set; }

        [ApiMember(Name = "LocationTypes", Description = "Optional. If specified, results will be filtered based on LocationType. This allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string LocationTypes { get; set; }

        [ApiMember(Name = "ExcludeLocationTypes", Description = "Optional. If specified, results will be filtered based on LocationType. This allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string ExcludeLocationTypes { get; set; }

        [ApiMember(Name = "IsMissing", Description = "Optional filter by items that are missing episodes or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsMissing { get; set; }

        [ApiMember(Name = "IsUnaired", Description = "Optional filter by items that are unaired episodes or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsUnaired { get; set; }

        [ApiMember(Name = "IsVirtualUnaired", Description = "Optional filter by items that are virtual unaired episodes or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsVirtualUnaired { get; set; }

        [ApiMember(Name = "MinCommunityRating", Description = "Optional filter by minimum community rating.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public double? MinCommunityRating { get; set; }

        [ApiMember(Name = "MinCriticRating", Description = "Optional filter by minimum critic rating.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public double? MinCriticRating { get; set; }

        [ApiMember(Name = "AiredDuringSeason", Description = "Gets all episodes that aired during a season, including specials.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? AiredDuringSeason { get; set; }

        [ApiMember(Name = "MinPremiereDate", Description = "Optional. The minimum premiere date. Format = ISO", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string MinPremiereDate { get; set; }

        [ApiMember(Name = "MaxPremiereDate", Description = "Optional. The maximum premiere date. Format = ISO", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string MaxPremiereDate { get; set; }

        [ApiMember(Name = "HasOverview", Description = "Optional filter by items that have an overview or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? HasOverview { get; set; }

        [ApiMember(Name = "HasImdbId", Description = "Optional filter by items that have an imdb id or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? HasImdbId { get; set; }

        [ApiMember(Name = "HasTmdbId", Description = "Optional filter by items that have a tmdb id or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? HasTmdbId { get; set; }

        [ApiMember(Name = "HasTvdbId", Description = "Optional filter by items that have a tvdb id or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? HasTvdbId { get; set; }

        [ApiMember(Name = "IsYearMismatched", Description = "Optional filter by items that are potentially misidentified.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsYearMismatched { get; set; }

        [ApiMember(Name = "IsInBoxSet", Description = "Optional filter by items that are in boxsets, or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsInBoxSet { get; set; }

        [ApiMember(Name = "IsLocked", Description = "Optional filter by items that are locked.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public bool? IsLocked { get; set; }

        [ApiMember(Name = "IsUnidentified", Description = "Optional filter by items that are unidentified by internet metadata providers.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public bool? IsUnidentified { get; set; }

        [ApiMember(Name = "IsPlaceHolder", Description = "Optional filter by items that are placeholders", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public bool? IsPlaceHolder { get; set; }

        [ApiMember(Name = "HasOfficialRating", Description = "Optional filter by items that have official ratings", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public bool? HasOfficialRating { get; set; }

        [ApiMember(Name = "CollapseBoxSetItems", Description = "Whether or not to hide items behind their boxsets.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? CollapseBoxSetItems { get; set; }

        public string[] GetStudios()
        {
            return (Studios ?? string.Empty).Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public string[] GetStudioIds()
        {
            return (StudioIds ?? string.Empty).Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public string[] GetPersonTypes()
        {
            return (PersonTypes ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public string[] GetPersonIds()
        {
            return (PersonIds ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public VideoType[] GetVideoTypes()
        {
            var val = VideoTypes;

            if (string.IsNullOrEmpty(val))
            {
                return new VideoType[] { };
            }

            return val.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(v => (VideoType)Enum.Parse(typeof(VideoType), v, true)).ToArray();
        }
    }

	[Route("/Reports/Items", "GET", Summary = "Gets reports based on library items")]
	public class GetItemReport : BaseReportRequest, IReturn<ReportResult>
	{

	}

	[Route("/Reports/Headers", "GET", Summary = "Gets reports headers based on library items")]
    public class GetReportHeaders : IReturn<List<ReportHeader>>, IReportsHeader
    {
        /// <summary> Gets or sets the report view. </summary>
        /// <value> The report view. </value>
        [ApiMember(Name = "ReportView", Description = "The report view. Values (ReportData, ReportStatistics, ReportActivities)", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ReportView { get; set; }

        /// <summary> Gets or sets a list of types of the include items. </summary>
        /// <value> A list of types of the include items. </value>
        [ApiMember(Name = "IncludeItemTypes", Description = "Optional. If specified, results will be filtered based on item type. This allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string IncludeItemTypes { get; set; }

        /// <summary> Gets or sets the report columns. </summary>
        /// <value> The report columns. </value>
        [ApiMember(Name = "ReportColumns", Description = "Optional. The columns to show.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ReportColumns { get; set; }
	}

	[Route("/Reports/Statistics", "GET", Summary = "Gets reports statistics based on library items")]
	public class GetReportStatistics : BaseReportRequest, IReturn<ReportStatResult>
	{
		public int? TopItems { get; set; }

	}

	[Route("/Reports/Items/Download", "GET", Summary = "Downloads report")]
    public class GetReportDownload : BaseReportRequest, IReportsDownload
	{
		public GetReportDownload()
		{
			ExportType = ReportExportType.CSV;
		}

		public ReportExportType ExportType { get; set; }

        /// <summary> Gets or sets the minimum date. </summary>
        /// <value> The minimum date. </value>
        [ApiMember(Name = "MinDate", Description = "Optional. The minimum date. Format = ISO", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string MinDate { get; set; }

	}


    [Route("/Reports/Activities", "GET", Summary = "Gets activities entries")]
    public class GetActivityLogs : IReturn<ReportResult>, IReportsQuery, IReportsDownload
    {
        /// <summary> Gets or sets the report view. </summary>
        /// <value> The report view. </value>
        [ApiMember(Name = "ReportView", Description = "The report view. Values (ReportData, ReportStatistics, ReportActivities)", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ReportView { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this MediaBrowser.Api.Reports.GetActivityLogs has
        /// query limit. </summary>
        /// <value>
        /// true if this MediaBrowser.Api.Reports.GetActivityLogs has query limit, false if not. </value>
        [ApiMember(Name = "HasQueryLimit", Description = "Optional. If specified, results will include all records.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool HasQueryLimit { get; set; }

        /// <summary> Gets or sets who group this MediaBrowser.Api.Reports.GetActivityLogs. </summary>
        /// <value> Describes who group this MediaBrowser.Api.Reports.GetActivityLogs. </value>
        [ApiMember(Name = "GroupBy", Description = "Optional. If specified, results will include grouped records.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string GroupBy { get; set; }

        /// <summary> Gets or sets the report columns. </summary>
        /// <value> The report columns. </value>
        [ApiMember(Name = "ReportColumns", Description = "Optional. The columns to show.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ReportColumns { get; set; }

        /// <summary>
        /// Skips over a given number of items within the results. Use for paging.
        /// </summary>
        /// <value>The start index.</value>
        [ApiMember(Name = "StartIndex", Description = "Optional. The record index to start at. All items with a lower index will be dropped from the results.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? StartIndex { get; set; }

        /// <summary>
        /// The maximum number of items to return
        /// </summary>
        /// <value>The limit.</value>
        [ApiMember(Name = "Limit", Description = "Optional. The maximum number of records to return", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? Limit { get; set; }

        /// <summary> Gets or sets the minimum date. </summary>
        /// <value> The minimum date. </value>
        [ApiMember(Name = "MinDate", Description = "Optional. The minimum date. Format = ISO", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string MinDate { get; set; }

        [ApiMember(Name = "IncludeItemTypes", Description = "Optional. If specified, results will be filtered based on item type. This allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string IncludeItemTypes { get; set; }
    }
}
