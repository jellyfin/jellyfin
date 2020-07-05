#nullable enable
#pragma warning disable CS1591

using System;
using System.Linq;
using System.Xml.Linq;

namespace Emby.Dlna.Ssdp
{
    public static class Extensions
    {
        public static string GetValue(this XElement container, XName name)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            XElement? node = container.Element(name);

            return node != null ? node.Value ?? string.Empty : string.Empty;
        }

        public static string GetAttributeValue(this XElement container, XName name)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            XAttribute? node = container.Attribute(name);

            return node != null ? node.Value ?? string.Empty : string.Empty;
        }

        public static string GetDescendantValue(this XElement container, XName name)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            var x = container.Descendants(name).FirstOrDefault();

            return x != null ? x.Value ?? string.Empty : string.Empty;
        }
    }
}
