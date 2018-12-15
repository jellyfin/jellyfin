//
// Tag.cs: Combines StartTag and EndTag in such a way as their children appear
// as its children.
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
using System.Collections.Generic;

namespace TagLib.NonContainer {
	/// <summary>
	///    This class extends <see cref="CombinedTag" />, combining <see
	///    cref="StartTag" /> and <see cref="EndTag" /> in such a way as
	///    their children appear as its children.
	/// </summary>
	public class Tag : CombinedTag
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the tags appearing at the start of the file.
		/// </summary>
		private StartTag start_tag;
		
		/// <summary>
		///    Contains the tags appearing at the end of the file.
		/// </summary>
		private EndTag end_tag;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Tag" /> for a specified <see cref="TagLib.File" />.
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
		public Tag (File file) : base ()
		{
			start_tag = new StartTag (file);
			end_tag = new EndTag (file);
			AddTag (start_tag);
			AddTag (end_tag);
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		/// <summary>
		///    Gets the collection of tags appearing at the start of the
		///    file.
		/// </summary>
		/// <value>
		///    A <see cref="TagLib.NonContainer.StartTag" /> storing the
		///    tags for the start of the file.
		/// </value>
		public StartTag StartTag {
			get {return start_tag;}
		}
		
		/// <summary>
		///    Gets the collection of tags appearing at the end of the
		///    file.
		/// </summary>
		/// <value>
		///    A <see cref="TagLib.NonContainer.EndTag" /> storing the
		///    tags for the end of the file.
		/// </value>
		public EndTag EndTag {
			get {return end_tag;}
		}
		
		/// <summary>
		///    Gets the tag types contained in the current instance.
		/// </summary>
		/// <value>
		///    A bitwise combined <see cref="TagLib.TagTypes" />
		///    containing the tag types contained in the current
		///    instance.
		/// </value>
		public override TagTypes TagTypes {
			get {return start_tag.TagTypes | end_tag.TagTypes;}
		}

		/// <summary>
		///    Gets the tags combined in the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:TagLib.Tag[]" /> containing the tags combined in
		///    the current instance.
		/// </value>
		/// <remarks>
		///    This contains the combined children of <see
		///    cref="Tag.StartTag" /> and <see cref="Tag.EndTag" />.
		/// </remarks>
		public override TagLib.Tag [] Tags {
			get {
				List<TagLib.Tag> tags = new List<TagLib.Tag> ();
				tags.AddRange (start_tag.Tags);
				tags.AddRange (end_tag.Tags);
				return tags.ToArray ();
			}
		}
		
		#endregion
		
		
		
		#region Public Methods
		
		/// <summary>
		///    Gets a tag of a specified type from the current instance.
		/// </summary>
		/// <param name="type">
		///    A <see cref="TagLib.TagTypes" /> value indicating the
		///    type of tag to read.
		/// </param>
		/// <returns>
		///    A <see cref="Tag" /> object containing the tag that was
		///    found in the current instance. If no
		///    matching tag was found and none was created, <see
		///    langword="null" /> is returned.
		/// </returns>
		public TagLib.Tag GetTag (TagTypes type)
		{
			foreach (TagLib.Tag t in Tags) {
				if (type == TagTypes.Id3v1 && t is TagLib.Id3v1.Tag)
					return t;
				
				if (type == TagTypes.Id3v2 && t is TagLib.Id3v2.Tag)
					return t;
				
				if (type == TagTypes.Ape && t is TagLib.Ape.Tag)
					return t;
			}
			
			return null;
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
			start_tag.RemoveTags (types);
			end_tag.RemoveTags (types);
		}
		
		/// <summary>
		///    Reads the tags at the start and end of the file.
		/// </summary>
		/// <param name="start">
		///    A <see cref="long" /> value reference which will be set
		///    to contain the seek position in the file at which the
		///    tags at the start end. This also marks the seek position
		///    at which the media begins.
		/// </param>
		/// <param name="end">
		///    A <see cref="long" /> value reference which will be set
		///    to contain the seek position in the file at which the
		///    tags at the end begin. This also marks the seek position
		///    at which the media ends.
		/// </param>
		public void Read (out long start, out long end)
		{
			start = ReadStart (ReadStyle.None);
			end = ReadEnd (ReadStyle.None);
		}
		
		/// <summary>
		///    Reads the tags stored at the start of the file into the
		///    current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="long" /> value indicating the seek position
		///    in the file at which the read tags end. This also
		///    marks the seek position at which the media begins.
		/// </returns>
		public long ReadStart (ReadStyle style)
		{
			return start_tag.Read (style);
		}
		
		/// <summary>
		///    Reads the tags stored at the end of the file into the
		///    current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="long" /> value indicating the seek position
		///    in the file at which the read tags begin. This also
		///    marks the seek position at which the media ends.
		/// </returns>
		public long ReadEnd (ReadStyle style)
		{
			return end_tag.Read (style);
		}
		
		/// <summary>
		///    Writes the tags to the start and end of the file.
		/// </summary>
		/// <param name="start">
		///    A <see cref="long" /> value reference which will be set
		///    to contain the new seek position in the file at which the
		///    tags at the start end. This also marks the seek position
		///    at which the media begins.
		/// </param>
		/// <param name="end">
		///    A <see cref="long" /> value reference which will be set
		///    to contain the new seek position in the file at which the
		///    tags at the end begin. This also marks the seek position
		///    at which the media ends.
		/// </param>
		public void Write (out long start, out long end)
		{
			start = start_tag.Write ();
			end = end_tag.Write ();
		}
		
		#endregion
	}
}
