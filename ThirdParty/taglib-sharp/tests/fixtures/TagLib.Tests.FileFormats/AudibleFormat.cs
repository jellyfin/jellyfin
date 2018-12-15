using System;
using NUnit.Framework;
using TagLib;
using System.IO;

namespace TagLib.Tests.FileFormats
{   
	[TestFixture]
	public class AudibleFormatTest
	{
		private static string BaseDirectory = TestPath.Samples + "audible";

		[Test]
		public void First ()
		{
			var tag = (Audible.Tag) File.Create(Path.Combine (BaseDirectory, "first.aa")).Tag;
			Assert.AreEqual (tag.Album, "Glyn Hughes"); // This is probably wrong. The publisher is not the album
			Assert.AreEqual (tag.Author, "Ricky Gervais, Steve Merchant, & Karl Pilkington");
			Assert.AreEqual (tag.Copyright, "&#169;2009 Ricky Gervais; (P)2009 Ricky Gervais");
			Assert.IsTrue (tag.Description.StartsWith ("This is the second in a new series of definitive discourses exploring the diversity of human"));
			Assert.AreEqual (tag.Narrator, "Ricky Gervais, Steve Merchant, & Karl Pilkington");
			Assert.AreEqual (tag.Title, "The Ricky Gervais Guide to... NATURAL HISTORY (Unabridged)");
		}

		[Test]
		[Ignore ("Not supported yet")]
		public void Second ()
		{
			var tag = (Audible.Tag) File.Create(Path.Combine (BaseDirectory, "second.aax")).Tag;
			Assert.AreEqual (tag.Album, "Glyn Hughes"); // This is probably wrong. The publisher is not the album
			Assert.AreEqual (tag.Author, "Ricky Gervais, Steve Merchant, & Karl Pilkington");
			Assert.AreEqual (tag.Copyright, "&#169;2009 Ricky Gervais; (P)2009 Ricky Gervais");
			Assert.IsTrue (tag.Description.StartsWith ("This is the second in a new series of definitive discourses exploring the diversity of human"));
			Assert.AreEqual (tag.Narrator, "Ricky Gervais, Steve Merchant, & Karl Pilkington");
			Assert.AreEqual (tag.Title, "The Ricky Gervais Guide to... NATURAL HISTORY (Unabridged)");
		}

		[Test]
		public void Third ()
		{
			var tag = (Audible.Tag) File.Create(Path.Combine (BaseDirectory, "third.aa")).Tag;
			Assert.AreEqual (tag.Album, "Glyn Hughes"); // This is probably wrong. The publisher is not the album
			Assert.AreEqual (tag.Author, "Ricky Gervais, Steve Merchant, & Karl Pilkington");
			Assert.AreEqual (tag.Copyright, "&#169;2009 Ricky Gervais; (P)2009 Ricky Gervais");
			Assert.IsTrue (tag.Description.StartsWith ("This is the second in a new series of definitive discourses exploring the diversity of human"));
			Assert.AreEqual (tag.Narrator, "Ricky Gervais, Steve Merchant, & Karl Pilkington");
			Assert.AreEqual (tag.Title, "The Ricky Gervais Guide to... NATURAL HISTORY (Unabridged)");
		}

		[Test]
		public void Fourth ()
		{
			var tag = (Audible.Tag) File.Create(Path.Combine (BaseDirectory, "fourth.aa")).Tag;
			Assert.AreEqual (tag.Album, "Glyn Hughes"); // This is probably wrong. The publisher is not the album
			Assert.AreEqual (tag.Author, "Ricky Gervais, Steve Merchant & Karl Pilkington");
			Assert.AreEqual (tag.Copyright, "&#169;2010 Ricky Gervais; (P)2010 Ricky Gervais");
			Assert.IsTrue (tag.Description.StartsWith ("The ninth episode in this new series considers the human body, its form, function, and failings"));
			Assert.AreEqual (tag.Narrator, "Ricky Gervais, Steve Merchant & Karl Pilkington");
			Assert.AreEqual (tag.Title, "The Ricky Gervais Guide to... THE HUMAN BODY");
		}
	}
}
