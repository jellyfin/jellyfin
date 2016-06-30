//
// PKCS8.cs: PKCS #8 - Private-Key Information Syntax Standard
//	ftp://ftp.rsasecurity.com/pub/pkcs/doc/pkcs-8.doc
//
// Author:
//	Sebastien Pouliot <sebastien@xamarin.com>
//
// (C) 2003 Motus Technologies Inc. (http://www.motus.com)
// Copyright (C) 2004-2006 Novell Inc. (http://www.novell.com)
// Copyright 2013 Xamarin Inc. (http://www.xamarin.com)
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

using Mono.Security.X509;

namespace Mono.Security.Cryptography {

#if !INSIDE_CORLIB
	public 
#endif
	sealed class PKCS8 {

		public enum KeyInfo {
			PrivateKey,
			EncryptedPrivateKey,
			Unknown
		}

		private PKCS8 () 
		{
		}

		static public KeyInfo GetType (byte[] data) 
		{
			if (data == null)
				throw new ArgumentNullException ("data");

			KeyInfo ki = KeyInfo.Unknown;
			try {
				ASN1 top = new ASN1 (data);
				if ((top.Tag == 0x30) && (top.Count > 0)) {
					ASN1 firstLevel = top [0];
					switch (firstLevel.Tag) {
						case 0x02:
							ki = KeyInfo.PrivateKey;
							break;
						case 0x30:
							ki = KeyInfo.EncryptedPrivateKey;
							break;
					}
				}
			}
			catch {
				throw new CryptographicException ("invalid ASN.1 data");
			}
			return ki;
		}

		/*
		 * PrivateKeyInfo ::= SEQUENCE {
		 *	version Version,
		 *	privateKeyAlgorithm PrivateKeyAlgorithmIdentifier,
		 *	privateKey PrivateKey,
		 *	attributes [0] IMPLICIT Attributes OPTIONAL 
		 * }
		 * 
		 * Version ::= INTEGER
		 * 
		 * PrivateKeyAlgorithmIdentifier ::= AlgorithmIdentifier
		 * 
		 * PrivateKey ::= OCTET STRING
		 * 
		 * Attributes ::= SET OF Attribute
		 */
		public class PrivateKeyInfo {

			private int _version;
			private string _algorithm;
			private byte[] _key;
			private ArrayList _list;

			public PrivateKeyInfo () 
			{
				_version = 0;
				_list = new ArrayList ();
			}

			public PrivateKeyInfo (byte[] data) : this () 
			{
				Decode (data);
			}

			// properties

			public string Algorithm {
				get { return _algorithm; }
				set { _algorithm = value; }
			}

			public ArrayList Attributes {
				get { return _list; }
			}

			public byte[] PrivateKey {
				get {
					if (_key == null)
						return null;
					return (byte[]) _key.Clone (); 
				}
				set { 
					if (value == null)
						throw new ArgumentNullException ("PrivateKey");
					_key = (byte[]) value.Clone (); 
				}
			}

			public int Version {
				get { return _version; }
				set { 
					if (value < 0)
						throw new ArgumentOutOfRangeException ("negative version");
					_version = value; 
				}
			}

			// methods

			private void Decode (byte[] data) 
			{
				ASN1 privateKeyInfo = new ASN1 (data);
				if (privateKeyInfo.Tag != 0x30)
					throw new CryptographicException ("invalid PrivateKeyInfo");

				ASN1 version = privateKeyInfo [0];
				if (version.Tag != 0x02)
					throw new CryptographicException ("invalid version");
				_version = version.Value [0];

				ASN1 privateKeyAlgorithm = privateKeyInfo [1];
				if (privateKeyAlgorithm.Tag != 0x30)
					throw new CryptographicException ("invalid algorithm");
				
				ASN1 algorithm = privateKeyAlgorithm [0];
				if (algorithm.Tag != 0x06)
					throw new CryptographicException ("missing algorithm OID");
				_algorithm = ASN1Convert.ToOid (algorithm);

				ASN1 privateKey = privateKeyInfo [2];
				_key = privateKey.Value;

				// attributes [0] IMPLICIT Attributes OPTIONAL
				if (privateKeyInfo.Count > 3) {
					ASN1 attributes = privateKeyInfo [3];
					for (int i=0; i < attributes.Count; i++) {
						_list.Add (attributes [i]);
					}
				}
			}

			public byte[] GetBytes () 
			{
				ASN1 privateKeyAlgorithm = new ASN1 (0x30);
				privateKeyAlgorithm.Add (ASN1Convert.FromOid (_algorithm));
				privateKeyAlgorithm.Add (new ASN1 (0x05)); // ASN.1 NULL

				ASN1 pki = new ASN1 (0x30);
				pki.Add (new ASN1 (0x02, new byte [1] { (byte) _version }));
				pki.Add (privateKeyAlgorithm);
				pki.Add (new ASN1 (0x04, _key));

				if (_list.Count > 0) {
					ASN1 attributes = new ASN1 (0xA0);
					foreach (ASN1 attribute in _list) {
						attributes.Add (attribute);
					}
					pki.Add (attributes);
				}

				return pki.GetBytes ();
			}

			// static methods

			static private byte[] RemoveLeadingZero (byte[] bigInt) 
			{
				int start = 0;
				int length = bigInt.Length;
				if (bigInt [0] == 0x00) {
					start = 1;
					length--;
				}
				byte[] bi = new byte [length];
				Buffer.BlockCopy (bigInt, start, bi, 0, length);
				return bi;
			}

			static private byte[] Normalize (byte[] bigInt, int length) 
			{
				if (bigInt.Length == length)
					return bigInt;
				else if (bigInt.Length > length)
					return RemoveLeadingZero (bigInt);
				else {
					// pad with 0
					byte[] bi = new byte [length];
					Buffer.BlockCopy (bigInt, 0, bi, (length - bigInt.Length), bigInt.Length);
					return bi;
				}
			}
			
			/*
			 * RSAPrivateKey ::= SEQUENCE {
			 *	version           Version, 
			 *	modulus           INTEGER,  -- n
			 *	publicExponent    INTEGER,  -- e
			 *	privateExponent   INTEGER,  -- d
			 *	prime1            INTEGER,  -- p
			 *	prime2            INTEGER,  -- q
			 *	exponent1         INTEGER,  -- d mod (p-1)
			 *	exponent2         INTEGER,  -- d mod (q-1) 
			 *	coefficient       INTEGER,  -- (inverse of q) mod p
			 *	otherPrimeInfos   OtherPrimeInfos OPTIONAL 
			 * }
			 */
			static public RSA DecodeRSA (byte[] keypair) 
			{
				ASN1 privateKey = new ASN1 (keypair);
				if (privateKey.Tag != 0x30)
					throw new CryptographicException ("invalid private key format");

				ASN1 version = privateKey [0];
				if (version.Tag != 0x02)
					throw new CryptographicException ("missing version");

				if (privateKey.Count < 9)
					throw new CryptographicException ("not enough key parameters");

				RSAParameters param = new RSAParameters ();
				// note: MUST remove leading 0 - else MS wont import the key
				param.Modulus = RemoveLeadingZero (privateKey [1].Value);
				int keysize = param.Modulus.Length;
				int keysize2 = (keysize >> 1); // half-size
				// size must be normalized - else MS wont import the key
				param.D = Normalize (privateKey [3].Value, keysize);
				param.DP = Normalize (privateKey [6].Value, keysize2);
				param.DQ = Normalize (privateKey [7].Value, keysize2);
				param.Exponent = RemoveLeadingZero (privateKey [2].Value);
				param.InverseQ = Normalize (privateKey [8].Value, keysize2);
				param.P = Normalize (privateKey [4].Value, keysize2);
				param.Q = Normalize (privateKey [5].Value, keysize2);

				RSA rsa = null;
				try {
					rsa = RSA.Create ();
					rsa.ImportParameters (param);
				}
				catch (CryptographicException) {
#if MONOTOUCH
					// there's no machine-wide store available for iOS so we can drop the dependency on
					// CspParameters (which drops other things, like XML key persistance, unless used elsewhere)
					throw;
#else
					// this may cause problem when this code is run under
					// the SYSTEM identity on Windows (e.g. ASP.NET). See
					// http://bugzilla.ximian.com/show_bug.cgi?id=77559
					CspParameters csp = new CspParameters ();
					csp.Flags = CspProviderFlags.UseMachineKeyStore;
					rsa = new RSACryptoServiceProvider (csp);
					rsa.ImportParameters (param);
#endif
				}
				return rsa;
			}

			/*
			 * RSAPrivateKey ::= SEQUENCE {
			 *	version           Version, 
			 *	modulus           INTEGER,  -- n
			 *	publicExponent    INTEGER,  -- e
			 *	privateExponent   INTEGER,  -- d
			 *	prime1            INTEGER,  -- p
			 *	prime2            INTEGER,  -- q
			 *	exponent1         INTEGER,  -- d mod (p-1)
			 *	exponent2         INTEGER,  -- d mod (q-1) 
			 *	coefficient       INTEGER,  -- (inverse of q) mod p
			 *	otherPrimeInfos   OtherPrimeInfos OPTIONAL 
			 * }
			 */
			static public byte[] Encode (RSA rsa) 
			{
				RSAParameters param = rsa.ExportParameters (true);

				ASN1 rsaPrivateKey = new ASN1 (0x30);
				rsaPrivateKey.Add (new ASN1 (0x02, new byte [1] { 0x00 }));
				rsaPrivateKey.Add (ASN1Convert.FromUnsignedBigInteger (param.Modulus));
				rsaPrivateKey.Add (ASN1Convert.FromUnsignedBigInteger (param.Exponent));
				rsaPrivateKey.Add (ASN1Convert.FromUnsignedBigInteger (param.D));
				rsaPrivateKey.Add (ASN1Convert.FromUnsignedBigInteger (param.P));
				rsaPrivateKey.Add (ASN1Convert.FromUnsignedBigInteger (param.Q));
				rsaPrivateKey.Add (ASN1Convert.FromUnsignedBigInteger (param.DP));
				rsaPrivateKey.Add (ASN1Convert.FromUnsignedBigInteger (param.DQ));
				rsaPrivateKey.Add (ASN1Convert.FromUnsignedBigInteger (param.InverseQ));

				return rsaPrivateKey.GetBytes ();
			}

			// DSA only encode it's X private key inside an ASN.1 INTEGER (Hint: Tag == 0x02)
			// which isn't enough for rebuilding the keypair. The other parameters
			// can be found (98% of the time) in the X.509 certificate associated
			// with the private key or (2% of the time) the parameters are in it's
			// issuer X.509 certificate (not supported in the .NET framework).
			static public DSA DecodeDSA (byte[] privateKey, DSAParameters dsaParameters) 
			{
				ASN1 pvk = new ASN1 (privateKey);
				if (pvk.Tag != 0x02)
					throw new CryptographicException ("invalid private key format");

				// X is ALWAYS 20 bytes (no matter if the key length is 512 or 1024 bits)
				dsaParameters.X = Normalize (pvk.Value, 20);
				DSA dsa = DSA.Create ();
				dsa.ImportParameters (dsaParameters);
				return dsa;
			}

			static public byte[] Encode (DSA dsa) 
			{
				DSAParameters param = dsa.ExportParameters (true);
				return ASN1Convert.FromUnsignedBigInteger (param.X).GetBytes ();
			}

			static public byte[] Encode (AsymmetricAlgorithm aa) 
			{
				if (aa is RSA)
					return Encode ((RSA)aa);
				else if (aa is DSA)
					return Encode ((DSA)aa);
				else
					throw new CryptographicException ("Unknown asymmetric algorithm {0}", aa.ToString ());
			}
		}

		/*
		 * EncryptedPrivateKeyInfo ::= SEQUENCE {
		 *	encryptionAlgorithm EncryptionAlgorithmIdentifier,
		 *	encryptedData EncryptedData 
		 * }
		 * 
		 * EncryptionAlgorithmIdentifier ::= AlgorithmIdentifier
		 * 
		 * EncryptedData ::= OCTET STRING
		 * 
		 * --
		 *  AlgorithmIdentifier  ::= SEQUENCE {
		 *	algorithm  OBJECT IDENTIFIER,
		 *	parameters ANY DEFINED BY algorithm OPTIONAL
		 * }
		 * 
		 * -- from PKCS#5
		 * PBEParameter ::= SEQUENCE {
		 *	salt OCTET STRING SIZE(8),
		 *	iterationCount INTEGER 
		 * }
		 */
		public class EncryptedPrivateKeyInfo {

			private string _algorithm;
			private byte[] _salt;
			private int _iterations;
			private byte[] _data;

			public EncryptedPrivateKeyInfo () {}

			public EncryptedPrivateKeyInfo (byte[] data) : this () 
			{
				Decode (data);
			}

			// properties

			public string Algorithm {
				get { return _algorithm; }
				set { _algorithm = value; }
			}

			public byte[] EncryptedData {
				get { return (_data == null) ? null : (byte[]) _data.Clone (); }
				set { _data = (value == null) ? null : (byte[]) value.Clone (); }
			}

			public byte[] Salt {
				get { 
					if (_salt == null) {
						RandomNumberGenerator rng = RandomNumberGenerator.Create ();
						_salt = new byte [8];
						rng.GetBytes (_salt);
					}
					return (byte[]) _salt.Clone (); 
				}
				set { _salt = (byte[]) value.Clone (); }
			}

			public int IterationCount {
				get { return _iterations; }
				set { 
					if (value < 0)
						throw new ArgumentOutOfRangeException ("IterationCount", "Negative");
					_iterations = value; 
				}
			}

			// methods

			private void Decode (byte[] data) 
			{
				ASN1 encryptedPrivateKeyInfo = new ASN1 (data);
				if (encryptedPrivateKeyInfo.Tag != 0x30)
					throw new CryptographicException ("invalid EncryptedPrivateKeyInfo");

				ASN1 encryptionAlgorithm = encryptedPrivateKeyInfo [0];
				if (encryptionAlgorithm.Tag != 0x30)
					throw new CryptographicException ("invalid encryptionAlgorithm");
				ASN1 algorithm = encryptionAlgorithm [0];
				if (algorithm.Tag != 0x06)
					throw new CryptographicException ("invalid algorithm");
				_algorithm = ASN1Convert.ToOid (algorithm);
				// parameters ANY DEFINED BY algorithm OPTIONAL
				if (encryptionAlgorithm.Count > 1) {
					ASN1 parameters = encryptionAlgorithm [1];
					if (parameters.Tag != 0x30)
						throw new CryptographicException ("invalid parameters");

					ASN1 salt = parameters [0];
					if (salt.Tag != 0x04)
						throw new CryptographicException ("invalid salt");
					_salt = salt.Value;

					ASN1 iterationCount = parameters [1];
					if (iterationCount.Tag != 0x02)
						throw new CryptographicException ("invalid iterationCount");
					_iterations = ASN1Convert.ToInt32 (iterationCount);
				}

				ASN1 encryptedData = encryptedPrivateKeyInfo [1];
				if (encryptedData.Tag != 0x04)
					throw new CryptographicException ("invalid EncryptedData");
				_data = encryptedData.Value;
			}

			// Note: PKCS#8 doesn't define how to generate the key required for encryption
			// so you're on your own. Just don't try to copy the big guys too much ;)
			// Netscape:	http://www.cs.auckland.ac.nz/~pgut001/pubs/netscape.txt
			// Microsoft:	http://www.cs.auckland.ac.nz/~pgut001/pubs/breakms.txt
			public byte[] GetBytes ()
			{
				if (_algorithm == null)
					throw new CryptographicException ("No algorithm OID specified");

				ASN1 encryptionAlgorithm = new ASN1 (0x30);
				encryptionAlgorithm.Add (ASN1Convert.FromOid (_algorithm));

				// parameters ANY DEFINED BY algorithm OPTIONAL
				if ((_iterations > 0) || (_salt != null)) {
					ASN1 salt = new ASN1 (0x04, _salt);
					ASN1 iterations = ASN1Convert.FromInt32 (_iterations);

					ASN1 parameters = new ASN1 (0x30);
					parameters.Add (salt);
					parameters.Add (iterations);
					encryptionAlgorithm.Add (parameters);
				}

				// encapsulates EncryptedData into an OCTET STRING
				ASN1 encryptedData = new ASN1 (0x04, _data);

				ASN1 encryptedPrivateKeyInfo = new ASN1 (0x30);
				encryptedPrivateKeyInfo.Add (encryptionAlgorithm);
				encryptedPrivateKeyInfo.Add (encryptedData);

				return encryptedPrivateKeyInfo.GetBytes ();
			}
		}
	}
}
