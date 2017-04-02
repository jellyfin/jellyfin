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

namespace SharpCifs.Ntlmssp
{
	/// <summary>Abstract superclass for all NTLMSSP messages.</summary>
	/// <remarks>Abstract superclass for all NTLMSSP messages.</remarks>
	public abstract class NtlmMessage : NtlmFlags
	{
		/// <summary>The NTLMSSP "preamble".</summary>
		/// <remarks>The NTLMSSP "preamble".</remarks>
		protected internal static readonly byte[] NtlmsspSignature = { unchecked(
			(byte)('N')), unchecked((byte)('T')), unchecked((byte)('L')), 
			unchecked((byte)('M')), unchecked((byte)('S')), unchecked((byte
			    )('S')), unchecked((byte)('P')), unchecked(0) };

		private static readonly string OemEncoding = Config.DefaultOemEncoding;

		protected internal static readonly string UniEncoding = "UTF-16LE";

		private int _flags;

		/// <summary>Returns the flags currently in use for this message.</summary>
		/// <remarks>Returns the flags currently in use for this message.</remarks>
		/// <returns>
		/// An <code>int</code> containing the flags in use for this
		/// message.
		/// </returns>
		public virtual int GetFlags()
		{
			return _flags;
		}

		/// <summary>Sets the flags for this message.</summary>
		/// <remarks>Sets the flags for this message.</remarks>
		/// <param name="flags">The flags for this message.</param>
		public virtual void SetFlags(int flags)
		{
			this._flags = flags;
		}

		/// <summary>Returns the status of the specified flag.</summary>
		/// <remarks>Returns the status of the specified flag.</remarks>
		/// <param name="flag">The flag to test (i.e., <code>NTLMSSP_NEGOTIATE_OEM</code>).</param>
		/// <returns>A <code>boolean</code> indicating whether the flag is set.</returns>
		public virtual bool GetFlag(int flag)
		{
			return (GetFlags() & flag) != 0;
		}

		/// <summary>Sets or clears the specified flag.</summary>
		/// <remarks>Sets or clears the specified flag.</remarks>
		/// <param name="flag">
		/// The flag to set/clear (i.e.,
		/// <code>NTLMSSP_NEGOTIATE_OEM</code>).
		/// </param>
		/// <param name="value">
		/// Indicates whether to set (<code>true</code>) or
		/// clear (<code>false</code>) the specified flag.
		/// </param>
		public virtual void SetFlag(int flag, bool value)
		{
			SetFlags(value ? (GetFlags() | flag) : (GetFlags() & (unchecked((int)(0xffffffff)
				) ^ flag)));
		}

		internal static int ReadULong(byte[] src, int index)
		{
			return (src[index] & unchecked(0xff)) | ((src[index + 1] & unchecked(0xff)) << 8) | ((src[index + 2] & unchecked(0xff)) << 16) | ((src[index
				 + 3] & unchecked(0xff)) << 24);
		}

		internal static int ReadUShort(byte[] src, int index)
		{
			return (src[index] & unchecked(0xff)) | ((src[index + 1] & unchecked(0xff)) << 8);
		}

		internal static byte[] ReadSecurityBuffer(byte[] src, int index)
		{
			int length = ReadUShort(src, index);
			int offset = ReadULong(src, index + 4);
			byte[] buffer = new byte[length];
			Array.Copy(src, offset, buffer, 0, length);
			return buffer;
		}

		internal static void WriteULong(byte[] dest, int offset, int value)
		{
			dest[offset] = unchecked((byte)(value & unchecked(0xff)));
			dest[offset + 1] = unchecked((byte)(value >> 8 & unchecked(0xff)));
			dest[offset + 2] = unchecked((byte)(value >> 16 & unchecked(0xff)));
			dest[offset + 3] = unchecked((byte)(value >> 24 & unchecked(0xff)));
		}

		internal static void WriteUShort(byte[] dest, int offset, int value)
		{
			dest[offset] = unchecked((byte)(value & unchecked(0xff)));
            dest[offset + 1] = unchecked((byte)(value >> 8 & unchecked(0xff)));
		}

		internal static void WriteSecurityBuffer(byte[] dest, int offset, int bodyOffset, 
			byte[] src)
		{
			int length = (src != null) ? src.Length : 0;
			if (length == 0)
			{
				return;
			}
			WriteUShort(dest, offset, length);
			WriteUShort(dest, offset + 2, length);
			WriteULong(dest, offset + 4, bodyOffset);
			Array.Copy(src, 0, dest, bodyOffset, length);
		}

		internal static string GetOemEncoding()
		{
			return OemEncoding;
		}

		/// <summary>Returns the raw byte representation of this message.</summary>
		/// <remarks>Returns the raw byte representation of this message.</remarks>
		/// <returns>A <code>byte[]</code> containing the raw message material.</returns>
		public abstract byte[] ToByteArray();
	}
}
