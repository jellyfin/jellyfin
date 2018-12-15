//
// FilePropertiesObject.cs: Provides a representation of an ASF File Properties
// object which can be read from and written to disk.
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

namespace TagLib.Asf {
	/// <summary>
	///    This class extends <see cref="Object" /> to provide a
	///    representation of an ASF File Properties object which can be read
	///    from and written to disk.
	/// </summary>
	public class FilePropertiesObject : Object
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the GUID for the file.
		/// </summary>
		private System.Guid file_id;
		
		/// <summary>
		///    Contains the file size.
		/// </summary>
		private ulong file_size;
		
		/// <summary>
		///    Contains the creation date.
		/// </summary>
		private ulong creation_date;
		
		/// <summary>
		///    Contains the packet count.
		/// </summary>
		private ulong data_packets_count;
		
		/// <summary>
		///    Contains the play duration.
		/// </summary>
		private ulong play_duration;
		
		/// <summary>
		///    Contains the send duration.
		/// </summary>
		private ulong send_duration;
		
		/// <summary>
		///    Contains the preroll.
		/// </summary>
		private ulong preroll;
		
		/// <summary>
		///    Contains the file flags.
		/// </summary>
		private uint flags;
		
		/// <summary>
		///    Contains the minimum packet size.
		/// </summary>
		private uint minimum_data_packet_size;
		
		/// <summary>
		///    Contains the maxximum packet size.
		/// </summary>
		private uint maximum_data_packet_size;
		
		/// <summary>
		///    Contains the maximum bitrate of the file.
		/// </summary>
		private uint maximum_bitrate;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="FilePropertiesObject" /> by reading the contents
		///    from a specified position in a specified file.
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
		public FilePropertiesObject (Asf.File file, long position)
			: base (file, position)
		{
			if (!Guid.Equals (Asf.Guid.AsfFilePropertiesObject))
				throw new CorruptFileException (
					"Object GUID incorrect.");
			
			if (OriginalSize < 104)
				throw new CorruptFileException (
					"Object size too small.");
			
			file_id = file.ReadGuid ();
			file_size = file.ReadQWord ();
			creation_date = file.ReadQWord ();
			data_packets_count = file.ReadQWord ();
			send_duration = file.ReadQWord ();
			play_duration = file.ReadQWord ();
			preroll = file.ReadQWord ();
			flags = file.ReadDWord ();
			minimum_data_packet_size = file.ReadDWord ();
			maximum_data_packet_size = file.ReadDWord ();
			maximum_bitrate = file.ReadDWord ();
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		/// <summary>
		///    Gets the GUID for the file described by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="System.Guid" /> value containing the GUID
		///    for the file described by the current instance.
		/// </value>
		public System.Guid FileId {
			get {return file_id;}
		}
		
		/// <summary>
		///    Gets the size of the file described by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="ulong" /> value containing the size of the
		///    file described by the current instance.
		/// </value>
		public ulong FileSize {
			get {return file_size;}
		}
		
		/// <summary>
		///    Gets the creation date of the file described by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="DateTime" /> value containing the creation
		///    date of the file described by the current instance.
		/// </value>
		public DateTime CreationDate {
			get {return new DateTime ((long)creation_date);}
		}
		
		/// <summary>
		///    Gets the number of data packets in the file described by
		///    the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="ulong" /> value containing the number of
		///    data packets in the file described by the current
		///    instance.
		/// </value>
		public ulong DataPacketsCount {
			get {return data_packets_count;}
		}
		
		/// <summary>
		///    Gets the play duration of the file described by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="TimeSpan" /> value containing the play
		///    duration of the file described by the current instance.
		/// </value>
		public TimeSpan PlayDuration {
			get {return new TimeSpan ((long)play_duration);}
		}
		
		/// <summary>
		///    Gets the send duration of the file described by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="TimeSpan" /> value containing the send
		///    duration of the file described by the current instance.
		/// </value>
		public TimeSpan SendDuration {
			get {return new TimeSpan ((long)send_duration);}
		}
		
		/// <summary>
		///    Gets the pre-roll of the file described by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="ulong" /> value containing the pre-roll of
		///    the file described by the current instance.
		/// </value>
		public ulong Preroll {
			get {return preroll;}
		}
		
		/// <summary>
		///    Gets the flags of the file described by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value containing the flags of the
		///    file described by the current instance.
		/// </value>
		public uint Flags {
			get {return flags;}
		}
		
		/// <summary>
		///    Gets the minimum data packet size of the file described
		///    by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value containing the minimum data
		///    packet size of the file described by the current
		///    instance.
		/// </value>
		public uint MinimumDataPacketSize {
			get {return minimum_data_packet_size;}
		}
		
		/// <summary>
		///    Gets the maximum data packet size of the file described
		///    by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value containing the maximum data
		///    packet size of the file described by the current
		///    instance.
		/// </value>
		public uint MaximumDataPacketSize {
			get {return maximum_data_packet_size;}
		}
		
		/// <summary>
		///    Gets the maximum bitrate of the file described by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value containing the maximum
		///    bitrate of the file described by the current instance.
		/// </value>
		public uint MaximumBitrate {
			get {return maximum_bitrate;}
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
			ByteVector output = file_id.ToByteArray ();
			output.Add (RenderQWord (file_size));
			output.Add (RenderQWord (creation_date));
			output.Add (RenderQWord (data_packets_count));
			output.Add (RenderQWord (send_duration));
			output.Add (RenderQWord (play_duration));
			output.Add (RenderQWord (preroll));
			output.Add (RenderDWord (flags));
			output.Add (RenderDWord (minimum_data_packet_size));
			output.Add (RenderDWord (maximum_data_packet_size));
			output.Add (RenderDWord (maximum_bitrate));
			
			return Render (output);
		}
		
		#endregion
	}
}
