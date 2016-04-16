using System.Collections.Generic;

namespace MediaBrowser.Api.Reports
{
	/// <summary> Encapsulates the result of a report stat. </summary>
	public class ReportStatResult 
	{
		/// <summary>
		/// Initializes a new instance of the MediaBrowser.Api.Reports.ReportStatResult class. </summary>
		public ReportStatResult()
		{
			Groups = new List<ReportStatGroup>();
			TotalRecordCount = 0;
		}

		/// <summary> Gets or sets the groups. </summary>
		/// <value> The groups. </value>
		public List<ReportStatGroup> Groups { get; set; }

		/// <summary> Gets or sets the number of total records. </summary>
		/// <value> The total number of record count. </value>
		public int TotalRecordCount { get; set; }	
	}
}
