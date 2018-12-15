//
// SRational.cs: A structure to represent signed rational values by a
// numerator and a denominator.
//
// Author:
//   Mike Gemuende (mike@gemuende.de)
//
// Copyright (C) 2009 Ruben Vermeersch
// Copyright (C) 2009 Mike Gemuende
//
// This library is free software; you can redistribute it and/or modify
// it  under the terms of the GNU Lesser General Public License version
// 2.1 as published by the Free Software Foundation.
//
// This library is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307
// USA
//

using System;

namespace TagLib.IFD.Entries
{

	/// <summary>
	///    Representation of a signed rational value
	/// </summary>
	public struct SRational : IFormattable
	{
#region Private Fields

		/// <summary>
		///    The numerator of the rational value
		/// </summary>
		private int numerator;

		/// <summary>
		///    The denominator of the rational value
		/// </summary>
		private int denominator;

#endregion

#region Constructor

		/// <summary>
		///    Creates a new Rational value
		/// </summary>
		/// <param name="numerator">
		///    A <see cref="System.Int32"/> with the numerator of the
		///    rational value
		/// </param>
		/// <param name="denominator">
		///    A <see cref="System.Int32"/> with the denominator of the
		///    rational value. It must be not 0.
		/// </param>
		public SRational (int numerator, int denominator)
		{
			if (denominator == 0)
				throw new ArgumentException ("denominator");
			this.numerator = numerator;
			this.denominator = denominator;
		}

#endregion

#region Public Methods

		/// <summary>
		///    Returns a rational value with reduced nominator and denominator
		/// </summary>
		/// <returns>
		///    A <see cref="SRational"/>
		/// </returns>
		public SRational Reduce ()
		{
			int den_sign = Math.Sign (Denominator);
			int gcd = Math.Abs (Denominator);
			int b = Math.Abs (Numerator);

			while (b != 0) {
				int tmp = gcd % b;
				gcd = b;
				b = tmp;
			}

			return new SRational (den_sign * (Numerator / gcd), Math.Abs (Denominator) / gcd);
		}

		/// <summary>
		///    Formatprovider to allow formatting of a value. <see cref="IFormattable"/>
		/// </summary>
		/// <param name="format">
		///    A <see cref="System.String"/>. <see cref="IFormattable"/>
		/// </param>
		/// <param name="provider">
		///    A <see cref="IFormatProvider"/>. <see cref="IFormattable"/>
		/// </param>
		/// <returns>
		///    A <see cref="System.String"/> formated according to the given parameter
		/// </returns>
		public string ToString (string format, IFormatProvider provider) {

			SRational reduced = Reduce ();

			return String.Format ("{0}/{1}", reduced.Numerator, reduced.Denominator);
		}

		/// <summary>
		///    Converts the value to a <see cref="System.String"/>.
		/// </summary>
		/// <returns>
		///    A <see cref="System.String"/> with the current value.
		/// </returns>
		public override string ToString ()
		{
			return String.Format ("{0}", this);
		}

#endregion

#region Public Properties

		/// <value>
		///    The numerator of the rational value
		/// </value>
		public int Numerator {
			get { return numerator; }
			set { numerator = value; }
		}

		/// <value>
		///    The denominator of the rational value
		/// </value>
		/// <remarks>
		///    Cannot be 0.
		/// </remarks>
		public int Denominator {
			get { return denominator; }
			set {
				if (value == 0)
					throw new ArgumentException ("denominator");

				denominator = value;
			}
		}

#endregion

#region Public Static Methods

		/// <summary>
		///    Cast the <see cref="Rational"/> value to a <see cref="System.Double"/>.
		/// </summary>
		/// <param name="rat">
		///    A <see cref="Rational"/> with the value to cast.
		/// </param>
		/// <returns>
		///    A <see cref="System.Double"/> with the double.
		/// </returns>
		public static implicit operator double (SRational rat)
		{
			return (double) rat.Numerator / (double) rat.Denominator;
		}

#endregion

	}
}
