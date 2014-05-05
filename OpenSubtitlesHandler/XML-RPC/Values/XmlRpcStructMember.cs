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
    public class XmlRpcStructMember
    {
        public XmlRpcStructMember(string name, IXmlRpcValue data)
        {
            this.name = name;
            this.data = data;
        }
        private string name;
        private IXmlRpcValue data;

        /// <summary>
        /// Get or set the name of this member
        /// </summary>
        public string Name
        { get { return name; } set { name = value; } }
        /// <summary>
        /// Get or set the data of this member
        /// </summary>
        public IXmlRpcValue Data
        { get { return data; } set { data = value; } }
    }
}
