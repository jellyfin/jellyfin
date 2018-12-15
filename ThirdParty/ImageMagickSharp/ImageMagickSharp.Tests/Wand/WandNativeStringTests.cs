using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageMagickSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
namespace ImageMagickSharp.Tests
{
	[TestClass()]
	public class WandNativeStringTests : BaseTest
	{
		string stringTest = "WandNativeString";

		[TestMethod()]
		public void LoadTest()
		{
			string val = WandNativeString.Load(Wand.GetHandle(), false);
			Debug.WriteLine(val);
			Assert.IsNotNull(val);
		}
	}
}
