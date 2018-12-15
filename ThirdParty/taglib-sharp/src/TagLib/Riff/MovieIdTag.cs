//
// MovieIdTag.cs:
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Copyright (C) 2007 Brian Nickel
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

namespace TagLib.Riff {
	/// <summary>
	///    This class extends <see cref="ListTag" /> to provide support for
	///    reading and writing MovieID tags.
	/// </summary>
	public class MovieIdTag : ListTag
	{
#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="MovieIdTag" /> with no contents.
		/// </summary>
		public MovieIdTag () : base ()
		{
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="MovieIdTag" /> by reading the contents of a raw
		///    RIFF list stored in a <see cref="ByteVector" /> object.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector"/> containing a raw RIFF list to
		///    read into the new instance.
		/// </param>
		public MovieIdTag (ByteVector data) : base (data)
		{
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="MovieIdTag" /> by reading the contents of a raw
		///    RIFF list from a specified position in a <see
		///    cref="TagLib.File"/>.
		/// </summary>
		/// <param name="file">
		///    A <see cref="TagLib.File" /> object containing the file
		///    from which the contents of the new instance is to be
		///    read.
		/// </param>
		/// <param name="position">
		///    A <see cref="long" /> value specify at what position to
		///    read the list.
		/// </param>
		/// <param name="length">
		///    A <see cref="int" /> value specifying the number of bytes
		///    to read.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="file" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///    <paramref name="position" /> is less than zero or greater
		///    than the size of the file.
		/// </exception>
		public MovieIdTag (TagLib.File file, long position, int length)
			: base (file, position, length)
		{
		}
		
#endregion
		
		
		
#region Public Methods
		
		/// <summary>
		///    Renders the current instance enclosed in a "MID " item.
		/// </summary>
		/// <returns>
		///    A <see cref="ByteVector"/> object containing the rendered
		///    version of the current instance.
		/// </returns>
		public override ByteVector RenderEnclosed ()
		{
			return RenderEnclosed ("MID ");
		}
		
#endregion
		
		
		
#region TagLib.Tag
		
		/// <summary>
		///    Gets the tag types contained in the current instance.
		/// </summary>
		/// <value>
		///    Always <see cref="TagTypes.MovieId" />.
		/// </value>
		public override TagTypes TagTypes {
			get {return TagTypes.MovieId;}
		}
		
		/// <summary>
		///    Gets and sets the title for the media described by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the title for
		///    the media described by the current instance or <see
		///    langword="null" /> if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "TITL" item.
		/// </remarks>
		public override string Title {
			get {
				foreach (string s in GetValuesAsStrings ("TITL"))
					if (!string.IsNullOrEmpty (s))
						return s;
				
				return null;
			}
			set {SetValue ("TITL", value);}
		}

		/// <summary>
		///    Gets and sets the performers or artists who performed in
		///    the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing the performers or
		///    artists who performed in the media described by the
		///    current instance or an empty array if no value is
		///    present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "IART" item.
		/// </remarks>
		public override string [] Performers {
			get {return GetValuesAsStrings ("IART");}
			set {SetValue ("IART", value);}
		}
		
		/// <summary>
		///    Gets and sets a user comment on the media represented by
		///    the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing user comments
		///    on the media represented by the current instance or <see
		///    langword="null" /> if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "COMM" item.
		/// </remarks>
		public override string Comment {
			get {
				foreach (string s in GetValuesAsStrings ("COMM"))
					if (!string.IsNullOrEmpty (s))
						return s;
				
				return null;
			}
			set {SetValue ("COMM", value);}
		}
		
		/// <summary>
		///    Gets and sets the genres of the media represented by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing the genres of the
		///    media represented by the current instance or an empty
		///    array if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "GENR" item.
		/// </remarks>
		public override string [] Genres {
			get {return GetValuesAsStrings ("GENR");}
			set {SetValue ("GENR", value);}
		}
		
		/// <summary>
		///    Gets and sets the position of the media represented by
		///    the current instance in its containing album.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> containing the position of the
		///    media represented by the current instance in its
		///    containing album or zero if not specified.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "PRT1" item.
		/// </remarks>
		public override uint Track {
			get {return GetValueAsUInt ("PRT1");}
			set {SetValue ("PRT1", value);}
		}
		
		/// <summary>
		///    Gets and sets the number of tracks in the album
		///    containing the media represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> containing the number of tracks in
		///    the album containing the media represented by the current
		///    instance or zero if not specified.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "PRT2" item.
		/// </remarks>
		public override uint TrackCount {
			get {return GetValueAsUInt ("PRT2");}
			set {SetValue ("PRT2", value);}
		}
#endregion
	}
}
