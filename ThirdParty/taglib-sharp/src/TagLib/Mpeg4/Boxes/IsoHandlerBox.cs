//
// IsoHandlerBox.cs: Provides an implementation of a ISO/IEC 14496-12
// HandlerBox.
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
	///    This class extends <see cref="FullBox" /> to provide an
	///    implementation of a ISO/IEC 14496-12 FullBox.
	/// </summary>
	public class IsoHandlerBox : FullBox
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the handler type.
		/// </summary>
		private ByteVector handler_type;
		
		/// <summary>
		///    Contains the handler name.
		/// </summary>
		private string name;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="IsoHandlerBox" /> with a provided header and
		///    handler by reading the contents from a specified file.
		/// </summary>
		/// <param name="header">
		///    A <see cref="BoxHeader" /> object containing the header
		///    to use for the new instance.
		/// </param>
		/// <param name="file">
		///    A <see cref="TagLib.File" /> object to read the contents
		///    of the box from.
		/// </param>
		/// <param name="handler">
		///    A <see cref="IsoHandlerBox" /> object containing the
		///    handler that applies to the new instance.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="file" /> is <see langword="null" />.
		/// </exception>
		public IsoHandlerBox (BoxHeader header, TagLib.File file,
		                      IsoHandlerBox handler)
			: base (header, file, handler)
		{
			if (file == null)
				throw new System.ArgumentNullException ("file");
			
			file.Seek (DataPosition + 4);
			ByteVector box_data = file.ReadBlock (DataSize - 4);
			handler_type = box_data.Mid (0, 4);
			
			int end = box_data.Find ((byte) 0, 16);
			if (end < 16)
				end = box_data.Count;
			name = end > 16 ? box_data.ToString (StringType.UTF8, 16, end - 16) : string.Empty;
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="IsoHandlerBox" /> with a specified type and name.
		/// </summary>
		/// <param name="handlerType">
		///    A <see cref="ByteVector" /> object specifying a 4 byte
		///    handler type.
		/// </param>
		/// <param name="name">
		///    A <see cref="string" /> object specifying the handler
		///    name.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="handlerType" /> is <see langword="null"
		///    />.
		/// </exception>
		/// <exception cref="ArgumentException">
		///    <paramref name="handlerType" /> is less than 4 bytes
		///    long.
		/// </exception>
		public IsoHandlerBox (ByteVector handlerType, string name)
			: base ("hdlr", 0, 0)
		{
			if (handlerType == null)
				throw new ArgumentNullException ("handlerType");
			
			if (handlerType.Count < 4)
				throw new ArgumentException (
					"The handler type must be four bytes long.",
					"handlerType");
			
			this.handler_type = handlerType.Mid (0,4);
			this.name = name;
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		/// <summary>
		///    Gets the data contained in the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="ByteVector" /> object containing the
		///    rendered version of the data contained in the current
		///    instance.
		/// </value>
		public override ByteVector Data {
			get {
				ByteVector output = new ByteVector (4);
				output.Add (handler_type);
				output.Add (new ByteVector (12));
				output.Add (ByteVector.FromString (name,
					StringType.UTF8));
				output.Add (new ByteVector (2));
				return output;
			}
		}
		
		/// <summary>
		///    Gets the handler type of the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="ByteVector" /> object containing the
		///    handler type of the current instance.
		/// </value>
		public ByteVector HandlerType {
			get {return handler_type;}
		}
		
		/// <summary>
		///    Gets the name of the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the name of the
		///    current instance.
		/// </value>
		public string Name {
			get {return name;}
		}
		
		#endregion
	}
}
