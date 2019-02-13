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

        public string Id;
        public Dictionary<string, string> Parameters = new Dictionary<string, string>();
        public string Salt;
        public byte[] SaltBytes;
        public string Hash;
        public byte[] HashBytes;
        public PasswordHash(string storageString)
        {
            string[] SplitStorageString = storageString.Split('$');
            Id = SplitStorageString[1];
            if (SplitStorageString[2].Contains("="))
            {
                foreach (string paramset in (SplitStorageString[2].Split(',')))
                {
                    if (!String.IsNullOrEmpty(paramset))
                    {
                        string[] fields = paramset.Split('=');
                        if(fields.Length == 2)
                        {
                            Parameters.Add(fields[0], fields[1]);
                        }
                    }
                }
                if (SplitStorageString.Length == 5)
                {
                    Salt = SplitStorageString[3];
                    SaltBytes = ConvertFromByteString(Salt);
                    Hash = SplitStorageString[4];
                    HashBytes = ConvertFromByteString(Hash);
                }
                else
                {
                    Salt = string.Empty;
                    Hash = SplitStorageString[3];
                    HashBytes = ConvertFromByteString(Hash);
                }
            }
            else
            {
                if (SplitStorageString.Length == 4)
                {
                    Salt = SplitStorageString[2];
                    SaltBytes = ConvertFromByteString(Salt);
                    Hash = SplitStorageString[3];
                    HashBytes = ConvertFromByteString(Hash);
                }
                else
                {
                    Salt = string.Empty;
                    Hash = SplitStorageString[2];
                    HashBytes = ConvertFromByteString(Hash);
                }

            }

        }

        public PasswordHash(ICryptoProvider cryptoProvider)
        {
            Id = cryptoProvider.DefaultHashMethod;
            SaltBytes = cryptoProvider.GenerateSalt();
            Salt = ConvertToByteString(SaltBytes);
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
            string ReturnString = String.Empty;
            foreach (var KVP in Parameters)
            {
                ReturnString += String.Format(",{0}={1}", KVP.Key, KVP.Value);
            }
            if ((!string.IsNullOrEmpty(ReturnString)) && ReturnString[0] == ',')
            {
                ReturnString = ReturnString.Remove(0, 1);
            }
            return ReturnString;
        }

        public override string ToString()
        {
            string OutString = "$";
            OutString += Id;
            string paramstring = SerializeParameters();
            if (!string.IsNullOrEmpty(paramstring))
            {
                OutString += $"${paramstring}";
            }
            if (!string.IsNullOrEmpty(Salt))
            {
                OutString += $"${Salt}";
            }
            OutString += $"${Hash}";
            return OutString;
        }
    }

}
