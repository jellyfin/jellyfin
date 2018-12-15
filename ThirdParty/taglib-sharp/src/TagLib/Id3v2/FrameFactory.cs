//
// FrameFactory.cs:
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Original Source:
//   id3v2framefactory.cpp from TagLib
//
// Copyright (C) 2005-2007 Brian Nickel
// Copyright (C) 2002,2003 Scott Wheeler (Original Implementation)
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
 
namespace TagLib.Id3v2
{
	/// <summary>
	///    This static class performs the necessary operations to determine
	///    and create the correct child class of <see cref="Frame" /> for a
	///    given raw ID3v2 frame.
	/// </summary>
	/// <remarks>
	///    By default, <see cref="FrameFactory" /> will only load frames
	///    contained in the library. To add additional frames to the
	///    process, register a frame creator with <see
	///    cref="AddFrameCreator" />.
	/// </remarks>
	public static class FrameFactory
	{
		/// <summary>
		///    Creates a frame from a specified block of data, or
		///    returns <see langword="null" /> if unsupported.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing a raw ID3v2
		///    frame.
		/// </param>
		/// <param name="offset">
		///    A <see cref="int" /> value specifying the offset in
		///    <paramref name="data"/> at which the frame data begins.
		/// </param>
		/// <param name="header">
		///    A <see cref="FrameHeader" /> object for the frame
		///    contained in <paramref name="data" />.
		/// </param>
		/// <param name="version">
		///    A <see cref="byte" /> specifying the version of ID3v2 the
		///    raw frame data is stored in.
		/// </param>
		/// <returns>
		///     A <see cref="Frame" /> object if the method was able to
		///     match and create one. Otherwise <see langword="null" />.
		/// </returns>
		/// <remarks>
		///    <para>Frame creators are used to provide access or
		///    support for items that are left out of TagLib#.</para>
		/// </remarks>
		/// <example>
		///    <code lang="C#">
		/// public Frame Creator (TagLib.ByteVector data, TagLib.Id3v2.FrameHeader header)
		/// {
		/// 	if (header.FrameId == "RVRB")
		/// 		return new ReverbFrame (data, header);
		/// 	else
		/// 		return null;
		/// }
		/// ...
		/// TagLib.Id3v2.FrameFactor.AddFrameCreator (ReverbFrame.Creator);
		///   </code>
		/// </example>
		/// <seealso cref="AddFrameCreator" />
		public delegate Frame FrameCreator (ByteVector data, int offset,
		                                    FrameHeader header,
		                                    byte version);
		
		/// <summary>
		///    Contains registered frame creators.
		/// </summary>
		private static List<FrameCreator> frame_creators =
			new List<FrameCreator> ();

		/// <summary>
		///    Creates a <see cref="Frame" /> object by reading it from
		///    raw ID3v2 frame data.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing a raw ID3v2
		///    frame.
		/// </param>
		/// <param name="file">
		///    A <see cref="File"/> object containing
		///    abstraction of the file to read. 
		///    Ignored if <paramref name="data"/> is not null.
		/// </param>
		/// <param name="offset">
		///    A <see cref="int" /> value reference specifying at what
		///    index in <paramref name="file" />, or in 
		///    <paramref name="data" /> if not null,
		///    at which the frame begins. After reading, it contains 
		///    the offset of the next frame to be read.
		/// </param>
		/// <param name="version">
		///    A <see cref="byte" /> value specifying the ID3v2 version
		///    the frame in <paramref name="data"/> is encoded in.
		/// </param>
		/// <param name="alreadyUnsynched">
		///    A <see cref="bool" /> value specifying whether the entire
		///    tag has already been unsynchronized.
		/// </param>
		/// <returns>
		///    A <see cref="Frame" /> object read from the data, or <see
		///    langword="null" /> if none is found.
		/// </returns>
		/// <exception cref="System.NotImplementedException">
		///    The frame contained in the raw data could not be
		///    converted to ID3v2 or uses encryption or compression.
		/// </exception>
		public static Frame CreateFrame (ByteVector data, File file,
		                                 ref int offset, byte version, bool alreadyUnsynched)
		{
			int position = 0;

			if ( data == null)
			{
				file.Seek(offset);
				data = file.ReadBlock((int)FrameHeader.Size(version));
			}
			else
			{
				file = null;
				position = offset;
			}

			// If the next data is position is 0, assume
			// that we've hit the padding portion of the
			// frame data.

			if (data[position] == 0)
				return null;

			FrameHeader header = new FrameHeader (data.Mid (position,
				(int) FrameHeader.Size (version)), version);

			int fileposition = offset + (int)FrameHeader.Size(version);
			offset += (int) (header.FrameSize + FrameHeader.Size (
				version));
			
			if (header.FrameId == null)
				throw new System.NotImplementedException ();
			
			foreach (byte b in header.FrameId) {
				char c = (char) b;
					if ((c < 'A' || c > 'Z') &&
						(c < '0' || c > '9'))
						return null;
			}

			if (alreadyUnsynched) {
				// Mark the frame as not Unsynchronozed because the entire
				// tag has already been Unsynchronized
				header.Flags &= ~FrameFlags.Unsynchronisation;
			}
			
			// Windows Media Player may create zero byte frames.
			// Just send them off as unknown and delete them.
			if (header.FrameSize == 0) {
				header.Flags |= FrameFlags.TagAlterPreservation;
				return new UnknownFrame (data, position, header,
					version);
			}
			
			// TODO: Support Compression.
			if ((header.Flags & FrameFlags.Compression) != 0)
				throw new System.NotImplementedException ();
			
			// TODO: Support Encryption.
			if ((header.Flags & FrameFlags.Encryption) != 0)
				throw new System.NotImplementedException ();
			
			foreach (FrameCreator creator in frame_creators) {
				Frame frame = creator (data, position, header,
					version);
				
				if (frame != null)
					return frame;
			}

			// This is where things get necessarily nasty.  Here we
			// determine which Frame subclass (or if none is found
			// simply an Frame) based on the frame ID. Since there
			// are a lot of possibilities, that means a lot of if
			// blocks.


			// Lazy objects loading handling

			if (file != null) {

				// Attached Picture (frames 4.14)
				// General Encapsulated Object (frames 4.15)
				if (header.FrameId == FrameType.APIC || 
					header.FrameId == FrameType.GEOB )
					return new AttachmentFrame(file.FileAbstraction,
						fileposition, offset - fileposition, header, version);

				// Read remaining part of the frame for the non lazy Frames
				file.Seek(fileposition);
				data.Add( file.ReadBlock(offset - fileposition) );
			}


			// Text Identification (frames 4.2)
			if (header.FrameId == FrameType.TXXX)
				return new UserTextInformationFrame (data,
					position, header, version);
			
			if (header.FrameId [0] == (byte) 'T')
				return new TextInformationFrame (data, position,
					header, version);
			
			// Unique File Identifier (frames 4.1)
			if (header.FrameId == FrameType.UFID)
				return new UniqueFileIdentifierFrame (data,
					position, header, version);
			
			// Music CD Identifier (frames 4.5)
			if (header.FrameId == FrameType.MCDI)
				return new MusicCdIdentifierFrame (data,
					position, header, version);
			
			// Unsynchronized Lyrics (frames 4.8)
			if (header.FrameId == FrameType.USLT)
				return new UnsynchronisedLyricsFrame (data,
					position, header, version);
			
			// Synchronized Lyrics (frames 4.9)
			if (header.FrameId == FrameType.SYLT)
				return new SynchronisedLyricsFrame (data,
					position, header, version);
			
			// Comments (frames 4.10)
			if (header.FrameId == FrameType.COMM)
				return new CommentsFrame (data, position,
					header, version);
			
			// Relative Volume Adjustment (frames 4.11)
			if (header.FrameId == FrameType.RVA2)
				return new RelativeVolumeFrame (data, position,
					header, version);

			// Attached Picture (frames 4.14)
			// General Encapsulated Object (frames 4.15)
			if (header.FrameId == FrameType.APIC || 
				header.FrameId == FrameType.GEOB )
				return new AttachmentFrame(data, position,
					header, version);

			// Play Count (frames 4.16)
			if (header.FrameId == FrameType.PCNT)
				return new PlayCountFrame (data, position,
					header, version);
			
			// Play Count (frames 4.17)
			if(header.FrameId == FrameType.POPM)
				return new PopularimeterFrame (data, position,
					header, version);
			
			// Terms of Use (frames 4.22)
			if(header.FrameId == FrameType.USER)
				return new TermsOfUseFrame (data, position,
					header, version);
			
			// Private (frames 4.27)
			if (header.FrameId == FrameType.PRIV)
				return new PrivateFrame (data, position, header,
					version);
			
			// Url Link (frames 4.3.1)
			if (header.FrameId[0] == (byte)'W')
				return new UrlLinkFrame(data, position,
					header, version);

			// Event timing codes (frames 4.6)
			if (header.FrameId == FrameType.ETCO)
				return new EventTimeCodesFrame(data, position, header,
					version);

			return new UnknownFrame (data, position, header,
				version);
		}

		/// <summary>
		///    Adds a curstom frame creator to try before using standard
		///    frame creation methods.
		/// </summary>
		/// <param name="creator">
		///    A <see cref="FrameCreator" /> delegate to be used by the
		///    frame factory.
		/// </param>
		/// <remarks>
		///    Frame creators are used before standard methods so custom
		///    checking can be used and new formats can be added. They
		///    are executed in the reverse order in which they are
		///    added.
		/// </remarks>
		/// <exception cref="System.ArgumentNullException">
		///    <paramref name="creator" /> is <see langword="null" />.
		/// </exception>
		public static void AddFrameCreator (FrameCreator creator)
		{
			if (creator == null)
				throw new System.ArgumentNullException (
					"creator");
			
			frame_creators.Insert (0, creator);
		}
	}
}
