//
// PKCS1.cs - Implements PKCS#1 primitives.
//
// Author:
//	Sebastien Pouliot  <sebastien@xamarin.com>
//
// (C) 2002, 2003 Motus Technologies Inc. (http://www.motus.com)
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
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
using System.Security.Cryptography;

namespace Mono.Security.Cryptography { 

	// References:
	// a.	PKCS#1: RSA Cryptography Standard 
	//	http://www.rsasecurity.com/rsalabs/pkcs/pkcs-1/index.html
	
#if INSIDE_CORLIB
	internal
#else
	public
#endif
	sealed class PKCS1 {

		private PKCS1 () 
		{
		}

		private static bool Compare (byte[] array1, byte[] array2) 
		{
			bool result = (array1.Length == array2.Length);
			if (result) {
				for (int i=0; i < array1.Length; i++)
					if (array1[i] != array2[i])
						return false;
			}
			return result;
		}
	
		private static byte[] xor (byte[] array1, byte[] array2) 
		{
			byte[] result = new byte [array1.Length];
			for (int i=0; i < result.Length; i++)
				result[i] = (byte) (array1[i] ^ array2[i]);
			return result;
		}
	
		private static byte[] emptySHA1   = { 0xda, 0x39, 0xa3, 0xee, 0x5e, 0x6b, 0x4b, 0x0d, 0x32, 0x55, 0xbf, 0xef, 0x95, 0x60, 0x18, 0x90, 0xaf, 0xd8, 0x07, 0x09 };
		private static byte[] emptySHA256 = { 0xe3, 0xb0, 0xc4, 0x42, 0x98, 0xfc, 0x1c, 0x14, 0x9a, 0xfb, 0xf4, 0xc8, 0x99, 0x6f, 0xb9, 0x24, 0x27, 0xae, 0x41, 0xe4, 0x64, 0x9b, 0x93, 0x4c, 0xa4, 0x95, 0x99, 0x1b, 0x78, 0x52, 0xb8, 0x55 };
		private static byte[] emptySHA384 = { 0x38, 0xb0, 0x60, 0xa7, 0x51, 0xac, 0x96, 0x38, 0x4c, 0xd9, 0x32, 0x7e, 0xb1, 0xb1, 0xe3, 0x6a, 0x21, 0xfd, 0xb7, 0x11, 0x14, 0xbe, 0x07, 0x43, 0x4c, 0x0c, 0xc7, 0xbf, 0x63, 0xf6, 0xe1, 0xda, 0x27, 0x4e, 0xde, 0xbf, 0xe7, 0x6f, 0x65, 0xfb, 0xd5, 0x1a, 0xd2, 0xf1, 0x48, 0x98, 0xb9, 0x5b };
		private static byte[] emptySHA512 = { 0xcf, 0x83, 0xe1, 0x35, 0x7e, 0xef, 0xb8, 0xbd, 0xf1, 0x54, 0x28, 0x50, 0xd6, 0x6d, 0x80, 0x07, 0xd6, 0x20, 0xe4, 0x05, 0x0b, 0x57, 0x15, 0xdc, 0x83, 0xf4, 0xa9, 0x21, 0xd3, 0x6c, 0xe9, 0xce, 0x47, 0xd0, 0xd1, 0x3c, 0x5d, 0x85, 0xf2, 0xb0, 0xff, 0x83, 0x18, 0xd2, 0x87, 0x7e, 0xec, 0x2f, 0x63, 0xb9, 0x31, 0xbd, 0x47, 0x41, 0x7a, 0x81, 0xa5, 0x38, 0x32, 0x7a, 0xf9, 0x27, 0xda, 0x3e };
	
		private static byte[] GetEmptyHash (HashAlgorithm hash) 
		{
			if (hash is SHA1)
				return emptySHA1;
			else if (hash is SHA256)
				return emptySHA256;
			else if (hash is SHA384)
				return emptySHA384;
			else if (hash is SHA512)
				return emptySHA512;
			else
				return hash.ComputeHash ((byte[])null);
		}
	
		// PKCS #1 v.2.1, Section 4.1
		// I2OSP converts a non-negative integer to an octet string of a specified length.
		public static byte[] I2OSP (int x, int size) 
		{
			byte[] array = BitConverterLE.GetBytes (x);
			Array.Reverse (array, 0, array.Length);
			return I2OSP (array, size);
		}
	
		public static byte[] I2OSP (byte[] x, int size) 
		{
			byte[] result = new byte [size];
			Buffer.BlockCopy (x, 0, result, (result.Length - x.Length), x.Length);
			return result;
		}
	
		// PKCS #1 v.2.1, Section 4.2
		// OS2IP converts an octet string to a nonnegative integer.
		public static byte[] OS2IP (byte[] x) 
		{
			int i = 0;
			while ((x [i++] == 0x00) && (i < x.Length)) {
				// confuse compiler into reporting a warning with {}
			}
			i--;
			if (i > 0) {
				byte[] result = new byte [x.Length - i];
				Buffer.BlockCopy (x, i, result, 0, result.Length);
				return result;
			}
			else
				return x;
		}
	
		// PKCS #1 v.2.1, Section 5.1.1
		public static byte[] RSAEP (RSA rsa, byte[] m) 
		{
			// c = m^e mod n
			return rsa.EncryptValue (m);
		}
	
		// PKCS #1 v.2.1, Section 5.1.2
		public static byte[] RSADP (RSA rsa, byte[] c) 
		{
			// m = c^d mod n
			// Decrypt value may apply CRT optimizations
			return rsa.DecryptValue (c);
		}
	
		// PKCS #1 v.2.1, Section 5.2.1
		public static byte[] RSASP1 (RSA rsa, byte[] m) 
		{
			// first form: s = m^d mod n
			// Decrypt value may apply CRT optimizations
			return rsa.DecryptValue (m);
		}
	
		// PKCS #1 v.2.1, Section 5.2.2
		public static byte[] RSAVP1 (RSA rsa, byte[] s) 
		{
			// m = s^e mod n
			return rsa.EncryptValue (s);
		}
	
		// PKCS #1 v.2.1, Section 7.1.1
		// RSAES-OAEP-ENCRYPT ((n, e), M, L)
		public static byte[] Encrypt_OAEP (RSA rsa, HashAlgorithm hash, RandomNumberGenerator rng, byte[] M) 
		{
			int size = rsa.KeySize / 8;
			int hLen = hash.HashSize / 8;
			if (M.Length > size - 2 * hLen - 2)
				throw new CryptographicException ("message too long");
			// empty label L SHA1 hash
			byte[] lHash = GetEmptyHash (hash);
			int PSLength = (size - M.Length - 2 * hLen - 2);
			// DB = lHash || PS || 0x01 || M
			byte[] DB = new byte [lHash.Length + PSLength + 1 + M.Length];
			Buffer.BlockCopy (lHash, 0, DB, 0, lHash.Length);
			DB [(lHash.Length + PSLength)] = 0x01;
			Buffer.BlockCopy (M, 0, DB, (DB.Length - M.Length), M.Length);
	
			byte[] seed = new byte [hLen];
			rng.GetBytes (seed);
	
			byte[] dbMask = MGF1 (hash, seed, size - hLen - 1);
			byte[] maskedDB = xor (DB, dbMask);
			byte[] seedMask = MGF1 (hash, maskedDB, hLen);
			byte[] maskedSeed = xor (seed, seedMask);
			// EM = 0x00 || maskedSeed || maskedDB
			byte[] EM = new byte [maskedSeed.Length + maskedDB.Length + 1];
			Buffer.BlockCopy (maskedSeed, 0, EM, 1, maskedSeed.Length);
			Buffer.BlockCopy (maskedDB, 0, EM, maskedSeed.Length + 1, maskedDB.Length);
	
			byte[] m = OS2IP (EM);
			byte[] c = RSAEP (rsa, m);
			return I2OSP (c, size);
		}
	
		// PKCS #1 v.2.1, Section 7.1.2
		// RSAES-OAEP-DECRYPT (K, C, L)
		public static byte[] Decrypt_OAEP (RSA rsa, HashAlgorithm hash, byte[] C) 
		{
			int size = rsa.KeySize / 8;
			int hLen = hash.HashSize / 8;
			if ((size < (2 * hLen + 2)) || (C.Length != size))
				throw new CryptographicException ("decryption error");
	
			byte[] c = OS2IP (C);
			byte[] m = RSADP (rsa, c);
			byte[] EM = I2OSP (m, size);
	
			// split EM = Y || maskedSeed || maskedDB
			byte[] maskedSeed = new byte [hLen];
			Buffer.BlockCopy (EM, 1, maskedSeed, 0, maskedSeed.Length);
			byte[] maskedDB = new byte [size - hLen - 1];
			Buffer.BlockCopy (EM, (EM.Length - maskedDB.Length), maskedDB, 0, maskedDB.Length);
	
			byte[] seedMask = MGF1 (hash, maskedDB, hLen);
			byte[] seed = xor (maskedSeed, seedMask);
			byte[] dbMask = MGF1 (hash, seed, size - hLen - 1);
			byte[] DB = xor (maskedDB, dbMask);
	
			byte[] lHash = GetEmptyHash (hash);
			// split DB = lHash' || PS || 0x01 || M
			byte[] dbHash = new byte [lHash.Length];
			Buffer.BlockCopy (DB, 0, dbHash, 0, dbHash.Length);
			bool h = Compare (lHash, dbHash);
	
			// find separator 0x01
			int nPos = lHash.Length;
			while (DB[nPos] == 0)
				nPos++;
	
			int Msize = DB.Length - nPos - 1;
			byte[] M = new byte [Msize];
			Buffer.BlockCopy (DB, (nPos + 1), M, 0, Msize);
	
			// we could have returned EM[0] sooner but would be helping a timing attack
			if ((EM[0] != 0) || (!h) || (DB[nPos] != 0x01))
				return null;
			return M;
		}
	
		// PKCS #1 v.2.1, Section 7.2.1
		// RSAES-PKCS1-V1_5-ENCRYPT ((n, e), M)
		public static byte[] Encrypt_v15 (RSA rsa, RandomNumberGenerator rng, byte[] M) 
		{
			int size = rsa.KeySize / 8;
			if (M.Length > size - 11)
				throw new CryptographicException ("message too long");
			int PSLength = System.Math.Max (8, (size - M.Length - 3));
			byte[] PS = new byte [PSLength];
			rng.GetNonZeroBytes (PS);
			byte[] EM = new byte [size];
			EM [1] = 0x02;
			Buffer.BlockCopy (PS, 0, EM, 2, PSLength);
			Buffer.BlockCopy (M, 0, EM, (size - M.Length), M.Length);
	
			byte[] m = OS2IP (EM);
			byte[] c = RSAEP (rsa, m);
			byte[] C = I2OSP (c, size);
			return C;
		}
	
		// PKCS #1 v.2.1, Section 7.2.2
		// RSAES-PKCS1-V1_5-DECRYPT (K, C)
		public static byte[] Decrypt_v15 (RSA rsa, byte[] C) 
		{
			int size = rsa.KeySize >> 3; // div by 8
			if ((size < 11) || (C.Length > size))
				throw new CryptographicException ("decryption error");
			byte[] c = OS2IP (C);
			byte[] m = RSADP (rsa, c);
			byte[] EM = I2OSP (m, size);
	
			if ((EM [0] != 0x00) || (EM [1] != 0x02))
				return null;
	
			int mPos = 10;
			// PS is a minimum of 8 bytes + 2 bytes for header
			while ((EM [mPos] != 0x00) && (mPos < EM.Length))
				mPos++;
			if (EM [mPos] != 0x00)
				return null;
			mPos++;
			byte[] M = new byte [EM.Length - mPos];
			Buffer.BlockCopy (EM, mPos, M, 0, M.Length);
			return M;
		}
	
		// PKCS #1 v.2.1, Section 8.2.1
		// RSASSA-PKCS1-V1_5-SIGN (K, M)
		public static byte[] Sign_v15 (RSA rsa, HashAlgorithm hash, byte[] hashValue) 
		{
			int size = (rsa.KeySize >> 3); // div 8
			byte[] EM = Encode_v15 (hash, hashValue, size);
			byte[] m = OS2IP (EM);
			byte[] s = RSASP1 (rsa, m);
			byte[] S = I2OSP (s, size);
			return S;
		}

		internal static byte[] Sign_v15 (RSA rsa, string hashName, byte[] hashValue) 
		{
			using (var hash = CreateFromName (hashName))
				return Sign_v15 (rsa, hash, hashValue);
		}

		// PKCS #1 v.2.1, Section 8.2.2
		// RSASSA-PKCS1-V1_5-VERIFY ((n, e), M, S)
		public static bool Verify_v15 (RSA rsa, HashAlgorithm hash, byte[] hashValue, byte[] signature) 
		{
			return Verify_v15 (rsa, hash, hashValue, signature, false);
		}

		internal static bool Verify_v15 (RSA rsa, string hashName, byte[] hashValue, byte[] signature) 
		{
			using (var hash = CreateFromName (hashName))
				return Verify_v15 (rsa, hash, hashValue, signature, false);
		}

		// DO NOT USE WITHOUT A VERY GOOD REASON
		public static bool Verify_v15 (RSA rsa, HashAlgorithm hash, byte [] hashValue, byte [] signature, bool tryNonStandardEncoding)
		{
			int size = (rsa.KeySize >> 3); // div 8
			byte[] s = OS2IP (signature);
			byte[] m = RSAVP1 (rsa, s);
			byte[] EM2 = I2OSP (m, size);
			byte[] EM = Encode_v15 (hash, hashValue, size);
			bool result = Compare (EM, EM2);
			if (result || !tryNonStandardEncoding)
				return result;

			// NOTE: some signatures don't include the hash OID (pretty lame but real)
			// and compatible with MS implementation. E.g. Verisign Authenticode Timestamps

			// we're making this "as safe as possible"
			if ((EM2 [0] != 0x00) || (EM2 [1] != 0x01))
				return false;
			int i;
			for (i = 2; i < EM2.Length - hashValue.Length - 1; i++) {
				if (EM2 [i] != 0xFF)
					return false;
			}
			if (EM2 [i++] != 0x00)
				return false;

			byte [] decryptedHash = new byte [hashValue.Length];
			Buffer.BlockCopy (EM2, i, decryptedHash, 0, decryptedHash.Length);
			return Compare (decryptedHash, hashValue);
		}
	
		// PKCS #1 v.2.1, Section 9.2
		// EMSA-PKCS1-v1_5-Encode
		public static byte[] Encode_v15 (HashAlgorithm hash, byte[] hashValue, int emLength) 
		{
			if (hashValue.Length != (hash.HashSize >> 3))
				throw new CryptographicException ("bad hash length for " + hash.ToString ());

			// DigestInfo ::= SEQUENCE {
			//	digestAlgorithm AlgorithmIdentifier,
			//	digest OCTET STRING
			// }
		
			byte[] t = null;

			string oid = CryptoConfig.MapNameToOID (hash.ToString ());
			if (oid != null)
			{
				ASN1 digestAlgorithm = new ASN1 (0x30);
				digestAlgorithm.Add (new ASN1 (CryptoConfig.EncodeOID (oid)));
				digestAlgorithm.Add (new ASN1 (0x05));		// NULL
				ASN1 digest = new ASN1 (0x04, hashValue);
				ASN1 digestInfo = new ASN1 (0x30);
				digestInfo.Add (digestAlgorithm);
				digestInfo.Add (digest);

				t = digestInfo.GetBytes ();
			}
			else
			{
				// There are no valid OID, in this case t = hashValue
				// This is the case of the MD5SHA hash algorithm
				t = hashValue;
			}

			Buffer.BlockCopy (hashValue, 0, t, t.Length - hashValue.Length, hashValue.Length);
	
			int PSLength = System.Math.Max (8, emLength - t.Length - 3);
			// PS = PSLength of 0xff
	
			// EM = 0x00 | 0x01 | PS | 0x00 | T
			byte[] EM = new byte [PSLength + t.Length + 3];
			EM [1] = 0x01;
			for (int i=2; i < PSLength + 2; i++)
				EM[i] = 0xff;
			Buffer.BlockCopy (t, 0, EM, PSLength + 3, t.Length);
	
			return EM;
		}
	
		// PKCS #1 v.2.1, Section B.2.1
		public static byte[] MGF1 (HashAlgorithm hash, byte[] mgfSeed, int maskLen) 
		{
			// 1. If maskLen > 2^32 hLen, output "mask too long" and stop.
			// easy - this is impossible by using a int (31bits) as parameter ;-)
			// BUT with a signed int we do have to check for negative values!
			if (maskLen < 0)
				throw new OverflowException();
	
			int mgfSeedLength = mgfSeed.Length;
			int hLen = (hash.HashSize >> 3); // from bits to bytes
			int iterations = (maskLen / hLen);
			if (maskLen % hLen != 0)
				iterations++;
			// 2. Let T be the empty octet string.
			byte[] T = new byte [iterations * hLen];
	
			byte[] toBeHashed = new byte [mgfSeedLength + 4];
			int pos = 0;
			// 3. For counter from 0 to \ceil (maskLen / hLen) - 1, do the following:
			for (int counter = 0; counter < iterations; counter++) {
				// a.	Convert counter to an octet string C of length 4 octets
				byte[] C = I2OSP (counter, 4); 
	
				// b.	Concatenate the hash of the seed mgfSeed and C to the octet string T:
				//	T = T || Hash (mgfSeed || C)
				Buffer.BlockCopy (mgfSeed, 0, toBeHashed, 0, mgfSeedLength);
				Buffer.BlockCopy (C, 0, toBeHashed, mgfSeedLength, 4);
				byte[] output = hash.ComputeHash (toBeHashed);
				Buffer.BlockCopy (output, 0, T, pos, hLen);
				pos += hLen;
			}
			
			// 4. Output the leading maskLen octets of T as the octet string mask.
			byte[] mask = new byte [maskLen];
			Buffer.BlockCopy (T, 0, mask, 0, maskLen);
			return mask;
		}

		static internal string HashNameFromOid (string oid, bool throwOnError = true)
		{
			switch (oid) {
			case "1.2.840.113549.1.1.2":	// MD2 with RSA encryption 
				return "MD2";
			case "1.2.840.113549.1.1.3":	// MD4 with RSA encryption 
				return "MD4";
			case "1.2.840.113549.1.1.4":	// MD5 with RSA encryption 
				return "MD5";
			case "1.2.840.113549.1.1.5":	// SHA-1 with RSA Encryption 
			case "1.3.14.3.2.29":		// SHA1 with RSA signature 
			case "1.2.840.10040.4.3":	// SHA1-1 with DSA
				return "SHA1";
			case "1.2.840.113549.1.1.11":	// SHA-256 with RSA Encryption
				return "SHA256";
			case "1.2.840.113549.1.1.12":	// SHA-384 with RSA Encryption
				return "SHA384";
			case "1.2.840.113549.1.1.13":	// SHA-512 with RSA Encryption
				return "SHA512";
			case "1.3.36.3.3.1.2":
				return "RIPEMD160";
			default:
				if (throwOnError)
					throw new CryptographicException ("Unsupported hash algorithm: " + oid);
				return null;
			}
		}
		
		static internal HashAlgorithm CreateFromOid (string oid)
		{
			return CreateFromName (HashNameFromOid (oid));
		}
		
		static internal HashAlgorithm CreateFromName (string name)
		{
#if FULL_AOT_RUNTIME
			switch (name) {
			case "MD2":
				return MD2.Create ();
			case "MD4":
				return MD4.Create ();
			case "MD5":
				return MD5.Create ();
			case "SHA1":
				return SHA1.Create ();
			case "SHA256":
				return SHA256.Create ();
			case "SHA384":
				return SHA384.Create ();
			case "SHA512":
				return SHA512.Create ();
			case "RIPEMD160":
				return RIPEMD160.Create ();
			default:
				try {
					return (HashAlgorithm) Activator.CreateInstance (Type.GetType (name));
				}
				catch {
					throw new CryptographicException ("Unsupported hash algorithm: " + name);
				}
			}
#else
			return HashAlgorithm.Create (name);
#endif
		}
	}
}
