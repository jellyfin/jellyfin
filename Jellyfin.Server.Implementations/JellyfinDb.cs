#nullable disable
#pragma warning disable CS1591

using System;
using System.Linq;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations
{
    /// <inheritdoc/>
    public class JellyfinDb : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JellyfinDb"/> class.
        /// </summary>
        /// <param name="options">The database context options.</param>
        public JellyfinDb(DbContextOptions<JellyfinDb> options) : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the default connection string.
        /// </summary>
        public static string ConnectionString { get; set; } = @"Data Source=jellyfin.db";

        public virtual DbSet<AccessSchedule> AccessSchedules { get; set; }

        public virtual DbSet<ActivityLog> ActivityLogs { get; set; }

        public virtual DbSet<DisplayPreferences> DisplayPreferences { get; set; }

        public virtual DbSet<ImageInfo> ImageInfos { get; set; }

        public virtual DbSet<ItemDisplayPreferences> ItemDisplayPreferences { get; set; }

        public virtual DbSet<CustomItemDisplayPreferences> CustomItemDisplayPreferences { get; set; }

        public virtual DbSet<Permission> Permissions { get; set; }

        public virtual DbSet<Preference> Preferences { get; set; }

        public virtual DbSet<User> Users { get; set; }

        /*public virtual DbSet<Artwork> Artwork { get; set; }

        public virtual DbSet<Book> Books { get; set; }

        public virtual DbSet<BookMetadata> BookMetadata { get; set; }

        public virtual DbSet<Chapter> Chapters { get; set; }

        public virtual DbSet<Collection> Collections { get; set; }

        public virtual DbSet<CollectionItem> CollectionItems { get; set; }

        public virtual DbSet<Company> Companies { get; set; }

        public virtual DbSet<CompanyMetadata> CompanyMetadata { get; set; }

        public virtual DbSet<CustomItem> CustomItems { get; set; }

        public virtual DbSet<CustomItemMetadata> CustomItemMetadata { get; set; }

        public virtual DbSet<Episode> Episodes { get; set; }

        public virtual DbSet<EpisodeMetadata> EpisodeMetadata { get; set; }

        public virtual DbSet<Genre> Genres { get; set; }

        public virtual DbSet<Group> Groups { get; set; }

        public virtual DbSet<Library> Libraries { get; set; }

        public virtual DbSet<LibraryItem> LibraryItems { get; set; }

        public virtual DbSet<LibraryRoot> LibraryRoot { get; set; }

        public virtual DbSet<MediaFile> MediaFiles { get; set; }

        public virtual DbSet<MediaFileStream> MediaFileStream { get; set; }

        public virtual DbSet<Metadata> Metadata { get; set; }

        public virtual DbSet<MetadataProvider> MetadataProviders { get; set; }

        public virtual DbSet<MetadataProviderId> MetadataProviderIds { get; set; }

        public virtual DbSet<Movie> Movies { get; set; }

        public virtual DbSet<MovieMetadata> MovieMetadata { get; set; }

        public virtual DbSet<MusicAlbum> MusicAlbums { get; set; }

        public virtual DbSet<MusicAlbumMetadata> MusicAlbumMetadata { get; set; }

        public virtual DbSet<Person> People { get; set; }

        public virtual DbSet<PersonRole> PersonRoles { get; set; }

        public virtual DbSet<Photo> Photo { get; set; }

        public virtual DbSet<PhotoMetadata> PhotoMetadata { get; set; }

        public virtual DbSet<ProviderMapping> ProviderMappings { get; set; }

        public virtual DbSet<Rating> Ratings { get; set; }

        /// <summary>
        /// Repository for global::Jellyfin.Data.Entities.RatingSource - This is the entity to
        /// store review ratings, not age ratings.
        /// </summary>
        public virtual DbSet<RatingSource> RatingSources { get; set; }

        public virtual DbSet<Release> Releases { get; set; }

        public virtual DbSet<Season> Seasons { get; set; }

        public virtual DbSet<SeasonMetadata> SeasonMetadata { get; set; }

        public virtual DbSet<Series> Series { get; set; }

        public virtual DbSet<SeriesMetadata> SeriesMetadata { get; set; }

        public virtual DbSet<Track> Tracks { get; set; }

        public virtual DbSet<TrackMetadata> TrackMetadata { get; set; }*/

        /// <inheritdoc/>
        public override int SaveChanges()
        {
            foreach (var saveEntity in ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Modified)
                .Select(entry => entry.Entity)
                .OfType<IHasConcurrencyToken>())
            {
                saveEntity.OnSavingChanges();
            }

            return base.SaveChanges();
        }

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.SetDefaultDateTimeKind(DateTimeKind.Utc);
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema("jellyfin");

            // Collations

            modelBuilder.Entity<User>()
                .Property(user => user.Username)
                .UseCollation("NOCASE");

            // Delete behavior

            modelBuilder.Entity<User>()
                .HasOne(u => u.ProfileImage)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Permissions)
                .WithOne()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Preferences)
                .WithOne()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.AccessSchedules)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.DisplayPreferences)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.ItemDisplayPreferences)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DisplayPreferences>()
                .HasMany(d => d.HomeSections)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes

            modelBuilder.Entity<User>()
                .HasIndex(entity => entity.Username)
                .IsUnique();

            modelBuilder.Entity<DisplayPreferences>()
                .HasIndex(entity => new { entity.UserId, entity.ItemId, entity.Client })
                .IsUnique();

            modelBuilder.Entity<CustomItemDisplayPreferences>()
                .HasIndex(entity => new { entity.UserId, entity.ItemId, entity.Client, entity.Key })
                .IsUnique();

            // Used to get a user's permissions or a specific permission for a user.
            // Also prevents multiple values being created for a user.
            // Filtered over non-null user ids for when other entities (groups, API keys) get permissions
            modelBuilder.Entity<Permission>()
                .HasIndex(p => new { p.UserId, p.Kind })
                .HasFilter("[UserId] IS NOT NULL")
                .IsUnique();

            modelBuilder.Entity<Preference>()
                .HasIndex(p => new { p.UserId, p.Kind })
                .HasFilter("[UserId] IS NOT NULL")
                .IsUnique();
        }
    }
}
