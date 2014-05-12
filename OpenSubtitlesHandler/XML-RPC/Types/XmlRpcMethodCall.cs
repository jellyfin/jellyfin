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
using System.Collections.Generic;

namespace XmlRpcHandler
{
    /// <summary>
    /// A method call
    /// </summary>
    public struct XmlRpcMethodCall
    {
        /// <summary>
        /// A method call
        /// </summary>
        /// <param name="name">The name of this method</param>
        public XmlRpcMethodCall(string name)
        {
            this.name = name;
            this.parameters = new List<IXmlRpcValue>();
        }
        /// <summary>
        /// A method call
        /// </summary>
        /// <param name="name">The name of this method</param>
        /// <param name="parameters">A list of parameters</param>
        public XmlRpcMethodCall(string name, List<IXmlRpcValue> parameters)
        {
            this.name = name;
            this.parameters = parameters;
        }

        private string name;
        private List<IXmlRpcValue> parameters;

        /// <summary>
        /// Get or set the name of this method
        /// </summary>
        public string Name
        { get { return name; } set { name = value; } }
        /// <summary>
        /// Get or set the parameters to be sent
        /// </summary>
        public List<IXmlRpcValue> Parameters
        { get { return parameters; } set { parameters = value; } }
    }
}
