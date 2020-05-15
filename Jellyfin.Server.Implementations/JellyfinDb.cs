#pragma warning disable CS1591
#pragma warning disable SA1201 // Constuctors should not follow properties
#pragma warning disable SA1516 // Elements should be followed by a blank line
#pragma warning disable SA1623 // Property's documentation should begin with gets or sets
#pragma warning disable SA1629 // Documentation should end with a period
#pragma warning disable SA1648 // Inheritdoc should be used with inheriting class

using System.Linq;
using Jellyfin.Data;
using Jellyfin.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations
{
    /// <inheritdoc/>
    public partial class JellyfinDb : DbContext
    {
        public virtual DbSet<ActivityLog> ActivityLogs { get; set; }
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
        public virtual DbSet<Permission> Permissions { get; set; }
        public virtual DbSet<Person> People { get; set; }
        public virtual DbSet<PersonRole> PersonRoles { get; set; }
        public virtual DbSet<Photo> Photo { get; set; }
        public virtual DbSet<PhotoMetadata> PhotoMetadata { get; set; }
        public virtual DbSet<Preference> Preferences { get; set; }
        public virtual DbSet<ProviderMapping> ProviderMappings { get; set; }
        public virtual DbSet<Rating> Ratings { get; set; }

        /// <summary>
        /// Repository for global::Jellyfin.Data.Entities.RatingSource - This is the entity to
        /// store review ratings, not age ratings
        /// </summary>
        public virtual DbSet<RatingSource> RatingSources { get; set; }
        public virtual DbSet<Release> Releases { get; set; }
        public virtual DbSet<Season> Seasons { get; set; }
        public virtual DbSet<SeasonMetadata> SeasonMetadata { get; set; }
        public virtual DbSet<Series> Series { get; set; }
        public virtual DbSet<SeriesMetadata> SeriesMetadata { get; set; }
        public virtual DbSet<Track> Tracks { get; set; }
        public virtual DbSet<TrackMetadata> TrackMetadata { get; set; }
        public virtual DbSet<User> Users { get; set; } */

        /// <summary>
        /// Gets or sets the default connection string.
        /// </summary>
        public static string ConnectionString { get; set; } = @"Data Source=jellyfin.db";

        /// <inheritdoc />
        public JellyfinDb(DbContextOptions<JellyfinDb> options) : base(options)
        {
        }

        partial void CustomInit(DbContextOptionsBuilder optionsBuilder);

        /// <inheritdoc />
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            CustomInit(optionsBuilder);
        }

        partial void OnModelCreatingImpl(ModelBuilder modelBuilder);
        partial void OnModelCreatedImpl(ModelBuilder modelBuilder);

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            OnModelCreatingImpl(modelBuilder);

            modelBuilder.HasDefaultSchema("jellyfin");

            /*modelBuilder.Entity<Artwork>().HasIndex(t => t.Kind);

            modelBuilder.Entity<Genre>().HasIndex(t => t.Name)
                        .IsUnique();

            modelBuilder.Entity<LibraryItem>().HasIndex(t => t.UrlId)
                        .IsUnique();*/

            OnModelCreatedImpl(modelBuilder);
        }

        public override int SaveChanges()
        {
            foreach (var saveEntity in ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Modified)
                .OfType<ISavingChanges>())
            {
                saveEntity.OnSavingChanges();
            }

            return base.SaveChanges();
        }
    }
}
