//
// BoxFactory.cs: Provides support for reading boxes from a file.
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
	///    This static class provides support for reading boxes from a file.
	/// </summary>
	public static class BoxFactory
	{
		/// <summary>
		///    Creates a box by reading it from a file given its header,
		///    parent header, handler, and index in its parent.
		/// </summary>
		/// <param name="file">
		///    A <see cref="TagLib.File" /> object containing the file
		///    to read from.
		/// </param>
		/// <param name="header">
		///    A <see cref="BoxHeader" /> object containing the header
		///    of the box to create.
		/// </param>
		/// <param name="parent">
		///    A <see cref="BoxHeader" /> object containing the header
		///    of the parent box.
		/// </param>
		/// <param name="handler">
		///    A <see cref="IsoHandlerBox" /> object containing the
		///    handler that applies to the new box.
		/// </param>
		/// <param name="index">
		///    A <see cref="int" /> value containing the index of the
		///    new box in its parent.
		/// </param>
		/// <returns>
		///    A newly created <see cref="Box" /> object.
		/// </returns>
		private static Box CreateBox (TagLib.File file,
		                              BoxHeader header,
		                              BoxHeader parent,
		                              IsoHandlerBox handler,
		                              int index)
		{
			// The first few children of an "stsd" are sample
			// entries.
			if (parent.BoxType == BoxType.Stsd &&
				parent.Box is IsoSampleDescriptionBox &&
				index < (parent.Box as IsoSampleDescriptionBox).EntryCount) {
				if (handler != null && handler.HandlerType == BoxType.Soun)
					return new IsoAudioSampleEntry (header, file, handler);
				else if (handler != null && handler.HandlerType == BoxType.Vide)
					return new IsoVisualSampleEntry (header, file, handler);
				else if (handler != null && handler.HandlerType == BoxType.Alis) {
					if (header.BoxType == BoxType.Text)
						return new TextBox (header, file, handler);
					else if (header.BoxType == BoxType.Url)
						return new UrlBox (header, file, handler);
					// This could be anything, so just parse it
					return new UnknownBox (header, file, handler);
				} else
					return new IsoSampleEntry (header,
						file, handler);
			}
			
			// Standard items...
			ByteVector type = header.BoxType;
			
			if (type == BoxType.Mvhd)
				return new IsoMovieHeaderBox (header, file,
					handler);
			else if (type == BoxType.Stbl)
				return new IsoSampleTableBox (header, file,
					handler);
			else if (type == BoxType.Stsd)
				return new IsoSampleDescriptionBox (header,
					file, handler);
			else if (type == BoxType.Stco)
				return new IsoChunkOffsetBox (header, file,
					handler);
			else if (type == BoxType.Co64)
				return new IsoChunkLargeOffsetBox (header, file,
					handler);
			else if (type == BoxType.Hdlr)
				return new IsoHandlerBox (header, file,
					handler);
			else if (type == BoxType.Udta)
				return new IsoUserDataBox (header, file,
					handler);
			else if (type == BoxType.Meta)
				return new IsoMetaBox (header, file, handler);
			else if (type == BoxType.Ilst)
				return new AppleItemListBox (header, file,
					handler);
			else if (type == BoxType.Data)
				return new AppleDataBox (header, file, handler);
			else if (type == BoxType.Esds)
				return new AppleElementaryStreamDescriptor (
					header, file, handler);
			else if (type == BoxType.Free || type == BoxType.Skip)
				return new IsoFreeSpaceBox (header, file,
					handler);
			else if (type == BoxType.Mean || type == BoxType.Name)
				return new AppleAdditionalInfoBox (header, file,
					handler);
			
			// If we still don't have a tag, and we're inside an
			// ItemListBox, load the box as an AnnotationBox
			// (Apple tag item).
			if (parent.BoxType == BoxType.Ilst)
				return new AppleAnnotationBox (header, file,
					handler);
			
			// Nothing good. Go generic.
			return new UnknownBox (header, file, handler);
		}
		
		/// <summary>
		///    Creates a box by reading it from a file given its
		///    position in the file, parent header, handler, and index
		///    in its parent.
		/// </summary>
		/// <param name="file">
		///    A <see cref="TagLib.File" /> object containing the file
		///    to read from.
		/// </param>
		/// <param name="position">
		///    A <see cref="long" /> value specifying at what seek
		///    position in <paramref name="file" /> to start reading.
		/// </param>
		/// <param name="parent">
		///    A <see cref="BoxHeader" /> object containing the header
		///    of the parent box.
		/// </param>
		/// <param name="handler">
		///    A <see cref="IsoHandlerBox" /> object containing the
		///    handler that applies to the new box.
		/// </param>
		/// <param name="index">
		///    A <see cref="int" /> value containing the index of the
		///    new box in its parent.
		/// </param>
		/// <returns>
		///    A newly created <see cref="Box" /> object.
		/// </returns>
		internal static Box CreateBox (TagLib.File file, long position,
		                               BoxHeader parent,
		                               IsoHandlerBox handler, int index)
		{
			BoxHeader header = new BoxHeader (file, position);
			return CreateBox (file, header, parent, handler, index);
		}
		
		/// <summary>
		///    Creates a box by reading it from a file given its
		///    position in the file and handler.
		/// </summary>
		/// <param name="file">
		///    A <see cref="TagLib.File" /> object containing the file
		///    to read from.
		/// </param>
		/// <param name="position">
		///    A <see cref="long" /> value specifying at what seek
		///    position in <paramref name="file" /> to start reading.
		/// </param>
		/// <param name="handler">
		///    A <see cref="IsoHandlerBox" /> object containing the
		///    handler that applies to the new box.
		/// </param>
		/// <returns>
		///    A newly created <see cref="Box" /> object.
		/// </returns>
		public static Box CreateBox (TagLib.File file, long position,
		                             IsoHandlerBox handler)
		{
			return CreateBox (file, position, BoxHeader.Empty,
				handler, -1);
		}
		
		/// <summary>
		///    Creates a box by reading it from a file given its
		///    position in the file.
		/// </summary>
		/// <param name="file">
		///    A <see cref="TagLib.File" /> object containing the file
		///    to read from.
		/// </param>
		/// <param name="position">
		///    A <see cref="long" /> value specifying at what seek
		///    position in <paramref name="file" /> to start reading.
		/// </param>
		/// <returns>
		///    A newly created <see cref="Box" /> object.
		/// </returns>
		public static Box CreateBox (TagLib.File file, long position)
		{
			return CreateBox (file, position, null);
		}
		
		/// <summary>
		///    Creates a box by reading it from a file given its header
		///    and handler.
		/// </summary>
		/// <param name="file">
		///    A <see cref="TagLib.File" /> object containing the file
		///    to read from.
		/// </param>
		/// <param name="header">
		///    A <see cref="BoxHeader" /> object containing the header
		///    of the box to create.
		/// </param>
		/// <param name="handler">
		///    A <see cref="IsoHandlerBox" /> object containing the
		///    handler that applies to the new box.
		/// </param>
		/// <returns>
		///    A newly created <see cref="Box" /> object.
		/// </returns>
		public static Box CreateBox (TagLib.File file, BoxHeader header,
		                             IsoHandlerBox handler)
		{
			return CreateBox (file, header, BoxHeader.Empty,
				handler, -1);
		}
		
		/// <summary>
		///    Creates a box by reading it from a file given its header
		///    and handler.
		/// </summary>
		/// <param name="file">
		///    A <see cref="TagLib.File" /> object containing the file
		///    to read from.
		/// </param>
		/// <param name="header">
		///    A <see cref="BoxHeader" /> object containing the header
		///    of the box to create.
		/// </param>
		/// <returns>
		///    A newly created <see cref="Box" /> object.
		/// </returns>
		public static Box CreateBox (TagLib.File file, BoxHeader header)
		{
			return CreateBox (file, header, null);
		}
	}
}
