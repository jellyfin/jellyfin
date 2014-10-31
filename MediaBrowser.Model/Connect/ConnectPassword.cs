
namespace MediaBrowser.Model.Connect
{
    public static class ConnectPassword
    {
        public static string PerformPreHashFilter(string password)
        {
            return password
                .Replace("&", "&amp;")
                .Replace("/", "&#092;")
                .Replace("!", "&#33;")
                .Replace("$", "&#036;")
                .Replace("\"", "&quot;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("'", "&#39;");
        }
    }
}
