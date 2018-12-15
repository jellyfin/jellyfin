//
//  UrlBox.cs
//
//  Author:
//       Alan McGovern <alan.mcgovern@gmail.com>
//
//  Copyright (c) 2012 Alan McGovern
//
//  This library is free software; you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as
//  published by the Free Software Foundation; either version 2.1 of the
//  License, or (at your option) any later version.
//
//  This library is distributed in the hope that it will be useful, but
//  WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//  Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public
//  License along with this library; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
using System;
using TagLib.Mpeg4;

namespace TagLib
{
	/// <summary>
	/// Represent a MP4 URL box
	/// </summary>
	public class UrlBox : Box
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the box's data.
		/// </summary>
		private ByteVector data;
		
#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="UnknownBox" /> with a provided header and handler
		///    by reading the contents from a specified file.
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
		public UrlBox (BoxHeader header, TagLib.File file,
		                IsoHandlerBox handler)
			: base (header, handler)
		{
			if (file == null)
				throw new ArgumentNullException ("file");
			
			this.data = LoadData (file);
		}
		
#endregion
		
		
		
		#region Public Properties
		
		/// <summary>
		///    Gets and sets the box data contained in the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="ByteVector" /> object containing the box
		///    data contained in the current instance.
		/// </value>
		public override ByteVector Data {
			get {return data;}
			set {data = value;}
		}
		
#endregion
	}
}

