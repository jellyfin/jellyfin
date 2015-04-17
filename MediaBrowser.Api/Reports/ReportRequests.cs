using MediaBrowser.Api.UserLibrary;
using MediaBrowser.Controller.Net;
using ServiceStack;
using System.Collections.Generic;

namespace MediaBrowser.Api.Reports
{
	public class BaseReportRequest : GetItems
	{
		public bool HasQueryLimit { get; set; }
		public string GroupBy { get; set; }

		public string ReportColumns { get; set; }
	}

	[Route("/Reports/Items", "GET", Summary = "Gets reports based on library items")]
	public class GetItemReport : BaseReportRequest, IReturn<ReportResult>
	{

	}

	[Route("/Reports/Headers", "GET", Summary = "Gets reports headers based on library items")]
	public class GetReportHeaders : BaseReportRequest, IReturn<List<ReportHeader>>
	{
	}

	[Route("/Reports/Statistics", "GET", Summary = "Gets reports statistics based on library items")]
	public class GetReportStatistics : BaseReportRequest, IReturn<ReportStatResult>
	{
		public int? TopItems { get; set; }

	}

	[Route("/Reports/Items/Download", "GET", Summary = "Downloads report")]
	public class GetReportDownload : BaseReportRequest
	{
		public GetReportDownload()
		{
			ExportType = ReportExportType.CSV;
		}

		public ReportExportType ExportType { get; set; }
	}

}
