namespace Jellyfin.Api.Constants
{
    /// <summary>
    /// Values for the <c>X-Application-Error-Code</c> response header.
    /// </summary>
    public static class ApplicationErrorCodes
    {
        /// <summary>
        /// The name of the response header carrying the error code.
        /// </summary>
        public const string HeaderName = "X-Application-Error-Code";

        /// <summary>
        /// The request was refused by parental control.
        /// </summary>
        public const string ParentalControl = "ParentalControl";
    }
}
