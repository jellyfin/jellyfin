namespace MediaBrowser.Api.Reports
{
	/// <summary> A report header. </summary>
	public class ReportHeader
	{
		/// <summary> Initializes a new instance of the ReportHeader class. </summary>
		public ReportHeader()
		{
			ItemViewType = ItemViewType.None;
			Visible = true;
			CanGroup = true;
            ShowHeaderLabel = true;
            DisplayType = ReportDisplayType.ScreenExport;
		}

		/// <summary> Gets or sets the type of the header field. </summary>
		/// <value> The type of the header field. </value>
		public ReportFieldType HeaderFieldType { get; set; }

		/// <summary> Gets or sets the name of the header. </summary>
		/// <value> The name of the header. </value>
		public string Name { get; set; }

		/// <summary> Gets or sets the name of the field. </summary>
		/// <value> The name of the field. </value>
		public HeaderMetadata FieldName { get; set; }

		/// <summary> Gets or sets the sort field. </summary>
		/// <value> The sort field. </value>
		public string SortField { get; set; }

		/// <summary> Gets or sets the type. </summary>
		/// <value> The type. </value>
		public string Type { get; set; }

		/// <summary> Gets or sets the type of the item view. </summary>
		/// <value> The type of the item view. </value>
		public ItemViewType ItemViewType { get; set; }

		/// <summary> Gets or sets a value indicating whether this object is visible. </summary>
		/// <value> true if visible, false if not. </value>
		public bool Visible { get; set; }

        /// <summary> Gets or sets the type of the display. </summary>
        /// <value> The type of the display. </value>
        public ReportDisplayType DisplayType { get; set; }

        /// <summary> Gets or sets a value indicating whether the header label is shown. </summary>
        /// <value> true if show header label, false if not. </value>
        public bool ShowHeaderLabel { get; set; }

		/// <summary> Gets or sets a value indicating whether we can group. </summary>
		/// <value> true if we can group, false if not. </value>
		public bool CanGroup { get; set; }

	}
}
