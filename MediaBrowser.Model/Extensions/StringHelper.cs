namespace MediaBrowser.Model.Extensions
{
    /// <summary>
    /// Helper methods for manipulating strings.
    /// </summary>
    public static class StringHelper
    {
        /// <summary>
        /// Returns the string with the first character as uppercase.
        /// </summary>
        /// <param name="str">The input string.</param>
        /// <returns>The string with the first character as uppercase.</returns>
        public static string FirstToUpper(string str)
        {
            if (str.Length == 0)
            {
                return str;
            }

            if (char.IsUpper(str[0]))
            {
                return str;
            }

#if NETSTANDARD2_0
            char[] a = str.ToCharArray();
            a[0] = char.ToUpperInvariant(a[0]);
            return new string(a);
#else
            return string.Create(
                str.Length,
                str,
                (chars, buf) =>
                {
                    chars[0] = char.ToUpperInvariant(buf[0]);
                    for (int i = 1; i < chars.Length; i++)
                    {
                        chars[i] = buf[i];
                    }
                });
#endif
        }
    }
}
