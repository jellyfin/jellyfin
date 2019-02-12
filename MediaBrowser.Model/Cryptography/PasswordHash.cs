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
        public PasswordHash(string StorageString)
        {
            string[] a = StorageString.Split('$');
            Id = a[1];
            if (a[2].Contains("="))
            {
                foreach (string paramset in (a[2].Split(',')))
                {
                    if (!String.IsNullOrEmpty(paramset))
                    {
                        string[] fields = paramset.Split('=');
                        Parameters.Add(fields[0], fields[1]);
                    }
                }
                if (a.Length == 4)
                {
                    Salt = a[2];
                    SaltBytes = FromByteString(Salt);
                    Hash = a[3];
                    HashBytes = FromByteString(Hash);
                }
                else
                {
                    Salt = string.Empty;
                    Hash = a[3];
                    HashBytes = FromByteString(Hash);
                }
            }
            else
            {
                if (a.Length == 4)
                {
                    Salt = a[2];
                    SaltBytes = FromByteString(Salt);
                    Hash = a[3];
                    HashBytes = FromByteString(Hash);
                }
                else
                {
                    Salt = string.Empty;
                    Hash = a[2];
                    HashBytes = FromByteString(Hash);
                }

            }

        }

        public PasswordHash(ICryptoProvider cryptoProvider2)
        {
            Id = "SHA256";
            SaltBytes = cryptoProvider2.GenerateSalt();
            Salt = BitConverter.ToString(SaltBytes).Replace("-", "");
        }

        private byte[] FromByteString(string ByteString)
        {
            List<byte> Bytes = new List<byte>();
            for (int i = 0; i < ByteString.Length; i += 2)
            {
                Bytes.Add(Convert.ToByte(ByteString.Substring(i, 2),16));
            }
            return Bytes.ToArray();
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
            if (!string.IsNullOrEmpty(SerializeParameters()))
                OutString += $"${SerializeParameters()}";
            if (!string.IsNullOrEmpty(Salt))
                OutString += $"${Salt}";
            OutString += $"${Hash}";
            return OutString;
        }
    }

}
