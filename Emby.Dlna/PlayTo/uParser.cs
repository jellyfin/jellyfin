using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Emby.Dlna.PlayTo
{
    public class uParser
    {
        public static IList<uBaseObject> ParseBrowseXml(XDocument doc)
        {
            if (doc == null)
            {
                throw new ArgumentException("doc");
            }

            var list = new List<uBaseObject>();

            var document = doc.Document;

            if (document == null)
                return list;
            
            var item = (from result in document.Descendants("Result") select result).FirstOrDefault();

            if (item == null)
                return list;

            var uPnpResponse = XElement.Parse((String)item);

            var uObjects = from container in uPnpResponse.Elements(uPnpNamespaces.containers)
                           select new uParserObject { Element = container };

            var uObjects2 = from container in uPnpResponse.Elements(uPnpNamespaces.items)
                            select new uParserObject { Element = container };

            list.AddRange(uObjects.Concat(uObjects2).Select(CreateObjectFromXML).Where(uObject => uObject != null));

            return list;
        }

        public static uBaseObject CreateObjectFromXML(uParserObject uItem)
        {
            return UpnpContainer.Create(uItem.Element);
        }
    }
}
