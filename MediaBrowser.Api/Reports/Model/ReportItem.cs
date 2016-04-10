namespace MediaBrowser.Api.Reports
{
	/// <summary> A report item. </summary>
	public class ReportItem
	{
		/// <summary> Gets or sets the identifier. </summary>
		/// <value> The identifier. </value>
		public string Id { get; set; }

		/// <summary> Gets or sets the name. </summary>
		/// <value> The name. </value>
		public string Name { get; set; }

		public string Image { get; set; }

		/// <summary> Gets or sets the custom tag. </summary>
		/// <value> The custom tag. </value>
		public string CustomTag { get; set; }

		/// <summary> Returns a string that represents the current object. </summary>
		/// <returns> A string that represents the current object. </returns>
		/// <seealso cref="M:System.Object.ToString()"/>
		public override string ToString()
		{
			return Name;
		}
	}
}
