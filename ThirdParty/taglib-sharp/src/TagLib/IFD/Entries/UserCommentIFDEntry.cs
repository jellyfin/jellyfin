//
// UserCommentIFDEntry.cs:
//
// Author:
//   Ruben Vermeersch (ruben@savanne.be)
//   Mike Gemuende (mike@gemuende.de)
//
// Copyright (C) 2009 Ruben Vermeersch
// Copyright (C) 2009 Mike Gemuende
//
// This library is free software; you can redistribute it and/or modify
// it  under the terms of the GNU Lesser General Public License version
// 2.1 as published by the Free Software Foundation.
//
// This library is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307
// USA
//

using System;

namespace TagLib.IFD.Entries
{
	/// <summary>
	///    Contains an ASCII STRING value.
	/// </summary>
	public class UserCommentIFDEntry : IFDEntry
	{

#region Constant Values

		/// <summary>
		///   Marker for an ASCII-encoded UserComment tag.
		/// </summary>
		public static readonly ByteVector COMMENT_ASCII_CODE = new byte[] {0x41, 0x53, 0x43, 0x49, 0x49, 0x00, 0x00, 0x00};

		/// <summary>
		///   Marker for a JIS-encoded UserComment tag.
		/// </summary>
		public static readonly ByteVector COMMENT_JIS_CODE = new byte[] {0x4A, 0x49, 0x53, 0x00, 0x00, 0x00, 0x00, 0x00};

		/// <summary>
		///   Marker for a UNICODE-encoded UserComment tag.
		/// </summary>
		public static readonly ByteVector COMMENT_UNICODE_CODE = new byte[] {0x55, 0x4E, 0x49, 0x43, 0x4F, 0x44, 0x45, 0x00};

		/// <summary>
		///   Corrupt marker that seems to be resembling unicode.
		/// </summary>
		public static readonly ByteVector COMMENT_BAD_UNICODE_CODE = new byte[] {0x55, 0x6E, 0x69, 0x63, 0x6F, 0x64, 0x65, 0x00};

		/// <summary>
		///   Marker for a UserComment tag with undefined encoding.
		/// </summary>
		public static readonly ByteVector COMMENT_UNDEFINED_CODE = new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};

#endregion

#region Properties

		/// <value>
		///    The ID of the tag, the current instance belongs to
		/// </value>
		public ushort Tag { get; private set; }

		/// <value>
		///    The value which is stored by the current instance
		/// </value>
		public string Value { get; private set; }

#endregion

#region Constructors

		/// <summary>
		///    Construcor.
		/// </summary>
		/// <param name="tag">
		///    A <see cref="System.UInt16"/> with the tag ID of the entry this instance
		///    represents
		/// </param>
		/// <param name="value">
		///    A <see cref="string"/> to be stored
		/// </param>
		public UserCommentIFDEntry (ushort tag, string value)
		{
			Tag = tag;
			Value = value;
		}

		/// <summary>
		///    Construcor.
		/// </summary>
		/// <param name="tag">
		///    A <see cref="System.UInt16"/> with the tag ID of the entry this instance
		///    represents
		/// </param>
		/// <param name="data">
		///    A <see cref="ByteVector"/> to be stored
		/// </param>
		/// <param name="file">
		///    The file that's currently being parsed, used for reporting corruptions.
		/// </param>
		public UserCommentIFDEntry (ushort tag, ByteVector data, TagLib.File file)
		{
			Tag = tag;

			if (data.StartsWith (COMMENT_ASCII_CODE)) {
				Value = TrimNull (data.ToString (StringType.Latin1, COMMENT_ASCII_CODE.Count, data.Count - COMMENT_ASCII_CODE.Count));
				return;
			}

			if (data.StartsWith (COMMENT_UNICODE_CODE)) {
				Value = TrimNull (data.ToString (StringType.UTF8, COMMENT_UNICODE_CODE.Count, data.Count - COMMENT_UNICODE_CODE.Count));
				return;
			}

			var trimmed = data.ToString ().Trim ();
			if (trimmed.Length == 0 || trimmed == "\0") {
				Value = String.Empty;
				return;
			}

			// Some programs like e.g. CanonZoomBrowser inserts just the first 0x00-byte
			// followed by 7-bytes of trash.
			if (data.StartsWith ((byte) 0x00) && data.Count >= 8) {

				// And CanonZoomBrowser fills some trailing bytes of the comment field
				// with '\0'. So we return only the characters before the first '\0'.
				int term = data.Find ("\0", 8);
				if (term != -1) {
					Value = data.ToString (StringType.Latin1, 8, term - 8);
				} else {
					Value = data.ToString (StringType.Latin1, 8, data.Count - 8);
				}
				return;
			}

			if (data.Data.Length == 0) {
				Value = String.Empty;
				return;
			}

			// Try to parse anyway
			int offset = 0;
			int length = data.Count - offset;

			// Corruption that starts with a Unicode header and a count byte.
			if (data.StartsWith (COMMENT_BAD_UNICODE_CODE)) {
				offset = COMMENT_BAD_UNICODE_CODE.Count;
				length = data.Count - offset;
			}

			file.MarkAsCorrupt ("UserComment with other encoding than Latin1 or Unicode");
			Value = TrimNull (data.ToString (StringType.UTF8, offset, length));
		}

		private string TrimNull (string value)
		{
			int term = value.IndexOf ('\0');
			if (term > -1)
				value = value.Substring (0, term);
			return value;
		}

#endregion

#region Public Methods

		/// <summary>
		///    Renders the current instance to a <see cref="ByteVector"/>
		/// </summary>
		/// <param name="is_bigendian">
		///    A <see cref="System.Boolean"/> indicating the endianess for rendering.
		/// </param>
		/// <param name="offset">
		///    A <see cref="System.UInt32"/> with the offset, the data is stored.
		/// </param>
		/// <param name="type">
		///    A <see cref="System.UInt16"/> the ID of the type, which is rendered
		/// </param>
		/// <param name="count">
		///    A <see cref="System.UInt32"/> with the count of the values which are
		///    rendered.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector"/> with the rendered data.
		/// </returns>
		public ByteVector Render (bool is_bigendian, uint offset, out ushort type, out uint count)
		{
			type = (ushort) IFDEntryType.Undefined;

			ByteVector data = new ByteVector ();
			data.Add (COMMENT_UNICODE_CODE);
			data.Add (ByteVector.FromString (Value, StringType.UTF8));

			count = (uint) data.Count;

			return data;
		}

#endregion

	}
}
