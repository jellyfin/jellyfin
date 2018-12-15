//
// StartTag.cs: Provides support for accessing and modifying a collection of
// tags appearing at the start of a file.
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Copyright (C) 2007 Brian Nickel
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

namespace TagLib.NonContainer {
	/// <summary>
	///    This class extends <see cref="CombinedTag" />, providing support
	///    for accessing and modifying a collection of tags appearing at the
	///    start of a file.
	/// </summary>
	/// <remarks>
	///    <para>This class is used by <see cref="TagLib.NonContainer.File"
	///    /> to read all the tags appearing at the start of the file but
	///    could be used by other classes. It currently supports ID3v2
	///    and APE tags.</para>
	/// </remarks>
	public class StartTag : CombinedTag
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the file to operate on.
		/// </summary>
		private TagLib.File file;
		
		/// <summary>
		///    Contains the number of bytes that must be read to
		///    hold all applicable indicators.
		/// </summary>
		int read_size = (int) Math.Max (TagLib.Ape.Footer.Size,
			TagLib.Id3v2.Header.Size);
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="StartTag" /> for a specified <see
		///    cref="TagLib.File" />.
		/// </summary>
		/// <param name="file">
		///    A <see cref="TagLib.File" /> object on which the new
		///    instance will perform its operations.
		/// </param>
		/// <remarks>
		///    Constructing a new instance does not automatically read
		///    the contents from the disk. <see cref="Read" /> must be
		///    called to read the tags.
		/// </remarks>
		public StartTag (TagLib.File file) : base ()
		{
			this.file = file;
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		/// <summary>
		///    Gets the total size of the tags located at the end of the
		///    file by reading from the file.
		/// </summary>
		public long TotalSize {
			get {
				long size = 0;
				
				while (ReadTagInfo (ref size) != TagTypes.None)
					;
				
				return size;
			}
		}
		
		#endregion
		
		
		
		#region Public Methods
		
		/// <summary>
		///    Reads the tags stored at the start of the file into the
		///    current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="long" /> value indicating the seek position
		///    in the file at which the read tags end. This also
		///    marks the seek position at which the media begins.
		/// </returns>
		public long Read (ReadStyle style)
		{
			TagLib.Tag tag;
			ClearTags ();
			long end = 0;
			
			while ((tag = ReadTag (ref end, style)) != null)
				AddTag (tag);
			
			return end;
		}
		
		/// <summary>
		///    Renders the tags contained in the current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="ByteVector" /> object containing the
		///    physical representation of the tags stored in the current
		///    instance.
		/// </returns>
		/// <remarks>
		///    The tags are rendered in the order that they are stored
		///    in the current instance.
		/// </remarks>
		public ByteVector Render ()
		{
			ByteVector data = new ByteVector ();
			foreach (TagLib.Tag t in Tags) {
				if (t is TagLib.Ape.Tag)
					data.Add ((t as TagLib.Ape.Tag).Render ());
				else if (t is TagLib.Id3v2.Tag)
					data.Add ((t as TagLib.Id3v2.Tag).Render ());
			}
			
			return data;
		}
		
		/// <summary>
		///    Writes the tags contained in the current instance to the
		///    beginning of the file that created it, overwriting the
		///    existing tags.
		/// </summary>
		/// <returns>
		///    A <see cref="long" /> value indicating the seek position
		///    in the file at which the written tags end. This also
		///    marks the seek position at which the media begins.
		/// </returns>
		public long Write ()
		{
			ByteVector data = Render ();
			file.Insert (data, 0, TotalSize);
			return data.Count;
		}
		
		/// <summary>
		///    Removes a set of tag types from the current instance.
		/// </summary>
		/// <param name="types">
		///    A bitwise combined <see cref="TagLib.TagTypes" /> value
		///    containing tag types to be removed from the file.
		/// </param>
		/// <remarks>
		///    In order to remove all tags from a file, pass <see
		///    cref="TagTypes.AllTags" /> as <paramref name="types" />.
		/// </remarks>
		public void RemoveTags (TagTypes types)
		{
			for (int i = Tags.Length - 1; i >= 0; i--) {
				var tag = Tags[i];
				if (types == TagTypes.AllTags || (tag.TagTypes & types) == tag.TagTypes) {
					RemoveTag (tag);
				}
			}
		}
		
		/// <summary>
		///    Adds a tag of a specified type to the current instance,
		///    optionally copying values from an existing type.
		/// </summary>
		/// <param name="type">
		///    A <see cref="TagTypes" /> value specifying the type of
		///    tag to add to the current instance. At the time of this
		///    writing, this is limited to <see cref="TagTypes.Ape" />
		///    and <see cref="TagTypes.Id3v2" />.
		/// </param>
		/// <param name="copy">
		///    A <see cref="TagLib.Tag" /> to copy values from using
		///    <see cref="TagLib.Tag.CopyTo" />, or <see
		///    langword="null" /> if no tag is to be copied.
		/// </param>
		/// <returns>
		///    The <see cref="TagLib.Tag" /> object added to the current
		///    instance, or <see langword="null" /> if it couldn't be
		///    created.
		/// </returns>
		/// <remarks>
		///    ID3v2 tags are added at the end of the current instance,
		///    while other tags are added to the beginning.
		/// </remarks>
		public TagLib.Tag AddTag (TagTypes type, TagLib.Tag copy)
		{
			TagLib.Tag tag = null;
			
			if (type == TagTypes.Id3v2) {
				tag = new TagLib.Id3v2.Tag ();
			} else if (type == TagTypes.Ape) {
				tag = new TagLib.Ape.Tag ();
				(tag as Ape.Tag).HeaderPresent = true;
			}
			
			if (tag != null) {
				if (copy != null)
					copy.CopyTo (tag, true);
				
				AddTag (tag);
			}
			
			return tag;
		}

		#endregion



		#region Private Methods

		/// <summary>
		///    Reads a tag starting at a specified position and moves the
		///    cursor to its start position.
		/// </summary>
		/// <param name="start">
		///    A <see cref="long" /> value reference specifying at what
		///    position the potential tag starts. If a tag is found,
		///    this value will be updated to the position at which the
		///    found tag ends.
		/// </param>
		/// <param name="style">
		///    A <see cref="ReadStyle"/> value specifying how the media
		///    data is to be read into the current instance.
		/// </param>
		/// <returns>
		///    A <see cref="TagLib.Tag" /> object representing the tag
		///    found at the specified position, or <see langword="null"
		///    /> if no tag was found.
		/// </returns>
		private TagLib.Tag ReadTag (ref long start, ReadStyle style)
		{
			long end = start;
			TagTypes type = ReadTagInfo (ref end);
			TagLib.Tag tag = null;
			
			switch (type) {
				case TagTypes.Ape:
					tag = new TagLib.Ape.Tag (file, start);
					break;
				case TagTypes.Id3v2:
					tag = new TagLib.Id3v2.Tag (file, start, style);
					break;
			}

			start = end;
			return tag;
		}
		
		/// <summary>
		///    Looks for a tag starting at a specified position and moves
		///    the cursor to its start position.
		/// </summary>
		/// <param name="position">
		///    A <see cref="long" /> value reference specifying at what
		///    position the potential tag starts. If a tag is found,
		///    this value will be updated to the position at which the
		///    found tag ends.
		/// </param>
		/// <returns>
		///    A <see cref="TagLib.TagTypes" /> value specifying the
		///    type of tag found at the specified position, or <see
		///    cref="TagTypes.None" /> if no tag was found.
		/// </returns>
		private TagTypes ReadTagInfo (ref long position)
		{
			file.Seek (position);
			ByteVector data = file.ReadBlock (read_size);
			
			try {
				if (data.StartsWith (TagLib.Ape.Footer.FileIdentifier)) {
					TagLib.Ape.Footer footer =
						new TagLib.Ape.Footer (data);
					
					position += footer.CompleteTagSize;
					return TagTypes.Ape;
				}
				
				if (data.StartsWith (TagLib.Id3v2.Header.FileIdentifier)) {
					TagLib.Id3v2.Header header =
						new TagLib.Id3v2.Header (data);
					
					position += header.CompleteTagSize;
					return TagTypes.Id3v2;
				}
			} catch (CorruptFileException) {
			}
			
			return TagTypes.None;
		}
		
		#endregion
	}
}
