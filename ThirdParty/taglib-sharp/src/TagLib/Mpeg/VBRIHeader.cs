//
// VBRIHeader.cs: Provides information about a variable bitrate MPEG audio
// stream encoded with the Fraunhofer Encoder.
//
// Author:
//   Helmut Wahrmann
//
// Original Source:
//   XingHeader.cs
//
// Copyright (C) 2007 Helmut Wahrmann
// Copyright (C) 2005-2007 Brian Nickel
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

using System.Collections;
using System;

namespace TagLib.Mpeg {
	/// <summary>
	///    This structure provides information about a variable bitrate MPEG
	///    audio stream encoded by the Fraunhofer Encoder.
	/// </summary>
	public struct VBRIHeader
	{
#region Private Fields
		
		/// <summary>
		///    Contains the frame count.
		/// </summary>
		private uint frames;
		
		/// <summary>
		///    Contains the stream size.
		/// </summary>
		private uint size;
		
		/// <summary>
		///    Indicates that a physical VBRI header is present.
		/// </summary>
		private bool present;
		
#endregion
		
		
		
#region Public Fields
		
		/// <summary>
		///    Contains te VBRI identifier.
		/// </summary>
		/// <value>
		///    "VBRI"
		/// </value>
		public static readonly ReadOnlyByteVector FileIdentifier = "VBRI";
		
		/// <summary>
		///    An empty and unset VBRI header.
		/// </summary>
		public static readonly VBRIHeader Unknown = new VBRIHeader (0, 0);
		
#endregion
		
		
		
#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="VBRIHeader" /> with a specified frame count and
		///    size.
		/// </summary>
		/// <param name="frame">
		///    A <see cref="uint" /> value specifying the frame count of
		///    the audio represented by the new instance.
		/// </param>
		/// <param name="size">
		///    A <see cref="uint" /> value specifying the stream size of
		///    the audio represented by the new instance.
		/// </param>
		private VBRIHeader (uint frame, uint size)
		{
			this.frames = frame;
			this.size = size;
			this.present = false;
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="VBRIHeader" /> by reading its raw contents.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the raw
		///    VBRI header.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="data" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="CorruptFileException">
		///    <paramref name="data" /> does not start with <see
		///    cref="FileIdentifier" />.
		/// </exception>
		public VBRIHeader (ByteVector data)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			
			// Check to see if a valid VBRI header is available.
			if (!data.StartsWith (FileIdentifier))
				throw new CorruptFileException (
					"Not a valid VBRI header");
			
			// Size starts at Position 10
			int position = 10;

			size = data.Mid(position, 4).ToUInt();
			position += 4;

			// The number of Frames are found at Posistion 14
			frames = data.Mid(position, 4).ToUInt();
			position += 4;
			
			present = true;
		}
		
#endregion
		
		
		
#region Public Properties
		
		/// <summary>
		///    Gets the total number of frames in the file, as indicated
		///    by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value containing the number of
		///    frames in the file, or <c>0</c> if not specified.
		/// </value>
		public uint TotalFrames {
			get {return frames;}
		}
		
		/// <summary>
		///    Gets the total size of the file, as indicated by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value containing the total size of
		///    the file, or <c>0</c> if not specified.
		/// </value>
		public uint TotalSize {
			get {return size;}
		}
		
		/// <summary>
		///    Gets whether or not a physical VBRI header is present in
		///    the file.
		/// </summary>
		/// <value>
		///    A <see cref="bool" /> value indicating whether or not the
		///    current instance represents a physical VBRI header.
		/// </value>
		public bool Present {
			get {return present;}
		}
		
#endregion
		
		
		
#region Public Static Methods
		
		/// <summary>
		///    Gets the offset at which a VBRI header would appear in an
		///    MPEG audio packet.
		///    Always 32 bytes after the end of the first MPEG Header.
		/// </summary>
		/// <returns>
		///    A <see cref="int" /> value indicating the offset in an
		///    MPEG audio packet at which the VBRI header would appear.
		/// </returns>
		public static int VBRIHeaderOffset ()
		{
			// A VBRI header always appears 32 bytes after the end
			// of the first MPEG Header. So it's position 36 (0x24).
	  		return 0x24;
		}
		
#endregion
	}
}
