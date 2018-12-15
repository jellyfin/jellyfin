//
// MusicCdIdentifierFrame.cs:
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

using System.Collections;
using System;

namespace TagLib.Id3v2 {
	/// <summary>
	///    This class extends <see cref="Frame" />, implementing support for
	///    ID3v2 Music CD Identifier (MCDI) Frames.
	/// </summary>
	/// <remarks>
	///    Music CD Identifier Frames should contain the table of
	///    contents data as stored on the physical CD. It is primarily used
	///    for track information lookup by through web sources like CDDB.
	/// </remarks>
	/// <example>
	///    <para>Reading the music CD identifier from a tag.</para>
	///    <code lang="C#">
	/// using TagLib;
	/// using TagLib.Id3v2;
	/// 
	/// public static class LookupUtil
	/// {
	/// 	public static ByteVector GetCdIdentifier (string filename)
	/// 	{
	/// 		File file = File.Create (filename, ReadStyle.None);
	/// 		Id3v2.Tag tag = file.GetTag (TagTypes.Id3v2, false) as Id3v2.Tag;
	/// 		if (tag == null)
	/// 			return new ByteVector ();
	/// 		
	/// 		MusicCdIdentifierFrame frame = MusicCdIdentifierFrame.Get (tag, false);
	/// 		if (frame == null)
	/// 			return new ByteVector ();
	///
	/// 		return frame.Data;
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
	/// public ref class LookupUtil abstract sealed
	/// {
	/// public:
	/// 	static ByteVector^ GetCdIdentifier (String^ filename)
	/// 	{
	/// 		File^ file = File::Create (filename, ReadStyle::None);
	/// 		Id3v2::Tag^ tag = dynamic_cast&lt;Id3v2::Tag^> (file.GetTag (TagTypes::Id3v2, false));
	/// 		if (tag == null)
	/// 			return gcnew ByteVector;
	/// 		
	/// 		MusicCdIdentifierFrame^ frame = MusicCdIdentifierFrame::Get (tag, false);
	/// 		if (frame == null)
	/// 			return gcnew ByteVector;
	///
	/// 		return frame->Data;
	/// 	}
	/// }
	///    </code>
	///    <code lang="VB">
	/// Imports TagLib
	/// Imports TagLib.Id3v2
	/// 
	/// Public Shared Class LookupUtil
	/// 	Public Shared Sub GetCdIdentifier (filename As String) As TagLib.ByteVector
	/// 		Dim file As File = File.Create (filename, ReadStyle.None)
	/// 		Dim tag As Id3v2.Tag = file.GetTag (TagTypes.Id3v2, False)
	/// 		If tag Is Nothing Return New ByteVector ()
	/// 		
	/// 		Dim frame As MusicCdIdentifierFrame = MusicCdIdentifierFrame.Get (tag, False)
	/// 		If frame Is Nothing Return New ByteVector ()
	///
	/// 		Return frame.Data
	/// 	End Sub
	/// End Class
	///    </code>
	///    <code lang="Boo">
	/// import TagLib
	/// import TagLib.Id3v2
	/// 
	/// public static class LookupUtil:
	/// 	static def GetCdIdentifier (filename as string) as TagLib.ByteVector:
	/// 		file as File = File.Create (filename, ReadStyle.None)
	/// 		tag as Id3v2.Tag = file.GetTag (TagTypes.Id3v2, false)
	/// 		if tag == null:
	/// 			return ByteVector ()
	/// 		
	/// 		frame as MusicCdIdentifierFrame = MusicCdIdentifierFrame.Get (tag, false)
	/// 		if frame == null:
	/// 			return ByteVector ()
	///
	/// 		return frame.Data
	///    </code>
	/// </example>
	public class MusicCdIdentifierFrame : Frame
	{
		#region Private Properties
		
		/// <summary>
		///    Contains the identifer data for the current instance.
		/// </summary>
		private ByteVector field_data = null;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="MusicCdIdentifierFrame" /> with empty
		///    identifier data.
		/// </summary>
		/// <remarks>
		///    When a frame is created, it is not automatically added to
		///    the tag. Consider using <see cref="Get" /> for more
		///    integrated frame creation.
		/// </remarks>
		public MusicCdIdentifierFrame () : base (FrameType.MCDI, 4)
		{
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="MusicCdIdentifierFrame" /> by reading its raw data
		///    in a specified ID3v2 version.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object starting with the raw
		///    representation of the new frame.
		/// </param>
		/// <param name="version">
		///    A <see cref="byte" /> indicating the ID3v2 version the
		///    raw frame is encoded in.
		/// </param>
		public MusicCdIdentifierFrame (ByteVector data, byte version)
			: base (data, version)
		{
			SetData (data, 0, version, true);
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="MusicCdIdentifierFrame" /> by reading its raw data
		///    in a specified ID3v2 version.
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
		protected internal MusicCdIdentifierFrame (ByteVector data,
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
		///    Gets and sets the identifier data stored in the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="ByteVector" /> containing the identifier
		///    data stored in the current instance.
		/// </value>
		public ByteVector Data {
			get {return field_data;}
			set {field_data = value;}
		}
		
		#endregion
		
		
		
		#region Public Static Methods
		
		/// <summary>
		///    Gets a music CD identifier frame from a specified tag,
		///    optionally creating it if it does not exist.
		/// </summary>
		/// <param name="tag">
		///    A <see cref="Tag" /> object to search in.
		/// </param>
		/// <param name="create">
		///    A <see cref="bool" /> specifying whether or not to create
		///    and add a new frame to the tag if a match is not found.
		/// </param>
		/// <returns>
		///    A <see cref="MusicCdIdentifierFrame" /> object containing
		///    the matching frame, or <see langword="null" /> if a match
		///    wasn't found and <paramref name="create" /> is <see
		///    langword="false" />.
		/// </returns>
		public static MusicCdIdentifierFrame Get (Tag tag, bool create)
		{
			MusicCdIdentifierFrame mcdi;
			foreach (Frame frame in tag) {
				mcdi = frame as MusicCdIdentifierFrame;
				
				if (mcdi != null)
					return mcdi;
			}
			
			if (!create)
				return null;
			
			mcdi = new MusicCdIdentifierFrame ();
			tag.AddFrame (mcdi);
			return mcdi;
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
			field_data = data;
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
			return field_data != null ? field_data : new ByteVector ();
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
			MusicCdIdentifierFrame frame = new MusicCdIdentifierFrame ();
			if (field_data != null)
				frame.field_data = new ByteVector (field_data);
			return frame;
		}
		
#endregion
	}
}
