//
// FrameHeader.cs:
//
// Authors:
//   Brian Nickel (brian.nickel@gmail.com)
//   Gabriel BUrt (gabriel.burt@gmail.com)
//
// Original Source:
//   id3v2frame.cpp from TagLib
//
// Copyright (C) 2010 Novell, Inc.
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

using System;

namespace TagLib.Id3v2 {
	/// <summary>
	///    Indicates the flags applied to a <see cref="FrameHeader" />
	///    object.
	/// </summary>
	[Flags]
	public enum FrameFlags : ushort
	{
		/// <summary>
		///    The header contains no flags.
		/// </summary>
		None = 0,
		
		/// <summary>
		///    Indicates that the frame is to be deleted if the tag is
		///    altered.
		/// </summary>
		TagAlterPreservation = 0x4000,
		
		/// <summary>
		///    Indicates that the frame is to be deleted if the file is
		///    altered.
		/// </summary>
		FileAlterPreservation = 0x2000,
		
		/// <summary>
		///    Indicates that the frame is read-only and should not be
		///    altered.
		/// </summary>
		ReadOnly = 0x1000,
		
		/// <summary>
		///    Indicates that the frame has a grouping identity.
		/// </summary>
		GroupingIdentity = 0x0040,
		
		/// <summary>
		///    Indicates that the frame data is compressed.
		/// </summary>
		Compression = 0x0008,
		
		/// <summary>
		///    Indicates that the frame data is encrypted.
		/// </summary>
		Encryption = 0x0004,
		
		/// <summary>
		///    Indicates that the frame data has been unsynchronized.
		/// </summary>
		Unsynchronisation = 0x0002,
		
		/// <summary>
		///    Indicates that the frame has a data length indicator.
		/// </summary>
		DataLengthIndicator = 0x0001
	}
	
	/// <summary>
	///    This structure provides a representation of an ID3v2 frame header
	///    which can be read from and written to disk.
	/// </summary>
	public struct FrameHeader
	{
		#region Private Fields
		
		/// <summary>
		///    Contains frame's ID.
		/// </summary>
		private ReadOnlyByteVector frame_id;
		
		/// <summary>
		///    Contains frame's size.
		/// </summary>
		private uint frame_size;
		
		/// <summary>
		///    Contains frame's flags.
		/// </summary>
		private FrameFlags flags;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="FrameHeader" /> by reading it from raw header data
		///    of a specified version.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the raw
		///    data to build the new instance from.
		/// </param>
		/// <param name="version">
		///    A <see cref="byte" /> value containing the ID3v2 version
		///    with which the data in <paramref name="data" /> was
		///    encoded.
		/// </param>
		/// <remarks>
		///    If the data size is smaller than the size of a full
		///    header, the data is just treated as a frame identifier 
		///    and the remaining values are zeroed.
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="data" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="CorruptFileException">
		///    <paramref name="data" /> is smaller than the size of a
		///    frame identifier or <paramref name="version" /> is less
		///    than 2 or more than 4.
		/// </exception>
		public FrameHeader (ByteVector data, byte version)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			
			flags = 0;
			frame_size = 0;
			
			if (version < 2 || version > 4)
				throw new CorruptFileException (
					"Unsupported tag version.");
			
			if (data.Count < (version == 2 ? 3 : 4))
				throw new CorruptFileException (
					"Data must contain at least a frame ID.");
			
			switch (version)
			{
			case 2:
				// Set the frame ID -- the first three bytes
				frame_id = ConvertId (data.Mid (0, 3), version,
					false);
			
				// If the full header information was not passed
				// in, do not continue to the steps to parse the
				// frame size and flags.
				if (data.Count < 6)
					return;
				
				frame_size = data.Mid (3, 3).ToUInt ();
				return;
			
			case 3:
				// Set the frame ID -- the first four bytes
				frame_id = ConvertId (data.Mid (0, 4), version,
					false);
				
				// If the full header information was not passed
				// in, do not continue to the steps to parse the
				// frame size and flags.
				if (data.Count < 10)
					return;
				
				// Store the flags internally as version 2.4.
				frame_size = data.Mid (4, 4).ToUInt ();
				flags = (FrameFlags) (
					((data [8] << 7) & 0x7000) |
					((data [9] >> 4) & 0x000C) |
					((data [9] << 1) & 0x0040));
				
				return;
			
			case 4:
				// Set the frame ID -- the first four bytes
				frame_id = new ReadOnlyByteVector (
					data.Mid (0, 4));
				
				// If the full header information was not passed
				// in, do not continue to the steps to parse the
				// frame size and flags.
				if (data.Count < 10)
					return;
				
				frame_size = SynchData.ToUInt (data.Mid (4, 4));
				flags = (FrameFlags) data.Mid (8, 2).ToUShort ();
				
				return;
			
			default:
				throw new CorruptFileException (
					"Unsupported tag version.");
			}
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		/// <summary>
		///    Gets and sets the identifier of the frame described by
		///    the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="ReadOnlyByteVector" /> object containing the
		///    identifier of the frame described by the current
		///    instance.
		/// </value>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="value" /> is <see langword="null" />.
		/// </exception>
		public ReadOnlyByteVector FrameId {
			get {return frame_id;}
			set {
				if (value == null)
					throw new ArgumentNullException (
						"value");
				
				frame_id = value.Count == 4 ?
					value : new ReadOnlyByteVector (
						value.Mid (0, 4));
			}
		}
		
		/// <summary>
		///    Gets and sets the size of the frame described by the
		///    current instance, minus the header.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value containing the size of the
		///    frame described by the current instance.
		/// </value>
		public uint FrameSize {
			get {return frame_size;}
			set {frame_size = value;}
		}
		
		/// <summary>
		///    Gets and sets the flags applied to the current instance.
		/// </summary>
		/// <value>
		///    A bitwise combined <see cref="HeaderFlags" /> value
		///    containing the flags applied to the current instance.
		/// </value>
		/// <exception cref="ArgumentException">
		///    <paramref name="value" /> contains a either compression
		///    or encryption, neither of which are supported by the
		///    library.
		/// </exception>
		public FrameFlags Flags {
			get {return flags;}
			set {
				if ((value & (FrameFlags.Compression |
					FrameFlags.Encryption)) != 0)
					throw new ArgumentException (
						"Encryption and compression are not supported.",
						"value");
				
				flags = value;
			}
		}
		
		#endregion
		
		#region Public Methods
		
		/// <summary>
		///    Renders the current instance, encoded in a specified
		///    ID3v2 version.
		/// </summary>
		/// <param name="version">
		///    A <see cref="byte" /> value specifying the version of
		///    ID3v2 to use when encoding the current instance.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector" /> object containing the
		///    rendered version of the current instance.
		/// </returns>
		/// <exception cref="NotImplementedException">
		///    The version specified in the current instance is
		///    unsupported.
		/// </exception>
		public ByteVector Render (byte version) {
			ByteVector data = new ByteVector ();
			ByteVector id = ConvertId (frame_id, version, true);
			
			if (id == null)
				throw new NotImplementedException ();
			
			switch (version)
			{
			case 2:
				data.Add (id);
				data.Add (ByteVector.FromUInt (frame_size)
					.Mid (1, 3));
				
				return data;
			
			case 3:
				ushort new_flags = (ushort) (
					(((ushort)flags << 1) & 0xE000) |
					(((ushort)flags << 4) & 0x00C0) |
					(((ushort)flags >> 1) & 0x0020));
				
				data.Add (id);
				data.Add (ByteVector.FromUInt (frame_size));
				data.Add (ByteVector.FromUShort (new_flags));
				
				return data;
			
			case 4:
				data.Add (id);
				data.Add (SynchData.FromUInt (frame_size));
				data.Add (ByteVector.FromUShort ((ushort) flags));
				
				return data;
			
			default:
				throw new NotImplementedException (
					"Unsupported tag version.");
			}
		}
		
		/// <summary>
		///    Gets the size of a header for a specified ID3v2 version.
		/// </summary>
		/// <param name="version">
		///    A <see cref="byte" /> value specifying the version of
		///    ID3v2 to get the size for.
		/// </param>
		public static uint Size (byte version)
		{
			return (uint) (version < 3 ? 6 : 10);
		}
		
		#endregion
		
		
		
		#region Private Methods
		
		private static ReadOnlyByteVector ConvertId (ByteVector id,
		                                             byte version,
		                                             bool toVersion)
		{
			if (version >= 4) {
				ReadOnlyByteVector outid =
					id as ReadOnlyByteVector;
				
				return outid != null ?
					outid : new ReadOnlyByteVector (id);
			}
			
			if (id == null || version < 2)
				return null;
			
			if (!toVersion && (id == FrameType.EQUA ||
				id == FrameType.RVAD || id == FrameType.TRDA ||
				id == FrameType.TSIZ))
				return null;
			
			if (version == 2)
				for (int i = 0; i < version2_frames.GetLength (0); i ++) {
					if (!version2_frames [i,
						toVersion ? 1 : 0].Equals (id))
						continue;
					
					return version2_frames [i,
						toVersion ? 0 : 1];
				}
			
			if (version == 3)
				for (int i = 0; i < version3_frames.GetLength (0); i ++) {
					if (!version3_frames [i,
						toVersion ? 1 : 0].Equals (id))
						continue;
					
					return version3_frames [i,
						toVersion ? 0 : 1];
				}
			
			if ((id.Count != 4 && version > 2) ||
				(id.Count != 3 && version == 2))
				return null;
			
			return id is ReadOnlyByteVector ?
				id as ReadOnlyByteVector :
				new ReadOnlyByteVector (id);
		}
		
		private static readonly ReadOnlyByteVector [,] version2_frames =
			new ReadOnlyByteVector [59,2] {
				{ "BUF", "RBUF" },
				{ "CNT", "PCNT" },
				{ "COM", "COMM" },
				{ "CRA", "AENC" },
				{ "ETC", "ETCO" },
				{ "GEO", "GEOB" },
				{ "IPL", "TIPL" },
				{ "MCI", "MCDI" },
				{ "MLL", "MLLT" },
				{ "PIC", "APIC" },
				{ "POP", "POPM" },
				{ "REV", "RVRB" },
				{ "SLT", "SYLT" },
				{ "STC", "SYTC" },
				{ "TAL", "TALB" },
				{ "TBP", "TBPM" },
				{ "TCM", "TCOM" },
				{ "TCO", "TCON" },
				{ "TCP", "TCMP" },
				{ "TCR", "TCOP" },
				{ "TDA", "TDAT" },
				{ "TIM", "TIME" },
				{ "TDY", "TDLY" },
				{ "TEN", "TENC" },
				{ "TFT", "TFLT" },
				{ "TKE", "TKEY" },
				{ "TLA", "TLAN" },
				{ "TLE", "TLEN" },
				{ "TMT", "TMED" },
				{ "TOA", "TOAL" },
				{ "TOF", "TOFN" },
				{ "TOL", "TOLY" },
				{ "TOR", "TDOR" },
				{ "TOT", "TOAL" },
				{ "TP1", "TPE1" },
				{ "TP2", "TPE2" },
				{ "TP3", "TPE3" },
				{ "TP4", "TPE4" },
				{ "TPA", "TPOS" },
				{ "TPB", "TPUB" },
				{ "TRC", "TSRC" },
				{ "TRK", "TRCK" },
				{ "TSS", "TSSE" },
				{ "TT1", "TIT1" },
				{ "TT2", "TIT2" },
				{ "TT3", "TIT3" },
				{ "TXT", "TOLY" },
				{ "TXX", "TXXX" },
				{ "TYE", "TDRC" },
				{ "UFI", "UFID" },
				{ "ULT", "USLT" },
				{ "WAF", "WOAF" },
				{ "WAR", "WOAR" },
				{ "WAS", "WOAS" },
				{ "WCM", "WCOM" },
				{ "WCP", "WCOP" },
				{ "WPB", "WPUB" },
				{ "WXX", "WXXX" },
				{ "XRV", "RVA2" }
			};
		
		private static readonly ReadOnlyByteVector [,] version3_frames =
			new ReadOnlyByteVector [3,2] {
				{ "TORY", "TDOR" },
				{ "TYER", "TDRC" },
				{ "XRVA", "RVA2" }

			};
		
		#endregion
	}
}
