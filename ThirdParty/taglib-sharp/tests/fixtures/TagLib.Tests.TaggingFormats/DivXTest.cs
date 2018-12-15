using System;
using System.Security.Cryptography;
using NUnit.Framework;
using TagLib;

namespace TagLib.Tests.TaggingFormats
{
	[TestFixture]
	public class DivXTest
	{   
		[Test]
		public void TestTitle ()
		{
			Riff.DivXTag tag = new Riff.DivXTag ();

			Assert.IsTrue (tag.IsEmpty, "Initially empty");
			Assert.IsNull (tag.Title, "Initially null");

			ByteVector rendered = tag.Render ();
			tag = new Riff.DivXTag (rendered);
			Assert.IsTrue (tag.IsEmpty, "Still empty");
			Assert.IsNull (tag.Title, "Still null");

			tag.Title = "01234567890123456789012345678901234567890123456789";
			Assert.IsFalse (tag.IsEmpty, "Not empty");
			Assert.AreEqual ("01234567890123456789012345678901234567890123456789", tag.Title);

			rendered = tag.Render ();
			tag = new Riff.DivXTag (rendered);
			Assert.IsFalse (tag.IsEmpty, "Still not empty");
			Assert.AreEqual ("01234567890123456789012345678901", tag.Title);

			tag.Title = string.Empty;
			Assert.IsTrue (tag.IsEmpty, "Again empty");
			Assert.IsNull (tag.Title, "Again null");

			rendered = tag.Render ();
			tag = new Riff.DivXTag (rendered);
			Assert.IsTrue (tag.IsEmpty, "Still empty");
			Assert.IsNull (tag.Title, "Still null");
		}

		[Test]
		public void TestPerformers ()
		{
			Riff.DivXTag tag = new Riff.DivXTag ();

			Assert.IsTrue (tag.IsEmpty, "Initially empty");
			Assert.AreEqual (0, tag.Performers.Length, "Initially empty");

			ByteVector rendered = tag.Render ();
			tag = new Riff.DivXTag (rendered);
			Assert.IsTrue (tag.IsEmpty, "Still empty");
			Assert.AreEqual (0, tag.Performers.Length, "Still empty");

			tag.Performers = new string [] {"A123456789", "B123456789", "C123456789", "D123456789", "E123456789"};
			Assert.IsFalse (tag.IsEmpty, "Not empty");
			Assert.AreEqual ("A123456789; B123456789; C123456789; D123456789; E123456789", tag.JoinedPerformers);

			rendered = tag.Render ();
			tag = new Riff.DivXTag (rendered);
			Assert.IsFalse (tag.IsEmpty, "Still not empty");
			Assert.AreEqual ("A123456789; B123456789; C12345", tag.JoinedPerformers);

			tag.Performers = new string [0];
			Assert.IsTrue (tag.IsEmpty, "Again empty");
			Assert.AreEqual (0, tag.Performers.Length, "Again empty");

			rendered = tag.Render ();
			tag = new Riff.DivXTag (rendered);
			Assert.IsTrue (tag.IsEmpty, "Still empty");
			Assert.AreEqual (0, tag.Performers.Length, "Still empty");
		}

		[Test]
		public void TestYear ()
		{
			Riff.DivXTag tag = new Riff.DivXTag ();

			Assert.IsTrue (tag.IsEmpty, "Initially empty");
			Assert.AreEqual (0, tag.Year, "Initially zero");

			ByteVector rendered = tag.Render ();
			tag = new Riff.DivXTag (rendered);
			Assert.IsTrue (tag.IsEmpty, "Still empty");
			Assert.AreEqual (0, tag.Year, "Still zero");

			tag.Year = 1999;
			Assert.IsFalse (tag.IsEmpty, "Not empty");
			Assert.AreEqual (1999, tag.Year);

			rendered = tag.Render ();
			tag = new Riff.DivXTag (rendered);
			Assert.IsFalse (tag.IsEmpty, "Still not empty");
			Assert.AreEqual (1999, tag.Year);

			tag.Year = 20000;
			Assert.IsTrue (tag.IsEmpty, "Again empty");
			Assert.AreEqual (0, tag.Year, "Again zero");

			rendered = tag.Render ();
			tag = new Riff.DivXTag (rendered);
			Assert.IsTrue (tag.IsEmpty, "Still empty");
			Assert.AreEqual (0, tag.Year, "Still zero");
		}

		[Test]
		public void TestComment ()
		{
			Riff.DivXTag tag = new Riff.DivXTag ();

			Assert.IsTrue (tag.IsEmpty, "Initially empty");
			Assert.IsNull (tag.Comment, "Initially null");

			ByteVector rendered = tag.Render ();
			tag = new Riff.DivXTag (rendered);
			Assert.IsTrue (tag.IsEmpty, "Still empty");
			Assert.IsNull (tag.Comment, "Still null");

			tag.Comment = "01234567890123456789012345678901234567890123456789";
			Assert.IsFalse (tag.IsEmpty, "Not empty");
			Assert.AreEqual ("01234567890123456789012345678901234567890123456789", tag.Comment);

			rendered = tag.Render ();
			tag = new Riff.DivXTag (rendered);
			Assert.IsFalse (tag.IsEmpty, "Still not empty");
			Assert.AreEqual ("012345678901234567890123456789012345678901234567", tag.Comment);

			tag.Comment = string.Empty;
			Assert.IsTrue (tag.IsEmpty, "Again empty");
			Assert.IsNull (tag.Comment, "Again null");

			rendered = tag.Render ();
			tag = new Riff.DivXTag (rendered);
			Assert.IsTrue (tag.IsEmpty, "Still empty");
			Assert.IsNull (tag.Comment, "Still null");
		}

		[Test]
		public void TestGenres ()
		{
			Riff.DivXTag tag = new Riff.DivXTag ();

			Assert.IsTrue (tag.IsEmpty, "Initially empty");
			Assert.AreEqual (0, tag.Genres.Length, "Initially empty");

			ByteVector rendered = tag.Render ();
			tag = new Riff.DivXTag (rendered);
			Assert.IsTrue (tag.IsEmpty, "Still empty");
			Assert.AreEqual (0, tag.Genres.Length, "Still empty");

			tag.Genres = new string [] {"Action", "Comedy", "Non-Genre", "Claymation"};
			Assert.IsFalse (tag.IsEmpty, "Not empty");
			Assert.AreEqual ("Action", tag.JoinedGenres);

			rendered = tag.Render ();
			tag = new Riff.DivXTag (rendered);
			Assert.IsFalse (tag.IsEmpty, "Still not empty");
			Assert.AreEqual ("Action", tag.JoinedGenres);

			tag.Genres = new string [] {"Non-Genre"};
			Assert.IsTrue (tag.IsEmpty, "Surprisingly empty");
			Assert.AreEqual (0, tag.Genres.Length, "Surprisingly empty");

			rendered = tag.Render ();
			tag = new Riff.DivXTag (rendered);
			Assert.IsTrue (tag.IsEmpty, "Still empty");
			Assert.AreEqual (0, tag.Genres.Length, "Still empty");

			tag.Genres = new string [0];
			Assert.IsTrue (tag.IsEmpty, "Again empty");
			Assert.AreEqual (0, tag.Genres.Length, "Again empty");

			rendered = tag.Render ();
			tag = new Riff.DivXTag (rendered);
			Assert.IsTrue (tag.IsEmpty, "Still empty");
			Assert.AreEqual (0, tag.Genres.Length, "Still empty");
		}

		[Test]
		public void TestClear ()
		{
			Riff.DivXTag tag = new Riff.DivXTag ();

			tag.Title = "A";
			tag.Performers = new string [] {"B"};
			tag.Album = "C";
			tag.Year = 123;
			tag.Comment = "D";
			tag.Track = 234;
			tag.Genres = new string [] {"Blues"};

			Assert.IsFalse (tag.IsEmpty, "Should be full.");
			tag.Clear ();
			Assert.IsNull (tag.Title, "Title");
			Assert.AreEqual (0, tag.Performers.Length, "Performers");
			Assert.IsNull (tag.Album, "Album");
			Assert.AreEqual (0, tag.Year, "Year");
			Assert.IsNull (tag.Comment, "Comment");
			Assert.AreEqual (0, tag.Track, "Track");
			Assert.AreEqual (0, tag.Genres.Length, "Genres");
			Assert.IsTrue (tag.IsEmpty, "Should be empty.");
		}

		[Test]
		public void TestRender ()
		{
			ByteVector rendered = new Riff.DivXTag ().Render ();
			Assert.AreEqual (128, rendered.Count);
			Assert.IsTrue (rendered.EndsWith (Riff.DivXTag.FileIdentifier));
		}
	}
}
