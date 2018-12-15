//
// InfoTag.cs:
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
namespace TagLib.Riff
{
	/// <summary>
	///    This class extends <see cref="ListTag" /> to provide support for
	///    reading and writing standard INFO tags.
	/// </summary>
	public class InfoTag : ListTag
	{
#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="InfoTag" /> with no contents.
		/// </summary>
		public InfoTag () : base ()
		{
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="InfoTag" /> by reading the contents of a raw
		///    RIFF list stored in a <see cref="ByteVector" /> object.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector"/> containing a raw RIFF list to
		///    read into the new instance.
		/// </param>
		public InfoTag (ByteVector data) : base (data)
		{
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="InfoTag" /> by reading the contents of a raw RIFF
		///    list from a specified position in a <see
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
		public InfoTag (TagLib.File file, long position, int length)
			: base (file, position, length)
		{
		}
		
#endregion
		
		
		
#region Public Methods
		
		/// <summary>
		///    Renders the current instance enclosed in a "INFO" item.
		/// </summary>
		/// <returns>
		///    A <see cref="ByteVector"/> object containing the rendered
		///    version of the current instance.
		/// </returns>
		public override ByteVector RenderEnclosed ()
		{
		return RenderEnclosed ("INFO");
		}
		
#endregion
		
		
		
#region TagLib.Tag
		
		/// <summary>
		///    Gets the tag types contained in the current instance.
		/// </summary>
		/// <value>
		///    Always <see cref="TagTypes.RiffInfo" />.
		/// </value>
		public override TagTypes TagTypes {
			get {return TagTypes.RiffInfo;}
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
		///    This property is implemented using the "INAM" item.
		/// </remarks>
		public override string Title {
			get {
				foreach (string s in GetValuesAsStrings ("INAM"))
					if (!string.IsNullOrEmpty (s))
						return s;
				
				return null;
			}
			set {SetValue ("INAM", value);}
		}

		/// <summary>
		///    Gets and sets a short description of the media.
		///    For a music, this could be the comment that the artist
		///    made of its artwork. For a video, this should be a 
		///    short summary of the story/plot, but a spoiler. This
		///    should give the impression of what to expect in the
		///    media.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the subtitle
		///    the media represented by the current instance 
		///    or an empty array if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "ISBJ" item.
		/// </remarks>
		public override string Description
		{
			get
			{
				foreach (string s in GetValuesAsStrings("ISBJ"))
					if (!string.IsNullOrEmpty(s))
						return s;

				return null;
			}
			set { SetValue("ISBJ", value); }
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
		///    This property is implemented using the "ISTR" item.
		/// </remarks>
		public override string [] Performers {
			get {return GetValuesAsStrings ("ISTR");}
			set {SetValue ("ISTR", value);}
		}

		/// <summary>
		///    Gets and sets the band or artist who is credited in the
		///    creation of the entire album or collection containing the
		///    media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing the band or artist
		///    who is credited in the creation of the entire album or
		///    collection containing the media described by the current
		///    instance or an empty array if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "IART" item.
		/// </remarks>
		public override string [] AlbumArtists {
			get {return GetValuesAsStrings ("IART");}
			set {SetValue ("IART", value);}
		}

		/// <summary>
		///    Gets and sets the composers of the media represented by
		///    the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing the composers of the
		///    media represented by the current instance or an empty
		///    array if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "IWRI" item.
		/// </remarks>
		public override string [] Composers {
			get {return GetValuesAsStrings ("IWRI");}
			set {SetValue ("IWRI", value);}
		}


		/// <summary>
		///    Gets and sets the album of the media represented by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the album of
		///    the media represented by the current instance or <see
		///    langword="null" /> if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the non-standard
		///    "DIRC" (Directory) item.
		/// </remarks>
		public override string Album
		{
			get
			{
				foreach (string s in GetValuesAsStrings("DIRC"))
					if (!string.IsNullOrEmpty(s))
						return s;

				return null;
			}
			set { SetValue("DIRC", value); }
		}

		/// <summary>
		///    Gets and sets the conductor or director of the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the conductor
		///    or director of the media represented by the current
		///    instance or <see langword="null" /> if no value present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "ICNM" 
		///    (Cinematographer) non-standard item.
		/// </remarks>
		public override string Conductor
		{
			get
			{
				foreach (string s in GetValuesAsStrings("ICNM"))
					if (!string.IsNullOrEmpty(s))
						return s;

				return null;
			}
			set { SetValue("ICNM", value); }
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
		///    This property is implemented using the "ICMT" item.
		/// </remarks>
		public override string Comment {
			get {
				foreach (string s in GetValuesAsStrings ("ICMT"))
					if (!string.IsNullOrEmpty (s))
						return s;
				
				return null;
			}
			set {SetValue ("ICMT", value);}
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
		///    This property is implemented using the "IGNR" item.
		/// </remarks>
		public override string [] Genres {
			get {return GetValuesAsStrings ("IGNR");}
			set {SetValue ("IGNR", value);}
		}

		/// <summary>
		///    Gets and sets the year that the media represented by the
		///    current instance was recorded.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> containing the year that the media
		///    represented by the current instance was created or zero
		///    if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "ICRD" item.
		/// </remarks>
		public override uint Year {
			get {return GetValueAsUInt ("ICRD");}
			set {SetValue ("ICRD", value);}
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
		///    This property is implemented using the "IPRT" item.
		/// </remarks>
		public override uint Track {
			get {return GetValueAsUInt ("IPRT");}
			set {SetValue ("IPRT", value);}
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
		///    This property is implemented using the "IFRM" item.
		/// </remarks>
		public override uint TrackCount {
			get {return GetValueAsUInt ("IFRM");}
			set {SetValue ("IFRM", value);}
		}

		/// <summary>
		///    Gets and sets the copyright information for the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the copyright
		///    information for the media represented by the current
		///    instance or <see langword="null" /> if no value present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "ICOP" item.
		/// </remarks>
		public override string Copyright {
			get {
				foreach (string s in GetValuesAsStrings ("ICOP"))
					if (!string.IsNullOrEmpty (s))
						return s;
				
				return null;
			}
			set {SetValue ("ICOP", value);}
		}
#endregion
	}
}