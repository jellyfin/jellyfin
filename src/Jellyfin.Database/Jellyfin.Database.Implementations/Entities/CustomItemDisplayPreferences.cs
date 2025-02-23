using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    /// <summary>
    /// An entity that represents a user's custom display preferences for a specific item.
    /// </summary>
    public class CustomItemDisplayPreferences
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomItemDisplayPreferences"/> class.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="itemId">The item id.</param>
        /// <param name="client">The client.</param>
        /// <param name="key">The preference key.</param>
        /// <param name="value">The preference value.</param>
        public CustomItemDisplayPreferences(Guid userId, Guid itemId, string client, string key, string? value)
        {
            UserId = userId;
            ItemId = itemId;
            Client = client;
            Key = key;
            Value = value;
        }

        /// <summary>
        /// Gets the Id.
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
        /// Gets or sets the preference key.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the preference value.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public string? Value { get; set; }
    }
}
