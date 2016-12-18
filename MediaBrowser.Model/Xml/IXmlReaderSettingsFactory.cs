using System.Xml;

namespace MediaBrowser.Model.Xml
{
    public interface IXmlReaderSettingsFactory
    {
        XmlReaderSettings Create(bool enableValidation);
    }
}
