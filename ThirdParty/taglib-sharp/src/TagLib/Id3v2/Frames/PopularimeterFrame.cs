//
// PopularimeterFrame.cs:
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

namespace TagLib.Id3v2
{
	/// <summary>
	///    This class extends <see cref="Frame" />, implementing support for
	///    ID3v2 Popularimeter (POPM) Frames.
	/// </summary>
	public class PopularimeterFrame : Frame
	{
		#region Private Properties
		
		/// <summary>
		///    Contains the email of the user this frame belongs to.
		/// </summary>
		private string user = string.Empty;
		
		/// <summary>
		///    Contains the rating of the files from 0 to 255.
		/// </summary>
		private byte rating = 0;
		
		/// <summary>
		///    Contains the number of times this file has been played.
		/// </summary>
		private ulong play_count = 0;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="PopularimeterFrame" /> for a specified user with a
		///    rating and play count of zero.
		/// </summary>
		/// <remarks>
		///    When a frame is created, it is not automatically added to
		///    the tag. Consider using <see cref="Get" /> for more
		///    integrated frame creation.
		/// </remarks>
		public PopularimeterFrame (string user)
			: base (FrameType.POPM, 4)
		{
			User = user;
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="PopularimeterFrame" /> by reading its raw data in a
		///    specified ID3v2 version.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object starting with the raw
		///    representation of the new frame.
		/// </param>
		/// <param name="version">
		///    A <see cref="byte" /> indicating the ID3v2 version the
		///    raw frame is encoded in.
		/// </param>
		public PopularimeterFrame (ByteVector data, byte version)
			: base (data, version)
		{
			SetData (data, 0, version, true);
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="PopularimeterFrame" /> by reading its raw data in a
		///    specified ID3v2 version.
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
		protected internal PopularimeterFrame (ByteVector data,
		                                       int offset,
		                                       FrameHeader header,
		                                       byte version)
			: base (header)
		{
			SetData (data, offset, version, false);
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		/// <summary>
		///    Gets and sets the user to whom the current instance
		///    belongs.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the user to whom the
		///    current instance belongs.
		/// </value>
		public string User {
			get {return user;}
			set {user = value != null ? value : string.Empty;}
		}
		
		/// <summary>
		///    Gets and sets the rating of the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="byte" /> containing the rating of the
		///    current instance.
		/// </value>
		public byte Rating {
			get {return rating;}
			set {rating = value;}
		}

		/// <summary>
		///    Gets and sets the play count of the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="ulong" /> containing the play count of the
		///    current instance.
		/// </value>
		public ulong PlayCount {
			get {return play_count;}
			set {play_count = value;}
		}
		
		#endregion
		
		
		
		#region Public Static Methods
		
		/// <summary>
		///    Gets a popularimeter frame from a specified tag,
		///    optionally creating it if it does not exist.
		/// </summary>
		/// <param name="tag">
		///    A <see cref="Tag" /> object to search in.
		/// </param>
		/// <param name="user">
		///    A <see cref="string" /> containing the user to search for
		///    in the current instance.
		/// </param>
		/// <param name="create">
		///    A <see cref="bool" /> specifying whether or not to create
		///    and add a new frame to the tag if a match is not found.
		/// </param>
		/// <returns>
		///    A <see cref="PopularimeterFrame" /> object containing the
		///    matching frame, or <see langword="null" /> if a match
		///    wasn't found and <paramref name="create" /> is <see
		///    langword="false" />.
		/// </returns>
		public static PopularimeterFrame Get (Tag tag, string user,
		                                      bool create)
		{
			PopularimeterFrame popm;
			foreach (Frame frame in tag) {
				popm = frame as PopularimeterFrame;
				
				if (popm != null && popm.user.Equals (user))
					return popm;
			}
			
			if (!create)
				return null;
			
			popm = new PopularimeterFrame (user);
			tag.AddFrame (popm);
			return popm;
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
			ByteVector delim = ByteVector.TextDelimiter (
				StringType.Latin1);
			
			int index = data.Find (delim);
			if (index < 0)
				throw new CorruptFileException (
					"Popularimeter frame does not contain a text delimiter");
			if (index + 2 > data.Count)
				throw new CorruptFileException("Popularimeter is too short");

			user = data.ToString (StringType.Latin1, 0, index);
			rating = data [index + 1];
			play_count = data.Mid (index + 2).ToULong ();
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
			ByteVector data = ByteVector.FromULong (play_count);
			while (data.Count > 0 && data [0] == 0)
				data.RemoveAt (0);
			
			data.Insert (0, rating);
			data.Insert (0, 0);
			data.Insert (0, ByteVector.FromString (user,
				StringType.Latin1));
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
			PopularimeterFrame frame = new PopularimeterFrame (user);
			frame.play_count = play_count;
			frame.rating = rating;
			return frame;
		}
		
#endregion
	}
}
