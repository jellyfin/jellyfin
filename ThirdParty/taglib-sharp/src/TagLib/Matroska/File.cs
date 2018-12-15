//
// File.cs:
//
// Author:
//   Julien Moutte <julien@fluendo.com>
//   Sebastien Mouy <starwer@laposte.net>
//
// Copyright (C) 2011 FLUENDO S.A.
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

using System.Collections.Generic;
using System;

namespace TagLib.Matroska
{
	/// <summary>
	/// Enumeration listing supported Matroska track types.
	/// </summary>
	public enum TrackType
	{
		/// <summary>
		/// Video track type.
		/// </summary>
		Video = 0x1,
		/// <summary>
		/// Audio track type.
		/// </summary>
		Audio = 0x2,
		/// <summary>
		/// Complex track type.
		/// </summary>
		Complex = 0x3,
		/// <summary>
		/// Logo track type.
		/// </summary>
		Logo = 0x10,
		/// <summary>
		/// Subtitle track type.
		/// </summary>
		Subtitle = 0x11,
		/// <summary>
		/// Buttons track type.
		/// </summary>
		Buttons = 0x12,
		/// <summary>
		/// Control track type.
		/// </summary>
		Control = 0x20
	}

	/// <summary>
	///    This class extends <see cref="TagLib.File" /> to provide tagging
	///    and properties support for Matroska files.
	/// </summary>
	[SupportedMimeType ("taglib/mkv", "mkv")]
	[SupportedMimeType ("taglib/mka", "mka")]
	[SupportedMimeType ("taglib/mks", "mks")]
	[SupportedMimeType ("video/webm")]
	[SupportedMimeType ("video/x-matroska")]
	public class File : TagLib.File
	{
		#region Private Fields

		/// <summary>
		///   Contains the tags for the file.
		/// </summary>
		private Matroska.Tags tags;

		/// <summary>
		///    Contains the media properties.
		/// </summary>
		private Properties properties;

		private double duration_unscaled;
		private ulong time_scale;
		private TimeSpan duration;

		private List<Track> tracks = new List<Track> ();

		private bool updateTags = false;

		#endregion

		
		#region Constructors

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="File" /> for a specified path in the local file
		///    system and specified read style.
		/// </summary>
		/// <param name="path">
		///    A <see cref="string" /> object containing the path of the
		///    file to use in the new instance.
		/// </param>
		/// <param name="propertiesStyle">
		///    A <see cref="ReadStyle" /> value specifying at what level
		///    of accuracy to read the media properties, or <see
		///    cref="ReadStyle.None" /> to ignore the properties.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="path" /> is <see langword="null" />.
		/// </exception>
		public File (string path, ReadStyle propertiesStyle)
			: this (new File.LocalFileAbstraction (path),
				propertiesStyle)
		{
		}

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="File" /> for a specified path in the local file
		///    system with an average read style.
		/// </summary>
		/// <param name="path">
		///    A <see cref="string" /> object containing the path of the
		///    file to use in the new instance.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="path" /> is <see langword="null" />.
		/// </exception>
		public File (string path)
			: this (path, ReadStyle.Average)
		{
		}

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="File" /> for a specified file abstraction and
		///    specified read style.
		/// </summary>
		/// <param name="abstraction">
		///    A <see cref="TagLib.File.IFileAbstraction" /> object to use when
		///    reading from and writing to the file.
		/// </param>
		/// <param name="propertiesStyle">
		///    A <see cref="ReadStyle" /> value specifying at what level
		///    of accuracy to read the media properties, or <see
		///    cref="ReadStyle.None" /> to ignore the properties.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="abstraction" /> is <see langword="null"
		///    />.
		/// </exception>
		public File (File.IFileAbstraction abstraction,
 					ReadStyle propertiesStyle)
			: base (abstraction)
		{
			tags = new Matroska.Tags(tracks);

			Mode = AccessMode.Read;

			try {
				ReadWrite (propertiesStyle);
				TagTypesOnDisk = TagTypes;
			}
			finally {
				Mode = AccessMode.Closed;
			}

			List<ICodec> codecs = new List<ICodec> ();

			foreach (Track track in tracks) {
				codecs.Add (track);
			}

			properties = new Properties (duration, codecs);

			updateTags = true;
		}

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="File" /> for a specified file abstraction with an
		///    average read style.
		/// </summary>
		/// <param name="abstraction">
		///    A <see cref="TagLib.File.IFileAbstraction" /> object to use when
		///    reading from and writing to the file.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="abstraction" /> is <see langword="null"
		///    />.
		/// </exception>
		public File (File.IFileAbstraction abstraction)
			: this (abstraction, ReadStyle.Average)
		{
		}

		#endregion

		
		#region Public Methods

		/// <summary>
		///    Saves the changes made in the current instance to the
		///    file it represents.
		/// </summary>
		public override void Save ()
		{
			// Boilerplate
			PreSave();

			Mode = AccessMode.Write;
			try {
				ReadWrite(ReadStyle.None);
			}
			finally {
				Mode = AccessMode.Closed;
			}
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
		public override void RemoveTags (TagLib.TagTypes types)
		{
			if((types & TagTypes.Matroska) !=0)
			{
				tags.Clear();
			}
		}

		/// <summary>
		///    Gets a tag of a specified type from the current instance,
		///    optionally creating a new tag if possible.
		/// </summary>
		/// <param name="type">
		///    A <see cref="TagLib.TagTypes" /> value indicating the
		///    type of tag to read.
		/// </param>
		/// <param name="create">
		///    A <see cref="bool" /> value specifying whether or not to
		///    try and create the tag if one is not found.
		/// </param>
		/// <returns>
		///    A <see cref="Tag" /> object containing the tag that was
		///    found in or added to the current instance. If no
		///    matching tag was found and none was created, <see
		///    langword="null" /> is returned.
		/// </returns>
		public override TagLib.Tag GetTag (TagLib.TagTypes type,
   										bool create)
		{
			TagLib.Tag ret = null;
			if (type == TagTypes.Matroska) ret = Tag;
			return ret;
		}

		#endregion


		#region Public Properties

		/// <summary>
		///    Gets a abstract representation of all tags stored in the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="TagLib.Tag" /> object representing all tags
		///    stored in the current instance.
		/// </value>
		public override TagLib.Tag Tag
		{
			get
			{
				if(updateTags)
				{
					var retag = new Tag[tags.Count];
					for (int i = 0; i< tags.Count; i++) retag[i] = tags[i];

					foreach (var tag in retag)
					{
						// This will force the default TagetTypeValue to get a proper value according to the medium type (audio/video)
						if (tag.TargetTypeValue == 0) tags.Add(tag);
					}
					updateTags = false;
				}

				// Add Empty Tag representing the Medium to avoid null object
				if (tags.Medium == null)
				{
					new Tag(tags); 
				}

				return tags.Medium;
			}
		}

		/// <summary>
		///    Gets the media properties of the file represented by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="TagLib.Properties" /> object containing the
		///    media properties of the file represented by the current
		///    instance.
		/// </value>
		public override TagLib.Properties Properties
		{
			get
			{
				return properties;
			}
		}

		#endregion


		#region Private Methods Read/Write

		/// <summary>
		///    Reads (and Write, if file Mode is Write) the file with a specified read style.
		/// </summary>
		/// <param name="propertiesStyle">
		///    A <see cref="ReadStyle" /> value specifying at what level
		///    of accuracy to read the media properties, or <see
		///    cref="ReadStyle.None" /> to ignore the properties.
		/// </param>
		private void ReadWrite (ReadStyle propertiesStyle)
		{
			ulong offset = ReadLeadText();

			bool hasSegment = false;
			while (offset < (ulong) Length) {
				EBMLreader element;
				try
				{
					element = new EBMLreader(this, offset);
				}
				catch(Exception ex)
				{
					// Sometimes, the file has zero padding at the end
					if (hasSegment) break; // Avoid crash 
					throw ex;
				}

				EBMLID ebml_id = (EBMLID) element.ID;
				MatroskaID matroska_id = element.ID;

				switch (ebml_id) {
					case EBMLID.EBMLHeader:
						ReadHeader (element);
						break;
					default:
						break;
				}
				switch (matroska_id) {
					case MatroskaID.Segment:
						ReadWriteSegment(element, propertiesStyle);
						hasSegment = true;
						break;
					default:
						break;
				}

				offset += element.Size;
			}
		}

		private void ReadWriteSegment (EBMLreader element, ReadStyle propertiesStyle, bool retry = true)
		{
			// First make reference of all EBML elements at level 1 (top) in the Segment

			var segm_list = ReadSegments(element, retry); // Try to get it from SeekHead the first time (way faster)

			// Now process (read and prepare to write) the referenced elements we care about

			EBMLelement ebml_sinfo = null;
			if (Mode == AccessMode.Write) ebml_sinfo = new EBMLelement(MatroskaID.SegmentInfo);

			bool valid = true;

			foreach (EBMLreader child in segm_list)
			{
				// the child here may be Abstract if it has been retrieved in the SeekHead,
				// so child.Read() must be used to retrieve the full EBML header

				MatroskaID matroska_id = child.ID;
				switch (matroska_id)
				{
					case MatroskaID.SeekHead:
						valid = child.Read();
						if (Mode == AccessMode.Write) child.SetVoid();
						break;

					case MatroskaID.SegmentInfo:
						if (valid = child.Read()) ReadCreateSegmentInfo(child, ebml_sinfo);
						if (Mode == AccessMode.Write) child.SetVoid();
						break;

					case MatroskaID.Tracks:
						if (Mode != AccessMode.Write && (propertiesStyle & ReadStyle.Average) != 0)
						{
							if (valid = child.Read()) ReadTracks(child);
						}
						break;

					case MatroskaID.Tags:
						valid = child.Read();
						if (Mode == AccessMode.Write) child.SetVoid();
						else if (valid) ReadTags(child);
						break;

					case MatroskaID.Attachments:
						valid = child.Read();
						if (Mode == AccessMode.Write) child.SetVoid();
						else if (valid) ReadAttachments(child, propertiesStyle);
						break;

					case MatroskaID.CRC32: // We don't support it
						valid = child.Read();
						if (Mode == AccessMode.Write) child.SetVoid(); // get it out of our way
						break;

					default:
						break;
				}

				if (!valid) break;
			}

			// Detect invalid SeekHead
			if (!valid)
			{
				if (retry)
				{
					MarkAsCorrupt("Invalid Meta Seek");

					// Retry the ReadWriteSegment without using SeekHead
					if (Mode != AccessMode.Write)
					{
						tracks.Clear();
						tags.Clear();
					}

					// Retry it one last time
					ReadWriteSegment(element, propertiesStyle, false);
				}
				else
				{
					MarkAsCorrupt("Invalid EBML element Read");
				}

			}
			else if (Mode == AccessMode.Write)
			{
				// Do the real writing
				WriteSegment(element, ebml_sinfo, segm_list);
			}
			else
			{
				// Resolve the stub UIDElement to their real object (if available)
				foreach (var tag in tags)
				{
					if (tag.Elements != null) {
						for (int k = 0; k < tag.Elements.Count; k++)
						{
							var stub = tag.Elements[k];

							// Attachments
							if (tags.Attachments != null)
							{
								foreach (var obj in tags.Attachments)
								{
									if (stub.UID == obj.UID && stub.UIDType == obj.UIDType)
										tag.Elements[k] = obj;
								}
							}


							// Tracks
							if (tracks != null)
							{
								foreach (var tobj in tracks)
								{
									var obj = tobj as IUIDElement;
									if (obj != null)
									{
										if (stub.UID == obj.UID && stub.UIDType == obj.UIDType)
											tag.Elements[k] = tobj;
									}
								}
							}

						}
					}
				}

			}
		}


		private void ReadCreateSegmentInfo(EBMLreader element, EBMLelement ebml_sinfo)
		{

			EBMLelement ebml_title = null;

			ulong i = 0;
			while (i < element.DataSize)
			{
				EBMLreader child = new EBMLreader(element, element.DataOffset + i);
				MatroskaID matroska_id = child.ID;

				if (Mode == AccessMode.Write)
				{
					// Store raw data to represent the SegmentInfo content
					if (matroska_id != MatroskaID.CRC32) // Ignore CRC-32
					{
						var ebml = new EBMLelement(matroska_id, child.ReadBytes());
						ebml_sinfo.Children.Add(ebml);
						if (matroska_id == MatroskaID.Title) ebml_title = ebml;
					}
				}
				else
				{
					switch (matroska_id)
					{
						case MatroskaID.Duration:
							duration_unscaled = child.ReadDouble();
							if (time_scale > 0)
							{
								duration = TimeSpan.FromMilliseconds(duration_unscaled * time_scale / 1000000);
							}
							break;
						case MatroskaID.TimeCodeScale:
							time_scale = child.ReadULong();
							if (duration_unscaled > 0)
							{
								duration = TimeSpan.FromMilliseconds(duration_unscaled * time_scale / 1000000);
							}
							break;
						case MatroskaID.Title:
							tags.Title = child.ReadString();
							break;
						default:
							break;
					}
				}

				i += child.Size;
			}

			if (Mode == AccessMode.Write)
			{
				// Write SegmentInfo Title
				string title = tags.Title;
				if (title != null)
				{
					if (ebml_title == null)
					{
						// Create the missing EBML string at the end for the current element
						ebml_title = new EBMLelement(MatroskaID.Title, title);
						ebml_sinfo.Children.Add(ebml_title);
					}
					else if (ebml_title.GetString() != title)
					{
						// Replace existing string inside the EBML string
						ebml_title.SetData(title);
					}
				}
				else if (ebml_title != null)
				{
					// Remove title
					ebml_sinfo.Children.Remove(ebml_title);
				}
			}
		}

		#endregion


		#region Private Methods Read

		private ulong ReadLeadText()
		{
			ulong offset = 0;

			// look up the 0x1A start byte
			const int buffer_size = 64;
			ByteVector leadtxt;
			int idx;
			do
			{
				Seek((long)offset);
				leadtxt = ReadBlock(buffer_size);
				idx = leadtxt.IndexOf((byte)0x1A);
				offset += buffer_size;
			} while (idx < 0 && offset < (ulong)Length);

			if (idx < 0)
				throw new Exception("Invalid Matroska file, missing data 0x1A.");

			offset = (offset + (ulong)idx) - buffer_size;

			return offset;
		}


		private void ReadHeader(EBMLreader element)
		{
			string doctype = null;
			ulong i = 0;

			while (i < element.DataSize)
			{
				EBMLreader child = new EBMLreader(element, element.DataOffset + i);

				EBMLID ebml_id = (EBMLID)child.ID;

				switch (ebml_id)
				{
					case EBMLID.EBMLDocType:
						doctype = child.ReadString();
						break;
					default:
						break;
				}

				i += child.Size;
			}

			// Check DocType
			if (String.IsNullOrEmpty(doctype) || (doctype != "matroska" && doctype != "webm"))
			{
				throw new UnsupportedFormatException("DocType is not matroska or webm");
			}
		}


		private List<EBMLreader> ReadSegments(EBMLreader element, bool allowSeekHead)
		{
			var segm_list = new List<EBMLreader>(10);

			bool foundCluster = false; // find first Cluster

			ulong i = 0;

			while (i < element.DataSize)
			{
				EBMLreader child;

				try
				{
					child = new EBMLreader(element, element.DataOffset + i);
				}
				catch
				{
					MarkAsCorrupt("Truncated file or invalid EBML entry");
					break; // Corrupted file: quit here and good luck for the rest
				}

				MatroskaID matroska_id = child.ID;
				bool refInSeekHead = false;

				switch (matroska_id)
				{
					case MatroskaID.SeekHead:
						if (allowSeekHead)
						{
							// Take only the first SeekHead into account
							var ebml_seek = new List<EBMLreader>(10) { child };
							if (ReadSeekHead(child, ebml_seek))
							{
								// Always reference the first element
								if (ebml_seek[0].Offset > element.DataOffset)
									ebml_seek.Insert(0, segm_list[0]);

								segm_list = ebml_seek;
								i = element.DataSize; // Exit the loop: we got what we need
							}
							else
							{
								MarkAsCorrupt("Invalid Meta Seek");
								refInSeekHead = true;
							}
						}
						else
						{
							refInSeekHead = true;
						}
						break;

					case MatroskaID.Void: // extend SeekHead space to following void
						if (Mode == AccessMode.Write) refInSeekHead = true; // This will serve optimization
						break;

					case MatroskaID.Cluster: // reference first Cluster only (too many)
						refInSeekHead = !foundCluster;
						foundCluster = true;
						break;

					// Reference the following elements
					case MatroskaID.Cues:
					case MatroskaID.Tracks:
					case MatroskaID.SegmentInfo:
					case MatroskaID.Tags:
					case MatroskaID.Attachments:
					default:
						refInSeekHead = true;
						break;
				}

				i += child.Size;

				if (refInSeekHead || i==0) segm_list.Add(child);
			}

			return segm_list;
		}



		private bool ReadSeekHead(EBMLreader element, List<EBMLreader> segm_list)
		{
			MatroskaID ebml_id = 0;
			ulong ebml_position = 0;

			ulong i = 0;
			while (i < element.DataSize)
			{
				EBMLreader ebml_seek = new EBMLreader(element, element.DataOffset + i);
				MatroskaID matroska_id = ebml_seek.ID;

				if (matroska_id != MatroskaID.Seek) return false; // corrupted SeekHead

				ulong j = 0;
				while (j < ebml_seek.DataSize)
				{
					EBMLreader child = new EBMLreader(ebml_seek, ebml_seek.DataOffset + j);
					matroska_id = child.ID;

					switch (matroska_id)
					{
						case MatroskaID.SeekID:
							ebml_id = (MatroskaID) child.ReadULong();
							break;
						case MatroskaID.SeekPosition:
							ebml_position = child.ReadULong() + element.Offset;
							break;
						default:
							break;
					}

					j += child.Size;
				}

				if (ebml_id > 0 && ebml_position > 0)
				{
					// Create abstract EBML representation of the segment EBML
					var ebml = new EBMLreader(element.Parent, ebml_position, ebml_id);

					// Sort the seek-entries by increasing position order
					int k;
					for (k = segm_list.Count - 1; k >= 0; k--)
					{
						if (ebml_position > segm_list[k].Offset) break;
					}
					segm_list.Insert(k + 1, ebml);

					// Chained SeekHead recursive read
					if (ebml_id == MatroskaID.SeekHead)
					{
						if (!ebml.Read()) return false; // Corrupted
						ReadSeekHead(ebml, segm_list);
					}
				}

				i += ebml_seek.Size;
			}

			return true;
		}


		private void ReadTags (EBMLreader element)
		{
			ulong i = 0;

			while (i < (ulong)((long)element.DataSize)) {
				EBMLreader child = new EBMLreader (element, element.DataOffset + i);

				MatroskaID matroska_id = child.ID;

				switch (matroska_id) {
					case MatroskaID.Tag:
						ReadTag(child);
						break;
					default:
						break;
				}

				i += child.Size;
			}
		}


		private void ReadTag(EBMLreader element)
		{
			ulong i = 0;

			// Create new Tag
			var tag = new Matroska.Tag(tags);

			while (i < (ulong)((long)element.DataSize))
			{
				EBMLreader child = new EBMLreader(element, element.DataOffset + i);

				MatroskaID matroska_id = child.ID;

				switch (matroska_id)
				{
					case MatroskaID.Targets:
						ReadTargets(child, tag);
						break;
					case MatroskaID.SimpleTag:
						ReadSimpleTag(child, tag);
						break;
					default:
						break;
				}

				i += child.Size;
			}

		}


		private void ReadTargets(EBMLreader element, Tag tag)
		{
			ulong i = 0;

			ushort targetTypeValue = 0;
			string targetType = null;
			var uids = new List<UIDElement>();

			while (i < element.DataSize)
			{
				EBMLreader child = new EBMLreader(element, element.DataOffset + i);

				MatroskaID matroska_id = child.ID;

				switch (matroska_id)
				{
					case MatroskaID.TargetTypeValue:
						targetTypeValue = (ushort)child.ReadULong();
						break;
					case MatroskaID.TargetType:
						targetType = child.ReadString();
						break;
					case MatroskaID.TagTrackUID:
					case MatroskaID.TagEditionUID:
					case MatroskaID.TagChapterUID:
					case MatroskaID.TagAttachmentUID:
						var uid = child.ReadULong();
						// Value 0 => apply to all
						if (uid != 0) uids.Add( new UIDElement(matroska_id, uid) );
						break;
					default:
						break;
				}

				i += child.Size;
			}

			if(targetTypeValue != 0)
			{
				if(targetType != null)
				{
					tag.TargetType = (TargetType) Enum.Parse(typeof(TargetType), targetType.ToUpper());
				}

				if (targetTypeValue != tag.TargetTypeValue) tag.TargetType = tag.MakeTargetType(targetTypeValue);
			}

			if (uids.Count > 0)
			{
				tag.Elements = new List<IUIDElement>(uids.Count);
				// tag.Elements.AddRange(uids); // In .NET 2.0
				foreach (var item in uids) tag.Elements.Add(item);
			}
		}


		private void ReadSimpleTag(EBMLreader element, Tag tag, SimpleTag simpletag = null)
		{
			ulong i = 0;
			string key = null;
			var stag = new SimpleTag();

			while (i < (ulong)((long)element.DataSize))
			{
				EBMLreader child = new EBMLreader(element, element.DataOffset + i);

				MatroskaID matroska_id = child.ID;

				switch (matroska_id)
				{
					case MatroskaID.TagName:
						key = child.ReadString();
						break;
					case MatroskaID.TagLanguage:
						stag.TagLanguage = child.ReadString();
						break;
					case MatroskaID.TagDefault:
						stag.TagDefault = child.ReadULong() != 0;
						break;
					case MatroskaID.TagString:
						stag.TagBinary = false;
						stag.Value = child.ReadBytes();
						break;
					case MatroskaID.TagBinary:
						stag.TagBinary = true;
						stag.Value = child.ReadBytes();
						break;
					case MatroskaID.SimpleTag:
						ReadSimpleTag(child, null, stag);
						break;
					default:
						break;
				}

				i += child.Size;
			}

			// Add the SimpleTag reference to its parent
			if (key != null) 
			{
				key = key.ToUpper();

				List<SimpleTag> list = null;

				if (tag != null)
				{
					if (tag.SimpleTags == null)
						tag.SimpleTags = new Dictionary<string, List<SimpleTag>>(StringComparer.OrdinalIgnoreCase);
					else
						tag.SimpleTags.TryGetValue(key, out list);

					if (list == null)
						tag.SimpleTags[key] = list = new List<SimpleTag>(6);
				}
				else
				{
					if (simpletag.SimpleTags == null)
						simpletag.SimpleTags = new Dictionary<string, List<SimpleTag>>(StringComparer.OrdinalIgnoreCase);
					else
						simpletag.SimpleTags.TryGetValue(key, out list);

					if (list == null)
						simpletag.SimpleTags[key] = list = new List<SimpleTag>(1);
				}

				list.Add(stag);
			}

		}


		private void ReadAttachments(EBMLreader element, ReadStyle propertiesStyle)
		{
			ulong i = 0;

			while (i < (ulong)((long)element.DataSize))
			{
				EBMLreader child = new EBMLreader(element, element.DataOffset + i);

				MatroskaID matroska_id = child.ID;

				switch (matroska_id)
				{
					case MatroskaID.AttachedFile:
						ReadAttachedFile(child, propertiesStyle);
						break;
					default:
						break;
				}

				i += child.Size;
			}
		}


		private void ReadAttachedFile(EBMLreader element, ReadStyle propertiesStyle)
		{
			ulong i = 0;
#pragma warning disable 219 // Assigned, never read
			string file_name = null, file_mime = null, file_desc = null;
			EBMLreader file_data = null;
			ulong file_uid = 0;
#pragma warning restore 219

			while (i < element.DataSize)
			{
				EBMLreader child = new EBMLreader(element, element.DataOffset + i);

				MatroskaID matroska_id = child.ID;

				switch (matroska_id)
				{
					case MatroskaID.FileName:
						file_name = child.ReadString();
						break;
					case MatroskaID.FileMimeType:
						file_mime = child.ReadString();
						break;
					case MatroskaID.FileDescription:
						file_desc = child.ReadString();
						break;
					case MatroskaID.FileData:
						file_data = child;
						break;
					case MatroskaID.FileUID:
						file_uid = child.ReadULong();
						break;
					default:
						break;
				}

				i += child.Size;
			}

			if (file_mime != null && file_data!=null)
			{
				var attachments = tags.Attachments;

				Array.Resize(ref attachments, tags.Attachments.Length + 1);

				var attach = new Attachment(file_abstraction, (long)file_data.DataOffset, (long)file_data.DataSize);
				if (Mode == AccessMode.Write || (propertiesStyle & ReadStyle.PictureLazy) == 0) attach.Load();

				attach.Filename = file_name;
				attach.Description = file_desc != null ? file_desc : file_name;
				attach.MimeType = file_mime;
				attach.UID = file_uid;

				// Set picture type from its name
				attach.SetTypeFromFilename();

				attachments[attachments.Length - 1] = attach;
				tags.Attachments = attachments;
			}

		}


		private void ReadTracks (EBMLreader element)
		{
			ulong i = 0;

			while (i < element.DataSize) {
				EBMLreader child = new EBMLreader (element, element.DataOffset + i);

				MatroskaID matroska_id = child.ID;

				switch (matroska_id) {
					case MatroskaID.TrackEntry:
						ReadTrackEntry (child);
						break;
					default:
						break;
				}

				i += child.Size;
			}
		}

		private void ReadTrackEntry (EBMLreader element)
		{
			ulong i = 0;

			while (i < element.DataSize) {
				EBMLreader child = new EBMLreader (element, element.DataOffset + i);

				MatroskaID matroska_id = child.ID;

				switch (matroska_id) {
					case MatroskaID.TrackType: {
							TrackType track_type = (TrackType) child.ReadULong ();

							switch (track_type) {
								case TrackType.Video: {
										tags.IsVideo = true;
										VideoTrack track = new VideoTrack (this, element);

										tracks.Add (track);
										break;
									}
								case TrackType.Audio: {
										AudioTrack track = new AudioTrack (this, element);

										tracks.Add (track);
										break;
									}
								case TrackType.Subtitle: {
										SubtitleTrack track = new SubtitleTrack (this, element);

										tracks.Add (track);
										break;
									}
								default:
									break;
							}
							break;
						}
					default:
						break;
				}

				i += child.Size;
			}
		}

		#endregion


		#region Private Methods Write/Create

		/// <summary>
		/// Central point for the Writing, after the master elements of the EBML Segment have been referenced.
		/// </summary>
		/// <param name="ebml_segm">EBML Segment containing the EBML to be written</param>
		/// <param name="ebml_sinfo">EBML SegmentInfo</param>
		/// <param name="segm_list">description of the mapping of EBML level 1 in the EBML Segment, ordered</param>
		private void WriteSegment(EBMLreader ebml_segm, EBMLelement ebml_sinfo, List<EBMLreader> segm_list)
		{
			// Organize the Voids (free space map)
			UpdateSegmentsMergeVoids(ebml_segm, segm_list);

			// Create all master elements
			var ebml_alloc = new List<EBMLelement>(3);

			if (tags.Attachments.Length > 0)
				ebml_alloc.Add( CreateAttachments() );

			if (tags.Count > 0 && (tags.Count > 1 || !tags[0].IsEmpty))
				ebml_alloc.Add( CreateTags() );

			// Reoder: biggest first in ebml_alloc to optimize space in Voids
			if (ebml_alloc.Count==2 && ebml_alloc[0].Size < ebml_alloc[1].Size)
			{
				var swap = ebml_alloc[0];
				ebml_alloc[0] = ebml_alloc[1];
				ebml_alloc[1] = swap;
			}

			// Always put the EBML SegmentInfo first (optimize the reading of the Matroska, penalty is in writing time)
			if (ebml_sinfo != null && ebml_sinfo.Children.Count > 0)
				ebml_alloc.Insert(0, ebml_sinfo);

			// Set position to the end of the Segment (just to have a better estimate of worst case address size)
			long pos = (long)(ebml_segm.Offset + ebml_segm.Size);

			// Create draft EBML abstract to create a stub SeekHead and estimate the required size
			foreach (var ebml in ebml_alloc)
			{
				segm_list.Add(new EBMLreader(ebml_segm, (ulong)pos, ebml.ID));
				pos += ebml.Size;
			}

			// SeekHead draft (to estimate its size)
			var ebml_seek = CreateSeekHead(segm_list);

			// Remove the newly created elements from the Segment list. (it was only there for estimation).
			// These will be  added later (with correct size and offset this time)
			segm_list.RemoveRange(segm_list.Count - ebml_alloc.Count, ebml_alloc.Count);

			// Now that all object are more-or-less created and referenced, and mapping is known (segm_list)
			// we can estimate the sizes and how to arrange the EBML to write these to the Voids

			// Estimate size of element that should be at the begining of the Segment, plus margin.
			// Make sure there is a Void at the begining and big enough to contain the reserved space.
			long reserved = WriteReservedEBML(ebml_segm, segm_list, ebml_seek.Size); 

			// Write created master EBMLs (excepted the SeekHead)
			foreach (var ebml in ebml_alloc)
			{
				WriteEBML(ebml, ebml_segm, segm_list, reserved);
			}

			// Claim size back on the last Void if bigger than required
			var last = segm_list[segm_list.Count - 1];
			if (last.ID == MatroskaID.Void && last.Offset + last.Size >= ebml_segm.Offset + ebml_segm.Size)
			{
				segm_list.RemoveAt(segm_list.Count - 1);
				ebml_segm.DataSize -= last.Size;
				last.Remove();
			}

			// Update Segment EBML data-size, resize to 8 (take space on the first reserved Void)
			var poffset = ebml_segm.WriteDataSize();

			// Adapt first Void dimensions to the space that has been taken by the WriteDataSize 
			if (poffset != 0)
				segm_list[0] = new EBMLreader(ebml_segm, ebml_segm.DataOffset, MatroskaID.Void, (ulong)((long)segm_list[0].Size - poffset) );

			// Re-create SeekHead, with correct values this time, and write it in the first (reserved) Void
			ebml_seek = CreateSeekHead(segm_list, -(long)ebml_segm.DataOffset);
			WriteEBML(ebml_seek, ebml_segm, segm_list, 0);

			// Finalize (Write) the remaining abstract EBML Voids
			foreach (var ebml in segm_list)
			{
				if (ebml.ID == MatroskaID.Void && ebml.Abstract)
				{
					ebml.WriteVoid();
				}
			}

		}


		/// <summary>
		/// Make sure there is a Void at the begining of a Segment EBML, big enough to contain the reserved (leading) space.
		/// This is the longest part of the Write if space must be reserved.
		/// </summary>
		/// <param name="ebml_segm">EBML Segment containing the EBML to be written</param>
		/// <param name="segm_list">description of the mapping of EBML level 1 in the EBML Segment, ordered</param>
		/// <param name="minSize">Size to be reserved. A Margin will be added to it.</param>
		/// <returns></returns>
		private long WriteReservedEBML(EBMLreader ebml_segm, List<EBMLreader> segm_list, long minSize)
		{
			long margin = 40;
			long reserved = minSize + margin;

			long woffset = 0;
			long pos = (long)ebml_segm.DataOffset;

			// This is the longest part of the Write if space must be reserved. Reserve a bigger margin
			// then to make sure it is the first and last time that this happens on this file.
			if (segm_list[0].Offset != (ulong)pos || segm_list[0].ID != MatroskaID.Void)
			{
				margin *= 3;
				reserved += margin;
				Insert(reserved, pos);
				woffset += reserved;
				segm_list.Insert(0, new EBMLreader(ebml_segm, (ulong)pos, MatroskaID.Void, (ulong)reserved));
			}
			else if (segm_list[0].Size < (ulong)reserved)
			{
				margin *= 3;
				reserved += margin;
				Insert(reserved - (long)segm_list[0].Size, pos + (long)segm_list[0].Size);
				woffset += reserved - (long)segm_list[0].Size;
				segm_list[0] = new EBMLreader(ebml_segm, (ulong)pos, MatroskaID.Void, (ulong)reserved);
			}

			if (woffset != 0)
			{
				// Update the Segment Data-Size
				ebml_segm.DataSize += (ulong) woffset;

				// Shift all addresses up but the first one
				for (int i = 1; i < segm_list.Count; i++)
					segm_list[i].Offset += (ulong)woffset;
			}

			return reserved;
		}



		/// <summary>
		/// Write an EMBL in an existing Void or at the end of the
		/// </summary>
		/// <param name="element">EBML to write</param>
		/// <param name="ebml_segm">EBML Segment containing the EBML to be written</param>
		/// <param name="segm_list">description of the mapping of EBML level 1 in the EBML Segment, ordered</param>
		/// <param name="reserved">Reserved space at the Segment, do not write there</param>
		private void WriteEBML(EBMLelement element, EBMLreader ebml_segm, List<EBMLreader> segm_list, long reserved)
		{
			long size = element.Size;
			long position = 0;
			long reservedBound = (long) ebml_segm.DataOffset + reserved;

			// Search a Void big enough to fit the element in
			EBMLreader dest = null;
			int idx;
			for (idx = 0; idx < segm_list.Count; idx++)
			{
				dest = segm_list[idx];
				if (dest.ID == MatroskaID.Void)
				{
					// Get Size available in the Void (skip the reserved zone)
					long dsize = (long)dest.Size;
					position = (long)dest.Offset;
					if (position < reservedBound)
					{
						var rsize = reservedBound - position;
						dsize -= rsize;
						position += rsize;
					}

					// Found a proper Void to overwrite
					if (dsize >= size)
					{
						if (dsize != size + 1) break;

						// A Void of size+1 can't be completed by a Void of size 1,
						// so we try to extend the size of the element to write by 1
						// instead.
						if (element.IncrementSize()) break;
					}
				}
			}

			if (idx < segm_list.Count)  // found Void big enough
			{
				// Set Void before element
				if (position > (long)dest.Offset)
				{
					var ebml = new EBMLreader(ebml_segm, dest.Offset, MatroskaID.Void, (ulong)position - dest.Offset);
					segm_list.Insert(idx, ebml);
					idx++;
				}

				// Write the element and reference it in the segment list
				element.Write(this, position, size);
				segm_list[idx] = new EBMLreader(ebml_segm, (ulong)position, element.ID, (ulong)size);

				// Set Void after element
				ulong pos = (ulong)(position + size);
				ulong voidBound = dest.Offset + dest.Size;
				if (pos < voidBound)
				{
					idx++;
					var ebml = new EBMLreader(ebml_segm, pos, MatroskaID.Void, voidBound - pos);
					segm_list.Insert(idx, ebml);
				}
			}
			else
			{
				long segm_dsize = (long)ebml_segm.DataSize;
				idx = segm_list.Count - 1;
				var last = segm_list[idx];
				ulong end = ebml_segm.Offset + ebml_segm.Size;
				if (last.ID == MatroskaID.Void && last.Offset + last.Size >= end)
				{
					position = (long)last.Offset;
					segm_dsize += size - (long)last.Size;

					// Overwrite and Extend the Void element
					element.Write(this, position, (long)last.Size);
					segm_list[idx] = new EBMLreader(ebml_segm, (ulong)position, element.ID, (ulong)size);
				}
				else
				{
					// Append new element to the Segment
					position = (long)end;
					segm_dsize += size;

					// Write the element
					element.Write(this, position);
					segm_list.Add( new EBMLreader(ebml_segm, (ulong)position, element.ID, (ulong)size) );
				}

				// Update the EBML Segment Data-Size length
				ebml_segm.DataSize = (ulong)segm_dsize;
			}

		}


		/// <summary>
		/// This tries to create a sensible map of the Voids between the other master element of the Segment.
		/// It will try to identify Voids hidden after a meaningful EBML. It will merge contiguous Voids as one.
		/// </summary>
		/// <param name="ebml_segm">EBML Segment containing the EBML to be written</param>
		/// <param name="segm_list">description of the mapping of EBML level 1 in the EBML Segment, ordered</param>
		private void UpdateSegmentsMergeVoids(EBMLreader ebml_segm, List<EBMLreader> segm_list)
		{
			ulong maxbound = ebml_segm.Offset + ebml_segm.Size - 2;

			for (int i = 0; i < segm_list.Count; i++)
			{
				var ebml = segm_list[i];
				if (ebml.Size == 0)
				{
					// Read Abstract to retrieve its size
					if (!ebml.Read()) continue; // Avoid problems after invalid read
				}

				ulong spos = ebml.Offset + ebml.Size;
				EBMLreader next;

				if (ebml.ID == MatroskaID.Void)
				{
					ulong pos = spos;
					int j = i;
					next = ebml;

					// Find next contiguous Void EBMLs
					while (pos < maxbound)
					{
						// Get next contiguous EBML
						if (j + 1 < segm_list.Count)
						{
							next = segm_list[j + 1];
							if (next.Offset == pos) j++; // Avoid reading it
							else next = new EBMLreader(ebml_segm, pos);
						}
						else
						{
							next = new EBMLreader(ebml_segm, pos);
						}

						if (next.ID != MatroskaID.Void) break;
						pos += next.Size;
					}

					if (pos > spos)
					{
						segm_list[i] = new EBMLreader(ebml_segm, ebml.Offset, MatroskaID.Void, pos - ebml.Offset);
						if (j != i)
						{
							if (segm_list[j].ID != MatroskaID.Void) j--;
							if (j != i) segm_list.RemoveRange(i + 1, j - i);
						}
					}

				}
				else if ( spos < maxbound && (i + 1 >= segm_list.Count || spos < segm_list[i + 1].Offset) )
				{
					// Next contiguous element is not in the segment list

					next = new EBMLreader(ebml_segm, spos);
					if (next.ID == MatroskaID.Void)
					{
						segm_list.Insert(i + 1, next); // Add an unreferenced Void to the list
					}
				}
			}
		}


		private EBMLelement CreateAttachments()
		{
			var ret = new EBMLelement(MatroskaID.Attachments);

			foreach (var attach in tags.Attachments)
			{
				// Write AttachedFile content
				if (attach != null && attach.Data != null && attach.Data.Count > 0)
				{
					// Try to keep the type info in the filename (more important than the Filename)
					attach.SetFilenameFromType();

					// Create new EBML AttachedFile
					var ebml_attach = CreateAttachedFile(attach);
					ret.Children.Add(ebml_attach);
				}
			}

			return ret;
		}

		private EBMLelement CreateAttachedFile(Attachment attach)
		{
			var ret = new EBMLelement(MatroskaID.AttachedFile);

			// Write AttachedFile content

			if (!string.IsNullOrEmpty(attach.Description) && attach.Description != attach.Filename)
			{
				var ebml_obj = new EBMLelement(MatroskaID.FileDescription, attach.Description);
				ret.Children.Add(ebml_obj);
			}

			if (!string.IsNullOrEmpty(attach.Filename))
			{
				var ebml_obj = new EBMLelement(MatroskaID.FileName, attach.Filename);
				ret.Children.Add(ebml_obj);
			}

			if (!string.IsNullOrEmpty(attach.MimeType))
			{
				var ebml_obj = new EBMLelement(MatroskaID.FileMimeType, attach.MimeType);
				ret.Children.Add(ebml_obj);
			}

			if (attach.UID > 0)
			{
				var ebml_obj = new EBMLelement(MatroskaID.FileUID, attach.UID);
				ret.Children.Add(ebml_obj);
			}

			var ebml_data = new EBMLelement(MatroskaID.FileData, attach.Data);
			ret.Children.Add(ebml_data);

			return ret;
		}



		private EBMLelement CreateTags()
		{
			var ret = new EBMLelement(MatroskaID.Tags);

			foreach (var tag in tags)
			{
				// Detect Tag targetting dead links (because attachment has been removed)
				bool notdeadlink = true;
				if (tag.Elements != null)
				{
					notdeadlink = false;
					foreach (var elm in tag.Elements)
					{
						var att = elm as Attachment;
						if (att != null)
						{
							foreach (var item in tags.Attachments)
							{
								if (item == att) notdeadlink = true;
							}
						}
						else
						{
							notdeadlink = true;
						}
					}
				}

				if (tag.SimpleTags != null && tag.SimpleTags.Count > 0 && notdeadlink)
				{
					// Create new EBML Tag
					var ebml_tag = CreateTag(tag);
					ret.Children.Add(ebml_tag);

				}
			}

			return ret;
		}


		private EBMLelement CreateTag(Tag tag)
		{
			var ret = new EBMLelement(MatroskaID.Tag);

			// Create Targets
			var ebml_targets = CreateTargets(tag);
			ret.Children.Add(ebml_targets);

			// Extract the SimpleTag from the Tag object
			foreach (var stagList in tag.SimpleTags)
			{
				string key = stagList.Key;
				foreach (var stag in stagList.Value)
				{
					var ebml_Simpletag = CreateSimpleTag(key, stag);
					ret.Children.Add(ebml_Simpletag);
				}
			}

			return ret;
		}


		private EBMLelement CreateSimpleTag(string key, SimpleTag value)
		{
			var ret = new EBMLelement(MatroskaID.SimpleTag);

			key = key.ToUpper();

			// Create SimpleTag content
			var ebml_tagName = new EBMLelement(MatroskaID.TagName, key);
			ret.Children.Add(ebml_tagName);

			var ebml_tagLanguage = new EBMLelement(MatroskaID.TagLanguage, value.TagLanguage);
			ret.Children.Add(ebml_tagLanguage);

			var ebml_tagDefault = new EBMLelement(MatroskaID.TagDefault, value.TagDefault ? (ulong)1 : 0);
			ret.Children.Add(ebml_tagDefault);

			var ebml_tagValue = new EBMLelement(value.TagBinary ? MatroskaID.TagBinary : MatroskaID.TagString, value);
			ret.Children.Add(ebml_tagValue);

			// Nested SimpleTag (Recursion)
			if (value.SimpleTags != null)
			{
				foreach (var stagList in value.SimpleTags)
				{
					foreach (var stag in stagList.Value)
					{
						var ebml_Simpletag = CreateSimpleTag(stagList.Key, stag);
						ret.Children.Add(ebml_Simpletag);
					}
				}
			}

			return ret;
		}


		private EBMLelement CreateTargets(Tag tag)
		{
			var ret = new EBMLelement(MatroskaID.Targets);

			if (tag.TargetType != TargetType.DEFAULT)
			{
				var ebml_targetTypeValue = new EBMLelement(MatroskaID.TargetTypeValue, tag.TargetTypeValue);
				ret.Children.Add(ebml_targetTypeValue);

				var ebml_targetType = new EBMLelement(MatroskaID.TargetType, tag.TargetType.ToString());
				ret.Children.Add(ebml_targetType);
			}

			if (tag.Elements != null)
			{
				foreach (var value in tag.Elements)
				{
					var ebml_targetUID = new EBMLelement(value.UIDType, value.UID);
					ret.Children.Add(ebml_targetUID);
				}
			}

			return ret;
		}



		private EBMLelement CreateSeekHead(List<EBMLreader> segm_list, long offset = 0)
		{
			var ret = new EBMLelement(MatroskaID.SeekHead);
			bool refCluster = true; // Reference only the first cluster


			// Create the Seek Entries
			foreach (var segm in segm_list)
			{
				if ( segm.ID != MatroskaID.Void 
					&& segm.ID != MatroskaID.CRC32
					&& (segm.ID != MatroskaID.Cluster || refCluster) 
					)
				{
					var seekEntry = new EBMLelement(MatroskaID.Seek);
					ret.Children.Add(seekEntry);

					// Create SeekEntry Content
					var seekId = new EBMLelement(MatroskaID.SeekID, (ulong)segm.ID);
					seekEntry.Children.Add(seekId);

					var seekPosition = new EBMLelement(MatroskaID.SeekPosition, (ulong)((long)segm.Offset + offset));
					seekEntry.Children.Add(seekPosition);

					if (segm.ID == MatroskaID.Cluster) refCluster = false; // don't reference subsequent Clusters
				}
			}

			return ret;
		}

		#endregion

	}
}
