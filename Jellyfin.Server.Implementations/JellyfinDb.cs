#pragma warning disable CS1591

using System;
using System.Linq;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Entities.Security;
using Jellyfin.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations;

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

    public DbSet<AccessSchedule> AccessSchedules => Set<AccessSchedule>();

    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    public DbSet<Device> Devices => Set<Device>();

    public DbSet<DeviceOptions> DeviceOptions => Set<DeviceOptions>();

    public DbSet<DisplayPreferences> DisplayPreferences => Set<DisplayPreferences>();

    public DbSet<ImageInfo> ImageInfos => Set<ImageInfo>();

    public DbSet<ItemDisplayPreferences> ItemDisplayPreferences => Set<ItemDisplayPreferences>();

    public DbSet<CustomItemDisplayPreferences> CustomItemDisplayPreferences => Set<CustomItemDisplayPreferences>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<Preference> Preferences => Set<Preference>();

    public DbSet<User> Users => Set<User>();

    /*public DbSet<Artwork> Artwork => Set<Artwork>();

    public DbSet<Book> Books => Set<Book>();

    public DbSet<BookMetadata> BookMetadata => Set<BookMetadata>();

    public DbSet<Chapter> Chapters => Set<Chapter>();

    public DbSet<Collection> Collections => Set<Collection>();

    public DbSet<CollectionItem> CollectionItems => Set<CollectionItem>();

    public DbSet<Company> Companies => Set<Company>();

    public DbSet<CompanyMetadata> CompanyMetadata => Set<CompanyMetadata>();

    public DbSet<CustomItem> CustomItems => Set<CustomItem>();

    public DbSet<CustomItemMetadata> CustomItemMetadata => Set<CustomItemMetadata>();

    public DbSet<Episode> Episodes => Set<Episode>();

    public DbSet<EpisodeMetadata> EpisodeMetadata => Set<EpisodeMetadata>();

    public DbSet<Genre> Genres => Set<Genre>();

    public DbSet<Group> Groups => Set<Groups>();

    public DbSet<Library> Libraries => Set<Library>();

    public DbSet<LibraryItem> LibraryItems => Set<LibraryItems>();

    public DbSet<LibraryRoot> LibraryRoot => Set<LibraryRoot>();

    public DbSet<MediaFile> MediaFiles => Set<MediaFiles>();

    public DbSet<MediaFileStream> MediaFileStream => Set<MediaFileStream>();

    public DbSet<Metadata> Metadata => Set<Metadata>();

    public DbSet<MetadataProvider> MetadataProviders => Set<MetadataProvider>();

    public DbSet<MetadataProviderId> MetadataProviderIds => Set<MetadataProviderId>();

    public DbSet<Movie> Movies => Set<Movie>();

    public DbSet<MovieMetadata> MovieMetadata => Set<MovieMetadata>();

    public DbSet<MusicAlbum> MusicAlbums => Set<MusicAlbum>();

    public DbSet<MusicAlbumMetadata> MusicAlbumMetadata => Set<MusicAlbumMetadata>();

    public DbSet<Person> People => Set<Person>();

    public DbSet<PersonRole> PersonRoles => Set<PersonRole>();

    public DbSet<Photo> Photo => Set<Photo>();

    public DbSet<PhotoMetadata> PhotoMetadata => Set<PhotoMetadata>();

    public DbSet<ProviderMapping> ProviderMappings => Set<ProviderMapping>();

    public DbSet<Rating> Ratings => Set<Rating>();

    /// <summary>
    /// Repository for global::Jellyfin.Data.Entities.RatingSource - This is the entity to
    /// store review ratings, not age ratings.
    /// </summary>
    public DbSet<RatingSource> RatingSources => Set<RatingSource>();

    public DbSet<Release> Releases => Set<Release>();

    public DbSet<Season> Seasons => Set<Season>();

    public DbSet<SeasonMetadata> SeasonMetadata => Set<SeasonMetadata>();

    public DbSet<Series> Series => Set<Series>();

    public DbSet<SeriesMetadata> SeriesMetadata => Set<SeriesMetadata();

    public DbSet<Track> Tracks => Set<Track>();

    public DbSet<TrackMetadata> TrackMetadata => Set<TrackMetadata>();*/

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

        // Configuration for each entity is in it's own class inside 'ModelConfiguration'.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JellyfinDb).Assembly);
    }
}
