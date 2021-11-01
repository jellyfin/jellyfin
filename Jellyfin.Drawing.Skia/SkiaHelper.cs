using SkiaSharp;

namespace Jellyfin.Drawing.Skia
{
    /// <summary>
    /// Class containing helper methods for working with SkiaSharp.
    /// </summary>
    public static class SkiaHelper
    {
        /// <summary>
        /// Ensures the result is a success
        /// by throwing an exception when that's not the case.
        /// </summary>
        /// <param name="result">The result returned by Skia.</param>
        public static void EnsureSuccess(SKCodecResult result)
        {
            if (result != SKCodecResult.Success)
            {
                throw new SkiaCodecException(result);
            }
        }
    }
}
