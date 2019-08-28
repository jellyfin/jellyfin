using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MediaBrowser.Model.Cryptography
{
    public class PasswordHash
    {
        // Defined from this hash storage spec
        // https://github.com/P-H-C/phc-string-format/blob/master/phc-sf-spec.md
        // $<id>[$<param>=<value>(,<param>=<value>)*][$<salt>[$<hash>]]
        // with one slight amendment to ease the transition, we're writing out the bytes in hex
        // rather than making them a BASE64 string with stripped padding

        private string _id;

        private Dictionary<string, string> _parameters = new Dictionary<string, string>();

        private byte[] _salt;

        private byte[] _hash;

        public PasswordHash(string storageString)
        {
            string[] splitted = storageString.Split('$');
            // The string should at least contain the hash function and the hash itself
            if (splitted.Length < 3)
            {
                throw new ArgumentException("String doesn't contain enough segments", nameof(storageString));
            }

            // Start at 1, the first index shouldn't contain any data
            int index = 1;

            // Name of the hash function
            _id = splitted[index++];

            // Optional parameters
            if (splitted[index].IndexOf('=') != -1)
            {
                foreach (string paramset in splitted[index++].Split(','))
                {
                    if (string.IsNullOrEmpty(paramset))
                    {
                        continue;
                    }

                    string[] fields = paramset.Split('=');
                    if (fields.Length != 2)
                    {
                        throw new InvalidDataException($"Malformed parameter in password hash string {paramset}");
                    }

                    _parameters.Add(fields[0], fields[1]);
                }
            }

            // Check if the string also contains a salt
            if (splitted.Length - index == 2)
            {
                _salt = ConvertFromByteString(splitted[index++]);
                _hash = ConvertFromByteString(splitted[index++]);
            }
            else
            {
                _salt = Array.Empty<byte>();
                _hash = ConvertFromByteString(splitted[index++]);
            }
        }

        public PasswordHash(ICryptoProvider cryptoProvider)
        {
            _id = cryptoProvider.DefaultHashMethod;
            _salt = cryptoProvider.GenerateSalt();
            _hash = Array.Empty<Byte>();
        }

        public string Id { get => _id; set => _id = value; }

        public Dictionary<string, string> Parameters { get => _parameters; set => _parameters = value; }

        public byte[] Salt { get => _salt; set => _salt = value; }

        public byte[] Hash { get => _hash; set => _hash = value; }

        // TODO: move this class and use the HexHelper class
        public static byte[] ConvertFromByteString(string byteString)
        {
            byte[] bytes = new byte[byteString.Length / 2];
            for (int i = 0; i < byteString.Length; i += 2)
            {
                // TODO: NetStandard2.1 switch this to use a span instead of a substring.
                bytes[i / 2] = Convert.ToByte(byteString.Substring(i, 2), 16);
            }

            return bytes;
        }

        public static string ConvertToByteString(byte[] bytes)
            => BitConverter.ToString(bytes).Replace("-", string.Empty);

        private void SerializeParameters(StringBuilder stringBuilder)
        {
            if (_parameters.Count == 0)
            {
                return;
            }

            stringBuilder.Append('$');
            foreach (var pair in _parameters)
            {
                stringBuilder.Append(pair.Key);
                stringBuilder.Append('=');
                stringBuilder.Append(pair.Value);
                stringBuilder.Append(',');
            }

            // Remove last ','
            stringBuilder.Length -= 1;
        }

        public override string ToString()
        {
            var str = new StringBuilder();
            str.Append('$');
            str.Append(_id);
            SerializeParameters(str);

            if (_salt.Length != 0)
            {
                str.Append('$');
                str.Append(ConvertToByteString(_salt));
            }

            str.Append('$');
            str.Append(ConvertToByteString(_hash));

            return str.ToString();
        }
    }
}
