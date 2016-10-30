using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rssdp
{
	/// <summary>
	/// Represents an icon published by an <see cref="SsdpDevice"/>.
	/// </summary>
	public sealed class SsdpDeviceIcon
	{
		/// <summary>
		/// The mime type for the image data returned by the <see cref="Url"/> property.
		/// </summary>
		/// <remarks>
		/// <para>Required. Icon's MIME type (cf. RFC 2045, 2046, and 2387). Single MIME image type. At least one icon should be of type “image/png” (Portable Network Graphics, see IETF RFC 2083).</para> 
		/// </remarks>
		/// <seealso cref="Url"/>
		public string MimeType { get; set; }

		/// <summary>
		/// The URL that can be called with an HTTP GET command to retrieve the image data.
		/// </summary>
		/// <remarks>
		/// <para>Required. May be relative to base URL. Specified by UPnP vendor. Single URL.</para>
		/// </remarks>
		/// <seealso cref="MimeType"/>
		public Uri Url { get; set; }
			
		/// <summary>
		/// The width of the image in pixels.
		/// </summary>
		/// <remarks><para>Required, must be greater than zero.</para></remarks>
		public int Width { get; set; }

		/// <summary>
		/// The height of the image in pixels.
		/// </summary>
		/// <remarks><para>Required, must be greater than zero.</para></remarks>
		public int Height { get; set; }

		/// <summary>
		/// The colour depth of the image.
		/// </summary>
		/// <remarks><para>Required, must be greater than zero.</para></remarks>
		public int ColorDepth { get; set; }

	}
}
