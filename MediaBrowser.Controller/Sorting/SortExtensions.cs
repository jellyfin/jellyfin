using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaBrowser.Controller.Sorting
{
    public static class SortExtensions
    {
        public static IEnumerable<T> OrderByString<T>(this IEnumerable<T> list, Func<T, string> getName)
        {
            return list.OrderBy(getName, new AlphanumComparator());
        }

        public static IEnumerable<T> OrderByStringDescending<T>(this IEnumerable<T> list, Func<T, string> getName)
        {
            return list.OrderByDescending(getName, new AlphanumComparator());
        }

        public static IOrderedEnumerable<T> ThenByString<T>(this IOrderedEnumerable<T> list, Func<T, string> getName)
        {
            return list.ThenBy(getName, new AlphanumComparator());
        }

        public static IOrderedEnumerable<T> ThenByStringDescending<T>(this IOrderedEnumerable<T> list, Func<T, string> getName)
        {
            return list.ThenByDescending(getName, new AlphanumComparator());
        }

        private class AlphanumComparator : IComparer<string>
        {
            private enum ChunkType { Alphanumeric, Numeric };

            private static bool InChunk(char ch, char otherCh)
            {
                var type = ChunkType.Alphanumeric;

                if (char.IsDigit(otherCh))
                {
                    type = ChunkType.Numeric;
                }

                if ((type == ChunkType.Alphanumeric && char.IsDigit(ch))
                    || (type == ChunkType.Numeric && !char.IsDigit(ch)))
                {
                    return false;
                }

                return true;
            }

            public static int CompareValues(string s1, string s2)
            {
                if (s1 == null || s2 == null)
                {
                    return 0;
                }

                int thisMarker = 0, thisNumericChunk = 0;
                int thatMarker = 0, thatNumericChunk = 0;

                while ((thisMarker < s1.Length) || (thatMarker < s2.Length))
                {
                    if (thisMarker >= s1.Length)
                    {
                        return -1;
                    }
                    else if (thatMarker >= s2.Length)
                    {
                        return 1;
                    }
                    char thisCh = s1[thisMarker];
                    char thatCh = s2[thatMarker];

                    StringBuilder thisChunk = new StringBuilder();
                    StringBuilder thatChunk = new StringBuilder();

                    while ((thisMarker < s1.Length) && (thisChunk.Length == 0 || InChunk(thisCh, thisChunk[0])))
                    {
                        thisChunk.Append(thisCh);
                        thisMarker++;

                        if (thisMarker < s1.Length)
                        {
                            thisCh = s1[thisMarker];
                        }
                    }

                    while ((thatMarker < s2.Length) && (thatChunk.Length == 0 || InChunk(thatCh, thatChunk[0])))
                    {
                        thatChunk.Append(thatCh);
                        thatMarker++;

                        if (thatMarker < s2.Length)
                        {
                            thatCh = s2[thatMarker];
                        }
                    }

                    int result = 0;
                    // If both chunks contain numeric characters, sort them numerically
                    if (char.IsDigit(thisChunk[0]) && char.IsDigit(thatChunk[0]))
                    {
                        if (!int.TryParse(thisChunk.ToString(), out thisNumericChunk))
                        {
                            return 0;
                        }
                        if (!int.TryParse(thatChunk.ToString(), out thatNumericChunk))
                        {
                            return 0;
                        }

                        if (thisNumericChunk < thatNumericChunk)
                        {
                            result = -1;
                        }

                        if (thisNumericChunk > thatNumericChunk)
                        {
                            result = 1;
                        }
                    }
                    else
                    {
                        result = thisChunk.ToString().CompareTo(thatChunk.ToString());
                    }

                    if (result != 0)
                    {
                        return result;
                    }
                }

                return 0;
            }

            public int Compare(string x, string y)
            {
                return CompareValues(x, y);
            }
        }
    }
}
