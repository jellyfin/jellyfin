using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MediaBrowser.Dlna.Ssdp
{
    public static class Extensions
    {
        public static string GetValue(this XElement container, XName name)
        {
            var node = container.Element(name);

            return node == null ? null : node.Value;
        }

        public static string GetAttributeValue(this XElement container, XName name)
        {
            var node = container.Attribute(name);

            return node == null ? null : node.Value;
        }

        public static string GetDescendantValue(this XElement container, XName name)
        {
            var node = container.Descendants(name)
                .FirstOrDefault();

            return node == null ? null : node.Value;
        }
    }
}
