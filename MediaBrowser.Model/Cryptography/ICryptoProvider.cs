using System;
using System.IO;

namespace MediaBrowser.Model.Cryptography
{
    public interface ICryptoProvider
    {
        Guid GetMD5(string str);
        byte[] ComputeMD5(Stream str);
        byte[] ComputeMD5(byte[] bytes);
        byte[] ComputeSHA1(byte[] bytes);
    }
}