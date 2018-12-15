using System;
using NUnit.Framework;
using TagLib.IFD.Entries;

namespace TagLib
{

	[TestFixture]
	public class RationalTest
	{
		[Test]
		public void Rational1 ()
		{
			Rational r1 = new Rational (5, 3);

			Assert.AreEqual (5, r1.Numerator);
			Assert.AreEqual (3, r1.Denominator);
			Assert.AreEqual (5.0d/3.0d, (double) r1);
			Assert.AreEqual ("5/3", r1.ToString ());

			Assert.AreEqual (5, r1.Reduce ().Numerator);
			Assert.AreEqual (3, r1.Reduce ().Denominator);
		}

		[Test]
		public void Rational2 ()
		{
			Rational r2 = new Rational (48, 18);

			Assert.AreEqual (48, r2.Numerator);
			Assert.AreEqual (18, r2.Denominator);
			Assert.AreEqual (48.0d/18.0d, (double) r2);
			Assert.AreEqual ("8/3", r2.ToString ());

			Assert.AreEqual (8, r2.Reduce ().Numerator);
			Assert.AreEqual (3, r2.Reduce ().Denominator);
		}

		[Test]
		public void Rational3 ()
		{
			Rational r3 = new Rational (0, 17);

			Assert.AreEqual (0, r3.Numerator);
			Assert.AreEqual (17, r3.Denominator);
			Assert.AreEqual (0.0d/17.0d, (double) r3);
			Assert.AreEqual ("0/1", r3.ToString ());

			Assert.AreEqual (0, r3.Reduce ().Numerator);
			Assert.AreEqual (1, r3.Reduce ().Denominator);
		}

		[Test]
		public void SRational1 ()
		{
			SRational r1 = new SRational (5, 3);

			Assert.AreEqual (5, r1.Numerator);
			Assert.AreEqual (3, r1.Denominator);
			Assert.AreEqual (5.0d/3.0d, (double) r1);
			Assert.AreEqual ("5/3", r1.ToString ());

			Assert.AreEqual (5, r1.Reduce ().Numerator);
			Assert.AreEqual (3, r1.Reduce ().Denominator);
		}

		[Test]
		public void SRational2 ()
		{
			SRational r2 = new SRational (48, 18);

			Assert.AreEqual (48, r2.Numerator);
			Assert.AreEqual (18, r2.Denominator);
			Assert.AreEqual (48.0d/18.0d, (double) r2);
			Assert.AreEqual ("8/3", r2.ToString ());

			Assert.AreEqual (8, r2.Reduce ().Numerator);
			Assert.AreEqual (3, r2.Reduce ().Denominator);
		}

		[Test]
		public void SRational3 ()
		{
			SRational r3 = new SRational (0, -17);

			Assert.AreEqual (0, r3.Numerator);
			Assert.AreEqual (-17, r3.Denominator);
			Assert.AreEqual (0.0d/-17.0d, (double) r3);
			Assert.AreEqual ("0/1", r3.ToString ());

			Assert.AreEqual (0, r3.Reduce ().Numerator);
			Assert.AreEqual (1, r3.Reduce ().Denominator);
		}

		[Test]
		public void SRational4 ()
		{
			SRational r4 = new SRational (-108, -46);

			Assert.AreEqual (-108, r4.Numerator);
			Assert.AreEqual (-46, r4.Denominator);
			Assert.AreEqual (-108.0d/-46.0d, (double) r4);
			Assert.AreEqual ("54/23", r4.ToString ());

			Assert.AreEqual (54, r4.Reduce ().Numerator);
			Assert.AreEqual (23, r4.Reduce ().Denominator);
		}

		[Test]
		public void SRational5 ()
		{
			SRational r5 = new SRational (-256, 96);

			Assert.AreEqual (-256, r5.Numerator);
			Assert.AreEqual (96, r5.Denominator);
			Assert.AreEqual (-256.0d/96.0d, (double) r5);
			Assert.AreEqual ("-8/3", r5.ToString ());

			Assert.AreEqual (-8, r5.Reduce ().Numerator);
			Assert.AreEqual (3, r5.Reduce ().Denominator);
		}
	}
}
