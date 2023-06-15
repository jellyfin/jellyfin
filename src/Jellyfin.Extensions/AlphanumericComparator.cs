using System;
using System.Collections.Generic;

namespace Jellyfin.Extensions
{
    /// <summary>
    /// Alphanumeric <see cref="IComparer{T}" />.
    /// </summary>
    public class AlphanumericComparator : IComparer<string?>
    {
        /// <summary>
        /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
        /// </summary>
        /// <param name="s1">The first object to compare.</param>
        /// <param name="s2">The second object to compare.</param>
        /// <returns>A signed integer that indicates the relative values of <c>x</c> and <c>y</c>.</returns>
        public static int CompareValues(string? s1, string? s2)
        {
            if (s1 is null && s2 is null)
            {
                return 0;
            }

            if (s1 is null)
            {
                return -1;
            }

            if (s2 is null)
            {
                return 1;
            }

            int len1 = s1.Length;
            int len2 = s2.Length;

            // Early return for empty strings
            if (len1 == 0 && len2 == 0)
            {
                return 0;
            }

            if (len1 == 0)
            {
                return -1;
            }

            if (len2 == 0)
            {
                return 1;
            }

            int pos1 = 0;
            int pos2 = 0;

            do
            {
                int start1 = pos1;
                int start2 = pos2;

                bool isNum1 = char.IsDigit(s1[pos1++]);
                bool isNum2 = char.IsDigit(s2[pos2++]);

                while (pos1 < len1 && char.IsDigit(s1[pos1]) == isNum1)
                {
                    pos1++;
                }

                while (pos2 < len2 && char.IsDigit(s2[pos2]) == isNum2)
                {
                    pos2++;
                }

                var span1 = s1.AsSpan(start1, pos1 - start1);
                var span2 = s2.AsSpan(start2, pos2 - start2);

                if (isNum1 && isNum2)
                {
                    // Trim leading zeros so we can compare the length
                    // of the strings to find the largest number
                    span1 = span1.TrimStart('0');
                    span2 = span2.TrimStart('0');
                    var span1Len = span1.Length;
                    var span2Len = span2.Length;
                    if (span1Len < span2Len)
                    {
                        return -1;
                    }

                    if (span1Len > span2Len)
                    {
                        return 1;
                    }
                }

                int result = span1.CompareTo(span2, StringComparison.InvariantCulture);
                if (result != 0)
                {
                    return result;
                }
            } while (pos1 < len1 && pos2 < len2);

            return len1 - len2;
        }

        /// <inheritdoc />
        public int Compare(string? x, string? y)
        {
            return CompareValues(x, y);
        }
    }
}
