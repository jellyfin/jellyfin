/*
MIT License

Copyright (c) 2019 Gérald Barré

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 */
#nullable enable
#pragma warning disable CS1591
#pragma warning disable CA1034
using System;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;

namespace MediaBrowser.Common.Extensions
{
    /// <summary>
    /// Extension class for splitting lines without unnecessary allocations.
    /// </summary>
    public static class SplitStringExtensions
    {
        /// <summary>
        /// Creates a new string split enumerator.
        /// </summary>
        /// <param name="str">The string to split.</param>
        /// <param name="separator">The separator to split on.</param>
        /// <returns>The enumerator struct.</returns>
        [Pure]
        public static SplitEnumerator SpanSplit(this string str, char separator) => new (str.AsSpan(), separator);

        /// <summary>
        /// Creates a new span split enumerator.
        /// </summary>
        /// <param name="str">The span to split.</param>
        /// <param name="separator">The separator to split on.</param>
        /// <returns>The enumerator struct.</returns>
        [Pure]
        public static SplitEnumerator Split(this ReadOnlySpan<char> str, char separator) => new (str, separator);

        [StructLayout(LayoutKind.Auto)]
        public ref struct SplitEnumerator
        {
            private readonly char _separator;
            private ReadOnlySpan<char> _str;

            public SplitEnumerator(ReadOnlySpan<char> str, char separator)
            {
                _str = str;
                _separator = separator;
                Current = default;
            }

            public ReadOnlySpan<char> Current { get; private set; }

            public readonly SplitEnumerator GetEnumerator() => this;

            public bool MoveNext()
            {
                if (_str.Length == 0)
                {
                    return false;
                }

                var span = _str;
                var index = span.IndexOf(_separator);
                if (index == -1)
                {
                    _str = ReadOnlySpan<char>.Empty;
                    Current = span;
                    return true;
                }

                Current = span.Slice(0, index);
                _str = span[(index + 1)..];
                return true;
            }
        }
    }
}
