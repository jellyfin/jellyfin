#pragma warning disable CS1591
#pragma warning disable SA1600

using System.Xml.Linq;

namespace Emby.Dlna.Ssdp
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
            foreach (var node in container.Descendants(name))
            {
                return node.Value;
            }

            return null;
        }
    }
}
