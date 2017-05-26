using System.Collections.Generic;
using System.Threading.Tasks;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;

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
        void FillUserDataDtoValues(UserItemDataDto dto, UserItemData userData, BaseItemDto itemDto, User user, List<ItemFields> fields);

        bool EnableRememberingTrackSelections { get; }

        bool SupportsPlayedStatus { get; }
    }
}
