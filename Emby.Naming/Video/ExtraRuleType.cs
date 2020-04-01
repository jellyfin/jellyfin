#pragma warning disable CS1591

namespace Emby.Naming.Video
{
    public enum ExtraRuleType
    {
        /// <summary>
        /// Match <see cref="ExtraRule.Token"/> against a suffix in the file name.
        /// </summary>
        Suffix = 0,

        /// <summary>
        /// Match <see cref="ExtraRule.Token"/> against the file name.
        /// </summary>
        Filename = 1,

        /// <summary>
        /// Match <see cref="ExtraRule.Token"/> against the a regex.
        /// </summary>
        Regex = 2,

        /// <summary>
        /// Match <see cref="ExtraRule.Token"/> against the name of the directory containing the file.
        /// </summary>
        DirectoryName = 3,
    }
}
