using System;
using System.Globalization;
using System.Xml;

namespace MediaBrowser.Controller.Xml
{
    public static class XmlExtensions
    {
        private static CultureInfo _usCulture = new CultureInfo("en-US");

        public static float ReadFloatSafe(this XmlReader reader)
        {
            string valueString = reader.ReadElementContentAsString();

            float value = 0;

            if (!string.IsNullOrEmpty(valueString))
            {
                // float.TryParse is local aware, so it can be probamatic, force us culture
                float.TryParse(valueString, NumberStyles.AllowDecimalPoint, _usCulture, out value);
            }

            return value;
        }

        public static int ReadIntSafe(this XmlReader reader)
        {
            string valueString = reader.ReadElementContentAsString();

            int value = 0;

            if (!string.IsNullOrEmpty(valueString))
            {

                int.TryParse(valueString, out value);
            }

            return value;
        }
    }
}
