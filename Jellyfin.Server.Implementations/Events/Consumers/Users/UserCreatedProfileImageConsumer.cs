using System;
using System.Threading.Tasks;
using Jellyfin.Data.Events.Users;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Events;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.Events.Consumers.Users
{
    /// <summary>
    /// Generates and saves a default profile image when a new user is created.
    /// </summary>
    public class UserCreatedProfileImageConsumer : IEventConsumer<UserCreatedEventArgs>
    {
        private readonly IProfileImageService _profileImageService;
        private readonly ILogger<UserCreatedProfileImageConsumer> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserCreatedProfileImageConsumer"/> class.
        /// </summary>
        /// <param name="profileImageService">The profile image service.</param>
        /// <param name="logger">The logger.</param>
        public UserCreatedProfileImageConsumer(
            IProfileImageService profileImageService,
            ILogger<UserCreatedProfileImageConsumer> logger)
        {
            _profileImageService = profileImageService;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task OnEvent(UserCreatedEventArgs eventArgs)
        {
            var user = eventArgs.Argument;
            try
            {
                await _profileImageService.GenerateAndSaveProfileImageAsync(user).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate default profile image for user {UserId}", user.Id);
            }
        }
    }
}
