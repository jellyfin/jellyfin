using System;

namespace MediaBrowser.Api.Reports
{

	/// <summary> A report options. </summary>
	public class ReportOptions<I>
	{
		/// <summary> Initializes a new instance of the ReportOptions class. </summary>
		public ReportOptions()
		{
		}

		/// <summary> Initializes a new instance of the ReportOptions class. </summary>
		/// <param name="header"> . </param>
		/// <param name="row"> . </param>
		public ReportOptions(ReportHeader header, Func<I, ReportRow, object> column)
		{
			Header = header;
			Column = column;
		}

		/// <summary>
		/// Initializes a new instance of the ReportOptions class.
		/// </summary>
		/// <param name="header"></param>
		/// <param name="column"></param>
		/// <param name="itemID"></param>
		public ReportOptions(ReportHeader header, Func<I, ReportRow, object> column, Func<I, object> itemID)
		{
			Header = header;
			Column = column;
			ItemID = itemID;
		}

		/// <summary> Gets or sets the header. </summary>
		/// <value> The header. </value>
		public ReportHeader Header { get; set; }

		/// <summary> Gets or sets the column. </summary>
		/// <value> The column. </value>
		public Func<I, ReportRow, object> Column { get; set; }

		/// <summary> Gets or sets the identifier of the item. </summary>
		/// <value> The identifier of the item. </value>
		public Func<I, object> ItemID { get; set; }
	}
}
