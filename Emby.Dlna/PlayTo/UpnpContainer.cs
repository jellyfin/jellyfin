#pragma warning disable CS1591

using System;
using System.Xml.Linq;
using Emby.Dlna.Ssdp;

namespace Emby.Dlna.PlayTo
{
    public class UpnpContainer : UBaseObject
    {
        public static UBaseObject Create(XElement container)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            return new UBaseObject
            {
                Id = container.GetAttributeValue(UPnpNamespaces.Id),
                ParentId = container.GetAttributeValue(UPnpNamespaces.ParentId),
                Title = container.GetValue(UPnpNamespaces.Title),
                IconUrl = container.GetValue(UPnpNamespaces.Artwork),
                UpnpClass = container.GetValue(UPnpNamespaces.Class)
            };
        }
    }
}
