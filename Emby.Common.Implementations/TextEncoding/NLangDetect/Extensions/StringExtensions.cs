using System;

namespace NLangDetect.Core.Extensions
{
  public static class StringExtensions
  {
    /// <summary>
    /// Returns a new character sequence that is a subsequence of this sequence. The subsequence starts with the character at the specified index and ends with the character at index end - 1. The length of the returned sequence is end - start, so if start == end then an empty sequence is returned.
    /// </summary>
    /// <param name="s"></param>
    /// <param name="start">the start index, inclusive</param>
    /// <param name="end">the end index, exclusive</param>
    /// <returns>the specified subsequence</returns>
    /// <exception cref="IndexOutOfRangeException"> if start or end are negative, if end is greater than length(), or if start is greater than end</exception>
    public static string SubSequence(this string s, int start, int end)
    {
      if (start < 0) throw new ArgumentOutOfRangeException("start", "Argument must not be negative.");
      if (end < 0) throw new ArgumentOutOfRangeException("end", "Argument must not be negative.");
      if (end > s.Length) throw new ArgumentOutOfRangeException("end", "Argument must not be greater than the input string's length.");
      if (start > end) throw new ArgumentOutOfRangeException("start", "Argument must not be greater than the 'end' argument.");
      
      return s.Substring(start, end - start);
    }
  }
}
