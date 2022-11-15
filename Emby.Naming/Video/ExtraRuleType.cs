namespace Emby.Naming.Video
{
    /// <summary>
    /// Extra rules type to determine against what <see cref="ExtraRule.Token"/> should be matched.
    /// </summary>
    public enum ExtraRuleType
    {
        /// <summary>
        /// Match <see cref="ExtraRule.Token"/> against a suffix in the file name.
        /// </summary>
        Suffix = 0,

        /// <summary>
        /// Match <see cref="ExtraRule.Token"/> against the file name, excluding the file extension.
        /// </summary>
        Filename = 1,

        /// <summary>
        /// Match <see cref="ExtraRule.Token"/> against the file name, including the file extension.
        /// </summary>
        Regex = 2,

        /// <summary>
        /// Match <see cref="ExtraRule.Token"/> against the name of the directory containing the file.
        /// </summary>
        DirectoryName = 3
    }
}
