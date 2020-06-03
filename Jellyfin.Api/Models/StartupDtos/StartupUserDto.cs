#nullable disable

namespace Jellyfin.Api.Models.StartupDtos
{
    /// <summary>
    /// The startup user DTO.
    /// </summary>
    public class StartupUserDto
    {
        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the user's password.
        /// </summary>
        public string Password { get; set; }
    }
}
