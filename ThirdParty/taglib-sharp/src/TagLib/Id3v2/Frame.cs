//
// Frame.cs:
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Original Source:
//   id3v2frame.cpp from TagLib
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

using System;

namespace TagLib.Id3v2 {
	/// <summary>
	///    This abstract class provides a basic framework for representing
	///    ID3v2.4 frames.
	/// </summary>
	public abstract class Frame : ICloneable
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the frame's header.
		/// </summary>
		protected FrameHeader header;
		
		/// <summary>
		///    Contains the frame's grouping ID.
		/// </summary>
		private byte group_id;
		
		/// <summary>
		///    Contains the frame's encryption ID.
		/// </summary>
		private byte encryption_id;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Frame" /> by reading the raw header encoded in the
		///    specified ID3v2 version.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the
		///    identifier or header data to use for the new instance.
		/// </param>
		/// <param name="version">
		///    A <see cref="byte" /> value indicating the ID3v2 version
		///    which <paramref name="data" /> is encoded in.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="data" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentException">
		///    <paramref name="data" /> does not contain a complete
		///    identifier.
		/// </exception>
		protected Frame (ByteVector data, byte version)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			
			if (data.Count < ((version < 3) ? 3 : 4))
				throw new ArgumentException (
					"Data contains an incomplete identifier.",
					"data");
			
			header = new FrameHeader (data, version);
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Frame" /> with a specified header.
		/// </summary>
		/// <param name="header">
		///    A <see cref="FrameHeader" /> value containing the header
		///    to use for the new instance.
		/// </param>
		protected Frame (FrameHeader header)
		{
			this.header = header;
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		/// <summary>
		///    Gets the frame ID for the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="ReadOnlyByteVector" /> object containing the
		///    four-byte ID3v2.4 frame header for the current instance.
		/// </value>
		public ReadOnlyByteVector FrameId {
			get {return header.FrameId;}
		}
		
		/// <summary>
		///    Gets the size of the current instance as it was last
		///    stored on disk.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value containing the size of the
		///    current instance as it was last stored on disk.
		/// </value>
		public uint Size {
			get {return header.FrameSize;}
		}
		
		/// <summary>
		///    Gets and sets the frame flags applied to the current
		///    instance.
		/// </summary>
		/// <value>
		///    A bitwise combined <see cref="FrameFlags" /> value
		///    containing the frame flags applied to the current
		///    instance.
		/// </value>
		/// <remarks>
		///    If the value includes either <see
		///    cref="FrameFlags.Encryption" /> or <see
		///    cref="FrameFlags.Compression" />, <see cref="Render" />
		///    will throw a <see cref="NotImplementedException" />.
		/// </remarks>
		public FrameFlags Flags {
			get {return header.Flags;}
			set {header.Flags = value;}
		}
		
		/// <summary>
		///    Gets and sets the grouping ID applied to the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="short" /> value containing the grouping
		///    identifier for the current instance, or -1 if not set.
		/// </value>
		/// <remarks>
		///    Grouping identifiers can be between 0 and 255. Setting
		///    any other value will unset the grouping identity and set
		///    the value to -1.
		/// </remarks>
		public short GroupId {
			get {
				return (Flags & FrameFlags.GroupingIdentity)
					!= 0 ? group_id : (short) -1;
			}
			set {
				if (value >= 0x00 && value <= 0xFF) {
					group_id = (byte) value;
					Flags |= FrameFlags.GroupingIdentity;
				} else {
					Flags &= ~FrameFlags.GroupingIdentity;
				}
			}
		}
		
		/// <summary>
		///    Gets and sets the encryption ID applied to the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="short" /> value containing the encryption
		///    identifier for the current instance, or -1 if not set.
		/// </value>
		/// <remarks>
		///    <para>Encryption identifiers can be between 0 and 255.
		///    Setting any other value will unset the grouping identity
		///    and set the value to -1.</para>
		///    <para>If set, <see cref="Render" /> will throw a <see
		///    cref="NotImplementedException" />.</para>
		/// </remarks>
		public short EncryptionId {
			get {
				return (Flags & FrameFlags.Encryption) != 0 ?
					encryption_id : (short) -1;
			}
			set {
				if (value >= 0x00 && value <= 0xFF) {
					encryption_id = (byte) value;
					Flags |= FrameFlags.Encryption;
				} else {
					Flags &= ~FrameFlags.Encryption;
				}
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
		///    The current instance uses some feature that cannot be
		///    implemented in the specified ID3v2 version, or uses a
		///    feature, such as encryption or compression, which is not
		///    yet implemented in the library.
		/// </exception>
		public virtual ByteVector Render (byte version)
		{
			// Remove flags that are not supported by older versions
			// of ID3v2.
			if (version < 4)
				Flags &= ~(FrameFlags.DataLengthIndicator |
					FrameFlags.Unsynchronisation);
			
			if (version < 3)
				Flags &= ~(FrameFlags.Compression |
					FrameFlags.Encryption |
					FrameFlags.FileAlterPreservation |
					FrameFlags.GroupingIdentity |
					FrameFlags.ReadOnly |
					FrameFlags.TagAlterPreservation);
			
			ByteVector field_data = RenderFields (version);
			
			// If we don't have any content, don't render anything.
			// This will cause the frame to not be rendered.
			if (field_data.Count == 0)
				return new ByteVector ();
			
			ByteVector front_data = new ByteVector ();
			
			if ((Flags & (FrameFlags.Compression |
				FrameFlags.DataLengthIndicator)) != 0)
				front_data.Add (ByteVector.FromUInt ((uint)
					field_data.Count));
			
			if ((Flags & FrameFlags.GroupingIdentity) != 0)
				front_data.Add (group_id);
			
			if ((Flags & FrameFlags.Encryption) != 0)
				front_data.Add (encryption_id);
			
			// FIXME: Implement compression.
			if ((Flags & FrameFlags.Compression) != 0)
				throw new NotImplementedException (
					"Compression not yet supported");
			
			// FIXME: Implement encryption.
			if ((Flags & FrameFlags.Encryption) != 0)
				throw new NotImplementedException (
					"Encryption not yet supported");
			
			if ((Flags & FrameFlags.Unsynchronisation) != 0)
				SynchData.UnsynchByteVector (field_data);
			
			if (front_data.Count > 0)
				field_data.Insert (0, front_data);
			
			header.FrameSize = (uint) field_data.Count;
			ByteVector header_data = header.Render (version);
			header_data.Add (field_data);
			
			return header_data;
		}
		
		#endregion
		
		
		
		#region Public Static Methods
		
		/// <summary>
		///    Gets the text delimiter for a specified encoding.
		/// </summary>
		/// <param name="type">
		///    A <see cref="StringType" /> value specifying the encoding
		///    to get the delimiter for.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector" /> object containing the
		///    delimiter for the specified encoding.
		/// </returns>
		[Obsolete("Use ByteVector.TextDelimiter.")]
		public static ByteVector TextDelimiter (StringType type)
		{
			return ByteVector.TextDelimiter (type);
		}
		
		#endregion
		
		
		
		#region Protected Methods
		
		/// <summary>
		///    Converts an encoding to be a supported encoding for a
		///    specified tag version.
		/// </summary>
		/// <param name="type">
		///    A <see cref="StringType" /> value containing the original
		///    encoding.
		/// </param>
		/// <param name="version">
		///    A <see cref="byte" /> value containing the ID3v2 version
		///    to be encoded for.
		/// </param>
		/// <returns>
		///    A <see cref="StringType" /> value containing the correct
		///    encoding to use, based on <see
		///    cref="Tag.ForceDefaultEncoding" /> and what is supported
		///    by <paramref name="version" />.
		/// </returns>
		protected static StringType CorrectEncoding (StringType type,
		                                             byte version)
		{
			if (Tag.ForceDefaultEncoding)
				type = Tag.DefaultEncoding;
			
			return (version < 4 && type == StringType.UTF8) ?
				StringType.UTF16 : type;
		}
		
		/// <summary>
		///    Populates the current instance by reading the raw frame
		///    from disk, optionally reading the header.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the raw
		///    ID3v2 frame.
		/// </param>
		/// <param name="offset">
		///    A <see cref="int" /> value containing the offset in
		///    <paramref name="data" /> at which the frame begins.
		/// </param>
		/// <param name="version">
		///    A <see cref="byte" /> value containing the ID3v2 version
		///    of the raw frame contained in <paramref name="data" />.
		/// </param>
		/// <param name="readHeader">
		///    A <see cref="bool" /> value indicating whether or not to
		///    read the header into current instance.
		/// </param>
		protected void SetData (ByteVector data, int offset,
		                        byte version, bool readHeader)
		{
			if (readHeader)
				header = new FrameHeader (data, version);
			ParseFields (FieldData (data, offset, version),
				version);
		}
		
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
		protected abstract void ParseFields (ByteVector data,
		                                     byte version);
		
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
		protected abstract ByteVector RenderFields (byte version);
		
		/// <summary>
		///    Extracts the field data from the raw data portion of an
		///    ID3v2 frame.
		/// </summary>
		/// <param name="frameData">
		///    A <see cref="ByteVector" /> object containing fraw frame
		///    data.
		/// </param>
		/// <param name="offset">
		///    A <see cref="int" /> value containing the index at which
		///    the data is contained.
		/// </param>
		/// <param name="version">
		///    A <see cref="byte" /> value containing the ID3v2 version
		///    of the data.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector" /> object containing the
		///    extracted field data.
		/// </returns>
		/// <remarks>
		///    This method is necessary for extracting extra data
		///    prepended to the frame such as the grouping ID.
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="frameData" /> is <see langword="null" />.
		/// </exception>
		protected ByteVector FieldData (ByteVector frameData,
		                                int offset, byte version)
		{
			if (frameData == null)
				throw new ArgumentNullException ("frameData");
			
			int data_offset = offset + (int) FrameHeader.Size (version);
			int data_length = (int) Size;

			if ((Flags & (FrameFlags.Compression |
				FrameFlags.DataLengthIndicator)) != 0) {
				data_offset += 4;
				data_length -= 4;
			}
			
			if ((Flags & FrameFlags.GroupingIdentity) != 0) {
				if (frameData.Count >= data_offset)
					throw new TagLib.CorruptFileException (
						"Frame data incomplete.");
				group_id = frameData [data_offset++];
				data_length--;
			}
			
			if ((Flags & FrameFlags.Encryption) != 0) {
				if (frameData.Count >= data_offset)
					throw new TagLib.CorruptFileException (
						"Frame data incomplete.");
				encryption_id = frameData [data_offset++];
				data_length--;
			}

			data_length = Math.Min (data_length, frameData.Count - data_offset);
			if (data_length < 0 )
				throw new CorruptFileException (
					"Frame size less than zero.");
			
			ByteVector data = frameData.Mid (data_offset,
				data_length);

			if ((Flags & FrameFlags.Unsynchronisation) != 0) {
				int before_length = data.Count;
				SynchData.ResynchByteVector (data);
				data_length -= (data.Count - before_length);
			}
			
			// FIXME: Implement encryption.
			if ((Flags & FrameFlags.Encryption) != 0)
				throw new NotImplementedException ();
			
			// FIXME: Implement compression.
			if ((Flags & FrameFlags.Compression) != 0)
				throw new NotImplementedException ();
			/*
			if(d->header->compression()) {
				ByteVector data(frameDataLength);
				uLongf uLongTmp = frameDataLength;
				::uncompress((Bytef *) data.data(),
				(uLongf *) &uLongTmp,
				(Bytef *) frameData.data() + frameDataOffset,
				size());
				return data;
			}
			*/
			
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
		/// <remarks>
		///    This method is implemented by rendering the current
		///    instance as an ID3v2.4 frame and using <see
		///    cref="FrameFactory.CreateFrame" /> to create a new
		///    frame. As such, this method should be overridden by
		///    child classes.
		/// </remarks>
		public virtual Frame Clone ()
		{
			int index = 0;
			return FrameFactory.CreateFrame(Render(4), null, ref index,
				4, false);
		}
		
		object ICloneable.Clone ()
		{
			return Clone ();
		}
		
#endregion
	}
}
