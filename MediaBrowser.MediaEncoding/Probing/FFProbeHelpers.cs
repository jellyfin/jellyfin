using System;
using System.Collections.Generic;

namespace MediaBrowser.MediaEncoding.Probing
{
    public static class FFProbeHelpers
    {
        /// <summary>
        /// Normalizes the FF probe result.
        /// </summary>
        /// <param name="result">The result.</param>
        public static void NormalizeFFProbeResult(InternalMediaInfoResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }

            if (result.format != null && result.format.tags != null)
            {
                result.format.tags = ConvertDictionaryToCaseInSensitive(result.format.tags);
            }

            if (result.streams != null)
            {
                // Convert all dictionaries to case insensitive
                foreach (var stream in result.streams)
                {
                    if (stream.tags != null)
                    {
                        stream.tags = ConvertDictionaryToCaseInSensitive(stream.tags);
                    }

                    if (stream.disposition != null)
                    {
                        stream.disposition = ConvertDictionaryToCaseInSensitive(stream.disposition);
                    }
                }
            }
        }

        /// <summary>
        /// Gets a string from an FFProbeResult tags dictionary
        /// </summary>
        /// <param name="tags">The tags.</param>
        /// <param name="key">The key.</param>
        /// <returns>System.String.</returns>
        public static string GetDictionaryValue(Dictionary<string, string> tags, string key)
        {
            if (tags == null)
            {
                return null;
            }

            string val;

            tags.TryGetValue(key, out val);
            return val;
        }

        /// <summary>
        /// Gets an int from an FFProbeResult tags dictionary
        /// </summary>
        /// <param name="tags">The tags.</param>
        /// <param name="key">The key.</param>
        /// <returns>System.Nullable{System.Int32}.</returns>
        public static int? GetDictionaryNumericValue(Dictionary<string, string> tags, string key)
        {
            var val = GetDictionaryValue(tags, key);

            if (!string.IsNullOrEmpty(val))
            {
                int i;

                if (int.TryParse(val, out i))
                {
                    return i;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets a DateTime from an FFProbeResult tags dictionary
        /// </summary>
        /// <param name="tags">The tags.</param>
        /// <param name="key">The key.</param>
        /// <returns>System.Nullable{DateTime}.</returns>
        public static DateTime? GetDictionaryDateTime(Dictionary<string, string> tags, string key)
        {
            var val = GetDictionaryValue(tags, key);

            if (!string.IsNullOrEmpty(val))
            {
                DateTime i;

                if (DateTime.TryParse(val, out i))
                {
                    return i.ToUniversalTime();
                }
            }

            return null;
        }

        /// <summary>
        /// Converts a dictionary to case insensitive
        /// </summary>
        /// <param name="dict">The dict.</param>
        /// <returns>Dictionary{System.StringSystem.String}.</returns>
        private static Dictionary<string, string> ConvertDictionaryToCaseInSensitive(Dictionary<string, string> dict)
        {
            return new Dictionary<string, string>(dict, StringComparer.OrdinalIgnoreCase);
        }
    }
}
