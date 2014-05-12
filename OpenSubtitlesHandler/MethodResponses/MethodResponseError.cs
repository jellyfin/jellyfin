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
namespace OpenSubtitlesHandler
{
    /// <summary>
    /// Response that can be used for general error like internet connection fail.
    /// </summary>
    [MethodResponseDescription("Error method response",
  "Error method response that describes error that occured")]
    public class MethodResponseError : IMethodResponse
    {
        public MethodResponseError()
            : base()
        { }
        public MethodResponseError(string name, string message)
            : base(name, message)
        { }
    }
}
