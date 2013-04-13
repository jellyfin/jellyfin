using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;

namespace MediaBrowser.Controller.Dto
{
    /// <summary>
    /// Class UserDtoBuilder
    /// </summary>
    public class UserDtoBuilder
    {
        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserDtoBuilder"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public UserDtoBuilder(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Converts a User to a DTOUser
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>DtoUser.</returns>
        /// <exception cref="System.ArgumentNullException">user</exception>
        public UserDto GetUserDto(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            var dto = new UserDto
            {
                Id = user.Id.ToString(),
                Name = user.Name,
                HasPassword = !String.IsNullOrEmpty(user.Password),
                LastActivityDate = user.LastActivityDate,
                LastLoginDate = user.LastLoginDate,
                Configuration = user.Configuration
            };

            var image = user.PrimaryImagePath;

            if (!string.IsNullOrEmpty(image))
            {
                dto.PrimaryImageTag = Kernel.Instance.ImageManager.GetImageCacheTag(user, ImageType.Primary, image);

                try
                {
                    DtoBuilder.AttachPrimaryImageAspectRatio(dto, user, _logger);
                }
                catch (Exception ex)
                {
                    // Have to use a catch-all unfortunately because some .net image methods throw plain Exceptions
                    _logger.ErrorException("Error generating PrimaryImageAspectRatio for {0}", ex, user.Name);
                }
            }

            return dto;
        }
    }
}
