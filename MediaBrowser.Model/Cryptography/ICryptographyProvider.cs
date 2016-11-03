using System;
using System.IO;

namespace MediaBrowser.Model.Cryptography
{
    public interface ICryptographyProvider
    {
        Guid GetMD5(string str);
        byte[] GetMD5Bytes(string str);
        byte[] GetSHA1Bytes(byte[] bytes);
        byte[] GetMD5Bytes(Stream str);
        byte[] GetMD5Bytes(byte[] bytes);
    }
}