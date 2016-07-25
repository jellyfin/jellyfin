using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

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

        /// <summary>
        /// Trims the start.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="c">The c.</param>
        /// <returns>System.String.</returns>
        public static string TrimStart(string str, char c)
        {
            return str.TrimStart(c);
        }

        /// <summary>
        /// Splits the specified string.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="term">The term.</param>
        /// <returns>System.String[].</returns>
        public static string[] RegexSplit(string str, string term)
        {
            return Regex.Split(str, term, RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Splits the specified string.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="term">The term.</param>
        /// <param name="limit">The limit.</param>
        /// <returns>System.String[].</returns>
        public static string[] RegexSplit(string str, string term, int limit)
        {
            return new Regex(term).Split(str, limit);
        }

        /// <summary>
        /// Replaces the specified STR.
        /// </summary>
        /// <param name="str">The STR.</param>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        /// <param name="comparison">The comparison.</param>
        /// <returns>System.String.</returns>
        public static string Replace(this string str, string oldValue, string newValue, StringComparison comparison)
        {
            var sb = new StringBuilder();

            var previousIndex = 0;
            var index = str.IndexOf(oldValue, comparison);

            while (index != -1)
            {
                sb.Append(str.Substring(previousIndex, index - previousIndex));
                sb.Append(newValue);
                index += oldValue.Length;

                previousIndex = index;
                index = str.IndexOf(oldValue, index, comparison);
            }

            sb.Append(str.Substring(previousIndex));

            return sb.ToString();
        }

        public static string FirstToUpper(this string str)
        {
            return string.IsNullOrEmpty(str) ? "" : str.Substring(0, 1).ToUpper() + str.Substring(1);
        }
    }
}
