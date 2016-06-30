//
// ASN1.cs: Abstract Syntax Notation 1 - micro-parser and generator
//
// Authors:
//	Sebastien Pouliot  <sebastien@ximian.com>
//	Jesper Pedersen  <jep@itplus.dk>
//
// (C) 2002, 2003 Motus Technologies Inc. (http://www.motus.com)
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
// (C) 2004 IT+ A/S (http://www.itplus.dk)
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

using System;
using System.Collections;
using System.IO;
using System.Text;

namespace MediaBrowser.Server.Mono.Security {

	// References:
	// a.	ITU ASN.1 standards (free download)
	//	http://www.itu.int/ITU-T/studygroups/com17/languages/

    public class ASN1 {

		private byte m_nTag;
		private byte[] m_aValue;
		private ArrayList elist;

		public ASN1 () : this (0x00, null) {}

		public ASN1 (byte tag) : this (tag, null) {}

		public ASN1 (byte tag, byte[] data) 
		{
			m_nTag = tag;
			m_aValue = data;
		}

		public ASN1 (byte[] data) 
		{
			m_nTag = data [0];

			int nLenLength = 0;
			int nLength = data [1];

			if (nLength > 0x80) {
				// composed length
				nLenLength = nLength - 0x80;
				nLength = 0;
				for (int i = 0; i < nLenLength; i++) {
					nLength *= 256;
					nLength += data [i + 2];
				}
			}
			else if (nLength == 0x80) {
				// undefined length encoding
				throw new NotSupportedException ("Undefined length encoding.");
			}

			m_aValue = new byte [nLength];
			Buffer.BlockCopy (data, (2 + nLenLength), m_aValue, 0, nLength);

			if ((m_nTag & 0x20) == 0x20) {
				int nStart = (2 + nLenLength);
				Decode (data, ref nStart, data.Length);
			}
		}

		public int Count {
			get { 
				if (elist == null)
					return 0;
				return elist.Count; 
			}
		}

		public byte Tag {
			get { return m_nTag; }
		}

		public int Length {
			get { 
				if (m_aValue != null)
					return m_aValue.Length; 
				else
					return 0;
			}
		}

		public byte[] Value {
			get { 
				if (m_aValue == null)
					GetBytes ();
				return (byte[]) m_aValue.Clone (); 
			}
			set { 
				if (value != null)
					m_aValue = (byte[]) value.Clone (); 
			}
		}

		private bool CompareArray (byte[] array1, byte[] array2)
		{
			bool bResult = (array1.Length == array2.Length);
			if (bResult) {
				for (int i = 0; i < array1.Length; i++) {
					if (array1[i] != array2[i])
						return false;
				}
			}
			return bResult;
		}

		public bool Equals (byte[] asn1) 
		{
			return CompareArray (this.GetBytes (), asn1);
		}

		public bool CompareValue (byte[] value) 
		{
			return CompareArray (m_aValue, value);
		}

		public ASN1 Add (ASN1 asn1) 
		{
			if (asn1 != null) {
				if (elist == null)
					elist = new ArrayList ();
				elist.Add (asn1);
			}
			return asn1;
		}

		public virtual byte[] GetBytes () 
		{
			byte[] val = null;
			
			if (Count > 0) {
				int esize = 0;
				ArrayList al = new ArrayList ();
				foreach (ASN1 a in elist) {
					byte[] item = a.GetBytes ();
					al.Add (item);
					esize += item.Length;
				}
				val = new byte [esize];
				int pos = 0;
				for (int i=0; i < elist.Count; i++) {
					byte[] item = (byte[]) al[i];
					Buffer.BlockCopy (item, 0, val, pos, item.Length);
					pos += item.Length;
				}
			} else if (m_aValue != null) {
				val = m_aValue;
			}

			byte[] der;
			int nLengthLen = 0;

			if (val != null) {
				int nLength = val.Length;
				// special for length > 127
				if (nLength > 127) {
					if (nLength <= Byte.MaxValue) {
						der = new byte [3 + nLength];
						Buffer.BlockCopy (val, 0, der, 3, nLength);
						nLengthLen = 0x81;
						der[2] = (byte)(nLength);
					}
					else if (nLength <= UInt16.MaxValue) {
						der = new byte [4 + nLength];
						Buffer.BlockCopy (val, 0, der, 4, nLength);
						nLengthLen = 0x82;
						der[2] = (byte)(nLength >> 8);
						der[3] = (byte)(nLength);
					}
					else if (nLength <= 0xFFFFFF) {
						// 24 bits
						der = new byte [5 + nLength];
						Buffer.BlockCopy (val, 0, der, 5, nLength);
						nLengthLen = 0x83;
						der [2] = (byte)(nLength >> 16);
						der [3] = (byte)(nLength >> 8);
						der [4] = (byte)(nLength);
					}
					else {
						// max (Length is an integer) 32 bits
						der = new byte [6 + nLength];
						Buffer.BlockCopy (val, 0, der, 6, nLength);
						nLengthLen = 0x84;
						der [2] = (byte)(nLength >> 24);
						der [3] = (byte)(nLength >> 16);
						der [4] = (byte)(nLength >> 8);
						der [5] = (byte)(nLength);
					}
				}
				else {
					// basic case (no encoding)
					der = new byte [2 + nLength];
					Buffer.BlockCopy (val, 0, der, 2, nLength);
					nLengthLen = nLength;
				}
				if (m_aValue == null)
					m_aValue = val;
			}
			else
				der = new byte[2];

			der[0] = m_nTag;
			der[1] = (byte)nLengthLen;

			return der;
		}

		// Note: Recursive
		protected void Decode (byte[] asn1, ref int anPos, int anLength) 
		{
			byte nTag;
			int nLength;
			byte[] aValue;

			// minimum is 2 bytes (tag + length of 0)
			while (anPos < anLength - 1) {
				DecodeTLV (asn1, ref anPos, out nTag, out nLength, out aValue);
				// sometimes we get trailing 0
				if (nTag == 0)
					continue;

				ASN1 elm = Add (new ASN1 (nTag, aValue));

				if ((nTag & 0x20) == 0x20) {
					int nConstructedPos = anPos;
					elm.Decode (asn1, ref nConstructedPos, nConstructedPos + nLength);
				}
				anPos += nLength; // value length
			}
		}

		// TLV : Tag - Length - Value
		protected void DecodeTLV (byte[] asn1, ref int pos, out byte tag, out int length, out byte[] content) 
		{
			tag = asn1 [pos++];
			length = asn1 [pos++];

			// special case where L contains the Length of the Length + 0x80
			if ((length & 0x80) == 0x80) {
				int nLengthLen = length & 0x7F;
				length = 0;
				for (int i = 0; i < nLengthLen; i++)
					length = length * 256 + asn1 [pos++];
			}

			content = new byte [length];
			Buffer.BlockCopy (asn1, pos, content, 0, length);
		}

		public ASN1 this [int index] {
			get { 		
				try {
					if ((elist == null) || (index >= elist.Count))
						return null;
					return (ASN1) elist [index];
				}
				catch (ArgumentOutOfRangeException) {
					return null;
				}
			}
		}

		public ASN1 Element (int index, byte anTag) 
		{
			try {
				if ((elist == null) || (index >= elist.Count))
					return null;

				ASN1 elm = (ASN1) elist [index];
				if (elm.Tag == anTag)
					return elm;
				else
					return null;
			}
			catch (ArgumentOutOfRangeException) {
				return null;
			}
		}

		public override string ToString()
		{
			StringBuilder hexLine = new StringBuilder ();
            
			// Add tag
			hexLine.AppendFormat ("Tag: {0} {1}", m_nTag.ToString ("X2"), Environment.NewLine);

			// Add length
			hexLine.AppendFormat ("Length: {0} {1}", Value.Length, Environment.NewLine);

			// Add value
			hexLine.Append ("Value: ");
			hexLine.Append (Environment.NewLine);
			for (int i = 0; i < Value.Length; i++) {
				hexLine.AppendFormat ("{0} ", Value [i].ToString ("X2"));
				if ((i+1) % 16 == 0)
					hexLine.AppendFormat (Environment.NewLine);
			}
			return hexLine.ToString ();
		}

		public void SaveToFile (string filename)
		{
			if (filename == null)
				throw new ArgumentNullException ("filename");

			using (FileStream fs = File.Create (filename)) {
				byte[] data = GetBytes ();
				fs.Write (data, 0, data.Length);
			}
		}
	}
}
