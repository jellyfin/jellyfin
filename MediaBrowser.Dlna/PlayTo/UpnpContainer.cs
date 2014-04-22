using System;
using System.Xml.Linq;

namespace MediaBrowser.Dlna.PlayTo
{
    public class UpnpContainer : uBaseObject
    {
        new public static uBaseObject Create(XElement container)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }

            return new uBaseObject
            {
                Id = container.GetAttributeValue(uPnpNamespaces.Id),
                ParentId = container.GetAttributeValue(uPnpNamespaces.ParentId),
                Title = container.GetValue(uPnpNamespaces.title),
                IconUrl = container.GetValue(uPnpNamespaces.Artwork),
                UpnpClass = container.GetValue(uPnpNamespaces.uClass)
            };
        }
    }
}
