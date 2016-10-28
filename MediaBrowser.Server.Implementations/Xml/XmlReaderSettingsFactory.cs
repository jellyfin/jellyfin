using System.Xml;
using MediaBrowser.Model.Xml;

namespace MediaBrowser.Server.Implementations.Xml
{
    public class XmlReaderSettingsFactory : IXmlReaderSettingsFactory
    {
        public XmlReaderSettings Create(bool enableValidation)
        {
            var settings = new XmlReaderSettings();

            if (!enableValidation)
            {
                settings.ValidationType = ValidationType.None;
            }

            return settings;
        }
    }
}
