using System.Collections.Generic;

namespace MediaBrowser.Api.Reports
{

	/// <summary> A report group. </summary>
	public class ReportGroup
	{
		/// <summary>
		/// Initializes a new instance of the MediaBrowser.Api.Reports.ReportGroup class. </summary>
		public ReportGroup()
		{
			Rows = new List<ReportRow>();
		}

		/// <summary>
		/// Initializes a new instance of the MediaBrowser.Api.Reports.ReportGroup class. </summary>
		/// <param name="rows"> The rows. </param>
		public ReportGroup(List<ReportRow> rows)
		{
			Rows = rows;
		}

		/// <summary> Gets or sets the name. </summary>
		/// <value> The name. </value>
		public string Name { get; set; }

		/// <summary> Gets or sets the rows. </summary>
		/// <value> The rows. </value>
		public List<ReportRow> Rows { get; set; }

		/// <summary> Returns a string that represents the current object. </summary>
		/// <returns> A string that represents the current object. </returns>
		/// <seealso cref="M:System.Object.ToString()"/>
		public override string ToString()
		{
			return Name;
		}
	}
}
