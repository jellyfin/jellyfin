//
// UniqueFileIdentifierFrame.cs:
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Original Source:
//   uniquefileidentifierframe.cpp from TagLib
//
// Copyright (C) 2005-2007 Brian Nickel
// Copyright (C) 2004 Scott Wheeler (Original Implementation)
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

using System.Collections;
using System;

namespace TagLib.Id3v2
{
	/// <summary>
	///    This class extends <see cref="Frame" />, implementing support for
	///    ID3v2 Unique File Identifier (UFID) Frames.
	/// </summary>
	public class UniqueFileIdentifierFrame : Frame
	{
#region Private Fields
		
		/// <summary>
		///    Contains the owner string.
		/// </summary>
		private string owner = null;
		
		/// <summary>
		///    Contains the identifier data.
		/// </summary>
		private ByteVector identifier = null;
		
#endregion
  	
  	
  	
#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="UniqueFileIdentifierFrame" /> with a specified
		///    owner and identifier data.
		/// </summary>
		/// <param name="owner">
		///    A <see cref="string" /> containing the owner of the new
		///    frame.
		/// </param>
		/// <param name="identifier">
		///    A <see cref="ByteVector" /> object containing the
		///    identifier for the new frame.
		/// </param>
		/// <remarks>
		///    When a frame is created, it is not automatically added to
		///    the tag. Consider using <see
		///    cref="Get(Tag,string,bool)" /> for more integrated frame
		///    creation.
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="owner" /> is <see langword="null" />.
		/// </exception>
		public UniqueFileIdentifierFrame (string owner,
		                                  ByteVector identifier)
			: base (FrameType.UFID, 4)
		{
			if (owner == null)
				throw new ArgumentNullException ("owner");
			
			this.owner = owner;
			this.identifier = identifier;
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="UniqueFileIdentifierFrame" /> with a specified
		///    owner.
		/// </summary>
		/// <param name="owner">
		///    A <see cref="string" /> containing the owner of the new
		///    frame.
		/// </param>
		/// <remarks>
		///    When a frame is created, it is not automatically added to
		///    the tag. Consider using <see
		///    cref="Get(Tag,string,bool)" /> for more integrated frame
		///    creation.
		/// </remarks>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="owner" /> is <see langword="null" />.
		/// </exception>
		public UniqueFileIdentifierFrame (string owner)
			: this (owner, null)
		{
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="UniqueFileIdentifierFrame" /> by reading its raw
		///    data in a specified ID3v2 version.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object starting with the raw
		///    representation of the new frame.
		/// </param>
		/// <param name="version">
		///    A <see cref="byte" /> indicating the ID3v2 version the
		///    raw frame is encoded in.
		/// </param>
		public UniqueFileIdentifierFrame (ByteVector data, byte version)
			: base (data, version)
		{
			SetData (data, 0, version, true);
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="UniqueFileIdentifierFrame" /> by reading its raw
		///    data in a specified ID3v2 version.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the raw
		///    representation of the new frame.
		/// </param>
		/// <param name="offset">
		///    A <see cref="int" /> indicating at what offset in
		///    <paramref name="data" /> the frame actually begins.
		/// </param>
		/// <param name="header">
		///    A <see cref="FrameHeader" /> containing the header of the
		///    frame found at <paramref name="offset" /> in the data.
		/// </param>
		/// <param name="version">
		///    A <see cref="byte" /> indicating the ID3v2 version the
		///    raw frame is encoded in.
		/// </param>
		protected internal UniqueFileIdentifierFrame (ByteVector data,
		                                              int offset,
		                                              FrameHeader header,
		                                              byte version)
			: base(header)
		{
			SetData (data, offset, version, false);
		}
		
#endregion
		
		
		
#region Public Properties
		
		/// <summary>
		///    Gets and sets the owner of the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the owner of the
		///    current instance.
		/// </value>
		/// <remarks>
		///    There should only be one frame with a matching owner per
		///    tag.
		/// </remarks>
		public string Owner {
			get {return owner;}
		}

		/// <summary>
		///    Gets and sets the identifier data stored in the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="ByteVector" /> object containiner the unique
		///    file identifier frame.
		/// </value>
		public ByteVector Identifier {
			get {return identifier;}
			set {identifier = value;}
		}
		
#endregion
		
		
		
#region Public Static Methods
		
		/// <summary>
		///    Gets a specified unique file identifer frame from the
		///    specified tag, optionally creating it if it does not
		///    exist.
		/// </summary>
		/// <param name="tag">
		///    A <see cref="Tag" /> object to search in.
		/// </param>
		/// <param name="owner">
		///    A <see cref="string" /> specifying the owner to match.
		/// </param>
		/// <param name="create">
		///    A <see cref="bool" /> specifying whether or not to create
		///    and add a new frame to the tag if a match is not found.
		/// </param>
		/// <returns>
		///    A <see cref="UserTextInformationFrame" /> object
		///    containing the matching frame, or <see langword="null" />
		///    if a match wasn't found and <paramref name="create" /> is
		///    <see langword="false" />.
		/// </returns>
		public static UniqueFileIdentifierFrame Get (Tag tag,
		                                             string owner,
		                                             bool create)
		{
			UniqueFileIdentifierFrame ufid;
			
			foreach (Frame frame in tag.GetFrames (FrameType.UFID)) {
				ufid = frame as UniqueFileIdentifierFrame;
				
				if (ufid == null)
					continue;
				
				if (ufid.Owner == owner)
					return ufid;
			}
			
			if (!create)
				return null;
			
			ufid = new UniqueFileIdentifierFrame (owner, null);
			tag.AddFrame (ufid);
			return ufid;
		}
		
#endregion
		
		
		
#region Protected Methods
		
		/// <summary>
		///    Populates the values in the current instance by parsing
		///    its field data in a specified version.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the
		///    extracted field data.
		/// </param>
		/// <param name="version">
		///    A <see cref="byte" /> indicating the ID3v2 version the
		///    field data is encoded in.
		/// </param>
		protected override void ParseFields (ByteVector data,
		                                     byte version)
		{
			ByteVectorCollection fields =
				ByteVectorCollection.Split (data, (byte) 0);
			
			if (fields.Count != 2)
				return;
			
			owner = fields [0].ToString (StringType.Latin1);
			identifier = fields [1];
		}
		
		/// <summary>
		///    Renders the values in the current instance into field
		///    data for a specified version.
		/// </summary>
		/// <param name="version">
		///    A <see cref="byte" /> indicating the ID3v2 version the
		///    field data is to be encoded in.
		/// </param>
		/// <returns>
		///    A <see cref="ByteVector" /> object containing the
		///    rendered field data.
		/// </returns>
		protected override ByteVector RenderFields (byte version)
		{
			ByteVector data = new ByteVector ();
			
			data.Add (ByteVector.FromString (owner, StringType.Latin1));
			data.Add (ByteVector.TextDelimiter (StringType.Latin1));
			data.Add (identifier);
			
			return data;
		}
		
#endregion
		
		
		
#region ICloneable
		
		/// <summary>
		///    Creates a deep copy of the current instance.
		/// </summary>
		/// <returns>
		///    A new <see cref="Frame" /> object identical to the
		///    current instance.
		/// </returns>
		public override Frame Clone ()
		{
			UniqueFileIdentifierFrame frame =
				new UniqueFileIdentifierFrame (owner);
			if (identifier != null)
				frame.identifier = new ByteVector (identifier);
			return frame;
		}
		
#endregion
	}
}
