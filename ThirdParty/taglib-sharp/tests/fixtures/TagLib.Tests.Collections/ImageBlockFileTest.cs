using System;

using TagLib;
using TagLib.Image;

using NUnit.Framework;


namespace TagLib.Tests.Collections
{

	[TestFixture]
	public class ImageBlockFileTest
	{
		private static readonly string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

		private TestBlockFile CreateFile (int length)
		{
			byte[] data = new byte[length];

			for (int i = 0; i < length; i++) {
				data [i] = (byte) Chars[i % Chars.Length];
			}

			return new TestBlockFile (new MemoryFileAbstraction (data.Length, data));
		}

		[Test]
		public void Test1 ()
		{
			var file = CreateFile (26);

			file.AddMetadataBlock (1, 3);
			file.AddMetadataBlock (5, 3);
			file.AddMetadataBlock (9, 3);

			file.SaveMetadata ("", 0);

			file.Seek (0);
			Assert.AreEqual ("AEIMNOPQRSTUVWXYZ", file.ReadBlock ((int) file.Length).ToString ());
		}

		[Test]
		public void Test2 ()
		{
			var file = CreateFile (26);

			file.AddMetadataBlock (9, 3);
			file.AddMetadataBlock (1, 3);
			file.AddMetadataBlock (5, 3);

			file.SaveMetadata ("", 0);

			file.Seek (0);
			Assert.AreEqual ("AEIMNOPQRSTUVWXYZ", file.ReadBlock ((int) file.Length).ToString ());
		}

		[Test]
		public void Test3 ()
		{
			var file = CreateFile (26);

			file.AddMetadataBlock (0, 0);
			file.AddMetadataBlock (5, 0);
			file.AddMetadataBlock (10, 0);

			file.SaveMetadata ("12345", 0);

			file.Seek (0);
			Assert.AreEqual ("12345ABCDEFGHIJKLMNOPQRSTUVWXYZ", file.ReadBlock ((int) file.Length).ToString ());

			file.SaveMetadata ("9", 2);

			file.Seek (0);
			Assert.AreEqual ("9ABCDEFGHIJKLMNOPQRSTUVWXYZ", file.ReadBlock ((int) file.Length).ToString ());

			file.AddMetadataBlock (8, 3);
			file.AddMetadataBlock (1, 6);

			file.SaveMetadata ("abcdefghijklmnop", 7);

			file.Seek (0);
			Assert.AreEqual ("abcdefghijklmnopGKLMNOPQRSTUVWXYZ", file.ReadBlock ((int) file.Length).ToString ());

		}

		[Test]
		public void Test4 ()
		{
			var file = CreateFile (26);

			file.AddMetadataBlock (1, 3);
			file.AddMetadataBlock (5, 3);
			file.AddMetadataBlock (9, 3);

			file.AddMetadataBlock (4, 1);

			file.SaveMetadata ("12", 26);

			file.Seek (0);
			Assert.AreEqual ("AIMNOPQRSTUVWXYZ12", file.ReadBlock ((int) file.Length).ToString ());
		}

		[Test]
		public void Test5 ()
		{
			var file = CreateFile (26);

			file.AddMetadataBlock (4, 1);
			file.AddMetadataBlock (1, 3);
			file.AddMetadataBlock (4, 1);
			file.AddMetadataBlock (5, 3);
			file.AddMetadataBlock (9, 3);
			file.AddMetadataBlock (9, 3);

			file.SaveMetadata ("9999999999999999999", 4);

			file.Seek (0);
			Assert.AreEqual ("A9999999999999999999IMNOPQRSTUVWXYZ", file.ReadBlock ((int) file.Length).ToString ());

			file.SaveMetadata ("0", 0);

			file.Seek (0);
			Assert.AreEqual ("0AIMNOPQRSTUVWXYZ", file.ReadBlock ((int) file.Length).ToString ());
		}

		[Test]
		public void Test6 ()
		{
			var file = CreateFile (26);

			file.AddMetadataBlock (5, 3);
			file.AddMetadataBlock (20, 6);
			file.AddMetadataBlock (1, 3);
			file.AddMetadataBlock (4, 1);
			file.AddMetadataBlock (9, 3);


			file.SaveMetadata ("", 0);

			file.Seek (0);
			Assert.AreEqual ("AIMNOPQRST", file.ReadBlock ((int) file.Length).ToString ());

			file.SaveMetadata ("4564536", 5);

			file.Seek (0);
			Assert.AreEqual ("AIMNO4564536PQRST", file.ReadBlock ((int) file.Length).ToString ());
		}

		[Test]
		public void Test7 ()
		{
			var file = CreateFile (26);

			file.AddMetadataBlock (1, 3);
			file.AddMetadataBlock (5, 3);
			file.AddMetadataBlock (9, 3);
			file.AddMetadataBlock (9, 3);

			file.AddMetadataBlock (20, 6);

			file.AddMetadataBlock (0, 26);

			file.SaveMetadata ("", 0);

			file.Seek (0);
			Assert.AreEqual ("", file.ReadBlock ((int) file.Length).ToString ());
		}

		[Test]
		public void Test8 ()
		{
			var file = CreateFile (26);

			file.AddMetadataBlock (4, 1);
			file.AddMetadataBlock (1, 3);
			file.AddMetadataBlock (4, 1);
			file.AddMetadataBlock (5, 3);
			file.AddMetadataBlock (9, 3);
			file.AddMetadataBlock (9, 3);

			file.SaveMetadata ("9999999999999999999", 15);

			file.Seek (0);
			Assert.AreEqual ("AIMNO9999999999999999999PQRSTUVWXYZ", file.ReadBlock ((int) file.Length).ToString ());
		}

	}

	// Hack, to create objects of the abstarct class ImageBlockFile to test some methods
	public class TestBlockFile : ImageBlockFile
	{

		public TestBlockFile (File.IFileAbstraction abstraction)
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

		public new void AddMetadataBlock (long start, long length)
		{
			base.AddMetadataBlock (start, length);
		}

		public new void SaveMetadata (ByteVector data, long start)
		{
			base.SaveMetadata (data, start);
		}
	}
}
