using System.Collections.Generic;

namespace MediaBrowser.Api.Reports
{
	/// <summary> A report stat group. </summary>
	public class ReportStatGroup
	{
		/// <summary>
		/// Initializes a new instance of the MediaBrowser.Api.Reports.ReportStatGroup class. </summary>
		public ReportStatGroup()
		{
			Items = new List<ReportStatItem>();
			TotalRecordCount = 0;
		}

		/// <summary> Gets or sets the header. </summary>
		/// <value> The header. </value>
		public string Header { get; set; }

		/// <summary> Gets or sets the items. </summary>
		/// <value> The items. </value>
		public List<ReportStatItem> Items { get; set; }

		/// <summary> Gets or sets the number of total records. </summary>
		/// <value> The total number of record count. </value>
		public int TotalRecordCount { get; set; }

		internal static string FormatedHeader(string header, int topItem)
		{
			return string.Format("Top {0} {1}", topItem, header);
		}
	}
}
