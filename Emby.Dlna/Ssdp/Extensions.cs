#pragma warning disable CS1591

using System.Linq;
using System.Xml.Linq;

namespace Emby.Dlna.Ssdp
{
    public static class Extensions
    {
        public static string GetValue(this XElement container, XName name)
        {
            var node = container.Element(name);

            return node?.Value;
        }

        public static string GetAttributeValue(this XElement container, XName name)
        {
            var node = container.Attribute(name);

            return node?.Value;
        }

        public static string GetDescendantValue(this XElement container, XName name)
            => container.Descendants(name).FirstOrDefault()?.Value;
    }
}
