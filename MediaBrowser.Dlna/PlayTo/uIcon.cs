using System;
using System.Xml.Linq;

namespace MediaBrowser.Dlna.PlayTo
{
    public class uIcon
    {
        public string Url { get; private set; }

        public string MimeType { get; private set; }

        public int Width { get; private set; }

        public int Height { get; private set; }

        public string Depth { get; private set; }

        public uIcon(string mimeType, string width, string height, string depth, string url)
        {
            MimeType = mimeType;
            Width = (!string.IsNullOrEmpty(width)) ? int.Parse(width) : 0;
            Height = (!string.IsNullOrEmpty(height)) ? int.Parse(height) : 0;
            Depth = depth;
            Url = url;
        }

        public static uIcon Create(XElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            var mimeType = element.GetDescendantValue(uPnpNamespaces.ud.GetName("mimetype"));
            var width = element.GetDescendantValue(uPnpNamespaces.ud.GetName("width"));
            var height = element.GetDescendantValue(uPnpNamespaces.ud.GetName("height"));
            var depth = element.GetDescendantValue(uPnpNamespaces.ud.GetName("depth"));
            var url = element.GetDescendantValue(uPnpNamespaces.ud.GetName("url"));

            return new uIcon(mimeType, width, height, depth, url);
        }

        public override string ToString()
        {
            return string.Format("{0}x{1}", Height, Width);
        }
    }
}
