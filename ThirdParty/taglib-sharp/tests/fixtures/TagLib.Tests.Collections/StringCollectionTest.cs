using System;
using NUnit.Framework;
using TagLib;

namespace TagLib.Tests.Collections
{   
	[TestFixture]
	public class StringCollectionTest
	{   
		private static StringCollection BuildList()
		{
			StringCollection list = new StringCollection();
			list.Add("ABC");
			list.Add("DEF");
			list.Add("GHI");
			return list;
		}
	
		[Test]
		public void Add()
		{
			Assert.AreEqual("ABC:DEF:GHI", BuildList().ToString(":"));
		}
		
		[Test]
		public void Remove()
		{
			StringCollection list = BuildList();
			list.Remove("DEF");
			Assert.AreEqual("ABCGHI", list.ToString(String.Empty));
		}
		
		[Test]
		public void Insert()
		{
			StringCollection list = BuildList();
			list.Insert(1, "QUACK");
			Assert.AreEqual("ABC,QUACK,DEF,GHI", list.ToString(","));
		}
		
		[Test]
		public void Contains()
		{
			StringCollection list = BuildList();
			Assert.IsTrue(list.Contains("DEF"));
			Assert.IsFalse(list.Contains("CDEFG"));
		}
	}
}
