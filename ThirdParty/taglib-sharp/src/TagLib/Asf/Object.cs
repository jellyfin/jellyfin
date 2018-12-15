//
// Object.cs: Provides a basic representation of an ASF object which can be read
// from and written to disk.
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
	///    This abstract class provides a basic representation of an ASF
	///    object which can be read from and written to disk.
	/// </summary>
	public abstract class Object
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the GUID of the object.
		/// </summary>
		private System.Guid id;
		
		/// <summary>
		///    Contains the size of the object on disk.
		/// </summary>
		private ulong size;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Object" /> by reading the contents from a
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
		protected Object (Asf.File file, long position)
		{
			if (file == null)
				throw new ArgumentNullException ("file");
			
			if (position < 0 ||
				position > file.Length - 24)
				throw new ArgumentOutOfRangeException (
					"position");
			
			file.Seek (position);
			id = file.ReadGuid ();
			size = file.ReadQWord ();
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Object" /> with a specified GUID.
		/// </summary>
		/// <param name="guid">
		///    A <see cref="System.Guid" /> value containing the GUID to
		///    use for the new instance.
		/// </param>
		protected Object (System.Guid guid)
		{
			id = guid;
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		/// <summary>
		///    Gets the GUID for the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="System.Guid" /> object containing the GUID
		///    of the current instance.
		/// </value>
		public System.Guid Guid {
			get {return id;}
		}
		
		/// <summary>
		///    Gets the original size of the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="ulong" /> value containing the size of the
		///    current instance as it originally appeared on disk.
		/// </value>
		public ulong OriginalSize {
			get {return size;}
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
		/// <seealso cref="Render(ByteVector)" />
		public abstract ByteVector Render ();
		
		#endregion
		
		
		
		#region Public Static Methods
		
		/// <summary>
		///    Renders a Unicode (wide) string.
		/// </summary>
		/// <param name="value">
		///    A <see cref="string" /> object containing the text to
		///    render.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector" /> object containing the
		///    rendered value.
		/// </returns>
		public static ByteVector RenderUnicode (string value)
		{
			ByteVector v = ByteVector.FromString (value,
				StringType.UTF16LE);
			v.Add (RenderWord (0));
			return v;
		}
		
		/// <summary>
		///    Renders a 4-byte DWORD.
		/// </summary>
		/// <param name="value">
		///    A <see cref="uint" /> value containing the DWORD to
		///    render.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector" /> object containing the
		///    rendered value.
		/// </returns>
		public static ByteVector RenderDWord (uint value)
		{
			return ByteVector.FromUInt (value, false);
		}
		
		/// <summary>
		///    Renders a 8-byte QWORD.
		/// </summary>
		/// <param name="value">
		///    A <see cref="ulong" /> value containing the QWORD to
		///    render.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector" /> object containing the
		///    rendered value.
		/// </returns>
		public static ByteVector RenderQWord (ulong value)
		{
			return ByteVector.FromULong (value, false);
		}
		
		/// <summary>
		///    Renders a 2-byte WORD.
		/// </summary>
		/// <param name="value">
		///    A <see cref="ushort" /> value containing the WORD to
		///    render.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector" /> object containing the
		///    rendered value.
		/// </returns>
		public static ByteVector RenderWord (ushort value)
		{
			return ByteVector.FromUShort (value, false);
		}
		
		#endregion
		
		
		
		#region Protected Methods
		
		/// <summary>
		///    Renders the current instance as a raw ASF object
		///    containing specified data.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the data to
		///    contained in the rendered version of the current
		///    instance.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector" /> object containing the
		///    rendered version of the current instance.
		/// </returns>
		/// <remarks>
		///    Child classes implementing <see cref="Render()" /> should
		///    render their contents and then send the data through this
		///    method to produce the final output.
		/// </remarks>
		protected ByteVector Render (ByteVector data)
		{
			ulong length = (ulong)
				((data != null ? data.Count : 0) + 24);
			ByteVector v = id.ToByteArray ();
			v.Add (RenderQWord (length));
			v.Add (data);
			return v;
		}
		
		#endregion
	}
}
