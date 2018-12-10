using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageMagickSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace ImageMagickSharp.Tests
{
	[TestClass()]
	public class WandTests : BaseTest
	{
		[TestMethod()]
		public void QueryFormatsTest()
		{
			//Wand.QueryFonts("*");
			Wand.QueryFormats("*");
		}

		[TestMethod()]
		public void OpenEnvironmentTest()
		{
			Wand.OpenEnvironment();

			Assert.IsTrue(Wand.IsInitialized);
		}

		[TestMethod()]
		public void CloseEnvironmentTest()
		{
			Wand.CloseEnvironment();

			Assert.IsFalse(Wand.IsInitialized);
		}

		[TestMethod()]
		public void EnsureInitializedTest()
		{
			Wand.OpenEnvironment();

			Assert.IsTrue(Wand.IsInitialized);
		}

		[TestMethod()]
		public void IsMagickWandInstantiatedTest()
		{
			Wand.OpenEnvironment();
			Assert.IsTrue(Wand.IsWandInstantiated);
			Wand.CloseEnvironment();
			Assert.IsFalse(Wand.IsWandInstantiated);
		}
	}
}
