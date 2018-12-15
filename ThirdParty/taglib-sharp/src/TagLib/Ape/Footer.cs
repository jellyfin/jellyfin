//
// Footer.cs: Provides a representation of an APEv2 tag footer which can be read
// from and written to disk.
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Original Source:
//   apefooter.cpp from TagLib
//
// Copyright (C) 2005-2007 Brian Nickel
// Copyright (C) 2004 Allan Sandfeld Jensen (Original Implementation)
// copyright (C) 2002, 2003 Scott Wheeler (Original Implementation)
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

namespace TagLib.Ape {
	#region Enums
	
	/// <summary>
	///    Indicates the flags applied to a <see cref="Footer" /> object.
	/// </summary>
	[Flags]
	public enum FooterFlags : uint {
		/// <summary>
		///    The tag lacks a footer object.
		/// </summary>
		FooterAbsent  = 0x40000000,
		
		/// <summary>
		///    The footer is actually a header.
		/// </summary>
		IsHeader      = 0x20000000,
		
		/// <summary>
		///    The tag contains a header.
		/// </summary>
		HeaderPresent = 0x80000000
	}
	
	#endregion
	
	
	
	/// <summary>
	///    This structure provides a representation of an APEv2 tag footer
	///    which can be read from and written to disk.
	/// </summary>
	public struct Footer : IEquatable<Footer>
	{
		#region Private Properties
		
		/// <summary>
		///    Contains the APE tag version.
		/// </summary>
		private uint version;
		
		/// <summary>
		///    Contains the footer flags.
		/// </summary>
		private FooterFlags flags;
		
		/// <summary>
		///    Contains the number of items in the tag.
		/// </summary>
		private uint item_count;
		
		/// <summary>
		///    Contains the tag size including the footer but excluding
		///    the header.
		/// </summary>
		private uint tag_size;
		
		#endregion
		
		
		
		#region Public Static Fields
		
		/// <summary>
		///    Specifies the size of an APEv2 footer.
		/// </summary>
		public const uint Size = 32;
		
		/// <summary>
		///    Specifies the identifier used find an APEv2 footer in a
		///    file.
		/// </summary>
		/// <value>
		///    "<c>APETAGEX</c>"
		/// </value>
		public static readonly ReadOnlyByteVector FileIdentifier = "APETAGEX";
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Footer" /> by reading it from raw footer data.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the raw
		///    data to build the new instance from.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="data" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="CorruptFileException">
		///    <paramref name="data" /> is smaller than <see
		///    cref="Size" /> or does not begin with <see
		///    cref="FileIdentifier" />.
		/// </exception>
		public Footer (ByteVector data)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			
			if (data.Count < Size)
				throw new CorruptFileException (
					"Provided data is smaller than object size.");
			
			if (!data.StartsWith (FileIdentifier))
				throw new CorruptFileException (
					"Provided data does not start with File Identifier");
			
			version = data.Mid (8, 4).ToUInt (false);
			tag_size = data.Mid (12, 4).ToUInt (false);
			item_count = data.Mid (16, 4).ToUInt (false);
			flags = (FooterFlags) data.Mid (20, 4).ToUInt (false);
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		/// <summary>
		///    Gets the version of APE tag described by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value containing the version of the
		///    APE tag described by the current instance.
		/// </value>
		public uint Version {
			get {return version == 0 ? 2000 : version;}
		}
		
		/// <summary>
		///    Gets and sets the flags that apply to the current
		///    instance.
		/// </summary>
		/// <value>
		///    A bitwise combined <see cref="FooterFlags" /> value
		///    containing the flags that apply to the current instance.
		/// </value>
		public FooterFlags Flags {
			get {return flags;}
			set {flags = value;}
		}
		
		/// <summary>
		///    Gets and sets the number of items in the tag represented
		///    by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value containing the number of
		///    items in the tag represented by the current instance.
		/// </value>
		public uint ItemCount {
			get {return item_count;}
			set {item_count = value;}
		}
		
		/// <summary>
		///    Gets the size of the tag represented by the current
		///    instance, including the footer but excluding the header
		///    if applicable.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value containing the size of the
		///    tag represented by the current instance.
		/// </value>
		public uint TagSize {
			get {return tag_size;}
			set {tag_size = value;}
		}
		
		/// <summary>
		///    Gets the complete size of the tag represented by the
		///    current instance, including the header and footer.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> value containing the size of the
		///    tag represented by the current instance.
		/// </value>
		public uint CompleteTagSize {
			get {
				return TagSize + ((Flags &
					FooterFlags.HeaderPresent) != 0 ?
					Size : 0);
			}
		}
		
		#endregion
		
		
		
		#region Public Methods
		
		/// <summary>
		///    Renders the current instance as an APE tag footer.
		/// </summary>
		/// <returns>
		///    A <see cref="ByteVector" /> object containing the
		///    rendered version of the current instance.
		/// </returns>
		public ByteVector RenderFooter ()
		{
			return Render (false);
		}
		
		/// <summary>
		///    Renders the current instance as an APE tag header.
		/// </summary>
		/// <returns>
		///    A <see cref="ByteVector" /> object containing the
		///    rendered version of the current instance or an empty
		///    <see cref="ByteVector" /> object if <see cref="Flags" />
		///    does not include <see cref="FooterFlags.HeaderPresent"
		///    />.
		/// </returns>
		public ByteVector RenderHeader ()
		{
			return (Flags & FooterFlags.HeaderPresent) != 0 ?
				Render (true) : new ByteVector ();
		}
		
		#endregion
		
		
		
		#region Private Methods
		
		/// <summary>
		///    Renders the current instance as either an APE tag header
		///    or footer.
		/// </summary>
		/// <param name="isHeader">
		///    A <see cref="bool" /> value indicating whether or not the
		///    current instance is to be rendered as a header.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector" /> object containing the
		///    rendered version of the current instance.
		/// </returns>
		private ByteVector Render (bool isHeader)
		{
			ByteVector v = new ByteVector ();
			
			// add the file identifier -- "APETAGEX"
			v.Add (FileIdentifier);
			
			// add the version number -- we always render a 2.000
			// tag regardless of what the tag originally was.
			v.Add (ByteVector.FromUInt (2000, false));
			
			// add the tag size
			v.Add (ByteVector.FromUInt (tag_size, false));
			
			// add the item count
			v.Add (ByteVector.FromUInt (item_count, false));
			
			// render and add the flags
			uint flags = 0;
			
			if ((Flags & FooterFlags.HeaderPresent) != 0)
				flags |= (uint) FooterFlags.HeaderPresent;
			
			// footer is always present
			if (isHeader)
				flags |= (uint) FooterFlags.IsHeader;
			else
				flags &= (uint) ~FooterFlags.IsHeader;
			
			v.Add (ByteVector.FromUInt (flags, false));
			
			// add the reserved 64bit
			v.Add (ByteVector.FromULong (0));
			
			return v;
		}
		
		#endregion
		
		
		
		#region IEquatable
		
		/// <summary>
		///    Generates a hash code for the current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="int" /> value containing the hash code for
		///    the current instance.
		/// </returns>
		public override int GetHashCode ()
		{
			unchecked {
				return (int) ((uint) flags ^ tag_size ^
					item_count ^ version);
			}
		}
		
		/// <summary>
		///    Checks whether or not the current instance is equal to
		///    another object.
		/// </summary>
		/// <param name="other">
		///    A <see cref="object" /> to compare to the current
		///    instance.
		/// </param>
		/// <returns>
		///    A <see cref="bool" /> value indicating whether or not the
		///    current instance is equal to <paramref name="other" />.
		/// </returns>
		/// <seealso cref="M:System.IEquatable`1.Equals" />
		public override bool Equals (object other)
		{
			if (!(other is Footer))
				return false;
			
			return Equals ((Footer) other);
		}
		
		/// <summary>
		///    Checks whether or not the current instance is equal to
		///    another instance of <see cref="Footer" />.
		/// </summary>
		/// <param name="other">
		///    A <see cref="Footer" /> object to compare to the current
		///    instance.
		/// </param>
		/// <returns>
		///    A <see cref="bool" /> value indicating whether or not the
		///    current instance is equal to <paramref name="other" />.
		/// </returns>
		/// <seealso cref="M:System.IEquatable`1.Equals" />
		public bool Equals (Footer other)
		{
			return flags == other.flags &&
				tag_size == other.tag_size &&
				item_count == other.item_count &&
				version == other.version;
		}
		
		/// <summary>
		///    Gets whether or not two instances of <see cref="Footer"
		///    /> are equal to eachother.
		/// </summary>
		/// <param name="first">
		///    The first <see cref="Footer" /> object to compare.
		/// </param>
		/// <param name="second">
		///    The second <see cref="Footer" /> object to compare.
		/// </param>
		/// <returns>
		///    <see langword="true" /> if <paramref name="first" /> is
		///    equal to <paramref name="second" />. Otherwise, <see
		///    langword="false" />.
		/// </returns>
		public static bool operator == (Footer first, Footer second)
		{
			return first.Equals (second);
		}
		
		/// <summary>
		///    Gets whether or not two instances of <see cref="Footer"
		///    /> are unequal to eachother.
		/// </summary>
		/// <param name="first">
		///    The first <see cref="Footer" /> object to compare.
		/// </param>
		/// <param name="second">
		///    The second <see cref="Footer" /> object to compare.
		/// </param>
		/// <returns>
		///    <see langword="true" /> if <paramref name="first" /> is
		///    unequal to <paramref name="second" />. Otherwise, <see
		///    langword="false" />.
		/// </returns>
		public static bool operator != (Footer first, Footer second)
		{
			return !first.Equals (second);
		}
		
		#endregion
	}
}
