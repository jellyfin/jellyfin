//
// Tag.cs:
//
// Author:
//   Guy Taylor (s0700260@sms.ed.ac.uk) (thebigguy.co.uk@gmail.com)
//
// Original Source:
//   Id3v1/Tag.cs from TagLib-sharp
//
// Copyright (C) 2009 Guy Taylor (Original Implementation)
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

namespace TagLib.Audible
{
	
	/// <summary>
	///    This class extends <see cref="Tag" /> to provide support for
	///    reading tags stored in the Audible Metadata format.
	/// </summary>
	public class Tag : TagLib.Tag
	{
		#region Private Fields
		
		private List<KeyValuePair<string, string>> tags;
		
		#endregion
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Tag" /> with no contents.
		/// </summary>
		public Tag ()
		{
			Clear ();
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Tag" /> by reading the contents from a specified
		///    position in a specified file.
		/// </summary>
		/// <param name="file">
		///    A <see cref="File" /> object containing the file from
		///    which the contents of the new instance is to be read.
		/// </param>
		/// <param name="position">
		///    A <see cref="long" /> value specify at what position to
		///    read the tag.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="file" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///    <paramref name="position" /> is less than zero or greater
		///    than the size of the file.
		/// </exception>
		/// <exception cref="CorruptFileException">
		///    The file does not contain FileIdentifier
		///    at the given position.
		/// </exception>
		public Tag (File file, long position)
		{
			// TODO: can we read from file
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="Tag" /> by reading the contents from a specified
		///    <see cref="ByteVector" /> object.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object to read the tag from.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="data" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="CorruptFileException">
		///    <paramref name="data" /> is less than 128 bytes or does
		///    not start with FileIdentifier.
		/// </exception>
		public Tag (ByteVector data)
		{
			
			if (data == null)
				throw new ArgumentNullException ("data");
			
			Clear ();
			Parse (data);
		}
		
		#endregion
		
		#region Private Methods
		
		/// <summary>
		///    Populates the current instance by parsing the contents of
		///    a raw AudibleMetadata tag.
		/// </summary>
		/// <param name="data">
		///    	A <see cref="ByteVector" /> object containing the whole tag
		/// 	object
		/// </param>
		/// <exception cref="CorruptFileException">
		///    <paramref name="data" /> is less than 128 bytes or does
		///    not start with FileIdentifier.
		/// </exception>
		private void Parse (ByteVector data)
		{
			String currentKey, currentValue;
			int keyLen, valueLen;
			
			try
			{
				do
				{
					keyLen = (int) data.ToUInt(true);
					data.RemoveRange (0, 4);
					valueLen = (int) data.ToUInt(true);
					data.RemoveRange (0, 4);
					currentKey = data.ToString ( TagLib.StringType.UTF8, 0, keyLen );
					data.RemoveRange (0, keyLen);
					currentValue = data.ToString ( TagLib.StringType.UTF8, 0, valueLen );
					data.RemoveRange (0, valueLen);
					
					tags.Add( new KeyValuePair<string, string>(currentKey, currentValue) );
					
					//StringHandle (currentKey, currentValue);
					
					// if it is not the last item remove the end byte (null terminated)
					if (data.Count != 0)
						data.RemoveRange(0,1);
				}
				while (data.Count >= 4);
			}
			catch (Exception)
			{
				//
			}
			
			if (data.Count != 0)
				throw new CorruptFileException();
		}

		void setTag (string tagName, string value) {
			for (int i = 0; i < tags.Count; i ++) {
				if(tags[i].Key == tagName)
					tags [i] = new KeyValuePair<string, string> (tags [i].Key, value);
			}
		}

		private string getTag(string tagName){
			foreach( KeyValuePair<string, string> tag in tags) {
				if(tag.Key == tagName)
					return tag.Value;
			}
			return null;
		}

		/*
		/// <summary>
		///		Given a key and value pair it will update the
		///		present metadata.
		/// </summary>
		/// <param name="key">
		///    A <see cref="String" /> containing the key.
		/// </param>
		/// <param name="strValue">
		///    A <see cref="String" /> containing the value.
		/// </param>
		private void StringHandle (String key, String strValue)
		{
			switch (key)
			{
			case "title":
				title = strValue;
				break;
			case "author":
				artist = strValue;
				break;
			case "provider":
				album = strValue;
				break;
			}
			
		}
		*/
		
		#endregion	
		
		#region TagLib.Tag
		
		/// <summary>
		///    Gets the tag types contained in the current instance.
		/// </summary>
		/// <value>
		///    Always <see cref="TagTypes.AudibleMetadata" />.
		/// </value>
		public override TagTypes TagTypes {
			get {return TagTypes.AudibleMetadata;}
		}

		/// <summary>
		/// Get or Set the Author Tag
		/// </summary>

		public string Author {
			get {
				return getTag ("author");
			}
		}

		/// <summary>
		/// Get or Set the Copyright Tag
		/// </summary>

		public override string Copyright {
			get {
				return getTag ("copyright");
			}
			set {
				setTag ("copyright", value);
			}
		}

		/// <summary>
		/// Get or Set the Description Tag
		/// </summary>

		public override string Description {
			get { return getTag ("description"); }
		}

		/// <summary>
		/// Get or Set the Narrator Tag
		/// </summary>
		public string Narrator {
			get {
				return getTag ("narrator");
			}
		}
		
		/// <summary>
		///    Gets the title for the media described by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the title for
		///    the media described by the current instance or <see
		///    langword="null" /> if no value is present.
		/// </value>
		public override string Title {
			get {
				return getTag("title");
			}
		}

		/// <summary>
		///    Gets the album for the media described by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the album for
		///    the media described by the current instance or <see
		///    langword="null" /> if no value is present.
		/// </value>
		public override string Album {
			get {
				return getTag("provider");
				//return string.IsNullOrEmpty (album) ?
				//	null : album;
			}
		}

		/// <summary>
		///    Gets the album artist for the media described by the
		///    current instance.
		/// </summary>
		/// <value>
		///    	A <see cref="T:string[]" /> object containing a single 
		/// 	artist described by the current instance or <see
		///    langword="null" /> if no value is present.
		/// </value>
		public override string[] AlbumArtists {
			get {
				String artist = getTag("provider");
				
				return string.IsNullOrEmpty (artist) ?
					null : new string[] {artist};
			}
		}
		
		/// <summary>
		///    Clears the values stored in the current instance.
		/// </summary>
		public override void Clear ()
		{
			tags = new List<KeyValuePair<string, string>>();
		}
		
		#endregion
		
	}
}

