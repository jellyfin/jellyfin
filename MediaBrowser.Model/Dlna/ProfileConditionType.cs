namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the <see cref="ProfileConditionType"/>.
    /// </summary>
    public enum ProfileConditionType
    {
        /// <summary>
        /// Defines the condition equals.
        /// </summary>
        Equals = 0,

        /// <summary>
        /// Defines the condition NotEquals.
        /// </summary>
        NotEquals = 1,

        /// <summary>
        /// Defines the condition LessThanEqual.
        /// </summary>
        LessThanEqual = 2,

        /// <summary>
        /// Defines the condition GreaterThanEqual.
        /// </summary>
        GreaterThanEqual = 3,

        /// <summary>
        /// Defines the condition EqualsAny.
        /// </summary>
        EqualsAny = 4
    }
}
