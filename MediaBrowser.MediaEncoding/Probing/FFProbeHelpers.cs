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
                throw new ArgumentNullException(nameof(result));
            }

            if (result.Format != null && result.Format.Tags != null)
            {
                result.Format.Tags = ConvertDictionaryToCaseInsensitive(result.Format.Tags);
            }

            if (result.Streams != null)
            {
                // Convert all dictionaries to case insensitive
                foreach (var stream in result.Streams)
                {
                    if (stream.Tags != null)
                    {
                        stream.Tags = ConvertDictionaryToCaseInsensitive(stream.Tags);
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
        public static string GetDictionaryValue(IReadOnlyDictionary<string, string> tags, string key)
        {
            if (tags == null)
            {
                return null;
            }

            tags.TryGetValue(key, out var val);
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
                if (int.TryParse(val, out var i))
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
                if (DateTime.TryParse(val, out var i))
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
        private static Dictionary<string, string> ConvertDictionaryToCaseInsensitive(IReadOnlyDictionary<string, string> dict)
        {
            return new Dictionary<string, string>(dict, StringComparer.OrdinalIgnoreCase);
        }
    }
}
