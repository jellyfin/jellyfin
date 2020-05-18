#pragma warning disable CS1591

namespace MediaBrowser.Model.Dlna
{
    public enum ProfileConditionType
    {
        /// <summary>
        /// Equals
        /// </summary>
        Equals = 0,

        /// <summary>
        /// Not equals
        /// </summary>
        NotEquals = 1,

        /// <summary>
        /// Less than or equal
        /// </summary>
        LessThanEqual = 2,

        /// <summary>
        /// Greater than or equal
        /// </summary>
        GreaterThanEqual = 3,

        /// <summary>
        /// Equals any
        /// </summary>
        EqualsAny = 4
    }
}
