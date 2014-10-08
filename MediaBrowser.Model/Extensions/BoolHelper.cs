namespace MediaBrowser.Model.Extensions
{
    public static class BoolHelper
    {
        /// <summary>
        /// Tries the parse culture invariant.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="result">The result.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool TryParseCultureInvariant(string s, out bool result)
        {
            return bool.TryParse(s, out result);
        }
    }
}