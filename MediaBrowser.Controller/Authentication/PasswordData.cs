namespace MediaBrowser.Controller.Authentication
{
    /// <summary>
    /// Record to hold password data for password-based authentication.
    /// </summary>
    public record struct PasswordData(string? Password);
}
