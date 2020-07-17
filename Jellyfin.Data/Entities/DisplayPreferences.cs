using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jellyfin.Data.Enums;

namespace Jellyfin.Data.Entities
{
    /// <summary>
    /// An entity representing a user's display preferences.
    /// </summary>
    public class DisplayPreferences
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DisplayPreferences"/> class.
        /// </summary>
        /// <param name="client">The client string.</param>
        /// <param name="userId">The user's id.</param>
        public DisplayPreferences(string client, Guid userId)
        {
            RememberIndexing = false;
            ShowBackdrop = true;
            Client = client;
            UserId = userId;

            HomeSections = new HashSet<HomeSection>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DisplayPreferences"/> class.
        /// </summary>
        protected DisplayPreferences()
        {
        }

        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; protected set; }

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
        /// This is currently unused. In the future, this will allow us to have users set
        /// display preferences per item.
        /// </remarks>
        public Guid? ItemId { get; set; }

        /// <summary>
        /// Gets or sets the client string.
        /// </summary>
        /// <remarks>
        /// Required. Max Length = 64.
        /// </remarks>
        [Required]
        [MaxLength(64)]
        [StringLength(64)]
        public string Client { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the indexing should be remembered.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public bool RememberIndexing { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the sorting type should be remembered.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public bool RememberSorting { get; set; }

        /// <summary>
        /// Gets or sets the sort order.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public SortOrder SortOrder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show the sidebar.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public bool ShowSidebar { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show the backdrop.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public bool ShowBackdrop { get; set; }

        /// <summary>
        /// Gets or sets what the view should be sorted by.
        /// </summary>
        [MaxLength(64)]
        [StringLength(64)]
        public string SortBy { get; set; }

        /// <summary>
        /// Gets or sets the view type.
        /// </summary>
        public ViewType? ViewType { get; set; }

        /// <summary>
        /// Gets or sets the scroll direction.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public ScrollDirection ScrollDirection { get; set; }

        /// <summary>
        /// Gets or sets what the view should be indexed by.
        /// </summary>
        public IndexingKind? IndexBy { get; set; }

        /// <summary>
        /// Gets or sets the length of time to skip forwards, in milliseconds.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public int SkipForwardLength { get; set; }

        /// <summary>
        /// Gets or sets the length of time to skip backwards, in milliseconds.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public int SkipBackwardLength { get; set; }

        /// <summary>
        /// Gets or sets the Chromecast Version.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public ChromecastVersion ChromecastVersion { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the next video info overlay should be shown.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public bool EnableNextVideoInfoOverlay { get; set; }

        /// <summary>
        /// Gets or sets the home sections.
        /// </summary>
        public virtual ICollection<HomeSection> HomeSections { get; protected set; }
    }
}
