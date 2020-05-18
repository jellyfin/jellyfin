#pragma warning disable CS1591

namespace MediaBrowser.Model.Users
{
    public enum ForgotPasswordAction
    {
        /// <summary>
        /// Contact admin
        /// </summary>
        ContactAdmin = 0,

        /// <summary>
        /// PIN code
        /// </summary>
        PinCode = 1,

        /// <summary>
        /// In network required
        /// </summary>
        InNetworkRequired = 2
    }
}
