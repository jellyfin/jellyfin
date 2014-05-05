/* This file is part of OpenSubtitles Handler
   A library that handle OpenSubtitles.org XML-RPC methods.

   Copyright © Ala Ibrahim Hadid 2013

   This program is free software: you can redistribute it and/or modify
   it under the terms of the GNU General Public License as published by
   the Free Software Foundation, either version 3 of the License, or
   (at your option) any later version.

   This program is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
   GNU General Public License for more details.

   You should have received a copy of the GNU General Public License
   along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace OpenSubtitlesHandler.Console
{
    public class OSHConsole
    {
        /// <summary>
        /// Write line to the console and raise the "LineWritten" event
        /// </summary>
        /// 
        /// <param name="text">The debug line</param>
        /// <param name="code">The status</param>
        public static void WriteLine(string text, DebugCode code = DebugCode.None)
        {
            if (LineWritten != null)
                LineWritten(null, new DebugEventArgs(text, code));
        }
        /// <summary>
        /// Update the last written line
        /// </summary>
        /// <param name="text">The debug line</param>
        /// <param name="code">The status</param>
        public static void UpdateLine(string text, DebugCode code = DebugCode.None)
        {
            if (UpdateLastLine != null)
                UpdateLastLine(null, new DebugEventArgs(text, code));
        }

        public static event EventHandler<DebugEventArgs> LineWritten;
        public static event EventHandler<DebugEventArgs> UpdateLastLine;
    }
    public enum DebugCode
    {
        None,
        Good,
        Warning,
        Error
    }
    /// <summary>
    /// Console Debug Args
    /// </summary>
    public class DebugEventArgs : System.EventArgs
    {
        public DebugCode Code { get; private set; }
        public string Text { get; private set; }

        /// <summary>
        /// Console Debug Args
        /// </summary>
        /// <param name="text">The debug line</param>
        /// <param name="code">The status</param>
        public DebugEventArgs(string text, DebugCode code)
        {
            this.Text = text;
            this.Code = code;
        }
    }
    public struct DebugLine
    {
        public DebugLine(string debugLine, DebugCode status)
        {
            this.debugLine = debugLine;
            this.status = status;
        }
        string debugLine;
        DebugCode status;

        public string Text
        { get { return debugLine; } set { debugLine = value; } }
        public DebugCode Code
        { get { return status; } set { status = value; } }
    }
}
