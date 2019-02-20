using System;
using System.Collections.Generic;
using System.Text;

namespace MediaBrowser.Model.Cryptography
{
    public class PasswordHash
    {
        //Defined from this hash storage spec
        //https://github.com/P-H-C/phc-string-format/blob/master/phc-sf-spec.md
        //$<id>[$<param>=<value>(,<param>=<value>)*][$<salt>[$<hash>]]

        private string id;
        private Dictionary<string, string> parameters = new Dictionary<string, string>();
        private string salt;
        private byte[] saltBytes;
        private string hash;
        private byte[] hashBytes;
        public string Id { get => id; set => id = value; }
        public Dictionary<string, string> Parameters { get => parameters; set => parameters = value; }
        public string Salt { get => salt; set => salt = value; }
        public byte[] SaltBytes { get => saltBytes; set => saltBytes = value; }
        public string Hash { get => hash; set => hash = value; }
        public byte[] HashBytes { get => hashBytes; set => hashBytes = value; }

        public PasswordHash(string storageString)
        {
            string[] splitted = storageString.Split('$');
            id = splitted[1];
            if (splitted[2].Contains("="))
            {
                foreach (string paramset in (splitted[2].Split(',')))
                {
                    if (!string.IsNullOrEmpty(paramset))
                    {
                        string[] fields = paramset.Split('=');
                        if (fields.Length == 2)
                        {
                            parameters.Add(fields[0], fields[1]);
                        }
                        else
                        {
                            throw new Exception($"Malformed parameter in password hash string {paramset}");
                        }
                    }
                }
                if (splitted.Length == 5)
                {
                    salt = splitted[3];
                    saltBytes = ConvertFromByteString(salt);
                    hash = splitted[4];
                    hashBytes = ConvertFromByteString(hash);
                }
                else
                {
                    salt = string.Empty;
                    hash = splitted[3];
                    hashBytes = ConvertFromByteString(hash);
                }
            }
            else
            {
                if (splitted.Length == 4)
                {
                    salt = splitted[2];
                    saltBytes = ConvertFromByteString(salt);
                    hash = splitted[3];
                    hashBytes = ConvertFromByteString(hash);
                }
                else
                {
                    salt = string.Empty;
                    hash = splitted[2];
                    hashBytes = ConvertFromByteString(hash);
                }

            }

        }

        public PasswordHash(ICryptoProvider cryptoProvider)
        {
            id = cryptoProvider.DefaultHashMethod;
            saltBytes = cryptoProvider.GenerateSalt();
            salt = ConvertToByteString(SaltBytes);
        }

        public static byte[] ConvertFromByteString(string byteString)
        {
            List<byte> Bytes = new List<byte>();
            for (int i = 0; i < byteString.Length; i += 2)
            {
                Bytes.Add(Convert.ToByte(byteString.Substring(i, 2),16));
            }
            return Bytes.ToArray();
        }

        public static string ConvertToByteString(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "");
        }

        private string SerializeParameters()
        {
            string ReturnString = string.Empty;
            foreach (var KVP in parameters)
            {
                ReturnString += $",{KVP.Key}={KVP.Value}";
            }

            if ((!string.IsNullOrEmpty(ReturnString)) && ReturnString[0] == ',')
            {
                ReturnString = ReturnString.Remove(0, 1);
            }

            return ReturnString;
        }

        public override string ToString()
        {
            string outString = "$" +id;
            string paramstring = SerializeParameters();
            if (!string.IsNullOrEmpty(paramstring))
            {
                outString += $"${paramstring}";
            }

            if (!string.IsNullOrEmpty(salt))
            {
                outString += $"${salt}";
            }

            outString += $"${hash}";
            return outString;
        }
    }

}
