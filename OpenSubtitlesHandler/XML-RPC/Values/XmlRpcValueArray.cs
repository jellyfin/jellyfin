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
    public class XmlRpcValueArray : IXmlRpcValue
    {
        public XmlRpcValueArray() :
            base()
        {
            values = new List<IXmlRpcValue>();
        }
        public XmlRpcValueArray(object data) :
            base(data)
        {
            values = new List<IXmlRpcValue>();
        }
        public XmlRpcValueArray(string[] texts) :
            base()
        {
            values = new List<IXmlRpcValue>();
            foreach (string val in texts)
            {
                values.Add(new XmlRpcValueBasic(val));
            }
        }
        public XmlRpcValueArray(int[] ints) :
            base()
        {
            values = new List<IXmlRpcValue>();
            foreach (int val in ints)
            {
                values.Add(new XmlRpcValueBasic(val));
            }
        }
        public XmlRpcValueArray(double[] doubles) :
            base()
        {
            values = new List<IXmlRpcValue>();
            foreach (double val in doubles)
            {
                values.Add(new XmlRpcValueBasic(val));
            }
        }
        public XmlRpcValueArray(bool[] bools) :
            base()
        {
            values = new List<IXmlRpcValue>();
            foreach (bool val in bools)
            {
                values.Add(new XmlRpcValueBasic(val));
            }
        }
        public XmlRpcValueArray(long[] base24s) :
            base()
        {
            values = new List<IXmlRpcValue>();
            foreach (long val in base24s)
            {
                values.Add(new XmlRpcValueBasic(val));
            }
        }
        public XmlRpcValueArray(DateTime[] dates) :
            base()
        {
            values = new List<IXmlRpcValue>();
            foreach (DateTime val in dates)
            {
                values.Add(new XmlRpcValueBasic(val));
            }
        }
        public XmlRpcValueArray(XmlRpcValueBasic[] basicValues) :
            base()
        {
            values = new List<IXmlRpcValue>();
            foreach (XmlRpcValueBasic val in basicValues)
            {
                values.Add(val);
            }
        }
        public XmlRpcValueArray(XmlRpcValueStruct[] structs) :
            base()
        {
            values = new List<IXmlRpcValue>();
            foreach (XmlRpcValueStruct val in structs)
            {
                values.Add(val);
            }
        }
        public XmlRpcValueArray(XmlRpcValueArray[] arrays) :
            base()
        {
            values = new List<IXmlRpcValue>();
            foreach (XmlRpcValueArray val in arrays)
            {
                values.Add(val);
            }
        }
        private List<IXmlRpcValue> values;

        public List<IXmlRpcValue> Values { get { return values; } set { values = value; } }
    }
}
