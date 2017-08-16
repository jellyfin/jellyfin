// This code is derived from jcifs smb client library <jcifs at samba dot org>
// Ported by J. Arturo <webmaster at komodosoft dot net>
//  
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
using System;
using SharpCifs.Util.Sharpen;

namespace SharpCifs.Util
{
	/// <summary>Implements the MD4 message digest algorithm in Java.</summary>
	/// <remarks>
	/// Implements the MD4 message digest algorithm in Java.
	/// <p>
	/// <b>References:</b>
	/// <ol>
	/// <li> Ronald L. Rivest,
	/// "<a href="http://www.roxen.com/rfc/rfc1320.html">
	/// The MD4 Message-Digest Algorithm</a>",
	/// IETF RFC-1320 (informational).
	/// </ol>
	/// <p><b>$Revision: 1.2 $</b>
	/// </remarks>
	/// <author>Raif S. Naffah</author>
	public class Md4 : MessageDigest
	{
		/// <summary>The size in bytes of the input block to the tranformation algorithm.</summary>
		/// <remarks>The size in bytes of the input block to the tranformation algorithm.</remarks>
		private const int BlockLength = 64;

		/// <summary>4 32-bit words (interim result)</summary>
		private int[] _context = new int[4];

		/// <summary>Number of bytes processed so far mod.</summary>
		/// <remarks>Number of bytes processed so far mod. 2 power of 64.</remarks>
		private long _count;

		/// <summary>512 bits input buffer = 16 x 32-bit words holds until reaches 512 bits.</summary>
		/// <remarks>512 bits input buffer = 16 x 32-bit words holds until reaches 512 bits.</remarks>
		private byte[] _buffer = new byte[BlockLength];

		/// <summary>512 bits work buffer = 16 x 32-bit words</summary>
		private int[] _x = new int[16];

		public Md4() 
		{
			// This file is currently unlocked (change this line if you lock the file)
			//
			// $Log: MD4.java,v $
			// Revision 1.2  1998/01/05 03:41:19  iang
			// Added references only.
			//
			// Revision 1.1.1.1  1997/11/03 22:36:56  hopwood
			// + Imported to CVS (tagged as 'start').
			//
			// Revision 0.1.0.0  1997/07/14  R. Naffah
			// + original version
			//
			// $Endlog$
			// MD4 specific object variables
			//...........................................................................
			//    = 512 / 8;
			// Constructors
			//...........................................................................
			EngineReset();
		}

		/// <summary>This constructor is here to implement cloneability of this class.</summary>
		/// <remarks>This constructor is here to implement cloneability of this class.</remarks>
		private Md4(Md4 md) : this()
		{
			_context = (int[])md._context.Clone();
			_buffer = (byte[])md._buffer.Clone();
			_count = md._count;
		}

		// Cloneable method implementation
		//...........................................................................
		/// <summary>Returns a copy of this MD object.</summary>
		/// <remarks>Returns a copy of this MD object.</remarks>
		public object Clone()
		{
			return new Md4(this);
		}

		// JCE methods
		//...........................................................................
		/// <summary>
		/// Resets this object disregarding any temporary data present at the
		/// time of the invocation of this call.
		/// </summary>
		/// <remarks>
		/// Resets this object disregarding any temporary data present at the
		/// time of the invocation of this call.
		/// </remarks>
		protected void EngineReset()
		{
			// initial values of MD4 i.e. A, B, C, D
			// as per rfc-1320; they are low-order byte first
			_context[0] = unchecked(0x67452301);
			_context[1] = unchecked((int)(0xEFCDAB89));
			_context[2] = unchecked((int)(0x98BADCFE));
			_context[3] = unchecked(0x10325476);
			_count = 0L;
			for (int i = 0; i < BlockLength; i++)
			{
				_buffer[i] = 0;
			}
		}

		/// <summary>Continues an MD4 message digest using the input byte.</summary>
		/// <remarks>Continues an MD4 message digest using the input byte.</remarks>
		protected void EngineUpdate(byte b)
		{
			// compute number of bytes still unhashed; ie. present in buffer
			int i = (int)(_count % BlockLength);
			_count++;
			// update number of bytes
			_buffer[i] = b;
			if (i == BlockLength - 1)
			{
				Transform(_buffer, 0);
			}
		}

		/// <summary>MD4 block update operation.</summary>
		/// <remarks>
		/// MD4 block update operation.
		/// <p>
		/// Continues an MD4 message digest operation, by filling the buffer,
		/// transform(ing) data in 512-bit message block(s), updating the variables
		/// context and count, and leaving (buffering) the remaining bytes in buffer
		/// for the next update or finish.
		/// </remarks>
		/// <param name="input">input block</param>
		/// <param name="offset">start of meaningful bytes in input</param>
		/// <param name="len">count of bytes in input block to consider</param>
		protected void EngineUpdate(byte[] input, int offset, int len)
		{
			// make sure we don't exceed input's allocated size/length
			if (offset < 0 || len < 0 || (long)offset + len > input.Length)
			{
				throw new IndexOutOfRangeException();
			}
			// compute number of bytes still unhashed; ie. present in buffer
			int bufferNdx = (int)(_count % BlockLength);
			_count += len;
			// update number of bytes
			int partLen = BlockLength - bufferNdx;
			int i = 0;
			if (len >= partLen)
			{
				Array.Copy(input, offset, _buffer, bufferNdx, partLen);
				Transform(_buffer, 0);
				for (i = partLen; i + BlockLength - 1 < len; i += BlockLength)
				{
					Transform(input, offset + i);
				}
				bufferNdx = 0;
			}
			// buffer remaining input
			if (i < len)
			{
				Array.Copy(input, offset + i, _buffer, bufferNdx, len - i);
			}
		}

		/// <summary>
		/// Completes the hash computation by performing final operations such
		/// as padding.
		/// </summary>
		/// <remarks>
		/// Completes the hash computation by performing final operations such
		/// as padding. At the return of this engineDigest, the MD engine is
		/// reset.
		/// </remarks>
		/// <returns>the array of bytes for the resulting hash value.</returns>
		protected byte[] EngineDigest()
		{
			// pad output to 56 mod 64; as RFC1320 puts it: congruent to 448 mod 512
			int bufferNdx = (int)(_count % BlockLength);
			int padLen = (bufferNdx < 56) ? (56 - bufferNdx) : (120 - bufferNdx);
			// padding is alwas binary 1 followed by binary 0s
			byte[] tail = new byte[padLen + 8];
			tail[0] = unchecked(unchecked(0x80));
			// append length before final transform:
			// save number of bits, casting the long to an array of 8 bytes
			// save low-order byte first.
			for (int i = 0; i < 8; i++)
			{
				tail[padLen + i] = unchecked((byte)((long)(((ulong)(_count * 8)) >> (8 * i))));
			}
			EngineUpdate(tail, 0, tail.Length);
			byte[] result = new byte[16];
			// cast this MD4's context (array of 4 ints) into an array of 16 bytes.
			for (int i1 = 0; i1 < 4; i1++)
			{
				for (int j = 0; j < 4; j++)
				{
					result[i1 * 4 + j] = unchecked((byte)((int)(((uint)_context[i1]) >> (8 * j))));
				}
			}
			// reset the engine
			EngineReset();
			return result;
		}

		// own methods
		//...........................................................................
		/// <summary>MD4 basic transformation.</summary>
		/// <remarks>
		/// MD4 basic transformation.
		/// <p>
		/// Transforms context based on 512 bits from input block starting
		/// from the offset'th byte.
		/// </remarks>
		/// <param name="block">input sub-array.</param>
		/// <param name="offset">starting position of sub-array.</param>
		private void Transform(byte[] block, int offset)
		{
			// encodes 64 bytes from input block into an array of 16 32-bit
			// entities. Use A as a temp var.
			for (int i = 0; i < 16; i++)
			{
				_x[i] = (block[offset++] & unchecked(0xFF)) | (block[offset++] & unchecked(
					0xFF)) << 8 | (block[offset++] & unchecked(0xFF)) << 16 | (block[offset
					++] & unchecked(0xFF)) << 24;
			}
			int a = _context[0];
			int b = _context[1];
			int c = _context[2];
			int d = _context[3];
			a = Ff(a, b, c, d, _x[0], 3);
			d = Ff(d, a, b, c, _x[1], 7);
			c = Ff(c, d, a, b, _x[2], 11);
			b = Ff(b, c, d, a, _x[3], 19);
			a = Ff(a, b, c, d, _x[4], 3);
			d = Ff(d, a, b, c, _x[5], 7);
			c = Ff(c, d, a, b, _x[6], 11);
			b = Ff(b, c, d, a, _x[7], 19);
			a = Ff(a, b, c, d, _x[8], 3);
			d = Ff(d, a, b, c, _x[9], 7);
			c = Ff(c, d, a, b, _x[10], 11);
			b = Ff(b, c, d, a, _x[11], 19);
			a = Ff(a, b, c, d, _x[12], 3);
			d = Ff(d, a, b, c, _x[13], 7);
			c = Ff(c, d, a, b, _x[14], 11);
			b = Ff(b, c, d, a, _x[15], 19);
			a = Gg(a, b, c, d, _x[0], 3);
			d = Gg(d, a, b, c, _x[4], 5);
			c = Gg(c, d, a, b, _x[8], 9);
			b = Gg(b, c, d, a, _x[12], 13);
			a = Gg(a, b, c, d, _x[1], 3);
			d = Gg(d, a, b, c, _x[5], 5);
			c = Gg(c, d, a, b, _x[9], 9);
			b = Gg(b, c, d, a, _x[13], 13);
			a = Gg(a, b, c, d, _x[2], 3);
			d = Gg(d, a, b, c, _x[6], 5);
			c = Gg(c, d, a, b, _x[10], 9);
			b = Gg(b, c, d, a, _x[14], 13);
			a = Gg(a, b, c, d, _x[3], 3);
			d = Gg(d, a, b, c, _x[7], 5);
			c = Gg(c, d, a, b, _x[11], 9);
			b = Gg(b, c, d, a, _x[15], 13);
			a = Hh(a, b, c, d, _x[0], 3);
			d = Hh(d, a, b, c, _x[8], 9);
			c = Hh(c, d, a, b, _x[4], 11);
			b = Hh(b, c, d, a, _x[12], 15);
			a = Hh(a, b, c, d, _x[2], 3);
			d = Hh(d, a, b, c, _x[10], 9);
			c = Hh(c, d, a, b, _x[6], 11);
			b = Hh(b, c, d, a, _x[14], 15);
			a = Hh(a, b, c, d, _x[1], 3);
			d = Hh(d, a, b, c, _x[9], 9);
			c = Hh(c, d, a, b, _x[5], 11);
			b = Hh(b, c, d, a, _x[13], 15);
			a = Hh(a, b, c, d, _x[3], 3);
			d = Hh(d, a, b, c, _x[11], 9);
			c = Hh(c, d, a, b, _x[7], 11);
			b = Hh(b, c, d, a, _x[15], 15);
			_context[0] += a;
			_context[1] += b;
			_context[2] += c;
			_context[3] += d;
		}

		// The basic MD4 atomic functions.
		private int Ff(int a, int b, int c, int d, int x, int s)
		{
			int t = a + ((b & c) | (~b & d)) + x;
			return t << s | (int)(((uint)t) >> (32 - s));
		}

		private int Gg(int a, int b, int c, int d, int x, int s)
		{
			int t = a + ((b & (c | d)) | (c & d)) + x + unchecked(0x5A827999);
			return t << s | (int)(((uint)t) >> (32 - s));
		}

		private int Hh(int a, int b, int c, int d, int x, int s)
		{
			int t = a + (b ^ c ^ d) + x + unchecked(0x6ED9EBA1);
			return t << s | (int)(((uint)t) >> (32 - s));
		}

	    public override byte[] Digest()
	    {
	        return EngineDigest();
	    }

	    public override int GetDigestLength()
	    {
	        return EngineDigest().Length;
	    }

	    public override void Reset()
	    {
	        EngineReset();
	    }

	    public override void Update(byte[] b)
	    {	        
            EngineUpdate(b, 0, b.Length);
	    }

	    public override void Update(byte b)
	    {
	        EngineUpdate(b);
	    }

	    public override void Update(byte[] b, int offset, int len)
	    {
	        EngineUpdate(b, offset, len);
	    }
	}
}
