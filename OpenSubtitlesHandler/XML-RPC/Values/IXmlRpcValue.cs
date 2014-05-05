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

namespace XmlRpcHandler
{
    public abstract class IXmlRpcValue
    {
        public IXmlRpcValue()
        { }
        public IXmlRpcValue(object data)
        {
            this.data = data;
        }
        private object data;
        /// <summary>
        /// Get or set the data of this value
        /// </summary>
        public virtual object Data { get { return data; } set { data = value; } }
    }
}
