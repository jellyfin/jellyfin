using MediaBrowser.Api.UserLibrary;
using ServiceStack;
using System.Collections.Generic;

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

        /// <summary> Gets or sets a list of types of the displays. </summary>
        /// <value> A list of types of the displays. </value>
        string DisplayType { get; set; }

    }

    public class BaseReportRequest : BaseItemsRequest, IReportsQuery
    {
        /// <summary> Gets or sets the report view. </summary>
        /// <value> The report view. </value>
        [ApiMember(Name = "ReportView", Description = "The report view. Values (ReportData, ReportStatistics, ReportActivities)", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ReportView { get; set; }
        
        /// <summary> Gets or sets the report view. </summary>
        /// <value> The report view. </value>
        [ApiMember(Name = "DisplayType", Description = "The report display type. Values (None, Screen, Export, ScreenExport)", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string DisplayType { get; set; }

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

        /// <summary> Gets or sets the report view. </summary>
        /// <value> The report view. </value>
        [ApiMember(Name = "DisplayType", Description = "The report display type. Values (None, Screen, Export, ScreenExport)", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string DisplayType { get; set; }

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

        /// <summary> Gets or sets the report view. </summary>
        /// <value> The report view. </value>
        [ApiMember(Name = "DisplayType", Description = "The report display type. Values (None, Screen, Export, ScreenExport)", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string DisplayType { get; set; }

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
