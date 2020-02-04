#pragma warning disable CS1591
#pragma warning disable SA1600

namespace MediaBrowser.Model.Users
{
    public enum ForgotPasswordAction
    {
        ContactAdmin = 0,
        PinCode = 1,
        InNetworkRequired = 2
    }
}
