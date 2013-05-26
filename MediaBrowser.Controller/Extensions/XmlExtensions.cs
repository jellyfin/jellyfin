using System;
using System.Globalization;
using System.Xml;

namespace MediaBrowser.Controller.Extensions
{
    /// <summary>
    /// Class XmlExtensions
    /// </summary>
    public static class XmlExtensions
    {

        /// <summary>
        /// Safes the get int32.
        /// </summary>
        /// <param name="doc">The doc.</param>
        /// <param name="path">The path.</param>
        /// <returns>System.Int32.</returns>
        public static int SafeGetInt32(this XmlDocument doc, string path)
        {
            return SafeGetInt32(doc, path, 0);
        }

        /// <summary>
        /// Safes the get int32.
        /// </summary>
        /// <param name="doc">The doc.</param>
        /// <param name="path">The path.</param>
        /// <param name="defaultInt">The default int.</param>
        /// <returns>System.Int32.</returns>
        public static int SafeGetInt32(this XmlDocument doc, string path, int defaultInt)
        {
            XmlNode rvalNode = doc.SelectSingleNode(path);
            if (rvalNode != null && rvalNode.InnerText.Length > 0)
            {
                int rval;
                if (Int32.TryParse(rvalNode.InnerText, out rval))
                {
                    return rval;
                }

            }
            return defaultInt;
        }

        /// <summary>
        /// The _us culture
        /// </summary>
        private static readonly CultureInfo _usCulture = new CultureInfo("en-US");

        /// <summary>
        /// Safes the get single.
        /// </summary>
        /// <param name="doc">The doc.</param>
        /// <param name="path">The path.</param>
        /// <param name="minValue">The min value.</param>
        /// <param name="maxValue">The max value.</param>
        /// <returns>System.Single.</returns>
        public static float SafeGetSingle(this XmlDocument doc, string path, float minValue, float maxValue)
        {
            XmlNode rvalNode = doc.SelectSingleNode(path);
            if (rvalNode != null && rvalNode.InnerText.Length > 0)
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


        /// <summary>
        /// Safes the get string.
        /// </summary>
        /// <param name="doc">The doc.</param>
        /// <param name="path">The path.</param>
        /// <returns>System.String.</returns>
        public static string SafeGetString(this XmlDocument doc, string path)
        {
            return SafeGetString(doc, path, null);
        }

        /// <summary>
        /// Safes the get string.
        /// </summary>
        /// <param name="doc">The doc.</param>
        /// <param name="path">The path.</param>
        /// <param name="defaultString">The default string.</param>
        /// <returns>System.String.</returns>
        public static string SafeGetString(this XmlDocument doc, string path, string defaultString)
        {
            var rvalNode = doc.SelectSingleNode(path);

            if (rvalNode != null)
            {
                var text = rvalNode.InnerText;

                return !string.IsNullOrWhiteSpace(text) ? text : defaultString;
            }

            return defaultString;
        }

        /// <summary>
        /// Safes the get DateTime.
        /// </summary>
        /// <param name="doc">The doc.</param>
        /// <param name="path">The path.</param>
        /// <returns>System.DateTime.</returns>
        public static DateTime? SafeGetDateTime(this XmlDocument doc, string path)
        {
            return SafeGetDateTime(doc, path, null);
        }

        /// <summary>
        /// Safes the get DateTime.
        /// </summary>
        /// <param name="doc">The doc.</param>
        /// <param name="path">The path.</param>
        /// <param name="defaultDate">The default date.</param>
        /// <returns>System.DateTime.</returns>
        public static DateTime? SafeGetDateTime(this XmlDocument doc, string path, DateTime? defaultDate)
        {
            var rvalNode = doc.SelectSingleNode(path);

            if (rvalNode != null)
            {
                var text = rvalNode.InnerText;
                DateTime date;
                if (DateTime.TryParse(text, out date))
                    return date;
            }
            return defaultDate;
        }

        /// <summary>
        /// Safes the get string.
        /// </summary>
        /// <param name="doc">The doc.</param>
        /// <param name="path">The path.</param>
        /// <returns>System.String.</returns>
        public static string SafeGetString(this XmlNode doc, string path)
        {
            return SafeGetString(doc, path, null);
        }

        /// <summary>
        /// Safes the get string.
        /// </summary>
        /// <param name="doc">The doc.</param>
        /// <param name="path">The path.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>System.String.</returns>
        public static string SafeGetString(this XmlNode doc, string path, string defaultValue)
        {
            var rvalNode = doc.SelectSingleNode(path);
            if (rvalNode != null)
            {
                var text = rvalNode.InnerText;

                return !string.IsNullOrWhiteSpace(text) ? text : defaultValue;
            }
            return defaultValue;
        }

        /// <summary>
        /// Reads the string safe.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>System.String.</returns>
        public static string ReadStringSafe(this XmlReader reader)
        {
            var val = reader.ReadElementContentAsString();

            return string.IsNullOrWhiteSpace(val) ? null : val;
        }

        /// <summary>
        /// Reads the value safe.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>System.String.</returns>
        public static string ReadValueSafe(this XmlReader reader)
        {
            reader.Read();

            var val = reader.Value;

            return string.IsNullOrWhiteSpace(val) ? null : val;
        }

        /// <summary>
        /// Reads a float from the current element of an XmlReader
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>System.Single.</returns>
        public static float ReadFloatSafe(this XmlReader reader)
        {
            string valueString = reader.ReadElementContentAsString();

            float value = 0;

            if (!string.IsNullOrWhiteSpace(valueString))
            {
                // float.TryParse is local aware, so it can be probamatic, force us culture
                float.TryParse(valueString, NumberStyles.AllowDecimalPoint, _usCulture, out value);
            }

            return value;
        }

        /// <summary>
        /// Reads an int from the current element of an XmlReader
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>System.Int32.</returns>
        public static int ReadIntSafe(this XmlReader reader)
        {
            string valueString = reader.ReadElementContentAsString();

            int value = 0;

            if (!string.IsNullOrWhiteSpace(valueString))
            {
                int.TryParse(valueString, out value);
            }

            return value;
        }
    }
}