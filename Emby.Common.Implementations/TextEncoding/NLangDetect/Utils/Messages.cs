using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Linq;
using System;

namespace NLangDetect.Core.Utils
{
    public static class Messages
    {
        private static readonly Dictionary<string, string> _messages;

        static Messages()
        {
            _messages = LoadMessages();
        }

        public static string getString(string key)
        {
            string value;

            return
              _messages.TryGetValue(key, out value)
                ? value
                : string.Format("!{0}!", key);
        }

        private static Dictionary<string, string> LoadMessages()
        {
            var manifestName = typeof(Messages).Assembly.GetManifestResourceNames().FirstOrDefault(i => i.IndexOf("messages.properties", StringComparison.Ordinal) != -1) ;

            Stream messagesStream =
              typeof(Messages).Assembly
                .GetManifestResourceStream(manifestName);

            if (messagesStream == null)
            {
                throw new InternalException(string.Format("Couldn't get embedded resource named '{0}'.", manifestName));
            }

            using (messagesStream)
            using (var sr = new StreamReader(messagesStream))
            {
                var messages = new Dictionary<string, string>();

                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();

                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    string[] keyValue = line.Split('=');

                    if (keyValue.Length != 2)
                    {
                        throw new InternalException(string.Format("Invalid format of the 'Messages.properties' resource. Offending line: '{0}'.", line.Trim()));
                    }

                    string key = keyValue[0];
                    string value = UnescapeUnicodeString(keyValue[1]);

                    messages.Add(key, value);
                }

                return messages;
            }
        }

        /// <remarks>
        /// Taken from: http://stackoverflow.com/questions/1615559/converting-unicode-strings-to-escaped-ascii-string/1615860#1615860
        /// </remarks>
        private static string UnescapeUnicodeString(string s)
        {
            if (s == null)
            {
                return null;
            }

            return
              Regex.Replace(
                s,
                @"\\u(?<Value>[a-zA-Z0-9]{4})",
                match => ((char)int.Parse(match.Groups["Value"].Value, NumberStyles.HexNumber)).ToString());
        }
    }
}
