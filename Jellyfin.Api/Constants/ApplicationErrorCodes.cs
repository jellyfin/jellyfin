namespace Jellyfin.Api.Constants
{
    /// <summary>
    /// Values for the <c>X-Application-Error-Code</c> response header.
    /// </summary>
    /// <remarks>
    /// Clients read this header to explain a failure to the user. jellyfin-web keys off
    /// <see cref="ParentalControl"/> to show a message and return to the login screen
    /// instead of rendering an empty library.
    /// </remarks>
    public static class ApplicationErrorCodes
    {
        /// <summary>
        /// The name of the response header carrying the error code.
        /// </summary>
        public const string HeaderName = "X-Application-Error-Code";

        /// <summary>
        /// The request was refused by parental control, for example an access schedule.
        /// </summary>
        public const string ParentalControl = "ParentalControl";
    }
}
