//
// StreamPropertiesObject.cs: Provides a representation of an ASF Stream
// Properties object which can be read from and written to disk.
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
using System.Text;

namespace TagLib.Asf {
	/// <summary>
	///    This class extends <see cref="Object" /> to provide a
	///    representation of an ASF Stream Properties object which can be
	///    read from and written to disk.
	/// </summary>
	public class StreamPropertiesObject : Object
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the stream type GUID.
		/// </summary>
		private System.Guid stream_type;
		
		/// <summary>
		///    Contains the error correction type GUID.
		/// </summary>
		private System.Guid error_correction_type;
		
		/// <summary>
		///    Contains the time offset of the stream.
		/// </summary>
		private ulong time_offset;
		
		/// <summary>
		///    Contains the stream flags.
		/// </summary>
		private ushort flags;
		
		/// <summary>
		///    Contains the reserved data.
		/// </summary>
		private uint reserved;
		
		/// <summary>
		///    Contains the type specific data.
		/// </summary>
		private ByteVector type_specific_data;
		
		/// <summary>
		///    Contains the error correction data.
		/// </summary>
		private ByteVector error_correction_data;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="PaddingObject" /> by reading the contents from a
		///    specified position in a specified file.
		/// </summary>
		/// <param name="file">
		///    A <see cref="Asf.File" /> object containing the file from
		///    which the contents of the new instance are to be read.
		/// </param>
		/// <param name="position">
		///    A <see cref="long" /> value specify at what position to
		///    read the object.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="file" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///    <paramref name="position" /> is less than zero or greater
		///    than the size of the file.
		/// </exception>
		/// <exception cref="CorruptFileException">
		///    The object read from disk does not have the correct GUID
		///    or smaller than the minimum size.
		/// </exception>
		public StreamPropertiesObject (Asf.File file, long position)
			: base (file, position)
		{
			if (!Guid.Equals (Asf.Guid.AsfStreamPropertiesObject))
				throw new CorruptFileException (
					"Object GUID incorrect.");
			
			if (OriginalSize < 78)
				throw new CorruptFileException (
					"Object size too small.");
			
			stream_type = file.ReadGuid ();
			error_correction_type = file.ReadGuid ();
			time_offset = file.ReadQWord ();
			
			int type_specific_data_length = (int) file.ReadDWord ();
			int error_correction_data_length = (int)
				file.ReadDWord ();
			
			flags = file.ReadWord ();
			reserved = file.ReadDWord ();
			type_specific_data =
				file.ReadBlock (type_specific_data_length);
			error_correction_data =
				file.ReadBlock (error_correction_data_length);
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		/// <summary>
		///    Gets the codec information contained in the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="ICodec" /> object containing the codec
		///    information read from <see cref="TypeSpecificData" /> or
		///    <see langword="null" /> if the data could not be decoded.
		/// </value>
		public ICodec Codec {
			get {
				if (stream_type == Asf.Guid.AsfAudioMedia)
					return new Riff.WaveFormatEx (
						type_specific_data, 0);
				
				if (stream_type == Asf.Guid.AsfVideoMedia)
					return new TagLib.Riff.BitmapInfoHeader (
						type_specific_data, 11);
				
				return null;
			}
		}
		
		/// <summary>
		///    Gets the stream type GUID of the current instance.
		/// </summary>
		/// <summary>
		///    A <see cref="System.Guid" /> object containing the stream
		///    type GUID of the current instance.
		/// </summary>
		public System.Guid StreamType {
			get {return stream_type;}
		}
		
		/// <summary>
		///    Gets the error correction type GUID of the current
		///    instance.
		/// </summary>
		/// <summary>
		///    A <see cref="System.Guid" /> object containing the error
		///    correction type GUID of the current instance.
		/// </summary>
		public System.Guid ErrorCorrectionType {
			get {return error_correction_type;}
		}
		
		/// <summary>
		///    Gets the time offset at which the stream described by the
		///    current instance begins.
		/// </summary>
		/// <value>
		///    A <see cref="TimeSpan" /> value containing the time
		///    offset at which the stream described by the current
		///    instance begins.
		/// </value>
		public TimeSpan TimeOffset {
			get {return new TimeSpan ((long)time_offset);}
		}
		
		/// <summary>
		///    Gets the flags that apply to the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="ushort" /> value containing the flags that
		///    apply to the current instance.
		/// </value>
		public ushort Flags {
			get {return flags;}
		}
		
		/// <summary>
		///    Gets the type specific data contained in the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="ByteVector" /> object containing the type
		///    specific data contained in the current instance.
		/// </value>
		/// <remarks>
		///    The contents of this value are dependant on the type
		///    contained in <see cref="StreamType" />.
		/// </remarks>
		public ByteVector TypeSpecificData {
			get {return type_specific_data;}
		}
		
		/// <summary>
		///    Gets the error correction data contained in the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="ByteVector" /> object containing the error
		///    correction data contained in the current instance.
		/// </value>
		/// <remarks>
		///    The contents of this value are dependant on the type
		///    contained in <see cref="ErrorCorrectionType" />.
		/// </remarks>
		public ByteVector ErrorCorrectionData {
			get {return error_correction_data;}
		}
		
		#endregion
		
		
		
		#region Public Methods
		
		/// <summary>
		///    Renders the current instance as a raw ASF object.
		/// </summary>
		/// <returns>
		///    A <see cref="ByteVector" /> object containing the
		///    rendered version of the current instance.
		/// </returns>
		public override ByteVector Render ()
		{
			ByteVector output = stream_type.ToByteArray ();
			output.Add (error_correction_type.ToByteArray ());
			output.Add (RenderQWord (time_offset));
			output.Add (RenderDWord ((uint)
				type_specific_data.Count));
			output.Add (RenderDWord ((uint)
				error_correction_data.Count));
			output.Add (RenderWord  (flags));
			output.Add (RenderDWord (reserved));
			output.Add (type_specific_data);
			output.Add (error_correction_data);
			
			return Render (output);
		}
		
		#endregion
	}
}
