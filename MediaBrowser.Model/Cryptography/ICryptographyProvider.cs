using System;

namespace MediaBrowser.Model.Cryptography
{
    public interface ICryptographyProvider
    {
        Guid GetMD5(string str);
    }
}
