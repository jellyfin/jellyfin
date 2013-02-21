using System.Globalization;
using System.Xml;

namespace MediaBrowser.Controller.Xml
{
    public static class XmlExtensions
    {
        private static readonly CultureInfo _usCulture = new CultureInfo("en-US");

        /// <summary>
        /// Reads a float from the current element of an XmlReader
        /// </summary>
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
