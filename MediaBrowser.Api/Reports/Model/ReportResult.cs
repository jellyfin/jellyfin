using System.Collections.Generic;

namespace MediaBrowser.Api.Reports
{

	/// <summary> Encapsulates the result of a report. </summary>
	public class ReportResult
	{
		/// <summary>
		/// Initializes a new instance of the MediaBrowser.Api.Reports.ReportResult class. </summary>
		public ReportResult()
		{
			Rows = new List<ReportRow>();
			Headers = new List<ReportHeader>();
			Groups = new List<ReportGroup>();
			TotalRecordCount = 0;
			IsGrouped = false;
		}

		/// <summary>
		/// Initializes a new instance of the MediaBrowser.Api.Reports.ReportResult class. </summary>
		/// <param name="headers"> The headers. </param>
		/// <param name="rows"> The rows. </param>
		public ReportResult(List<ReportHeader> headers, List<ReportRow> rows)
		{
			Rows = rows;
			Headers = headers;
			TotalRecordCount = 0;
		}

		/// <summary> Gets or sets the rows. </summary>
		/// <value> The rows. </value>
		public List<ReportRow> Rows { get; set; }

		/// <summary> Gets or sets the headers. </summary>
		/// <value> The headers. </value>
		public List<ReportHeader> Headers { get; set; }

		/// <summary> Gets or sets the groups. </summary>
		/// <value> The groups. </value>
		public List<ReportGroup> Groups { get; set; }


		/// <summary> Gets or sets the number of total records. </summary>
		/// <value> The total number of record count. </value>
		public int TotalRecordCount { get; set; }

		/// <summary> Gets or sets the is grouped. </summary>
		/// <value> The is grouped. </value>
		public bool IsGrouped { get; set; }

	}
}
