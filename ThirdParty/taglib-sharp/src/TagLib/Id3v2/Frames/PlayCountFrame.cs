//
// PlayCountFrame.cs:
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

namespace TagLib.Id3v2 {
	/// <summary>
	///    This class extends <see cref="Frame" />, implementing support for
	///    ID3v2 Play Count (PCNT) Frames.
	/// </summary>
	/// <example>
	///    <para>Getting and incrementing the play count of a file.</para>
	///    <code lang="C#">
	/// using TagLib;
	/// using TagLib.Id3v2;
	///
	/// public static class TrackUtil
	/// {
	/// 	public static int GetPlayCount (string filename)
	/// 	{
	/// 		File file = File.Create (filename, ReadStyle.None);
	/// 		Id3v2.Tag tag = file.GetTag (TagTypes.Id3v2, false) as Id3v2.Tag;
	/// 		if (tag == null)
	/// 			return 0;
	/// 		
	/// 		PlayCountFrame frame = PlayCountFrame.Get (tag, false);
	/// 		if (frame == null)
	/// 			return 0;
	///
	/// 		return frame.PlayCount;
	/// 	}
	/// 	
	/// 	public static void IncrementPlayCount (string filename)
	/// 	{
	/// 		File file = File.Create (filename, ReadStyle.None);
	/// 		Id3v2.Tag tag = file.GetTag (TagTypes.Id3v2, true) as Id3v2.Tag;
	/// 		if (tag == null)
	/// 			return;
	/// 		
	/// 		PlayCountFrame.Get (tag, true).PlayCount ++;
	/// 		file.Save ();
	/// 	}
	/// }
	///    </code>
	///    <code lang="C++">
	/// #using &lt;System.dll>
	/// #using &lt;taglib-sharp.dll>
	///
	/// using System;
	/// using TagLib;
	/// using TagLib::Id3v2;
	///
	/// public ref class TrackUtil abstract sealed
	/// {
	/// public:
	/// 	static int GetPlayCount (String^ filename)
	/// 	{
	/// 		File^ file = File.Create (filename, ReadStyle.None);
	/// 		Id3v2::Tag^ tag = dynamic_cast&lt;Id3v2::Tag^> (file.GetTag (TagTypes::Id3v2, false));
	/// 		if (tag == null)
	/// 			return 0;
	/// 		
	/// 		PlayCountFrame^ frame = PlayCountFrame::Get (tag, false);
	/// 		if (frame == null)
	/// 			return 0;
	///
	/// 		return frame->PlayCount;
	/// 	}
	/// 	
	/// 	static void IncrementPlayCount (String^ filename)
	/// 	{
	/// 		File^ file = File::Create (filename, ReadStyle::None);
	/// 		Id3v2.Tag^ tag = dynamic_cast&lt;Id3v2::Tag^> (file.GetTag (TagTypes::Id3v2, true));
	/// 		if (tag == null)
	/// 			return;
	/// 		
	/// 		PlayCountFrame::Get (tag, true)->PlayCount ++;
	/// 		file->Save ();
	/// 	}
	/// }
	///    </code>
	///    <code lang="VB">
	/// Imports TagLib
	/// Imports TagLib.Id3v2
	///
	/// Public Shared Class TrackUtil
	/// 	Public Shared Sub GetPlayCount (filename As String) As Integer
	/// 		Dim file As File = File.Create (filename, ReadStyle.None)
	/// 		Dim tag As Id3v2.Tag = file.GetTag (TagTypes.Id3v2, False)
	/// 		If tag Is Nothing Then Return 0
	/// 		
	/// 		Dim frame As PlayCountFrame = PlayCountFrame.Get (tag, False)
	///		If frame Is Nothing Then Return 0
	///
	/// 		Return frame.PlayCount
	/// 	End Sub
	///
	///	Public Shared Sub IncrementPlayCount (filename As String)
	/// 		Dim file As File = File.Create (filename, ReadStyle.None)
	/// 		Dim tag As Id3v2.Tag = file.GetTag (TagTypes.Id3v2, True)
	/// 		If tag Is Nothing Then Exit Sub
	/// 		
	/// 		PlayCountFrame.Get (tag, True).PlayCount += 1
	/// 		file.Save ()
	/// 	End Sub
	/// End Class
	///    </code>
	///    <code lang="Boo">
	/// import TagLib
	/// import TagLib.Id3v2
	/// 
	/// public static class TrackUtil:
	/// 	static def GetPlayCount (filename as string) as int:
	/// 		file As File = File.Create (filename, ReadStyle.None)
	/// 		tag as Id3v2.Tag = file.GetTag (TagTypes.Id3v2, false)
	///		if tag == null:
	/// 			return 0
	/// 		
	/// 		frame as PlayCountFrame = PlayCountFrame.Get (tag, false)
	/// 		if frame == null:
	///			return 0
	///
	/// 		return frame.PlayCount
	///
	///	static def IncrementPlayCount (filename as string):
	/// 		file as File = File.Create (filename, ReadStyle.None)
	/// 		tag as Id3v2.Tag = file.GetTag (TagTypes.Id3v2, True)
	///		if tag == null:
	/// 			return
	/// 		
	/// 		PlayCountFrame.Get (tag, true).PlayCount ++
	/// 		file.Save ()
	///    </code>
	/// </example>
	public class PlayCountFrame : Frame
	{
		#region Private Properties
		
		/// <summary>
		///    Contains the total number of times the file has been
		///    played.
		/// </summary>
		private ulong play_count = 0;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="PlayCountFrame" /> with a count of zero.
		/// </summary>
		/// <remarks>
		///    When a frame is created, it is not automatically added to
		///    the tag. Consider using <see cref="Get" /> for more
		///    integrated frame creation.
		/// </remarks>
		public PlayCountFrame () : base (FrameType.PCNT, 4)
		{
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="PlayCountFrame" /> by reading its raw data in a
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
		public PlayCountFrame (ByteVector data, byte version)
			: base (data, version)
		{
			SetData (data, 0, version, true);
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="PlayCountFrame" /> by reading its raw data in a
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
		protected internal PlayCountFrame (ByteVector data, int offset,
		                                   FrameHeader header,
		                                   byte version) : base(header)
		{
			SetData (data, offset, version, false);
		}
		
		#endregion
		
		
		
		#region Public Properties
		
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
		///    Gets a play count frame from a specified tag, optionally
		///    creating it if it does not exist.
		/// </summary>
		/// <param name="tag">
		///    A <see cref="Tag" /> object to search in.
		/// </param>
		/// <param name="create">
		///    A <see cref="bool" /> specifying whether or not to create
		///    and add a new frame to the tag if a match is not found.
		/// </param>
		/// <returns>
		///    A <see cref="PlayCountFrame" /> object containing the
		///    matching frame, or <see langword="null" /> if a match
		///    wasn't found and <paramref name="create" /> is <see
		///    langword="false" />.
		/// </returns>
		public static PlayCountFrame Get (Tag tag, bool create)
		{
			PlayCountFrame pcnt;
			foreach (Frame frame in tag) {
				pcnt = frame as PlayCountFrame;
				
				if (pcnt != null)
					return pcnt;
			}
			
			if (!create)
				return null;
			
			pcnt = new PlayCountFrame ();
			tag.AddFrame (pcnt);
			return pcnt;
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
		protected override void ParseFields (ByteVector data, byte version)
		{
			play_count = data.ToULong ();
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
			while (data.Count > 4 && data [0] == 0)
				data.RemoveAt (0);
			
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
			PlayCountFrame frame = new PlayCountFrame ();
			frame.play_count = play_count;
			return frame;
		}
		
#endregion
	}
}
