using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Api.Models.UserDtos
{
    /// <summary>
    /// Forgot Password request body DTO.
    /// </summary>
    public class ForgotPasswordDto
    {
        /// <summary>
        /// Gets or sets the entered username to have its password reset.
        /// </summary>
        [Required]
        public string? EnteredUsername { get; set; }
    }
}
