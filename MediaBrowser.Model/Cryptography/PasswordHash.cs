#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Text;

namespace MediaBrowser.Model.Cryptography
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
            ArgumentException.ThrowIfNullOrEmpty(id);

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

        public static PasswordHash Parse(ReadOnlySpan<char> hashString)
        {
            if (hashString.IsEmpty)
            {
                throw new ArgumentException("String can't be empty", nameof(hashString));
            }

            if (hashString[0] != '$')
            {
                throw new FormatException("Hash string must start with a $");
            }

            // Ignore first $
            hashString = hashString[1..];

            int nextSegment = hashString.IndexOf('$');
            if (hashString.IsEmpty || nextSegment == 0)
            {
                throw new FormatException("Hash string must contain a valid id");
            }

            if (nextSegment == -1)
            {
                return new PasswordHash(hashString.ToString(), Array.Empty<byte>());
            }

            ReadOnlySpan<char> id = hashString[..nextSegment];
            hashString = hashString[(nextSegment + 1)..];
            Dictionary<string, string>? parameters = null;

            nextSegment = hashString.IndexOf('$');

            // Optional parameters
            ReadOnlySpan<char> parametersSpan = nextSegment == -1 ? hashString : hashString[..nextSegment];
            if (parametersSpan.Contains('='))
            {
                while (!parametersSpan.IsEmpty)
                {
                    ReadOnlySpan<char> parameter;
                    int index = parametersSpan.IndexOf(',');
                    if (index == -1)
                    {
                        parameter = parametersSpan;
                        parametersSpan = ReadOnlySpan<char>.Empty;
                    }
                    else
                    {
                        parameter = parametersSpan[..index];
                        parametersSpan = parametersSpan[(index + 1)..];
                    }

                    int splitIndex = parameter.IndexOf('=');
                    if (splitIndex == -1 || splitIndex == 0 || splitIndex == parameter.Length - 1)
                    {
                        throw new FormatException("Malformed parameter in password hash string");
                    }

                    (parameters ??= new Dictionary<string, string>()).Add(
                        parameter[..splitIndex].ToString(),
                        parameter[(splitIndex + 1)..].ToString());
                }

                if (nextSegment == -1)
                {
                    // parameters can't be null here
                    return new PasswordHash(id.ToString(), Array.Empty<byte>(), Array.Empty<byte>(), parameters!);
                }

                hashString = hashString[(nextSegment + 1)..];
                nextSegment = hashString.IndexOf('$');
            }

            if (nextSegment == 0)
            {
                throw new FormatException("Hash string contains an empty segment");
            }

            byte[] hash;
            byte[] salt;

            if (nextSegment == -1)
            {
                salt = Array.Empty<byte>();
                hash = Convert.FromHexString(hashString);
            }
            else
            {
                salt = Convert.FromHexString(hashString[..nextSegment]);
                hashString = hashString[(nextSegment + 1)..];
                nextSegment = hashString.IndexOf('$');
                if (nextSegment != -1)
                {
                    throw new FormatException("Hash string contains too many segments");
                }

                if (hashString.IsEmpty)
                {
                    throw new FormatException("Hash segment is empty");
                }

                hash = Convert.FromHexString(hashString);
            }

            return new PasswordHash(id.ToString(), hash, salt, parameters ?? new Dictionary<string, string>());
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
                    .Append(Convert.ToHexString(_salt));
            }

            if (_hash.Length != 0)
            {
                str.Append('$')
                    .Append(Convert.ToHexString(_hash));
            }

            return str.ToString();
        }
    }
}
