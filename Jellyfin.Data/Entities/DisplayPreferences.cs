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
        /// <param name="userId">The user's id.</param>
        /// <param name="itemId">The item id.</param>
        /// <param name="client">The client string.</param>
        public DisplayPreferences(Guid userId, Guid itemId, string client)
        {
            UserId = userId;
            ItemId = itemId;
            Client = client;
            ShowSidebar = false;
            ShowBackdrop = true;
            SkipForwardLength = 30000;
            SkipBackwardLength = 10000;
            ScrollDirection = ScrollDirection.Horizontal;
            ChromecastVersion = ChromecastVersion.Stable;

            HomeSections = new HashSet<HomeSection>();
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
        /// Gets or sets the dashboard theme.
        /// </summary>
        [MaxLength(32)]
        [StringLength(32)]
        public string? DashboardTheme { get; set; }

        /// <summary>
        /// Gets or sets the tv home screen.
        /// </summary>
        [MaxLength(32)]
        [StringLength(32)]
        public string? TvHome { get; set; }

        /// <summary>
        /// Gets the home sections.
        /// </summary>
        public virtual ICollection<HomeSection> HomeSections { get; private set; }
    }
}
