using System;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Entities.Security;
using Jellyfin.Database.Implementations.Interfaces;
using Jellyfin.Database.Implementations.Locking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Database.Implementations;

/// <inheritdoc/>
/// <summary>
/// Initializes a new instance of the <see cref="JellyfinDbContext"/> class.
/// </summary>
/// <param name="options">The database context options.</param>
/// <param name="logger">Logger.</param>
/// <param name="jellyfinDatabaseProvider">The provider for the database engine specific operations.</param>
/// <param name="entityFrameworkCoreLocking">The locking behavior.</param>
public class JellyfinDbContext(DbContextOptions<JellyfinDbContext> options, ILogger<JellyfinDbContext> logger, IJellyfinDatabaseProvider jellyfinDatabaseProvider, IEntityFrameworkCoreLockingBehavior entityFrameworkCoreLocking) : DbContext(options)
{
    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> containing the access schedules.
    /// </summary>
    public DbSet<AccessSchedule> AccessSchedules => Set<AccessSchedule>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> containing the activity logs.
    /// </summary>
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> containing the API keys.
    /// </summary>
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> containing the devices.
    /// </summary>
    public DbSet<Device> Devices => Set<Device>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> containing the device options.
    /// </summary>
    public DbSet<DeviceOptions> DeviceOptions => Set<DeviceOptions>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> containing the display preferences.
    /// </summary>
    public DbSet<DisplayPreferences> DisplayPreferences => Set<DisplayPreferences>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> containing the image infos.
    /// </summary>
    public DbSet<ImageInfo> ImageInfos => Set<ImageInfo>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> containing the item display preferences.
    /// </summary>
    public DbSet<ItemDisplayPreferences> ItemDisplayPreferences => Set<ItemDisplayPreferences>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> containing the custom item display preferences.
    /// </summary>
    public DbSet<CustomItemDisplayPreferences> CustomItemDisplayPreferences => Set<CustomItemDisplayPreferences>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> containing the permissions.
    /// </summary>
    public DbSet<Permission> Permissions => Set<Permission>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> containing the preferences.
    /// </summary>
    public DbSet<Preference> Preferences => Set<Preference>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> containing the users.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> containing the trickplay metadata.
    /// </summary>
    public DbSet<TrickplayInfo> TrickplayInfos => Set<TrickplayInfo>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> containing the media segments.
    /// </summary>
    public DbSet<MediaSegment> MediaSegments => Set<MediaSegment>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> containing the user data.
    /// </summary>
    public DbSet<UserData> UserData => Set<UserData>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> containing the user data.
    /// </summary>
    public DbSet<AncestorId> AncestorIds => Set<AncestorId>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> containing the user data.
    /// </summary>
    public DbSet<AttachmentStreamInfo> AttachmentStreamInfos => Set<AttachmentStreamInfo>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> containing the user data.
    /// </summary>
    public DbSet<BaseItemEntity> BaseItems => Set<BaseItemEntity>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> containing the user data.
    /// </summary>
    public DbSet<Chapter> Chapters => Set<Chapter>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/>.
    /// </summary>
    public DbSet<ItemValue> ItemValues => Set<ItemValue>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/>.
    /// </summary>
    public DbSet<ItemValueMap> ItemValuesMap => Set<ItemValueMap>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/>.
    /// </summary>
    public DbSet<MediaStreamInfo> MediaStreamInfos => Set<MediaStreamInfo>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/>.
    /// </summary>
    public DbSet<People> Peoples => Set<People>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/>.
    /// </summary>
    public DbSet<PeopleBaseItemMap> PeopleBaseItemMap => Set<PeopleBaseItemMap>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> containing the referenced Providers with ids.
    /// </summary>
    public DbSet<BaseItemProvider> BaseItemProviders => Set<BaseItemProvider>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/>.
    /// </summary>
    public DbSet<BaseItemImageInfo> BaseItemImageInfos => Set<BaseItemImageInfo>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/>.
    /// </summary>
    public DbSet<BaseItemMetadataField> BaseItemMetadataFields => Set<BaseItemMetadataField>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/>.
    /// </summary>
    public DbSet<BaseItemTrailerType> BaseItemTrailerTypes => Set<BaseItemTrailerType>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/>.
    /// </summary>
    public DbSet<KeyframeData> KeyframeData => Set<KeyframeData>();

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
    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        HandleConcurrencyToken();

        try
        {
            var result = -1;
            await entityFrameworkCoreLocking.OnSaveChangesAsync(this, async () =>
            {
                result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
            return result;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error trying to save changes.");
            throw;
        }
    }

    /// <inheritdoc/>
    public override int SaveChanges(bool acceptAllChangesOnSuccess) // SaveChanges(bool) is beeing called by SaveChanges() with default to false.
    {
        HandleConcurrencyToken();

        try
        {
            var result = -1;
            entityFrameworkCoreLocking.OnSaveChanges(this, () =>
            {
                result = base.SaveChanges(acceptAllChangesOnSuccess);
            });
            return result;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error trying to save changes.");
            throw;
        }
    }

    private void HandleConcurrencyToken()
    {
        foreach (var saveEntity in ChangeTracker.Entries()
                     .Where(e => e.State == EntityState.Modified)
                     .Select(entry => entry.Entity)
                     .OfType<IHasConcurrencyToken>())
        {
            saveEntity.OnSavingChanges();
        }
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        jellyfinDatabaseProvider.OnModelCreating(modelBuilder);
        base.OnModelCreating(modelBuilder);

        // Configuration for each entity is in its own class inside 'ModelConfiguration'.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JellyfinDbContext).Assembly);
    }

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        jellyfinDatabaseProvider.ConfigureConventions(configurationBuilder);
        base.ConfigureConventions(configurationBuilder);
    }
}
