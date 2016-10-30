using System.Text;
using MediaBrowser.Model.TextEncoding;

namespace MediaBrowser.Server.Implementations.TextEncoding
{
    public  class TextEncoding : IEncoding
    {
        public byte[] GetASCIIBytes(string text)
        {
            return Encoding.ASCII.GetBytes(text);
        }

        public string GetASCIIString(byte[] bytes, int startIndex, int length)
        {
            return Encoding.ASCII.GetString(bytes, 0, bytes.Length);
        }
    }
}
