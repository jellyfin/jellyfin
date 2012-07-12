using System;
using System.Globalization;
using System.Xml;

namespace MediaBrowser.Controller.Xml
{
    public static class XmlExtensions
    {
        public static int SafeGetInt32(this XmlNode node)
        {
            return SafeGetInt32(node, 0);
        }

        public static int SafeGetInt32(this XmlNode node, int defaultInt)
        {
            if (node != null && node.InnerText.Length > 0)
            {
                int rval;
                if (Int32.TryParse(node.InnerText, out rval))
                {
                    return rval;
                }

            }
            return defaultInt;
        }

        private static CultureInfo _usCulture = new CultureInfo("en-US");

        public static float SafeGetSingle(this XmlNode rvalNode, float minValue, float maxValue)
        {
            if (rvalNode.InnerText.Length > 0)
            {
                float rval;
                // float.TryParse is local aware, so it can be probamatic, force us culture
                if (float.TryParse(rvalNode.InnerText, NumberStyles.AllowDecimalPoint, _usCulture, out rval))
                {
                    if (rval >= minValue && rval <= maxValue)
                    {
                        return rval;
                    }
                }

            }
            return minValue;
        }
        
        public static float SafeGetSingle(this XmlNode doc, string path, float minValue, float maxValue)
        {
            XmlNode rvalNode = doc.SelectSingleNode(path);
            if (rvalNode != null)
            {
                rvalNode.SafeGetSingle(minValue, maxValue);

            }
            return minValue;
        }


        public static string SafeGetString(this XmlNode node)
        {
            return SafeGetString(node, null);
        }

        public static string SafeGetString(this XmlNode node, string defaultValue)
        {
            if (node != null && node.InnerText.Length > 0)
            {
                return node.InnerText;
            }
            return defaultValue;
        }
    }
}
