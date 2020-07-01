using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jellyfin.Data.Enums;

namespace Jellyfin.Data.Entities
{
    public class DisplayPreferences
    {
        public DisplayPreferences(string client, Guid userId)
        {
            RememberIndexing = false;
            ShowBackdrop = true;
            Client = client;
            UserId = userId;

            HomeSections = new HashSet<HomeSection>();
        }

        protected DisplayPreferences()
        {
        }

        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; protected set; }

        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the id of the associated item.
        /// </summary>
        /// <remarks>
        /// This is currently unused. In the future, this will allow us to have users set
        /// display preferences per item.
        /// </remarks>
        public Guid? ItemId { get; set; }

        [Required]
        [MaxLength(64)]
        [StringLength(64)]
        public string Client { get; set; }

        [Required]
        public bool RememberIndexing { get; set; }

        [Required]
        public bool RememberSorting { get; set; }

        [Required]
        public SortOrder SortOrder { get; set; }

        [Required]
        public bool ShowSidebar { get; set; }

        [Required]
        public bool ShowBackdrop { get; set; }

        public string SortBy { get; set; }

        public ViewType? ViewType { get; set; }

        [Required]
        public ScrollDirection ScrollDirection { get; set; }

        public IndexingKind? IndexBy { get; set; }

        public virtual ICollection<HomeSection> HomeSections { get; protected set; }
    }
}
