using System.Globalization;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Controller.Xml
{
    public static class XmlExtensions
    {
        private static CultureInfo _usCulture = new CultureInfo("en-US");

        /// <summary>
        /// Reads a float from the current element of an XmlReader
        /// </summary>
        public static async Task<float> ReadFloatSafe(this XmlReader reader)
        {
            string valueString = await reader.ReadElementContentAsStringAsync();

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
        public static async Task<int> ReadIntSafe(this XmlReader reader)
        {
            string valueString = await reader.ReadElementContentAsStringAsync();

            int value = 0;

            if (!string.IsNullOrWhiteSpace(valueString))
            {

                int.TryParse(valueString, out value);
            }

            return value;
        }
    }
}
