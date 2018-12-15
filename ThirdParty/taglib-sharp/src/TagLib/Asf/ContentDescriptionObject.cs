//
// ContentDescriptionObject.cs: Provides a representation of an ASF Content
// Description object which can be read from and written to disk.
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
	///    representation of an ASF Content Description object which can be
	///    read from and written to disk.
	/// </summary>
	public class ContentDescriptionObject : Object
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the media title.
		/// </summary>
		private string title = string.Empty;
		
		/// <summary>
		///    Contains the author/performer.
		/// </summary>
		private string author = string.Empty;
		
		/// <summary>
		///    Contains the copyright information.
		/// </summary>
		private string copyright = string.Empty;
		
		/// <summary>
		///    Contains the description of the media.
		/// </summary>
		private string description = string.Empty;
		
		/// <summary>
		///    Contains the rating of the media.
		/// </summary>
		private string rating = string.Empty;
		
		#endregion
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="ContentDescriptionObject" /> by reading the
		///    contents from a specified position in a specified file.
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
		public ContentDescriptionObject (Asf.File file, long position)
			: base (file, position)
		{
			if (Guid != Asf.Guid.AsfContentDescriptionObject)
				throw new CorruptFileException (
					"Object GUID incorrect.");
			
			if (OriginalSize < 34)
				throw new CorruptFileException (
					"Object size too small.");
			
			ushort title_length = file.ReadWord ();
			ushort author_length = file.ReadWord ();
			ushort copyright_length = file.ReadWord ();
			ushort description_length = file.ReadWord ();
			ushort rating_length = file.ReadWord ();
			
			title = file.ReadUnicode (title_length);
			author = file.ReadUnicode (author_length);
			copyright = file.ReadUnicode (copyright_length);
			description = file.ReadUnicode (description_length);
			rating = file.ReadUnicode (rating_length);
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="ContentDescriptionObject" /> with no contents.
		/// </summary>
		public ContentDescriptionObject ()
			: base (Asf.Guid.AsfContentDescriptionObject)
		{
		}
		
		#endregion
		
		
		
		#region Public Region
		
		/// <summary>
		///    Gets and sets the title of the media described by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the title of
		///    the media or <see langword="null" /> if it is not set.
		/// </value>
		public string Title {
			get {return title.Length == 0 ? null : title;}
			set {
				title = string.IsNullOrEmpty (value) ?
					string.Empty : value;
			}
		}
		
		/// <summary>
		///    Gets and sets the author or performer of the media
		///    described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the author of
		///    the media or <see langword="null" /> if it is not set.
		/// </value>
		public string Author {
			get {return author.Length == 0 ? null : author;}
			set {
				author = string.IsNullOrEmpty (value) ?
					string.Empty : value;
			}
		}
		
		/// <summary>
		///    Gets and sets the copyright information for the media
		///    described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the copyright
		///    information for the media or <see langword="null" /> if
		///    it is not set.
		/// </value>
		public string Copyright {
			get {return copyright.Length == 0 ? null : copyright;}
			set {
				copyright = string.IsNullOrEmpty (value) ?
					string.Empty : value;
			}
		}
		
		/// <summary>
		///    Gets and sets the description of the media described by
		///    the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing a description
		///    of the media or <see langword="null" /> if it is not set.
		/// </value>
		public string Description {
			get {
				return description.Length == 0 ?
					null : description;
			}
			set {
				description = string.IsNullOrEmpty (value) ?
					string.Empty : value;
			}
		}
		
		/// <summary>
		///    Gets and sets the rating of the media described by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing a rating of the
		///    media or <see langword="null" /> if it is not set.
		/// </value>
		public string Rating {
			get {return rating.Length == 0 ? null : rating;}
			set {
				rating = string.IsNullOrEmpty (value) ?
					string.Empty : value;
			}
		}
		
		/// <summary>
		///    Gets whether or not the current instance is empty.
		/// </summary>
		/// <value>
		///    <see langword="true" /> if all the values are cleared.
		///    Otherwise <see langword="false" />.
		/// </value>
		public bool IsEmpty {
			get {
				return title.Length == 0 &&
				author.Length == 0 &&
				copyright.Length == 0 &&
				description.Length == 0 &&
				rating.Length == 0;
			}
		}
		
		#endregion
		
		
		
		#region Public Region
		
		/// <summary>
		///    Renders the current instance as a raw ASF object.
		/// </summary>
		/// <returns>
		///    A <see cref="ByteVector" /> object containing the
		///    rendered version of the current instance.
		/// </returns>
		public override ByteVector Render ()
		{
			ByteVector title_bytes = RenderUnicode (title);
			ByteVector author_bytes = RenderUnicode (author);
			ByteVector copyright_bytes = RenderUnicode (copyright);
			ByteVector description_bytes =
				RenderUnicode (description);
			ByteVector rating_bytes = RenderUnicode (rating);
			
			ByteVector output = RenderWord ((ushort)
				title_bytes.Count);
			output.Add (RenderWord ((ushort) author_bytes.Count));
			output.Add (RenderWord ((ushort) copyright_bytes.Count));
			output.Add (RenderWord ((ushort)
				description_bytes.Count));
			output.Add (RenderWord ((ushort) rating_bytes.Count));
			output.Add (title_bytes);
			output.Add (author_bytes);
			output.Add (copyright_bytes);
			output.Add (description_bytes);
			output.Add (rating_bytes);
			
			return Render (output);
		}
		
		#endregion
	}
}
