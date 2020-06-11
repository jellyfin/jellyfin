namespace MediaBrowser.Model.Serialization.Attributes
{
    /// <summary>
    /// Json ignore conditions.
    /// TODO remove after .Net 5.0
    /// </summary>
    public enum JsonIgnoreCondition
    {
        /// <summary>
        /// Always ignore property.
        /// </summary>
        Always = 0,

        /// <summary>
        /// Ignore property when null.
        /// </summary>
        WhenNull = 1,

        /// <summary>
        /// Never ignore property.
        /// </summary>
        Never = 2,
    }
}
