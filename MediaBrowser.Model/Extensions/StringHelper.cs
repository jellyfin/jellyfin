using System;
using System.Globalization;

namespace MediaBrowser.Model.Extensions
{
    /// <summary>
    /// Isolating these helpers allow this entire project to be easily converted to Java
    /// </summary>
    public static class StringHelper
    {
        /// <summary>
        /// Equalses the ignore case.
        /// </summary>
        /// <param name="str1">The STR1.</param>
        /// <param name="str2">The STR2.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool EqualsIgnoreCase(string str1, string str2)
        {
            return string.Equals(str1, str2, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Indexes the of ignore case.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="value">The value.</param>
        /// <returns>System.Int32.</returns>
        public static int IndexOfIgnoreCase(string str, string value)
        {
            return str.IndexOf(value, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// To the string culture invariant.
        /// </summary>
        /// <param name="val">The value.</param>
        /// <returns>System.String.</returns>
        public static string ToStringCultureInvariant(int val)
        {
            return val.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// To the string culture invariant.
        /// </summary>
        /// <param name="val">The value.</param>
        /// <returns>System.String.</returns>
        public static string ToStringCultureInvariant(long val)
        {
            return val.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// To the string culture invariant.
        /// </summary>
        /// <param name="val">The value.</param>
        /// <returns>System.String.</returns>
        public static string ToStringCultureInvariant(double val)
        {
            return val.ToString(CultureInfo.InvariantCulture);
        }
    }
}
