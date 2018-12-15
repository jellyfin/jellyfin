//
// ReadOnlyByteVector.cs: This class extends ByteVector" to provide an
// immutable version.
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

namespace TagLib {
	/// <summary>
	///    This class extends <see cref="ByteVector" /> to provide an
	///    immutable version.
	/// </summary>
	public sealed class ReadOnlyByteVector : ByteVector
	{
		#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="ReadOnlyByteVector" /> with no contents.
		/// </summary>
		public ReadOnlyByteVector () : base ()
		{
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="ReadOnlyByteVector" /> of a specified length filled
		///    with bytes of a specified value.
		/// </summary>
		/// <param name="size">
		///    A <see cref="int" /> specifying the number of bytes to
		///    add to the new instance.
		/// </param>
		/// <param name="value">
		///    A <see cref="byte" /> specifying the value to use for the
		///    bytes added to the new instance.
		/// </param>
		public ReadOnlyByteVector (int size, byte value)
			: base (size, value)
		{
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="ReadOnlyByteVector" /> of a specified length filled
		///    with bytes with a value of zero.
		/// </summary>
		/// <param name="size">
		///    A <see cref="int" /> specifying the number of bytes to
		///    add to the new instance.
		/// </param>
		/// <remarks>
		///    <para>To specify the value to fill the new instance with,
		///    use <see cref="ReadOnlyByteVector(int,byte)" />.</para>
		/// </remarks>
		public ReadOnlyByteVector (int size) : this (size, 0)
		{
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="ReadOnlyByteVector" /> by copying the contents from
		///    another instance.
		/// </summary>
		/// <param name="vector">
		///    A <see cref="ByteVector" /> object to copy the values
		///    from.
		/// </param>
		public ReadOnlyByteVector (ByteVector vector) : base (vector)
		{
		}

		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="ReadOnlyByteVector" /> by copying a specified
		///    number of bytes from an array.
		/// </summary>
		/// <param name="data">
		///    A <see cref="T:byte[]" /> to copy values from.
		/// </param>
		/// <param name="length">
		///    A <see cref="int" /> specifying the number of bytes to
		///    copy.
		/// </param>
		/// <remarks>
		///    <para>If copying the entire contents of an array, use
		///    <see cref="ReadOnlyByteVector(byte[])" />.</para>
		/// </remarks>
		public ReadOnlyByteVector (byte [] data, int length)
			: base (data, length)
		{
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="ReadOnlyByteVector" /> by copying the contents of a
		///    specified array.
		/// </summary>
		/// <param name="data">
		///    A <see cref="T:byte[]" /> to copy values from.
		/// </param>
		/// <remarks>
		///    <para>To copy only part of the array, use <see
		///    cref="ReadOnlyByteVector(byte[],int)" />.</para>
		/// </remarks>
		public ReadOnlyByteVector (params byte [] data) : base (data)
		{
		}
		
		#endregion
		
		
		
		#region Operators
		
		/// <summary>
		///    Implicitly converts a <see cref="byte" /> to a new
		///    <see cref="ReadOnlyByteVector" />.
		/// </summary>
		/// <param name="value">
		///    A <see cref="byte" /> object to convert.
		/// </param>
		/// <returns>
		///    A <see cref="ReadOnlyByteVector" /> equivalent to
		///    <paramref name="value" />.
		/// </returns>
		public static implicit operator ReadOnlyByteVector (byte value)
		{
			return new ReadOnlyByteVector (value);
		}
		
		/// <summary>
		///    Implicitly converts a <see cref="T:byte[]" /> to a new
		///    <see cref="ReadOnlyByteVector" />.
		/// </summary>
		/// <param name="value">
		///    A <see cref="T:byte[]" /> object to convert.
		/// </param>
		/// <returns>
		///    A <see cref="ReadOnlyByteVector" /> equivalent to
		///    <paramref name="value" />.
		/// </returns>
		public static implicit operator ReadOnlyByteVector (byte [] value)
		{
			return new ReadOnlyByteVector (value);
		}
		
		/// <summary>
		///    Implicitly converts a <see cref="string" /> object to a
		///    new <see cref="ReadOnlyByteVector" /> using the UTF-8
		///    encoding.
		/// </summary>
		/// <param name="value">
		///    A <see cref="string" /> object to convert.
		/// </param>
		/// <returns>
		///    A <see cref="ReadOnlyByteVector" /> equivalent to
		///    <paramref name="value" />.
		/// </returns>
		public static implicit operator ReadOnlyByteVector (string value)
		{
			return new ReadOnlyByteVector (ByteVector.FromString (
				value, StringType.UTF8));
		}
		
		#endregion
		
		
		
		#region IList<T>
		
		/// <summary>
		///    Gets whether or not the current instance is read-only.
		/// </summary>
		/// <value>
		///    Always <see langword="true" />.
		/// </value>
		public override bool IsReadOnly {
			get {return true;}
		}
		
		/// <summary>
		///    Gets whether or not the current instance is fixed size.
		/// </summary>
		/// <value>
		///    Always <see langword="true" />.
		/// </value>
		public override bool IsFixedSize {
			get {return true;}
		}
		
		#endregion
	}
}