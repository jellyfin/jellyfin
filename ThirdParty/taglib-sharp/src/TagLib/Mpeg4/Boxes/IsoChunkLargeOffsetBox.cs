//
// IsoChunkLargeOffsetBox.cs: Provides an implementation of a ISO/IEC 14496-12
// ChunkLargeOffsetBox.
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Copyright (C) 2006-2007 Brian Nickel
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

namespace TagLib.Mpeg4 {
	/// <summary>
	///    This class extends <see cref="FullBox" /> to provide an
	///    implementation of a ISO/IEC 14496-12 ChunkLargeOffsetBox.
	/// </summary>
	/// <remarks>
	///    <see cref="IsoChunkOffsetBox" /> and <see
	///    cref="IsoChunkLargeOffsetBox" /> contain offsets of media data
	///    within the file. As such, if the file changes by even one byte,
	///    these values are devalidatated and the box will have to be
	///    overwritten to maintain playability.
	/// </remarks>
	public class IsoChunkLargeOffsetBox : FullBox
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the chunk offsets.
		/// </summary>
		private ulong [] offsets;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="IsoChunkLargeOffsetBox" /> with a provided header
		///    and handler by reading the contents from a specified
		///    file.
		/// </summary>
		/// <param name="header">
		///    A <see cref="BoxHeader" /> object containing the header
		///    to use for the new instance.
		/// </param>
		/// <param name="file">
		///    A <see cref="TagLib.File" /> object to read the contents
		///    of the box from.
		/// </param>
		/// <param name="handler">
		///    A <see cref="IsoHandlerBox" /> object containing the
		///    handler that applies to the new instance.
		/// </param>
		public IsoChunkLargeOffsetBox (BoxHeader header,
		                               TagLib.File file,
		                               IsoHandlerBox handler)
			: base (header, file, handler)
		{
			ByteVector box_data = file.ReadBlock (DataSize);
			
			offsets = new ulong [(int)
				box_data.Mid (0, 4).ToUInt ()];
			
			for (int i = 0; i < offsets.Length; i ++)
				offsets [i] = box_data.Mid (4 + i * 8,
					8).ToULong ();
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		/// <summary>
		///    Gets and sets the data contained in the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="ByteVector" /> object containing the data
		///    contained in the current instance.
		/// </value>
		public override ByteVector Data {
			get {
				ByteVector output = ByteVector.FromUInt ((uint)
					offsets.Length);
				for (int i = 0; i < offsets.Length; i ++)
					output.Add (ByteVector.FromULong (
						offsets [i]));
				
				return output;
			}
		}

		/// <summary>
		///    Gets the offset table contained in the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:ulong[]" /> containing the offset table
		///    contained in the current instance.
		/// </value>
		public ulong [] Offsets {
			get {return offsets;}
		}
		
		#endregion
		
		
		
		#region Public Methods
		
		/// <summary>
		///    Overwrites the existing box in the file after updating
		///    the table for a size change.
		/// </summary>
		/// <param name="file">
		///    A <see cref="File" /> object containing the file to which
		///    the current instance belongs and wo which modifications
		///    must be applied.
		/// </param>
		/// <param name="sizeDifference">
		///    A <see cref="long" /> value containing the size
		///    change that occurred in the file.
		/// </param>
		/// <param name="after">
		///    A <see cref="long" /> value containing the position in
		///    the file after which offsets will be invalidated. If an
		///    offset is before this point, it won't be updated.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <see param="file" /> is <see langword="null" />.
		/// </exception>
		public void Overwrite (File file, long sizeDifference,
		                       long after)
		{
			if (file == null)
				throw new ArgumentNullException ("file");
			
			file.Insert (Render (sizeDifference, after),
				Header.Position, Size);
		}
  	
		/// <summary>
		///    Renders the current instance after updating the table for
		///    a size change.
		/// </summary>
		/// <param name="sizeDifference">
		///    A <see cref="long" /> value containing the size
		///    change that occurred in the file.
		/// </param>
		/// <param name="after">
		///    A <see cref="long" /> value containing the position in
		///    the file after which offsets will be invalidated. If an
		///    offset is before this point, it won't be updated.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector" /> object containing the
		///    rendered version of the file.
		/// </returns>
		public ByteVector Render (long sizeDifference, long after)
		{
			for (int i = 0; i < offsets.Length; i ++)
				if (offsets [i] >= (ulong) after)
					offsets [i] = (ulong)
						((long) offsets [i] +
							sizeDifference);
			
			return Render ();
		}
		
		#endregion
	}
}
