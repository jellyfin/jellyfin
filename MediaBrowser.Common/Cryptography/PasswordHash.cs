#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MediaBrowser.Common.Cryptography
{
    // Defined from this hash storage spec
    // https://github.com/P-H-C/phc-string-format/blob/master/phc-sf-spec.md
    // $<id>[$<param>=<value>(,<param>=<value>)*][$<salt>[$<hash>]]
    // with one slight amendment to ease the transition, we're writing out the bytes in hex
    // rather than making them a BASE64 string with stripped padding
    public class PasswordHash
    {
        private readonly Dictionary<string, string> _parameters;
        private readonly byte[] _salt;
        private readonly byte[] _hash;

        public PasswordHash(string id, byte[] hash)
            : this(id, hash, Array.Empty<byte>())
        {
        }

        public PasswordHash(string id, byte[] hash, byte[] salt)
            : this(id, hash, salt, new Dictionary<string, string>())
        {
        }

        public PasswordHash(string id, byte[] hash, byte[] salt, Dictionary<string, string> parameters)
        {
            Id = id;
            _hash = hash;
            _salt = salt;
            _parameters = parameters;
        }

        /// <summary>
        /// Gets the symbolic name for the function used.
        /// </summary>
        /// <value>Returns the symbolic name for the function used.</value>
        public string Id { get; }

        /// <summary>
        /// Gets the additional parameters used by the hash function.
        /// </summary>
        public IReadOnlyDictionary<string, string> Parameters => _parameters;

        /// <summary>
        /// Gets the salt used for hashing the password.
        /// </summary>
        /// <value>Returns the salt used for hashing the password.</value>
        public ReadOnlySpan<byte> Salt => _salt;

        /// <summary>
        /// Gets the hashed password.
        /// </summary>
        /// <value>Return the hashed password.</value>
        public ReadOnlySpan<byte> Hash => _hash;

        public static PasswordHash Parse(string hashString)
        {
            // The string should at least contain the hash function and the hash itself
            string[] splitted = hashString.Split('$');
            if (splitted.Length < 3)
            {
                throw new ArgumentException("String doesn't contain enough segments", nameof(hashString));
            }

            // Start at 1, the first index shouldn't contain any data
            int index = 1;

            // Name of the hash function
            string id = splitted[index++];

            // Optional parameters
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            if (splitted[index].IndexOf('=', StringComparison.Ordinal) != -1)
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

                    parameters.Add(fields[0], fields[1]);
                }
            }

            byte[] hash;
            byte[] salt;

            // Check if the string also contains a salt
            if (splitted.Length - index == 2)
            {
                salt = Hex.Decode(splitted[index++]);
                hash = Hex.Decode(splitted[index++]);
            }
            else
            {
                salt = Array.Empty<byte>();
                hash = Hex.Decode(splitted[index++]);
            }

            return new PasswordHash(id, hash, salt, parameters);
        }

        private void SerializeParameters(StringBuilder stringBuilder)
        {
            if (_parameters.Count == 0)
            {
                return;
            }

            stringBuilder.Append('$');
            foreach (var pair in _parameters)
            {
                stringBuilder.Append(pair.Key)
                    .Append('=')
                    .Append(pair.Value)
                    .Append(',');
            }

            // Remove last ','
            stringBuilder.Length -= 1;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var str = new StringBuilder()
                .Append('$')
                .Append(Id);
            SerializeParameters(str);

            if (_salt.Length != 0)
            {
                str.Append('$')
                    .Append(Hex.Encode(_salt, false));
            }

            return str.Append('$')
                .Append(Hex.Encode(_hash, false)).ToString();
        }
    }
}
