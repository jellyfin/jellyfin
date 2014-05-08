using System.Xml.Serialization;

namespace MediaBrowser.Model.Dlna
{
    public class ProfileCondition
    {
        [XmlAttribute("condition")]
        public ProfileConditionType Condition { get; set; }

        [XmlAttribute("property")]
        public ProfileConditionValue Property { get; set; }

        [XmlAttribute("value")]
        public string Value { get; set; }

        [XmlAttribute("isRequired")]
        public bool IsRequired { get; set; }

        public ProfileCondition()
        {
            IsRequired = true;
        }
    }
}