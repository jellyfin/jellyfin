//
// X509Extensions.cs: Handles X.509 extensions.
//
// Author:
//	Sebastien Pouliot  <sebastien@ximian.com>
//
// (C) 2003 Motus Technologies Inc. (http://www.motus.com)
// (C) 2004 Novell (http://www.novell.com)
//

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

namespace MediaBrowser.Server.Mono.Security {
	/*
	 * Extensions  ::=  SEQUENCE SIZE (1..MAX) OF Extension
	 * 
	 * Note: 1..MAX -> There shouldn't be 0 Extensions in the ASN1 structure
	 */
    public sealed class X509ExtensionCollection : CollectionBase, IEnumerable {

		private bool readOnly;

		public X509ExtensionCollection () : base ()
		{
		}

		public X509ExtensionCollection (ASN1 asn1) : this ()
		{
			readOnly = true;
			if (asn1 == null)
				return;
			if (asn1.Tag != 0x30)
				throw new Exception ("Invalid extensions format");
			for (int i=0; i < asn1.Count; i++) {
				X509Extension extension = new X509Extension (asn1 [i]);
				InnerList.Add (extension);
			}
		}

		public int Add (X509Extension extension) 
		{
			if (extension == null)
				throw new ArgumentNullException ("extension");
			if (readOnly)
				throw new NotSupportedException ("Extensions are read only");
		
			return InnerList.Add (extension);
		}

		public void AddRange (X509Extension[] extension) 
		{
			if (extension == null)
				throw new ArgumentNullException ("extension");
			if (readOnly)
				throw new NotSupportedException ("Extensions are read only");

			for (int i = 0; i < extension.Length; i++) 
				InnerList.Add (extension [i]);
		}
	
		public void AddRange (X509ExtensionCollection collection) 
		{
			if (collection == null)
				throw new ArgumentNullException ("collection");
			if (readOnly)
				throw new NotSupportedException ("Extensions are read only");

			for (int i = 0; i < collection.InnerList.Count; i++) 
				InnerList.Add (collection [i]);
		}

		public bool Contains (X509Extension extension) 
		{
			return (IndexOf (extension) != -1);
		}

		public bool Contains (string oid) 
		{
			return (IndexOf (oid) != -1);
		}

		public void CopyTo (X509Extension[] extensions, int index) 
		{
			if (extensions == null)
				throw new ArgumentNullException ("extensions");

			InnerList.CopyTo (extensions, index);
		}

		public int IndexOf (X509Extension extension) 
		{
			if (extension == null)
				throw new ArgumentNullException ("extension");

			for (int i=0; i < InnerList.Count; i++) {
				X509Extension ex = (X509Extension) InnerList [i];
				if (ex.Equals (extension))
					return i;
			}
			return -1;
		}

		public int IndexOf (string oid) 
		{
			if (oid == null)
				throw new ArgumentNullException ("oid");

			for (int i=0; i < InnerList.Count; i++) {
				X509Extension ex = (X509Extension) InnerList [i];
				if (ex.Oid == oid)
					return i;
			}
			return -1;
		}

		public void Insert (int index, X509Extension extension) 
		{
			if (extension == null)
				throw new ArgumentNullException ("extension");

			InnerList.Insert (index, extension);
		}

		public void Remove (X509Extension extension) 
		{
			if (extension == null)
				throw new ArgumentNullException ("extension");

			InnerList.Remove (extension);
		}

		public void Remove (string oid) 
		{
			if (oid == null)
				throw new ArgumentNullException ("oid");

			int index = IndexOf (oid);
			if (index != -1)
				InnerList.RemoveAt (index);
		}

		IEnumerator IEnumerable.GetEnumerator () 
		{
			return InnerList.GetEnumerator ();
		}

		public X509Extension this [int index] {
			get { return (X509Extension) InnerList [index]; }
		}

		public X509Extension this [string oid] {
			get {
				int index = IndexOf (oid);
				if (index == -1)
					return null;
				return (X509Extension) InnerList [index];
			}
		}

		public byte[] GetBytes () 
		{
			if (InnerList.Count < 1)
				return null;
			ASN1 sequence = new ASN1 (0x30);
			for (int i=0; i < InnerList.Count; i++) {
				X509Extension x = (X509Extension) InnerList [i];
				sequence.Add (x.ASN1);
			}
			return sequence.GetBytes ();
		}
	}
}
