//
//  IIMTag.cs
//
//  Author:
//       Eberhard Beilharz <eb1@sil.org>
//
//  Copyright (c) 2012 Eberhard Beilharz
//
//  This library is free software; you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as
//  published by the Free Software Foundation; either version 2.1 of the
//  License, or (at your option) any later version.
//
//  This library is distributed in the hope that it will be useful, but
//  WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//  Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public
//  License along with this library; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
using System;
using System.Collections.Generic;

using TagLib.Image;
using TagLib.IFD.Entries;

namespace TagLib.IIM
{
	/// <summary>
	/// Represents an IIM Tag
	/// </summary>
	public class IIMTag: Xmp.XmpTag
	{
		private List<string> m_Keywords;

		/// <summary>
		/// Constructor
		/// </summary>
		public IIMTag ()
		{
		}

		/// <summary>
		/// Defines the supported TagType
		/// </summary>
		public override TagLib.TagTypes TagTypes
		{
			get
			{
				return TagLib.TagTypes.IPTCIIM;
			}
		}

		/// <summary>
		/// Clear IIM content
		/// </summary>
		public override void Clear ()
		{
			Title = null;
			m_Keywords = null;
			Creator = null;
			Copyright = null;
			Comment = null;
		}

		/// <summary>
		/// Get or Set the Title Tag
		/// </summary>

		public override string Title { get; set; }

		/// <summary>
		/// Get or Set the Creator Tag
		/// </summary>

		public override string Creator { get; set; }

		/// <summary>
		/// Get or Set the Copyright Tag
		/// </summary>

		public override string Copyright { get; set; }

		/// <summary>
		/// Get or Set the comment Tag
		/// </summary>
		public override string Comment { get; set; }

		/// <summary>
		/// Get or Set the Keywords Tag
		/// </summary>
		public override string[] Keywords
		{
			get {
				if (m_Keywords == null)
					return null;
				return m_Keywords.ToArray ();
			}
		}

		internal void AddKeyword (string keyword)
		{
			if (m_Keywords == null)
				m_Keywords = new List<string> ();
			m_Keywords.Add (keyword);
		}
	}
}
