//
// UnsupportedFormatException.cs:
//
// Author:
//   Aaron Bockover (abockover@novell.com)
//
// Original Source:
//   Entagged#
//
// Copyright (C) 2005-2006 Novell, Inc.
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
using System.Runtime.Serialization;

namespace TagLib {
	/// <summary>
	///    This class extends <see cref="Exception" /> and is used to
	///    indicate that a file or tag is stored in an unsupported format
	///    and cannot be read or written by the current implementation.
	/// </summary>
	/// <example>
	///    <para>Catching an exception when creating a <see
	///    cref="File" />.</para>
	///    <code lang="C#">
	/// using System;
	/// using TagLib;
	///
	/// public class ExceptionTest
	/// {
	/// 	public static void Main ()
	/// 	{
	/// 		try {
	/// 			File file = File.Create ("myfile.flv"); // Not supported, YET!
	/// 		} catch (UnsupportedFormatException e) {
	/// 			Console.WriteLine ("That file format is not supported: {0}", e.ToString ());
	/// 		}
	///	}
	/// }
	///    </code>
	///    <code lang="C++">
	/// #using &lt;System.dll>
	/// #using &lt;taglib-sharp.dll>
	/// 
	/// using System;
	/// using TagLib;
	///
	/// void main ()
	/// {
	/// 	try {
	/// 		File file = File::Create ("myfile.flv"); // Not supported, YET!
	/// 	} catch (UnsupportedFormatException^ e) {
	/// 		Console::WriteLine ("That file format is not supported: {0}", e);
	/// 	}
	/// }
	///    </code>
	///    <code lang="VB">
	/// Imports System
	/// Imports TagLib
	///
	/// Public Class ExceptionTest
	/// 	Public Shared Sub Main ()
	/// 		Try
	/// 			file As File = File.Create ("myfile.flv") ' Not supported, YET!
	/// 		Catch e As UnsupportedFormatException
	/// 			Console.WriteLine ("That file format is not supported: {0}", e.ToString ());
	/// 		End Try
	///	End Sub
	/// End Class
	///    </code>
	///    <code lang="Boo">
	/// import System
	/// import TagLib
	///
	/// try:
	/// 	file As File = File.Create ("myfile.flv") # Not supported, YET!
	/// catch e as UnsupportedFormatException:
	/// 	Console.WriteLine ("That file format is not supported: {0}", e.ToString ());
	///    </code>
	/// </example>
	[Serializable]
	public class UnsupportedFormatException : Exception
	{
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="UnsupportedFormatException" /> with a specified
		///    message.
		/// </summary>
		/// <param name="message">
		///    A <see cref="string" /> containing a message explaining
		///    the reason for the exception.
		/// </param>
		public UnsupportedFormatException (string message)
			: base(message)
		{
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="UnsupportedFormatException" /> with the default
		///    values.
		/// </summary>
		public UnsupportedFormatException () : base()
		{
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="UnsupportedFormatException" /> with a specified
		///    message containing a specified exception.
		/// </summary>
		/// <param name="message">
		///    A <see cref="string" /> containing a message explaining
		///    the reason for the exception.
		/// </param>
		/// <param name="innerException">
		///    A <see cref="Exception" /> object to be contained in the
		///    new exception. For example, previously caught exception.
		/// </param>
		public UnsupportedFormatException (string message,
		                                   Exception innerException)
			: base (message, innerException)
		{
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="UnsupportedFormatException" /> from a specified
		///    serialization info and streaming context.
		/// </summary>
		/// <param name="info">
		///    A <see cref="SerializationInfo" /> object containing the
		///    serialized data to be used for the new instance.
		/// </param>
		/// <param name="context">
		///    A <see cref="StreamingContext" /> object containing the
		///    streaming context information for the new instance.
		/// </param>
		/// <remarks>
		///    This constructor is implemented because <see
		///    cref="UnsupportedFormatException" /> implements the <see
		///    cref="ISerializable" /> interface.
		/// </remarks>
		protected UnsupportedFormatException (SerializationInfo info,
		                                      StreamingContext context)
			: base(info, context)
		{
		}
	}
}