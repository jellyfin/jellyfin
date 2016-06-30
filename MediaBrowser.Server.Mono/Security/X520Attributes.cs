//
// X520.cs: X.520 related stuff (attributes, RDN)
//
// Author:
//	Sebastien Pouliot <sebastien@ximian.com>
//
// (C) 2002, 2003 Motus Technologies Inc. (http://www.motus.com)
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
using System.Text;

namespace MediaBrowser.Server.Mono.Security {

	// References:
	// 1.	Information technology - Open Systems Interconnection - The Directory: Selected attribute types 
	//	http://www.itu.int/rec/recommendation.asp?type=folders&lang=e&parent=T-REC-X.520 
	// 2.	Internet X.509 Public Key Infrastructure Certificate and CRL Profile
	//	http://www.ietf.org/rfc/rfc3280.txt
	// 3.	A Summary of the X.500(96) User Schema for use with LDAPv3
	//	http://www.faqs.org/rfcs/rfc2256.html
	// 4.	RFC 2247 - Using Domains in LDAP/X.500 Distinguished Names
	//	http://www.faqs.org/rfcs/rfc2247.html

	/* 
	 * AttributeTypeAndValue ::= SEQUENCE {
	 * 	type     AttributeType,
	 * 	value    AttributeValue 
	 * }
	 * 
	 * AttributeType ::= OBJECT IDENTIFIER
	 * 
	 * AttributeValue ::= ANY DEFINED BY AttributeType
	 */
    public class X520 {

		public abstract class AttributeTypeAndValue {
			private string oid;
			private string attrValue;
			private int upperBound;
			private byte encoding;

			protected AttributeTypeAndValue (string oid, int upperBound)
			{
				this.oid = oid;
				this.upperBound = upperBound;
				this.encoding = 0xFF;
			}

			protected AttributeTypeAndValue (string oid, int upperBound, byte encoding) 
			{
				this.oid = oid;
				this.upperBound = upperBound;
				this.encoding = encoding;
			}

			public string Value {
				get { return attrValue; }
				set { 
					if ((attrValue != null) && (attrValue.Length > upperBound)) {
						string msg = ("Value length bigger than upperbound ({0}).");
						throw new FormatException (String.Format (msg, upperBound));
					}
					attrValue = value; 
				}
			}

			public ASN1 ASN1 {
				get { return GetASN1 (); }
			}

			internal ASN1 GetASN1 (byte encoding) 
			{
				byte encode = encoding;
				if (encode == 0xFF)
					encode = SelectBestEncoding ();
					
				ASN1 asn1 = new ASN1 (0x30);
				asn1.Add (ASN1Convert.FromOid (oid));
				switch (encode) {
					case 0x13:
						// PRINTABLESTRING
						asn1.Add (new ASN1 (0x13, Encoding.ASCII.GetBytes (attrValue)));
						break;
					case 0x16:
						// IA5STRING
						asn1.Add (new ASN1 (0x16, Encoding.ASCII.GetBytes (attrValue)));
						break;
					case 0x1E:
						// BMPSTRING
						asn1.Add (new ASN1 (0x1E, Encoding.BigEndianUnicode.GetBytes (attrValue)));
						break;
				}
				return asn1;
			}

			internal ASN1 GetASN1 () 
			{
				return GetASN1 (encoding);
			}

			public byte[] GetBytes (byte encoding) 
			{
				return GetASN1 (encoding) .GetBytes ();
			}

			public byte[] GetBytes () 
			{
				return GetASN1 () .GetBytes ();
			}

			private byte SelectBestEncoding ()
			{
				foreach (char c in attrValue) {
					switch (c) {
					case '@':
					case '_':
						return 0x1E; // BMPSTRING
					default:
						if (c > 127)
							return 0x1E; // BMPSTRING
						break;
					}
				}
				return 0x13; // PRINTABLESTRING
			}
		}

		public class Name : AttributeTypeAndValue {

			public Name () : base ("2.5.4.41", 32768) 
			{
			}
		}

		public class CommonName : AttributeTypeAndValue {

			public CommonName () : base ("2.5.4.3", 64) 
			{
			}
		}

		// RFC2256, Section 5.6
		public class SerialNumber : AttributeTypeAndValue {

			// max length 64 bytes, Printable String only
			public SerialNumber ()
				: base ("2.5.4.5", 64, 0x13)
			{
			}
		}

		public class LocalityName : AttributeTypeAndValue {

			public LocalityName () : base ("2.5.4.7", 128)
			{
			}
		}

		public class StateOrProvinceName : AttributeTypeAndValue {

			public StateOrProvinceName () : base ("2.5.4.8", 128) 
			{
			}
		}
		 
		public class OrganizationName : AttributeTypeAndValue {

			public OrganizationName () : base ("2.5.4.10", 64)
			{
			}
		}
		 
		public class OrganizationalUnitName : AttributeTypeAndValue {

			public OrganizationalUnitName () : base ("2.5.4.11", 64)
			{
			}
		}

		// NOTE: Not part of RFC2253
		public class EmailAddress : AttributeTypeAndValue {

			public EmailAddress () : base ("1.2.840.113549.1.9.1", 128, 0x16)
			{
			}
		}

		// RFC2247, Section 4
		public class DomainComponent : AttributeTypeAndValue {

			// no maximum length defined
			public DomainComponent ()
				: base ("0.9.2342.19200300.100.1.25", Int32.MaxValue, 0x16)
			{
			}
		}

		// RFC1274, Section 9.3.1
		public class UserId : AttributeTypeAndValue {

			public UserId ()
				: base ("0.9.2342.19200300.100.1.1", 256)
			{
			}
		}

		public class Oid : AttributeTypeAndValue {

			public Oid (string oid)
				: base (oid, Int32.MaxValue)
			{
			}
		}

		/* -- Naming attributes of type X520Title
		 * id-at-title             AttributeType ::= { id-at 12 }
		 * 
		 * X520Title ::= CHOICE {
		 *       teletexString     TeletexString   (SIZE (1..ub-title)),
		 *       printableString   PrintableString (SIZE (1..ub-title)),
		 *       universalString   UniversalString (SIZE (1..ub-title)),
		 *       utf8String        UTF8String      (SIZE (1..ub-title)),
		 *       bmpString         BMPString       (SIZE (1..ub-title)) 
		 * }
		 */
		public class Title : AttributeTypeAndValue {

			public Title () : base ("2.5.4.12", 64)
			{
			}
		}

		public class CountryName : AttributeTypeAndValue {

			// (0x13) PRINTABLESTRING
			public CountryName () : base ("2.5.4.6", 2, 0x13) 
			{
			}
		}

		public class DnQualifier : AttributeTypeAndValue {

			// (0x13) PRINTABLESTRING
			public DnQualifier () : base ("2.5.4.46", 2, 0x13) 
			{
			}
		}

		public class Surname : AttributeTypeAndValue {

			public Surname () : base ("2.5.4.4", 32768) 
			{
			}
		}

		public class GivenName : AttributeTypeAndValue {

			public GivenName () : base ("2.5.4.42", 16) 
			{
			}
		}

		public class Initial : AttributeTypeAndValue {

			public Initial () : base ("2.5.4.43", 5) 
			{
			}
		}

	}
        
	/* From RFC3280
	 * --  specifications of Upper Bounds MUST be regarded as mandatory
	 * --  from Annex B of ITU-T X.411 Reference Definition of MTS Parameter
	 * 
	 * --  Upper Bounds
	 * 
	 * ub-name INTEGER ::= 32768
	 * ub-common-name INTEGER ::= 64
	 * ub-locality-name INTEGER ::= 128
	 * ub-state-name INTEGER ::= 128
	 * ub-organization-name INTEGER ::= 64
	 * ub-organizational-unit-name INTEGER ::= 64
	 * ub-title INTEGER ::= 64
	 * ub-serial-number INTEGER ::= 64
	 * ub-match INTEGER ::= 128
	 * ub-emailaddress-length INTEGER ::= 128
	 * ub-common-name-length INTEGER ::= 64
	 * ub-country-name-alpha-length INTEGER ::= 2
	 * ub-country-name-numeric-length INTEGER ::= 3
	 * ub-domain-defined-attributes INTEGER ::= 4
	 * ub-domain-defined-attribute-type-length INTEGER ::= 8
	 * ub-domain-defined-attribute-value-length INTEGER ::= 128
	 * ub-domain-name-length INTEGER ::= 16
	 * ub-extension-attributes INTEGER ::= 256
	 * ub-e163-4-number-length INTEGER ::= 15
	 * ub-e163-4-sub-address-length INTEGER ::= 40
	 * ub-generation-qualifier-length INTEGER ::= 3
	 * ub-given-name-length INTEGER ::= 16
	 * ub-initials-length INTEGER ::= 5
	 * ub-integer-options INTEGER ::= 256
	 * ub-numeric-user-id-length INTEGER ::= 32
	 * ub-organization-name-length INTEGER ::= 64
	 * ub-organizational-unit-name-length INTEGER ::= 32
	 * ub-organizational-units INTEGER ::= 4
	 * ub-pds-name-length INTEGER ::= 16
	 * ub-pds-parameter-length INTEGER ::= 30
	 * ub-pds-physical-address-lines INTEGER ::= 6
	 * ub-postal-code-length INTEGER ::= 16
	 * ub-pseudonym INTEGER ::= 128
	 * ub-surname-length INTEGER ::= 40
	 * ub-terminal-id-length INTEGER ::= 24
	 * ub-unformatted-address-length INTEGER ::= 180
	 * ub-x121-address-length INTEGER ::= 16
	 * 
	 * -- Note - upper bounds on string types, such as TeletexString, are
	 * -- measured in characters.  Excepting PrintableString or IA5String, a
	 * -- significantly greater number of octets will be required to hold
	 * -- such a value.  As a minimum, 16 octets, or twice the specified
	 * -- upper bound, whichever is the larger, should be allowed for
	 * -- TeletexString.  For UTF8String or UniversalString at least four
	 * -- times the upper bound should be allowed.
	 */
}
