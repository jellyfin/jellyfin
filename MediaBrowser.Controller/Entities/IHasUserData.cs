using System.Collections.Generic;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Interface IHasUserData
    /// </summary>
    public interface IHasUserData : IHasId
    {
        List<string> GetUserDataKeys();

        /// <summary>
        /// Fills the user data dto values.
        /// </summary>
        /// <param name="dto">The dto.</param>
        /// <param name="userData">The user data.</param>
        /// <param name="user">The user.</param>
        void FillUserDataDtoValues(UserItemDataDto dto, UserItemData userData, User user);

        bool EnableRememberingTrackSelections { get; }
    }
}
