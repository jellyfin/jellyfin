using System;
using System.Text.RegularExpressions;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Server.Implementations.Users
{
    /// <summary>
    /// UserValidation.
    /// </summary>
    public partial class UserValidation : IUserValidation
    {
        // This is some regex that matches only on unicode "word" characters, as well as -, _ and @
        // In theory this will cut out most if not all 'control' characters which should help minimize any weirdness
        // Usernames can contain letters (a-z + whatever else unicode is cool with), numbers (0-9), at-signs (@), dashes (-), underscores (_), apostrophes ('), periods (.) and spaces ( )
        [GeneratedRegex(@"^(?!\s)[\w\ \-'._@+]+(?<!\s)$")]
        private static partial Regex ValidUsernameRegex();

        /// <inheritdoc/>
        public void ThrowIfInvalidUsername(string name)
        {
            if (!string.IsNullOrWhiteSpace(name) && ValidUsernameRegex().IsMatch(name))
            {
                return;
            }

            throw new ArgumentException("Usernames can contain unicode symbols, numbers (0-9), dashes (-), underscores (_), apostrophes ('), and periods (.)", nameof(name));
        }

        /// <inheritdoc/>
        public bool IsValidUsername(string name)
        {
            return ValidUsernameRegex().IsMatch(name);
        }
    }
}
