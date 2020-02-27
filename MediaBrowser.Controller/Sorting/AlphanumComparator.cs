using System.Collections.Generic;
using System.Text;
using MediaBrowser.Controller.Sorting;

namespace MediaBrowser.Controller.Sorting
{
    public class AlphanumComparator : IComparer<string>
    {
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

                var thisChunk = new StringBuilder();
                var thatChunk = new StringBuilder();
                bool thisNumeric = char.IsDigit(thisCh), thatNumeric = char.IsDigit(thatCh);

                while ((thisMarker < s1.Length) && (char.IsDigit(thisCh) == thisNumeric))
                {
                    thisChunk.Append(thisCh);
                    thisMarker++;

                    if (thisMarker < s1.Length)
                    {
                        thisCh = s1[thisMarker];
                    }
                }

                while ((thatMarker < s2.Length) && (char.IsDigit(thatCh) == thatNumeric))
                {
                    thatChunk.Append(thatCh);
                    thatMarker++;

                    if (thatMarker < s2.Length)
                    {
                        thatCh = s2[thatMarker];
                    }
                }


                // If both chunks contain numeric characters, sort them numerically
                if (thisNumeric && thatNumeric)
                {
                    if (!int.TryParse(thisChunk.ToString(), out thisNumericChunk)
                        || !int.TryParse(thatChunk.ToString(), out thatNumericChunk))
                    {
                        return 0;
                    }

                    if (thisNumericChunk < thatNumericChunk)
                    {
                        return -1;
                    }

                    if (thisNumericChunk > thatNumericChunk)
                    {
                        return 1;
                    }
                }
                else
                {
                    int result = thisChunk.ToString().CompareTo(thatChunk.ToString());
                    if (result != 0)
                    {
                        return result;
                    }
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
