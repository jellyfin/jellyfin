using System;
using System.Xml.Linq;

namespace MediaBrowser.Dlna.PlayTo
{
    public class uContainer : uBaseObject
    {
        new public static uBaseObject Create(XElement container)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }

            return new uBaseObject
            {
                Id = (string)container.Attribute(uPnpNamespaces.Id),
                ParentId = (string)container.Attribute(uPnpNamespaces.ParentId),
                Title = (string)container.Element(uPnpNamespaces.title),
                IconUrl = container.GetValue(uPnpNamespaces.Artwork)
            };
        }
    }
}
