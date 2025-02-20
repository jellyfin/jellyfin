using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jellyfin.Data.Enums;

namespace Jellyfin.Data.Entities
{
    /// <summary>
    /// An entity that represents a user's display preferences for a specific item.
    /// </summary>
    public class ItemDisplayPreferences
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemDisplayPreferences"/> class.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="itemId">The item id.</param>
        /// <param name="client">The client.</param>
        public ItemDisplayPreferences(Guid userId, Guid itemId, string client)
        {
            UserId = userId;
            ItemId = itemId;
            Client = client;

            SortBy = "SortName";
            SortOrder = SortOrder.Ascending;
            RememberSorting = false;
            RememberIndexing = false;
        }

        /// <summary>
        /// Gets the id.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }

        /// <summary>
        /// Gets or sets the user Id.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the id of the associated item.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public Guid ItemId { get; set; }

        /// <summary>
        /// Gets or sets the client string.
        /// </summary>
        /// <remarks>
        /// Required. Max Length = 32.
        /// </remarks>
        [MaxLength(32)]
        [StringLength(32)]
        public string Client { get; set; }

        /// <summary>
        /// Gets or sets the view type.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public ViewType ViewType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the indexing should be remembered.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public bool RememberIndexing { get; set; }

        /// <summary>
        /// Gets or sets what the view should be indexed by.
        /// </summary>
        public IndexingKind? IndexBy { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the sorting type should be remembered.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public bool RememberSorting { get; set; }

        /// <summary>
        /// Gets or sets what the view should be sorted by.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        [MaxLength(64)]
        [StringLength(64)]
        public string SortBy { get; set; }

        /// <summary>
        /// Gets or sets the sort order.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public SortOrder SortOrder { get; set; }
    }
}
