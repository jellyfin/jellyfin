using System.Xml;
using MediaBrowser.Model.Xml;

namespace Emby.Common.Implementations.Xml
{
    public class XmlReaderSettingsFactory : IXmlReaderSettingsFactory
    {
        public XmlReaderSettings Create(bool enableValidation)
        {
            var settings = new XmlReaderSettings();

            if (!enableValidation)
            {
#if NET46
                settings.ValidationType = ValidationType.None;
#endif
            }

            return settings;
        }
    }
}
