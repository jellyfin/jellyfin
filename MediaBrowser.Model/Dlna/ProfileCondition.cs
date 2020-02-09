#pragma warning disable CS1591
#pragma warning disable SA1600

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

        public ProfileCondition(ProfileConditionType condition, ProfileConditionValue property, string value)
            : this(condition, property, value, false)
        {

        }

        public ProfileCondition(ProfileConditionType condition, ProfileConditionValue property, string value, bool isRequired)
        {
            Condition = condition;
            Property = property;
            Value = value;
            IsRequired = isRequired;
        }
    }
}
