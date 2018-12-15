//
// PrivateFrame.cs:
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Copyright (C) 2005-2007 Brian Nickel
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

namespace TagLib.Id3v2 {	
	/// <summary>
	///    This class extends <see cref="Frame" />, implementing support for
	///    ID3v2 Private (PRIV) Frames.
	/// </summary>
	/// <remarks>
	///    <para>A <see cref="PrivateFrame" /> should be used for storing
	///    values specific to the application that cannot or should not be
	///    stored in another frame type.</para>
	/// </remarks>
	/// <example>
	///    <para>Serializing a database entry and storing it in a private
	///    field.</para>
	///    <code lang="C#">
	/// using System;
	/// using System.IO;
	/// using System.Runtime.Serialization;
	/// using System.Text;
	/// using System.Xml.Serialization;
	/// using TagLib.Id3v2;
	///
	/// public static class DbUtil
	/// {
	/// 	public static void StoreDatabaseEntry (Tag tag, ISerializable dbEntry)
	/// 	{
	/// 		StringWriter data = new StringWriter (new StringBuilder ());
	/// 		XmlSerializer serializer = new XmlSerializer (dbEntry.GetType ());
	/// 		serializer.Serialize (data, dbEntry);
	/// 		PrivateFrame frame = PrivateFrame.Get (tag, "org.MyProgram.DatabaseEntry", true);
	/// 		frame.PrivateData = Encoding.UTF8.GetBytes (data.ToString ());
	/// 	}
	/// 	
	/// 	public static object GetDatabaseEntry (Tag tag, Type type)
	/// 	{
	/// 		PrivateFrame frame = PrivateFrame.Get (tag, "org.MyProgram.DatabaseEntry", false);
	/// 		if (frame == null)
	/// 			return null;
	/// 	
	/// 		XmlSerializer serializer = new XmlSerializer (type);
	/// 		return serializer.Deserialize (new MemoryStream (frame.PrivateData));
	/// 	}
	/// }
	///    </code>
	///    <code lang="C++">
	/// #using &lt;System.dll>
	/// #using &lt;System.Xml.dll>
	/// #using &lt;taglib-sharp.dll>
	/// 
	/// using System;
	/// using System::IO;
	/// using System::Runtime::Serialization;
	/// using System::Text;
	/// using System::Xml::Serialization;
	/// using TagLib::Id3v2;
	/// 
	/// public ref class DbUtil abstract sealed
	/// {
	/// public:
	/// 	static void StoreDatabaseEntry (Tag^ tag, ISerializable^ dbEntry)
	/// 	{
	/// 		StringWriter^ data = gcnew StringWriter (gcnew StringBuilder);
	/// 		XmlSerializer serializer = gcnew XmlSerializer (dbEntry->GetType ());
	/// 		serializer->Serialize (data, dbEntry);
	/// 		PrivateFrame frame = PrivateFrame::Get (tag, L"org.MyProgram.DatabaseEntry", true);
	/// 		frame.PrivateData = Encoding::UTF8->GetBytes (data->ToString ());
	/// 	}
	/// 	
	/// 	static Object^ GetDatabaseEntry (Tag^ tag, Type^ type)
	/// 	{
	/// 		PrivateFrame^ frame = PrivateFrame::Get (tag, L"org.MyProgram.DatabaseEntry", false);
	/// 		if (frame == null)
	/// 			return null;
	/// 	
	/// 		XmlSerializer serializer = gcnew XmlSerializer (type);
	/// 		return serializer->Deserialize (gcnew MemoryStream (frame->PrivateData));
	/// 	}
	/// }
	///    </code>
	///    <code lang="VB">
	/// Imports System
	/// Imports System.IO
	/// Imports System.Runtime.Serialization
	/// Imports System.Text
	/// Imports System.Xml.Serialization
	/// Imports TagLib.Id3v2
	///
	/// Public Shared Class DbUtil
	/// 	Public Shared Sub StoreDatabaseEntry (tag As Tag, dbEntry As ISerializable)
	/// 		Dim data As New StringWriter (New StringBuilder ())
	/// 		Dim serializer As New XmlSerializer (dbEntry.GetType ())
	/// 		serializer.Serialize (data, dbEntry)
	/// 		Dim frame As PrivateFrame = PrivateFrame.Get (tag, "org.MyProgram.DatabaseEntry", True)
	/// 		frame.PrivateData = Encoding.UTF8.GetBytes (data.ToString ())
	/// 	End Sub
	/// 	
	/// 	Public Shared Sub GetDatabaseEntry (tag As Tag, type As Type)
	/// 		Dim frame As PrivateFrame = PrivateFrame.Get (tag, "org.MyProgram.DatabaseEntry", False)
	/// 		If frame Is Nothing Then Return Nothing
	/// 	
	/// 		Dim serializer As XmlSerializer = New XmlSerializer (type)
	/// 		Return serializer.Deserialize (New MemoryStream (frame.PrivateData))
	/// 	End Sub
	/// End Class
	///    </code>
	///    <code lang="Boo">
	/// import System
	/// import System.IO
	/// import System.Runtime.Serialization
	/// import System.Text
	/// import System.Xml.Serialization
	/// import TagLib.Id3v2
	/// 
	/// public static class DbUtil:
	/// 	static def StoreDatabaseEntry (tag as Tag, dbEntry as ISerializable):
	/// 		data as StringWriter = StringWriter (StringBuilder ())
	/// 		serializer as XmlSerializer = XmlSerializer (dbEntry.GetType ())
	/// 		serializer.Serialize (data, dbEntry)
	/// 		frame as PrivateFrame = PrivateFrame.Get (tag, "org.MyProgram.DatabaseEntry", true)
	/// 		frame.PrivateData = Encoding.UTF8.GetBytes (data.ToString ())
	///	
	/// 	static def GetDatabaseEntry (tag As Tag, type As Type):
	/// 		frame as PrivateFrame = PrivateFrame.Get (tag, "org.MyProgram.DatabaseEntry", false)
	/// 		if frame == null:
	///			return null
	/// 		
	/// 		serializer as XmlSerializer = XmlSerializer (type)
	/// 		return serializer.Deserialize (MemoryStream (frame.PrivateData))
	///    </code>
	/// </example>
	public class PrivateFrame : Frame
	{
		#region Private Properties
		
		/// <summary>
		///    Contains the owner of the current instance.
		/// </summary>
		private string owner = null;
		
		/// <summary>
		///    Contains private data stored in the current instance.
		/// </summary>
		private ByteVector data  = null;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="PrivateFrame" /> for a specified owner and data.
		/// </summary>
		/// <param name="owner">
		///    A <see cref="string" /> containing the owner of the new
		///    frame.
		/// </param>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the data
		///    for the new frame.
		/// </param>
		/// <remarks>
		///    When a frame is created, it is not automatically added to
		///    the tag. Consider using <see cref="Get" /> for more
		///    integrated frame creation.
		/// </remarks>
		public PrivateFrame (string owner, ByteVector data)
			: base (FrameType.PRIV, 4)
		{
			this.owner = owner;
			this.data  = data;
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="PrivateFrame" /> without data for a specified
		///    owner.
		/// </summary>
		/// <param name="owner">
		///    A <see cref="string" /> containing the owner of the new
		///    frame.
		/// </param>
		/// <remarks>
		///    When a frame is created, it is not automatically added to
		///    the tag. Consider using <see cref="Get" /> for more
		///    integrated frame creation.
		/// </remarks>
		public PrivateFrame (string owner) : this (owner, null)
		{
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="PrivateFrame" /> by reading its raw data in a
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
		public PrivateFrame (ByteVector data, byte version)
			: base (data, version)
		{
			SetData (data, 0, version, true);
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="PrivateFrame" /> by reading its raw data in a
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
		protected internal PrivateFrame (ByteVector data, int offset,
		                                 FrameHeader header,
		                                 byte version) : base(header)
		{
			SetData (data, offset, version, false);
		}
		
		#endregion
		
		
		
		#region Public Properties
		
		/// <summary>
		///    Gets the owner of the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the owner of the
		///    current instance.
		/// </value>
		/// <remarks>
		///    There should only be one frame with a given owner per
		///    tag.
		/// </remarks>
		public string Owner {
			get {return owner;}
		}
		
		/// <summary>
		///    Gets and sets the private data stored in the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="ByteVector" /> containing the private data
		///    stored in the current instance.
		/// </value>
		public ByteVector PrivateData {
			get {return data;}
			set {data = value;}
		}
		
		#endregion
		
		
		
		#region Public Static Methods
		
		/// <summary>
		///    Gets a specified private frame from the specified tag,
		///    optionally creating it if it does not exist.
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
		///    A <see cref="PrivateFrame" /> object containing the
		///    matching frame, or <see langword="null" /> if a match
		///    wasn't found and <paramref name="create" /> is <see
		///    langword="false" />.
		/// </returns>
		public static PrivateFrame Get (Tag tag, string owner,
		                                bool create)
		{
			PrivateFrame priv;
			
			foreach (Frame frame in tag.GetFrames (FrameType.PRIV)) {
				priv = frame as PrivateFrame;
				if (priv != null && priv.Owner == owner)
					return priv;
			}
			
			if (!create)
				return null;
			
			priv = new PrivateFrame (owner);
			tag.AddFrame (priv);
			return priv;
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
			if (data.Count < 1)
				throw new CorruptFileException (
					"A private frame must contain at least 1 byte.");
			
			ByteVectorCollection l = ByteVectorCollection.Split (
				data,
				ByteVector.TextDelimiter (StringType.Latin1),
				1, 2);
			
			if (l.Count == 2) {
				this.owner = l [0].ToString (StringType.Latin1);
				this.data  = l [1];
			}
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
		/// <exception cref="NotImplementedException">
		///    <paramref name="version" /> is less than 3. ID3v2.2 does
		///    not support this frame.
		/// </exception>
		protected override ByteVector RenderFields (byte version)
		{
			if (version < 3)
				throw new NotImplementedException ();
			
			ByteVector v = new ByteVector ();
			
			v.Add (ByteVector.FromString (owner, StringType.Latin1));
			v.Add (ByteVector.TextDelimiter (StringType.Latin1));
			v.Add (data);
			
			return v;
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
			PrivateFrame frame = new PrivateFrame (owner);
			if (data != null)
				frame.data = new ByteVector (data);
			return frame;
		}
		
#endregion
	}
}
