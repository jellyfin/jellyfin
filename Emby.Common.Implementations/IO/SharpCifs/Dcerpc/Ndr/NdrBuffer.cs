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
using SharpCifs.Util;
using SharpCifs.Util.Sharpen;

namespace SharpCifs.Dcerpc.Ndr
{
	public class NdrBuffer
	{
		internal int Referent;

		internal Hashtable Referents;

		internal class Entry
		{
			internal int Referent;

			internal object Obj;
		}

		public byte[] Buf;

		public int Start;

		public int Index;

		public int Length;

		public NdrBuffer Deferred;

		public NdrBuffer(byte[] buf, int start)
		{
			this.Buf = buf;
			this.Start = Index = start;
			Length = 0;
			Deferred = this;
		}

		public virtual NdrBuffer Derive(int idx)
		{
			NdrBuffer nb = new NdrBuffer(Buf, Start);
			nb.Index = idx;
			nb.Deferred = Deferred;
			return nb;
		}

		public virtual void Reset()
		{
			Index = Start;
			Length = 0;
			Deferred = this;
		}

		public virtual int GetIndex()
		{
			return Index;
		}

		public virtual void SetIndex(int index)
		{
			this.Index = index;
		}

		public virtual int GetCapacity()
		{
			return Buf.Length - Start;
		}

		public virtual int GetTailSpace()
		{
			return Buf.Length - Index;
		}

		public virtual byte[] GetBuffer()
		{
			return Buf;
		}

		public virtual int Align(int boundary, byte value)
		{
			int n = Align(boundary);
			int i = n;
			while (i > 0)
			{
				Buf[Index - i] = value;
				i--;
			}
			return n;
		}

		public virtual void WriteOctetArray(byte[] b, int i, int l)
		{
			Array.Copy(b, i, Buf, Index, l);
			Advance(l);
		}

		public virtual void ReadOctetArray(byte[] b, int i, int l)
		{
			Array.Copy(Buf, Index, b, i, l);
			Advance(l);
		}

		public virtual int GetLength()
		{
			return Deferred.Length;
		}

		public virtual void SetLength(int length)
		{
			Deferred.Length = length;
		}

		public virtual void Advance(int n)
		{
			Index += n;
			if ((Index - Start) > Deferred.Length)
			{
				Deferred.Length = Index - Start;
			}
		}

		public virtual int Align(int boundary)
		{
			int m = boundary - 1;
			int i = Index - Start;
			int n = ((i + m) & ~m) - i;
			Advance(n);
			return n;
		}

		public virtual void Enc_ndr_small(int s)
		{
			Buf[Index] = unchecked((byte)(s & unchecked(0xFF)));
			Advance(1);
		}

		public virtual int Dec_ndr_small()
		{
			int val = Buf[Index] & unchecked(0xFF);
			Advance(1);
			return val;
		}

		public virtual void Enc_ndr_short(int s)
		{
			Align(2);
			Encdec.Enc_uint16le((short)s, Buf, Index);
			Advance(2);
		}

		public virtual int Dec_ndr_short()
		{
			Align(2);
			int val = Encdec.Dec_uint16le(Buf, Index);
			Advance(2);
			return val;
		}

		public virtual void Enc_ndr_long(int l)
		{
			Align(4);
			Encdec.Enc_uint32le(l, Buf, Index);
			Advance(4);
		}

		public virtual int Dec_ndr_long()
		{
			Align(4);
			int val = Encdec.Dec_uint32le(Buf, Index);
			Advance(4);
			return val;
		}

		public virtual void Enc_ndr_hyper(long h)
		{
			Align(8);
			Encdec.Enc_uint64le(h, Buf, Index);
			Advance(8);
		}

		public virtual long Dec_ndr_hyper()
		{
			Align(8);
			long val = Encdec.Dec_uint64le(Buf, Index);
			Advance(8);
			return val;
		}

		public virtual void Enc_ndr_string(string s)
		{
			Align(4);
			int i = Index;
			int len = s.Length;
			Encdec.Enc_uint32le(len + 1, Buf, i);
			i += 4;
			Encdec.Enc_uint32le(0, Buf, i);
			i += 4;
			Encdec.Enc_uint32le(len + 1, Buf, i);
			i += 4;
			try
			{
				Array.Copy(Runtime.GetBytesForString(s, "UTF-16LE"), 0, Buf, i, len
					 * 2);
			}
			catch (UnsupportedEncodingException)
			{
			}
			i += len * 2;
			Buf[i++] = unchecked((byte)('\0'));
			Buf[i++] = unchecked((byte)('\0'));
			Advance(i - Index);
		}

		/// <exception cref="SharpCifs.Dcerpc.Ndr.NdrException"></exception>
		public virtual string Dec_ndr_string()
		{
			Align(4);
			int i = Index;
			string val = null;
			int len = Encdec.Dec_uint32le(Buf, i);
			i += 12;
			if (len != 0)
			{
				len--;
				int size = len * 2;
				try
				{
					if (size < 0 || size > unchecked(0xFFFF))
					{
						throw new NdrException(NdrException.InvalidConformance);
					}
					val = Runtime.GetStringForBytes(Buf, i, size, "UTF-16LE");
					i += size + 2;
				}
				catch (UnsupportedEncodingException)
				{
				}
			}
			Advance(i - Index);
			return val;
		}

		private int GetDceReferent(object obj)
		{
			Entry e;
			if (Referents == null)
			{
				Referents = new Hashtable();
				Referent = 1;
			}
			if ((e = (Entry)Referents.Get(obj)) == null)
			{
				e = new Entry();
				e.Referent = Referent++;
				e.Obj = obj;
				Referents.Put(obj, e);
			}
			return e.Referent;
		}

		public virtual void Enc_ndr_referent(object obj, int type)
		{
			if (obj == null)
			{
				Enc_ndr_long(0);
				return;
			}
			switch (type)
			{
				case 1:
				case 3:
				{
					Enc_ndr_long(Runtime.IdentityHashCode(obj));
					return;
				}

				case 2:
				{
					Enc_ndr_long(GetDceReferent(obj));
					return;
				}
			}
		}

		public override string ToString()
		{
			return "start=" + Start + ",index=" + Index + ",length=" + GetLength();
		}
	}
}
