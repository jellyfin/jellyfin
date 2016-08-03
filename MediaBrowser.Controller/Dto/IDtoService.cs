using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaBrowser.Controller.Sync;

namespace MediaBrowser.Controller.Dto
{
    /// <summary>
    /// Interface IDtoService
    /// </summary>
    public interface IDtoService
    {
        /// <summary>
        /// Gets the dto id.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        string GetDtoId(BaseItem item);

        /// <summary>
        /// Attaches the primary image aspect ratio.
        /// </summary>
        /// <param name="dto">The dto.</param>
        /// <param name="item">The item.</param>
        void AttachPrimaryImageAspectRatio(IItemDto dto, IHasImages item);

        /// <summary>
        /// Gets the primary image aspect ratio.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.Nullable&lt;System.Double&gt;.</returns>
        double? GetPrimaryImageAspectRatio(IHasImages item);

        /// <summary>
        /// Gets the base item dto.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="fields">The fields.</param>
        /// <param name="user">The user.</param>
        /// <param name="owner">The owner.</param>
        /// <returns>Task{BaseItemDto}.</returns>
        BaseItemDto GetBaseItemDto(BaseItem item, List<ItemFields> fields, User user = null, BaseItem owner = null);

        /// <summary>
        /// Gets the base item dto.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <param name="user">The user.</param>
        /// <param name="owner">The owner.</param>
        /// <returns>BaseItemDto.</returns>
        BaseItemDto GetBaseItemDto(BaseItem item, DtoOptions options, User user = null, BaseItem owner = null);

        /// <summary>
        /// Gets the base item dtos.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="options">The options.</param>
        /// <param name="user">The user.</param>
        /// <param name="owner">The owner.</param>
        /// <returns>IEnumerable&lt;BaseItemDto&gt;.</returns>
        Task<List<BaseItemDto>> GetBaseItemDtos(IEnumerable<BaseItem> items, DtoOptions options, User user = null,
            BaseItem owner = null);
        
        /// <summary>
        /// Gets the chapter information dto.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>ChapterInfoDto.</returns>
        List<ChapterInfoDto> GetChapterInfoDtos(BaseItem item);

        /// <summary>
        /// Gets the user item data dto.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>UserItemDataDto.</returns>
        UserItemDataDto GetUserItemDataDto(UserItemData data);

        /// <summary>
        /// Gets the item by name dto.
        /// </summary>
        BaseItemDto GetItemByNameDto(BaseItem item, DtoOptions options, List<BaseItem> taggedItems, Dictionary<string, SyncedItemProgress> syncProgress, User user = null);

        Dictionary<string, SyncedItemProgress> GetSyncedItemProgress(DtoOptions options);
    }
}
