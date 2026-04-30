using System;
using System.IO;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;

namespace MediaBrowser.Controller.Drawing
{
    /// <summary>
    /// Service for generating default profile images.
    /// </summary>
    public interface IProfileImageService
    {
        /// <summary>
        /// Generates a PNG profile image for the given display name and user identity.
        /// The returned stream must be disposed by the caller; storage is the caller's responsibility.
        /// </summary>
        /// <param name="displayName">The display name used to derive the initials rendered on the image.</param>
        /// <param name="userId">The user's unique identifier, used for deterministic color selection.</param>
        /// <returns>A <see cref="Stream"/> containing PNG image data.</returns>
        Task<Stream> GenerateProfileImageAsync(string displayName, Guid userId);

        /// <summary>
        /// Generates a profile image for the user, saves it to the user's profile image path,
        /// and updates the user's <see cref="User.ProfileImage"/> in the database.
        /// Any previous profile image file is deleted before saving.
        /// </summary>
        /// <param name="user">The user to generate and save a profile image for.</param>
        /// <param name="displayName">
        /// Optional display name override used to derive initials.
        /// When <c>null</c>, the user's <see cref="User.Username"/> is used.
        /// </param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task GenerateAndSaveProfileImageAsync(User user, string? displayName = null);
    }
}
