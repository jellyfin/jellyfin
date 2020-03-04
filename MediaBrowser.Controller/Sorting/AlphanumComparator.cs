#nullable enable

using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Sorting
{
    public class AlphanumComparator : IComparer<string?>
    {
        public static int CompareValues(string? s1, string? s2)
        {
            if (s1 == null && s2 == null)
            {
                return 0;
            }
            else if (s1 == null)
            {
                return -1;
            }
            else if (s2 == null)
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
            else if (len1 == 0)
            {
                return -1;
            }
            else if (len2 == 0)
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
                    else if (span1Len > span2Len)
                    {
                        return 1;
                    }
                    else if (span1Len >= 20) // Number is probably too big for a ulong
                    {
                        // Trim all the first digits that are the same
                        int i = 0;
                        while (i < span1Len && span1[i] == span2[i])
                        {
                            i++;
                        }

                        // If there are no more digits it's the same number
                        if (i == span1Len)
                        {
                            continue;
                        }

                        // Only need to compare the most significant digit
                        span1 = span1.Slice(i, 1);
                        span2 = span2.Slice(i, 1);
                    }

                    if (!ulong.TryParse(span1, out var num1)
                        || !ulong.TryParse(span2, out var num2))
                    {
                        return 0;
                    }
                    else if (num1 < num2)
                    {
                        return -1;
                    }
                    else if (num1 > num2)
                    {
                        return 1;
                    }
                }
                else
                {
                    int result = span1.CompareTo(span2, StringComparison.InvariantCulture);
                    if (result != 0)
                    {
                        return result;
                    }
                }
            } while (pos1 < len1 && pos2 < len2);

            return len1 - len2;
        }

        /// <inheritdoc />
        public int Compare(string x, string y)
        {
            return CompareValues(x, y);
        }
    }
}
