using System;
using NUnit.Framework;
using TagLib;

namespace TagLib.Tests.Collections
{   
	[TestFixture]
	public class ByteVectorCollectionTest
	{   
		private static ByteVectorCollection BuildList()
		{
			ByteVectorCollection list = new ByteVectorCollection();
			list.Add("ABC");
			list.Add("DEF");
			list.Add("GHI");
			return list;
		}
	
		[Test]
		public void Add()
		{
			Assert.AreEqual("ABC:DEF:GHI", BuildList().ToByteVector(":").ToString());
		}
		
		[Test]
		public void Remove()
		{
			ByteVectorCollection list = BuildList();
			list.Remove("DEF");
			Assert.AreEqual("ABCGHI", list.ToByteVector("").ToString());
		}
		
		[Test]
		public void Insert()
		{
			ByteVectorCollection list = BuildList();
			list.Insert(1, "QUACK");
			Assert.AreEqual("ABC,QUACK,DEF,GHI", list.ToByteVector(",").ToString());
		}
		
		[Test]
		public void Contains()
		{
			ByteVectorCollection list = BuildList();
			Assert.IsTrue(list.Contains("DEF"));
			Assert.IsFalse(list.Contains("CDEFG"));
			Assert.AreEqual(2, list.ToByteVector("").Find("CDEFG"));
		}
		
		/*[Test]
		public void SortedInsert()
		{
			ByteVectorCollection list = BuildList();
			list.SortedInsert("000");
			Console.WriteLine(list.ToByteVector(",").ToString());
		}*/
	}
}
