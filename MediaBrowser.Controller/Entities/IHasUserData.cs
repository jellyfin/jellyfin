using MediaBrowser.Model.Dto;
using System;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Interface IHasUserData
    /// </summary>
    public interface IHasUserData
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        Guid Id { get; set; }

        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        string GetUserDataKey();

        /// <summary>
        /// Fills the user data dto values.
        /// </summary>
        /// <param name="dto">The dto.</param>
        /// <param name="userData">The user data.</param>
        /// <param name="user">The user.</param>
        void FillUserDataDtoValues(UserItemDataDto dto, UserItemData userData, User user);
    }
}
