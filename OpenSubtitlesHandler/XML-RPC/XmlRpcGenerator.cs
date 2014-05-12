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
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Xml;
namespace XmlRpcHandler
{
    /// <summary>
    /// A class for making XML-RPC requests. The requests then can be sent directly as http request.
    /// </summary>
    public class XmlRpcGenerator
    {
        /// <summary>
        /// Generate XML-RPC request using method call.
        /// </summary>
        /// <param name="method">The method call</param>
        /// <returns>The request in xml.</returns>
        public static byte[] Generate(XmlRpcMethodCall method)
        { return Generate(new XmlRpcMethodCall[] { method }); }
        /// <summary>
        /// Generate XML-RPC request using method calls array.
        /// </summary>
        /// <param name="methods">The method calls array</param>
        /// <returns>The request in utf8 xml string as buffer</returns>
        public static byte[] Generate(XmlRpcMethodCall[] methods)
        {
            if (methods == null)
                throw new Exception("No method to write !");
            if (methods.Length == 0)
                throw new Exception("No method to write !");
            // Create xml
            XmlWriterSettings sett = new XmlWriterSettings();
            sett.Indent = true;

            sett.Encoding = Encoding.UTF8;
            FileStream str = new FileStream(Path.GetTempPath() + "\\request.xml", FileMode.Create, FileAccess.Write);

            XmlWriter XMLwrt = XmlWriter.Create(str, sett);
            // Let's write the methods
            foreach (XmlRpcMethodCall method in methods)
            {
                XMLwrt.WriteStartElement("methodCall");//methodCall
                XMLwrt.WriteStartElement("methodName");//methodName
                XMLwrt.WriteString(method.Name);
                XMLwrt.WriteEndElement();//methodName
                XMLwrt.WriteStartElement("params");//params
                // Write values
                foreach (IXmlRpcValue p in method.Parameters)
                {
                    XMLwrt.WriteStartElement("param");//param
                    if (p is XmlRpcValueBasic)
                    {
                        WriteBasicValue(XMLwrt, (XmlRpcValueBasic)p);
                    }
                    else if (p is XmlRpcValueStruct)
                    {
                        WriteStructValue(XMLwrt, (XmlRpcValueStruct)p);
                    }
                    else if (p is XmlRpcValueArray)
                    {
                        WriteArrayValue(XMLwrt, (XmlRpcValueArray)p);
                    }
                    XMLwrt.WriteEndElement();//param
                }

                XMLwrt.WriteEndElement();//params
                XMLwrt.WriteEndElement();//methodCall
            }
            XMLwrt.Flush();
            XMLwrt.Close();
            str.Close();
            string requestContent = File.ReadAllText(Path.GetTempPath() + "\\request.xml");
            return Encoding.UTF8.GetBytes(requestContent);
        }
        /// <summary>
        /// Decode response then return the values
        /// </summary>
        /// <param name="xmlResponse">The response xml string as provided by server as methodResponse</param>
        /// <returns></returns>
        public static XmlRpcMethodCall[] DecodeMethodResponse(string xmlResponse)
        {
            List<XmlRpcMethodCall> methods = new List<XmlRpcMethodCall>();
            XmlReaderSettings sett = new XmlReaderSettings();
            sett.DtdProcessing = DtdProcessing.Ignore;
            sett.IgnoreWhitespace = true;
            MemoryStream str = new MemoryStream(Encoding.ASCII.GetBytes(xmlResponse));
            if (xmlResponse.Contains(@"encoding=""utf-8"""))
            {
                str = new MemoryStream(Encoding.UTF8.GetBytes(xmlResponse));
            }
            XmlReader XMLread = XmlReader.Create(str, sett);

            XmlRpcMethodCall call = new XmlRpcMethodCall("methodResponse");
            // Read parameters
            while (XMLread.Read())
            {
                if (XMLread.Name == "param" && XMLread.IsStartElement())
                {
                    IXmlRpcValue val = ReadValue(XMLread);
                    if (val != null)
                        call.Parameters.Add(val);
                }
            }
            methods.Add(call);
            XMLread.Close();
            return methods.ToArray();
        }

        private static void WriteBasicValue(XmlWriter XMLwrt, XmlRpcValueBasic val)
        {
            XMLwrt.WriteStartElement("value");//value
            switch (val.ValueType)
            {
                case XmlRpcBasicValueType.String:
                    XMLwrt.WriteStartElement("string");
                    if (val.Data != null)
                        XMLwrt.WriteString(val.Data.ToString());
                    XMLwrt.WriteEndElement();
                    break;
                case XmlRpcBasicValueType.Int:
                    XMLwrt.WriteStartElement("int");
                    if (val.Data != null)
                        XMLwrt.WriteString(val.Data.ToString());
                    XMLwrt.WriteEndElement();
                    break;
                case XmlRpcBasicValueType.Boolean:
                    XMLwrt.WriteStartElement("boolean");
                    if (val.Data != null)
                        XMLwrt.WriteString(((bool)val.Data) ? "1" : "0");
                    XMLwrt.WriteEndElement();
                    break;
                case XmlRpcBasicValueType.Double:
                    XMLwrt.WriteStartElement("double");
                    if (val.Data != null)
                        XMLwrt.WriteString(val.Data.ToString());
                    XMLwrt.WriteEndElement();
                    break;
                case XmlRpcBasicValueType.dateTime_iso8601:
                    XMLwrt.WriteStartElement("dateTime.iso8601");
                    // Get date time format
                    if (val.Data != null)
                    {
                        DateTime time = (DateTime)val.Data;
                        string dt = time.Year + time.Month.ToString("D2") + time.Day.ToString("D2") +
                            "T" + time.Hour.ToString("D2") + ":" + time.Minute.ToString("D2") + ":" +
                            time.Second.ToString("D2");
                        XMLwrt.WriteString(dt);
                    }
                    XMLwrt.WriteEndElement();
                    break;
                case XmlRpcBasicValueType.base64:
                    XMLwrt.WriteStartElement("base64");
                    if (val.Data != null)
                        XMLwrt.WriteString(Convert.ToBase64String(BitConverter.GetBytes((long)val.Data)));
                    XMLwrt.WriteEndElement();
                    break;
            }
            XMLwrt.WriteEndElement();//value
        }
        private static void WriteStructValue(XmlWriter XMLwrt, XmlRpcValueStruct val)
        {
            XMLwrt.WriteStartElement("value");//value
            XMLwrt.WriteStartElement("struct");//struct
            foreach (XmlRpcStructMember member in val.Members)
            {
                XMLwrt.WriteStartElement("member");//member

                XMLwrt.WriteStartElement("name");//name
                XMLwrt.WriteString(member.Name);
                XMLwrt.WriteEndElement();//name

                if (member.Data is XmlRpcValueBasic)
                {
                    WriteBasicValue(XMLwrt, (XmlRpcValueBasic)member.Data);
                }
                else if (member.Data is XmlRpcValueStruct)
                {
                    WriteStructValue(XMLwrt, (XmlRpcValueStruct)member.Data);
                }
                else if (member.Data is XmlRpcValueArray)
                {
                    WriteArrayValue(XMLwrt, (XmlRpcValueArray)member.Data);
                }

                XMLwrt.WriteEndElement();//member
            }
            XMLwrt.WriteEndElement();//struct
            XMLwrt.WriteEndElement();//value
        }
        private static void WriteArrayValue(XmlWriter XMLwrt, XmlRpcValueArray val)
        {
            XMLwrt.WriteStartElement("value");//value
            XMLwrt.WriteStartElement("array");//array
            XMLwrt.WriteStartElement("data");//data
            foreach (IXmlRpcValue o in val.Values)
            {
                if (o is XmlRpcValueBasic)
                {
                    WriteBasicValue(XMLwrt, (XmlRpcValueBasic)o);
                }
                else if (o is XmlRpcValueStruct)
                {
                    WriteStructValue(XMLwrt, (XmlRpcValueStruct)o);
                }
                else if (o is XmlRpcValueArray)
                {
                    WriteArrayValue(XMLwrt, (XmlRpcValueArray)o);
                }
            }
            XMLwrt.WriteEndElement();//data
            XMLwrt.WriteEndElement();//array
            XMLwrt.WriteEndElement();//value
        }
        private static IXmlRpcValue ReadValue(XmlReader xmlReader)
        {
            while (xmlReader.Read())
            {
                if (xmlReader.Name == "value" && xmlReader.IsStartElement())
                {
                    xmlReader.Read();
                    if (xmlReader.Name == "string" && xmlReader.IsStartElement())
                    {
                        return new XmlRpcValueBasic(xmlReader.ReadString(), XmlRpcBasicValueType.String);
                    }
                    else if (xmlReader.Name == "int" && xmlReader.IsStartElement())
                    {
                        return new XmlRpcValueBasic(int.Parse(xmlReader.ReadString()), XmlRpcBasicValueType.Int);
                    }
                    else if (xmlReader.Name == "boolean" && xmlReader.IsStartElement())
                    {
                        return new XmlRpcValueBasic(xmlReader.ReadString() == "1", XmlRpcBasicValueType.Boolean);
                    }
                    else if (xmlReader.Name == "double" && xmlReader.IsStartElement())
                    {
                        return new XmlRpcValueBasic(double.Parse(xmlReader.ReadString()), XmlRpcBasicValueType.Double);
                    }
                    else if (xmlReader.Name == "dateTime.iso8601" && xmlReader.IsStartElement())
                    {
                        string date = xmlReader.ReadString();
                        int year = int.Parse(date.Substring(0, 4));
                        int month = int.Parse(date.Substring(4, 2));
                        int day = int.Parse(date.Substring(6, 2));
                        int hour = int.Parse(date.Substring(9, 2));
                        int minute = int.Parse(date.Substring(12, 2));//19980717T14:08:55
                        int sec = int.Parse(date.Substring(15, 2));
                        DateTime time = new DateTime(year, month, day, hour, minute, sec);
                        return new XmlRpcValueBasic(time, XmlRpcBasicValueType.dateTime_iso8601);
                    }
                    else if (xmlReader.Name == "base64" && xmlReader.IsStartElement())
                    {
                        return new XmlRpcValueBasic(BitConverter.ToInt64(Convert.FromBase64String(xmlReader.ReadString()), 0)
                            , XmlRpcBasicValueType.Double);
                    }
                    else if (xmlReader.Name == "struct" && xmlReader.IsStartElement())
                    {
                        XmlRpcValueStruct strct = new XmlRpcValueStruct(new List<XmlRpcStructMember>());
                        // Read members...
                        while (xmlReader.Read())
                        {
                            if (xmlReader.Name == "member" && xmlReader.IsStartElement())
                            {
                                XmlRpcStructMember member = new XmlRpcStructMember("", null);
                                xmlReader.Read();// read name
                                member.Name = xmlReader.ReadString();

                                IXmlRpcValue val = ReadValue(xmlReader);
                                if (val != null)
                                {
                                    member.Data = val;
                                    strct.Members.Add(member);
                                }
                            }
                            else if (xmlReader.Name == "struct" && !xmlReader.IsStartElement())
                            {
                                return strct;
                            }
                        }
                        return strct;
                    }
                    else if (xmlReader.Name == "array" && xmlReader.IsStartElement())
                    {
                        XmlRpcValueArray array = new XmlRpcValueArray();
                        // Read members...
                        while (xmlReader.Read())
                        {
                            if (xmlReader.Name == "array" && !xmlReader.IsStartElement())
                            {
                                return array;
                            }
                            else
                            {
                                IXmlRpcValue val = ReadValue(xmlReader);
                                if (val != null)
                                    array.Values.Add(val);
                            }
                        }
                        return array;
                    }
                }
                else break;
            }
            return null;
        }
    }
}
