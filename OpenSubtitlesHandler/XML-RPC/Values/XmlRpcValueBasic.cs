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
    public class XmlRpcValueBasic : IXmlRpcValue
    {
        public XmlRpcValueBasic()
            : base()
        { }
        public XmlRpcValueBasic(string data)
            : base(data)
        { this.type = XmlRpcBasicValueType.String; }
        public XmlRpcValueBasic(int data)
            : base(data)
        { this.type = XmlRpcBasicValueType.Int; }
        public XmlRpcValueBasic(double data)
            : base(data)
        { this.type = XmlRpcBasicValueType.Double; }
        public XmlRpcValueBasic(DateTime data)
            : base(data)
        { this.type = XmlRpcBasicValueType.dateTime_iso8601; }
        public XmlRpcValueBasic(bool data)
            : base(data)
        { this.type = XmlRpcBasicValueType.Boolean; }
        public XmlRpcValueBasic(long data)
            : base(data)
        { this.type = XmlRpcBasicValueType.base64; }
        public XmlRpcValueBasic(object data, XmlRpcBasicValueType type)
            : base(data)
        { this.type = type; }

        private XmlRpcBasicValueType type = XmlRpcBasicValueType.String;
        /// <summary>
        /// Get or set the type of this basic value
        /// </summary>
        public XmlRpcBasicValueType ValueType { get { return type; } set { type = value; } }
        /*Oprators. help a lot.*/
        public static implicit operator string(XmlRpcValueBasic f)
        {
            if (f.type == XmlRpcBasicValueType.String)
                return f.Data.ToString();
            else
                throw new Exception("Unable to convert, this value is not string type.");
        }
        public static implicit operator int(XmlRpcValueBasic f)
        {
            if (f.type == XmlRpcBasicValueType.String)
                return (int)f.Data;
            else
                throw new Exception("Unable to convert, this value is not int type.");
        }
        public static implicit operator double(XmlRpcValueBasic f)
        {
            if (f.type == XmlRpcBasicValueType.String)
                return (double)f.Data;
            else
                throw new Exception("Unable to convert, this value is not double type.");
        }
        public static implicit operator bool(XmlRpcValueBasic f)
        {
            if (f.type == XmlRpcBasicValueType.String)
                return (bool)f.Data;
            else
                throw new Exception("Unable to convert, this value is not bool type.");
        }
        public static implicit operator long(XmlRpcValueBasic f)
        {
            if (f.type == XmlRpcBasicValueType.String)
                return (long)f.Data;
            else
                throw new Exception("Unable to convert, this value is not long type.");
        }
        public static implicit operator DateTime(XmlRpcValueBasic f)
        {
            if (f.type == XmlRpcBasicValueType.String)
                return (DateTime)f.Data;
            else
                throw new Exception("Unable to convert, this value is not DateTime type.");
        }
    }
}
