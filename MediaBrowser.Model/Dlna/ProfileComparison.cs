namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the <see cref="ProfileComparison"/> constants.
    /// </summary>
    public static class ProfileComparison
    {
        /// <summary>
        /// Defines the constant representing no match.
        /// </summary>
        public const int NoMatch = 0;

        /// <summary>
        /// Defines the constant representing an IP source match.
        /// </summary>
        public const int IpMatch = 1000;

        /// <summary>
        /// Defines the constant representing and exact string match.
        /// </summary>
        public const int ExactMatch = 100;

        /// <summary>
        /// Defines the constant representing a substring match.
        /// </summary>
        public const int SubStringMatch = 10;

        /// <summary>
        /// Defines the constant representing a regular expression match.
        /// </summary>
        public const int RegExMatch = 1;
    }
}
