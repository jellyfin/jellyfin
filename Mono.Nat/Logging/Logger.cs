//
// Logger.cs
//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//
// Copyright (C) 2006 Alan McGovern
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Mono.Nat.Logging
{
    public class Logger
    {
        /// <summary>
        /// The factory method used to create new ILogger instances. The <see cref="string"/> parameter
        /// is the <see cref="Type.FullName"/> for the class the ILogger is associated with. You can
        /// return <see langword="null"/> for any class to disable logging for that class.
        /// </summary>
        public static Func<string, ILogger> Factory { get; set; }

        [MethodImpl (MethodImplOptions.NoInlining)]
        internal static Logger Create ()
        {
            var callingClassName = new StackFrame (1).GetMethod ().ReflectedType.FullName;
            var writer = Factory?.Invoke (callingClassName);
            return new Logger (writer);
        }

        ILogger Writer { get; }

        internal Logger (ILogger writer)
        {
            Writer = writer;
        }

        internal void Info (string message)
        {
            if (Writer != null)
                Writer.Info (message);
        }

        internal void InfoFormatted (string format, int p1, int p2)
        {
            if (Writer != null)
                Writer.Info (string.Format (format, p1, p2));
        }

        internal void InfoFormatted (string format, object p1)
        {
            if (Writer != null)
                Writer.Info (string.Format (format, p1));
        }

        internal void InfoFormatted (string format, object p1, object p2)
        {
            if (Writer != null)
                Writer.Info (string.Format (format, p1, p2));
        }

        internal void InfoFormatted (string format, object p1, int p2)
        {
            if (Writer != null)
                Writer.Info (string.Format (format, p1, p2));
        }

        internal void Error (string message)
        {
            if (Writer != null)
                Writer.Error (message);
        }

        internal void ErrorFormatted (string format, object p1)
        {
            if (Writer != null)
                Writer.Error (string.Format (format, p1));
        }

        internal void ErrorFormatted (string format, object p1, object p2)
        {
            if (Writer != null)
                Writer.Error (string.Format (format, p1, p2));
        }

        internal void Exception (Exception ex, string message)
        {
            if (Writer != null)
                Writer.Error (string.Format ("{0}{1}{2}", message, Environment.NewLine, ex));
        }

        internal void ExceptionFormated (Exception ex, string formatString, object p1)
        {
            if (Writer != null)
                Writer.Error (string.Format ("{0}{1}{2}", string.Format (formatString, p1), Environment.NewLine, ex));
        }
    }
}
