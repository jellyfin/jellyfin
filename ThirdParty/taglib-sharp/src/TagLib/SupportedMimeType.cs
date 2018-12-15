//
// SupportedMimeType.cs:
//
// Author:
//   Aaron Bockover (abockover@novell.com)
//
// Original Source:
//   Entagged#
//
// Copyright (C) 2006 Novell, Inc.
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
using System.Collections.Generic;

namespace TagLib {
	/// <summary>
	///    This class provides an attribute for listing supported mime-types
	///    for classes that extend <see cref="File" />.
	/// </summary>
	/// <remarks>
	///    When classes that extend <see cref="File" /> are registered with
	///    <see cref="FileTypes.Register" />, its <see
	///    cref="SupportedMimeType" /> attributes are read.
	/// </remarks>
	/// <example>
	///    <code lang="C#">using TagLib;
	///
	///[SupportedMimeType("taglib/wv", "wv")]
	///[SupportedMimeType("audio/x-wavpack")]
	///public class MyFile : File {
	///	...
	///}</code>
	/// </example>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple=true)]
	public sealed class SupportedMimeType : Attribute
	{
		/// <summary>
		///    Contains the registered <see cref="SupportedMimeType" />
		///    objects.
		/// </summary>
		private static List<SupportedMimeType> mimetypes =
			new List<SupportedMimeType> ();
		
		/// <summary>
		///    Contains the mime-type.
		/// </summary>
		private string mimetype;
		
		/// <summary>
		///    Contains the extension.
		/// </summary>
		private string extension;
		
		/// <summary>
		///    Constructs and initializes the <see
		///    cref="SupportedMimeType" /> class by initializing the
		///    <see cref="FileTypes" /> class.
		/// </summary>
		static SupportedMimeType ()
		{
			FileTypes.Init ();
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of the <see
		///    cref="SupportedMimeType" /> attribute for a specified
		///    mime-type.
		/// </summary>
		/// <param name="mimetype">
		///    A <see cref="string" /> object containing a standard
		///    mime-type.
		/// </param>
		/// <remarks>
		///    <para>Standard practice is to use <see
		///    cref="SupportedMimeType(string)" /> to register standard
		///    mime-types, like "audio/mp3" and "video/mpeg" and to use
		///    <see cref="SupportedMimeType(string,string)" /> strictly
		///    to register extensions, using "taglib/ext" for the mime
		///    type. Eg. <c>SupportedMimeType("taglib/mp3",
		///    "mp3")</c>.</para>
		/// </remarks>
		public SupportedMimeType (string mimetype)
		{
			this.mimetype = mimetype;
			mimetypes.Add (this);
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of the <see
		///    cref="SupportedMimeType" /> attribute for a specified
		///    mime-type and extension.
		/// </summary>
		/// <param name="mimetype">
		///    A <see cref="string" /> object containing a standard
		///    mime-type.
		/// </param>
		/// <param name="extension">
		///    A <see cref="string" /> object containing a file
		///    extension.
		/// </param>
		/// <remarks>
		///    <para>Standard practice is to use <see
		///    cref="SupportedMimeType(string)" /> to register standard
		///    mime-types, like "audio/mp3" and "video/mpeg" and to use
		///    <see cref="SupportedMimeType(string,string)" /> strictly
		///    to register extensions, using "taglib/ext" for the mime
		///    type. Eg. <c>SupportedMimeType("taglib/mp3",
		///    "mp3")</c>.</para>
		/// </remarks>
		public SupportedMimeType (string mimetype, string extension)
			: this (mimetype)
		{
			this.extension = extension;
		}
		
		/// <summary>
		///    Gets the mime-type registered by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the mime-type
		///    registered by the current instance.
		/// </value>
		/// <remarks>
		///    <para>The value is in the format "generic/specific". For
		///    example, "video/mp4".</para>
		/// </remarks>
		public string MimeType {
			get {return mimetype;}
		}
		
		/// <summary>
		///    Gets the extension registered by the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing the extension
		///    registered by the current instance, or <see
		///    langword="null" /> if not specified.
		/// </value>
		/// <remarks>
		///    <para>The value is the file extension minus the preceding
		///    ".". For example, "m4v".</para>
		/// </remarks>
		public string Extension {
			get {return extension;}
		}
		
		/// <summary>
		///    Gets all the mime-types that have been registered with
		///    <see cref="SupportedMimeType" />.
		/// </summary>
		/// <value>
		///    A <see cref="T:System.Collections.Generic.IEnumerable`1" /> object containing all the
		///    mime-types that have been registered with <see
		///    cref="SupportedMimeType" />.
		/// </value>
		/// <remarks>
		///    <para>These values are used by <see
		///    cref="TagLib.File.Create(string,string,ReadStyle)" /> to
		///    match file types.</para>
		/// </remarks>
		public static IEnumerable<string> AllMimeTypes {
			get {
				foreach(SupportedMimeType type in mimetypes)
					yield return type.MimeType;
			}
		}
		
		/// <summary>
		///    Gets all the extensions that have been registered with
		///    <see cref="SupportedMimeType" />.
		/// </summary>
		/// <value>
		///    A <see cref="T:System.Collections.Generic.IEnumerable`1" /> object containing all the
		///    extensions that have been registered with <see
		///    cref="SupportedMimeType" />.
		/// </value>
		/// <remarks>
		///    <para>These values are currently not used in file type
		///    recognition.</para>
		/// </remarks>
		public static IEnumerable<string> AllExtensions {
			get {
				foreach(SupportedMimeType type in mimetypes)
					if(type.Extension != null)
						yield return type.Extension;
			}
		}
	}
}
