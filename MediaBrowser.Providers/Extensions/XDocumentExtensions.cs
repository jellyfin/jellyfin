using System.Xml;
using System.Xml.Linq;

namespace MediaBrowser.Providers.Extensions
{
    public static class XDocumentExtensions
    {
        public static XmlDocument ToXmlDocument(this XElement xDocument)
        {
            var xmlDocument = new XmlDocument();
            using (var xmlReader = xDocument.CreateReader())
            {
                xmlDocument.Load(xmlReader);
            }
            return xmlDocument;
        }
    }
}