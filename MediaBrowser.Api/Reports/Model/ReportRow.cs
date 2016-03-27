using System.Collections.Generic;

namespace MediaBrowser.Api.Reports
{
	public class ReportRow
	{
		/// <summary>
		/// Initializes a new instance of the ReportRow class.
		/// </summary>
		public ReportRow()
		{
			Columns = new List<ReportItem>();
		}

		/// <summary> Gets or sets the identifier. </summary>
		/// <value> The identifier. </value>
		public string Id { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this object has backdrop image. </summary>
		/// <value> true if this object has backdrop image, false if not. </value>
		public bool HasImageTagsBackdrop { get; set; }

		/// <summary> Gets or sets a value indicating whether this object has image tags. </summary>
		/// <value> true if this object has image tags, false if not. </value>
		public bool HasImageTagsPrimary { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this object has image tags logo. </summary>
		/// <value> true if this object has image tags logo, false if not. </value>
		public bool HasImageTagsLogo { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this object has local trailer. </summary>
		/// <value> true if this object has local trailer, false if not. </value>
		public bool HasLocalTrailer { get; set; }

		/// <summary> Gets or sets a value indicating whether this object has lock data. </summary>
		/// <value> true if this object has lock data, false if not. </value>
		public bool HasLockData { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this object has embedded image. </summary>
		/// <value> true if this object has embedded image, false if not. </value>
		public bool HasEmbeddedImage { get; set; }

		/// <summary> Gets or sets a value indicating whether this object has subtitles. </summary>
		/// <value> true if this object has subtitles, false if not. </value>
		public bool HasSubtitles { get; set; }

		/// <summary> Gets or sets a value indicating whether this object has specials. </summary>
		/// <value> true if this object has specials, false if not. </value>
		public bool HasSpecials { get; set; }

		/// <summary> Gets or sets the columns. </summary>
		/// <value> The columns. </value>
		public List<ReportItem> Columns { get; set; }

		/// <summary> Gets or sets the type. </summary>
		/// <value> The type. </value>
		public ReportIncludeItemTypes RowType { get; set; }

        /// <summary> Gets or sets the identifier of the user. </summary>
        /// <value> The identifier of the user. </value>
        public string UserId { get; set; }
	}
}
