//
// X509Extension.cs: Base class for all X.509 extensions.
//
// Author:
//	Sebastien Pouliot  <sebastien@ximian.com>
//
// (C) 2003 Motus Technologies Inc. (http://www.motus.com)
// Copyright (C) 2004-2005 Novell, Inc (http://www.novell.com)
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
using System.Globalization;
using System.Text;

namespace MediaBrowser.Server.Mono.Security {
	/*
	 * Extension  ::=  SEQUENCE  {
	 *	extnID      OBJECT IDENTIFIER,
	 *	critical    BOOLEAN DEFAULT FALSE,
	 *	extnValue   OCTET STRING  
	 * }
	 */
    public class X509Extension {

		protected string extnOid;
		protected bool extnCritical;
		protected ASN1 extnValue;

		protected X509Extension () 
		{
			extnCritical = false;
		}

		public X509Extension (ASN1 asn1) 
		{
			if ((asn1.Tag != 0x30) || (asn1.Count < 2))
				throw new ArgumentException (("Invalid X.509 extension."));
			if (asn1[0].Tag != 0x06)
				throw new ArgumentException (("Invalid X.509 extension."));

			extnOid = ASN1Convert.ToOid (asn1[0]);
			extnCritical = ((asn1[1].Tag == 0x01) && (asn1[1].Value[0] == 0xFF));
			// last element is an octet string which may need to be decoded
			extnValue = asn1 [asn1.Count - 1];
			if ((extnValue.Tag == 0x04) && (extnValue.Length > 0) && (extnValue.Count == 0)) {
				try {
					ASN1 encapsulated = new ASN1 (extnValue.Value);
					extnValue.Value = null;
					extnValue.Add (encapsulated);
				}
				catch {
					// data isn't ASN.1
				}
			}
			Decode ();
		}

		public X509Extension (X509Extension extension)
		{
			if (extension == null)
				throw new ArgumentNullException ("extension");
			if ((extension.Value == null) || (extension.Value.Tag != 0x04) || (extension.Value.Count != 1))
				throw new ArgumentException (("Invalid X.509 extension."));

			extnOid = extension.Oid;
			extnCritical = extension.Critical;
			extnValue = extension.Value;
			Decode ();
		}

		// encode the extension *into* an OCTET STRING
		protected virtual void Decode () 
		{
		}

		// decode the extension from *inside* an OCTET STRING
		protected virtual void Encode ()
		{
		}

		public ASN1 ASN1 {
			get {
				ASN1 extension = new ASN1 (0x30);
				extension.Add (ASN1Convert.FromOid (extnOid));
				if (extnCritical)
					extension.Add (new ASN1 (0x01, new byte [1] { 0xFF }));
				Encode ();
				extension.Add (extnValue);
				return extension;
			}
		}

		public string Oid {
			get { return extnOid; }
		}

		public bool Critical {
			get { return extnCritical; }
			set { extnCritical = value; }
		}

		// this gets overrided with more meaningful names
		public virtual string Name {
			get { return extnOid; }
		}

		public ASN1 Value {
			get {
				if (extnValue == null) {
					Encode ();
				}
				return extnValue;
			}
		}

		public override bool Equals (object obj) 
		{
			if (obj == null)
				return false;
			
			X509Extension ex = (obj as X509Extension);
			if (ex == null)
				return false;

			if (extnCritical != ex.extnCritical)
				return false;
			if (extnOid != ex.extnOid)
				return false;
			if (extnValue.Length != ex.extnValue.Length)
				return false;
			
                        for (int i=0; i < extnValue.Length; i++) {
				if (extnValue [i] != ex.extnValue [i])
					return false;
			}
			return true;
		}

		public byte[] GetBytes () 
		{
			return ASN1.GetBytes ();
		}

		public override int GetHashCode () 
		{
			// OID should be unique in a collection of extensions
			return extnOid.GetHashCode ();
		}

		private void WriteLine (StringBuilder sb, int n, int pos) 
		{
			byte[] value = extnValue.Value;
			int p = pos;
			for (int j=0; j < 8; j++) {
				if (j < n) {
					sb.Append (value [p++].ToString ("X2", CultureInfo.InvariantCulture));
					sb.Append (" ");
				}
				else
					sb.Append ("   ");
			}
			sb.Append ("  ");
			p = pos;
			for (int j=0; j < n; j++) {
				byte b = value [p++];
				if (b < 0x20)
					sb.Append (".");
				else
					sb.Append (Convert.ToChar (b));
			}
			sb.Append (Environment.NewLine);
		}

		public override string ToString () 
		{
			StringBuilder sb = new StringBuilder ();
			int div = (extnValue.Length >> 3);
			int rem = (extnValue.Length - (div << 3));
			int x = 0;
			for (int i=0; i < div; i++) {
				WriteLine (sb, 8, x);
				x += 8;
			}
			WriteLine (sb, rem, x);
			return sb.ToString ();
		}
	}
}
