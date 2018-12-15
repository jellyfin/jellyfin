//
// File.cs: Provides tagging and properties support for MPEG-1, MPEG-2, and
// MPEG-2.5 audio files.
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Copyright (C) 2007 Brian Nickel
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

namespace TagLib.Mpeg {
	/// <summary>
	///    Indicates the type of marker found in a MPEG file.
	/// </summary>
	public enum Marker {
		/// <summary>
		///    An invalid marker.
		/// </summary>
		Corrupt = -1,
		
		/// <summary>
		///    A zero value marker.
		/// </summary>
		Zero = 0,
		
		/// <summary>
		///   A marker indicating a system sync packet.
		/// </summary>
		SystemSyncPacket = 0xBA,
		
		/// <summary>
		///   A marker indicating a video sync packet.
		/// </summary>
		VideoSyncPacket = 0xB3,
		
		/// <summary>
		///   A marker indicating a system packet.
		/// </summary>
		SystemPacket = 0xBB,
		
		/// <summary>
		///   A marker indicating a padding packet.
		/// </summary>
		PaddingPacket = 0xBE,
		
		/// <summary>
		///   A marker indicating a audio packet.
		/// </summary>
		AudioPacket = 0xC0,
		
		/// <summary>
		///   A marker indicating a video packet.
		/// </summary>
		VideoPacket = 0xE0,
		
		/// <summary>
		///   A marker indicating the end of a stream.
		/// </summary>
		EndOfStream = 0xB9
	}
	
	/// <summary>
	///    This class extends <see cref="TagLib.NonContainer.File" /> to
	///    provide tagging and properties support for MPEG-1, MPEG-2, and
	///    MPEG-2.5 video files.
	/// </summary>
	/// <remarks>
	///    A <see cref="TagLib.Id3v1.Tag" /> and <see
	///    cref="TagLib.Id3v2.Tag" /> will be added automatically to any
	///    file that doesn't contain one. This change does not effect the
	///    file until it is saved and can be reversed using the following
	///    method:
	///    <code>file.RemoveTags (file.TagTypes &amp; ~file.TagTypesOnDisk);</code>
	/// </remarks>
	[SupportedMimeType("taglib/mpg", "mpg")]
	[SupportedMimeType("taglib/mpeg", "mpeg")]
	[SupportedMimeType("taglib/mpe", "mpe")]
	[SupportedMimeType("taglib/mpv2", "mpv2")]
	[SupportedMimeType("taglib/m2v", "m2v")]
	[SupportedMimeType("video/x-mpg")]
	[SupportedMimeType("video/mpeg")]
	public class File : TagLib.NonContainer.File
	{
		#region Private Static Fields
		
		private static readonly ByteVector MarkerStart =
			new byte [] {0, 0, 1};
		
		#endregion
		
		
		
		#region Private Fields
		
		/// <summary>
		///    Contains the MPEG version.
		/// </summary>
		private Version version;
		
		/// <summary>
		///    Contains the first audio header.
		/// </summary>
		private AudioHeader audio_header;
		
		/// <summary>
		///    Contains the first video header.
		/// </summary>
		private VideoHeader video_header;
		
		/// <summary>
		///    Indicates whether or not audio was found.
		/// </summary>
		private bool video_found = false;
		
		/// <summary>
		///    Indicates whether or not video was found.
		/// </summary>
		private bool audio_found = false;
		
		/// <summary>
		///    Contains the start time of the file.
		/// </summary>
		private double? start_time = null;
		
		/// <summary>
		///    Contains the end time of the file.
		/// </summary>
		private double end_time;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="File" /> for a specified path in the local file
		///    system and specified read style.
		/// </summary>
		/// <param name="path">
		///    A <see cref="string" /> object containing the path of the
		///    file to use in the new instance.
		/// </param>
		/// <param name="propertiesStyle">
		///    A <see cref="ReadStyle" /> value specifying at what level
		///    of accuracy to read the media properties, or <see
		///    cref="ReadStyle.None" /> to ignore the properties.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="path" /> is <see langword="null" />.
		/// </exception>
		public File (string path, ReadStyle propertiesStyle)
			: base (path, propertiesStyle)
		{
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="File" /> for a specified path in the local file
		///    system with an average read style.
		/// </summary>
		/// <param name="path">
		///    A <see cref="string" /> object containing the path of the
		///    file to use in the new instance.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="path" /> is <see langword="null" />.
		/// </exception>
		public File (string path) : base (path)
		{
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="File" /> for a specified file abstraction and
		///    specified read style.
		/// </summary>
		/// <param name="abstraction">
		///    A <see cref="TagLib.File.IFileAbstraction" /> object to use when
		///    reading from and writing to the file.
		/// </param>
		/// <param name="propertiesStyle">
		///    A <see cref="ReadStyle" /> value specifying at what level
		///    of accuracy to read the media properties, or <see
		///    cref="ReadStyle.None" /> to ignore the properties.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="abstraction" /> is <see langword="null"
		///    />.
		/// </exception>
		public File (File.IFileAbstraction abstraction,
		             ReadStyle propertiesStyle)
			: base (abstraction, propertiesStyle)
		{
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="File" /> for a specified file abstraction with an
		///    average read style.
		/// </summary>
		/// <param name="abstraction">
		///    A <see cref="TagLib.File.IFileAbstraction" /> object to use when
		///    reading from and writing to the file.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="abstraction" /> is <see langword="null"
		///    />.
		/// </exception>
		public File (File.IFileAbstraction abstraction)
			: base (abstraction)
		{
		}
		
		#endregion
		
		
		
		#region Public Methods
		
		/// <summary>
		///    Gets a tag of a specified type from the current instance,
		///    optionally creating a new tag if possible.
		/// </summary>
		/// <param name="type">
		///    A <see cref="TagLib.TagTypes" /> value indicating the
		///    type of tag to read.
		/// </param>
		/// <param name="create">
		///    A <see cref="bool" /> value specifying whether or not to
		///    try and create the tag if one is not found.
		/// </param>
		/// <returns>
		///    A <see cref="Tag" /> object containing the tag that was
		///    found in or added to the current instance. If no
		///    matching tag was found and none was created, <see
		///    langword="null" /> is returned.
		/// </returns>
		/// <remarks>
		///    If a <see cref="TagLib.Id3v2.Tag" /> is added to the
		///    current instance, it will be placed at the start of the
		///    file. On the other hand, <see cref="TagLib.Id3v1.Tag" />
		///    <see cref="TagLib.Ape.Tag" /> will be added to the end of
		///    the file. All other tag types will be ignored.
		/// </remarks>
		public override TagLib.Tag GetTag (TagTypes type, bool create)
		{
			Tag t = (Tag as TagLib.NonContainer.Tag).GetTag (type);
			
			if (t != null || !create)
				return t;
			
			switch (type)
			{
			case TagTypes.Id3v1:
				return EndTag.AddTag (type, Tag);
			
			case TagTypes.Id3v2:
				return EndTag.AddTag (type, Tag);
			
			case TagTypes.Ape:
				return EndTag.AddTag (type, Tag);
			
			default:
				return null;
			}
		}
		
		#endregion
		
		
		
		#region Protected Methods
		
		/// <summary>
		///    Reads format specific information at the start of the
		///    file.
		/// </summary>
		/// <param name="start">
		///    A <see cref="long" /> value containing the seek position
		///    at which the tags end and the media data begins.
		/// </param>
		/// <param name="propertiesStyle">
		///    A <see cref="ReadStyle" /> value specifying at what level
		///    of accuracy to read the media properties, or <see
		///    cref="ReadStyle.None" /> to ignore the properties.
		/// </param>
		protected override void ReadStart (long start,
		                                   ReadStyle propertiesStyle)
		{
			if ((propertiesStyle & ReadStyle.Average) == 0)
				return;
			
			FindMarker (ref start, Marker.SystemSyncPacket);
			ReadSystemFile (start);
		}
		
		/// <summary>
		///    Reads format specific information at the end of the
		///    file.
		/// </summary>
		/// <param name="end">
		///    A <see cref="long" /> value containing the seek position
		///    at which the media data ends and the tags begin.
		/// </param>
		/// <param name="propertiesStyle">
		///    A <see cref="ReadStyle" /> value specifying at what level
		///    of accuracy to read the media properties, or <see
		///    cref="ReadStyle.None" /> to ignore the properties.
		/// </param>
		protected override void ReadEnd (long end,
		                                 ReadStyle propertiesStyle)
		{
			// Make sure we have ID3v1 and ID3v2 tags.
			GetTag (TagTypes.Id3v1, true);
			GetTag (TagTypes.Id3v2, true);
			
			if ((propertiesStyle & ReadStyle.Average) == 0 ||
				start_time == null)
				return;

			// Enable to search the marker in the entire file if none is found so far
			if (end == Length)
				end = 0;

			RFindMarker (ref end, Marker.SystemSyncPacket);
			
			end_time = ReadTimestamp (end + 4);
		}
		
		/// <summary>
		///    Reads the audio properties from the file represented by
		///    the current instance.
		/// </summary>
		/// <param name="start">
		///    A <see cref="long" /> value containing the seek position
		///    at which the tags end and the media data begins.
		/// </param>
		/// <param name="end">
		///    A <see cref="long" /> value containing the seek position
		///    at which the media data ends and the tags begin.
		/// </param>
		/// <param name="propertiesStyle">
		///    A <see cref="ReadStyle" /> value specifying at what level
		///    of accuracy to read the media properties, or <see
		///    cref="ReadStyle.None" /> to ignore the properties.
		/// </param>
		/// <returns>
		///    A <see cref="TagLib.Properties" /> object describing the
		///    media properties of the file represented by the current
		///    instance.
		/// </returns>
		protected override Properties ReadProperties (long start,
		                                              long end,
		                                              ReadStyle propertiesStyle)
		{
			TimeSpan duration = start_time == null ?
				TimeSpan.Zero : TimeSpan.FromSeconds (
					end_time - (double) start_time);
			
			return new Properties (duration, video_header,
				audio_header);
		}
		
		/// <summary>
		///    Gets the marker at a specified position.
		/// </summary>
		/// <param name="position">
		///    A <see cref="long" /> value specifying the postion in the
		///    file represented by the current instance at which to
		///    read.
		/// </param>
		/// <returns>
		///    A <see cref="Marker" /> value containing the type of
		///    marker found at the specified position.
		/// </returns>
		/// <exception cref="CorruptFileException">
		///    A valid marker does not exist at the specified position.
		/// </exception>
		protected Marker GetMarker (long position)
		{
			Seek (position);
			ByteVector identifier = ReadBlock (4);
			
			if (identifier.Count == 4 && identifier.StartsWith (
				MarkerStart))
				return (Marker) identifier [3];
			
			throw new CorruptFileException (
				"Invalid marker at position " + position);
		}
		
		/// <summary>
		///    Finds the next marker starting at a specified position.
		/// </summary>
		/// <param name="position">
		///    A <see cref="long" /> value reference specifying the
		///    position at which to start searching. This value
		///    is updated to the position of the found marker.
		/// </param>
		/// <returns>
		///    A <see cref="Marker" /> value containing the type of
		///    marker found at the specified position.
		/// </returns>
		/// <exception cref="CorruptFileException">
		///    A valid marker could not be found.
		/// </exception>
		protected Marker FindMarker (ref long position)
		{
			position = Find (MarkerStart, position);
			if (position < 0)
				throw new CorruptFileException (
					"Marker not found");
			
			return GetMarker (position);
		}
		
		/// <summary>
		///    Finds the next marker of a specified type, starting at a
		///    specified position.
		/// </summary>
		/// <param name="position">
		///    A <see cref="long" /> value reference specifying the
		///    position at which to start searching. This value
		///    is updated to the position of the found marker.
		/// </param>
		/// <param name="marker">
		///    A <see cref="Marker" /> value specifying the type of
		///    marker to search for.
		/// </param>
		/// <exception cref="CorruptFileException">
		///    A valid marker could not be found.
		/// </exception>
		protected void FindMarker (ref long position, Marker marker)
		{
			ByteVector packet = new ByteVector (MarkerStart);
			packet.Add ((byte) marker);
			position = Find (packet, position);
			
			if (position < 0)
				throw new CorruptFileException (
					"Marker not found");
		}

		/// <summary>
		///    Finds the previous marker of a specified type, starting
		///    at a specified position.
		/// </summary>
		/// <param name="position">
		///    A <see cref="long" /> value reference specifying the
		///    position at which to start searching. This value
		///    is updated to the position of the found marker.
		/// </param>
		/// <param name="marker">
		///    A <see cref="Marker" /> value specifying the type of
		///    marker to search for.
		/// </param>
		/// <exception cref="CorruptFileException">
		///    A valid marker could not be found.
		/// </exception>
		protected void RFindMarker (ref long position, Marker marker)
		{
			ByteVector packet = new ByteVector (MarkerStart);
			packet.Add ((byte) marker);
			position = RFind (packet, position);
			
			if (position < 0)
				throw new CorruptFileException (
					"Marker not found");
		}
		
		/// <summary>
		///    Reads the contents of the file as a system file, starting
		///    at a specified position.
		/// </summary>
		/// <param name="position">
		///    A <see cref="long" /> value specifying the postion in the
		///    file represented by the current instance at which to
		///    start reading.
		/// </param>
		/// <remarks>
		///    This method will stop when it has read both an audio and
		///    a video header, or once it's read 100 packets. This is to
		///    prevent the entire file from being read if it lacks one
		///    type of stream.
		/// </remarks>
		protected void ReadSystemFile (long position)
		{
			int sanity_limit = 100;
			
			for (int i = 0; i < sanity_limit && (start_time == null ||
				!audio_found || !video_found); i ++) {
				
				Marker marker = FindMarker (ref position);
				
				switch (marker)
				{
				case Marker.SystemSyncPacket:
					ReadSystemSyncPacket (ref position);
					break;
				
				case Marker.SystemPacket:
				case Marker.PaddingPacket:
					Seek (position + 4);
					position += ReadBlock (2).ToUShort () +
						6;
					break;
				
				case Marker.VideoPacket:
					ReadVideoPacket (ref position);
					break;
				
				case Marker.AudioPacket:
					ReadAudioPacket (ref position);
					break;
				
				case Marker.EndOfStream:
					return;
				
				default:
					position += 4;
					break;
				}
			}
		}
		
		#endregion
		
		
		
		#region Private Methods
		
		/// <summary>
		///    Reads an audio packet, assigning the audio header and
		///    advancing the position to the next packet position.
		/// </summary>
		/// <param name="position">
		///    A <see cref="long" /> value reference specifying the
		///    position at which to start reading the packet. This value
		///    is updated to the position of the next packet.
		/// </param>
		void ReadAudioPacket (ref long position)
		{
			Seek (position + 4);
			int length = ReadBlock (2).ToUShort ();
			
			if (!audio_found)
				audio_found = AudioHeader.Find (
					out audio_header, this, position + 15,
					length - 9);
			position += length;
		}
		
		/// <summary>
		///    Reads a video packet, assigning the video header and
		///    advancing the position to the next packet position.
		/// </summary>
		/// <param name="position">
		///    A <see cref="long" /> value reference specifying the
		///    position at which to start reading the packet. This value
		///    is updated to the position of the next packet.
		/// </param>
		void ReadVideoPacket (ref long position)
		{
			Seek (position + 4);
			int length = ReadBlock (2).ToUShort ();
			long offset = position + 6;
			
			while (!video_found && offset < position + length)
				if (FindMarker (ref offset) ==
					Marker.VideoSyncPacket) {
					video_header = new VideoHeader (this,
						offset + 4);
					video_found = true;
				} else {
					// advance the offset by 6 bytes, so the next iteration of the
					// loop won't find the same marker and get stuck.  6 bytes because findMarker is a
					// generic find that could get both PES packets and Stream packets, the smallest
					// posible pes packet with a size =0 would be 6 bytes.
					offset += 6;
				}
			
			position += length;
		}
		
		/// <summary>
		///    Reads a system sync packet, filling in version
		///    information and the first timestamp value, advancing the
		///    position to the next packet position.
		/// </summary>
		/// <param name="position">
		///    A <see cref="long" /> value reference specifying the
		///    position at which to start reading the packet. If the
		///    method is called without exception, this is updated to
		///    the position of the next packet.
		/// </param>
		/// <exception cref="UnsupportedFormatException">
		///    The MPEG version contained in the packet is unknown.
		/// </exception>
		void ReadSystemSyncPacket (ref long position)
		{
			int packet_size = 0;
			Seek (position + 4);
			byte version_info = ReadBlock (1) [0];
			
			if ((version_info & 0xF0) == 0x20) {
				version = Version.Version1;
				packet_size = 12;
			} else if ((version_info & 0xC0) == 0x40) {
				version = Version.Version2;
				Seek (position + 13);
				packet_size = 14 + (ReadBlock (1) [0] & 0x07);
			} else
				throw new UnsupportedFormatException (
					"Unknown MPEG version.");
			
			if (start_time == null)
				start_time = ReadTimestamp (position + 4);
			
			position += packet_size;
		}
		
		/// <summary>
		///    Reads an MPEG timestamp from a specified position in the
		///    file represented by the current instance.
		/// </summary>
		/// <param name="position">
		///    A <see cref="long" /> value containing the position in
		///    the file at which to read. This should be immediately
		///    following a system sync packet marker.
		/// </param>
		/// <returns>
		///    A <see cref="double" /> value containing the read time in
		///    seconds.
		/// </returns>
		private double ReadTimestamp (long position)
		{
			double high;
			uint low;
			
			Seek (position);

			if (version == Version.Version1) {
				ByteVector data = ReadBlock (5);
				high = (double) ((data [0] >> 3) & 0x01);
				
				low =  ((uint)((data [0] >> 1) & 0x03) << 30) |
					(uint) (data [1] << 22) |
					(uint)((data [2] >> 1) << 15) |
					(uint) (data [3] << 7) |
					(uint) (data [4] >> 1);
			} else {
				ByteVector data = ReadBlock (6);
				high = (double) ((data [0] & 0x20) >> 5);
				
				low =  ((uint) ((data [0] & 0x18) >> 3) << 30) |
					(uint) ((data [0] & 0x03) << 28) |
					(uint)  (data [1] << 20) |
					(uint) ((data [2] & 0xF8) << 12) |
					(uint) ((data [2] & 0x03) << 13) |
					(uint)  (data [3] << 5) |
					(uint)  (data [4] >> 3);
			}
			
			return (((high * 0x10000) * 0x10000) + low) / 90000.0;
		}
		
		#endregion
	}
}
