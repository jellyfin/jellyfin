using System.Xml.Serialization;

namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the <see cref="ProfileCondition" />.
    /// </summary>
    public class ProfileCondition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileCondition"/> class.
        /// </summary>
        public ProfileCondition()
        {
            Value = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileCondition"/> class.
        /// </summary>
        /// <param name="condition">The <see cref="ProfileConditionType"/>.</param>
        /// <param name="property">The <see cref="ProfileConditionValue"/>.</param>
        /// <param name="value">The value.</param>
        public ProfileCondition(ProfileConditionType condition, ProfileConditionValue property, string value)
        {
            Condition = condition;
            Property = property;
            Value = value;
            IsRequired = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileCondition"/> class.
        /// </summary>
        /// <param name="condition">The <see cref="ProfileConditionType"/>.</param>
        /// <param name="property">The <see cref="ProfileConditionValue"/>.</param>
        /// <param name="value">The value.</param>
        /// <param name="isRequired">True if it is required.</param>
        public ProfileCondition(ProfileConditionType condition, ProfileConditionValue property, string value, bool isRequired)
        {
            Condition = condition;
            Property = property;
            Value = value;
            IsRequired = isRequired;
        }

        /// <summary>
        /// Gets or sets the condition.
        /// </summary>
        [XmlAttribute("condition")]
        public ProfileConditionType Condition { get; set; }

        /// <summary>
        /// Gets or sets the property value.
        /// </summary>
        [XmlAttribute("property")]
        public ProfileConditionValue Property { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        [XmlAttribute("value")]
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the value is required.
        /// </summary>
        [XmlAttribute("isRequired")]
        public bool IsRequired { get; set; }
    }
}
