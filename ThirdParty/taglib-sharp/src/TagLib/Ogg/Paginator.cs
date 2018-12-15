//
// Paginator.cs:
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Original Source:
//   oggpage.cpp from TagLib
//
// Copyright (C) 2006-2007 Brian Nickel
// Copyright (C) 2003 Scott Wheeler (Original Implementation)
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
using System.Collections.Generic;

namespace TagLib.Ogg
{
	/// <summary>
	///    This class accepts a sequence of pages for a single Ogg stream,
	///    accepts changes, and produces a new sequence of pages to write to
	///    disk.
	/// </summary>
	public class Paginator
	{
#region Private Fields
		
		/// <summary>
		///    Contains the packets to paginate.
		/// </summary>
		private ByteVectorCollection packets =
			new ByteVectorCollection ();
		
		/// <summary>
		///    Contains the first page header.
		/// </summary>
		private PageHeader? first_page_header = null;
		
		/// <summary>
		///    Contains the codec to use.
		/// </summary>
		private Codec codec;
		
		/// <summary>
		///    contains the number of pages read.
		/// </summary>
		private int pages_read = 0;
#endregion
		
		
		
#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Paginator" /> for a given <see cref="Codec" />
		///    object.
		/// </summary>
		/// <param name="codec">
		///    A <see cref="Codec"/> object to use when processing
		///    packets.
		/// </param>
		public Paginator (Codec codec)
		{
			this.codec = codec;
		}
		
#endregion
		
		
		
#region Public Methods
		
		/// <summary>
		///    Adds the next page to the current instance.
		/// </summary>
		/// <param name="page">
		///    The next <see cref="Page" /> object found in the stream.
		/// </param>
		public void AddPage (Page page)
		{
			pages_read ++;
			
			if (first_page_header == null)
				first_page_header = page.Header;
			
			if (page.Packets.Length == 0)
				return;
			
			ByteVector[] page_packets = page.Packets;
			
			for (int i = 0; i < page_packets.Length; i ++) {
				if ((page.Header.Flags & PageFlags
					.FirstPacketContinued) != 0 && i == 0 &&
					packets.Count > 0)
					packets [packets.Count - 1].Add (page_packets [0]);
				else
					packets.Add (page_packets [i]);
			}
		}
		
		/// <summary>
		///    Stores a Xiph comment in the codec-specific comment
		///    packet.
		/// </summary>
		/// <param name="comment">
		///    A <see cref="XiphComment" /> object to store in the
		///    comment packet.
		/// </param>
		public void SetComment (XiphComment comment)
		{
			codec.SetCommentPacket (packets, comment);
		}

		/// <summary>
		///    Repaginates the pages passed into the current instance to
		///    handle changes made to the Xiph comment.
		/// </summary>
		/// <returns>
		///    A <see cref="T:Page[]" /> containing the new page
		///    collection.
		/// </returns>
		[Obsolete("Use Paginator.Paginate(out int)")]
		public Page [] Paginate ()
		{
			int dummy;
			return Paginate (out dummy);
		}

		/// <summary>
		///    Repaginates the pages passed into the current instance to
		///    handle changes made to the Xiph comment.
		/// </summary>
		/// <param name="change">
		///    A <see cref="int" /> value reference containing the
		///    the difference between the number of pages returned and
		///    the number of pages that were added to the class.
		/// </param>
		/// <returns>
		///    A <see cref="T:Page[]" /> containing the new page
		///    collection.
		/// </returns>
		public Page [] Paginate (out int change)
		{
			// Ogg Pagination: Welcome to sucksville!
			// If you don't understand this, you're not alone.
			// It is confusing as Hell.
			
			// TODO: Document this method, in the mean time, there
			// is always http://xiph.org/ogg/doc/framing.html
			
			if (pages_read == 0) {
				change = 0;
				return new Page [0];
			}
			
			int count = pages_read;
			ByteVectorCollection packets = new ByteVectorCollection (
				this.packets);
			PageHeader first_header = (PageHeader) first_page_header;
			List<Page> pages = new List<Page> ();
			uint index = 0;
			bool bos = first_header.PageSequenceNumber == 0;
			
			if (bos) {
				pages.Add (new Page (new ByteVectorCollection (packets [0]), first_header));
				index ++;
				packets.RemoveAt (0);
				count --;
			}
			
			int lacing_per_page = 0xfc;
			if (count > 0) {
				int total_lacing_bytes = 0;
				
				for (int i = 0; i < packets.Count; i ++)
					total_lacing_bytes += GetLacingValueLength (
						packets, i);
				
				lacing_per_page = Math.Min (total_lacing_bytes / count + 1, lacing_per_page);
			}
			
			int lacing_bytes_used = 0;
			ByteVectorCollection page_packets = new ByteVectorCollection ();
			bool first_packet_continued = false;
			
			while (packets.Count > 0) {
				int packet_bytes = GetLacingValueLength (packets, 0);
				int remaining = lacing_per_page - lacing_bytes_used;
				bool whole_packet = packet_bytes <= remaining;
				if (whole_packet) {
					page_packets.Add (packets [0]);
					lacing_bytes_used += packet_bytes;
					packets.RemoveAt (0);
				} else {
					page_packets.Add (packets [0].Mid (0, remaining * 0xff));
					packets [0] = packets [0].Mid (remaining * 0xff);
					lacing_bytes_used += remaining;
				}
				
				if (lacing_bytes_used == lacing_per_page) {
					pages.Add (new Page (page_packets,
						new PageHeader (first_header,
							index, first_packet_continued ?
							PageFlags.FirstPacketContinued :
							PageFlags.None)));
					page_packets = new ByteVectorCollection ();
					lacing_bytes_used = 0;
					index ++;
					count --;
					first_packet_continued = !whole_packet;
				}
			}
			
			if (page_packets.Count > 0) {
				pages.Add (new Page (page_packets,
					new PageHeader (
						first_header.StreamSerialNumber,
						index, first_packet_continued ?
						PageFlags.FirstPacketContinued :
						PageFlags.None)));
				index ++;
				count --;
			}
			change = -count;
			return pages.ToArray ();
		}
		
#endregion
		
		
		
#region Private Methods
		
		/// <summary>
		///    Gets the number of lacing value bytes that would be
		///    required for a given packet.
		/// </summary>
		/// <param name="packets">
		///    A <see cref="ByteVectorCollection" /> object containing
		///    the packet.
		/// </param>
		/// <param name="index">
		///    A <see cref="int" /> value containing the index of the
		///    packet to compute.
		/// </param>
		/// <returns>
		///    A <see cref="int" /> value containing the number of bytes
		///    needed to store the length.
		/// </returns>
		private static int GetLacingValueLength (ByteVectorCollection packets,
		                                         int index)
		{
			int size = packets [index].Count;
			return size / 0xff + ((index + 1 < packets.Count ||
				size % 0xff > 0) ? 1 : 0);
		}
		
#endregion
	}
}
