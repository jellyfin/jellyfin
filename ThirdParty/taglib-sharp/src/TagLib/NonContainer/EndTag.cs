//
// EndTag.cs: Provides support for accessing and modifying a collection of tags
// appearing at the end of a file.
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
	///    end of a file.
	/// </summary>
	/// <remarks>
	///    <para>This class is used by <see cref="TagLib.NonContainer.File"
	///    /> to read all the tags appearing at the end of the file but
	///    could be used by other classes. It currently supports ID3v1,
	///    ID3v2, and APE tags.</para>
	/// </remarks>
	public class EndTag : CombinedTag
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
		private static int read_size = (int) Math.Max (Math.Max (
			TagLib.Ape.Footer.Size, TagLib.Id3v2.Footer.Size),
			TagLib.Id3v1.Tag.Size);
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="EndTag" /> for a specified <see cref="TagLib.File"
		///    />.
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
		public EndTag (TagLib.File file) : base ()
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
				long start = file.Length;
				
				while (ReadTagInfo (ref start) != TagTypes.None)
					;
				
				return file.Length - start;
			}
		}
		
		#endregion
		
		
		
		#region Public Methods
		
		/// <summary>
		///    Reads the tags stored at the end of the file into the
		///    current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="long" /> value indicating the seek position
		///    in the file at which the read tags begin. This also
		///    marks the seek position at which the media ends.
		/// </returns>
		public long Read (ReadStyle style)
		{
			TagLib.Tag tag;
			ClearTags ();
			long start = file.Length;
			
			while ((tag = ReadTag (ref start, style)) != null)
				InsertTag (0, tag);
			
			return start;
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
				else if (t is TagLib.Id3v1.Tag)
					data.Add ((t as TagLib.Id3v1.Tag).Render ());
			}
			
			return data;
		}
		
		/// <summary>
		///    Writes the tags contained in the current instance to the
		///    end of the file that created it, overwriting the existing
		///    tags.
		/// </summary>
		/// <returns>
		///    A <see cref="long" /> value indicating the seek position
		///    in the file at which the written tags begin. This also
		///    marks the seek position at which the media ends.
		/// </returns>
		public long Write ()
		{
			long total_size = TotalSize;
			ByteVector data = Render ();
			file.Insert (data, file.Length - total_size, total_size);
			return file.Length - data.Count;
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
		///    writing, this is limited to <see cref="TagTypes.Ape" />,
		///    <see cref="TagTypes.Id3v1" />, and <see
		///    cref="TagTypes.Id3v2" />.
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
			
			if (type == TagTypes.Id3v1) {
				tag = new TagLib.Id3v1.Tag ();
			} else if (type == TagTypes.Id3v2) {
				Id3v2.Tag tag32 = new Id3v2.Tag ();
				tag32.Version = 4;
				tag32.Flags |= Id3v2.HeaderFlags.FooterPresent;
				tag = tag32;
			} else if (type == TagTypes.Ape) {
				tag = new TagLib.Ape.Tag ();
			}
			
			if (tag != null) {
				if (copy != null)
					copy.CopyTo (tag, true);
				
				if (type == TagTypes.Id3v1)
					AddTag (tag);
				else
					InsertTag (0, tag);
			}
			
			return tag;
		}

		#endregion



		#region Private Methods

		/// <summary>
		///    Reads a tag ending at a specified position and moves the
		///    cursor to its start position.
		/// </summary>
		/// <param name="end">
		///    A <see cref="long" /> value reference specifying at what
		///    position the potential tag ends at. If a tag is found,
		///    this value will be updated to the position at which the
		///    found tag starts.
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
		private TagLib.Tag ReadTag (ref long end, ReadStyle style)
		{
			long start = end;
			TagTypes type = ReadTagInfo (ref start);
			TagLib.Tag tag = null;
			
			try {
				switch (type)
				{
				case TagTypes.Ape:
					tag = new TagLib.Ape.Tag (file, end - TagLib.Ape.Footer.Size);
					break;
				case TagTypes.Id3v2:
					tag = new TagLib.Id3v2.Tag (file, start, style);
					break;
				case TagTypes.Id3v1:
					tag = new TagLib.Id3v1.Tag (file, start);
					break;
				}
				
				end = start;
			} catch (CorruptFileException) {
			}
			
			return tag;
		}
		
		/// <summary>
		///    Looks for a tag ending at a specified position and moves
		///    the cursor to its start position.
		/// </summary>
		/// <param name="position">
		///    A <see cref="long" /> value reference specifying at what
		///    position the potential tag ends. If a tag is found,
		///    this value will be updated to the position at which the
		///    found tag starts.
		/// </param>
		/// <returns>
		///    A <see cref="TagLib.TagTypes" /> value specifying the
		///    type of tag found at the specified position, or <see
		///    cref="TagTypes.None" /> if no tag was found.
		/// </returns>
		private TagTypes ReadTagInfo (ref long position)
		{
			if (position - read_size < 0)
				return TagTypes.None;
			
			file.Seek (position - read_size);
			ByteVector data = file.ReadBlock (read_size);
			
			try {
				int offset = (int) (data.Count - TagLib.Ape.Footer.Size);
				if (data.ContainsAt (TagLib.Ape.Footer.FileIdentifier,
					offset)) {
					TagLib.Ape.Footer footer =
						new TagLib.Ape.Footer (
							data.Mid (offset));
					
					// If the complete tag size is zero or
					// the tag is a header, this indicates
					// some sort of corruption.
					if (footer.CompleteTagSize == 0 ||
						(footer.Flags &
						TagLib.Ape.FooterFlags.IsHeader) != 0)
						return TagTypes.None;
					
					position -= footer.CompleteTagSize;
					return TagTypes.Ape;
				}
				
				offset = (int) (data.Count - TagLib.Id3v2.Footer.Size);
				if (data.ContainsAt (TagLib.Id3v2.Footer.FileIdentifier,
					offset)) {
					TagLib.Id3v2.Footer footer =
						new TagLib.Id3v2.Footer (
							data.Mid (offset));
					
					position -= footer.CompleteTagSize;
					return TagTypes.Id3v2;
				}
				
				if (data.StartsWith (
					TagLib.Id3v1.Tag.FileIdentifier)) {
					position -= TagLib.Id3v1.Tag.Size;
					return TagTypes.Id3v1;
				}
			} catch (CorruptFileException) {
			}
			
			return TagTypes.None;
		}
		
		#endregion
	}
}
