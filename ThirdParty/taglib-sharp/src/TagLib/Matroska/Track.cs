//
// Track.cs:
//
// Author:
//   Julien Moutte <julien@fluendo.com>
//
// Copyright (C) 2011 FLUENDO S.A.
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

using System.Collections.Generic;
using System;

namespace TagLib.Matroska
{
	/// <summary>
	/// Describes a Matroska Track.
	/// </summary>
	public class Track : ICodec, IUIDElement
	{
		#region Private fields

#pragma warning disable 414 // Assigned, never used
		private ulong track_number;
		private string track_codec_id;
		private string track_codec_name;
		private string track_name;
		private string track_language;
		private bool track_enabled;
		private bool track_default;
		private ByteVector codec_data;
#pragma warning restore 414

		private List<EBMLreader> unknown_elems = new List<EBMLreader> ();

		#endregion

		#region Constructors

		/// <summary>
		/// Constructs a <see cref="Track" /> parsing from provided 
		/// file data.
		/// Parsing will be done reading from _file at position references by 
		/// parent element's data section.
		/// </summary>
		/// <param name="_file"><see cref="File" /> instance to read from.</param>
		/// <param name="element">Parent <see cref="EBMLreader" />.</param>
		public Track (File _file, EBMLreader element)
		{
			ulong i = 0;

			while (i < element.DataSize) {
				EBMLreader child = new EBMLreader (_file, element.DataOffset + i);

				MatroskaID matroska_id = (MatroskaID) child.ID;

				switch (matroska_id) {
					case MatroskaID.TrackNumber:
						track_number = child.ReadULong ();
						break;
					case MatroskaID.TrackUID:
						_UID = child.ReadULong ();
						break;
					case MatroskaID.CodecID:
						track_codec_id = child.ReadString ();
						break;
					case MatroskaID.CodecName:
						track_codec_name = child.ReadString ();
						break;
					case MatroskaID.TrackName:
						track_name = child.ReadString ();
						break;
					case MatroskaID.TrackLanguage:
						track_language = child.ReadString ();
						break;
					case MatroskaID.TrackFlagEnabled:
						track_enabled = child.ReadBool ();
						break;
					case MatroskaID.TrackFlagDefault:
						track_default = child.ReadBool ();
						break;
					case MatroskaID.CodecPrivate:
						codec_data = child.ReadBytes ();
						break;
					default:
						unknown_elems.Add (child);
						break;
				}

				i += child.Size;
			}
		}

		#endregion

		#region Public fields

		/// <summary>
		/// List of unknown elements encountered while parsing.
		/// </summary>
		public List<EBMLreader> UnknownElements
		{
			get { return unknown_elems; }
		}

		#endregion

		#region Public methods

		#endregion

		#region ICodec

		/// <summary>
		/// Describes track duration.
		/// </summary>
		public virtual TimeSpan Duration
		{
			get { return TimeSpan.Zero; }
		}

		/// <summary>
		/// Describes track media types.
		/// </summary>
		public virtual MediaTypes MediaTypes
		{
			get { return MediaTypes.None; }
		}

		/// <summary>
		/// Track description.
		/// </summary>
		public virtual string Description
		{
			get { return String.Format ("{0} {1}", track_codec_name, track_language); }
		}

		#endregion

		#region IUIDElement Boilerplate

		/// <summary>
		/// Unique ID representing the element, as random as possible (setting zero will generate automatically a new one).
		/// </summary>
		public ulong UID
		{
			get { return _UID; }
			set { _UID = UIDElement.GenUID(value); }
		}
		private ulong _UID = UIDElement.GenUID();

		/// <summary>
		/// Get the Tag type the UID should be represented by, or 0 if undefined
		/// </summary>
		public MatroskaID UIDType { get { return MatroskaID.TagTrackUID; } }

		#endregion


	}
}
