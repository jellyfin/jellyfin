//
// IFDEntry.cs:
//
// Author:
//   Ruben Vermeersch (ruben@savanne.be)
//   Mike Gemuende (mike@gemuende.de)
//
// Copyright (C) 2009 Ruben Vermeersch
// Copyright (C) 2009 Mike Gemuende
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

namespace TagLib.IFD
{
	/// <summary>
	///    An IFD entry, which is a key/value pair inside an IFD.
	/// </summary>
	public interface IFDEntry
	{

#region Properties

		/// <value>
		///    The ID of the tag, the current instance belongs to
		/// </value>
		ushort Tag {
			get;
		}

#endregion

#region Methods

		/// <summary>
		///    Renders the current instance to a <see cref="ByteVector"/>
		/// </summary>
		/// <param name="is_bigendian">
		///    A <see cref="System.Boolean"/> indicating the endianess for rendering.
		/// </param>
		/// <param name="offset">
		///    A <see cref="System.UInt32"/> with the offset, the data is stored.
		/// </param>
		/// <param name="type">
		///    A <see cref="System.UInt16"/> the ID of the type, which is rendered
		/// </param>
		/// <param name="count">
		///    A <see cref="System.UInt32"/> with the count of the values which are
		///    rendered.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector"/> with the rendered data.
		/// </returns>
		ByteVector Render (bool is_bigendian, uint offset, out ushort type, out uint count);

#endregion

	}


	/// <summary>
	///    This class abstracts common stuff for array IFD entries
	/// </summary>
	public abstract class ArrayIFDEntry<T> : IFDEntry
	{

#region Properties

		/// <value>
		///    The ID of the tag, the current instance belongs to
		/// </value>
		public ushort Tag { get; private set; }

		/// <value>
		///    The values stored by the current instance.
		/// </value>
		public T [] Values { get; protected set; }

#endregion

#region Constructors

		/// <summary>
		///    Constructor.
		/// </summary>
		/// <param name="tag">
		///    A <see cref="System.UInt16"/> with the tag ID of the entry this instance
		///    represents
		/// </param>
		public ArrayIFDEntry (ushort tag)
		{
			Tag = tag;
		}

#endregion

#region Public Methods

		/// <summary>
		///    Renders the current instance to a <see cref="ByteVector"/>
		/// </summary>
		/// <param name="is_bigendian">
		///    A <see cref="System.Boolean"/> indicating the endianess for rendering.
		/// </param>
		/// <param name="offset">
		///    A <see cref="System.UInt32"/> with the offset, the data is stored.
		/// </param>
		/// <param name="type">
		///    A <see cref="System.UInt16"/> the ID of the type, which is rendered
		/// </param>
		/// <param name="count">
		///    A <see cref="System.UInt32"/> with the count of the values which are
		///    rendered.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector"/> with the rendered data.
		/// </returns>
		public abstract ByteVector Render (bool is_bigendian, uint offset, out ushort type, out uint count);

#endregion

	}
}
