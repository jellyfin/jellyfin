//
// TermsOfUseFrame.cs:
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Copyright (C) 2007 Brian Nickel
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

namespace TagLib.Id3v2
{
	/// <summary>
	///    This class extends <see cref="Frame" />, implementing support for
	///    ID3v2 Terms of Use (USER) Frames.
	/// </summary>
	/// <remarks>
	///    This frame contains license text or restrictions on the use of a
	///    media file.
	/// </remarks>
	public class TermsOfUseFrame : Frame
	{
#region Private Fields
		
		/// <summary>
		///    Contains the text encoding to use when rendering the
		///    current instance.
		/// </summary>
		private StringType encoding = Tag.DefaultEncoding;
		
		/// <summary>
		///    Contains the ISO-639-2 language code of the current
		///    instance.
		/// </summary>
		private string language = null;
		
		/// <summary>
		///    Contains the text in the current instance.
		/// </summary>
		private string text = null;
		
#endregion
		
		
		
#region Constructors
		
		/// <summary>
		///    Constructs and intializes a new instance of <see
		///    cref="TermsOfUseFrame" /> with a specified language and
		///    encoding.
		/// </summary>
		/// <param name="language">
		///    A <see cref="string" /> containing the ISO-639-2 language
		///    code of the new frame.
		/// </param>
		/// <param name="encoding">
		///    A <see cref="StringType" /> containing the text encoding
		///    to use when rendering the new frame.
		/// </param>
		/// <remarks>
		///    When a frame is created, it is not automatically added to
		///    the tag. Consider using <see cref="Get" /> for more
		///    integrated frame creation.
		/// </remarks>
		public TermsOfUseFrame (string language, StringType encoding)
			: base (FrameType.USER, 4)
		{
			this.encoding = encoding;
			this.language = language;
		}
		
		/// <summary>
		///    Constructs and intializes a new instance of <see
		///    cref="TermsOfUseFrame" /> with a specified language.
		/// </summary>
		/// <param name="language">
		///    A <see cref="string" /> containing the ISO-639-2 language
		///    code of the new frame.
		/// </param>
		/// <remarks>
		///    When a frame is created, it is not automatically added to
		///    the tag. Consider using <see cref="Get" /> for more
		///    integrated frame creation.
		/// </remarks>
		public TermsOfUseFrame (string language)
			: base (FrameType.USER, 4)
		{
			this.language = language;
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="TermsOfUseFrame" /> by reading its raw data in a
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
		public TermsOfUseFrame (ByteVector data, byte version)
			: base (data, version)
		{
			SetData (data, 0, version, true);
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="TermsOfUseFrame" /> by reading its raw data in a
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
		protected internal TermsOfUseFrame (ByteVector data, int offset,
		                                    FrameHeader header,
		                                    byte version)
			: base (header)
		{
			SetData (data, offset, version, false);
		}
		
#endregion
		
		
		
#region Public Properties
		
		/// <summary>
		///    Gets and sets the text encoding to use when storing the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the text encoding to
		///    use when storing the current instance.
		/// </value>
		/// <remarks>
		///    This encoding is overridden when rendering if <see
		///    cref="Tag.ForceDefaultEncoding" /> is <see
		///    langword="true" /> or the render version does not support
		///    it.
		/// </remarks>
		public StringType TextEncoding {
			get {return encoding;}
			set {encoding = value;}
		}
		
		/// <summary>
		///    Gets and sets the ISO-639-2 language code stored in the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the ISO-639-2 language
		///    code stored in the current instance.
		/// </value>
		/// <remarks>
		///    There should only be one file with a matching 
		///    ISO-639-2 language code per tag.
		/// </remarks>
		public string Language {
			get {
				return (language != null && language.Length > 2)
					? language.Substring (0, 3) : "XXX";
			}
			set {language = value;}
		}
		
		/// <summary>
		///    Gets and sets the terms of use stored in the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the terms of
		///    use.
		/// </value>
		public string Text {
			get {return text;}
			set {text = value;}
		}
		
#endregion
		
		
		
#region Public Methods
		
		/// <summary>
		///    Gets a string representation of the current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="string" /> containing the terms of use.
		/// </returns>
		public override string ToString ()
		{
			return text;
		}
		
#endregion
		
		
		
#region Public Static Methods
		
		/// <summary>
		///    Gets a specified terms of use frame from the specified
		///    tag, optionally creating it if it does not exist.
		/// </summary>
		/// <param name="tag">
		///    A <see cref="Tag" /> object to search in.
		/// </param>
		/// <param name="language">
		///    A <see cref="string" /> specifying the ISO-639-2 language
		///   code to match.
		/// </param>
		/// <param name="create">
		///    A <see cref="bool" /> specifying whether or not to create
		///    and add a new frame to the tag if a match is not found.
		/// </param>
		/// <returns>
		///    A <see cref="TermsOfUseFrame" /> object containing the
		///    matching frame, or <see langword="null" /> if a match
		///    wasn't found and <paramref name="create" /> is <see
		///    langword="false" />.
		/// </returns>
		public static TermsOfUseFrame Get (Tag tag, string language,
		                                   bool create)
		{
			foreach (Frame f in tag.GetFrames (FrameType.USER)) {
				TermsOfUseFrame cf = f as TermsOfUseFrame;
				
				if (cf != null && (language == null ||
					language == cf.Language))
					return cf;
			}
			
			if (!create)
				return null;
			
			TermsOfUseFrame frame = new TermsOfUseFrame (language);
			tag.AddFrame (frame);
			return frame;
		}
		
		/// <summary>
		///    Gets a specified terms of use frame from the specified
		///    tag, trying to to match the language but accepting one
		///    with a different language if a match was not found.
		/// </summary>
		/// <param name="tag">
		///    A <see cref="Tag" /> object to search in.
		/// </param>
		/// <param name="language">
		///    A <see cref="string" /> specifying the ISO-639-2 language
		///   code to match.
		/// </param>
		/// <returns>
		///    A <see cref="TermsOfUseFrame" /> object containing the
		///    matching frame, or <see langword="null" /> if a match
		///    wasn't found.
		/// </returns>
		public static TermsOfUseFrame GetPreferred (Tag tag,
		                                            string language)
		{
			TermsOfUseFrame best = null;
			foreach (Frame f in tag.GetFrames (FrameType.USER)) {
				TermsOfUseFrame cf = f as TermsOfUseFrame;
				if (cf == null)
					continue;
				
				if (cf.Language == language)
					return cf;
				
				if (best == null)
					best = cf;
			}
			
			return best;
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
			if (data.Count < 4)
				throw new CorruptFileException (
					"Not enough bytes in field.");
			
			encoding = (StringType) data [0];
			language = data.ToString (StringType.Latin1, 1, 3);
			text = data.ToString (encoding, 4, data.Count - 4);
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
			StringType encoding = CorrectEncoding (TextEncoding,
				version);
			ByteVector v = new ByteVector ();
			
			v.Add ((byte) encoding);
			v.Add (ByteVector.FromString (Language,
				StringType.Latin1));
			v.Add (ByteVector.FromString (text, encoding));
			
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
			TermsOfUseFrame frame = new TermsOfUseFrame (language, encoding);
			frame.text = text;
			return frame;
		}
		
#endregion
	}
}
