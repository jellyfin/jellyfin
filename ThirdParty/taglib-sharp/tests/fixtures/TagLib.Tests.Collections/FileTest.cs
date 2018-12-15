using System;
using System.Security.Cryptography;
using NUnit.Framework;
using TagLib;

namespace TagLib.Tests.Collections
{
	[TestFixture]
	public class FileTest
	{
		private static readonly string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
		private static readonly string Pattern1 = "efg";
		private static readonly string Pattern3 = "bbbbba";

		// length1 is smaller than the used buffer size
		// length2 is bigger than the used buffer size
		// length3 is even more bigger than length2 and used to catch some special cases
		private static readonly int length1 = (int) (0.75 * File.BufferSize);
		private static readonly int length2 = (int) (1.5  * File.BufferSize);
		private static readonly int length3 = (int) (3.1  * File.BufferSize);

		private File CreateFile (int length)
		{
			byte[] data = new byte[length];

			for (int i = 0; i < length; i++) {
				data [i] = (byte) Chars[i % Chars.Length];
			}

			return new TestFile (new MemoryFileAbstraction (data.Length, data));
		}

		[Test]
		public void RFind ()
		{
			// file1
			File file1 = CreateFile (length1);
			Assert.AreEqual (-1, file1.RFind (Pattern1));
			Assert.AreEqual (-1, file1.RFind (Pattern1, 5));

			file1.Insert (Pattern1, 30, 2);
			Assert.AreEqual (30, file1.RFind (Pattern1));

			file1.Insert (Pattern1, length1 / 2, 2);
			Assert.AreEqual (length1 / 2, file1.RFind (Pattern1));

			file1.Insert (Pattern1, length1 - 30, 2);
			Assert.AreEqual (length1 - 30, file1.RFind (Pattern1));

			Assert.AreEqual (30, file1.RFind (Pattern1, length1 - length1 / 2 + 1));
			Assert.AreEqual (length1 / 2, file1.RFind (Pattern1, 30 + 1));
			Assert.AreEqual (length1 - 30, file1.RFind (Pattern1, 2 + 1));


			// file2
			File file2 = CreateFile (length2);
			Assert.AreEqual (-1, file2.RFind (Pattern1));
			Assert.AreEqual (-1, file2.RFind (Pattern1, 8));

			file2.Insert (Pattern1, 30, Pattern1.Length);
			Assert.AreEqual (30, file2.RFind (Pattern1));

			file2.Insert (Pattern1, length2 / 2, Pattern1.Length);
			Assert.AreEqual (length2 / 2, file2.RFind (Pattern1));

			file2.Insert (Pattern1, length2 - 30, Pattern1.Length);
			Assert.AreEqual (length2 - 30, file2.RFind (Pattern1));

			Assert.AreEqual (30, file2.RFind (Pattern1, length2 - length2 / 2));
			Assert.AreEqual (length2 / 2, file2.RFind (Pattern1, 30));
			Assert.AreEqual (length2 - 30, file2.RFind (Pattern1, 2));


			// file3
			// especially used to test searching if the search pattern is splattened to
			// different buffer reads
			// this test is specialized to the used algorithm
			File file3 = CreateFile (length3);
			Assert.AreEqual (-1, file3.RFind (Pattern1));
			Assert.AreEqual (-1, file3.RFind (Pattern1, 13));

			long buffer_cross2 = file3.Length - 2 * File.BufferSize - (Pattern1.Length / 2);
			file3.Insert (Pattern1, buffer_cross2, Pattern1.Length);
			Assert.AreEqual (buffer_cross2, file3.RFind (Pattern1));

			long buffer_cross1 = file3.Length - File.BufferSize - (Pattern1.Length / 2);
			file3.Insert (Pattern1, buffer_cross1, Pattern1.Length);
			Assert.AreEqual (buffer_cross1, file3.RFind (Pattern1));

			// see Find()
			long buffer_cross3 = file3.Length - File.BufferSize - Pattern3.Length + 1;
			file3.Insert (Pattern3, buffer_cross3 + 1, Pattern3.Length);
			file3.Insert (Pattern3, buffer_cross3, Pattern3.Length);
			Assert.AreEqual (buffer_cross3, file3.RFind (Pattern3));
		}

		[Test]
		public void Find ()
		{
			// file1
			File file1 = CreateFile (length1);
			Assert.AreEqual (Chars.IndexOf ('U'), file1.Find ((byte) 'U'));

			Assert.AreEqual (-1, file1.Find (Pattern1));
			Assert.AreEqual (-1, file1.Find (Pattern1, 9));

			file1.Insert (Pattern1, length1 - 30, Pattern1.Length);
			Assert.AreEqual (length1 - 30, file1.Find (Pattern1));

			file1.Insert (Pattern1, length1 / 2, Pattern1.Length);
			Assert.AreEqual (length1 / 2, file1.Find (Pattern1));

			file1.Insert (Pattern1, 30, Pattern1.Length);
			Assert.AreEqual (30, file1.Find (Pattern1));


			// file2
			File file2 = CreateFile (length2);
			Assert.AreEqual (Chars.IndexOf ('M'), file2.Find ((byte) 'M'));

			Assert.AreEqual (-1, file2.Find (Pattern1));
			Assert.AreEqual (-1, file2.Find (Pattern1, 3));

			file2.Insert (Pattern1, length2 - 30, Pattern1.Length);
			Assert.AreEqual (length2 - 30, file2.Find (Pattern1));

			file2.Insert (Pattern1, length2 / 2, Pattern1.Length);
			Assert.AreEqual (length2 / 2, file2.Find (Pattern1));

			file2.Insert (Pattern1, 30, Pattern1.Length);
			Assert.AreEqual (30, file2.Find (Pattern1));

			Assert.AreEqual (30, file2.Find (Pattern1, 2));
			Assert.AreEqual (length2 / 2, file2.Find (Pattern1, 31));
			Assert.AreEqual (length2 - 30, file2.Find (Pattern1, length2 / 2 + 3));

			// file3
			// especially used to test searching if the search pattern is splattened to
			// different buffer reads.
			// this test is specialized to the used algorithm
			File file3 = CreateFile (length3);
			Assert.AreEqual (-1, file3.Find (Pattern1));
			Assert.AreEqual (-1, file3.Find (Pattern1, 13));

			long buffer_cross2 = 2 * File.BufferSize - (Pattern1.Length / 2);
			file3.Insert (Pattern1, buffer_cross2, Pattern1.Length);
			Assert.AreEqual (buffer_cross2, file3.Find (Pattern1));

			long buffer_cross1 = File.BufferSize - (Pattern1.Length / 2);
			file3.Insert (Pattern1, buffer_cross1, Pattern1.Length);
			Assert.AreEqual (buffer_cross1, file3.Find (Pattern1));

			Assert.AreEqual (buffer_cross2, file3.Find (Pattern1, buffer_cross1 + 1));

			long buffer_cross3 = File.BufferSize - 1;
			file3.Insert (Pattern3, buffer_cross3 - 1, Pattern3.Length);
			file3.Insert (Pattern3, buffer_cross3, Pattern3.Length);
			Assert.AreEqual (buffer_cross3, file3.Find (Pattern3));
		}
	}


	// Hack, to create objects of the abstarct class File to test some methods
	public class TestFile : File
	{

		public TestFile (File.IFileAbstraction abstraction)
		: base (abstraction) {}

		public override Tag GetTag (TagTypes type, bool create)
		{
			throw new System.NotImplementedException ();
		}

		public override Properties Properties {
			get {
				throw new System.NotImplementedException ();
			}
		}

		public override void RemoveTags (TagTypes types)
		{
			throw new System.NotImplementedException ();
		}

		public override void Save ()
		{
			throw new System.NotImplementedException ();
		}

		public override Tag Tag {
			get {
				throw new System.NotImplementedException ();
			}
		}
	}
}
