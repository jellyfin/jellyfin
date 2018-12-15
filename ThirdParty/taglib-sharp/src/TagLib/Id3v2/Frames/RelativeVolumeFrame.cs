//
// RelativeVolumeFrame.cs:
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Original Source:
//   textidentificationframe.cpp from TagLib
//
// Copyright (C) 2005-2007 Brian Nickel
// Copyright (C) 2004 Scott Wheeler (Original Implementation)
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
using System.Collections.Generic;
using System;

namespace TagLib.Id3v2 {
	/// <summary>
	///    Specified the type of channel data to get from or set to a
	///    <see cref="RelativeVolumeFrame" /> object.
	/// </summary>
	public enum ChannelType {
		/// <summary>
		///    The channel data is for some other speaker.
		/// </summary>
		Other = 0x00,
		
		/// <summary>
		///    The channel data is for the master volume.
		/// </summary>
		MasterVolume = 0x01,
		
		/// <summary>
		///    The channel data is for the front right speaker.
		/// </summary>
		FrontRight = 0x02,
		
		/// <summary>
		///    The channel data is for the front left speaker.
		/// </summary>
		FrontLeft = 0x03,
		
		/// <summary>
		///    The channel data is for the back right speaker.
		/// </summary>
		BackRight = 0x04,
		
		/// <summary>
		///    The channel data is for the back left speaker.
		/// </summary>
		BackLeft = 0x05,
		
		/// <summary>
		///    The channel data is for the front center speaker.
		/// </summary>
		FrontCentre = 0x06,
		
		/// <summary>
		///    The channel data is for the back center speaker.
		/// </summary>
		BackCentre = 0x07,
		
		/// <summary>
		///    The channel data is for the subwoofer.
		/// </summary>
		Subwoofer = 0x08
	}
	
	/// <summary>
	///    This class extends <see cref="Frame" />, implementing support for
	///    ID3v2 Relative Volume (RVA2) Frames.
	/// </summary>
	public class RelativeVolumeFrame : Frame
	{
#region Private Fields
		
		/// <summary>
		///    Contains the frame identification.
		/// </summary>
		private string identification = null;
		
		/// <summary>
		///    Contains the channel data.
		/// </summary>
		private ChannelData [] channels = new ChannelData [9];
		
#endregion
		
		
		
#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="RelativeVolumeFrame" /> with a specified
		///    identifier.
		/// </summary>
		/// <param name="identification">
		///    A <see cref="string" /> object containing the
		///    identification to use for the new frame.
		/// </param>
		public RelativeVolumeFrame (string identification)
			: base (FrameType.RVA2, 4)
		{
			this.identification = identification;
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="RelativeVolumeFrame" /> by reading its raw data in
		///    a specified ID3v2 version.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object starting with the raw
		///    representation of the new frame.
		/// </param>
		/// <param name="version">
		///    A <see cref="byte" /> indicating the ID3v2 version the
		///    raw frame is encoded in.
		/// </param>
		public RelativeVolumeFrame (ByteVector data, byte version)
			: base (data, version)
		{
			SetData (data, 0, version, true);
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="RelativeVolumeFrame" /> by reading its raw data in
		///    a specified ID3v2 version.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the raw
		///    representation of the new frame.
		/// </param>
		/// <param name="offset">
		///    A <see cref="int" /> indicating at what offset in
		///    <paramref name="data" /> the frame actually begins.
		/// </param>
		/// <param name="header">
		///    A <see cref="FrameHeader" /> containing the header of the
		///    frame found at <paramref name="offset" /> in the data.
		/// </param>
		/// <param name="version">
		///    A <see cref="byte" /> indicating the ID3v2 version the
		///    raw frame is encoded in.
		/// </param>
		protected internal RelativeVolumeFrame (ByteVector data,
		                                        int offset,
		                                        FrameHeader header,
		                                        byte version)
			: base(header)
		{
			SetData (data, offset, version, false);
		}
		
#endregion
		
		
		
#region Public Properties
		
		/// <summary>
		///    Gets the identification used for the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the
		///    identification used for the current instance.
		/// </value>
		public string Identification {
			get {return identification;}
		}
		
		/// <summary>
		///    Gets a list of the channels in the current instance that
		///    contain a value.
		/// </summary>
		/// <value>
		///    A <see cref="T:ChannelType[]" /> containing the channels
		///    which have a value set in the current instance.
		/// </value>
		public ChannelType [] Channels {
			get {
				List<ChannelType> types = new List<ChannelType> ();
				for (int i = 0; i < 9; i ++)
					if (channels [i].IsSet)
						types.Add ((ChannelType) i);
				return types.ToArray ();
			}
		}
		
#endregion
		
		
		
#region Public Methods
		
		/// <summary>
		///    Creates a text description of the current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="string" /> object containing a description
		///    of the current instance.
		/// </returns>
		public override string ToString ()
		{
			return identification;
		}
		
		/// <summary>
		///    Gets the volume adjustment index for a specified channel.
		/// </summary>
		/// <param name="type">
		///    A <see cref="ChannelType" /> value specifying which
		///    channel to get the value for.
		/// </param>
		/// <returns>
		///    A <see cref="short" /> value containing the volume
		///    adjustment index.
		/// </returns>
		/// <remarks>
		///    The volume adjustment index is simply the volume
		///    adjustment multiplied by 512.
		/// </remarks>
		/// <seealso cref="SetVolumeAdjustmentIndex"/>
		/// <seealso cref="GetVolumeAdjustment"/>
		public short GetVolumeAdjustmentIndex (ChannelType type)
		{
			return channels [(int) type].VolumeAdjustmentIndex;
		}
		
		/// <summary>
		///    Sets the volume adjustment index for a specified channel.
		/// </summary>
		/// <param name="type">
		///    A <see cref="ChannelType" /> value specifying which
		///    channel to set the value for.
		/// </param>
		/// <param name="index">
		///    A <see cref="short" /> value containing the volume
		///    adjustment index.
		/// </param>
		/// <seealso cref="GetVolumeAdjustmentIndex"/>
		/// <seealso cref="SetVolumeAdjustment"/>
		public void SetVolumeAdjustmentIndex (ChannelType type,
		                                      short index)
		{
			channels [(int) type].VolumeAdjustmentIndex = index;
		}
		
		/// <summary>
		///    Gets the volume adjustment for a specified channel.
		/// </summary>
		/// <param name="type">
		///    A <see cref="ChannelType" /> value specifying which
		///    channel to get the value for.
		/// </param>
		/// <returns>
		///    A <see cref="float" /> value containing the volume
		///    adjustment in decibles.
		/// </returns>
		/// <remarks>
		///    The value can be between -64dB and +64dB.
		/// </remarks>
		/// <seealso cref="SetVolumeAdjustment"/>
		/// <seealso cref="GetVolumeAdjustmentIndex"/>
		public float GetVolumeAdjustment (ChannelType type)
		{
			return channels [(int) type].VolumeAdjustment;
		}
		
		/// <summary>
		///    Sets the volume adjustment for a specified channel.
		/// </summary>
		/// <param name="type">
		///    A <see cref="ChannelType" /> value specifying which
		///    channel to set the value for.
		/// </param>
		/// <param name="adjustment">
		///    A <see cref="float" /> value containing the volume
		///    adjustment in decibles.
		/// </param>
		/// <remarks>
		///    The value can be between -64dB and +64dB.
		/// </remarks>
		/// <seealso cref="GetVolumeAdjustment"/>
		/// <seealso cref="SetVolumeAdjustmentIndex"/>
		public void SetVolumeAdjustment (ChannelType type,
		                                 float adjustment)
		{
			channels [(int) type].VolumeAdjustment = adjustment;
		}
		
		/// <summary>
		///    Gets the peak volume index for a specified channel.
		/// </summary>
		/// <param name="type">
		///    A <see cref="ChannelType" /> value specifying which
		///    channel to get the value for.
		/// </param>
		/// <returns>
		///    A <see cref="ulong" /> value containing the peak volume
		///    index.
		/// </returns>
		/// <remarks>
		///    The peak volume index is simply the peak volume
		///    multiplied by 512.
		/// </remarks>
		/// <seealso cref="SetPeakVolumeIndex"/>
		/// <seealso cref="GetPeakVolume"/>
		public ulong GetPeakVolumeIndex (ChannelType type)
		{
			return channels [(int) type].PeakVolumeIndex;
		}
		
		/// <summary>
		///    Sets the peak volume index for a specified channel.
		/// </summary>
		/// <param name="type">
		///    A <see cref="ChannelType" /> value specifying which
		///    channel to set the value for.
		/// </param>
		/// <param name="index">
		///    A <see cref="ulong" /> value containing the peak volume
		///    index.
		/// </param>
		/// <remarks>
		///    The peak volume index is simply the peak volume
		///    multiplied by 512.
		/// </remarks>
		/// <seealso cref="GetPeakVolumeIndex"/>
		/// <seealso cref="SetPeakVolume"/>
		public void SetPeakVolumeIndex (ChannelType type, ulong index)
		{
			channels [(int) type].PeakVolumeIndex = index;
		}
		
		/// <summary>
		///    Gets the peak volume for a specified channel.
		/// </summary>
		/// <param name="type">
		///    A <see cref="ChannelType" /> value specifying which
		///    channel to get the value for.
		/// </param>
		/// <returns>
		///    A <see cref="double" /> value containing the peak volume.
		/// </returns>
		/// <seealso cref="SetPeakVolume"/>
		/// <seealso cref="GetPeakVolumeIndex"/>
		public double GetPeakVolume (ChannelType type)
		{
			return channels [(int) type].PeakVolume;
		}
		
		/// <summary>
		///    Sets the peak volume for a specified channel.
		/// </summary>
		/// <param name="type">
		///    A <see cref="ChannelType" /> value specifying which
		///    channel to set the value for.
		/// </param>
		/// <param name="peak">
		///    A <see cref="double" /> value containing the peak volume.
		/// </param>
		/// <seealso cref="GetPeakVolume"/>
		/// <seealso cref="SetPeakVolumeIndex"/>
		public void SetPeakVolume (ChannelType type, double peak)
		{
			channels [(int) type].PeakVolume = peak;
		}
		
#endregion
		
		
		
#region Public Static Methods
		
		/// <summary>
		///    Gets a specified volume adjustment frame from the
		///    specified tag, optionally creating it if it does not
		///    exist.
		/// </summary>
		/// <param name="tag">
		///    A <see cref="Tag" /> object to search in.
		/// </param>
		/// <param name="identification">
		///    A <see cref="string" /> specifying the identification to
		///    match.
		/// </param>
		/// <param name="create">
		///    A <see cref="bool" /> specifying whether or not to create
		///    and add a new frame to the tag if a match is not found.
		/// </param>
		/// <returns>
		///    A <see cref="RelativeVolumeFrame" /> object containing
		///    the matching frame, or <see langword="null" /> if a match
		///    wasn't found and <paramref name="create" /> is <see
		///    langword="false" />.
		/// </returns>
		public static RelativeVolumeFrame Get (Tag tag,
		                                       string identification,
		                                       bool create)
		{
			RelativeVolumeFrame rva2;
			foreach (Frame frame in tag.GetFrames (FrameType.RVA2)) {
				rva2 = frame as RelativeVolumeFrame;
				
				if (rva2 == null)
					continue;
				
				if (rva2.Identification != identification)
					continue;
				
				return rva2;
			}
			
			if (!create)
				return null;
			
			rva2 = new RelativeVolumeFrame (identification);
			tag.AddFrame (rva2);
			return rva2;
		}
		
#endregion
		
		
		
#region Protected Properties
		
		/// <summary>
		///    Populates the values in the current instance by parsing
		///    its field data in a specified version.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the
		///    extracted field data.
		/// </param>
		/// <param name="version">
		///    A <see cref="byte" /> indicating the ID3v2 version the
		///    field data is encoded in.
		/// </param>
		/// <exception cref="CorruptFileException">
		///    <paramref name="data" /> contains less than 5 bytes.
		/// </exception>
		protected override void ParseFields (ByteVector data,
		                                     byte version)
		{
			int pos = data.Find (ByteVector.TextDelimiter (
				StringType.Latin1));
			if (pos < 0)
				return;
			
			identification = data.ToString (StringType.Latin1, 0,
				pos++);
			
			// Each channel is at least 4 bytes.
			
			while (pos <= data.Count - 4) {
				int type = data [pos++];
				
				unchecked {
					channels [type].VolumeAdjustmentIndex =
						(short) data.Mid (pos,
							2).ToUShort ();
				}
				pos += 2;
				
				int bytes = BitsToBytes (data [pos++]);
				
				if (data.Count < pos + bytes)
					break;
				
				channels [type].PeakVolumeIndex = data.Mid (pos,
					bytes).ToULong ();
				pos += bytes;
			}
		}
		
		/// <summary>
		///    Renders the values in the current instance into field
		///    data for a specified version.
		/// </summary>
		/// <param name="version">
		///    A <see cref="byte" /> indicating the ID3v2 version the
		///    field data is to be encoded in.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector" /> object containing the
		///    rendered field data.
		/// </returns>
		protected override ByteVector RenderFields (byte version)
		{
			ByteVector data = new ByteVector ();
			data.Add (ByteVector.FromString (identification,
				StringType.Latin1));
			data.Add (ByteVector.TextDelimiter(StringType.Latin1));
			
			for (byte i = 0; i < 9; i ++) {
				if (!channels [i].IsSet)
					continue;
				
				data.Add (i);
				unchecked {
					data.Add (ByteVector.FromUShort (
						(ushort) channels [i]
							.VolumeAdjustmentIndex));
				}
				
				byte bits = 0;
				
				for (byte j = 0; j < 64; j ++)
					if ((channels [i].PeakVolumeIndex &
						(1UL << j)) != 0)
						bits = (byte)(j + 1);
				
				data.Add (bits);
				
				if (bits > 0)
					data.Add (ByteVector.FromULong (
						channels [i].PeakVolumeIndex)
							.Mid (8 - BitsToBytes (bits)));
			}
			
			return data;
		}
		
#endregion
		
		
		
#region ICloneable
		
		/// <summary>
		///    Creates a deep copy of the current instance.
		/// </summary>
		/// <returns>
		///    A new <see cref="Frame" /> object identical to the
		///    current instance.
		/// </returns>
		public override Frame Clone ()
		{
			RelativeVolumeFrame frame =
				new RelativeVolumeFrame (identification);
			for (int i = 0; i < 9; i ++)
				frame.channels [i] = channels [i];
			return frame;
		}
		
#endregion
		
		
		
#region Private Static Methods
		
		private static int BitsToBytes (int i)
		{
			return i % 8 == 0 ? i / 8 : (i - i % 8) / 8 + 1;
		}
		
		#endregion
		
		
		
		#region Classes
		
		private struct ChannelData
		{
			public short VolumeAdjustmentIndex;
			public ulong PeakVolumeIndex;
			
			public bool IsSet {
				get {
					return VolumeAdjustmentIndex != 0 ||
						PeakVolumeIndex != 0;
				}
			}
			
			public float VolumeAdjustment {
				get {return VolumeAdjustmentIndex / 512f;}
				set {
					VolumeAdjustmentIndex =
						(short) (value * 512f);
				}
			}
			
			public double PeakVolume {
				get {return PeakVolumeIndex / 512.0;}
				set {PeakVolumeIndex = (ulong) (value * 512.0);}
			}
		}
		
		#endregion
	}
}
