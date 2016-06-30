//
// PKCS7.cs: PKCS #7 - Cryptographic Message Syntax Standard 
//	http://www.rsasecurity.com/rsalabs/pkcs/pkcs-7/index.html
//
// Authors:
//	Sebastien Pouliot <sebastien@ximian.com>
//	Daniel Granath <dgranath#gmail.com>
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
using System.Collections;
using System.Security.Cryptography;

namespace MediaBrowser.Server.Mono.Security {

    public sealed class PKCS7 {

		public class Oid {
			// pkcs 1
			public const string rsaEncryption = "1.2.840.113549.1.1.1";
			// pkcs 7
			public const string data = "1.2.840.113549.1.7.1";
			public const string signedData = "1.2.840.113549.1.7.2";
			public const string envelopedData = "1.2.840.113549.1.7.3";
			public const string signedAndEnvelopedData = "1.2.840.113549.1.7.4";
			public const string digestedData = "1.2.840.113549.1.7.5";
			public const string encryptedData = "1.2.840.113549.1.7.6";
			// pkcs 9
			public const string contentType = "1.2.840.113549.1.9.3";
			public const string messageDigest  = "1.2.840.113549.1.9.4";
			public const string signingTime = "1.2.840.113549.1.9.5";
			public const string countersignature = "1.2.840.113549.1.9.6";

			public Oid () 
			{
			}
		}

		private PKCS7 ()
		{
		}

		static public ASN1 Attribute (string oid, ASN1 value) 
		{
			ASN1 attr = new ASN1 (0x30);
			attr.Add (ASN1Convert.FromOid (oid));
			ASN1 aset = attr.Add (new ASN1 (0x31));
			aset.Add (value);
			return attr;
		}

		static public ASN1 AlgorithmIdentifier (string oid)
		{
			ASN1 ai = new ASN1 (0x30);
			ai.Add (ASN1Convert.FromOid (oid));
			ai.Add (new ASN1 (0x05));	// NULL
			return ai;
		}

		static public ASN1 AlgorithmIdentifier (string oid, ASN1 parameters) 
		{
			ASN1 ai = new ASN1 (0x30);
			ai.Add (ASN1Convert.FromOid (oid));
			ai.Add (parameters);
			return ai;
		}

		/*
		 * IssuerAndSerialNumber ::= SEQUENCE {
		 *	issuer Name,
		 *	serialNumber CertificateSerialNumber 
		 * }
		 */
		static public ASN1 IssuerAndSerialNumber (X509Certificate x509) 
		{
			ASN1 issuer = null;
			ASN1 serial = null;
			ASN1 cert = new ASN1 (x509.RawData);
			int tbs = 0;
			bool flag = false;
			while (tbs < cert[0].Count) {
				ASN1 e = cert[0][tbs++];
				if (e.Tag == 0x02)
					serial = e;
				else if (e.Tag == 0x30) {
					if (flag) {
						issuer = e;
						break;
					}
					flag = true;
				}
			}
			ASN1 iasn = new ASN1 (0x30);
			iasn.Add (issuer);
			iasn.Add (serial);
			return iasn;
		}

		/*
		 * ContentInfo ::= SEQUENCE {
		 *	contentType ContentType,
		 *	content [0] EXPLICIT ANY DEFINED BY contentType OPTIONAL 
		 * }
		 * ContentType ::= OBJECT IDENTIFIER
		 */
		public class ContentInfo {

			private string contentType;
			private ASN1 content;

			public ContentInfo () 
			{
				content = new ASN1 (0xA0);
			}

			public ContentInfo (string oid) : this ()
			{
				contentType = oid;
			}

			public ContentInfo (byte[] data) 
				: this (new ASN1 (data)) {}

			public ContentInfo (ASN1 asn1) 
			{
				// SEQUENCE with 1 or 2 elements
				if ((asn1.Tag != 0x30) || ((asn1.Count < 1) && (asn1.Count > 2)))
					throw new ArgumentException ("Invalid ASN1");
				if (asn1[0].Tag != 0x06)
					throw new ArgumentException ("Invalid contentType");
				contentType = ASN1Convert.ToOid (asn1[0]);
				if (asn1.Count > 1) {
					if (asn1[1].Tag != 0xA0)
						throw new ArgumentException ("Invalid content");
					content = asn1[1];
				}
			}

			public ASN1 ASN1 {
				get { return GetASN1(); }
			}

			public ASN1 Content {
				get { return content; }
				set { content = value; }
			}

			public string ContentType {
				get { return contentType; }
				set { contentType = value; }
			}

			internal ASN1 GetASN1 () 
			{
				// ContentInfo ::= SEQUENCE {
				ASN1 contentInfo = new ASN1 (0x30);
				// contentType ContentType, -> ContentType ::= OBJECT IDENTIFIER
				contentInfo.Add (ASN1Convert.FromOid (contentType));
				// content [0] EXPLICIT ANY DEFINED BY contentType OPTIONAL 
				if ((content != null) && (content.Count > 0))
					contentInfo.Add (content);
				return contentInfo;
			}

			public byte[] GetBytes () 
			{
				return GetASN1 ().GetBytes ();
			}
		}

		/*
		 * EncryptedData ::= SEQUENCE {
		 *	version		INTEGER {edVer0(0)} (edVer0),
		 *	 encryptedContentInfo  EncryptedContentInfo
		 * }
		 */
		public class EncryptedData {
			private byte _version;
			private ContentInfo _content;
			private ContentInfo _encryptionAlgorithm;
			private byte[] _encrypted;

			public EncryptedData () 
			{
				_version = 0;
			}

			public EncryptedData (byte[] data) 
				: this (new ASN1 (data))
			{
			}

			public EncryptedData (ASN1 asn1) : this () 
			{
				if ((asn1.Tag != 0x30) || (asn1.Count < 2))
					throw new ArgumentException ("Invalid EncryptedData");

				if (asn1 [0].Tag != 0x02)
					throw new ArgumentException ("Invalid version");
				_version = asn1 [0].Value [0];

				ASN1 encryptedContentInfo = asn1 [1];
				if (encryptedContentInfo.Tag != 0x30)
					throw new ArgumentException ("missing EncryptedContentInfo");

				ASN1 contentType = encryptedContentInfo [0];
				if (contentType.Tag != 0x06)
					throw new ArgumentException ("missing EncryptedContentInfo.ContentType");
				_content = new ContentInfo (ASN1Convert.ToOid (contentType));

				ASN1 contentEncryptionAlgorithm = encryptedContentInfo [1];
				if (contentEncryptionAlgorithm.Tag != 0x30)
					throw new ArgumentException ("missing EncryptedContentInfo.ContentEncryptionAlgorithmIdentifier");
				_encryptionAlgorithm = new ContentInfo (ASN1Convert.ToOid (contentEncryptionAlgorithm [0]));
				_encryptionAlgorithm.Content = contentEncryptionAlgorithm [1];
				
				ASN1 encryptedContent = encryptedContentInfo [2];
				if (encryptedContent.Tag != 0x80)
					throw new ArgumentException ("missing EncryptedContentInfo.EncryptedContent");
				_encrypted = encryptedContent.Value;
			}

			public ASN1 ASN1 {
				get { return GetASN1(); }
			}

			public ContentInfo ContentInfo {
				get { return _content; }
			}

			public ContentInfo EncryptionAlgorithm {
				get { return _encryptionAlgorithm; }
			}

			public byte[] EncryptedContent {
				get {
					if (_encrypted == null)
						return null;
					return (byte[]) _encrypted.Clone ();
				}
			}

			public byte Version {
				get { return _version; }
				set { _version = value; }
			}

			// methods

			internal ASN1 GetASN1 () 
			{
				return null;
			}

			public byte[] GetBytes () 
			{
				return GetASN1 ().GetBytes ();
			}
		}

		/*
		 * EnvelopedData ::= SEQUENCE {
		 *	version Version,
		 *	recipientInfos RecipientInfos,
		 *	encryptedContentInfo EncryptedContentInfo 
		 * }
		 * 
		 * RecipientInfos ::= SET OF RecipientInfo
		 * 
		 * EncryptedContentInfo ::= SEQUENCE {
		 *	contentType ContentType,
		 *	contentEncryptionAlgorithm ContentEncryptionAlgorithmIdentifier,
		 *	encryptedContent [0] IMPLICIT EncryptedContent OPTIONAL 
		 * }
		 * 
		 * EncryptedContent ::= OCTET STRING
		 * 
		 */
		public class EnvelopedData {
			private byte _version;
			private ContentInfo _content;
			private ContentInfo _encryptionAlgorithm;
			private ArrayList _recipientInfos;
			private byte[] _encrypted;

			public EnvelopedData () 
			{
				_version = 0;
				_content = new ContentInfo ();
				_encryptionAlgorithm = new ContentInfo ();
				_recipientInfos = new ArrayList ();
			}

			public EnvelopedData (byte[] data) 
				: this (new ASN1 (data))
			{
			}

			public EnvelopedData (ASN1 asn1) : this ()
			{
				if ((asn1[0].Tag != 0x30) || (asn1[0].Count < 3))
					throw new ArgumentException ("Invalid EnvelopedData");

				if (asn1[0][0].Tag != 0x02)
					throw new ArgumentException ("Invalid version");
				_version = asn1[0][0].Value[0];

				// recipientInfos

				ASN1 recipientInfos = asn1 [0][1];
				if (recipientInfos.Tag != 0x31)
					throw new ArgumentException ("missing RecipientInfos");
				for (int i=0; i < recipientInfos.Count; i++) {
					ASN1 recipientInfo = recipientInfos [i];
					_recipientInfos.Add (new RecipientInfo (recipientInfo));
				}

				ASN1 encryptedContentInfo = asn1[0][2];
				if (encryptedContentInfo.Tag != 0x30)
					throw new ArgumentException ("missing EncryptedContentInfo");

				ASN1 contentType = encryptedContentInfo [0];
				if (contentType.Tag != 0x06)
					throw new ArgumentException ("missing EncryptedContentInfo.ContentType");
				_content = new ContentInfo (ASN1Convert.ToOid (contentType));

				ASN1 contentEncryptionAlgorithm = encryptedContentInfo [1];
				if (contentEncryptionAlgorithm.Tag != 0x30)
					throw new ArgumentException ("missing EncryptedContentInfo.ContentEncryptionAlgorithmIdentifier");
				_encryptionAlgorithm = new ContentInfo (ASN1Convert.ToOid (contentEncryptionAlgorithm [0]));
				_encryptionAlgorithm.Content = contentEncryptionAlgorithm [1];
				
				ASN1 encryptedContent = encryptedContentInfo [2];
				if (encryptedContent.Tag != 0x80)
					throw new ArgumentException ("missing EncryptedContentInfo.EncryptedContent");
				_encrypted = encryptedContent.Value;
			}

			public ArrayList RecipientInfos {
				  get { return _recipientInfos; }
			}

			public ASN1 ASN1 {
				get { return GetASN1(); }
			}

			public ContentInfo ContentInfo {
				get { return _content; }
			}

			public ContentInfo EncryptionAlgorithm {
				get { return _encryptionAlgorithm; }
			}

			public byte[] EncryptedContent {
				get { 
					if (_encrypted == null)
						return null;
					return (byte[]) _encrypted.Clone ();
				}
			}

			public byte Version {
				get { return _version; }
				set { _version = value; }
			}

			internal ASN1 GetASN1 () 
			{
				// SignedData ::= SEQUENCE {
				ASN1 signedData = new ASN1 (0x30);
				// version Version -> Version ::= INTEGER
/*				byte[] ver = { _version };
				signedData.Add (new ASN1 (0x02, ver));
				// digestAlgorithms DigestAlgorithmIdentifiers -> DigestAlgorithmIdentifiers ::= SET OF DigestAlgorithmIdentifier
				ASN1 digestAlgorithms = signedData.Add (new ASN1 (0x31));
				if (hashAlgorithm != null) {
					string hashOid = CryptoConfig.MapNameToOid (hashAlgorithm);
					digestAlgorithms.Add (AlgorithmIdentifier (hashOid));
				}

				// contentInfo ContentInfo,
				ASN1 ci = contentInfo.ASN1;
				signedData.Add (ci);
				if ((mda == null) && (hashAlgorithm != null)) {
					// automatically add the messageDigest authenticated attribute
					HashAlgorithm ha = HashAlgorithm.Create (hashAlgorithm);
					byte[] idcHash = ha.ComputeHash (ci[1][0].Value);
					ASN1 md = new ASN1 (0x30);
					mda = Attribute (messageDigest, md.Add (new ASN1 (0x04, idcHash)));
					signerInfo.AuthenticatedAttributes.Add (mda);
				}

				// certificates [0] IMPLICIT ExtendedCertificatesAndCertificates OPTIONAL,
				if (certs.Count > 0) {
					ASN1 a0 = signedData.Add (new ASN1 (0xA0));
					foreach (X509Certificate x in certs)
						a0.Add (new ASN1 (x.RawData));
				}
				// crls [1] IMPLICIT CertificateRevocationLists OPTIONAL,
				if (crls.Count > 0) {
					ASN1 a1 = signedData.Add (new ASN1 (0xA1));
					foreach (byte[] crl in crls)
						a1.Add (new ASN1 (crl));
				}
				// signerInfos SignerInfos -> SignerInfos ::= SET OF SignerInfo
				ASN1 signerInfos = signedData.Add (new ASN1 (0x31));
				if (signerInfo.Key != null)
					signerInfos.Add (signerInfo.ASN1);*/
				return signedData;
			}

			public byte[] GetBytes () {
				return GetASN1 ().GetBytes ();
			}
		}

		/* RecipientInfo ::= SEQUENCE {
		 *	version Version,
		 *	issuerAndSerialNumber IssuerAndSerialNumber,
		 *	keyEncryptionAlgorithm KeyEncryptionAlgorithmIdentifier,
		 *	encryptedKey EncryptedKey 
		 * }
		 * 
		 * KeyEncryptionAlgorithmIdentifier ::= AlgorithmIdentifier
		 * 
		 * EncryptedKey ::= OCTET STRING
		 */
		public class RecipientInfo {

			private int _version;
			private string _oid;
			private byte[] _key;
			private byte[] _ski;
			private string _issuer;
			private byte[] _serial;

			public RecipientInfo () {}

			public RecipientInfo (ASN1 data) 
			{
				if (data.Tag != 0x30)
					throw new ArgumentException ("Invalid RecipientInfo");
				
				ASN1 version = data [0];
				if (version.Tag != 0x02)
					throw new ArgumentException ("missing Version");
				_version = version.Value [0];

				// issuerAndSerialNumber IssuerAndSerialNumber
				ASN1 subjectIdentifierType = data [1];
				if ((subjectIdentifierType.Tag == 0x80) && (_version == 3)) {
					_ski = subjectIdentifierType.Value;
				}
				else {
					_issuer = X501.ToString (subjectIdentifierType [0]);
					_serial = subjectIdentifierType [1].Value;
				}

				ASN1 keyEncryptionAlgorithm = data [2];
				_oid = ASN1Convert.ToOid (keyEncryptionAlgorithm [0]);

				ASN1 encryptedKey = data [3];
				_key = encryptedKey.Value;
			}

			public string Oid {
				get { return _oid; }
			}

			public byte[] Key {
				get { 
					if (_key == null)
						return null;
                                        return (byte[]) _key.Clone ();
				}
			}

			public byte[] SubjectKeyIdentifier {
				get { 
					if (_ski == null)
						return null;
					return (byte[]) _ski.Clone ();
				}
			}

			public string Issuer {
				get { return _issuer; }
			}

			public byte[] Serial {
				get { 
					if (_serial == null)
						return null;
					return (byte[]) _serial.Clone ();
				}
			}

			public int Version {
				get { return _version; }
			}
		}

		/*
		 * SignedData ::= SEQUENCE {
		 *	version Version,
		 *	digestAlgorithms DigestAlgorithmIdentifiers,
		 *	contentInfo ContentInfo,
		 *	certificates [0] IMPLICIT ExtendedCertificatesAndCertificates OPTIONAL,
		 *	crls [1] IMPLICIT CertificateRevocationLists OPTIONAL,
		 *	signerInfos SignerInfos 
		 * }
		 */
		public class SignedData {
			private byte version;
			private string hashAlgorithm;
			private ContentInfo contentInfo;
			private X509CertificateCollection certs;
			private ArrayList crls;
			private SignerInfo signerInfo;
			private bool mda;
			private bool signed;

			public SignedData () 
			{
				version = 1;
				contentInfo = new ContentInfo ();
				certs = new X509CertificateCollection ();
				crls = new ArrayList ();
				signerInfo = new SignerInfo ();
				mda = true;
				signed = false;
			}

			public SignedData (byte[] data) 
				: this (new ASN1 (data)) 
			{
			}

			public SignedData (ASN1 asn1) 
			{
				if ((asn1[0].Tag != 0x30) || (asn1[0].Count < 4))
					throw new ArgumentException ("Invalid SignedData");

				if (asn1[0][0].Tag != 0x02)
					throw new ArgumentException ("Invalid version");
				version = asn1[0][0].Value[0];

				contentInfo = new ContentInfo (asn1[0][2]);

				int n = 3;
				certs = new X509CertificateCollection ();
				if (asn1[0][n].Tag == 0xA0) {
					for (int i=0; i < asn1[0][n].Count; i++)
						certs.Add (new X509Certificate (asn1[0][n][i].GetBytes ()));
					n++;
				}

				crls = new ArrayList ();
				if (asn1[0][n].Tag == 0xA1) {
					for (int i=0; i < asn1[0][n].Count; i++)
						crls.Add (asn1[0][n][i].GetBytes ());
					n++;
				}

				if (asn1[0][n].Count > 0)
					signerInfo = new SignerInfo (asn1[0][n]);
				else
					signerInfo = new SignerInfo ();

				// Exchange hash algorithm Oid from SignerInfo
				if (signerInfo.HashName != null) {
					HashName = OidToName(signerInfo.HashName);
				}
				
				// Check if SignerInfo has authenticated attributes
				mda = (signerInfo.AuthenticatedAttributes.Count > 0);
			}

			public ASN1 ASN1 {
				get { return GetASN1(); }
			}

			public X509CertificateCollection Certificates {
				get { return certs; }
			}

			public ContentInfo ContentInfo {
				get { return contentInfo; }
			}

			public ArrayList Crls {
				get { return crls; }
			}

			public string HashName {
				get { return hashAlgorithm; }
				// todo add validation
				set { 
					hashAlgorithm = value; 
					signerInfo.HashName = value;
				}
			}

			public SignerInfo SignerInfo {
				get { return signerInfo; }
			}

			public byte Version {
				get { return version; }
				set { version = value; }
			}

			public bool UseAuthenticatedAttributes {
				get { return mda; }
				set { mda = value; }
			}

			public bool VerifySignature (AsymmetricAlgorithm aa)
			{
				if (aa == null) {
					return false;
				}

				RSAPKCS1SignatureDeformatter r = new RSAPKCS1SignatureDeformatter (aa);
				r.SetHashAlgorithm (hashAlgorithm);
				HashAlgorithm ha = HashAlgorithm.Create (hashAlgorithm);

				byte[] signature = signerInfo.Signature;
				byte[] hash = null;

				if (mda) {
					ASN1 asn = new ASN1 (0x31);
					foreach (ASN1 attr in signerInfo.AuthenticatedAttributes)
						asn.Add (attr);

					hash = ha.ComputeHash (asn.GetBytes ());
				} else {
					hash = ha.ComputeHash (contentInfo.Content[0].Value);
				}

				if (hash != null && signature != null) {
					return r.VerifySignature (hash, signature);
				}
				return false;
			}

			internal string OidToName (string oid)
			{
				switch (oid) {
				case "1.3.14.3.2.26" :
					return "SHA1";
				case "1.2.840.113549.2.2" :
					return "MD2";
				case "1.2.840.113549.2.5" :
					return "MD5";
				case "2.16.840.1.101.3.4.1" :
					return "SHA256";
				case "2.16.840.1.101.3.4.2" :
					return "SHA384";
				case "2.16.840.1.101.3.4.3" :
					return "SHA512";
				default :
					break;
				}
				// Unknown Oid
				return oid;
			}

			internal ASN1 GetASN1 () 
			{
				// SignedData ::= SEQUENCE {
				ASN1 signedData = new ASN1 (0x30);
				// version Version -> Version ::= INTEGER
				byte[] ver = { version };
				signedData.Add (new ASN1 (0x02, ver));
				// digestAlgorithms DigestAlgorithmIdentifiers -> DigestAlgorithmIdentifiers ::= SET OF DigestAlgorithmIdentifier
				ASN1 digestAlgorithms = signedData.Add (new ASN1 (0x31));
				if (hashAlgorithm != null) {
					string hashOid = CryptoConfig.MapNameToOID (hashAlgorithm);
					digestAlgorithms.Add (AlgorithmIdentifier (hashOid));
				}

				// contentInfo ContentInfo,
				ASN1 ci = contentInfo.ASN1;
				signedData.Add (ci);
				if (!signed && (hashAlgorithm != null)) {
					if (mda) {
						// Use authenticated attributes for signature
						
						// Automatically add the contentType authenticated attribute
						ASN1 ctattr = Attribute (Oid.contentType, ci[0]);
						signerInfo.AuthenticatedAttributes.Add (ctattr);
						
						// Automatically add the messageDigest authenticated attribute
						HashAlgorithm ha = HashAlgorithm.Create (hashAlgorithm);
						byte[] idcHash = ha.ComputeHash (ci[1][0].Value);
						ASN1 md = new ASN1 (0x30);
						ASN1 mdattr = Attribute (Oid.messageDigest, md.Add (new ASN1 (0x04, idcHash)));
						signerInfo.AuthenticatedAttributes.Add (mdattr);
					} else {
						// Don't use authenticated attributes for signature -- signature is content
						RSAPKCS1SignatureFormatter r = new RSAPKCS1SignatureFormatter (signerInfo.Key);
						r.SetHashAlgorithm (hashAlgorithm);
						HashAlgorithm ha = HashAlgorithm.Create (hashAlgorithm);
						byte[] sig = ha.ComputeHash (ci[1][0].Value);
						signerInfo.Signature = r.CreateSignature (sig);
					}
					signed = true;
				}

				// certificates [0] IMPLICIT ExtendedCertificatesAndCertificates OPTIONAL,
				if (certs.Count > 0) {
					ASN1 a0 = signedData.Add (new ASN1 (0xA0));
					foreach (X509Certificate x in certs)
						a0.Add (new ASN1 (x.RawData));
				}
				// crls [1] IMPLICIT CertificateRevocationLists OPTIONAL,
				if (crls.Count > 0) {
					ASN1 a1 = signedData.Add (new ASN1 (0xA1));
					foreach (byte[] crl in crls)
						a1.Add (new ASN1 (crl));
				}
				// signerInfos SignerInfos -> SignerInfos ::= SET OF SignerInfo
				ASN1 signerInfos = signedData.Add (new ASN1 (0x31));
				if (signerInfo.Key != null)
					signerInfos.Add (signerInfo.ASN1);
				return signedData;
			}

			public byte[] GetBytes () 
			{
				return GetASN1 ().GetBytes ();
			}
		}

		/*
		 * SignerInfo ::= SEQUENCE {
		 *	version Version,
		 * 	issuerAndSerialNumber IssuerAndSerialNumber,
		 * 	digestAlgorithm DigestAlgorithmIdentifier,
		 * 	authenticatedAttributes [0] IMPLICIT Attributes OPTIONAL,
		 * 	digestEncryptionAlgorithm DigestEncryptionAlgorithmIdentifier,
		 * 	encryptedDigest EncryptedDigest,
		 * 	unauthenticatedAttributes [1] IMPLICIT Attributes OPTIONAL 
		 * }
		 * 
		 * For version == 3 issuerAndSerialNumber may be replaced by ...
		 */
		public class SignerInfo {

			private byte version;
			private X509Certificate x509;
			private string hashAlgorithm;
			private AsymmetricAlgorithm key;
			private ArrayList authenticatedAttributes;
			private ArrayList unauthenticatedAttributes;
			private byte[] signature;
			private string issuer;
			private byte[] serial;
			private byte[] ski;

			public SignerInfo () 
			{
				version = 1;
				authenticatedAttributes = new ArrayList ();
				unauthenticatedAttributes = new ArrayList ();
			}

			public SignerInfo (byte[] data) 
				: this (new ASN1 (data)) {}

			// TODO: INCOMPLETE
			public SignerInfo (ASN1 asn1) : this () 
			{
				if ((asn1[0].Tag != 0x30) || (asn1[0].Count < 5))
					throw new ArgumentException ("Invalid SignedData");

				// version Version
				if (asn1[0][0].Tag != 0x02)
					throw new ArgumentException ("Invalid version");
				version = asn1[0][0].Value[0];

				// issuerAndSerialNumber IssuerAndSerialNumber
				ASN1 subjectIdentifierType = asn1 [0][1];
				if ((subjectIdentifierType.Tag == 0x80) && (version == 3)) {
					ski = subjectIdentifierType.Value;
				}
				else {
					issuer = X501.ToString (subjectIdentifierType [0]);
					serial = subjectIdentifierType [1].Value;
				}

				// digestAlgorithm DigestAlgorithmIdentifier
				ASN1 digestAlgorithm = asn1 [0][2];
				hashAlgorithm = ASN1Convert.ToOid (digestAlgorithm [0]);

				// authenticatedAttributes [0] IMPLICIT Attributes OPTIONAL
				int n = 3;
				ASN1 authAttributes = asn1 [0][n];
				if (authAttributes.Tag == 0xA0) {
					n++;
					for (int i=0; i < authAttributes.Count; i++)
						authenticatedAttributes.Add (authAttributes [i]);
				}

				// digestEncryptionAlgorithm DigestEncryptionAlgorithmIdentifier
				n++;
				// ASN1 digestEncryptionAlgorithm = asn1 [0][n++];
				// string digestEncryptionAlgorithmOid = ASN1Convert.ToOid (digestEncryptionAlgorithm [0]);

				// encryptedDigest EncryptedDigest
				ASN1 encryptedDigest = asn1 [0][n++];
				if (encryptedDigest.Tag == 0x04)
					signature = encryptedDigest.Value;

				// unauthenticatedAttributes [1] IMPLICIT Attributes OPTIONAL
				ASN1 unauthAttributes = asn1 [0][n];
				if ((unauthAttributes != null) && (unauthAttributes.Tag == 0xA1)) {
					for (int i=0; i < unauthAttributes.Count; i++)
						unauthenticatedAttributes.Add (unauthAttributes [i]);
				}
			}

			public string IssuerName {
				get { return issuer; }
			}

			public byte[] SerialNumber {
				get { 
					if (serial == null)
						return null;
					return (byte[]) serial.Clone (); 
				}
			}

			public byte[] SubjectKeyIdentifier {
				get { 
					if (ski == null)
						return null;
					return (byte[]) ski.Clone (); 
				}
			}

			public ASN1 ASN1 {
				get { return GetASN1(); }
			}

			public ArrayList AuthenticatedAttributes {
				get { return authenticatedAttributes; }
			}

			public X509Certificate Certificate {
				get { return x509; }
				set { x509 = value; }
			}

			public string HashName {
				get { return hashAlgorithm; }
				set { hashAlgorithm = value; }
			}

			public AsymmetricAlgorithm Key {
				get { return key; }
				set { key = value; }
			}

			public byte[] Signature {
				get { 
					if (signature == null)
						return null;
					return (byte[]) signature.Clone (); 
				}

				set {
					if (value != null) {
						signature = (byte[]) value.Clone ();
					}
				}
			}

			public ArrayList UnauthenticatedAttributes {
				get { return unauthenticatedAttributes; }
			}

			public byte Version {
				get { return version; }
				set { version = value; }
			}

			internal ASN1 GetASN1 () 
			{
				if ((key == null) || (hashAlgorithm == null))
					return null;
				byte[] ver = { version };
				ASN1 signerInfo = new ASN1 (0x30);
				// version Version -> Version ::= INTEGER
				signerInfo.Add (new ASN1 (0x02, ver));
				// issuerAndSerialNumber IssuerAndSerialNumber,
				signerInfo.Add (PKCS7.IssuerAndSerialNumber (x509));
				// digestAlgorithm DigestAlgorithmIdentifier,
				string hashOid = CryptoConfig.MapNameToOID (hashAlgorithm);
				signerInfo.Add (AlgorithmIdentifier (hashOid));
				// authenticatedAttributes [0] IMPLICIT Attributes OPTIONAL,
				ASN1 aa = null;
				if (authenticatedAttributes.Count > 0) {
					aa = signerInfo.Add (new ASN1 (0xA0));
					authenticatedAttributes.Sort(new SortedSet ());
					foreach (ASN1 attr in authenticatedAttributes)
						aa.Add (attr);
				}
				// digestEncryptionAlgorithm DigestEncryptionAlgorithmIdentifier,
				if (key is RSA) {
					signerInfo.Add (AlgorithmIdentifier (PKCS7.Oid.rsaEncryption));

					if (aa != null) {
						// Calculate the signature here; otherwise it must be set from SignedData
						RSAPKCS1SignatureFormatter r = new RSAPKCS1SignatureFormatter (key);
						r.SetHashAlgorithm (hashAlgorithm);
						byte[] tbs = aa.GetBytes ();
						tbs [0] = 0x31; // not 0xA0 for signature
						HashAlgorithm ha = HashAlgorithm.Create (hashAlgorithm);
						byte[] tbsHash = ha.ComputeHash (tbs);
						signature = r.CreateSignature (tbsHash);
					}
				}
				else if (key is DSA) {
					throw new NotImplementedException ("not yet");
				}
				else
					throw new CryptographicException ("Unknown assymetric algorithm");
				// encryptedDigest EncryptedDigest,
				signerInfo.Add (new ASN1 (0x04, signature));
				// unauthenticatedAttributes [1] IMPLICIT Attributes OPTIONAL 
				if (unauthenticatedAttributes.Count > 0) {
					ASN1 ua = signerInfo.Add (new ASN1 (0xA1));
					unauthenticatedAttributes.Sort(new SortedSet ());
					foreach (ASN1 attr in unauthenticatedAttributes)
						ua.Add (attr);
				}
				return signerInfo;
			}

			public byte[] GetBytes () 
			{
				return GetASN1 ().GetBytes ();
			}
		}

		internal class SortedSet : IComparer {

			public int Compare (object x, object y)
			{
				if (x == null)
					return (y == null) ? 0 : -1;
				else if (y == null)
					return 1;

				ASN1 xx = x as ASN1;
				ASN1 yy = y as ASN1;
				
				if ((xx == null) || (yy == null)) {
					throw new ArgumentException (("Invalid objects."));
				}

				byte[] xb = xx.GetBytes ();
				byte[] yb = yy.GetBytes ();

				for (int i = 0; i < xb.Length; i++) {
					if (i == yb.Length)
						break;

					if (xb[i] == yb[i]) 
						continue;
						
					return (xb[i] < yb[i]) ? -1 : 1; 
				}

				// The arrays are equal up to the shortest of them.
				if (xb.Length > yb.Length)
					return 1;
				else if (xb.Length < yb.Length)
					return -1;

				return 0;
			}
		}
	}
}
