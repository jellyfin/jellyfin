//
// Box.cs: Provides a generic implementation of a ISO/IEC 14496-12 box.
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
using System.Collections.Generic;

namespace TagLib.Mpeg4 {
	/// <summary>
	///    This abstract class provides a generic implementation of a
	///    ISO/IEC 14496-12 box.
	/// </summary>
	public class Box
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the box header.
		/// </summary>
		private BoxHeader header;
		
		/// <summary>
		///    Contains the box's handler, if applicable.
		/// </summary>
		private IsoHandlerBox handler;
		
		/// <summary>
		///    Contains the position of the box data.
		/// </summary>
		private long data_position;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Box" /> with a specified header and handler.
		/// </summary>
		/// <param name="header">
		///    A <see cref="BoxHeader" /> object describing the new
		///    instance.
		/// </param>
		/// <param name="handler">
		///    A <see cref="IsoHandlerBox" /> object containing the
		///    handler that applies to the new instance, or <see
		///    langword="null" /> if no handler applies.
		/// </param>
		protected Box (BoxHeader header, IsoHandlerBox handler)
		{
			this.header = header;
			this.data_position = header.Position + header.HeaderSize;
			this.handler = handler;
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Box" /> with a specified header.
		/// </summary>
		/// <param name="header">
		///    A <see cref="BoxHeader" /> object describing the new
		///    instance.
		/// </param>
		protected Box (BoxHeader header) : this (header, null)
		{
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Box" /> with a specified box type.
		/// </summary>
		/// <param name="type">
		///    A <see cref="ByteVector" /> object containing the box
		///    type to use for the new instance.
		/// </param>
		protected Box (ByteVector type) : this (new BoxHeader (type))
		{
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		/// <summary>
		///    Gets the MPEG-4 box type of the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="ByteVector" /> object containing the four
		///    byte box type of the current instance.
		/// </value>
		public virtual ByteVector BoxType {
			get {return header.BoxType;}
		}
		
		/// <summary>
		///    Gets the total size of the current instance as it last
		///    appeared on disk.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing the total size of
		///    the current instance as it last appeared on disk.
		/// </value>
		public virtual int Size {
			get {return (int)header.TotalBoxSize;}
		}
		
		/// <summary>
		///    Gets and sets the data contained in the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="ByteVector" /> object containing the data
		///    contained in the current instance.
		/// </value>
		public virtual ByteVector Data {
			get {return null;}
			set {}
		}
		
		/// <summary>
		///    Gets the children of the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:System.Collections.Generic.IEnumerable`1" /> object enumerating the
		///    children of the current instance.
		/// </value>
		public virtual IEnumerable<Box> Children {
			get {return null;}
		}
		
		/// <summary>
		///    Gets the handler box that applies to the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="IsoHandlerBox" /> object containing the
		///    handler that applies to the current instance, or <see
		///    langword="null" /> if no handler applies.
		/// </value>
		public IsoHandlerBox Handler {
			get {return handler;}
		}
		
		#endregion
		
		
		
		#region Public Methods
		
		/// <summary>
		///    Renders the current instance, including its children, to
		///    a new <see cref="ByteVector" /> object.
		/// </summary>
		/// <returns>
		///    A <see cref="ByteVector" /> object containing the
		///    rendered version of the current instance.
		/// </returns>
		public ByteVector Render ()
		{
			return Render (new ByteVector ());
		}
		
		/// <summary>
		///    Gets a child box from the current instance by finding
		///    a matching box type.
		/// </summary>
		/// <param name="type">
		///    A <see cref="ByteVector" /> object containing the box
		///    type to match.
		/// </param>
		/// <returns>
		///    A <see cref="Box" /> object containing the matched box,
		///    or <see langword="null" /> if no matching box was found.
		/// </returns>
		public Box GetChild (ByteVector type)
		{
			if (Children == null)
				return null;
			
			foreach (Box box in Children)
				if (box.BoxType == type)
					return box;
			
			return null;
		}
		
		/*
		/// <summary>
		///    Gets a child box from the current instance by finding
		///    a matching object type.
		/// </summary>
		/// <param name="type">
		///    A <see cref="System.Type" /> object containing the object
		///    type to match.
		/// </param>
		/// <returns>
		///    A <see cref="Box" /> object containing the matched box,
		///    or <see langword="null" /> if no matching box was found.
		/// </returns>
		public Box GetChild (System.Type type)
		{
			if (Children == null)
				return null;
			
			foreach (Box box in Children)
				if (box.GetType () == type)
					return box;
			
			return null;
		}
		*/
		
		/// <summary>
		///    Gets a child box from the current instance by finding
		///    a matching box type, searching recursively.
		/// </summary>
		/// <param name="type">
		///    A <see cref="ByteVector" /> object containing the box
		///    type to match.
		/// </param>
		/// <returns>
		///    A <see cref="Box" /> object containing the matched box,
		///    or <see langword="null" /> if no matching box was found.
		/// </returns>
		public Box GetChildRecursively (ByteVector type)
		{
			if (Children == null)
				return null;
			
			foreach (Box box in Children)
				if (box.BoxType == type)
					return box;
			
			foreach (Box box in Children) {
				Box child_box = box.GetChildRecursively (type);
				if (child_box != null)
					return child_box;
			}
			
			return null;
		}
		
		/*
		/// <summary>
		///    Gets a child box from the current instance by finding
		///    a matching object type, searching recursively.
		/// </summary>
		/// <param name="type">
		///    A <see cref="System.Type" /> object containing the object
		///    type to match.
		/// </param>
		/// <returns>
		///    A <see cref="Box" /> object containing the matched box,
		///    or <see langword="null" /> if no matching box was found.
		/// </returns>
		public Box GetChildRecursively (System.Type type)
		{
			if (Children == null)
				return null;
			
			foreach (Box box in Children)
				if (box.GetType () == type)
					return box;
			
			foreach (Box box in Children) {
				Box child_box = box.GetChildRecursively (type);
				if (child_box != null)
					return child_box;
			}
			
			return null;
		}
		*/
		
		/// <summary>
		///    Removes all children with a specified box type from the
		///    current instance.
		/// </summary>
		/// <param name="type">
		///    A <see cref="ByteVector" /> object containing the box
		///    type to remove.
		/// </param>
		public void RemoveChild (ByteVector type)
		{
			ICollection<Box> children = Children as ICollection<Box>;
			
			if (children == null)
				return;
			
			foreach (Box b in new List<Box> (children))
				if (b.BoxType == type)
					children.Remove (b);
		}
		
		/*
		/// <summary>
		///    Removes all children with a specified box type from the
		///    current instance.
		/// </summary>
		/// <param name="type">
		///    A <see cref="ByteVector" /> object containing the box
		///    type to remove.
		/// </param>
		public void RemoveChild (System.Type type)
		{
			ICollection<Box> children = Children as ICollection<Box>;
			
			if (children == null)
				return;
			
			foreach (Box b in new List<Box> (children))
				if (b.GetType () == type)
					children.Remove (b);
		}
		*/
		
		/// <summary>
		///    Removes a specified box from the current instance.
		/// </summary>
		/// <param name="box">
		///    A <see cref="Box" /> object to remove from the current
		///    instance.
		/// </param>
		public void RemoveChild (Box box)
		{
			ICollection<Box> children = Children as ICollection<Box>;
			
			if (children != null)
				children.Remove (box);
		}
		
		/// <summary>
		///    Adds a specified box to the current instance.
		/// </summary>
		/// <param name="box">
		///    A <see cref="Box" /> object to add to the current
		///    instance.
		/// </param>
		public void AddChild (Box box)
		{
			ICollection<Box> children = Children as ICollection<Box>;
			
			if (children != null)
				children.Add (box);
		}
		
		/// <summary>
		///    Removes all children from the current instance.
		/// </summary>
		public void ClearChildren ()
		{
			ICollection<Box> children = Children as ICollection<Box>;
			
			if (children != null)
				children.Clear ();
		}
		
		/// <summary>
		///    Gets whether or not the current instance has children.
		/// </summary>
		/// <value>
		///    A <see cref="bool" /> value indicating whether or not the
		///    current instance has any children.
		/// </value>
		public bool HasChildren {
			get {
				ICollection<Box> children =
					Children as ICollection<Box>;
				
				return children != null && children.Count > 0;
			}
		}
		
		#endregion
		
		
		
		#region Protected Properties
		
		/// <summary>
		///    Gets the size of the data contained in the current
		///    instance, minux the size of any box specific headers.
		/// </summary>
		/// <value>
		///    A <see cref="long" /> value containing the size of
		///    the data contained in the current instance.
		/// </value>
		protected int DataSize {
			get {return (int)(header.DataSize + data_position -
				DataPosition);}
		}
		
		/// <summary>
		///    Gets the position of the data contained in the current
		///    instance, after any box specific headers.
		/// </summary>
		/// <value>
		///    A <see cref="long" /> value containing the position of
		///    the data contained in the current instance.
		/// </value>
		protected virtual long DataPosition {
			get {return data_position;}
		}
		
		/// <summary>
		///    Gets the header of the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="BoxHeader" /> object containing the header
		///    of the current instance.
		/// </value>
		protected BoxHeader Header {
			get {return header;}
		}
		
		#endregion
		
		
		
		#region Protected Methods
		
		/// <summary>
		///    Loads the children of the current instance from a
		///    specified file using the internal data position and size.
		/// </summary>
		/// <param name="file">
		///    The <see cref="TagLib.File" /> from which the current
		///    instance was read and from which to read the children.
		/// </param>
		/// <returns>
		///    A <see cref="T:System.Collections.Generic.IEnumerable`1" /> object enumerating the
		///    boxes read from the file.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="file" /> is <see langword="null" />.
		/// </exception>
		protected IEnumerable<Box> LoadChildren (TagLib.File file)
		{
			if (file == null)
				throw new ArgumentNullException ("file");
			
			List<Box> children = new List<Box> ();
			
			long position = DataPosition;
			long end = position + DataSize;
			
			header.Box = this;
			while (position < end) {
				Box child = BoxFactory.CreateBox (file,
					position, header, handler,
					children.Count);
				children.Add (child);
				position += child.Size;
			}
			header.Box = null;
			
			return children;
		}
		
		/// <summary>
		///    Loads the data of the current instance from a specified
		///    file using the internal data position and size.
		/// </summary>
		/// <param name="file">
		///    The <see cref="TagLib.File" /> from which the current
		///    instance was read and from which to read the data.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector" /> object containing the data
		///    read from the file.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="file" /> is <see langword="null" />.
		/// </exception>
		protected ByteVector LoadData (TagLib.File file)
		{
			if (file == null)
				throw new ArgumentNullException ("file");
			
			file.Seek (DataPosition);
			return file.ReadBlock (DataSize);
		}
		
		/// <summary>
		///    Renders the current instance, including its children, to
		///    a new <see cref="ByteVector" /> object, preceeding the
		///    contents with a specified block of data.
		/// </summary>
		/// <param name="topData">
		///    A <see cref="ByteVector" /> object containing box
		///    specific header data to preceed the content.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector" /> object containing the
		///    rendered version of the current instance.
		/// </returns>
		protected virtual ByteVector Render (ByteVector topData)
		{
			bool free_found = false;
			ByteVector output = new ByteVector ();
			
			if (Children != null)
				foreach (Box box in Children)
					if (box.GetType () == typeof (
						IsoFreeSpaceBox))
						free_found = true;
					else
						output.Add (box.Render ());
			else if (Data != null)
				output.Add (Data);
			
			// If there was a free, don't take it away, and let meta
			// be a special case.
			if (free_found || BoxType == Mpeg4.BoxType.Meta) {
				long size_difference = DataSize - output.Count;
				
				// If we have room for free space, add it so we
				// don't have to resize the file.
				if (header.DataSize != 0 && size_difference >= 8)
					output.Add ((new IsoFreeSpaceBox (
						size_difference)).Render ());
				
				// If we're getting bigger, get a lot bigger so
				// we might not have to again.
				else
					output.Add ((new IsoFreeSpaceBox (2048
						)).Render ());
			}
			
			// Adjust the header's data size to match the content.
			header.DataSize = topData.Count + output.Count;
			
			// Render the full box.
			output.Insert (0, topData);
			output.Insert (0, header.Render ());
			
			return output;
		}
		
		#endregion
  	
		/*
		#region Internal Methods
		
		/// <summary>
		///    Dumps the child tree of the current instance to the
		///    console.
		/// </summary>
		/// <param name="start">
		///    A <see cref="string" /> object to preface each line with.
		/// </param>
		internal void DumpTree (string start)
		{
			if (BoxType == BoxType.Data)
			Console.WriteLine ("{0}{1} {2}", start,
				BoxType.ToString (),
				(this as AppleDataBox).Text);
			else
				Console.WriteLine ("{0}{1}", start,
				BoxType.ToString ());
			
			if (Children != null)
				foreach (Box child in Children)
					child.DumpTree (start + "   ");
		}
		
		#endregion
		*/
	}
}
