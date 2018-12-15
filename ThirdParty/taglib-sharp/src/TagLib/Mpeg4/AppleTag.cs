//
// AppleTag.cs: Provides support for processing Apple "ilst" tags.
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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace TagLib.Mpeg4 {
	/// <summary>
	///    This class extends <see cref="TagLib.Tag" /> to provide support
	///    for processing Apple "ilst" tags.
	/// </summary>
	public class AppleTag : TagLib.Tag, IEnumerable<Box>
	{
		#region Private Fields
		
		/// <summary>
		///    Contains the ISO meta box in which that tag will be
		///    stored.
		/// </summary>
		private IsoMetaBox meta_box;
		
		/// <summary>
		///    Contains the ILST box which holds all the values.
		/// </summary>
		private AppleItemListBox ilst_box;
		
		#endregion
		
		
		
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="AppleTag" /> for a specified ISO user data box.
		/// </summary>
		/// <param name="box">
		///    A <see cref="IsoUserDataBox" /> from which the tag is to
		///    be read.
		/// </param>
		public AppleTag (IsoUserDataBox box)
		{
			if (box == null)
				throw new ArgumentNullException ("box");
			
			meta_box = box.GetChild (BoxType.Meta) as IsoMetaBox;
			if (meta_box == null) {
				meta_box = new IsoMetaBox ("mdir", null);
				box.AddChild (meta_box);
			}
			
			ilst_box = meta_box.GetChild (BoxType.Ilst)
				as AppleItemListBox;
			
			if (ilst_box == null) {
				ilst_box = new AppleItemListBox ();
				meta_box.AddChild (ilst_box);
			}
		}
		
		#endregion
		
		
		
		#region Public Methods
		
		/// <summary>
		///    Gets and sets whether or not the album described by the
		///    current instance is a compilation.
		/// </summary>
		/// <value>
		///    A <see cref="bool" /> value indicating whether or not the
		///    album described by the current instance is a compilation.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "cpil" data box.
		/// </remarks>
		public bool IsCompilation {
			get {
				foreach (AppleDataBox box in DataBoxes (
					BoxType.Cpil))
					return box.Data.ToUInt () != 0;
				
				return false;
			}
			set {
				SetData (BoxType.Cpil, new ByteVector(
					(byte) (value ? 1 : 0)),
					(uint) AppleDataBox.FlagType.ForTempo);
			}
		}
		
		#endregion
		
		
		
		#region Public Methods
		
		/// <summary>
		///    Gets all data boxes that match any of the provided types.
		/// </summary>
		/// <param name="types">
		///    A <see cref="T:System.Collections.Generic.IEnumerable`1" /> object enumerating a list
		///    of box types to match.
		/// </param>
		/// <returns>
		///    A <see cref="T:System.Collections.Generic.IEnumerable`1" /> object enumerating the
		///    matching boxes.
		/// </returns>
		public IEnumerable<AppleDataBox> DataBoxes (IEnumerable<ByteVector> types)
		{
			// Check each box to see if the match any of the
			// provided types. If a match is found, loop through the
			// children and add any data box.
			foreach (Box box in ilst_box.Children)
				foreach (ByteVector v in types) {
					if (FixId (v) != box.BoxType)
						continue;
					foreach (Box data_box in box.Children) {
						AppleDataBox adb = data_box as
							AppleDataBox;
						if (adb != null)
							yield return adb;
					}
				}
		}
		
		/// <summary>
		///    Gets all data boxes that match any of the provided types.
		/// </summary>
		/// <param name="types">
		///    A <see cref="T:ByteVector[]" /> containing list of box
		///    types to match.
		/// </param>
		/// <returns>
		///    A <see cref="T:System.Collections.Generic.IEnumerable`1" /> object enumerating the
		///    matching boxes.
		/// </returns>
		public IEnumerable<AppleDataBox> DataBoxes (params ByteVector [] types)
		{
			return DataBoxes (types as IEnumerable<ByteVector>);
		}
		
		/// <summary>
		///    Gets all custom data boxes that match the specified mean
		///    and name pair.
		/// </summary>
		/// <param name="mean">
		///    A <see cref="string" /> object containing the "mean" to
		///    match.
		/// </param>
		/// <param name="name">
		///    A <see cref="string" /> object containing the name to
		///    match.
		/// </param>
		/// <returns>
		///    A <see cref="T:System.Collections.Generic.IEnumerable`1" /> object enumerating the
		///    matching boxes.
		/// </returns>
		public IEnumerable<AppleDataBox> DataBoxes (string mean, string name)
		{
			// These children will have a box type of "----"
			foreach (Box box in ilst_box.Children) {
				if (box.BoxType != BoxType.DASH)
					continue;
				
				// Get the mean and name boxes, make sure
				// they're legit, and make sure that they match
				// what we want. Then loop through and add all
				// the data box children to our output.
				AppleAdditionalInfoBox mean_box =
					(AppleAdditionalInfoBox)
					box.GetChild (BoxType.Mean);
				AppleAdditionalInfoBox name_box =
					(AppleAdditionalInfoBox)
					box.GetChild (BoxType.Name);
				
				if (mean_box == null || name_box == null ||
					mean_box.Text != mean ||
					name_box.Text != name)
					continue;
				
				foreach (Box data_box in box.Children) {
					AppleDataBox adb =
						data_box as AppleDataBox;
					
					if (adb != null)
						yield return adb;
				}
			}
		}

		/// <summary>
		///    Gets all text values contained in a specified box type.
		/// </summary>
		/// <param name="type">
		///    A <see cref="ByteVector" /> object containing the box
		///    type to match.
		/// </param>
		/// <returns>
		///    A <see cref="T:string[]" /> containing text from all
		///    matching boxes.
		/// </returns>
		public string [] GetText (ByteVector type) {
			List<string> result = new List<string> ();
			foreach (AppleDataBox box in DataBoxes (type)) {
				if (box.Text == null)
					continue;
				
				foreach (string text in box.Text.Split (';'))
					result.Add (text.Trim ());
			}
			
			return result.ToArray ();
		}
		
		/// <summary>
		///    Sets the data for a specified box type to a collection of
		///    boxes.
		/// </summary>
		/// <param name="type">
		///    A <see cref="ByteVector" /> object containing the type to
		///    add to the new instance.
		/// </param>
		/// <param name="boxes">
		///    A <see cref="T:AppleDataBox[]" /> containing boxes to add
		///    for the specified type.
		/// </param>
		public void SetData (ByteVector type, AppleDataBox [] boxes)
		{
			// Fix the type.
			type = FixId (type);
			
			bool added = false;
			
			foreach (Box box in ilst_box.Children)
				if (type == box.BoxType) {
					
					// Clear the box's children.
					box.ClearChildren ();
					
					// If we've already added new childen,
					// continue.
					if (added)
						continue;
					
					added = true;
					
					// Add the children.
					foreach (AppleDataBox b in boxes)
						box.AddChild (b);
				}
			
			if (added)
				return;
			
			Box box2 = new AppleAnnotationBox (type);
			ilst_box.AddChild (box2);
			
			foreach (AppleDataBox b in boxes)
				box2.AddChild (b);
		}
		
		/// <summary>
		///    Sets the data for a specified box type using values from
		///    a <see cref="ByteVectorCollection" /> object.
		/// </summary>
		/// <param name="type">
		///    A <see cref="ByteVector" /> object containing the type to
		///    add to the new instance.
		/// </param>
		/// <param name="data">
		///    A <see cref="ByteVectorCollection" /> object containing
		///    data to add for the specified type.
		/// </param>
		/// <param name="flags">
		///    A <see cref="uint" /> value containing flags to use for
		///    the added boxes.
		/// </param>
		public void SetData (ByteVector type, ByteVectorCollection data,
		                     uint flags)
		{
			if (data == null || data.Count == 0) {
				ClearData (type);
				return;
			}
			
			AppleDataBox [] boxes = new AppleDataBox [data.Count];
			for (int i = 0; i < data.Count; i ++)
				boxes [i] = new AppleDataBox (data [i], flags);
			
			SetData (type, boxes);
		}
		
		/// <summary>
		///    Sets the data for a specified box type using a single
		///    <see cref="ByteVector" /> object.
		/// </summary>
		/// <param name="type">
		///    A <see cref="ByteVector" /> object containing the type to
		///    add to the new instance.
		/// </param>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing data to add
		///    for the specified type.
		/// </param>
		/// <param name="flags">
		///    A <see cref="uint" /> value containing flags to use for
		///    the added box.
		/// </param>
		public void SetData (ByteVector type, ByteVector data, uint flags)
		{
			if (data == null || data.Count == 0)
				ClearData (type);
			else
				SetData (type, new ByteVectorCollection (data),
					flags);
		}
	
		/// <summary>
		///    Sets the text for a specified box type.
		/// </summary>
		/// <param name="type">
		///    A <see cref="ByteVector" /> object containing the type to
		///    add to the new instance.
		/// </param>
		/// <param name="text">
		///    A <see cref="T:string[]" /> containing text to store.
		/// </param>
		public void SetText (ByteVector type, string [] text)
		{
			// Remove empty data and return.
			if (text == null) {
				ilst_box.RemoveChild (FixId (type));
				return;
			}
			
			SetText (type, string.Join ("; ", text));
		}
		
		/// <summary>
		///    Sets the text for a specified box type.
		/// </summary>
		/// <param name="type">
		///    A <see cref="ByteVector" /> object containing the type to
		///    add to the new instance.
		/// </param>
		/// <param name="text">
		///    A <see cref="string" /> object containing text to store.
		/// </param>
		public void SetText (ByteVector type, string text)
		{
			// Remove empty data and return.
			if (string.IsNullOrEmpty (text)) {
				ilst_box.RemoveChild (FixId (type));
				return;
			}
			
			ByteVectorCollection l = new ByteVectorCollection ();
			l.Add (ByteVector.FromString (text, StringType.UTF8));
			SetData (type, l, (uint)
				AppleDataBox.FlagType.ContainsText);
		}
		
		/// <summary>
		///    Clears all data for a specified box type.
		/// </summary>
		/// <param name="type">
		///    A <see cref="ByteVector" /> object containing the type of
		///    box to remove from the current instance.
		/// </param>
		public void ClearData (ByteVector type)
		{
			ilst_box.RemoveChild (FixId (type));
		}
		
		/// <summary>
		///    Detaches the internal "ilst" box from its parent element.
		/// </summary>
		public void DetachIlst ()
		{
			meta_box.RemoveChild (ilst_box);
		}
		
		/// <summary>
		/// Gets the text string from a specific data box in a Dash (----) atom
		/// </summary>
		/// <param name="meanstring">String specifying text from mean box</param>
		/// <param name="namestring">String specifying text from name box</param>
		/// <returns>Text string from data box</returns>
		public string GetDashBox(string meanstring, string namestring)
		{
			AppleDataBox data_box = GetDashAtoms(meanstring, namestring);
			if (data_box != null) {
				return data_box.Text;
			} else {
				return null;
			}
		}
			
		/// <summary>
		/// Sets a specific strings in Dash (----) atom.  This method updates
		/// and existing atom, or creates a new one.  If an empty datastring is
		/// specified, the Dash box and its children are removed.
		/// </summary>
		/// <param name="meanstring">String specifying text for mean box</param>
		/// <param name="namestring">String specifying text for name box</param>
		/// <param name="datastring">String specifying text for data box</param>
		public void SetDashBox(string meanstring, string namestring, string datastring)
		{
			AppleDataBox data_box = GetDashAtoms(meanstring, namestring);
			
			// If we did find a data_box and we have an empty datastring we should
			// remove the entire dash box.
			if (data_box != null && string.IsNullOrEmpty(datastring)) {
				AppleAnnotationBox dash_box = GetParentDashBox(meanstring, namestring);
				dash_box.ClearChildren();
				ilst_box.RemoveChild(dash_box);
				return;
			}
			
			if (data_box != null) {
				data_box.Text = datastring;
			} else {
				//Create the new boxes, should use 1 for text as a flag
				AppleAdditionalInfoBox amean_box = new AppleAdditionalInfoBox(BoxType.Mean);
				AppleAdditionalInfoBox aname_box = new AppleAdditionalInfoBox(BoxType.Name);
				AppleDataBox adata_box = new AppleDataBox(BoxType.Data, 1);
				amean_box.Text = meanstring;
				aname_box.Text = namestring;
				adata_box.Text = datastring;
				AppleAnnotationBox whole_box = new AppleAnnotationBox(BoxType.DASH);
				whole_box.AddChild(amean_box);
				whole_box.AddChild(aname_box);
				whole_box.AddChild(adata_box);
				ilst_box.AddChild(whole_box);
			}
		}
		
		/// <summary>
		/// Gets the AppleDataBox that corresponds to the specified mean and name values.
		/// </summary>
		/// <param name="meanstring">String specifying text for mean box</param>
		/// <param name="namestring">String specifying text for name box</param>
		/// <returns>Existing AppleDataBox or null if one does not exist</returns>
		private AppleDataBox GetDashAtoms(string meanstring, string namestring)
		{
			foreach (Box box in ilst_box.Children) {
				if (box.BoxType != BoxType.DASH)
					continue;
				
				// Get the mean and name boxes, make sure
				// they're legit, check the Text fields for
				// a match.  If we have a match return
				// the AppleDatabox containing the data
				
				AppleAdditionalInfoBox mean_box =
					(AppleAdditionalInfoBox)
					box.GetChild(BoxType.Mean);
				AppleAdditionalInfoBox name_box =
					(AppleAdditionalInfoBox)
					box.GetChild(BoxType.Name);
					
				if (mean_box == null || name_box == null ||
					mean_box.Text != meanstring ||
					name_box.Text != namestring) {
					continue;
				} else {
					return (AppleDataBox)box.GetChild(BoxType.Data);
				}
			}
			// If we haven't returned the found box yet, there isn't one, return null
			return null;
		}
		
		/// <summary>
		/// Returns the Parent Dash box object for a given mean/name combination
		/// </summary>
		/// <param name="meanstring">String specifying text for mean box</param>
		/// <param name="namestring">String specifying text for name box</param>
		/// <returns>AppleAnnotationBox object that is the parent for the mean/name combination</returns>
		private AppleAnnotationBox GetParentDashBox(string meanstring, string namestring)
		{
			foreach (Box box in ilst_box.Children) {
				if (box.BoxType != BoxType.DASH)
					continue;
				
				// Get the mean and name boxes, make sure
				// they're legit, check the Text fields for
				// a match.  If we have a match return
				// the AppleAnnotationBox that is the Parent
				
				AppleAdditionalInfoBox mean_box =
					(AppleAdditionalInfoBox)
					box.GetChild(BoxType.Mean);
				AppleAdditionalInfoBox name_box =
					(AppleAdditionalInfoBox)
					box.GetChild(BoxType.Name);
					
				if (mean_box == null || name_box == null ||
					mean_box.Text != meanstring ||
					name_box.Text != namestring) {
					continue;
				} else {
					return (AppleAnnotationBox)box;
				}
			}
			// If we haven't returned the found box yet, there isn't one, return null
			return null;
		}
		#endregion
		
		
		
		#region Internal Methods
		
		/// <summary>
		///    Converts the provided ID into a readonly ID and fixes a
		///    3 byte ID.
		/// </summary>
		/// <param name="id">
		///    A <see cref="ByteVector" /> object containing an ID to
		///    fix.
		/// </param>
		/// <returns>
		///    A fixed <see cref="ReadOnlyByteVector" /> or <see
		///    langword="null" /> if the ID could not be fixed.
		/// </returns>
		internal static ReadOnlyByteVector FixId (ByteVector id)
		{
			if (id.Count == 4) {
				ReadOnlyByteVector roid =
					id as ReadOnlyByteVector;
				if (roid != null)
					return roid;
				
				return new ReadOnlyByteVector (id);
			}
			
			if (id.Count == 3)
				return new ReadOnlyByteVector (
					0xa9, id [0], id [1], id [2]);
			
			return null;
		}
		
		#endregion
		
		
		
		#region IEnumerable<Box>
		
		/// <summary>
		///    Gets an enumerator for enumerating through the tag's data
		///    boxes.
		/// </summary>
		/// <returns>
		///    A <see cref="T:System.Collections.IEnumerator`1" /> for
		///    enumerating through the tag's data boxes.
		/// </returns>
		public IEnumerator<Box> GetEnumerator()
		{
			return ilst_box.Children.GetEnumerator();
		}
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ilst_box.Children.GetEnumerator();
		}
		
		#endregion
		
		
		
		#region TagLib.Tag
		
		/// <summary>
		///    Gets the tag types contained in the current instance.
		/// </summary>
		/// <value>
		///    Always <see cref="TagTypes.Apple" />.
		/// </value>
		public override TagTypes TagTypes {
			get {return TagTypes.Apple;}
		}
		
		/// <summary>
		///    Gets and sets the title for the media described by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the title for
		///    the media described by the current instance or <see
		///    langword="null" /> if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "@nam" data box.
		/// </remarks>
		public override string Title {
			get {
				string [] text = GetText (BoxType.Nam);
				return text.Length == 0 ? null : text [0];
			}
			set {
				SetText (BoxType.Nam, value);
			}
		}

		/// <summary>
		///    Gets and sets a short description, one-liner. 
		///    It represents the tagline of the Video/music.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the subtitle
		///    the media represented by the current instance 
		///    or an empty array if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "Subt" data box.
		///    Should be: ----:com.apple.iTunes:SUBTITLE
		/// </remarks>
		public override string Subtitle
		{
			get
			{
				string[] text = GetText(BoxType.Subt);
				return text.Length == 0 ? null : text[0];
			}
			set
			{
				SetText(BoxType.Subt, value);
			}
		}

		/// <summary>
		///    Gets and sets a short description of the media.
		///    For a music, this could be the comment that the artist
		///    made of its artwork. For a video, this should be a 
		///    short summary of the story/plot, but a spoiler. This
		///    should give the impression of what to expect in the
		///    media.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the subtitle
		///    the media represented by the current instance 
		///    or an empty array if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "Desc" data box.
		/// </remarks>
		public override string Description
		{
			get
			{
				string[] text = GetText(BoxType.Desc);
				return text.Length == 0 ? null : text[0];
			}
			set
			{
				SetText(BoxType.Desc, value);
			}
		}
		
		/// <summary>
		///    Gets and sets the performers or artists who performed in
		///    the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing the performers or
		///    artists who performed in the media described by the
		///    current instance or an empty array if no value is
		///    present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "@ART" data box.
		/// </remarks>
		public override string [] Performers {
			get {return GetText (BoxType.Art);}
			set {SetText (BoxType.Art, value);}
		}

		/// <summary>
		///    Gets and sets the Charaters for a video media, or
		///    instruments played for music media. 
		///    This should match the <see cref="Performers"/> array (for
		///    each person correspond one/more role). Several roles for
		///    the same artist/actor can be made up with semicolons. 
		///    For example, "Marty McFly; Marty McFly Jr.; Marlene McFly".
		/// </summary>
		/// <remarks>
		///    This property is implemented using the "role" data box.
		/// </remarks>
		public override string[] PerformersRole
		{
			get
			{
				string[] ret =  GetText(BoxType.Role);
				if (ret == null) return ret;

				// Reformat '/' to ';'
				for (int i = 0; i < ret.Length; i++)
				{
					ret[i] = ret[i].Replace('/', ';').Trim();
				}
				return ret;
			}
			set
			{
				string[] ret = value;
				if (ret != null)
				{
					// Reformat ';' to '/'
					for (int i = 0; i < ret.Length; i++)
					{
						ret[i] = ret[i].Replace(';', '/');
					}
				}
				SetText(BoxType.Role, value);
			}
		}


		/// <summary>
		///    Gets and sets the band or artist who is credited in the
		///    creation of the entire album or collection containing the
		///    media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing the band or artist
		///    who is credited in the creation of the entire album or
		///    collection containing the media described by the current
		///    instance or an empty array if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "aART" data box.
		/// </remarks>
		public override string [] AlbumArtists {
			get {return GetText (BoxType.Aart);}
			set {SetText(BoxType.Aart, value);}
		}
		
		/// <summary>
		///    Gets and sets the composers of the media represented by
		///    the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing the composers of the
		///    media represented by the current instance or an empty
		///    array if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "@wrt" data box.
		/// </remarks>
		public override string [] Composers {
			get {return GetText (BoxType.Wrt);}
			set {SetText (BoxType.Wrt, value);}
		}
		
		/// <summary>
		///    Gets and sets the album of the media represented by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the album of
		///    the media represented by the current instance or <see
		///    langword="null" /> if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "@alb" data box.
		/// </remarks>
		public override string Album {
			get {
				string [] text = GetText (BoxType.Alb);
				return text.Length == 0 ? null : text [0];
			}
			set {SetText (BoxType.Alb, value);}
		}
		
		/// <summary>
		///    Gets and sets a user comment on the media represented by
		///    the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing user comments
		///    on the media represented by the current instance or <see
		///    langword="null" /> if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "@cmt" data box.
		/// </remarks>
		public override string Comment {
			get {
				string [] text = GetText (BoxType.Cmt);
				return text.Length == 0 ? null : text [0];
			}
			set {SetText (BoxType.Cmt, value);}
		}
		
		/// <summary>
		///    Gets and sets the genres of the media represented by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing the genres of the
		///    media represented by the current instance or an empty
		///    array if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "@gen" and "gnre"
		///    data boxes.
		/// </remarks>
		public override string [] Genres {
			get {
				string [] text = GetText (BoxType.Gen);
				if (text.Length > 0)
					return text;
				
				foreach (AppleDataBox box in DataBoxes (BoxType.Gnre)) {
					if (box.Flags != (int) AppleDataBox
						.FlagType.ContainsData)
						continue;
					
					// iTunes stores genre's in the GNRE box
					// as (ID3# + 1).
					
					ushort index = box.Data.ToUShort (true);
					if (index == 0) continue;
					
					string str = TagLib.Genres
						.IndexToAudio ((byte) (index - 1));
					
					if (str == null)
						continue;
					
					text = new string [] {str};
					break;
				}
				
				return text;
			}
			set {
				ClearData (BoxType.Gnre);
				SetText (BoxType.Gen, value);
			}
		}
		
		/// <summary>
		///    Gets and sets the year that the media represented by the
		///    current instance was recorded.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> containing the year that the media
		///    represented by the current instance was created or zero
		///    if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "@day" data box.
		/// </remarks>
		public override uint Year {
			get {
				uint value;
				foreach (AppleDataBox box in DataBoxes (BoxType.Day))
					if (box.Text != null && (uint.TryParse (
						box.Text, out value) ||
						uint.TryParse (
							box.Text.Length > 4 ?
							box.Text.Substring (0, 4)
							: box.Text, out value)))
						return value;
				
				return 0;
			}
			set {
				if (value == 0)
					ClearData (BoxType.Day);
				else
					SetText (BoxType.Day, value.ToString (
						CultureInfo.InvariantCulture));
			}
		}
		
		/// <summary>
		///    Gets and sets the position of the media represented by
		///    the current instance in its containing album.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> containing the position of the
		///    media represented by the current instance in its
		///    containing album or zero if not specified.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "trkn" data box.
		/// </remarks>
		public override uint Track {
			get {
				foreach (AppleDataBox box in DataBoxes (BoxType.Trkn))
					if (box.Flags == (int)
						AppleDataBox.FlagType.ContainsData &&
						box.Data.Count >= 4)
						return box.Data.Mid (2, 2).ToUShort ();
				
				return 0;
			}
			set {
				uint count = TrackCount;
				if (value == 0 && count == 0) {
					ClearData (BoxType.Trkn);
					return;
				}
				
				ByteVector v = ByteVector.FromUShort (0);
				v.Add (ByteVector.FromUShort ((ushort) value));
				v.Add (ByteVector.FromUShort ((ushort) count));
				v.Add (ByteVector.FromUShort (0));
				
				SetData (BoxType.Trkn, v, (int)
					AppleDataBox.FlagType.ContainsData);
			}
		}
		
		/// <summary>
		///    Gets and sets the number of tracks in the album
		///    containing the media represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> containing the number of tracks in
		///    the album containing the media represented by the current
		///    instance or zero if not specified.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "trkn" data box.
		/// </remarks>
		public override uint TrackCount {
			get {
				foreach (AppleDataBox box in DataBoxes (BoxType.Trkn))
					if (box.Flags == (int)
						AppleDataBox.FlagType.ContainsData &&
						box.Data.Count >= 6)
						return box.Data.Mid (4, 2).ToUShort ();
				
				return 0;
			}
			set {
				uint track = Track;
				if (value == 0 && track == 0) {
					ClearData (BoxType.Trkn);
					return;
				}
				
				ByteVector v = ByteVector.FromUShort (0);
				v.Add (ByteVector.FromUShort ((ushort) track));
				v.Add (ByteVector.FromUShort ((ushort) value));
				v.Add (ByteVector.FromUShort (0));
				SetData (BoxType.Trkn, v, (int)
					AppleDataBox.FlagType.ContainsData);
			}
		}
		
		/// <summary>
		///    Gets and sets the number of the disc containing the media
		///    represented by the current instance in the boxed set.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> containing the number of the disc
		///    containing the media represented by the current instance
		///    in the boxed set.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "disk" data box.
		/// </remarks>
		public override uint Disc {
			get {
				foreach (AppleDataBox box in DataBoxes (BoxType.Disk))
					if (box.Flags == (int)
						AppleDataBox.FlagType.ContainsData &&
						box.Data.Count >= 4)
						return box.Data.Mid (2, 2).ToUShort ();
				
				return 0;
			}
			set {
				uint count = DiscCount;
				if (value == 0 && count == 0) {
					ClearData (BoxType.Disk);
					return;
				}
				
				ByteVector v = ByteVector.FromUShort (0);
				v.Add (ByteVector.FromUShort ((ushort) value));
				v.Add (ByteVector.FromUShort ((ushort) count));
				v.Add (ByteVector.FromUShort (0));
				
				SetData (BoxType.Disk, v, (int)
					AppleDataBox.FlagType.ContainsData);
			}
		}
		
		/// <summary>
		///    Gets and sets the number of discs in the boxed set
		///    containing the media represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> containing the number of discs in
		///    the boxed set containing the media represented by the
		///    current instance or zero if not specified.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "disk" data box.
		/// </remarks>
		public override uint DiscCount {
			get {
				foreach (AppleDataBox box in DataBoxes (BoxType.Disk))
					if (box.Flags == (int)
						AppleDataBox.FlagType.ContainsData &&
						box.Data.Count >= 6)
						return box.Data.Mid (4, 2).ToUShort ();
				
				return 0;
			}
			set {
				uint disc = Disc;
				if (value == 0 && disc == 0) {
					ClearData (BoxType.Disk);
					return;
				}
				
				ByteVector v = ByteVector.FromUShort (0);
				v.Add (ByteVector.FromUShort ((ushort) disc));
				v.Add (ByteVector.FromUShort ((ushort) value));
				v.Add (ByteVector.FromUShort (0));
				SetData (BoxType.Disk, v, (int)
					AppleDataBox.FlagType.ContainsData);
			}
		}
		
		/// <summary>
		///    Gets and sets the lyrics or script of the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the lyrics or
		///    script of the media represented by the current instance
		///    or <see langword="null" /> if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "@lyr" data box.
		/// </remarks>
		public override string Lyrics {
			get {
				foreach (AppleDataBox box in DataBoxes (BoxType.Lyr))
					return box.Text;
				return null;
			}
			set {
				SetText (BoxType.Lyr, value);
			}
		}
		
		/// <summary>
		///    Gets and sets the grouping on the album which the media
		///    in the current instance belongs to.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the grouping on
		///    the album which the media in the current instance belongs
		///    to or <see langword="null" /> if no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "@grp" data box.
		/// </remarks>
		public override string Grouping {
			get {
				foreach (AppleDataBox box in DataBoxes(BoxType.Grp))
					return box.Text;
				
				return null;
			}
			set {SetText(BoxType.Grp, value);}
		}
		
		/// <summary>
		///    Gets and sets the number of beats per minute in the audio
		///    of the media represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="uint" /> containing the number of beats per
		///    minute in the audio of the media represented by the
		///    current instance, or zero if not specified.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "tmpo" data box.
		/// </remarks>
		public override uint BeatsPerMinute {
			get {
				foreach (AppleDataBox box in DataBoxes (BoxType.Tmpo))
					if (box.Flags == (uint)
						AppleDataBox.FlagType.ForTempo)
						return box.Data.ToUInt ();
				
				return 0;
			}
			set {
				if (value == 0) {
					ClearData (BoxType.Tmpo);
					return;
				}
				
				SetData (BoxType.Tmpo,
					ByteVector.FromUShort ((ushort)value),
					(uint) AppleDataBox.FlagType.ForTempo);
			}
		}
		
		/// <summary>
		///    Gets and sets the conductor or director of the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the conductor
		///    or director of the media represented by the current
		///    instance or <see langword="null" /> if no value present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "cond" data box.
		/// </remarks>
		public override string Conductor {
			get {
				foreach (AppleDataBox box in DataBoxes(BoxType.Cond))
					return box.Text;
				
				return null;
			}
			set {SetText(BoxType.Cond, value);}
		}
		
		/// <summary>
		///    Gets and sets the copyright information for the media
		///    represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the copyright
		///    information for the media represented by the current
		///    instance or <see langword="null" /> if no value present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "cprt" data box.
		/// </remarks>
		public override string Copyright {
			get {
				foreach (AppleDataBox box in DataBoxes(BoxType.Cprt))
					return box.Text;
				
				return null;
			}
			set {SetText(BoxType.Cprt, value);}
		}

		/// <summary>
		///    Gets and sets the date at which the tag has been written.
		/// </summary>
		/// <value>
		///    A nullable <see cref="DateTime" /> object containing the 
		///    date at which the tag has been written, or <see 
		///    langword="null" /> if no value present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "dtag" data box.
		/// </remarks>
		public override DateTime? DateTagged
		{
			get
			{
				string[] text = GetText(BoxType.Dtag);
				string value = text.Length == 0 ? null : text[0];
				if (value != null)
				{
					DateTime date;
					if (DateTime.TryParseExact(value, "yyyy-MM-dd HH:mm:ss", null, DateTimeStyles.None, out date))
					{
						return date;
					}
				}
				return null;
			}
			set
			{
				string date = null;
				if (value != null)
				{
					date = string.Format("{0:yyyy-MM-dd HH:mm:ss}", value);
				}
				SetText(BoxType.Dtag, date);
			}
		}
		/// <summary>
		///    Gets and sets the sort names for the band or artist who
		///    is credited in the creation of the entire album or
		///    collection containing the media described by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing the sort names
		///    for the band or artist who is credited in the creation
		///    of the entire album or collection containing the media
		///    described by the current instance or an empty array if
		///    no value is present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "soaa"
		///    Box type.
		///    http://musicbrainz.org/doc/PicardTagMapping
		///    http://code.google.com/p/mp4v2/wiki/iTunesMetadata
		/// </remarks>
		public override string [] AlbumArtistsSort {
			get {return GetText (BoxType.Soaa);}
			set {SetText (BoxType.Soaa, value);}
		}
		
		/// <summary>
		///    Gets and sets the sort names of the performers or artists
		///    who performed in the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing the sort names for
		///    the performers or artists who performed in the media
		///    described by the current instance, or an empty array if
		///    no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "soar" box type.
		///    http://musicbrainz.org/doc/PicardTagMapping
		///    http://code.google.com/p/mp4v2/wiki/iTunesMetadata
		/// </remarks>
		public override string[] PerformersSort {
			get {return GetText (BoxType.Soar);}
			set {SetText (BoxType.Soar, value);}
		}
		
		/// <summary>
		///    Gets and sets the sort names of the Composer credited
		///    in the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:string[]" /> containing the sort names for
		///    the Composers in the media described by the current instance,
		///    or an empty array if no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "soar" box type.
		///    http://musicbrainz.org/doc/PicardTagMapping
		///    http://code.google.com/p/mp4v2/wiki/iTunesMetadata
		/// </remarks>
		public override string[] ComposersSort {
			get {return GetText (BoxType.Soco);}
			set {SetText (BoxType.Soco, value);}
		}
		
		/// <summary>
		///    Gets and sets the sort names of the Album Title of
		///    the media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the sort names for
		///    the Album Title in the media described by the current
		///    instance, or null if no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "soal" box type.
		///    http://musicbrainz.org/doc/PicardTagMapping
		///    http://code.google.com/p/mp4v2/wiki/iTunesMetadata
		/// </remarks>
		public override string AlbumSort {
			get {
				string [] text = GetText (BoxType.Soal);
				return text.Length == 0 ? null : text [0];
			}
			set {SetText (BoxType.Soal, value);}
		}
		
		/// <summary>
		///    Gets and sets the sort names of the Track Title in the
		///    media described by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the sort names for
		///    the Track Title in the media described by the current 
		///    instance, or null if no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "sonm" box type.
		///    http://musicbrainz.org/doc/PicardTagMapping
		///    http://code.google.com/p/mp4v2/wiki/iTunesMetadata
		/// </remarks>
		public override string TitleSort {
			get {
				string [] text = GetText (BoxType.Sonm);
				return text.Length == 0 ? null : text [0];
			}
			set {SetText (BoxType.Sonm, value);}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz ArtistID
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz
		///    ArtistID for the media described by the current 
		///    instance, or null if no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "dash"/"----" box type.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzArtistId {
			get {return GetDashBox("com.apple.iTunes","MusicBrainz Artist Id");}
			set {SetDashBox("com.apple.iTunes", "MusicBrainz Artist Id", value);}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz ReleaseGroupID
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz
		///    ReleaseGroupID for the media described by the current 
		///    instance, or null if no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "dash"/"----" box type.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzReleaseGroupId {
			get { return GetDashBox("com.apple.iTunes", "MusicBrainz Release Group Id"); }
			set { SetDashBox("com.apple.iTunes", "MusicBrainz Release Group Id", value); }
		}

		/// <summary>
		///    Gets and sets the MusicBrainz ReleaseID
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz
		///    ReleaseID for the media described by the current 
		///    instance, or null if no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "dash"/"----" box type.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzReleaseId {
			get {return GetDashBox("com.apple.iTunes","MusicBrainz Album Id");}
			set {SetDashBox("com.apple.iTunes", "MusicBrainz Album Id",value);}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz ReleaseArtistID
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz
		///    ReleaseArtistID for the media described by the current 
		///    instance, or null if no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "dash"/"----" box type.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzReleaseArtistId {
			get {return GetDashBox("com.apple.iTunes","MusicBrainz Album Artist Id");}
			set {SetDashBox("com.apple.iTunes", "MusicBrainz Album Artist Id",value);}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz TrackID
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz
		///    TrackID for the media described by the current 
		///    instance, or null if no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "dash"/"----" box type.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzTrackId {
			get {return GetDashBox("com.apple.iTunes","MusicBrainz Track Id");}
			set {SetDashBox("com.apple.iTunes", "MusicBrainz Track Id", value);}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz DiscID
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz
		///    DiscID for the media described by the current 
		///    instance, or null if no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "dash"/"----" box type.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzDiscId {
			get {return GetDashBox("com.apple.iTunes","MusicBrainz Disc Id");}
			set {SetDashBox("com.apple.iTunes", "MusicBrainz Disc Id",value);}
		}

		/// <summary>
		///    Gets and sets the MusicIP PUID
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicIP Puid
		///    for the media described by the current 
		///    instance, or null if no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "dash"/"----" box type.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicIpId {
			get {return GetDashBox("com.apple.iTunes","MusicIP PUID");}
			set {SetDashBox("com.apple.iTunes", "MusicIP PUID",value);}
		}

		/// <summary>
		///    Gets and sets the AmazonID
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the AmazonID
		///    for the media described by the current 
		///    instance, or null if no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "dash"/"----" box type.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string AmazonId {
			get {return GetDashBox("com.apple.iTunes","ASIN");}
			set {SetDashBox("com.apple.iTunes", "ASIN",value);}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz ReleaseStatus
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz
		///    ReleaseStatus for the media described by the current 
		///    instance, or null if no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "dash"/"----" box type.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzReleaseStatus {
			get {return GetDashBox("com.apple.iTunes","MusicBrainz Album Status");}
			set {SetDashBox("com.apple.iTunes", "MusicBrainz Album Status",value);}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz ReleaseType
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz
		///    ReleaseType for the media described by the current 
		///    instance, or null if no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "dash"/"----" box type.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzReleaseType {
			get {return GetDashBox("com.apple.iTunes","MusicBrainz Album Type");}
			set {SetDashBox("com.apple.iTunes", "MusicBrainz Album Type",value);}
		}

		/// <summary>
		///    Gets and sets the MusicBrainz Release Country
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the MusicBrainz
		///    ReleaseCountry for the media described by the current 
		///    instance, or null if no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "dash"/"----" box type.
		///    http://musicbrainz.org/doc/PicardTagMapping
		/// </remarks>
		public override string MusicBrainzReleaseCountry {
			get {return GetDashBox("com.apple.iTunes","MusicBrainz Album Release Country");}
			set {SetDashBox("com.apple.iTunes", "MusicBrainz Album Release Country",value);}
		}

	/// <summary>
	///    Gets and sets the ReplayGain Track Value of the media represented by
	///    the current instance.
	/// </summary>
	/// <value>
	///    A <see cref="double" /> containing the ReplayGain Track Value of the
	///    media represented by the current instance or an empty
	///    array if no value is present.
	/// </value>
	/// <remarks>
	///    This property is implemented using the "dash"/"----" box type.
	/// </remarks>
	public override double ReplayGainTrackGain
	{
			get
			{
				string text = GetDashBox("com.apple.iTunes", "REPLAYGAIN_TRACK_GAIN");
				double value;

				if (text == null)
				{
					return double.NaN;
				}
				if (text.ToLower(CultureInfo.InvariantCulture).EndsWith("db"))
				{
					text = text.Substring(0, text.Length - 2).Trim();
				}

				if (double.TryParse(text, NumberStyles.Float,
					CultureInfo.InvariantCulture, out value))
				{
					return value;
				}
				return double.NaN;
			}
			set
			{
				string text = value.ToString("0.00 dB",
					CultureInfo.InvariantCulture);
				SetDashBox("com.apple.iTunes", "REPLAYGAIN_TRACK_GAIN", text);
			}
		}

	/// <summary>
	///    Gets and sets the ReplayGain Peak Value of the media represented by
	///    the current instance.
	/// </summary>
	/// <value>
	///    A <see cref="double" /> containing the ReplayGain Peak Value of the
	///    media represented by the current instance or an empty
	///    array if no value is present.
	/// </value>
	/// <remarks>
	///    This property is implemented using the "dash"/"----" box type.
	/// </remarks>
	public override double ReplayGainTrackPeak
	{
			get
			{
				string text;
				double value;

				if ((text = GetDashBox("com.apple.iTunes", "REPLAYGAIN_TRACK_PEAK")) !=
					null && double.TryParse(text, NumberStyles.Float,
						CultureInfo.InvariantCulture, out value))
				{
					return value;
				}
				return double.NaN;
			}
			set
			{
				string text = value.ToString("0.000000", CultureInfo.InvariantCulture);
				SetDashBox("com.apple.iTunes", "REPLAYGAIN_TRACK_PEAK", text);
			}
		}

	/// <summary>
	///    Gets and sets the ReplayGain Album Value of the media represented by
	///    the current instance.
	/// </summary>
	/// <value>
	///    A <see cref="double" /> containing the ReplayGain Album Value of the
	///    media represented by the current instance or an empty
	///    array if no value is present.
	/// </value>
	/// <remarks>
	///    This property is implemented using the "dash"/"----" box type.
	/// </remarks>
	public override double ReplayGainAlbumGain
	{
			get
			{
				string text = GetDashBox("com.apple.iTunes", "REPLAYGAIN_ALBUM_GAIN");
				double value;

				if (text == null)
				{
					return double.NaN;
				}
				if (text.ToLower(CultureInfo.InvariantCulture).EndsWith("db"))
				{
					text = text.Substring(0, text.Length - 2).Trim();
				}

				if (double.TryParse(text, NumberStyles.Float,
					CultureInfo.InvariantCulture, out value))
				{
					return value;
				}
				return double.NaN;
			}
			set
			{
				string text = value.ToString("0.00 dB",
					CultureInfo.InvariantCulture);
				SetDashBox("com.apple.iTunes", "REPLAYGAIN_ALBUM_GAIN", text);
			}
		}

	/// <summary>
	///    Gets and sets the ReplayGain Album Peak Value of the media represented by
	///    the current instance.
	/// </summary>
	/// <value>
	///    A <see cref="double" /> containing the ReplayGain Album Peak Value of the
	///    media represented by the current instance or an empty
	///    array if no value is present.
	/// </value>
	/// <remarks>
	///    This property is implemented using the "dash"/"----" box type.
	/// </remarks>
	public override double ReplayGainAlbumPeak
	{
			get
			{
				string text;
				double value;

				if ((text = GetDashBox("com.apple.iTunes", "REPLAYGAIN_ALBUM_PEAK")) !=
					null && double.TryParse(text, NumberStyles.Float,
						CultureInfo.InvariantCulture, out value))
				{
					return value;
				}
				return double.NaN;
			}
			set
			{
				string text = value.ToString("0.000000", CultureInfo.InvariantCulture);
				SetDashBox("com.apple.iTunes", "REPLAYGAIN_ALBUM_PEAK", text);
			}
		}

		/// <summary>
		///    Gets and sets the InitialKey
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the InitialKey
		///    for the media described by the current  instance, 
		///    or null if no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "dash"/"----" box type.
		/// </remarks>
		public override string InitialKey
		{
			get { return GetDashBox("com.apple.iTunes", "initialkey"); }
			set { SetDashBox("com.apple.iTunes", "initialkey", value); }
		}

		/// <summary>
		///    Gets and sets the ISRC
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the ISRC
		///    for the media described by the current  instance, 
		///    or null if no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "dash"/"----" box type.
		/// </remarks>
		public override string ISRC
		{
			get { return GetDashBox("com.apple.iTunes", "ISRC"); }
			set { SetDashBox("com.apple.iTunes", "ISRC", value); }
		}

		/// <summary>
		///    Gets and sets the Publisher
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the Publisher
		///    for the media described by the current  instance, 
		///    or null if no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "dash"/"----" box type.
		/// </remarks>
		public override string Publisher
		{
			get { return GetDashBox("com.apple.iTunes", "publisher"); }
			set { SetDashBox("com.apple.iTunes", "publisher", value); }
		}

		/// <summary>
		///    Gets and sets the Remixer
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the Remixer
		///    for the media described by the current  instance, 
		///    or null if no value is present. 
		/// </value>
		/// <remarks>
		///    This property is implemented using the "dash"/"----" box type.
		/// </remarks>
		public override string RemixedBy
		{
			get { return GetDashBox("com.apple.iTunes", "REMIXEDBY"); }
			set { SetDashBox("com.apple.iTunes", "REMIXEDBY", value); }
		}

		/// <summary>
		///    Gets and sets a collection of pictures associated with
		///    the media represented by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="T:IPicture[]" /> containing a collection of
		///    pictures associated with the media represented by the
		///    current instance or an empty array if none are present.
		/// </value>
		/// <remarks>
		///    This property is implemented using the "covr" data box.
		/// </remarks>
		public override IPicture [] Pictures {
			get {
				List<Picture> l = new List<Picture> ();
				
				foreach (AppleDataBox box in DataBoxes(BoxType.Covr)) {
					Picture p = new Picture (box.Data);
					l.Add (p);
				}
				
				return (Picture []) l.ToArray ();
			}
			set {
				if (value == null || value.Length == 0) {
					ClearData (BoxType.Covr);
					return;
				}
				
				AppleDataBox [] boxes =
					new AppleDataBox [value.Length];
				for (int i = 0; i < value.Length; i ++) {
					uint type = (uint)
						AppleDataBox.FlagType.ContainsData;
					
					if (value [i].MimeType == "image/jpeg")
						type = (uint)
							AppleDataBox.FlagType.ContainsJpegData;
					else if (value [i].MimeType == "image/png")
						type = (uint)
							AppleDataBox.FlagType.ContainsPngData;
					else if (value[i].MimeType == "image/x-windows-bmp")
						type = (uint)
							AppleDataBox.FlagType.ContainsBmpData;

					boxes[i] = new AppleDataBox (value [i].Data, type);
				}
				
				SetData(BoxType.Covr, boxes);
			}
		}
		
		/// <summary>
		///    Gets whether or not the current instance is empty.
		/// </summary>
		/// <value>
		///    <see langword="true" /> if the current instance does not
		///    any values. Otherwise <see langword="false" />.
		/// </value>
		public override bool IsEmpty {
			get {return !ilst_box.HasChildren;}
		}
		
		/// <summary>
		///    Clears the values stored in the current instance.
		/// </summary>
		public override void Clear ()
		{
			ilst_box.ClearChildren ();
		}
		
		#endregion
	}
}
