using System.IO;
using System.Text;

namespace MediaBrowser.Model.Text
{
    public interface ITextEncoding
    {
        Encoding GetASCIIEncoding();

        string GetDetectedEncodingName(byte[] bytes, string language);
        Encoding GetDetectedEncoding(byte[] bytes, string language);
        Encoding GetEncodingFromCharset(string charset);
    }
}
