using System;
using System.Xml.Linq;

namespace MediaBrowser.Dlna.PlayTo
{
    public class uBaseObject 
    {
        public string Id { get; set; }

        public string ParentId { get; set; }

        public string Title { get; set; }

        public string SecondText { get; set; }

        public string IconUrl { get; set; }

        public string MetaData { get; set; }

        public string Url { get; set; }

        public string[] ProtocolInfo { get; set; }

        public static uBaseObject Create(XElement container)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }

            return new uBaseObject
            {
                Id = container.Attribute(uPnpNamespaces.Id).Value,
                ParentId = container.Attribute(uPnpNamespaces.ParentId).Value,
                Title = container.GetValue(uPnpNamespaces.title),
                IconUrl = container.GetValue(uPnpNamespaces.Artwork),
                SecondText = "",
                Url = container.GetValue(uPnpNamespaces.Res),
                ProtocolInfo = GetProtocolInfo(container),
                MetaData = container.ToString()
            };
        }

        private static string[] GetProtocolInfo(XElement container)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }

            var resElement = container.Element(uPnpNamespaces.Res);

            if (resElement != null)
            {
                var info = resElement.Attribute(uPnpNamespaces.ProtocolInfo);

                if (info != null && !string.IsNullOrWhiteSpace(info.Value))
                {
                    return info.Value.Split(':');
                }
            }

            return new string[4];
        }
    }
}
