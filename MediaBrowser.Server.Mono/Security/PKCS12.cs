//
// PKCS12.cs: PKCS 12 - Personal Information Exchange Syntax
//
// Author:
//	Sebastien Pouliot  <sebastien@xamarin.com>
//
// (C) 2003 Motus Technologies Inc. (http://www.motus.com)
// Copyright (C) 2004,2005,2006 Novell Inc. (http://www.novell.com)
// Copyright 2013 Xamarin Inc. (http://www.xamarin.com)
//
// Key derivation translated from Bouncy Castle JCE (http://www.bouncycastle.org/)
// See bouncycastle.txt for license.
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
using System.Security.Cryptography;
using System.Text;

namespace MediaBrowser.Server.Mono.Security {

    public class PKCS5 {

		public const string pbeWithMD2AndDESCBC = "1.2.840.113549.1.5.1";
		public const string pbeWithMD5AndDESCBC = "1.2.840.113549.1.5.3";
		public const string pbeWithMD2AndRC2CBC = "1.2.840.113549.1.5.4";
		public const string pbeWithMD5AndRC2CBC = "1.2.840.113549.1.5.6";
		public const string pbeWithSHA1AndDESCBC = "1.2.840.113549.1.5.10";
		public const string pbeWithSHA1AndRC2CBC = "1.2.840.113549.1.5.11";

		public PKCS5 () {}
	}

    public class PKCS9 {

		public const string friendlyName = "1.2.840.113549.1.9.20";
		public const string localKeyId = "1.2.840.113549.1.9.21";

		public PKCS9 () {}
	}


	internal class SafeBag {
		private string _bagOID;
		private ASN1 _asn1;

		public SafeBag(string bagOID, ASN1 asn1) {
			_bagOID = bagOID;
			_asn1 = asn1;
		}

		public string BagOID {
			get { return _bagOID; }
		}

		public ASN1 ASN1 {
			get { return _asn1; }
		}
	}


    public class PKCS12 : ICloneable {

		public const string pbeWithSHAAnd128BitRC4 = "1.2.840.113549.1.12.1.1";
		public const string pbeWithSHAAnd40BitRC4 = "1.2.840.113549.1.12.1.2";
		public const string pbeWithSHAAnd3KeyTripleDESCBC = "1.2.840.113549.1.12.1.3";
		public const string pbeWithSHAAnd2KeyTripleDESCBC = "1.2.840.113549.1.12.1.4";
		public const string pbeWithSHAAnd128BitRC2CBC = "1.2.840.113549.1.12.1.5";
		public const string pbeWithSHAAnd40BitRC2CBC = "1.2.840.113549.1.12.1.6";

		// bags
		public const string keyBag  = "1.2.840.113549.1.12.10.1.1";
		public const string pkcs8ShroudedKeyBag  = "1.2.840.113549.1.12.10.1.2";
		public const string certBag  = "1.2.840.113549.1.12.10.1.3";
		public const string crlBag  = "1.2.840.113549.1.12.10.1.4";
		public const string secretBag  = "1.2.840.113549.1.12.10.1.5";
		public const string safeContentsBag  = "1.2.840.113549.1.12.10.1.6";

		// types
		public const string x509Certificate = "1.2.840.113549.1.9.22.1";
 		public const string sdsiCertificate = "1.2.840.113549.1.9.22.2";
		public const string x509Crl = "1.2.840.113549.1.9.23.1";

		// Adapted from BouncyCastle PKCS12ParametersGenerator.java
		public class DeriveBytes {

			public enum Purpose {
				Key,
				IV,
				MAC
			}

			static private byte[] keyDiversifier = { 1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1 };
			static private byte[] ivDiversifier  = { 2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2 };
			static private byte[] macDiversifier = { 3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3 };

			private string _hashName;
			private int _iterations;
			private byte[] _password;
			private byte[] _salt;

			public DeriveBytes () {}

			public string HashName {
				get { return _hashName; } 
				set { _hashName = value; }
			}

			public int IterationCount {
				get { return _iterations; }
				set { _iterations = value; }
			}

			public byte[] Password {
				get { return (byte[]) _password.Clone (); }
				set { 
					if (value == null)
						_password = new byte [0];
					else
						_password = (byte[]) value.Clone ();
				}
			}

			public byte[] Salt {
				get { return (byte[]) _salt.Clone ();  }
				set {
					if (value != null)
						_salt = (byte[]) value.Clone ();
					else
						_salt = null;
				}
			}

			private void Adjust (byte[] a, int aOff, byte[] b) 
			{
				int x = (b[b.Length - 1] & 0xff) + (a [aOff + b.Length - 1] & 0xff) + 1;

				a [aOff + b.Length - 1] = (byte) x;
				x >>= 8;

				for (int i = b.Length - 2; i >= 0; i--) {
					x += (b [i] & 0xff) + (a [aOff + i] & 0xff);
					a [aOff + i] = (byte) x;
					x >>= 8;
				}
			}

			private byte[] Derive (byte[] diversifier, int n) 
			{
				HashAlgorithm digest = PKCS1.CreateFromName (_hashName);
				int u = (digest.HashSize >> 3); // div 8
				int v = 64;
				byte[] dKey = new byte [n];

				byte[] S;
				if ((_salt != null) && (_salt.Length != 0)) {
					S = new byte[v * ((_salt.Length + v - 1) / v)];

					for (int i = 0; i != S.Length; i++) {
						S[i] = _salt[i % _salt.Length];
					}
				}
				else {
					S = new byte[0];
				}

				byte[] P;
				if ((_password != null) && (_password.Length != 0)) {
					P = new byte[v * ((_password.Length + v - 1) / v)];

					for (int i = 0; i != P.Length; i++) {
						P[i] = _password[i % _password.Length];
					}
				}
				else {
					P = new byte[0];
				}

				byte[] I = new byte [S.Length + P.Length];

				Buffer.BlockCopy (S, 0, I, 0, S.Length);
				Buffer.BlockCopy (P, 0, I, S.Length, P.Length);

				byte[]  B = new byte[v];
				int     c = (n + u - 1) / u;

				for (int i = 1; i <= c; i++) {
					digest.TransformBlock (diversifier, 0, diversifier.Length, diversifier, 0);
					digest.TransformFinalBlock (I, 0, I.Length);
					byte[] A = digest.Hash;
					digest.Initialize ();
					for (int j = 1; j != _iterations; j++) {
						A = digest.ComputeHash (A, 0, A.Length);
					}

					for (int j = 0; j != B.Length; j++) {
						B [j] = A [j % A.Length];
					}

					for (int j = 0; j != I.Length / v; j++) {
						Adjust (I, j * v, B);
					}

					if (i == c) {
						Buffer.BlockCopy(A, 0, dKey, (i - 1) * u, dKey.Length - ((i - 1) * u));
					}
					else {
						Buffer.BlockCopy(A, 0, dKey, (i - 1) * u, A.Length);
					}
				}

				return dKey;
			}

			public byte[] DeriveKey (int size) 
			{
				return Derive (keyDiversifier, size);
			}

			public byte[] DeriveIV (int size) 
			{
				return Derive (ivDiversifier, size);
			}

			public byte[] DeriveMAC (int size) 
			{
				return Derive (macDiversifier, size);
			}
		}

		const int recommendedIterationCount = 2000;

		//private int _version;
		private byte[] _password;
		private ArrayList _keyBags;
		private ArrayList _secretBags;
		private X509CertificateCollection _certs;
		private bool _keyBagsChanged;
		private bool _secretBagsChanged;
		private bool _certsChanged;
		private int _iterations;
		private ArrayList _safeBags;
		private RandomNumberGenerator _rng;

		// constructors

		public PKCS12 () 
		{
			_iterations = recommendedIterationCount;
			_keyBags = new ArrayList ();
			_secretBags = new ArrayList ();
			_certs = new X509CertificateCollection ();
			_keyBagsChanged = false;
			_secretBagsChanged = false;
			_certsChanged = false;
			_safeBags = new ArrayList ();
		}

		public PKCS12 (byte[] data)
			: this ()
		{
			Password = null;
			Decode (data);
		}

		/*
		 * PFX ::= SEQUENCE {
		 *	version INTEGER {v3(3)}(v3,...),
		 *	authSafe ContentInfo,
		 *	macData MacData OPTIONAL
		 * }
		 * 
		 * MacData ::= SEQUENCE {
		 *	mac DigestInfo,
		 *	macSalt OCTET STRING,
		 *	iterations INTEGER DEFAULT 1
		 *	-- Note: The default is for historical reasons and its use is deprecated. A higher
		 *	-- value, like 1024 is recommended.
		 * }
		 * 
		 * SafeContents ::= SEQUENCE OF SafeBag
		 * 
		 * SafeBag ::= SEQUENCE {
		 *	bagId BAG-TYPE.&id ({PKCS12BagSet}),
		 *	bagValue [0] EXPLICIT BAG-TYPE.&Type({PKCS12BagSet}{@bagId}),
		 *	bagAttributes SET OF PKCS12Attribute OPTIONAL
		 * }
		 */
		public PKCS12 (byte[] data, string password)
			: this ()
		{
			Password = password;
			Decode (data);
		}

		public PKCS12 (byte[] data, byte[] password)
			: this ()
		{
			_password = password;
			Decode (data);
		}

		private void Decode (byte[] data)
		{
			ASN1 pfx = new ASN1 (data);
			if (pfx.Tag != 0x30)
				throw new ArgumentException ("invalid data");
			
			ASN1 version = pfx [0];
			if (version.Tag != 0x02)
				throw new ArgumentException ("invalid PFX version");
			//_version = version.Value [0];

			PKCS7.ContentInfo authSafe = new PKCS7.ContentInfo (pfx [1]);
			if (authSafe.ContentType != PKCS7.Oid.data)
				throw new ArgumentException ("invalid authenticated safe");

			// now that we know it's a PKCS#12 file, check the (optional) MAC
			// before decoding anything else in the file
			if (pfx.Count > 2) {
				ASN1 macData = pfx [2];
				if (macData.Tag != 0x30)
					throw new ArgumentException ("invalid MAC");
				
				ASN1 mac = macData [0];
				if (mac.Tag != 0x30)
					throw new ArgumentException ("invalid MAC");
				ASN1 macAlgorithm = mac [0];
				string macOid = ASN1Convert.ToOid (macAlgorithm [0]);
				if (macOid != "1.3.14.3.2.26")
					throw new ArgumentException ("unsupported HMAC");
				byte[] macValue = mac [1].Value;

				ASN1 macSalt = macData [1];
				if (macSalt.Tag != 0x04)
					throw new ArgumentException ("missing MAC salt");

				_iterations = 1; // default value
				if (macData.Count > 2) {
					ASN1 iters = macData [2];
					if (iters.Tag != 0x02)
						throw new ArgumentException ("invalid MAC iteration");
					_iterations = ASN1Convert.ToInt32 (iters);
				}

				byte[] authSafeData = authSafe.Content [0].Value;
				byte[] calculatedMac = MAC (_password, macSalt.Value, _iterations, authSafeData);
				if (!Compare (macValue, calculatedMac)) {
					byte[] nullPassword = {0, 0};
					calculatedMac = MAC(nullPassword, macSalt.Value, _iterations, authSafeData);
					if (!Compare (macValue, calculatedMac))
						throw new CryptographicException ("Invalid MAC - file may have been tampe red!");
					_password = nullPassword;
				}
			}

			// we now returns to our original presentation - PFX
			ASN1 authenticatedSafe = new ASN1 (authSafe.Content [0].Value);
			for (int i=0; i < authenticatedSafe.Count; i++) {
				PKCS7.ContentInfo ci = new PKCS7.ContentInfo (authenticatedSafe [i]);
				switch (ci.ContentType) {
					case PKCS7.Oid.data:
						// unencrypted (by PKCS#12)
						ASN1 safeContents = new ASN1 (ci.Content [0].Value);
						for (int j=0; j < safeContents.Count; j++) {
							ASN1 safeBag = safeContents [j];
							ReadSafeBag (safeBag);
						}
						break;
					case PKCS7.Oid.encryptedData:
						// password encrypted
						PKCS7.EncryptedData ed = new PKCS7.EncryptedData (ci.Content [0]);
						ASN1 decrypted = new ASN1 (Decrypt (ed));
						for (int j=0; j < decrypted.Count; j++) {
							ASN1 safeBag = decrypted [j];
							ReadSafeBag (safeBag);
						}
						break;
					case PKCS7.Oid.envelopedData:
						// public key encrypted
						throw new NotImplementedException ("public key encrypted");
					default:
						throw new ArgumentException ("unknown authenticatedSafe");
				}
			}
		}

		~PKCS12 () 
		{
			if (_password != null) {
				Array.Clear (_password, 0, _password.Length);
			}
			_password = null;
		}

		// properties

		public string Password {
			set {
				// Clear old password.
				if (_password != null)
					Array.Clear (_password, 0, _password.Length);
				_password = null;
				if (value != null) {
					if (value.Length > 0) {
						int size = value.Length;
						int nul = 0;
						if (size < MaximumPasswordLength) {
							// if not present, add space for a NULL (0x00) character
							if (value[size - 1] != 0x00)
								nul = 1;
						} else {
							size = MaximumPasswordLength;
						}
						_password = new byte[(size + nul) << 1]; // double for unicode
						Encoding.BigEndianUnicode.GetBytes (value, 0, size, _password, 0);
					} else {
						// double-byte (Unicode) NULL (0x00) - see bug #79617
						_password = new byte[2];
					}
				}
			}
		}

		public int IterationCount {
			get { return _iterations; }
			set { _iterations = value; }
		}

		public ArrayList Keys {
			get {
				if (_keyBagsChanged) {
					_keyBags.Clear ();
					foreach (SafeBag sb in _safeBags) {
						if (sb.BagOID.Equals (keyBag)) {
							ASN1 safeBag = sb.ASN1;
							ASN1 bagValue = safeBag [1];
							PKCS8.PrivateKeyInfo pki = new PKCS8.PrivateKeyInfo (bagValue.Value);
							byte[] privateKey = pki.PrivateKey;
							switch (privateKey [0]) {
							case 0x02:
								DSAParameters p = new DSAParameters (); // FIXME
								_keyBags.Add (PKCS8.PrivateKeyInfo.DecodeDSA (privateKey, p));
								break;
							case 0x30:
								_keyBags.Add (PKCS8.PrivateKeyInfo.DecodeRSA (privateKey));
								break;
							default:
								break;
							}
							Array.Clear (privateKey, 0, privateKey.Length);

						} else if (sb.BagOID.Equals (pkcs8ShroudedKeyBag)) {
							ASN1 safeBag = sb.ASN1;
							ASN1 bagValue = safeBag [1];
							PKCS8.EncryptedPrivateKeyInfo epki = new PKCS8.EncryptedPrivateKeyInfo (bagValue.Value);
							byte[] decrypted = Decrypt (epki.Algorithm, epki.Salt, epki.IterationCount, epki.EncryptedData);
							PKCS8.PrivateKeyInfo pki = new PKCS8.PrivateKeyInfo (decrypted);
							byte[] privateKey = pki.PrivateKey;
							switch (privateKey [0]) {
							case 0x02:
								DSAParameters p = new DSAParameters (); // FIXME
								_keyBags.Add (PKCS8.PrivateKeyInfo.DecodeDSA (privateKey, p));
								break;
							case 0x30:
								_keyBags.Add (PKCS8.PrivateKeyInfo.DecodeRSA (privateKey));
								break;
							default:
								break;
							}
							Array.Clear (privateKey, 0, privateKey.Length);
							Array.Clear (decrypted, 0, decrypted.Length);
						}
					}
					_keyBagsChanged = false;
				}
				return ArrayList.ReadOnly(_keyBags);
			}
		}

		public ArrayList Secrets {
			get {
				if (_secretBagsChanged) {
					_secretBags.Clear ();
					foreach (SafeBag sb in _safeBags) {
						if (sb.BagOID.Equals (secretBag)) {
							ASN1 safeBag = sb.ASN1;
							ASN1 bagValue = safeBag [1];
							byte[] secret = bagValue.Value;
							_secretBags.Add(secret);
						}
					}
					_secretBagsChanged = false;
				}
				return ArrayList.ReadOnly(_secretBags);
			}
		}

		public X509CertificateCollection Certificates {
			get {
				if (_certsChanged) {
					_certs.Clear ();
					foreach (SafeBag sb in _safeBags) {
						if (sb.BagOID.Equals (certBag)) {
							ASN1 safeBag = sb.ASN1;
							ASN1 bagValue = safeBag [1];
							PKCS7.ContentInfo cert = new PKCS7.ContentInfo (bagValue.Value);
							_certs.Add (new X509Certificate (cert.Content [0].Value));
						}
					}
					_certsChanged = false;
				}
				return _certs;
			}
		}

		internal RandomNumberGenerator RNG {
			get {
				if (_rng == null)
					_rng = RandomNumberGenerator.Create ();
				return _rng;
			}
		}

		// private methods

		private bool Compare (byte[] expected, byte[] actual) 
		{
			bool compare = false;
			if (expected.Length == actual.Length) {
				for (int i=0; i < expected.Length; i++) {
					if (expected [i] != actual [i])
						return false;
				}
				compare = true;
			}
			return compare;
		}

		private SymmetricAlgorithm GetSymmetricAlgorithm (string algorithmOid, byte[] salt, int iterationCount)
		{
			string algorithm = null;
			int keyLength = 8;	// 64 bits (default)
			int ivLength = 8;	// 64 bits (default)

			PKCS12.DeriveBytes pd = new PKCS12.DeriveBytes ();
			pd.Password = _password; 
			pd.Salt = salt;
			pd.IterationCount = iterationCount;

			switch (algorithmOid) {
				case PKCS5.pbeWithMD2AndDESCBC:			// no unit test available
					pd.HashName = "MD2";
					algorithm = "DES";
					break;
				case PKCS5.pbeWithMD5AndDESCBC:			// no unit test available
					pd.HashName = "MD5";
					algorithm = "DES";
					break;
				case PKCS5.pbeWithMD2AndRC2CBC:			// no unit test available
					// TODO - RC2-CBC-Parameter (PKCS5)
					// if missing default to 32 bits !!!
					pd.HashName = "MD2";
					algorithm = "RC2";
					keyLength = 4;		// default
					break;
				case PKCS5.pbeWithMD5AndRC2CBC:			// no unit test available
					// TODO - RC2-CBC-Parameter (PKCS5)
					// if missing default to 32 bits !!!
					pd.HashName = "MD5";
					algorithm = "RC2";
					keyLength = 4;		// default
					break;
				case PKCS5.pbeWithSHA1AndDESCBC: 		// no unit test available
					pd.HashName = "SHA1";
					algorithm = "DES";
					break;
				case PKCS5.pbeWithSHA1AndRC2CBC:		// no unit test available
					// TODO - RC2-CBC-Parameter (PKCS5)
					// if missing default to 32 bits !!!
					pd.HashName = "SHA1";
					algorithm = "RC2";
					keyLength = 4;		// default
					break;
				case PKCS12.pbeWithSHAAnd128BitRC4: 		// no unit test available
					pd.HashName = "SHA1";
					algorithm = "RC4";
					keyLength = 16;
					ivLength = 0;		// N/A
					break;
				case PKCS12.pbeWithSHAAnd40BitRC4: 		// no unit test available
					pd.HashName = "SHA1";
					algorithm = "RC4";
					keyLength = 5;
					ivLength = 0;		// N/A
					break;
				case PKCS12.pbeWithSHAAnd3KeyTripleDESCBC: 
					pd.HashName = "SHA1";
					algorithm = "TripleDES";
					keyLength = 24;
					break;
				case PKCS12.pbeWithSHAAnd2KeyTripleDESCBC:	// no unit test available
					pd.HashName = "SHA1";
					algorithm = "TripleDES";
					keyLength = 16;
					break;
				case PKCS12.pbeWithSHAAnd128BitRC2CBC: 		// no unit test available
					pd.HashName = "SHA1";
					algorithm = "RC2";
					keyLength = 16;
					break;
				case PKCS12.pbeWithSHAAnd40BitRC2CBC: 
					pd.HashName = "SHA1";
					algorithm = "RC2";
					keyLength = 5;
					break;
				default:
					throw new NotSupportedException ("unknown oid " + algorithm);
			}

			SymmetricAlgorithm sa = null;
            sa = SymmetricAlgorithm.Create(algorithm);
            sa.Key = pd.DeriveKey (keyLength);
			// IV required only for block ciphers (not stream ciphers)
			if (ivLength > 0) {
				sa.IV = pd.DeriveIV (ivLength);
				sa.Mode = CipherMode.CBC;
			}
			return sa;
		}

		public byte[] Decrypt (string algorithmOid, byte[] salt, int iterationCount, byte[] encryptedData) 
		{
			SymmetricAlgorithm sa = null;
			byte[] result = null;
			try {
				sa = GetSymmetricAlgorithm (algorithmOid, salt, iterationCount);
				ICryptoTransform ct = sa.CreateDecryptor ();
				result = ct.TransformFinalBlock (encryptedData, 0, encryptedData.Length);
			}
			finally {
				if (sa != null)
					sa.Clear ();
			}
			return result;
		}

		public byte[] Decrypt (PKCS7.EncryptedData ed)
		{
			return Decrypt (ed.EncryptionAlgorithm.ContentType, 
				ed.EncryptionAlgorithm.Content [0].Value, 
				ASN1Convert.ToInt32 (ed.EncryptionAlgorithm.Content [1]),
				ed.EncryptedContent);
		}

		public byte[] Encrypt (string algorithmOid, byte[] salt, int iterationCount, byte[] data) 
		{
			byte[] result = null;
			using (SymmetricAlgorithm sa = GetSymmetricAlgorithm (algorithmOid, salt, iterationCount)) {
				ICryptoTransform ct = sa.CreateEncryptor ();
				result = ct.TransformFinalBlock (data, 0, data.Length);
			}
			return result;
		}

		private DSAParameters GetExistingParameters (out bool found)
		{
			foreach (X509Certificate cert in Certificates) {
				// FIXME: that won't work if parts of the parameters are missing
				if (cert.KeyAlgorithmParameters != null) {
					DSA dsa = cert.DSA;
					if (dsa != null) {
						found = true;
						return dsa.ExportParameters (false);
					}
				}
			}
			found = false;
			return new DSAParameters ();
		}

		private void AddPrivateKey (PKCS8.PrivateKeyInfo pki) 
		{
			byte[] privateKey = pki.PrivateKey;
			switch (privateKey [0]) {
				case 0x02:
					bool found;
					DSAParameters p = GetExistingParameters (out found);
					if (found) {
						_keyBags.Add (PKCS8.PrivateKeyInfo.DecodeDSA (privateKey, p));
					}
					break;
				case 0x30:
					_keyBags.Add (PKCS8.PrivateKeyInfo.DecodeRSA (privateKey));
					break;
				default:
					Array.Clear (privateKey, 0, privateKey.Length);
					throw new CryptographicException ("Unknown private key format");
			}
			Array.Clear (privateKey, 0, privateKey.Length);
		}

		private void ReadSafeBag (ASN1 safeBag) 
		{
			if (safeBag.Tag != 0x30)
				throw new ArgumentException ("invalid safeBag");

			ASN1 bagId = safeBag [0];
			if (bagId.Tag != 0x06)
				throw new ArgumentException ("invalid safeBag id");

			ASN1 bagValue = safeBag [1];
			string oid = ASN1Convert.ToOid (bagId);
			switch (oid) {
				case keyBag:
					// NEED UNIT TEST
					AddPrivateKey (new PKCS8.PrivateKeyInfo (bagValue.Value));
					break;
				case pkcs8ShroudedKeyBag:
					PKCS8.EncryptedPrivateKeyInfo epki = new PKCS8.EncryptedPrivateKeyInfo (bagValue.Value);
					byte[] decrypted = Decrypt (epki.Algorithm, epki.Salt, epki.IterationCount, epki.EncryptedData);
					AddPrivateKey (new PKCS8.PrivateKeyInfo (decrypted));
					Array.Clear (decrypted, 0, decrypted.Length);
					break;
				case certBag:
					PKCS7.ContentInfo cert = new PKCS7.ContentInfo (bagValue.Value);
					if (cert.ContentType != x509Certificate)
						throw new NotSupportedException ("unsupport certificate type");
					X509Certificate x509 = new X509Certificate (cert.Content [0].Value);
					_certs.Add (x509);
					break;
				case crlBag:
					// TODO
					break;
				case secretBag: 
					byte[] secret = bagValue.Value;
					_secretBags.Add(secret);
					break;
				case safeContentsBag:
					// TODO - ? recurse ?
					break;
				default:
					throw new ArgumentException ("unknown safeBag oid");
			}

			if (safeBag.Count > 2) {
				ASN1 bagAttributes = safeBag [2];
				if (bagAttributes.Tag != 0x31)
					throw new ArgumentException ("invalid safeBag attributes id");

				for (int i = 0; i < bagAttributes.Count; i++) {
					ASN1 pkcs12Attribute = bagAttributes[i];
						
					if (pkcs12Attribute.Tag != 0x30)
						throw new ArgumentException ("invalid PKCS12 attributes id");

					ASN1 attrId = pkcs12Attribute [0];
					if (attrId.Tag != 0x06)
						throw new ArgumentException ("invalid attribute id");
						
					string attrOid = ASN1Convert.ToOid (attrId);

					ASN1 attrValues = pkcs12Attribute[1];
					for (int j = 0; j < attrValues.Count; j++) {
						ASN1 attrValue = attrValues[j];

						switch (attrOid) {
						case PKCS9.friendlyName:
							if (attrValue.Tag != 0x1e)
								throw new ArgumentException ("invalid attribute value id");
							break;
						case PKCS9.localKeyId:
							if (attrValue.Tag != 0x04)
								throw new ArgumentException ("invalid attribute value id");
							break;
						default:
							// Unknown OID -- don't check Tag
							break;
						}
					}
				}
			}

			_safeBags.Add (new SafeBag(oid, safeBag));
		}

		private ASN1 Pkcs8ShroudedKeyBagSafeBag (AsymmetricAlgorithm aa, IDictionary attributes) 
		{
			PKCS8.PrivateKeyInfo pki = new PKCS8.PrivateKeyInfo ();
			if (aa is RSA) {
				pki.Algorithm = "1.2.840.113549.1.1.1";
				pki.PrivateKey = PKCS8.PrivateKeyInfo.Encode ((RSA)aa);
			}
			else if (aa is DSA) {
				pki.Algorithm = null;
				pki.PrivateKey = PKCS8.PrivateKeyInfo.Encode ((DSA)aa);
			}
			else
				throw new CryptographicException ("Unknown asymmetric algorithm {0}", aa.ToString ());

			PKCS8.EncryptedPrivateKeyInfo epki = new PKCS8.EncryptedPrivateKeyInfo ();
			epki.Algorithm = pbeWithSHAAnd3KeyTripleDESCBC;
			epki.IterationCount = _iterations;
			epki.EncryptedData = Encrypt (pbeWithSHAAnd3KeyTripleDESCBC, epki.Salt, _iterations, pki.GetBytes ());

			ASN1 safeBag = new ASN1 (0x30);
			safeBag.Add (ASN1Convert.FromOid (pkcs8ShroudedKeyBag));
			ASN1 bagValue = new ASN1 (0xA0);
			bagValue.Add (new ASN1 (epki.GetBytes ()));
			safeBag.Add (bagValue);

			if (attributes != null) {
				ASN1 bagAttributes = new ASN1 (0x31);
				IDictionaryEnumerator de = attributes.GetEnumerator ();

				while (de.MoveNext ()) {
					string oid = (string)de.Key;
					switch (oid) {
					case PKCS9.friendlyName:
						ArrayList names = (ArrayList)de.Value;
						if (names.Count > 0) {
							ASN1 pkcs12Attribute = new ASN1 (0x30);
							pkcs12Attribute.Add (ASN1Convert.FromOid (PKCS9.friendlyName));
							ASN1 attrValues = new ASN1 (0x31);
							foreach (byte[] name in names) {
								ASN1 attrValue = new ASN1 (0x1e);
								attrValue.Value = name;
								attrValues.Add (attrValue);
							}
							pkcs12Attribute.Add (attrValues);
							bagAttributes.Add (pkcs12Attribute);
						}
						break;
					case PKCS9.localKeyId:
						ArrayList keys = (ArrayList)de.Value;
						if (keys.Count > 0) {
							ASN1 pkcs12Attribute = new ASN1 (0x30);
							pkcs12Attribute.Add (ASN1Convert.FromOid (PKCS9.localKeyId));
							ASN1 attrValues = new ASN1 (0x31);
							foreach (byte[] key in keys) {
								ASN1 attrValue = new ASN1 (0x04);
								attrValue.Value = key;
								attrValues.Add (attrValue);
							}
							pkcs12Attribute.Add (attrValues);
							bagAttributes.Add (pkcs12Attribute);
						}
						break;
					default:
						break;
					}
				}

				if (bagAttributes.Count > 0) {
					safeBag.Add (bagAttributes);
				}
			}

			return safeBag;
		}

		private ASN1 KeyBagSafeBag (AsymmetricAlgorithm aa, IDictionary attributes) 
		{
			PKCS8.PrivateKeyInfo pki = new PKCS8.PrivateKeyInfo ();
			if (aa is RSA) {
				pki.Algorithm = "1.2.840.113549.1.1.1";
				pki.PrivateKey = PKCS8.PrivateKeyInfo.Encode ((RSA)aa);
			}
			else if (aa is DSA) {
				pki.Algorithm = null;
				pki.PrivateKey = PKCS8.PrivateKeyInfo.Encode ((DSA)aa);
			}
			else
				throw new CryptographicException ("Unknown asymmetric algorithm {0}", aa.ToString ());

			ASN1 safeBag = new ASN1 (0x30);
			safeBag.Add (ASN1Convert.FromOid (keyBag));
			ASN1 bagValue = new ASN1 (0xA0);
			bagValue.Add (new ASN1 (pki.GetBytes ()));
			safeBag.Add (bagValue);

			if (attributes != null) {
				ASN1 bagAttributes = new ASN1 (0x31);
				IDictionaryEnumerator de = attributes.GetEnumerator ();

				while (de.MoveNext ()) {
					string oid = (string)de.Key;
					switch (oid) {
					case PKCS9.friendlyName:
						ArrayList names = (ArrayList)de.Value;
						if (names.Count > 0) {
							ASN1 pkcs12Attribute = new ASN1 (0x30);
							pkcs12Attribute.Add (ASN1Convert.FromOid (PKCS9.friendlyName));
							ASN1 attrValues = new ASN1 (0x31);
							foreach (byte[] name in names) {
								ASN1 attrValue = new ASN1 (0x1e);
								attrValue.Value = name;
								attrValues.Add (attrValue);
							}
							pkcs12Attribute.Add (attrValues);
							bagAttributes.Add (pkcs12Attribute);
						}
						break;
					case PKCS9.localKeyId:
						ArrayList keys = (ArrayList)de.Value;
						if (keys.Count > 0) {
							ASN1 pkcs12Attribute = new ASN1 (0x30);
							pkcs12Attribute.Add (ASN1Convert.FromOid (PKCS9.localKeyId));
							ASN1 attrValues = new ASN1 (0x31);
							foreach (byte[] key in keys) {
								ASN1 attrValue = new ASN1 (0x04);
								attrValue.Value = key;
								attrValues.Add (attrValue);
							}
							pkcs12Attribute.Add (attrValues);
							bagAttributes.Add (pkcs12Attribute);
						}
						break;
					default:
						break;
					}
				}

				if (bagAttributes.Count > 0) {
					safeBag.Add (bagAttributes);
				}
			}

			return safeBag;
		}

		private ASN1 SecretBagSafeBag (byte[] secret, IDictionary attributes) 
		{
			ASN1 safeBag = new ASN1 (0x30);
			safeBag.Add (ASN1Convert.FromOid (secretBag));
			ASN1 bagValue = new ASN1 (0x80, secret);
			safeBag.Add (bagValue);

			if (attributes != null) {
				ASN1 bagAttributes = new ASN1 (0x31);
				IDictionaryEnumerator de = attributes.GetEnumerator ();

				while (de.MoveNext ()) {
					string oid = (string)de.Key;
					switch (oid) {
					case PKCS9.friendlyName:
						ArrayList names = (ArrayList)de.Value;
						if (names.Count > 0) {
							ASN1 pkcs12Attribute = new ASN1 (0x30);
							pkcs12Attribute.Add (ASN1Convert.FromOid (PKCS9.friendlyName));
							ASN1 attrValues = new ASN1 (0x31);
							foreach (byte[] name in names) {
								ASN1 attrValue = new ASN1 (0x1e);
								attrValue.Value = name;
								attrValues.Add (attrValue);
							}
							pkcs12Attribute.Add (attrValues);
							bagAttributes.Add (pkcs12Attribute);
						}
						break;
					case PKCS9.localKeyId:
						ArrayList keys = (ArrayList)de.Value;
						if (keys.Count > 0) {
							ASN1 pkcs12Attribute = new ASN1 (0x30);
							pkcs12Attribute.Add (ASN1Convert.FromOid (PKCS9.localKeyId));
							ASN1 attrValues = new ASN1 (0x31);
							foreach (byte[] key in keys) {
								ASN1 attrValue = new ASN1 (0x04);
								attrValue.Value = key;
								attrValues.Add (attrValue);
							}
							pkcs12Attribute.Add (attrValues);
							bagAttributes.Add (pkcs12Attribute);
						}
						break;
					default:
						break;
					}
				}

				if (bagAttributes.Count > 0) {
					safeBag.Add (bagAttributes);
				}
			}

			return safeBag;
		}

		private ASN1 CertificateSafeBag (X509Certificate x509, IDictionary attributes) 
		{
			ASN1 encapsulatedCertificate = new ASN1 (0x04, x509.RawData);

			PKCS7.ContentInfo ci = new PKCS7.ContentInfo ();
			ci.ContentType = x509Certificate;
			ci.Content.Add (encapsulatedCertificate);

			ASN1 bagValue = new ASN1 (0xA0);
			bagValue.Add (ci.ASN1);

			ASN1 safeBag = new ASN1 (0x30);
			safeBag.Add (ASN1Convert.FromOid (certBag));
			safeBag.Add (bagValue);

			if (attributes != null) {
				ASN1 bagAttributes = new ASN1 (0x31);
				IDictionaryEnumerator de = attributes.GetEnumerator ();

				while (de.MoveNext ()) {
					string oid = (string)de.Key;
					switch (oid) {
					case PKCS9.friendlyName:
						ArrayList names = (ArrayList)de.Value;
						if (names.Count > 0) {
							ASN1 pkcs12Attribute = new ASN1 (0x30);
							pkcs12Attribute.Add (ASN1Convert.FromOid (PKCS9.friendlyName));
							ASN1 attrValues = new ASN1 (0x31);
							foreach (byte[] name in names) {
								ASN1 attrValue = new ASN1 (0x1e);
								attrValue.Value = name;
								attrValues.Add (attrValue);
							}
							pkcs12Attribute.Add (attrValues);
							bagAttributes.Add (pkcs12Attribute);
						}
						break;
					case PKCS9.localKeyId:
						ArrayList keys = (ArrayList)de.Value;
						if (keys.Count > 0) {
							ASN1 pkcs12Attribute = new ASN1 (0x30);
							pkcs12Attribute.Add (ASN1Convert.FromOid (PKCS9.localKeyId));
							ASN1 attrValues = new ASN1 (0x31);
							foreach (byte[] key in keys) {
								ASN1 attrValue = new ASN1 (0x04);
								attrValue.Value = key;
								attrValues.Add (attrValue);
							}
							pkcs12Attribute.Add (attrValues);
							bagAttributes.Add (pkcs12Attribute);
						}
						break;
					default:
						break;
					}
				}

				if (bagAttributes.Count > 0) {
					safeBag.Add (bagAttributes);
				}
			}

			return safeBag;
		}

		private byte[] MAC (byte[] password, byte[] salt, int iterations, byte[] data) 
		{
			PKCS12.DeriveBytes pd = new PKCS12.DeriveBytes ();
			pd.HashName = "SHA1";
			pd.Password = password;
			pd.Salt = salt;
			pd.IterationCount = iterations;

			HMACSHA1 hmac = (HMACSHA1) HMACSHA1.Create ();
			hmac.Key = pd.DeriveMAC (20);
			return hmac.ComputeHash (data, 0, data.Length);
		}

		/*
		 * SafeContents ::= SEQUENCE OF SafeBag
		 * 
		 * SafeBag ::= SEQUENCE {
		 *	bagId BAG-TYPE.&id ({PKCS12BagSet}),
		 *	bagValue [0] EXPLICIT BAG-TYPE.&Type({PKCS12BagSet}{@bagId}),
		 *	bagAttributes SET OF PKCS12Attribute OPTIONAL
		 * }
		 */
		public byte[] GetBytes () 
		{
			// TODO (incomplete)
			ASN1 safeBagSequence = new ASN1 (0x30);

			// Sync Safe Bag list since X509CertificateCollection may be updated
			ArrayList scs = new ArrayList ();
			foreach (SafeBag sb in _safeBags) {
				if (sb.BagOID.Equals (certBag)) {
					ASN1 safeBag = sb.ASN1;
					ASN1 bagValue = safeBag [1];
					PKCS7.ContentInfo cert = new PKCS7.ContentInfo (bagValue.Value);
					scs.Add (new X509Certificate (cert.Content [0].Value));
				}
			}

			ArrayList addcerts = new ArrayList ();
			ArrayList removecerts = new ArrayList ();

			foreach (X509Certificate c in Certificates) {
				bool found = false;

				foreach (X509Certificate lc in scs) {
					if (Compare (c.RawData, lc.RawData)) {
						found = true;
					}
				}

				if (!found) {
					addcerts.Add (c);
				}
			}
			foreach (X509Certificate c in scs) {
				bool found = false;

				foreach (X509Certificate lc in Certificates) {
					if (Compare (c.RawData, lc.RawData)) {
						found = true;
					}
				}

				if (!found) {
					removecerts.Add (c);
				}
			}

			foreach (X509Certificate c in removecerts) {
				RemoveCertificate (c);
			}

			foreach (X509Certificate c in addcerts) {
				AddCertificate (c);
			}
			// Sync done

			if (_safeBags.Count > 0) {
				ASN1 certsSafeBag = new ASN1 (0x30);
				foreach (SafeBag sb in _safeBags) {
					if (sb.BagOID.Equals (certBag)) {
						certsSafeBag.Add (sb.ASN1);
					}
				}

				if (certsSafeBag.Count > 0) {
					PKCS7.ContentInfo contentInfo = EncryptedContentInfo (certsSafeBag, pbeWithSHAAnd3KeyTripleDESCBC);
					safeBagSequence.Add (contentInfo.ASN1);
				}
			}

			if (_safeBags.Count > 0) {
				ASN1 safeContents = new ASN1 (0x30);
				foreach (SafeBag sb in _safeBags) {
					if (sb.BagOID.Equals (keyBag) ||
					    sb.BagOID.Equals (pkcs8ShroudedKeyBag)) {
						safeContents.Add (sb.ASN1);
					}
				}
				if (safeContents.Count > 0) {
					ASN1 content = new ASN1 (0xA0);
					content.Add (new ASN1 (0x04, safeContents.GetBytes ()));
				
					PKCS7.ContentInfo keyBag = new PKCS7.ContentInfo (PKCS7.Oid.data);
					keyBag.Content = content;
					safeBagSequence.Add (keyBag.ASN1);
				}
			}

			// Doing SecretBags separately in case we want to change their encryption independently.
			if (_safeBags.Count > 0) {
				ASN1 secretsSafeBag = new ASN1 (0x30);
				foreach (SafeBag sb in _safeBags) {
					if (sb.BagOID.Equals (secretBag)) {
						secretsSafeBag.Add (sb.ASN1);
					}
				}

				if (secretsSafeBag.Count > 0) {
					PKCS7.ContentInfo contentInfo = EncryptedContentInfo (secretsSafeBag, pbeWithSHAAnd3KeyTripleDESCBC);
					safeBagSequence.Add (contentInfo.ASN1);
				}
			}


			ASN1 encapsulates = new ASN1 (0x04, safeBagSequence.GetBytes ());
			ASN1 ci = new ASN1 (0xA0);
			ci.Add (encapsulates);
			PKCS7.ContentInfo authSafe = new PKCS7.ContentInfo (PKCS7.Oid.data);
			authSafe.Content = ci;
			
			ASN1 macData = new ASN1 (0x30);
			if (_password != null) {
				// only for password based encryption
				byte[] salt = new byte [20];
				RNG.GetBytes (salt);
				byte[] macValue = MAC (_password, salt, _iterations, authSafe.Content [0].Value);
				ASN1 oidSeq = new ASN1 (0x30);
				oidSeq.Add (ASN1Convert.FromOid ("1.3.14.3.2.26"));	// SHA1
				oidSeq.Add (new ASN1 (0x05));

				ASN1 mac = new ASN1 (0x30);
				mac.Add (oidSeq);
				mac.Add (new ASN1 (0x04, macValue));

				macData.Add (mac);
				macData.Add (new ASN1 (0x04, salt));
				macData.Add (ASN1Convert.FromInt32 (_iterations));
			}
			
			ASN1 version = new ASN1 (0x02, new byte [1] { 0x03 });
			
			ASN1 pfx = new ASN1 (0x30);
			pfx.Add (version);
			pfx.Add (authSafe.ASN1);
			if (macData.Count > 0) {
				// only for password based encryption
				pfx.Add (macData);
			}

			return pfx.GetBytes ();
		}

		// Creates an encrypted PKCS#7 ContentInfo with safeBags as its SafeContents.  Used in GetBytes(), above.
		private PKCS7.ContentInfo EncryptedContentInfo(ASN1 safeBags, string algorithmOid)
		{
			byte[] salt = new byte [8];
			RNG.GetBytes (salt);

			ASN1 seqParams = new ASN1 (0x30);
			seqParams.Add (new ASN1 (0x04, salt));
			seqParams.Add (ASN1Convert.FromInt32 (_iterations));

			ASN1 seqPbe = new ASN1 (0x30);
			seqPbe.Add (ASN1Convert.FromOid (algorithmOid));
			seqPbe.Add (seqParams);

			byte[] encrypted = Encrypt (algorithmOid, salt, _iterations, safeBags.GetBytes ());
			ASN1 encryptedContent = new ASN1 (0x80, encrypted);

			ASN1 seq = new ASN1 (0x30);
			seq.Add (ASN1Convert.FromOid (PKCS7.Oid.data));
			seq.Add (seqPbe);
			seq.Add (encryptedContent);

			ASN1 version = new ASN1 (0x02, new byte [1] { 0x00 });
			ASN1 encData = new ASN1 (0x30);
			encData.Add (version);
			encData.Add (seq);

			ASN1 finalContent = new ASN1 (0xA0);
			finalContent.Add (encData);

			PKCS7.ContentInfo bag = new PKCS7.ContentInfo (PKCS7.Oid.encryptedData);
			bag.Content = finalContent;
			return bag;
		}

		public void AddCertificate (X509Certificate cert)
		{
			AddCertificate (cert, null);
		}

		public void AddCertificate (X509Certificate cert, IDictionary attributes)
		{
			bool found = false;

			for (int i = 0; !found && i < _safeBags.Count; i++) {
				SafeBag sb = (SafeBag)_safeBags [i];

				if (sb.BagOID.Equals (certBag)) {
					ASN1 safeBag = sb.ASN1;
					ASN1 bagValue = safeBag [1];
					PKCS7.ContentInfo crt = new PKCS7.ContentInfo (bagValue.Value);
					X509Certificate c = new X509Certificate (crt.Content [0].Value);
					if (Compare (cert.RawData, c.RawData)) {
						found = true;
					}
				}
			}

			if (!found) {
				_safeBags.Add (new SafeBag (certBag, CertificateSafeBag (cert, attributes)));
				_certsChanged = true;
			}
		}

		public void RemoveCertificate (X509Certificate cert)
		{
			RemoveCertificate (cert, null);
		}

		public void RemoveCertificate (X509Certificate cert, IDictionary attrs)
		{
			int certIndex = -1;

			for (int i = 0; certIndex == -1 && i < _safeBags.Count; i++) {
				SafeBag sb = (SafeBag)_safeBags [i];

				if (sb.BagOID.Equals (certBag)) {
					ASN1 safeBag = sb.ASN1;
					ASN1 bagValue = safeBag [1];
					PKCS7.ContentInfo crt = new PKCS7.ContentInfo (bagValue.Value);
					X509Certificate c = new X509Certificate (crt.Content [0].Value);
					if (Compare (cert.RawData, c.RawData)) {
						if (attrs != null) {
							if (safeBag.Count == 3) {
								ASN1 bagAttributes = safeBag [2];
								int bagAttributesFound = 0;
								for (int j = 0; j < bagAttributes.Count; j++) {
									ASN1 pkcs12Attribute = bagAttributes [j];
									ASN1 attrId = pkcs12Attribute [0];
									string ao = ASN1Convert.ToOid (attrId);
									ArrayList dattrValues = (ArrayList)attrs [ao];

									if (dattrValues != null) {
										ASN1 attrValues = pkcs12Attribute [1];

										if (dattrValues.Count == attrValues.Count) {
											int attrValuesFound = 0;
											for (int k = 0; k < attrValues.Count; k++) {
												ASN1 attrValue = attrValues [k];
												byte[] value = (byte[])dattrValues [k];
									
												if (Compare (value, attrValue.Value)) {
													attrValuesFound += 1;
												}
											}
											if (attrValuesFound == attrValues.Count) {
												bagAttributesFound += 1;
											}
										}
									}
								}
								if (bagAttributesFound == bagAttributes.Count) {
									certIndex = i;
								}
							}
						} else {
							certIndex = i;
						}
					}
				}
			}

			if (certIndex != -1) {
				_safeBags.RemoveAt (certIndex);
				_certsChanged = true;
			}
		}

		private bool CompareAsymmetricAlgorithm (AsymmetricAlgorithm a1, AsymmetricAlgorithm a2)
		{
			// fast path
			if (a1.KeySize != a2.KeySize)
				return false;
			// compare public keys - if they match we can assume the private match too
			return (a1.ToXmlString (false) == a2.ToXmlString (false));
		}

		public void AddPkcs8ShroudedKeyBag (AsymmetricAlgorithm aa)
		{
			AddPkcs8ShroudedKeyBag (aa, null);
		}

		public void AddPkcs8ShroudedKeyBag (AsymmetricAlgorithm aa, IDictionary attributes)
		{
			bool found = false;

			for (int i = 0; !found && i < _safeBags.Count; i++) {
				SafeBag sb = (SafeBag)_safeBags [i];

				if (sb.BagOID.Equals (pkcs8ShroudedKeyBag)) {
					ASN1 bagValue = sb.ASN1 [1];
					PKCS8.EncryptedPrivateKeyInfo epki = new PKCS8.EncryptedPrivateKeyInfo (bagValue.Value);
					byte[] decrypted = Decrypt (epki.Algorithm, epki.Salt, epki.IterationCount, epki.EncryptedData);
					PKCS8.PrivateKeyInfo pki = new PKCS8.PrivateKeyInfo (decrypted);
					byte[] privateKey = pki.PrivateKey;

					AsymmetricAlgorithm saa = null;
					switch (privateKey [0]) {
					case 0x02:
						DSAParameters p = new DSAParameters (); // FIXME
						saa = PKCS8.PrivateKeyInfo.DecodeDSA (privateKey, p);
						break;
					case 0x30:
						saa = PKCS8.PrivateKeyInfo.DecodeRSA (privateKey);
						break;
					default:
						Array.Clear (decrypted, 0, decrypted.Length);
						Array.Clear (privateKey, 0, privateKey.Length);
						throw new CryptographicException ("Unknown private key format");
					}

					Array.Clear (decrypted, 0, decrypted.Length);
					Array.Clear (privateKey, 0, privateKey.Length);

					if (CompareAsymmetricAlgorithm (aa , saa)) {
						found = true;
					}
				}
			}

			if (!found) {
				_safeBags.Add (new SafeBag (pkcs8ShroudedKeyBag, Pkcs8ShroudedKeyBagSafeBag (aa, attributes)));
				_keyBagsChanged = true;
			}
		}

		public void RemovePkcs8ShroudedKeyBag (AsymmetricAlgorithm aa)
		{
			int aaIndex = -1;

			for (int i = 0; aaIndex == -1 && i < _safeBags.Count; i++) {
				SafeBag sb = (SafeBag)_safeBags [i];

				if (sb.BagOID.Equals (pkcs8ShroudedKeyBag)) {
					ASN1 bagValue = sb.ASN1 [1];
					PKCS8.EncryptedPrivateKeyInfo epki = new PKCS8.EncryptedPrivateKeyInfo (bagValue.Value);
					byte[] decrypted = Decrypt (epki.Algorithm, epki.Salt, epki.IterationCount, epki.EncryptedData);
					PKCS8.PrivateKeyInfo pki = new PKCS8.PrivateKeyInfo (decrypted);
					byte[] privateKey = pki.PrivateKey;

					AsymmetricAlgorithm saa = null;
					switch (privateKey [0]) {
					case 0x02:
						DSAParameters p = new DSAParameters (); // FIXME
						saa = PKCS8.PrivateKeyInfo.DecodeDSA (privateKey, p);
						break;
					case 0x30:
						saa = PKCS8.PrivateKeyInfo.DecodeRSA (privateKey);
						break;
					default:
						Array.Clear (decrypted, 0, decrypted.Length);
						Array.Clear (privateKey, 0, privateKey.Length);
						throw new CryptographicException ("Unknown private key format");
					}

					Array.Clear (decrypted, 0, decrypted.Length);
					Array.Clear (privateKey, 0, privateKey.Length);

					if (CompareAsymmetricAlgorithm (aa, saa)) {
						aaIndex = i;
					}
				}
			}

			if (aaIndex != -1) {
				_safeBags.RemoveAt (aaIndex);
				_keyBagsChanged = true;
			}
		}

		public void AddKeyBag (AsymmetricAlgorithm aa)
		{
			AddKeyBag (aa, null);
		}

		public void AddKeyBag (AsymmetricAlgorithm aa, IDictionary attributes)
		{
			bool found = false;

			for (int i = 0; !found && i < _safeBags.Count; i++) {
				SafeBag sb = (SafeBag)_safeBags [i];

				if (sb.BagOID.Equals (keyBag)) {
					ASN1 bagValue = sb.ASN1 [1];
					PKCS8.PrivateKeyInfo pki = new PKCS8.PrivateKeyInfo (bagValue.Value);
					byte[] privateKey = pki.PrivateKey;

					AsymmetricAlgorithm saa = null;
					switch (privateKey [0]) {
					case 0x02:
						DSAParameters p = new DSAParameters (); // FIXME
						saa = PKCS8.PrivateKeyInfo.DecodeDSA (privateKey, p);
						break;
					case 0x30:
						saa = PKCS8.PrivateKeyInfo.DecodeRSA (privateKey);
						break;
					default:
						Array.Clear (privateKey, 0, privateKey.Length);
						throw new CryptographicException ("Unknown private key format");
					}

					Array.Clear (privateKey, 0, privateKey.Length);

					if (CompareAsymmetricAlgorithm (aa, saa)) {
						found = true;
					}
				}
			}

			if (!found) {
				_safeBags.Add (new SafeBag (keyBag, KeyBagSafeBag (aa, attributes)));
				_keyBagsChanged = true;
			}
		}

		public void RemoveKeyBag (AsymmetricAlgorithm aa)
		{
			int aaIndex = -1;

			for (int i = 0; aaIndex == -1 && i < _safeBags.Count; i++) {
				SafeBag sb = (SafeBag)_safeBags [i];

				if (sb.BagOID.Equals (keyBag)) {
					ASN1 bagValue = sb.ASN1 [1];
					PKCS8.PrivateKeyInfo pki = new PKCS8.PrivateKeyInfo (bagValue.Value);
					byte[] privateKey = pki.PrivateKey;

					AsymmetricAlgorithm saa = null;
					switch (privateKey [0]) {
					case 0x02:
						DSAParameters p = new DSAParameters (); // FIXME
						saa = PKCS8.PrivateKeyInfo.DecodeDSA (privateKey, p);
						break;
					case 0x30:
						saa = PKCS8.PrivateKeyInfo.DecodeRSA (privateKey);
						break;
					default:
						Array.Clear (privateKey, 0, privateKey.Length);
						throw new CryptographicException ("Unknown private key format");
					}

					Array.Clear (privateKey, 0, privateKey.Length);

					if (CompareAsymmetricAlgorithm (aa, saa)) {
						aaIndex = i;
					}
				}
			}

			if (aaIndex != -1) {
				_safeBags.RemoveAt (aaIndex);
				_keyBagsChanged = true;
			}
		}

		public void AddSecretBag (byte[] secret)
		{
			AddSecretBag (secret, null);
		}

		public void AddSecretBag (byte[] secret, IDictionary attributes)
		{
			bool found = false;

			for (int i = 0; !found && i < _safeBags.Count; i++) {
				SafeBag sb = (SafeBag)_safeBags [i];

				if (sb.BagOID.Equals (secretBag)) {
					ASN1 bagValue = sb.ASN1 [1];
					byte[] ssecret = bagValue.Value;

					if (Compare (secret, ssecret)) {
						found = true;
					}
				}
			}

			if (!found) {
				_safeBags.Add (new SafeBag (secretBag, SecretBagSafeBag (secret, attributes)));
				_secretBagsChanged = true;
			}
		}

		public void RemoveSecretBag (byte[] secret)
		{
			int sIndex = -1;

			for (int i = 0; sIndex == -1 && i < _safeBags.Count; i++) {
				SafeBag sb = (SafeBag)_safeBags [i];

				if (sb.BagOID.Equals (secretBag)) {
					ASN1 bagValue = sb.ASN1 [1];
					byte[] ssecret = bagValue.Value;

					if (Compare (secret, ssecret)) {
						sIndex = i;
					}
				}
			}

			if (sIndex != -1) {
				_safeBags.RemoveAt (sIndex);
				_secretBagsChanged = true;
			}
		}

		public AsymmetricAlgorithm GetAsymmetricAlgorithm (IDictionary attrs)
		{
			foreach (SafeBag sb in _safeBags) {
				if (sb.BagOID.Equals (keyBag) || sb.BagOID.Equals (pkcs8ShroudedKeyBag)) {
					ASN1 safeBag = sb.ASN1;

					if (safeBag.Count == 3) {
						ASN1 bagAttributes = safeBag [2];

						int bagAttributesFound = 0;
						for (int i = 0; i < bagAttributes.Count; i++) {
							ASN1 pkcs12Attribute = bagAttributes [i];
							ASN1 attrId = pkcs12Attribute [0];
							string ao = ASN1Convert.ToOid (attrId);
							ArrayList dattrValues = (ArrayList)attrs [ao];

							if (dattrValues != null) {
								ASN1 attrValues = pkcs12Attribute [1];

								if (dattrValues.Count == attrValues.Count) {
									int attrValuesFound = 0;
									for (int j = 0; j < attrValues.Count; j++) {
										ASN1 attrValue = attrValues [j];
										byte[] value = (byte[])dattrValues [j];
									
										if (Compare (value, attrValue.Value)) {
											attrValuesFound += 1;
										}
									}
									if (attrValuesFound == attrValues.Count) {
										bagAttributesFound += 1;
									}
								}
							}
						}
						if (bagAttributesFound == bagAttributes.Count) {
							ASN1 bagValue = safeBag [1];
							AsymmetricAlgorithm aa = null;
							if (sb.BagOID.Equals (keyBag)) {
								PKCS8.PrivateKeyInfo pki = new PKCS8.PrivateKeyInfo (bagValue.Value);
								byte[] privateKey = pki.PrivateKey;
								switch (privateKey [0]) {
								case 0x02:
									DSAParameters p = new DSAParameters (); // FIXME
									aa = PKCS8.PrivateKeyInfo.DecodeDSA (privateKey, p);
									break;
								case 0x30:
									aa = PKCS8.PrivateKeyInfo.DecodeRSA (privateKey);
									break;
								default:
									break;
								}
								Array.Clear (privateKey, 0, privateKey.Length);
							} else if (sb.BagOID.Equals (pkcs8ShroudedKeyBag)) {
								PKCS8.EncryptedPrivateKeyInfo epki = new PKCS8.EncryptedPrivateKeyInfo (bagValue.Value);
								byte[] decrypted = Decrypt (epki.Algorithm, epki.Salt, epki.IterationCount, epki.EncryptedData);
								PKCS8.PrivateKeyInfo pki = new PKCS8.PrivateKeyInfo (decrypted);
								byte[] privateKey = pki.PrivateKey;
								switch (privateKey [0]) {
								case 0x02:
									DSAParameters p = new DSAParameters (); // FIXME
									aa = PKCS8.PrivateKeyInfo.DecodeDSA (privateKey, p);
									break;
								case 0x30:
									aa = PKCS8.PrivateKeyInfo.DecodeRSA (privateKey);
									break;
								default:
									break;
								}
								Array.Clear (privateKey, 0, privateKey.Length);
								Array.Clear (decrypted, 0, decrypted.Length);
							}
							return aa;
						}
					}
				}
			}

			return null;
		}

		public byte[] GetSecret (IDictionary attrs)
		{
			foreach (SafeBag sb in _safeBags) {
				if (sb.BagOID.Equals (secretBag)) {
					ASN1 safeBag = sb.ASN1;

					if (safeBag.Count == 3) {
						ASN1 bagAttributes = safeBag [2];

						int bagAttributesFound = 0;
						for (int i = 0; i < bagAttributes.Count; i++) {
							ASN1 pkcs12Attribute = bagAttributes [i];
							ASN1 attrId = pkcs12Attribute [0];
							string ao = ASN1Convert.ToOid (attrId);
							ArrayList dattrValues = (ArrayList)attrs [ao];

							if (dattrValues != null) {
								ASN1 attrValues = pkcs12Attribute [1];

								if (dattrValues.Count == attrValues.Count) {
									int attrValuesFound = 0;
									for (int j = 0; j < attrValues.Count; j++) {
										ASN1 attrValue = attrValues [j];
										byte[] value = (byte[])dattrValues [j];
									
										if (Compare (value, attrValue.Value)) {
											attrValuesFound += 1;
										}
									}
									if (attrValuesFound == attrValues.Count) {
										bagAttributesFound += 1;
									}
								}
							}
						}
						if (bagAttributesFound == bagAttributes.Count) {
							ASN1 bagValue = safeBag [1];
							return bagValue.Value;
						}
					}
				}
			}

			return null;
		}

		public X509Certificate GetCertificate (IDictionary attrs)
		{
			foreach (SafeBag sb in _safeBags) {
				if (sb.BagOID.Equals (certBag)) {
					ASN1 safeBag = sb.ASN1;

					if (safeBag.Count == 3) {
						ASN1 bagAttributes = safeBag [2];

						int bagAttributesFound = 0;
						for (int i = 0; i < bagAttributes.Count; i++) {
							ASN1 pkcs12Attribute = bagAttributes [i];
							ASN1 attrId = pkcs12Attribute [0];
							string ao = ASN1Convert.ToOid (attrId);
							ArrayList dattrValues = (ArrayList)attrs [ao];

							if (dattrValues != null) {
								ASN1 attrValues = pkcs12Attribute [1];
								
								if (dattrValues.Count == attrValues.Count) {
									int attrValuesFound = 0;
									for (int j = 0; j < attrValues.Count; j++) {
										ASN1 attrValue = attrValues [j];
										byte[] value = (byte[])dattrValues [j];
									
										if (Compare (value, attrValue.Value)) {
											attrValuesFound += 1;
										}
									}
									if (attrValuesFound == attrValues.Count) {
										bagAttributesFound += 1;
									}
								}
							}
						}
						if (bagAttributesFound == bagAttributes.Count) {
							ASN1 bagValue = safeBag [1];
							PKCS7.ContentInfo crt = new PKCS7.ContentInfo (bagValue.Value);
							return new X509Certificate (crt.Content [0].Value);
						}
					}
				}
			}

			return null;
		}

		public IDictionary GetAttributes (AsymmetricAlgorithm aa)
		{
			IDictionary result = new Hashtable ();

			foreach (SafeBag sb in _safeBags) {
				if (sb.BagOID.Equals (keyBag) || sb.BagOID.Equals (pkcs8ShroudedKeyBag)) {
					ASN1 safeBag = sb.ASN1;

					ASN1 bagValue = safeBag [1];
					AsymmetricAlgorithm saa = null;
					if (sb.BagOID.Equals (keyBag)) {
						PKCS8.PrivateKeyInfo pki = new PKCS8.PrivateKeyInfo (bagValue.Value);
						byte[] privateKey = pki.PrivateKey;
						switch (privateKey [0]) {
						case 0x02:
							DSAParameters p = new DSAParameters (); // FIXME
							saa = PKCS8.PrivateKeyInfo.DecodeDSA (privateKey, p);
							break;
						case 0x30:
							saa = PKCS8.PrivateKeyInfo.DecodeRSA (privateKey);
							break;
						default:
							break;
						}
						Array.Clear (privateKey, 0, privateKey.Length);
					} else if (sb.BagOID.Equals (pkcs8ShroudedKeyBag)) {
						PKCS8.EncryptedPrivateKeyInfo epki = new PKCS8.EncryptedPrivateKeyInfo (bagValue.Value);
						byte[] decrypted = Decrypt (epki.Algorithm, epki.Salt, epki.IterationCount, epki.EncryptedData);
						PKCS8.PrivateKeyInfo pki = new PKCS8.PrivateKeyInfo (decrypted);
						byte[] privateKey = pki.PrivateKey;
						switch (privateKey [0]) {
						case 0x02:
							DSAParameters p = new DSAParameters (); // FIXME
							saa = PKCS8.PrivateKeyInfo.DecodeDSA (privateKey, p);
							break;
						case 0x30:
							saa = PKCS8.PrivateKeyInfo.DecodeRSA (privateKey);
							break;
						default:
							break;
						}
						Array.Clear (privateKey, 0, privateKey.Length);
						Array.Clear (decrypted, 0, decrypted.Length);
					}

					if (saa != null && CompareAsymmetricAlgorithm (saa, aa)) {
						if (safeBag.Count == 3) {
							ASN1 bagAttributes = safeBag [2];
							
							for (int i = 0; i < bagAttributes.Count; i++) {
								ASN1 pkcs12Attribute = bagAttributes [i];
								ASN1 attrId = pkcs12Attribute [0];
								string aOid = ASN1Convert.ToOid (attrId);
								ArrayList aValues = new ArrayList ();

								ASN1 attrValues = pkcs12Attribute [1];
									
								for (int j = 0; j < attrValues.Count; j++) {
									ASN1 attrValue = attrValues [j];
									aValues.Add (attrValue.Value);
								}
								result.Add (aOid, aValues);
							}
						}
					}
				}
			}

			return result;
		}

		public IDictionary GetAttributes (X509Certificate cert)
		{
			IDictionary result = new Hashtable ();

			foreach (SafeBag sb in _safeBags) {
				if (sb.BagOID.Equals (certBag)) {
					ASN1 safeBag = sb.ASN1;
					ASN1 bagValue = safeBag [1];
					PKCS7.ContentInfo crt = new PKCS7.ContentInfo (bagValue.Value);
					X509Certificate xc = new X509Certificate (crt.Content [0].Value);

					if (Compare (cert.RawData, xc.RawData)) {
						if (safeBag.Count == 3) {
							ASN1 bagAttributes = safeBag [2];

							for (int i = 0; i < bagAttributes.Count; i++) {
								ASN1 pkcs12Attribute = bagAttributes [i];
								ASN1 attrId = pkcs12Attribute [0];

								string aOid = ASN1Convert.ToOid (attrId);
								ArrayList aValues = new ArrayList ();

								ASN1 attrValues = pkcs12Attribute [1];
									
								for (int j = 0; j < attrValues.Count; j++) {
									ASN1 attrValue = attrValues [j];
									aValues.Add (attrValue.Value);
								}
								result.Add (aOid, aValues);
							}
						}
					}
				}
			}

			return result;
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

		public object Clone ()
		{
			PKCS12 clone = null;
			if (_password != null) {
				clone = new PKCS12 (GetBytes (), Encoding.BigEndianUnicode.GetString (_password));
			} else {
				clone = new PKCS12 (GetBytes ());
			}
			clone.IterationCount = this.IterationCount;

			return clone;
		}

		// static

		public const int CryptoApiPasswordLimit = 32;
		
		static private int password_max_length = Int32.MaxValue;

		// static properties
		
		// MS CryptoAPI limits the password to a maximum of 31 characters
		// other implementations, like OpenSSL, have no such limitation.
		// Setting a maximum value will truncate the password length to 
		// ensure compatibility with MS's PFXImportCertStore API.
		static public int MaximumPasswordLength {
			get { return password_max_length; }
			set {
				if (value < CryptoApiPasswordLimit) {
					string msg = string.Format ("Maximum password length cannot be less than {0}.", CryptoApiPasswordLimit);
					throw new ArgumentOutOfRangeException (msg);
				}
				password_max_length = value;
			}
		}
	}
}
