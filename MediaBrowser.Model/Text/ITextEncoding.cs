using System.IO;
using System.Text;

namespace MediaBrowser.Model.Text
{
    public interface ITextEncoding
    {
        Encoding GetASCIIEncoding();

        string GetDetectedEncodingName(byte[] bytes, string language, bool enableLanguageDetection);
        Encoding GetDetectedEncoding(byte[] bytes, string language, bool enableLanguageDetection);
        Encoding GetEncodingFromCharset(string charset);
    }
}
