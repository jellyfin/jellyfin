//
// VideoTrack.cs:
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
	/// Enumeration describing supported Video Aspect Ratio types.
	/// </summary>
	public enum VideoAspectRatioType
	{
		/// <summary>
		/// Free Aspect Ratio.
		/// </summary>
		AspectRatioModeFree = 0x0,
		/// <summary>
		/// Keep Aspect Ratio.
		/// </summary>
		AspectRatioModeKeep = 0x1,
		/// <summary>
		/// Fixed Aspect Ratio.
		/// </summary>
		AspectRatioModeFixed = 0x2
	}

	/// <summary>
	/// Describes a Matroska Video Track.
	/// </summary>
	public class VideoTrack : Track, IVideoCodec
	{
		#region Private fields

#pragma warning disable 414 // Assigned, never used
		private ulong width;
		private ulong height;
		private ulong disp_width;
		private ulong disp_height;
		private double framerate;
		private bool interlaced;
		private VideoAspectRatioType ratio_type;
		private ByteVector fourcc;
#pragma warning restore 414

		private List<EBMLreader> unknown_elems = new List<EBMLreader> ();

		#endregion

		#region Constructors

		/// <summary>
		/// Constructs a <see cref="VideoTrack" /> parsing from provided
		/// file data.
		/// Parsing will be done reading from _file at position references by 
		/// parent element's data section.
		/// </summary>
		/// <param name="_file"><see cref="File" /> instance to read from.</param>
		/// <param name="element">Parent <see cref="EBMLreader" />.</param>
		public VideoTrack (File _file, EBMLreader element)
			: base (_file, element)
		{
			MatroskaID matroska_id;

			// Here we handle the unknown elements we know, and store the rest
			foreach (EBMLreader elem in base.UnknownElements) {
				matroska_id = (MatroskaID) elem.ID;


				switch (matroska_id)
				{
					case MatroskaID.TrackVideo:
						{
							ulong i = 0;

							while (i < elem.DataSize)
							{
								EBMLreader child = new EBMLreader(_file, elem.DataOffset + i);

								matroska_id = (MatroskaID)child.ID;

								switch (matroska_id)
								{
									case MatroskaID.VideoDisplayWidth:
										disp_width = child.ReadULong();
										break;
									case MatroskaID.VideoDisplayHeight:
										disp_height = child.ReadULong();
										break;
									case MatroskaID.VideoPixelWidth:
										width = child.ReadULong();
										break;
									case MatroskaID.VideoPixelHeight:
										height = child.ReadULong();
										break;
									case MatroskaID.VideoFrameRate:
										framerate = child.ReadDouble();
										break;
									case MatroskaID.VideoFlagInterlaced:
										interlaced = child.ReadBool();
										break;
									case MatroskaID.VideoAspectRatioType:
										ratio_type = (VideoAspectRatioType)child.ReadULong();
										break;
									case MatroskaID.VideoColourSpace:
										fourcc = child.ReadBytes();
										break;
									default:
										unknown_elems.Add(child);
										break;
								}

								i += child.Size;
							}
							break;
						}

					case MatroskaID.TrackDefaultDuration:
						ulong tmp = elem.ReadULong();
						framerate = 1000000000.0 / (double)tmp;
						break;

					default:
						unknown_elems.Add(elem);
						break;
				}
				
			}
		}

		#endregion

		#region Public fields

		/// <summary>
		/// List of unknown elements encountered while parsing.
		/// </summary>
		public new List<EBMLreader> UnknownElements
		{
			get { return unknown_elems; }
		}

		#endregion

		#region Public methods

		#endregion

		#region ICodec

		/// <summary>
		/// This type of track only has video media type.
		/// </summary>
		public override MediaTypes MediaTypes
		{
			get { return MediaTypes.Video; }
		}

		#endregion

		#region IVideoCodec

		/// <summary>
		/// Describes video track width in pixels.
		/// </summary>
		public int VideoWidth
		{
			get { return (int) width; }
		}

		/// <summary>
		/// Describes video track height in pixels.
		/// </summary>
		public int VideoHeight
		{
			get { return (int) height; }
		}

		#endregion

	}
}
