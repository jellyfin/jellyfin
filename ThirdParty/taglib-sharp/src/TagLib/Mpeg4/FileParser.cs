//
// FileParser.cs: Provides methods for reading important information from an
// MPEG-4 file.
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
using System.Collections.Generic;

namespace TagLib.Mpeg4 {
	/// <summary>
	///    This class provides methods for reading important information
	///    from an MPEG-4 file.
	/// </summary>
	public class FileParser
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the file to read from.
		/// </summary>
		private TagLib.File file;
		
		/// <summary>
		///    Contains the first header found in the file.
		/// </summary>
		private BoxHeader first_header;
		
		/// <summary>
		///    Contains the ISO movie header box.
		/// </summary>
		private IsoMovieHeaderBox mvhd_box;
		
		/// <summary>
		///    Contains the ISO user data boxes.
		/// </summary>
		private List<IsoUserDataBox> udta_boxes = new List<IsoUserDataBox> ();
		
		/// <summary>
		///    Contains the box headers from the top of the file to the
		///    "moov" box.
		/// </summary>
		private BoxHeader [] moov_tree;
		
		/// <summary>
		///    Contains the box headers from the top of the file to the
		///    "udta" box.
		/// </summary>
		private BoxHeader [] udta_tree;
		
		/// <summary>
		///    Contains the "stco" boxes found in the file.
		/// </summary>
		private List<Box> stco_boxes = new List<Box> ();
		
		/// <summary>
		///    Contains the "stsd" boxes found in the file.
		/// </summary>
		private List<Box> stsd_boxes = new List<Box> ();
		
		/// <summary>
		///    Contains the position at which the "mdat" box starts.
		/// </summary>
		private long mdat_start = -1;
		
		/// <summary>
		///    Contains the position at which the "mdat" box ends.
		/// </summary>
		private long mdat_end = -1;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="FileParser" /> for a specified file.
		/// </summary>
		/// <param name="file">
		///    A <see cref="TagLib.File" /> object to perform operations
		///    on.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="file" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="CorruptFileException">
		///    <paramref name="file" /> does not start with a
		///    "<c>ftyp</c>" box.
		/// </exception>
		public FileParser (TagLib.File file)
		{
			if (file == null)
				throw new ArgumentNullException ("file");
			
			this.file = file;
			first_header = new BoxHeader (file, 0);
			
			if (first_header.BoxType != "ftyp")
				throw new CorruptFileException (
					"File does not start with 'ftyp' box.");
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		/// <summary>
		///    Gets the movie header box read by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="IsoMovieHeaderBox" /> object read by the
		///    current instance, or <see langword="null" /> if not found.
		/// </value>
		/// <remarks>
		///    This value will only be set by calling <see
		///    cref="ParseTagAndProperties()" />.
		/// </remarks>
		public IsoMovieHeaderBox MovieHeaderBox {
			get {return mvhd_box;}
		}
		
		/// <summary>
		///    Gets all user data boxes read by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="IsoUserDataBox" /> array read by the
		///    current instance.
		/// </value>
		/// <remarks>
		///    This value will only be set by calling <see
		///    cref="ParseTag()" /> and <see
		///    cref="ParseTagAndProperties()" />.
		/// </remarks>
		public IsoUserDataBox [] UserDataBoxes {
			get {return udta_boxes.ToArray ();}
		}
		
		/// <summary>
		/// Get the User Data Box
		/// </summary>
		public IsoUserDataBox UserDataBox {
			get {return UserDataBoxes.Length == 0 ? null : UserDataBoxes[0];}
		}

		/// <summary>
		///    Gets the audio sample entry read by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="IsoAudioSampleEntry" /> object read by the
		///    current instance, or <see langword="null" /> if not found.
		/// </value>
		/// <remarks>
		///    This value will only be set by calling <see
		///    cref="ParseTagAndProperties()" />.
		/// </remarks>
		public IsoAudioSampleEntry AudioSampleEntry {
			get {
				foreach (IsoSampleDescriptionBox box in stsd_boxes)
					foreach (Box sub in box.Children) {
						IsoAudioSampleEntry entry = sub
							as IsoAudioSampleEntry;
						
						if (entry != null)
							return entry;
					}
				return null;
			}
		}
		
		/// <summary>
		///    Gets the visual sample entry read by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="IsoVisualSampleEntry" /> object read by the
		///    current instance, or <see langword="null" /> if not found.
		/// </value>
		/// <remarks>
		///    This value will only be set by calling <see
		///    cref="ParseTagAndProperties()" />.
		/// </remarks>
		public IsoVisualSampleEntry VisualSampleEntry {
			get {
				foreach (IsoSampleDescriptionBox box in stsd_boxes)
					foreach (Box sub in box.Children) {
						IsoVisualSampleEntry entry = sub
							as IsoVisualSampleEntry;
						
						if (entry != null)
							return entry;
					}
				return null;
			}
		}
		
		/// <summary>
		///    Gets the box headers for the first "<c>moov</c>" box and
		///    all parent boxes up to the top of the file as read by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:BoxHeader[]" /> containing the headers for
		///    the first "<c>moov</c>" box and its parent boxes up to
		///    the top of the file, in the order they appear, or <see
		///    langword="null" /> if none is present.
		/// </value>
		/// <remarks>
		///    This value is useful for overwriting box headers, and is
		///    only be set by calling <see cref="ParseBoxHeaders()" />.
		/// </remarks>
		public BoxHeader [] MoovTree {
			get {return moov_tree;}
		}
		
		/// <summary>
		///    Gets the box headers for the first "<c>udta</c>" box and
		///    all parent boxes up to the top of the file as read by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:BoxHeader[]" /> containing the headers for
		///    the first "<c>udta</c>" box and its parent boxes up to
		///    the top of the file, in the order they appear, or <see
		///    langword="null" /> if none is present.
		/// </value>
		/// <remarks>
		///    This value is useful for overwriting box headers, and is
		///    only be set by calling <see cref="ParseBoxHeaders()" />.
		/// </remarks>
		public BoxHeader [] UdtaTree {
			get {return udta_tree;}
		}
		
		/// <summary>
		///    Gets all chunk offset boxes read by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:Box[]" /> containing all chunk offset boxes
		///    read by the current instance.
		/// </value>
		/// <remarks>
		///    These boxes contain offset information for media data in
		///    the current instance and can be devalidated by size
		///    change operations, in which case they need to be
		///    corrected. This value will only be set by calling <see
		///    cref="ParseChunkOffsets()" />.
		/// </remarks>
		public Box [] ChunkOffsetBoxes {
			get {return stco_boxes.ToArray ();}
		}
		
		/// <summary>
		///    Gets the position at which the "<c>mdat</c>" box starts.
		/// </summary>
		/// <value>
		///    A <see cref="long" /> value containing the seek position
		///    at which the "<c>mdat</c>" box starts.
		/// </value>
		/// <remarks>
		///    The "<c>mdat</c>" box contains the media data for the
		///    file and is used for estimating the invariant data
		///    portion of the file.
		/// </remarks>
		public long MdatStartPosition {
			get {return mdat_start;}
		}
		
		/// <summary>
		///    Gets the position at which the "<c>mdat</c>" box ends.
		/// </summary>
		/// <value>
		///    A <see cref="long" /> value containing the seek position
		///    at which the "<c>mdat</c>" box ends.
		/// </value>
		/// <remarks>
		///    The "<c>mdat</c>" box contains the media data for the
		///    file and is used for estimating the invariant data
		///    portion of the file.
		/// </remarks>
		public long MdatEndPosition {
			get {return mdat_end;}
		}
		
		#endregion
		
		
		
		#region Public Methods
		
		/// <summary>
		///    Parses the file referenced by the current instance,
		///    searching for box headers that will be useful in saving
		///    the file.
		/// </summary>
		public void ParseBoxHeaders ()
		{
			try {
				ResetFields ();
				ParseBoxHeaders (first_header.TotalBoxSize,
				                 file.Length, null);
			} catch (CorruptFileException e) {
				file.MarkAsCorrupt (e.Message);
			}
		}
		
		/// <summary>
		///    Parses the file referenced by the current instance,
		///    searching for tags.
		/// </summary>
		public void ParseTag ()
		{
			try {
				ResetFields ();
				ParseTag (first_header.TotalBoxSize, file.Length, null);
			} catch (CorruptFileException e) {
				file.MarkAsCorrupt (e.Message);
			}
		}
		
		/// <summary>
		///    Parses the file referenced by the current instance,
		///    searching for tags and properties.
		/// </summary>
		public void ParseTagAndProperties ()
		{
			try {
				ResetFields ();
				ParseTagAndProperties (first_header.TotalBoxSize,
				                       file.Length, null, null);
			} catch (CorruptFileException e) {
				file.MarkAsCorrupt (e.Message);
			}
		}
		
		/// <summary>
		///    Parses the file referenced by the current instance,
		///    searching for chunk offset boxes.
		/// </summary>
		public void ParseChunkOffsets ()
		{
			try {
				ResetFields ();
				ParseChunkOffsets (first_header.TotalBoxSize,
				                   file.Length);
			} catch (CorruptFileException e) {
				file.MarkAsCorrupt (e.Message);
			}
		}
		
		#endregion
		
		
		
		#region Private Methods
		
		/// <summary>
		///    Parses boxes for a specified range, looking for headers.
		/// </summary>
		/// <param name="start">
		///    A <see cref="long" /> value specifying the seek position
		///    at which to start reading.
		/// </param>
		/// <param name="end">
		///    A <see cref="long" /> value specifying the seek position
		///    at which to stop reading.
		/// </param>
		/// <param name="parents">
		///    A <see cref="T:System.Collections.Generic.List`1" /> object containing all the parent
		///    handlers that apply to the range.
		/// </param>
		private void ParseBoxHeaders (long start, long end,
		                              List<BoxHeader> parents)
		{
			BoxHeader header;
			
			for (long position = start; position < end;
				position += header.TotalBoxSize) {
				header = new BoxHeader (file, position);
				
				if (moov_tree == null &&
					header.BoxType == BoxType.Moov) {
					List<BoxHeader> new_parents = AddParent (
						parents, header);
					moov_tree = new_parents.ToArray ();
					ParseBoxHeaders (
						header.HeaderSize + position,
						header.TotalBoxSize + position,
						new_parents);
				} else if (header.BoxType == BoxType.Mdia ||
					header.BoxType == BoxType.Minf ||
					header.BoxType == BoxType.Stbl ||
					header.BoxType == BoxType.Trak) {
					ParseBoxHeaders (
						header.HeaderSize + position,
						header.TotalBoxSize + position,
						AddParent (parents, header));
				} else if (udta_tree == null &&
					header.BoxType == BoxType.Udta) {
					// For compatibility, we still store the tree to the first udta
					// block. The proper way to get this info is from the individual
					// IsoUserDataBox.ParentTree member.
					udta_tree = AddParent (parents,
						header).ToArray ();
				} else if (header.BoxType == BoxType.Mdat) {
					mdat_start = position;
					mdat_end = position + header.TotalBoxSize;
				}
				
				if (header.TotalBoxSize == 0)
					break;
			}
		}

		/// <summary>
		///    Parses boxes for a specified range, looking for tags.
		/// </summary>
		/// <param name="start">
		///    A <see cref="long" /> value specifying the seek position
		///    at which to start reading.
		/// </param>
		/// <param name="end">
		///    A <see cref="long" /> value specifying the seek position
		///    at which to stop reading.
		/// </param>
		/// <param name="parents">
		///    A <see cref="T:List" /> of <see cref="BoxHeader" /> parents.
		/// </param>
		private void ParseTag (long start, long end,
		                              List<BoxHeader> parents)
		{
			BoxHeader header;
			
			for (long position = start; position < end;
				position += header.TotalBoxSize) {
				header = new BoxHeader (file, position);
				
				if (header.BoxType == BoxType.Moov) {
					ParseTag (header.HeaderSize + position,
						header.TotalBoxSize + position,
						AddParent (parents, header));
				} else if (header.BoxType == BoxType.Mdia ||
					header.BoxType == BoxType.Minf ||
					header.BoxType == BoxType.Stbl ||
					header.BoxType == BoxType.Trak) {
					ParseTag (header.HeaderSize + position,
						header.TotalBoxSize + position,
						AddParent (parents, header));
				} else if (header.BoxType == BoxType.Udta) {
					IsoUserDataBox udtaBox = BoxFactory.CreateBox (file,
					header) as IsoUserDataBox;

					// Since we can have multiple udta boxes, save the parent for each one
					List<BoxHeader> new_parents = AddParent (
						parents, header);
					udtaBox.ParentTree = new_parents.ToArray ();

					udta_boxes.Add(udtaBox);
				} else if (header.BoxType == BoxType.Mdat) {
					mdat_start = position;
					mdat_end = position + header.TotalBoxSize;
				}
				
				if (header.TotalBoxSize == 0)
					break;
			}
		}

		/// <summary>
		///    Parses boxes for a specified range, looking for tags and
		///    properties.
		/// </summary>
		/// <param name="start">
		///    A <see cref="long" /> value specifying the seek position
		///    at which to start reading.
		/// </param>
		/// <param name="end">
		///    A <see cref="long" /> value specifying the seek position
		///    at which to stop reading.
		/// </param>
		/// <param name="handler">
		///    A <see cref="IsoHandlerBox" /> object that applied to the
		///    range being searched.
		/// </param>
		/// <param name="parents">
		///    A <see cref="T:List" /> of <see cref="BoxHeader" /> parents.
		/// </param>
		private void ParseTagAndProperties (long start, long end,
		                                    IsoHandlerBox handler, List<BoxHeader> parents)
		{
			BoxHeader header;
			
			for (long position = start; position < end;
				position += header.TotalBoxSize) {
				header = new BoxHeader (file, position);
				ByteVector type = header.BoxType;
				
				if (type == BoxType.Moov) {
					ParseTagAndProperties (header.HeaderSize + position,
						header.TotalBoxSize + position,
						handler,
						AddParent (parents, header));
				} else if (type == BoxType.Mdia ||
					type == BoxType.Minf ||
					type == BoxType.Stbl ||
					type == BoxType.Trak) {
					ParseTagAndProperties (
						header.HeaderSize + position,
						header.TotalBoxSize + position,
						handler,
						AddParent (parents, header));
				} else if (type == BoxType.Stsd) {
					stsd_boxes.Add (BoxFactory.CreateBox (
						file, header, handler));
				} else if (type == BoxType.Hdlr) {
					handler = BoxFactory.CreateBox (file,
						header, handler) as
							IsoHandlerBox;
				} else if (mvhd_box == null &&
					type == BoxType.Mvhd) {
					mvhd_box = BoxFactory.CreateBox (file,
						header, handler) as
							IsoMovieHeaderBox;
				} else if (type == BoxType.Udta) {
					IsoUserDataBox udtaBox = BoxFactory.CreateBox (file,
						header, handler) as
							IsoUserDataBox;

					// Since we can have multiple udta boxes, save the parent for each one
					List<BoxHeader> new_parents = AddParent (
						parents, header);
					udtaBox.ParentTree = new_parents.ToArray ();

					udta_boxes.Add(udtaBox);
				} else if (type == BoxType.Mdat) {
					mdat_start = position;
					mdat_end = position + header.TotalBoxSize;
				}
				
				if (header.TotalBoxSize == 0)
					break;
			}
		}
		
		/// <summary>
		///    Parses boxes for a specified range, looking for chunk
		///    offset boxes.
		/// </summary>
		/// <param name="start">
		///    A <see cref="long" /> value specifying the seek position
		///    at which to start reading.
		/// </param>
		/// <param name="end">
		///    A <see cref="long" /> value specifying the seek position
		///    at which to stop reading.
		/// </param>
		private void ParseChunkOffsets (long start, long end)
		{
			BoxHeader header;
			
			for (long position = start; position < end;
				position += header.TotalBoxSize) {
				header = new BoxHeader (file, position);
				
				if (header.BoxType == BoxType.Moov) {
					ParseChunkOffsets (
						header.HeaderSize + position,
						header.TotalBoxSize + position);
				} else if (header.BoxType == BoxType.Moov ||
					header.BoxType == BoxType.Mdia ||
					header.BoxType == BoxType.Minf ||
					header.BoxType == BoxType.Stbl ||
					header.BoxType == BoxType.Trak) {
					ParseChunkOffsets (
						header.HeaderSize + position,
						header.TotalBoxSize + position);
				} else if (header.BoxType == BoxType.Stco ||
					header.BoxType == BoxType.Co64) {
					stco_boxes.Add (BoxFactory.CreateBox (
						file, header));
				} else if (header.BoxType == BoxType.Mdat) {
					mdat_start = position;
					mdat_end = position + header.TotalBoxSize;
				}
				
				if (header.TotalBoxSize == 0)
					break;
			}
		}
		
		/// <summary>
		///    Resets all internal fields.
		/// </summary>
		private void ResetFields ()
		{
			mvhd_box = null;
			udta_boxes.Clear ();
			moov_tree = null;
			udta_tree = null;
			stco_boxes.Clear ();
			stsd_boxes.Clear ();
			mdat_start = -1;
			mdat_end = -1;
		}
		
		#endregion
		
		#region Private Static Methods
		
		/// <summary>
		///    Adds a parent to the end of an existing list of parents.
		/// </summary>
		/// <param name="parents">
		///    A <see cref="T:System.Collections.Generic.List`1" /> object containing an existing
		///    list of parents.
		/// </param>
		/// <param name="current">
		///    A <see cref="BoxHeader" /> object to add to the list.
		/// </param>
		/// <returns>
		///    A new <see cref="T:System.Collections.Generic.List`1" /> object containing the list
		///    of parents, including the added header.
		/// </returns>
		private static List<BoxHeader> AddParent (List<BoxHeader> parents,
		                                          BoxHeader current)
		{
			List<BoxHeader> boxes = new List<BoxHeader> ();
			if (parents != null)
				boxes.AddRange (parents);
			boxes.Add (current);
			return boxes;
		}
		
		#endregion
	}
}
