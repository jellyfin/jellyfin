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

namespace SharpCifs.Util
{
	/// <summary>
	/// This code is derived from the above source
	/// JCIFS API
	/// Norbert Hranitzky
	/// <p>and modified again by Michael B.
	/// </summary>
	/// <remarks>
	/// This code is derived from the above source
	/// JCIFS API
	/// Norbert Hranitzky
	/// <p>and modified again by Michael B. Allen
	/// </remarks>
	public class DES
	{
		private int[] _encryptKeys = new int[32];

		private int[] _decryptKeys = new int[32];

		private int[] _tempInts = new int[2];

		public DES()
		{
		}

		public DES(byte[] key)
		{
			// DesCipher - the DES encryption method
			//
			// The meat of this code is by Dave Zimmerman <dzimm@widget.com>, and is:
			//
			// Copyright (c) 1996 Widget Workshop, Inc. All Rights Reserved.
			//
			// Permission to use, copy, modify, and distribute this software
			// and its documentation for NON-COMMERCIAL or COMMERCIAL purposes and
			// without fee is hereby granted, provided that this copyright notice is kept
			// intact.
			//
			// WIDGET WORKSHOP MAKES NO REPRESENTATIONS OR WARRANTIES ABOUT THE SUITABILITY
			// OF THE SOFTWARE, EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
			// TO THE IMPLIED WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
			// PARTICULAR PURPOSE, OR NON-INFRINGEMENT. WIDGET WORKSHOP SHALL NOT BE LIABLE
			// FOR ANY DAMAGES SUFFERED BY LICENSEE AS A RESULT OF USING, MODIFYING OR
			// DISTRIBUTING THIS SOFTWARE OR ITS DERIVATIVES.
			//
			// THIS SOFTWARE IS NOT DESIGNED OR INTENDED FOR USE OR RESALE AS ON-LINE
			// CONTROL EQUIPMENT IN HAZARDOUS ENVIRONMENTS REQUIRING FAIL-SAFE
			// PERFORMANCE, SUCH AS IN THE OPERATION OF NUCLEAR FACILITIES, AIRCRAFT
			// NAVIGATION OR COMMUNICATION SYSTEMS, AIR TRAFFIC CONTROL, DIRECT LIFE
			// SUPPORT MACHINES, OR WEAPONS SYSTEMS, IN WHICH THE FAILURE OF THE
			// SOFTWARE COULD LEAD DIRECTLY TO DEATH, PERSONAL INJURY, OR SEVERE
			// PHYSICAL OR ENVIRONMENTAL DAMAGE ("HIGH RISK ACTIVITIES").  WIDGET WORKSHOP
			// SPECIFICALLY DISCLAIMS ANY EXPRESS OR IMPLIED WARRANTY OF FITNESS FOR
			// HIGH RISK ACTIVITIES.
			//
			//
			// The rest is:
			//
			// Copyright (C) 1996 by Jef Poskanzer <jef@acme.com>.  All rights reserved.
			//
			// Copyright (C) 1996 by Wolfgang Platzer
			// email: wplatzer@iaik.tu-graz.ac.at
			//
			// All rights reserved.
			//
			// Redistribution and use in source and binary forms, with or without
			// modification, are permitted provided that the following conditions
			// are met:
			// 1. Redistributions of source code must retain the above copyright
			//    notice, this list of conditions and the following disclaimer.
			// 2. Redistributions in binary form must reproduce the above copyright
			//    notice, this list of conditions and the following disclaimer in the
			//    documentation and/or other materials provided with the distribution.
			//
			// THIS SOFTWARE IS PROVIDED BY THE AUTHOR AND CONTRIBUTORS ``AS IS'' AND
			// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
			// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
			// ARE DISCLAIMED.  IN NO EVENT SHALL THE AUTHOR OR CONTRIBUTORS BE LIABLE
			// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
			// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
			// OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
			// HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
			// LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
			// OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
			// SUCH DAMAGE.
			//
			// Constructor, byte-array key.
			if (key.Length == 7)
			{
				byte[] key8 = new byte[8];
				MakeSmbKey(key, key8);
				SetKey(key8);
			}
			else
			{
				SetKey(key);
			}
		}

		public static void MakeSmbKey(byte[] key7, byte[] key8)
		{
			int i;
			key8[0] = unchecked((byte)((key7[0] >> 1) & unchecked(0xff)));
			key8[1] = unchecked((byte)((((key7[0] & unchecked(0x01)) << 6) | (((key7[1
				] & unchecked(0xff)) >> 2) & unchecked(0xff))) & unchecked(0xff)));
			key8[2] = unchecked((byte)((((key7[1] & unchecked(0x03)) << 5) | (((key7[2
				] & unchecked(0xff)) >> 3) & unchecked(0xff))) & unchecked(0xff)));
			key8[3] = unchecked((byte)((((key7[2] & unchecked(0x07)) << 4) | (((key7[3
				] & unchecked(0xff)) >> 4) & unchecked(0xff))) & unchecked(0xff)));
			key8[4] = unchecked((byte)((((key7[3] & unchecked(0x0F)) << 3) | (((key7[4
				] & unchecked(0xff)) >> 5) & unchecked(0xff))) & unchecked(0xff)));
			key8[5] = unchecked((byte)((((key7[4] & unchecked(0x1F)) << 2) | (((key7[5
				] & unchecked(0xff)) >> 6) & unchecked(0xff))) & unchecked(0xff)));
			key8[6] = unchecked((byte)((((key7[5] & unchecked(0x3F)) << 1) | (((key7[6
				] & unchecked(0xff)) >> 7) & unchecked(0xff))) & unchecked(0xff)));
			key8[7] = unchecked((byte)(key7[6] & unchecked(0x7F)));
			for (i = 0; i < 8; i++)
			{
				key8[i] = unchecked((byte)(key8[i] << 1));
			}
		}

		/// Set the key.
		public virtual void SetKey(byte[] key)
		{
			// CHECK PAROTY TBD
			Deskey(key, true, _encryptKeys);
			Deskey(key, false, _decryptKeys);
		}

		// Turn an 8-byte key into internal keys.
		private void Deskey(byte[] keyBlock, bool encrypting, int[] knL)
		{
			int i;
			int j;
			int l;
			int m;
			int n;
			int[] pc1M = new int[56];
			int[] pcr = new int[56];
			int[] kn = new int[32];
			for (j = 0; j < 56; ++j)
			{
				l = _pc1[j];
				m = l & 0x7;
				pc1M[j] = ((keyBlock[(int)(((uint)l) >> 3)] & _bytebit[m]) != 0) ? 1 : 0;
			}
			for (i = 0; i < 16; ++i)
			{
				if (encrypting)
				{
					m = i << 1;
				}
				else
				{
					m = (15 - i) << 1;
				}
				n = m + 1;
				kn[m] = kn[n] = 0;
				for (j = 0; j < 28; ++j)
				{
					l = j + _totrot[i];
					if (l < 28)
					{
						pcr[j] = pc1M[l];
					}
					else
					{
						pcr[j] = pc1M[l - 28];
					}
				}
				for (j = 28; j < 56; ++j)
				{
					l = j + _totrot[i];
					if (l < 56)
					{
						pcr[j] = pc1M[l];
					}
					else
					{
						pcr[j] = pc1M[l - 28];
					}
				}
				for (j = 0; j < 24; ++j)
				{
					if (pcr[_pc2[j]] != 0)
					{
						kn[m] |= _bigbyte[j];
					}
					if (pcr[_pc2[j + 24]] != 0)
					{
						kn[n] |= _bigbyte[j];
					}
				}
			}
			Cookey(kn, knL);
		}

		private void Cookey(int[] raw, int[] knL)
		{
			int raw0;
			int raw1;
			int rawi;
			int knLi;
			int i;
			for (i = 0, rawi = 0, knLi = 0; i < 16; ++i)
			{
				raw0 = raw[rawi++];
				raw1 = raw[rawi++];
				knL[knLi] = (raw0 & unchecked(0x00fc0000)) << 6;
				knL[knLi] |= (raw0 & unchecked(0x00000fc0)) << 10;
				knL[knLi] |= (int)(((uint)(raw1 & unchecked(0x00fc0000))) >> 10);
				knL[knLi] |= (int)(((uint)(raw1 & unchecked(0x00000fc0))) >> 6);
				++knLi;
				knL[knLi] = (raw0 & unchecked(0x0003f000)) << 12;
				knL[knLi] |= (raw0 & unchecked(0x0000003f)) << 16;
				knL[knLi] |= (int)(((uint)(raw1 & unchecked(0x0003f000))) >> 4);
				knL[knLi] |= (raw1 & unchecked(0x0000003f));
				++knLi;
			}
		}

		/// Encrypt a block of eight bytes.
		private void Encrypt(byte[] clearText, int clearOff, byte[] cipherText, int cipherOff
			)
		{
			SquashBytesToInts(clearText, clearOff, _tempInts, 0, 2);
			Des(_tempInts, _tempInts, _encryptKeys);
			SpreadIntsToBytes(_tempInts, 0, cipherText, cipherOff, 2);
		}

		/// Decrypt a block of eight bytes.
		private void Decrypt(byte[] cipherText, int cipherOff, byte[] clearText, int clearOff
			)
		{
			SquashBytesToInts(cipherText, cipherOff, _tempInts, 0, 2);
			Des(_tempInts, _tempInts, _decryptKeys);
			SpreadIntsToBytes(_tempInts, 0, clearText, clearOff, 2);
		}

		// The DES function.
		private void Des(int[] inInts, int[] outInts, int[] keys)
		{
			int fval;
			int work;
			int right;
			int leftt;
			int round;
			int keysi = 0;
			leftt = inInts[0];
			right = inInts[1];
			work = (((int)(((uint)leftt) >> 4)) ^ right) & unchecked(0x0f0f0f0f);
			right ^= work;
			leftt ^= (work << 4);
			work = (((int)(((uint)leftt) >> 16)) ^ right) & unchecked(0x0000ffff);
			right ^= work;
			leftt ^= (work << 16);
			work = (((int)(((uint)right) >> 2)) ^ leftt) & unchecked(0x33333333);
			leftt ^= work;
			right ^= (work << 2);
			work = (((int)(((uint)right) >> 8)) ^ leftt) & unchecked(0x00ff00ff);
			leftt ^= work;
			right ^= (work << 8);
			right = (right << 1) | (((int)(((uint)right) >> 31)) & 1);
			work = (leftt ^ right) & unchecked((int)(0xaaaaaaaa));
			leftt ^= work;
			right ^= work;
			leftt = (leftt << 1) | (((int)(((uint)leftt) >> 31)) & 1);
			for (round = 0; round < 8; ++round)
			{
				work = (right << 28) | ((int)(((uint)right) >> 4));
				work ^= keys[keysi++];
				fval = _sp7[work & unchecked(0x0000003f)];
				fval |= _sp5[((int)(((uint)work) >> 8)) & unchecked(0x0000003f)];
				fval |= _sp3[((int)(((uint)work) >> 16)) & unchecked(0x0000003f)];
				fval |= _sp1[((int)(((uint)work) >> 24)) & unchecked(0x0000003f)];
				work = right ^ keys[keysi++];
				fval |= _sp8[work & unchecked(0x0000003f)];
				fval |= _sp6[((int)(((uint)work) >> 8)) & unchecked(0x0000003f)];
				fval |= _sp4[((int)(((uint)work) >> 16)) & unchecked(0x0000003f)];
				fval |= _sp2[((int)(((uint)work) >> 24)) & unchecked(0x0000003f)];
				leftt ^= fval;
				work = (leftt << 28) | ((int)(((uint)leftt) >> 4));
				work ^= keys[keysi++];
				fval = _sp7[work & unchecked(0x0000003f)];
				fval |= _sp5[((int)(((uint)work) >> 8)) & unchecked(0x0000003f)];
				fval |= _sp3[((int)(((uint)work) >> 16)) & unchecked(0x0000003f)];
				fval |= _sp1[((int)(((uint)work) >> 24)) & unchecked(0x0000003f)];
				work = leftt ^ keys[keysi++];
				fval |= _sp8[work & unchecked(0x0000003f)];
				fval |= _sp6[((int)(((uint)work) >> 8)) & unchecked(0x0000003f)];
				fval |= _sp4[((int)(((uint)work) >> 16)) & unchecked(0x0000003f)];
				fval |= _sp2[((int)(((uint)work) >> 24)) & unchecked(0x0000003f)];
				right ^= fval;
			}
			right = (right << 31) | ((int)(((uint)right) >> 1));
			work = (leftt ^ right) & unchecked((int)(0xaaaaaaaa));
			leftt ^= work;
			right ^= work;
			leftt = (leftt << 31) | ((int)(((uint)leftt) >> 1));
			work = (((int)(((uint)leftt) >> 8)) ^ right) & unchecked(0x00ff00ff);
			right ^= work;
			leftt ^= (work << 8);
			work = (((int)(((uint)leftt) >> 2)) ^ right) & unchecked(0x33333333);
			right ^= work;
			leftt ^= (work << 2);
			work = (((int)(((uint)right) >> 16)) ^ leftt) & unchecked(0x0000ffff);
			leftt ^= work;
			right ^= (work << 16);
			work = (((int)(((uint)right) >> 4)) ^ leftt) & unchecked(0x0f0f0f0f);
			leftt ^= work;
			right ^= (work << 4);
			outInts[0] = right;
			outInts[1] = leftt;
		}

		/// Encrypt a block of bytes.
		public virtual void Encrypt(byte[] clearText, byte[] cipherText)
		{
			Encrypt(clearText, 0, cipherText, 0);
		}

		/// Decrypt a block of bytes.
		public virtual void Decrypt(byte[] cipherText, byte[] clearText)
		{
			Decrypt(cipherText, 0, clearText, 0);
		}

		/// <summary>encrypts an array where the length must be a multiple of 8</summary>
		public virtual byte[] Encrypt(byte[] clearText)
		{
			int length = clearText.Length;
			if (length % 8 != 0)
			{
				Console.Out.WriteLine("Array must be a multiple of 8");
				return null;
			}
			byte[] cipherText = new byte[length];
			int count = length / 8;
			for (int i = 0; i < count; i++)
			{
				Encrypt(clearText, i * 8, cipherText, i * 8);
			}
			return cipherText;
		}

		/// <summary>decrypts an array where the length must be a multiple of 8</summary>
		public virtual byte[] Decrypt(byte[] cipherText)
		{
			int length = cipherText.Length;
			if (length % 8 != 0)
			{
				Console.Out.WriteLine("Array must be a multiple of 8");
				return null;
			}
			byte[] clearText = new byte[length];
			int count = length / 8;
			for (int i = 0; i < count; i++)
			{
				Encrypt(cipherText, i * 8, clearText, i * 8);
			}
			return clearText;
		}

		private static byte[] _bytebit = { unchecked(unchecked(0x80)), unchecked(unchecked(0x40)), unchecked(unchecked(0x20)), unchecked(unchecked(0x10)), unchecked(unchecked(0x08)), unchecked(unchecked(0x04)), unchecked(unchecked(0x02)), unchecked(unchecked(0x01)) };

		private static int[] _bigbyte = { unchecked(0x800000), unchecked(
			0x400000), unchecked(0x200000), unchecked(0x100000), unchecked(
			0x080000), unchecked(0x040000), unchecked(0x020000), unchecked(
			0x010000), unchecked(0x008000), unchecked(0x004000), unchecked(
			0x002000), unchecked(0x001000), unchecked(0x000800), unchecked(
			0x000400), unchecked(0x000200), unchecked(0x000100), unchecked(
			0x000080), unchecked(0x000040), unchecked(0x000020), unchecked(
			0x000010), unchecked(0x000008), unchecked(0x000004), unchecked(
			0x000002), unchecked(0x000001) };

		private static byte[] _pc1 = { unchecked(56), unchecked(48)
			, unchecked(40), unchecked(32), unchecked(24), unchecked(16), unchecked(8), unchecked(0), unchecked(57), unchecked(49), unchecked(41), unchecked(33), unchecked(25), unchecked(17), unchecked(9), unchecked(1), unchecked(58), unchecked(
			50), unchecked(42), unchecked(34), unchecked(26), unchecked(
			18), unchecked(10), unchecked(2), unchecked(59), unchecked(
			51), unchecked(43), unchecked(35), unchecked(62), unchecked(
			54), unchecked(46), unchecked(38), unchecked(30), unchecked(
			22), unchecked(14), unchecked(6), unchecked(61), unchecked(
			53), unchecked(45), unchecked(37), unchecked(29), unchecked(
			21), unchecked(13), unchecked(5), unchecked(60), unchecked(
			52), unchecked(44), unchecked(36), unchecked(28), unchecked(
			20), unchecked(12), unchecked(4), unchecked(27), unchecked(
			19), unchecked(11), unchecked(3) };

		private static int[] _totrot = { 1, 2, 4, 6, 8, 10, 12, 14, 15, 17, 19, 
			21, 23, 25, 27, 28 };

		private static byte[] _pc2 = { unchecked(13), unchecked(16)
			, unchecked(10), unchecked(23), unchecked(0), unchecked(4), unchecked(2), unchecked(27), unchecked(14), unchecked(5), unchecked(20), unchecked(9), unchecked(22), unchecked(18), unchecked(11), unchecked(3), unchecked(25), unchecked(7), unchecked(15), unchecked(6), unchecked(26), unchecked(19), unchecked(12), unchecked(1), unchecked(40), unchecked(51), unchecked(30), unchecked(36), unchecked(46), unchecked(54), unchecked(29), unchecked(39), unchecked(50), unchecked(
			44), unchecked(32), unchecked(47), unchecked(43), unchecked(
			48), unchecked(38), unchecked(55), unchecked(33), unchecked(
			52), unchecked(45), unchecked(41), unchecked(49), unchecked(
			35), unchecked(28), unchecked(31) };

		private static int[] _sp1 = { unchecked(0x01010400), unchecked(0x00000000), unchecked(0x00010000), unchecked(0x01010404), unchecked(
			0x01010004), unchecked(0x00010404), unchecked(0x00000004), 
			unchecked(0x00010000), unchecked(0x00000400), unchecked(0x01010400), unchecked(0x01010404), unchecked(0x00000400), unchecked(0x01000404), unchecked(0x01010004), unchecked(0x01000000), unchecked(
			0x00000004), unchecked(0x00000404), unchecked(0x01000400), 
			unchecked(0x01000400), unchecked(0x00010400), unchecked(0x00010400), unchecked(0x01010000), unchecked(0x01010000), unchecked(0x01000404), unchecked(0x00010004), unchecked(0x01000004), unchecked(
			0x01000004), unchecked(0x00010004), unchecked(0x00000000), 
			unchecked(0x00000404), unchecked(0x00010404), unchecked(0x01000000), unchecked(0x00010000), unchecked(0x01010404), unchecked(0x00000004), unchecked(0x01010000), unchecked(0x01010400), unchecked(
			0x01000000), unchecked(0x01000000), unchecked(0x00000400), 
			unchecked(0x01010004), unchecked(0x00010000), unchecked(0x00010400), unchecked(0x01000004), unchecked(0x00000400), unchecked(0x00000004), unchecked(0x01000404), unchecked(0x00010404), unchecked(
			0x01010404), unchecked(0x00010004), unchecked(0x01010000), 
			unchecked(0x01000404), unchecked(0x01000004), unchecked(0x00000404), unchecked(0x00010404), unchecked(0x01010400), unchecked(0x00000404), unchecked(0x01000400), unchecked(0x01000400), unchecked(
			0x00000000), unchecked(0x00010004), unchecked(0x00010400), 
			unchecked(0x00000000), unchecked(0x01010004) };

		private static int[] _sp2 = { unchecked((int)(0x80108020)), unchecked((int
			)(0x80008000)), unchecked(0x00008000), unchecked(0x00108020), unchecked(
			0x00100000), unchecked(0x00000020), unchecked((int)(0x80100020)), 
			unchecked((int)(0x80008020)), unchecked((int)(0x80000020)), unchecked((int)(0x80108020
			)), unchecked((int)(0x80108000)), unchecked((int)(0x80000000)), unchecked((int)(
			0x80008000)), unchecked(0x00100000), unchecked(0x00000020), unchecked(
			(int)(0x80100020)), unchecked(0x00108000), unchecked(0x00100020), 
			unchecked((int)(0x80008020)), unchecked(0x00000000), unchecked((int)(0x80000000
			)), unchecked(0x00008000), unchecked(0x00108020), unchecked((int)(
			0x80100000)), unchecked(0x00100020), unchecked((int)(0x80000020)), unchecked(
			0x00000000), unchecked(0x00108000), unchecked(0x00008020), 
			unchecked((int)(0x80108000)), unchecked((int)(0x80100000)), unchecked(0x00008020), unchecked(0x00000000), unchecked(0x00108020), unchecked((int)(
			0x80100020)), unchecked(0x00100000), unchecked((int)(0x80008020)), unchecked(
			(int)(0x80100000)), unchecked((int)(0x80108000)), unchecked(0x00008000), 
			unchecked((int)(0x80100000)), unchecked((int)(0x80008000)), unchecked(0x00000020), unchecked((int)(0x80108020)), unchecked(0x00108020), unchecked(0x00000020), unchecked(0x00008000), unchecked((int)(0x80000000)), unchecked(
			0x00008020), unchecked((int)(0x80108000)), unchecked(0x00100000), 
			unchecked((int)(0x80000020)), unchecked(0x00100020), unchecked((int)(0x80008020
			)), unchecked((int)(0x80000020)), unchecked(0x00100020), unchecked(0x00108000), unchecked(0x00000000), unchecked((int)(0x80008000)), unchecked(
			0x00008020), unchecked((int)(0x80000000)), unchecked((int)(0x80100020)), 
			unchecked((int)(0x80108020)), unchecked(0x00108000) };

		private static int[] _sp3 = { unchecked(0x00000208), unchecked(0x08020200), unchecked(0x00000000), unchecked(0x08020008), unchecked(
			0x08000200), unchecked(0x00000000), unchecked(0x00020208), 
			unchecked(0x08000200), unchecked(0x00020008), unchecked(0x08000008), unchecked(0x08000008), unchecked(0x00020000), unchecked(0x08020208), unchecked(0x00020008), unchecked(0x08020000), unchecked(
			0x00000208), unchecked(0x08000000), unchecked(0x00000008), 
			unchecked(0x08020200), unchecked(0x00000200), unchecked(0x00020200), unchecked(0x08020000), unchecked(0x08020008), unchecked(0x00020208), unchecked(0x08000208), unchecked(0x00020200), unchecked(
			0x00020000), unchecked(0x08000208), unchecked(0x00000008), 
			unchecked(0x08020208), unchecked(0x00000200), unchecked(0x08000000), unchecked(0x08020200), unchecked(0x08000000), unchecked(0x00020008), unchecked(0x00000208), unchecked(0x00020000), unchecked(
			0x08020200), unchecked(0x08000200), unchecked(0x00000000), 
			unchecked(0x00000200), unchecked(0x00020008), unchecked(0x08020208), unchecked(0x08000200), unchecked(0x08000008), unchecked(0x00000200), unchecked(0x00000000), unchecked(0x08020008), unchecked(
			0x08000208), unchecked(0x00020000), unchecked(0x08000000), 
			unchecked(0x08020208), unchecked(0x00000008), unchecked(0x00020208), unchecked(0x00020200), unchecked(0x08000008), unchecked(0x08020000), unchecked(0x08000208), unchecked(0x00000208), unchecked(
			0x08020000), unchecked(0x00020208), unchecked(0x00000008), 
			unchecked(0x08020008), unchecked(0x00020200) };

		private static int[] _sp4 = { unchecked(0x00802001), unchecked(0x00002081), unchecked(0x00002081), unchecked(0x00000080), unchecked(
			0x00802080), unchecked(0x00800081), unchecked(0x00800001), 
			unchecked(0x00002001), unchecked(0x00000000), unchecked(0x00802000), unchecked(0x00802000), unchecked(0x00802081), unchecked(0x00000081), unchecked(0x00000000), unchecked(0x00800080), unchecked(
			0x00800001), unchecked(0x00000001), unchecked(0x00002000), 
			unchecked(0x00800000), unchecked(0x00802001), unchecked(0x00000080), unchecked(0x00800000), unchecked(0x00002001), unchecked(0x00002080), unchecked(0x00800081), unchecked(0x00000001), unchecked(
			0x00002080), unchecked(0x00800080), unchecked(0x00002000), 
			unchecked(0x00802080), unchecked(0x00802081), unchecked(0x00000081), unchecked(0x00800080), unchecked(0x00800001), unchecked(0x00802000), unchecked(0x00802081), unchecked(0x00000081), unchecked(
			0x00000000), unchecked(0x00000000), unchecked(0x00802000), 
			unchecked(0x00002080), unchecked(0x00800080), unchecked(0x00800081), unchecked(0x00000001), unchecked(0x00802001), unchecked(0x00002081), unchecked(0x00002081), unchecked(0x00000080), unchecked(
			0x00802081), unchecked(0x00000081), unchecked(0x00000001), 
			unchecked(0x00002000), unchecked(0x00800001), unchecked(0x00002001), unchecked(0x00802080), unchecked(0x00800081), unchecked(0x00002001), unchecked(0x00002080), unchecked(0x00800000), unchecked(
			0x00802001), unchecked(0x00000080), unchecked(0x00800000), 
			unchecked(0x00002000), unchecked(0x00802080) };

		private static int[] _sp5 = { unchecked(0x00000100), unchecked(0x02080100), unchecked(0x02080000), unchecked(0x42000100), unchecked(
			0x00080000), unchecked(0x00000100), unchecked(0x40000000), 
			unchecked(0x02080000), unchecked(0x40080100), unchecked(0x00080000), unchecked(0x02000100), unchecked(0x40080100), unchecked(0x42000100), unchecked(0x42080000), unchecked(0x00080100), unchecked(
			0x40000000), unchecked(0x02000000), unchecked(0x40080000), 
			unchecked(0x40080000), unchecked(0x00000000), unchecked(0x40000100), unchecked(0x42080100), unchecked(0x42080100), unchecked(0x02000100), unchecked(0x42080000), unchecked(0x40000100), unchecked(
			0x00000000), unchecked(0x42000000), unchecked(0x02080100), 
			unchecked(0x02000000), unchecked(0x42000000), unchecked(0x00080100), unchecked(0x00080000), unchecked(0x42000100), unchecked(0x00000100), unchecked(0x02000000), unchecked(0x40000000), unchecked(
			0x02080000), unchecked(0x42000100), unchecked(0x40080100), 
			unchecked(0x02000100), unchecked(0x40000000), unchecked(0x42080000), unchecked(0x02080100), unchecked(0x40080100), unchecked(0x00000100), unchecked(0x02000000), unchecked(0x42080000), unchecked(
			0x42080100), unchecked(0x00080100), unchecked(0x42000000), 
			unchecked(0x42080100), unchecked(0x02080000), unchecked(0x00000000), unchecked(0x40080000), unchecked(0x42000000), unchecked(0x00080100), unchecked(0x02000100), unchecked(0x40000100), unchecked(
			0x00080000), unchecked(0x00000000), unchecked(0x40080000), 
			unchecked(0x02080100), unchecked(0x40000100) };

		private static int[] _sp6 = { unchecked(0x20000010), unchecked(0x20400000), unchecked(0x00004000), unchecked(0x20404010), unchecked(
			0x20400000), unchecked(0x00000010), unchecked(0x20404010), 
			unchecked(0x00400000), unchecked(0x20004000), unchecked(0x00404010), unchecked(0x00400000), unchecked(0x20000010), unchecked(0x00400010), unchecked(0x20004000), unchecked(0x20000000), unchecked(
			0x00004010), unchecked(0x00000000), unchecked(0x00400010), 
			unchecked(0x20004010), unchecked(0x00004000), unchecked(0x00404000), unchecked(0x20004010), unchecked(0x00000010), unchecked(0x20400010), unchecked(0x20400010), unchecked(0x00000000), unchecked(
			0x00404010), unchecked(0x20404000), unchecked(0x00004010), 
			unchecked(0x00404000), unchecked(0x20404000), unchecked(0x20000000), unchecked(0x20004000), unchecked(0x00000010), unchecked(0x20400010), unchecked(0x00404000), unchecked(0x20404010), unchecked(
			0x00400000), unchecked(0x00004010), unchecked(0x20000010), 
			unchecked(0x00400000), unchecked(0x20004000), unchecked(0x20000000), unchecked(0x00004010), unchecked(0x20000010), unchecked(0x20404010), unchecked(0x00404000), unchecked(0x20400000), unchecked(
			0x00404010), unchecked(0x20404000), unchecked(0x00000000), 
			unchecked(0x20400010), unchecked(0x00000010), unchecked(0x00004000), unchecked(0x20400000), unchecked(0x00404010), unchecked(0x00004000), unchecked(0x00400010), unchecked(0x20004010), unchecked(
			0x00000000), unchecked(0x20404000), unchecked(0x20000000), 
			unchecked(0x00400010), unchecked(0x20004010) };

		private static int[] _sp7 = { unchecked(0x00200000), unchecked(0x04200002), unchecked(0x04000802), unchecked(0x00000000), unchecked(
			0x00000800), unchecked(0x04000802), unchecked(0x00200802), 
			unchecked(0x04200800), unchecked(0x04200802), unchecked(0x00200000), unchecked(0x00000000), unchecked(0x04000002), unchecked(0x00000002), unchecked(0x04000000), unchecked(0x04200002), unchecked(
			0x00000802), unchecked(0x04000800), unchecked(0x00200802), 
			unchecked(0x00200002), unchecked(0x04000800), unchecked(0x04000002), unchecked(0x04200000), unchecked(0x04200800), unchecked(0x00200002), unchecked(0x04200000), unchecked(0x00000800), unchecked(
			0x00000802), unchecked(0x04200802), unchecked(0x00200800), 
			unchecked(0x00000002), unchecked(0x04000000), unchecked(0x00200800), unchecked(0x04000000), unchecked(0x00200800), unchecked(0x00200000), unchecked(0x04000802), unchecked(0x04000802), unchecked(
			0x04200002), unchecked(0x04200002), unchecked(0x00000002), 
			unchecked(0x00200002), unchecked(0x04000000), unchecked(0x04000800), unchecked(0x00200000), unchecked(0x04200800), unchecked(0x00000802), unchecked(0x00200802), unchecked(0x04200800), unchecked(
			0x00000802), unchecked(0x04000002), unchecked(0x04200802), 
			unchecked(0x04200000), unchecked(0x00200800), unchecked(0x00000000), unchecked(0x00000002), unchecked(0x04200802), unchecked(0x00000000), unchecked(0x00200802), unchecked(0x04200000), unchecked(
			0x00000800), unchecked(0x04000002), unchecked(0x04000800), 
			unchecked(0x00000800), unchecked(0x00200002) };

		private static int[] _sp8 = { unchecked(0x10001040), unchecked(0x00001000), unchecked(0x00040000), unchecked(0x10041040), unchecked(
			0x10000000), unchecked(0x10001040), unchecked(0x00000040), 
			unchecked(0x10000000), unchecked(0x00040040), unchecked(0x10040000), unchecked(0x10041040), unchecked(0x00041000), unchecked(0x10041000), unchecked(0x00041040), unchecked(0x00001000), unchecked(
			0x00000040), unchecked(0x10040000), unchecked(0x10000040), 
			unchecked(0x10001000), unchecked(0x00001040), unchecked(0x00041000), unchecked(0x00040040), unchecked(0x10040040), unchecked(0x10041000), unchecked(0x00001040), unchecked(0x00000000), unchecked(
			0x00000000), unchecked(0x10040040), unchecked(0x10000040), 
			unchecked(0x10001000), unchecked(0x00041040), unchecked(0x00040000), unchecked(0x00041040), unchecked(0x00040000), unchecked(0x10041000), unchecked(0x00001000), unchecked(0x00000040), unchecked(
			0x10040040), unchecked(0x00001000), unchecked(0x00041040), 
			unchecked(0x10001000), unchecked(0x00000040), unchecked(0x10000040), unchecked(0x10040000), unchecked(0x10040040), unchecked(0x10000000), unchecked(0x00040000), unchecked(0x10001040), unchecked(
			0x00000000), unchecked(0x10041040), unchecked(0x00040040), 
			unchecked(0x10000040), unchecked(0x10040000), unchecked(0x10001000), unchecked(0x10001040), unchecked(0x00000000), unchecked(0x10041040), unchecked(0x00041000), unchecked(0x00041000), unchecked(
			0x00001040), unchecked(0x00001040), unchecked(0x00040040), 
			unchecked(0x10000000), unchecked(0x10041000) };

		// Tables, permutations, S-boxes, etc.
		/// Squash bytes down to ints.
		public static void SquashBytesToInts(byte[] inBytes, int inOff, int[] outInts, int
			 outOff, int intLen)
		{
			for (int i = 0; i < intLen; ++i)
			{
				outInts[outOff + i] = ((inBytes[inOff + i * 4] & unchecked(0xff)) << 24) |
					 ((inBytes[inOff + i * 4 + 1] & unchecked(0xff)) << 16) | ((inBytes[inOff
					 + i * 4 + 2] & unchecked(0xff)) << 8) | (inBytes[inOff + i * 4 + 3] & unchecked(
					0xff));
			}
		}

		/// Spread ints into bytes.
		public static void SpreadIntsToBytes(int[] inInts, int inOff, byte[] outBytes, int
			 outOff, int intLen)
		{
			for (int i = 0; i < intLen; ++i)
			{
				outBytes[outOff + i * 4] = unchecked((byte)((int)(((uint)inInts[inOff + i]) >> 24
					)));
				outBytes[outOff + i * 4 + 1] = unchecked((byte)((int)(((uint)inInts[inOff + i]) >>
					 16)));
				outBytes[outOff + i * 4 + 2] = unchecked((byte)((int)(((uint)inInts[inOff + i]) >>
					 8)));
				outBytes[outOff + i * 4 + 3] = unchecked((byte)inInts[inOff + i]);
			}
		}
	}
}
