#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.CustomNetflix;
using Npgsql;
using NpgsqlTypes;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal sealed class PostgreSqlCustomNetflixRepository : ICustomNetflixRepository
{
    private const string SelectProfileSql = """
        select
            p.id,
            p.jellyfin_user_id,
            p.name,
            p.avatar_id,
            p.is_default,
            p.is_child,
            p.created_at,
            p.updated_at,
            coalesce(s.autoplay_enabled, true) as autoplay_enabled,
            coalesce(s.autoplay_delay_seconds, 8) as autoplay_delay_seconds,
            coalesce(s.skip_intro_enabled, true) as skip_intro_enabled,
            coalesce(s.skip_recap_enabled, true) as skip_recap_enabled,
            coalesce(pp.prefer_direct_play, true) as prefer_direct_play,
            coalesce(pp.allow_container_remuxing, true) as allow_container_remuxing,
            coalesce(pp.allow_video_transcoding, true) as allow_video_transcoding,
            coalesce(pp.allow_audio_transcoding, true) as allow_audio_transcoding,
            coalesce(pp.prefer_hardware_transcoding, true) as prefer_hardware_transcoding,
            pp.max_streaming_bitrate,
            pp.preferred_audio_language,
            pp.preferred_subtitle_language,
            coalesce(pp.subtitles_enabled, false) as subtitles_enabled,
            coalesce(pp.audio_description_enabled, false) as audio_description_enabled,
            coalesce(pp.closed_captions_enabled, false) as closed_captions_enabled,
            coalesce(pp.skip_credits_enabled, false) as skip_credits_enabled
        from cnx_profiles p
        left join cnx_profile_settings s on s.profile_id = p.id
        left join cnx_playback_preferences pp on pp.profile_id = p.id
        """;

    private readonly NpgsqlDataSource _dataSource;

    public PostgreSqlCustomNetflixRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public bool IsEnabled => true;

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        const string baseSchemaSql = """
            create table if not exists cnx_profiles (
                id uuid primary key,
                jellyfin_user_id uuid not null,
                name text not null check (char_length(name) between 1 and 64),
                avatar_id text null,
                is_default boolean not null default false,
                is_child boolean not null default false,
                created_at timestamptz not null default now(),
                updated_at timestamptz not null default now(),
                deleted_at timestamptz null
            );

            create unique index if not exists ux_cnx_profiles_default
                on cnx_profiles(jellyfin_user_id)
                where is_default = true and deleted_at is null;

            create index if not exists ix_cnx_profiles_user
                on cnx_profiles(jellyfin_user_id)
                where deleted_at is null;

            create table if not exists cnx_profile_settings (
                profile_id uuid primary key references cnx_profiles(id),
                autoplay_enabled boolean not null default true,
                autoplay_delay_seconds int not null default 8 check (autoplay_delay_seconds between 0 and 60),
                skip_intro_enabled boolean not null default true,
                skip_recap_enabled boolean not null default true,
                updated_at timestamptz not null default now()
            );

            create table if not exists cnx_playback_preferences (
                profile_id uuid primary key references cnx_profiles(id),
                prefer_direct_play boolean not null default true,
                allow_container_remuxing boolean not null default true,
                allow_video_transcoding boolean not null default true,
                allow_audio_transcoding boolean not null default true,
                prefer_hardware_transcoding boolean not null default true,
                max_streaming_bitrate int null check (
                    max_streaming_bitrate is null
                    or (max_streaming_bitrate between 1000000 and 500000000)
                ),
                preferred_audio_language text null,
                preferred_subtitle_language text null,
                subtitles_enabled boolean not null default false,
                audio_description_enabled boolean not null default false,
                closed_captions_enabled boolean not null default false,
                skip_credits_enabled boolean not null default false,
                autoplay_consecutive_count int not null default 0,
                autoplay_last_item_id uuid null,
                still_watching_required boolean not null default false,
                still_watching_confirmed_at timestamptz null,
                updated_at timestamptz not null default now()
            );

            create table if not exists cnx_active_profiles (
                jellyfin_user_id uuid not null,
                token_hash text not null,
                profile_id uuid not null references cnx_profiles(id),
                updated_at timestamptz not null default now(),
                primary key (jellyfin_user_id, token_hash)
            );

            create table if not exists cnx_watch_progress (
                profile_id uuid not null references cnx_profiles(id),
                item_id uuid not null,
                media_source_id uuid null,
                position_seconds double precision not null default 0,
                duration_seconds double precision not null default 0,
                percent_viewed double precision not null default 0,
                completed boolean not null default false,
                play_count int not null default 0,
                last_played_at timestamptz not null default now(),
                updated_at timestamptz not null default now(),
                primary key (profile_id, item_id)
            );

            create index if not exists ix_cnx_watch_progress_continue
                on cnx_watch_progress(profile_id, last_played_at desc)
                where completed = false and position_seconds >= 30;

            create table if not exists cnx_watch_events (
                id uuid primary key,
                profile_id uuid not null references cnx_profiles(id),
                jellyfin_user_id uuid not null,
                item_id uuid not null,
                item_type text not null,
                event_type text not null,
                position_seconds double precision null,
                duration_seconds double precision null,
                play_session_id text null,
                client_name text null,
                created_at timestamptz not null default now()
            );

            create index if not exists ix_cnx_watch_events_trending
                on cnx_watch_events(created_at desc, item_id, profile_id);

            create index if not exists ix_cnx_watch_events_item_recent
                on cnx_watch_events(item_id, created_at desc, profile_id);

            create table if not exists cnx_watch_history (
                profile_id uuid not null references cnx_profiles(id),
                item_id uuid not null,
                first_played_at timestamptz not null default now(),
                last_played_at timestamptz not null default now(),
                completed_at timestamptz null,
                play_count int not null default 0,
                primary key(profile_id, item_id)
            );

            create index if not exists ix_cnx_watch_history_profile_last
                on cnx_watch_history(profile_id, last_played_at desc);

            create table if not exists cnx_profile_hidden_items (
                profile_id uuid not null references cnx_profiles(id),
                item_id uuid not null,
                reason text not null default 'continue_watching',
                created_at timestamptz not null default now(),
                primary key (profile_id, item_id, reason)
            );

            create table if not exists cnx_media_segments (
                id uuid primary key,
                item_id uuid not null,
                segment_type text not null,
                start_seconds double precision not null,
                end_seconds double precision not null,
                source text not null,
                updated_at timestamptz not null default now(),
                unique(item_id, segment_type, source, start_seconds, end_seconds)
            );

            create index if not exists ix_cnx_media_segments_item
                on cnx_media_segments(item_id, segment_type);

            create table if not exists cnx_ranking_snapshots (
                ranking_id text primary key,
                generated_at timestamptz not null,
                expires_at timestamptz not null,
                items_json jsonb not null,
                updated_at timestamptz not null default now()
            );

            create index if not exists ix_cnx_ranking_snapshots_expires
                on cnx_ranking_snapshots(expires_at);

            create table if not exists cnx_home_row_snapshots (
                profile_id uuid not null references cnx_profiles(id),
                snapshot_key text not null,
                generated_at timestamptz not null,
                expires_at timestamptz not null,
                response_json jsonb not null,
                updated_at timestamptz not null default now(),
                primary key (profile_id, snapshot_key)
            );

            create index if not exists ix_cnx_home_row_snapshots_expires
                on cnx_home_row_snapshots(expires_at);
            """;

        const string myListSchemaSql = """
            create table if not exists cnx_profile_my_list (
                profile_id uuid not null references cnx_profiles(id),
                item_id uuid not null,
                added_at timestamptz not null default now(),
                primary key (profile_id, item_id)
            );

            create index if not exists ix_cnx_profile_my_list_added
                on cnx_profile_my_list(profile_id, added_at desc);
            """;

        const string activeProfilesByTokenSchemaSql = """
            alter table cnx_active_profiles
                add column if not exists token_hash text;

            update cnx_active_profiles
            set token_hash = 'legacy'
            where token_hash is null;

            alter table cnx_active_profiles
                alter column token_hash set not null;

            alter table cnx_active_profiles
                drop constraint if exists cnx_active_profiles_pkey;

            alter table cnx_active_profiles
                add primary key (jellyfin_user_id, token_hash);
            """;

        const string disableUnsafeChildProfilesSchemaSql = """
            update cnx_profiles
            set
                is_child = false,
                updated_at = now()
            where is_child = true;

            alter table cnx_profiles
                drop constraint if exists ck_cnx_profiles_child_profiles_disabled;

            alter table cnx_profiles
                add constraint ck_cnx_profiles_child_profiles_disabled
                check (is_child = false);
            """;

        const string playbackProfilePreferencesSchemaSql = """
            alter table cnx_playback_preferences
                add column if not exists preferred_audio_language text null,
                add column if not exists preferred_subtitle_language text null,
                add column if not exists subtitles_enabled boolean not null default false,
                add column if not exists audio_description_enabled boolean not null default false,
                add column if not exists closed_captions_enabled boolean not null default false,
                add column if not exists skip_credits_enabled boolean not null default false,
                add column if not exists autoplay_consecutive_count int not null default 0,
                add column if not exists autoplay_last_item_id uuid null,
                add column if not exists still_watching_required boolean not null default false,
                add column if not exists still_watching_confirmed_at timestamptz null;

            alter table cnx_playback_preferences
                drop constraint if exists ck_cnx_playback_preferences_audio_language,
                drop constraint if exists ck_cnx_playback_preferences_subtitle_language,
                drop constraint if exists ck_cnx_playback_preferences_autoplay_count,
                drop constraint if exists ck_cnx_playback_preferences_still_watching;

            alter table cnx_playback_preferences
                add constraint ck_cnx_playback_preferences_audio_language
                    check (
                        preferred_audio_language is null
                        or char_length(preferred_audio_language) between 1 and 35),
                add constraint ck_cnx_playback_preferences_subtitle_language
                    check (
                        preferred_subtitle_language is null
                        or char_length(preferred_subtitle_language) between 1 and 35),
                add constraint ck_cnx_playback_preferences_autoplay_count
                    check (autoplay_consecutive_count between 0 and 3),
                add constraint ck_cnx_playback_preferences_still_watching
                    check (not still_watching_required or autoplay_consecutive_count = 3);

            delete from cnx_home_row_snapshots;
            """;

        const string profileItemFeedbackSchemaSql = """
            create table if not exists cnx_profile_item_feedback (
                profile_id uuid not null references cnx_profiles(id) on delete cascade,
                item_id uuid not null,
                feedback text not null check (feedback in ('like', 'dislike', 'not-interested')),
                updated_at timestamptz not null default now(),
                primary key (profile_id, item_id)
            );

            create index if not exists ix_cnx_profile_item_feedback_recent
                on cnx_profile_item_feedback(profile_id, feedback, updated_at desc);
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await EnsureSchemaMigrationsTableAsync(connection, cancellationToken).ConfigureAwait(false);
        var appliedVersions = await GetAppliedSchemaMigrationVersionsAsync(connection, cancellationToken).ConfigureAwait(false);
        var pendingVersions = CustomNetflixSchemaMigrationPolicy.GetPendingVersions(
            appliedVersions,
            CustomNetflixSchemaMigrationPolicy.KnownVersions);

        foreach (var version in pendingVersions)
        {
            if (version == CustomNetflixSchemaMigrationPolicy.BaseSchemaVersion)
            {
                await ApplySchemaMigrationAsync(
                    connection,
                    version,
                    CustomNetflixSchemaMigrationPolicy.BaseSchemaMigrationName,
                    baseSchemaSql,
                    cancellationToken).ConfigureAwait(false);
            }
            else if (version == CustomNetflixSchemaMigrationPolicy.MyListSchemaVersion)
            {
                await ApplySchemaMigrationAsync(
                    connection,
                    version,
                    CustomNetflixSchemaMigrationPolicy.MyListSchemaMigrationName,
                    myListSchemaSql,
                    cancellationToken).ConfigureAwait(false);
            }
            else if (version == CustomNetflixSchemaMigrationPolicy.ActiveProfilesByTokenSchemaVersion)
            {
                await ApplySchemaMigrationAsync(
                    connection,
                    version,
                    CustomNetflixSchemaMigrationPolicy.ActiveProfilesByTokenSchemaMigrationName,
                    activeProfilesByTokenSchemaSql,
                    cancellationToken).ConfigureAwait(false);
            }
            else if (version == CustomNetflixSchemaMigrationPolicy.DisableUnsafeChildProfilesSchemaVersion)
            {
                await ApplySchemaMigrationAsync(
                    connection,
                    version,
                    CustomNetflixSchemaMigrationPolicy.DisableUnsafeChildProfilesSchemaMigrationName,
                    disableUnsafeChildProfilesSchemaSql,
                    cancellationToken).ConfigureAwait(false);
            }
            else if (version == CustomNetflixSchemaMigrationPolicy.PlaybackProfilePreferencesSchemaVersion)
            {
                await ApplySchemaMigrationAsync(
                    connection,
                    version,
                    CustomNetflixSchemaMigrationPolicy.PlaybackProfilePreferencesSchemaMigrationName,
                    playbackProfilePreferencesSchemaSql,
                    cancellationToken).ConfigureAwait(false);
            }
            else if (version == CustomNetflixSchemaMigrationPolicy.ProfileItemFeedbackSchemaVersion)
            {
                await ApplySchemaMigrationAsync(
                    connection,
                    version,
                    CustomNetflixSchemaMigrationPolicy.ProfileItemFeedbackSchemaMigrationName,
                    profileItemFeedbackSchemaSql,
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static async Task EnsureSchemaMigrationsTableAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            create table if not exists cnx_schema_migrations (
                version int primary key,
                name text not null,
                applied_at timestamptz not null default now()
            )
            """,
            connection);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IReadOnlySet<int>> GetAppliedSchemaMigrationVersionsAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        var versions = new HashSet<int>();
        await using var command = new NpgsqlCommand("select version from cnx_schema_migrations", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            versions.Add(reader.GetInt32(0));
        }

        return versions;
    }

    private static async Task ApplySchemaMigrationAsync(
        NpgsqlConnection connection,
        int version,
        string name,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await using (var migrationCommand = new NpgsqlCommand(sql, connection, transaction))
        {
            await migrationCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await using (var recordCommand = new NpgsqlCommand(
            """
            insert into cnx_schema_migrations (version, name)
            values (@version, @name)
            on conflict (version) do nothing
            """,
            connection,
            transaction))
        {
            recordCommand.Parameters.AddWithValue("version", version);
            recordCommand.Parameters.AddWithValue("name", name);
            await recordCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task CheckHealthAsync(CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            """
            select coalesce(max(version), 0)
            from cnx_schema_migrations
            """);
        var version = (int)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) ?? 0);
        if (!CustomNetflixSchemaMigrationPolicy.IsCurrent(version))
        {
            throw new CustomNetflixUnavailableException("CustomNetflix PostgreSQL schema migrations are not up to date.");
        }
    }

    public async Task<IReadOnlyList<ProfileRow>> GetProfilesAsync(Guid jellyfinUserId, CancellationToken cancellationToken)
    {
        using var timer = CustomNetflixMetrics.MeasurePostgreSqlOperation("get_profiles");
        var profiles = new List<ProfileRow>();
        await using var command = _dataSource.CreateCommand(SelectProfileSql + "\n" + """
            where p.jellyfin_user_id = @jellyfin_user_id
                and p.deleted_at is null
            order by p.is_default desc, p.created_at asc
            """);
        command.Parameters.AddWithValue("jellyfin_user_id", jellyfinUserId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            profiles.Add(ReadProfile(reader));
        }

        return profiles;
    }

    public async Task<ProfileRow?> GetProfileAsync(Guid profileId, CancellationToken cancellationToken)
    {
        using var timer = CustomNetflixMetrics.MeasurePostgreSqlOperation("get_profile");
        await using var command = _dataSource.CreateCommand(SelectProfileSql + "\n" + """
            where p.id = @profile_id
                and p.deleted_at is null
            """);
        command.Parameters.AddWithValue("profile_id", profileId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? ReadProfile(reader) : null;
    }

    public async Task<ProfileRow> CreateProfileAsync(
        Guid jellyfinUserId,
        string name,
        string? avatarId,
        bool isChild,
        bool isDefault,
        int maxProfiles,
        CancellationToken cancellationToken)
    {
        if (isChild)
        {
            throw new ArgumentException(
                "Child profiles are unavailable because native Jellyfin routes cannot enforce their restrictions.",
                nameof(isChild));
        }

        maxProfiles = Math.Clamp(maxProfiles, 1, 20);
        var profileId = Guid.NewGuid();
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await using (var lockCommand = new NpgsqlCommand(
            "select pg_advisory_xact_lock(hashtextextended(@jellyfin_user_id::text, 0))",
            connection,
            transaction))
        {
            lockCommand.Parameters.AddWithValue("jellyfin_user_id", jellyfinUserId);
            await lockCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        }

        long activeProfileCount;
        Guid? defaultProfileId;
        await using (var stateCommand = new NpgsqlCommand(
            """
            select
                count(*),
                (min(id::text) filter (where is_default))::uuid
            from cnx_profiles
            where jellyfin_user_id = @jellyfin_user_id
                and deleted_at is null
            """,
            connection,
            transaction))
        {
            stateCommand.Parameters.AddWithValue("jellyfin_user_id", jellyfinUserId);
            await using var reader = await stateCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            activeProfileCount = reader.GetInt64(0);
            defaultProfileId = await reader.IsDBNullAsync(1, cancellationToken).ConfigureAwait(false) ? null : reader.GetGuid(1);
        }

        if (isDefault && activeProfileCount > 0 && defaultProfileId.HasValue)
        {
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return await GetProfileAsync(defaultProfileId.Value, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The existing default profile could not be reloaded.");
        }

        if (activeProfileCount >= maxProfiles)
        {
            throw new CustomNetflixProfileLimitExceededException(maxProfiles);
        }

        var createAsDefault = activeProfileCount == 0;
        await using (var insertProfileCommand = new NpgsqlCommand(
            """
            insert into cnx_profiles (id, jellyfin_user_id, name, avatar_id, is_default, is_child)
            values (@id, @jellyfin_user_id, @name, @avatar_id, @is_default, @is_child)
            on conflict (jellyfin_user_id)
                where is_default = true and deleted_at is null
                do nothing
            returning id
            """,
            connection,
            transaction))
        {
            insertProfileCommand.Parameters.AddWithValue("id", profileId);
            insertProfileCommand.Parameters.AddWithValue("jellyfin_user_id", jellyfinUserId);
            insertProfileCommand.Parameters.AddWithValue("name", name);
            insertProfileCommand.Parameters.AddWithValue("avatar_id", (object?)avatarId ?? DBNull.Value);
            insertProfileCommand.Parameters.AddWithValue("is_default", createAsDefault);
            insertProfileCommand.Parameters.AddWithValue("is_child", false);
            var insertedProfileId = await insertProfileCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (insertedProfileId is not Guid)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return (await GetProfilesAsync(jellyfinUserId, cancellationToken).ConfigureAwait(false))
                    .First(profile => profile.IsDefault);
            }
        }

        await using (var insertSettingsCommand = new NpgsqlCommand(
            "insert into cnx_profile_settings (profile_id) values (@profile_id)",
            connection,
            transaction))
        {
            insertSettingsCommand.Parameters.AddWithValue("profile_id", profileId);
            await insertSettingsCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await using (var insertPreferencesCommand = new NpgsqlCommand(
            "insert into cnx_playback_preferences (profile_id) values (@profile_id)",
            connection,
            transaction))
        {
            insertPreferencesCommand.Parameters.AddWithValue("profile_id", profileId);
            await insertPreferencesCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return await GetProfileAsync(profileId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The profile was inserted but could not be reloaded.");
    }

    public async Task<ProfileRow?> UpdateProfileAsync(
        Guid profileId,
        string? name,
        string? avatarId,
        ProfileSettingsRow? settings,
        PlaybackPreferencesRow? playbackPreferences,
        CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await using (var updateProfileCommand = new NpgsqlCommand(
            """
            update cnx_profiles
            set
                name = coalesce(@name, name),
                avatar_id = coalesce(@avatar_id, avatar_id),
                updated_at = now()
            where id = @profile_id
                and deleted_at is null
            """,
            connection,
            transaction))
        {
            updateProfileCommand.Parameters.AddWithValue("profile_id", profileId);
            updateProfileCommand.Parameters.AddWithValue("name", (object?)name ?? DBNull.Value);
            updateProfileCommand.Parameters.AddWithValue("avatar_id", (object?)avatarId ?? DBNull.Value);
            await updateProfileCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        if (settings is not null)
        {
            await using var updateSettingsCommand = new NpgsqlCommand(
                """
                insert into cnx_profile_settings (
                    profile_id,
                    autoplay_enabled,
                    autoplay_delay_seconds,
                    skip_intro_enabled,
                    skip_recap_enabled)
                values (
                    @profile_id,
                    @autoplay_enabled,
                    @autoplay_delay_seconds,
                    @skip_intro_enabled,
                    @skip_recap_enabled)
                on conflict (profile_id) do update set
                    autoplay_enabled = excluded.autoplay_enabled,
                    autoplay_delay_seconds = excluded.autoplay_delay_seconds,
                    skip_intro_enabled = excluded.skip_intro_enabled,
                    skip_recap_enabled = excluded.skip_recap_enabled,
                    updated_at = now()
                """,
                connection,
                transaction);
            updateSettingsCommand.Parameters.AddWithValue("profile_id", profileId);
            updateSettingsCommand.Parameters.AddWithValue("autoplay_enabled", settings.AutoplayEnabled);
            updateSettingsCommand.Parameters.AddWithValue("autoplay_delay_seconds", settings.AutoplayDelaySeconds);
            updateSettingsCommand.Parameters.AddWithValue("skip_intro_enabled", settings.SkipIntroEnabled);
            updateSettingsCommand.Parameters.AddWithValue("skip_recap_enabled", settings.SkipRecapEnabled);
            await updateSettingsCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        if (playbackPreferences is not null)
        {
            await using var updatePreferencesCommand = new NpgsqlCommand(
                """
                insert into cnx_playback_preferences (
                    profile_id,
                    prefer_direct_play,
                    allow_container_remuxing,
                    allow_video_transcoding,
                    allow_audio_transcoding,
                    prefer_hardware_transcoding,
                    max_streaming_bitrate,
                    preferred_audio_language,
                    preferred_subtitle_language,
                    subtitles_enabled,
                    audio_description_enabled,
                    closed_captions_enabled,
                    skip_credits_enabled)
                values (
                    @profile_id,
                    @prefer_direct_play,
                    @allow_container_remuxing,
                    @allow_video_transcoding,
                    @allow_audio_transcoding,
                    @prefer_hardware_transcoding,
                    @max_streaming_bitrate,
                    @preferred_audio_language,
                    @preferred_subtitle_language,
                    @subtitles_enabled,
                    @audio_description_enabled,
                    @closed_captions_enabled,
                    @skip_credits_enabled)
                on conflict (profile_id) do update set
                    prefer_direct_play = excluded.prefer_direct_play,
                    allow_container_remuxing = excluded.allow_container_remuxing,
                    allow_video_transcoding = excluded.allow_video_transcoding,
                    allow_audio_transcoding = excluded.allow_audio_transcoding,
                    prefer_hardware_transcoding = excluded.prefer_hardware_transcoding,
                    max_streaming_bitrate = excluded.max_streaming_bitrate,
                    preferred_audio_language = excluded.preferred_audio_language,
                    preferred_subtitle_language = excluded.preferred_subtitle_language,
                    subtitles_enabled = excluded.subtitles_enabled,
                    audio_description_enabled = excluded.audio_description_enabled,
                    closed_captions_enabled = excluded.closed_captions_enabled,
                    skip_credits_enabled = excluded.skip_credits_enabled,
                    updated_at = now()
                """,
                connection,
                transaction);
            updatePreferencesCommand.Parameters.AddWithValue("profile_id", profileId);
            updatePreferencesCommand.Parameters.AddWithValue("prefer_direct_play", playbackPreferences.PreferDirectPlay);
            updatePreferencesCommand.Parameters.AddWithValue("allow_container_remuxing", playbackPreferences.AllowContainerRemuxing);
            updatePreferencesCommand.Parameters.AddWithValue("allow_video_transcoding", playbackPreferences.AllowVideoTranscoding);
            updatePreferencesCommand.Parameters.AddWithValue("allow_audio_transcoding", playbackPreferences.AllowAudioTranscoding);
            updatePreferencesCommand.Parameters.AddWithValue("prefer_hardware_transcoding", playbackPreferences.PreferHardwareTranscoding);
            updatePreferencesCommand.Parameters.AddWithValue(
                "max_streaming_bitrate",
                NpgsqlDbType.Integer,
                (object?)playbackPreferences.MaxStreamingBitrate ?? DBNull.Value);
            updatePreferencesCommand.Parameters.AddWithValue(
                "preferred_audio_language",
                NpgsqlDbType.Text,
                (object?)playbackPreferences.PreferredAudioLanguage ?? DBNull.Value);
            updatePreferencesCommand.Parameters.AddWithValue(
                "preferred_subtitle_language",
                NpgsqlDbType.Text,
                (object?)playbackPreferences.PreferredSubtitleLanguage ?? DBNull.Value);
            updatePreferencesCommand.Parameters.AddWithValue("subtitles_enabled", playbackPreferences.SubtitlesEnabled);
            updatePreferencesCommand.Parameters.AddWithValue("audio_description_enabled", playbackPreferences.AudioDescriptionEnabled);
            updatePreferencesCommand.Parameters.AddWithValue("closed_captions_enabled", playbackPreferences.ClosedCaptionsEnabled);
            updatePreferencesCommand.Parameters.AddWithValue("skip_credits_enabled", playbackPreferences.SkipCreditsEnabled);
            await updatePreferencesCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return await GetProfileAsync(profileId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> SoftDeleteProfileAsync(Guid profileId, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        Guid jellyfinUserId;
        await using (var ownerCommand = new NpgsqlCommand(
            """
            select jellyfin_user_id
            from cnx_profiles
            where id = @profile_id
                and deleted_at is null
            """,
            connection,
            transaction))
        {
            ownerCommand.Parameters.AddWithValue("profile_id", profileId);
            var result = await ownerCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (result is not Guid userId)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return false;
            }

            jellyfinUserId = userId;
        }

        await using (var lockCommand = new NpgsqlCommand(
            "select pg_advisory_xact_lock(hashtextextended(@jellyfin_user_id::text, 0))",
            connection,
            transaction))
        {
            lockCommand.Parameters.AddWithValue("jellyfin_user_id", jellyfinUserId);
            await lockCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        }

        long activeProfileCount;
        long targetProfileCount;
        await using (var stateCommand = new NpgsqlCommand(
            """
            select
                count(*),
                count(*) filter (where id = @profile_id)
            from cnx_profiles
            where jellyfin_user_id = @jellyfin_user_id
                and deleted_at is null
            """,
            connection,
            transaction))
        {
            stateCommand.Parameters.AddWithValue("jellyfin_user_id", jellyfinUserId);
            stateCommand.Parameters.AddWithValue("profile_id", profileId);
            await using var reader = await stateCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            activeProfileCount = reader.GetInt64(0);
            targetProfileCount = reader.GetInt64(1);
        }

        if (targetProfileCount == 0)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return false;
        }

        if (activeProfileCount <= 1)
        {
            throw new InvalidOperationException("Cannot delete the last profile for a Jellyfin user.");
        }

        await using (var deleteCommand = new NpgsqlCommand(
            """
            update cnx_profiles
            set
                is_default = false,
                deleted_at = now(),
                updated_at = now()
            where id = @profile_id
            """,
            connection,
            transaction))
        {
            deleteCommand.Parameters.AddWithValue("profile_id", profileId);
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        var replacementProfileId = await EnsureDefaultProfileAfterDeleteAsync(connection, transaction, jellyfinUserId, cancellationToken).ConfigureAwait(false);
        await RepairActiveProfileAfterDeleteAsync(connection, transaction, jellyfinUserId, profileId, replacementProfileId, cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<CustomNetflixUserDataKeys> PurgeUserDataAsync(
        Guid jellyfinUserId,
        CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await using (var lockCommand = new NpgsqlCommand(
            "select pg_advisory_xact_lock(hashtextextended(@jellyfin_user_id::text, 0))",
            connection,
            transaction))
        {
            lockCommand.Parameters.AddWithValue("jellyfin_user_id", jellyfinUserId);
            await lockCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        }

        var profileIds = new List<Guid>();
        await using (var profileCommand = new NpgsqlCommand(
            "select id from cnx_profiles where jellyfin_user_id = @jellyfin_user_id",
            connection,
            transaction))
        {
            profileCommand.Parameters.AddWithValue("jellyfin_user_id", jellyfinUserId);
            await using var reader = await profileCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                profileIds.Add(reader.GetGuid(0));
            }
        }

        var tokenHashes = new List<string>();
        await using (var activeProfileCommand = new NpgsqlCommand(
            "select token_hash from cnx_active_profiles where jellyfin_user_id = @jellyfin_user_id",
            connection,
            transaction))
        {
            activeProfileCommand.Parameters.AddWithValue("jellyfin_user_id", jellyfinUserId);
            await using var reader = await activeProfileCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                tokenHashes.Add(reader.GetString(0));
            }
        }

        await using (var purgeCommand = new NpgsqlCommand(
            """
            delete from cnx_active_profiles
            where jellyfin_user_id = @jellyfin_user_id;

            delete from cnx_home_row_snapshots
            where profile_id in (
                select id from cnx_profiles where jellyfin_user_id = @jellyfin_user_id);

            delete from cnx_profile_my_list
            where profile_id in (
                select id from cnx_profiles where jellyfin_user_id = @jellyfin_user_id);

            delete from cnx_profile_hidden_items
            where profile_id in (
                select id from cnx_profiles where jellyfin_user_id = @jellyfin_user_id);

            delete from cnx_watch_history
            where profile_id in (
                select id from cnx_profiles where jellyfin_user_id = @jellyfin_user_id);

            delete from cnx_watch_events
            where jellyfin_user_id = @jellyfin_user_id
                or profile_id in (
                    select id from cnx_profiles where jellyfin_user_id = @jellyfin_user_id);

            delete from cnx_watch_progress
            where profile_id in (
                select id from cnx_profiles where jellyfin_user_id = @jellyfin_user_id);

            delete from cnx_playback_preferences
            where profile_id in (
                select id from cnx_profiles where jellyfin_user_id = @jellyfin_user_id);

            delete from cnx_profile_settings
            where profile_id in (
                select id from cnx_profiles where jellyfin_user_id = @jellyfin_user_id);

            delete from cnx_profiles
            where jellyfin_user_id = @jellyfin_user_id;

            delete from cnx_ranking_snapshots;
            """,
            connection,
            transaction))
        {
            purgeCommand.Parameters.AddWithValue("jellyfin_user_id", jellyfinUserId);
            await purgeCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return new CustomNetflixUserDataKeys(profileIds, tokenHashes);
    }

    public async Task<Guid?> GetActiveProfileAsync(Guid jellyfinUserId, string tokenHash, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            """
            select profile_id
            from cnx_active_profiles
            where jellyfin_user_id = @jellyfin_user_id
                and token_hash in (@token_hash, 'legacy')
            order by (token_hash = @token_hash) desc
            limit 1
            """);
        command.Parameters.AddWithValue("jellyfin_user_id", jellyfinUserId);
        command.Parameters.AddWithValue("token_hash", tokenHash);

        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return result is Guid profileId ? profileId : null;
    }

    public async Task SetActiveProfileAsync(Guid jellyfinUserId, string tokenHash, Guid profileId, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            """
            insert into cnx_active_profiles (jellyfin_user_id, token_hash, profile_id)
            values (@jellyfin_user_id, @token_hash, @profile_id)
            on conflict (jellyfin_user_id, token_hash) do update set
                profile_id = excluded.profile_id,
                updated_at = now()
            """);
        command.Parameters.AddWithValue("jellyfin_user_id", jellyfinUserId);
        command.Parameters.AddWithValue("token_hash", tokenHash);
        command.Parameters.AddWithValue("profile_id", profileId);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<WatchProgressRow?> GetProgressAsync(Guid profileId, Guid itemId, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            """
            select
                profile_id,
                item_id,
                media_source_id,
                position_seconds,
                duration_seconds,
                percent_viewed,
                completed,
                play_count,
                last_played_at
            from cnx_watch_progress
            where profile_id = @profile_id
                and item_id = @item_id
            """);
        command.Parameters.AddWithValue("profile_id", profileId);
        command.Parameters.AddWithValue("item_id", itemId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? ReadProgress(reader) : null;
    }

    public async Task<IReadOnlyList<WatchProgressRow>> GetProgressForItemsAsync(Guid profileId, IReadOnlyList<Guid> itemIds, CancellationToken cancellationToken)
    {
        using var timer = CustomNetflixMetrics.MeasurePostgreSqlOperation("get_progress_batch");
        if (itemIds.Count == 0)
        {
            return Array.Empty<WatchProgressRow>();
        }

        var rows = new List<WatchProgressRow>();
        await using var command = _dataSource.CreateCommand(
            """
            select
                profile_id,
                item_id,
                media_source_id,
                position_seconds,
                duration_seconds,
                percent_viewed,
                completed,
                play_count,
                last_played_at
            from cnx_watch_progress
            where profile_id = @profile_id
                and item_id = any(@item_ids)
            """);
        command.Parameters.AddWithValue("profile_id", profileId);
        command.Parameters.AddWithValue("item_ids", itemIds.ToArray());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            rows.Add(ReadProgress(reader));
        }

        return rows;
    }

    public async Task UpsertProgressAsync(WatchProgressRow progress, CancellationToken cancellationToken)
    {
        using var timer = CustomNetflixMetrics.MeasurePostgreSqlOperation("upsert_progress");
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        var savedProgress = await UpsertProgressCoreAsync(connection, transaction, progress, cancellationToken).ConfigureAwait(false);
        if (savedProgress is not null)
        {
            await UpsertWatchHistoryAsync(connection, transaction, savedProgress, cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpsertProgressRowsAsync(IReadOnlyList<WatchProgressRow> progressRows, CancellationToken cancellationToken)
    {
        using var timer = CustomNetflixMetrics.MeasurePostgreSqlOperation("upsert_progress_batch");
        if (progressRows.Count == 0)
        {
            return;
        }

        await using var command = _dataSource.CreateCommand(
            """
            with input as (
                select
                    profile_id,
                    item_id,
                    case when has_media_source_id then media_source_id else null end as media_source_id,
                    position_seconds,
                    duration_seconds,
                    percent_viewed,
                    completed,
                    last_played_at
                from unnest(
                    @profile_ids::uuid[],
                    @item_ids::uuid[],
                    @media_source_ids::uuid[],
                    @has_media_source_ids::boolean[],
                    @position_seconds::double precision[],
                    @duration_seconds::double precision[],
                    @percent_viewed::double precision[],
                    @completed::boolean[],
                    @last_played_at::timestamp with time zone[])
                    as rows(
                        profile_id,
                        item_id,
                        media_source_id,
                        has_media_source_id,
                        position_seconds,
                        duration_seconds,
                        percent_viewed,
                        completed,
                        last_played_at)
            ),
            saved_progress as (
                insert into cnx_watch_progress (
                    profile_id,
                    item_id,
                    media_source_id,
                    position_seconds,
                    duration_seconds,
                    percent_viewed,
                    completed,
                    play_count,
                    last_played_at)
                select
                    profile_id,
                    item_id,
                    media_source_id,
                    position_seconds,
                    duration_seconds,
                    percent_viewed,
                    completed,
                    case when completed then 1 else 0 end,
                    last_played_at
                from input
                where exists (
                    select 1
                    from cnx_profiles p
                    where p.id = input.profile_id
                        and p.deleted_at is null)
                on conflict (profile_id, item_id) do update set
                    media_source_id = excluded.media_source_id,
                    position_seconds = excluded.position_seconds,
                    duration_seconds = excluded.duration_seconds,
                    percent_viewed = excluded.percent_viewed,
                    completed = excluded.completed,
                    play_count = cnx_watch_progress.play_count
                        + case when excluded.completed and not cnx_watch_progress.completed then 1 else 0 end,
                    last_played_at = excluded.last_played_at,
                    updated_at = now()
                where cnx_watch_progress.last_played_at <= excluded.last_played_at
                returning profile_id, item_id, last_played_at, completed, play_count
            )
            insert into cnx_watch_history (
                profile_id,
                item_id,
                first_played_at,
                last_played_at,
                completed_at,
                play_count)
            select
                profile_id,
                item_id,
                last_played_at,
                last_played_at,
                case when completed then last_played_at else null end,
                play_count
            from saved_progress
            on conflict (profile_id, item_id) do update set
                last_played_at = excluded.last_played_at,
                completed_at = case
                    when excluded.completed_at is not null then excluded.completed_at
                    else cnx_watch_history.completed_at
                end,
                play_count = greatest(cnx_watch_history.play_count, excluded.play_count)
            """);
        command.Parameters.AddWithValue("profile_ids", NpgsqlDbType.Array | NpgsqlDbType.Uuid, progressRows.Select(row => row.ProfileId).ToArray());
        command.Parameters.AddWithValue("item_ids", NpgsqlDbType.Array | NpgsqlDbType.Uuid, progressRows.Select(row => row.ItemId).ToArray());
        command.Parameters.AddWithValue("media_source_ids", NpgsqlDbType.Array | NpgsqlDbType.Uuid, progressRows.Select(row => row.MediaSourceId.GetValueOrDefault()).ToArray());
        command.Parameters.AddWithValue("has_media_source_ids", NpgsqlDbType.Array | NpgsqlDbType.Boolean, progressRows.Select(row => row.MediaSourceId.HasValue).ToArray());
        command.Parameters.AddWithValue("position_seconds", NpgsqlDbType.Array | NpgsqlDbType.Double, progressRows.Select(row => row.PositionSeconds).ToArray());
        command.Parameters.AddWithValue("duration_seconds", NpgsqlDbType.Array | NpgsqlDbType.Double, progressRows.Select(row => row.DurationSeconds).ToArray());
        command.Parameters.AddWithValue("percent_viewed", NpgsqlDbType.Array | NpgsqlDbType.Double, progressRows.Select(row => row.PercentViewed).ToArray());
        command.Parameters.AddWithValue("completed", NpgsqlDbType.Array | NpgsqlDbType.Boolean, progressRows.Select(row => row.Completed).ToArray());
        command.Parameters.AddWithValue("last_played_at", NpgsqlDbType.Array | NpgsqlDbType.TimestampTz, progressRows.Select(row => row.LastPlayedAt).ToArray());
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task InsertWatchEventsAsync(IReadOnlyList<WatchEventRow> watchEvents, CancellationToken cancellationToken)
    {
        using var timer = CustomNetflixMetrics.MeasurePostgreSqlOperation("insert_watch_events");
        if (watchEvents.Count == 0)
        {
            return;
        }

        await using var command = _dataSource.CreateCommand(
            """
            insert into cnx_watch_events (
                id,
                profile_id,
                jellyfin_user_id,
                item_id,
                item_type,
                event_type,
                position_seconds,
                duration_seconds,
                play_session_id,
                client_name)
            select input.*
            from unnest(
                @ids::uuid[],
                @profile_ids::uuid[],
                @jellyfin_user_ids::uuid[],
                @item_ids::uuid[],
                @item_types::text[],
                @event_types::text[],
                @position_seconds::double precision[],
                @duration_seconds::double precision[],
                @play_session_ids::text[],
                @client_names::text[])
                as input(
                    id,
                    profile_id,
                    jellyfin_user_id,
                    item_id,
                    item_type,
                    event_type,
                    position_seconds,
                    duration_seconds,
                    play_session_id,
                    client_name)
            where exists (
                select 1
                from cnx_profiles p
                where p.id = input.profile_id
                    and p.jellyfin_user_id = input.jellyfin_user_id
                    and p.deleted_at is null)
            on conflict (id) do nothing
            """);
        command.Parameters.AddWithValue("ids", NpgsqlDbType.Array | NpgsqlDbType.Uuid, watchEvents.Select(row => row.Id).ToArray());
        command.Parameters.AddWithValue("profile_ids", NpgsqlDbType.Array | NpgsqlDbType.Uuid, watchEvents.Select(row => row.ProfileId).ToArray());
        command.Parameters.AddWithValue("jellyfin_user_ids", NpgsqlDbType.Array | NpgsqlDbType.Uuid, watchEvents.Select(row => row.JellyfinUserId).ToArray());
        command.Parameters.AddWithValue("item_ids", NpgsqlDbType.Array | NpgsqlDbType.Uuid, watchEvents.Select(row => row.ItemId).ToArray());
        command.Parameters.AddWithValue("item_types", NpgsqlDbType.Array | NpgsqlDbType.Text, watchEvents.Select(row => row.ItemType).ToArray());
        command.Parameters.AddWithValue("event_types", NpgsqlDbType.Array | NpgsqlDbType.Text, watchEvents.Select(row => row.EventType).ToArray());
        command.Parameters.AddWithValue("position_seconds", NpgsqlDbType.Array | NpgsqlDbType.Double, watchEvents.Select(row => row.PositionSeconds).ToArray());
        command.Parameters.AddWithValue("duration_seconds", NpgsqlDbType.Array | NpgsqlDbType.Double, watchEvents.Select(row => row.DurationSeconds).ToArray());
        command.Parameters.AddWithValue("play_session_ids", NpgsqlDbType.Array | NpgsqlDbType.Text, watchEvents.Select(row => row.PlaySessionId).ToArray());
        command.Parameters.AddWithValue("client_names", NpgsqlDbType.Array | NpgsqlDbType.Text, watchEvents.Select(row => row.ClientName).ToArray());
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> PurgeWatchEventsAsync(DateTime cutoff, int batchSize, CancellationToken cancellationToken)
    {
        using var timer = CustomNetflixMetrics.MeasurePostgreSqlOperation("purge_watch_events");
        await using var command = _dataSource.CreateCommand(
            """
            delete from cnx_watch_events
            where id in (
                select id
                from cnx_watch_events
                where created_at < @cutoff
                order by created_at
                limit @batch_size)
            """);
        command.Parameters.AddWithValue("cutoff", NpgsqlDbType.TimestampTz, cutoff);
        command.Parameters.AddWithValue("batch_size", Math.Clamp(batchSize, 1, 10000));
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<WatchProgressRow>> GetContinueWatchingAsync(Guid profileId, int limit, CancellationToken cancellationToken)
    {
        using var timer = CustomNetflixMetrics.MeasurePostgreSqlOperation("get_continue_watching");
        var rows = new List<WatchProgressRow>();
        await using var command = _dataSource.CreateCommand(
            """
            select
                p.profile_id,
                p.item_id,
                p.media_source_id,
                p.position_seconds,
                p.duration_seconds,
                p.percent_viewed,
                p.completed,
                p.play_count,
                p.last_played_at
            from cnx_watch_progress p
            where p.profile_id = @profile_id
                and p.completed = false
                and p.position_seconds >= 30
                and not exists (
                    select 1
                    from cnx_profile_hidden_items h
                    where h.profile_id = p.profile_id
                        and h.item_id = p.item_id
                        and h.reason = 'continue_watching')
            order by p.last_played_at desc
            limit @limit
            """);
        command.Parameters.AddWithValue("profile_id", profileId);
        command.Parameters.AddWithValue("limit", Math.Clamp(limit, 1, 100));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            rows.Add(ReadProgress(reader));
        }

        return rows;
    }

    public async Task<IReadOnlyList<WatchHistoryRow>> GetWatchHistoryAsync(Guid profileId, int limit, CancellationToken cancellationToken)
    {
        using var timer = CustomNetflixMetrics.MeasurePostgreSqlOperation("get_watch_history");
        var rows = new List<WatchHistoryRow>();
        await using var command = _dataSource.CreateCommand(
            """
            select
                profile_id,
                item_id,
                first_played_at,
                last_played_at,
                completed_at,
                play_count
            from cnx_watch_history
            where profile_id = @profile_id
            order by last_played_at desc
            limit @limit
            """);
        command.Parameters.AddWithValue("profile_id", profileId);
        command.Parameters.AddWithValue("limit", CustomNetflixHistoryPolicy.NormalizeLimit(limit));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            rows.Add(ReadWatchHistory(reader));
        }

        return rows;
    }

    public async Task<IReadOnlyList<MyListRow>> GetMyListAsync(Guid profileId, int limit, CancellationToken cancellationToken)
    {
        using var timer = CustomNetflixMetrics.MeasurePostgreSqlOperation("get_my_list");
        var rows = new List<MyListRow>();
        await using var command = _dataSource.CreateCommand(
            """
            select profile_id, item_id, added_at
            from cnx_profile_my_list
            where profile_id = @profile_id
            order by added_at desc
            limit @limit
            """);
        command.Parameters.AddWithValue("profile_id", profileId);
        command.Parameters.AddWithValue("limit", CustomNetflixMyListPolicy.NormalizeLimit(limit));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            rows.Add(new MyListRow(reader.GetGuid(0), reader.GetGuid(1), reader.GetDateTime(2)));
        }

        return rows;
    }

    public async Task<MyListRow?> GetMyListItemAsync(Guid profileId, Guid itemId, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            """
            select profile_id, item_id, added_at
            from cnx_profile_my_list
            where profile_id = @profile_id and item_id = @item_id
            """);
        command.Parameters.AddWithValue("profile_id", profileId);
        command.Parameters.AddWithValue("item_id", itemId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
            ? new MyListRow(reader.GetGuid(0), reader.GetGuid(1), reader.GetDateTime(2))
            : null;
    }

    public async Task<MyListRow> AddToMyListAsync(Guid profileId, Guid itemId, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            """
            insert into cnx_profile_my_list (profile_id, item_id)
            values (@profile_id, @item_id)
            on conflict (profile_id, item_id) do update set
                added_at = cnx_profile_my_list.added_at
            returning profile_id, item_id, added_at
            """);
        command.Parameters.AddWithValue("profile_id", profileId);
        command.Parameters.AddWithValue("item_id", itemId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("The My List update did not return a row.");
        }

        return new MyListRow(reader.GetGuid(0), reader.GetGuid(1), reader.GetDateTime(2));
    }

    public async Task<bool> RemoveFromMyListAsync(Guid profileId, Guid itemId, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            "delete from cnx_profile_my_list where profile_id = @profile_id and item_id = @item_id");
        command.Parameters.AddWithValue("profile_id", profileId);
        command.Parameters.AddWithValue("item_id", itemId);
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) > 0;
    }

    public async Task<ItemFeedbackRow?> GetItemFeedbackAsync(Guid profileId, Guid itemId, CancellationToken cancellationToken)
    {
        using var timer = CustomNetflixMetrics.MeasurePostgreSqlOperation("get_item_feedback");
        await using var command = _dataSource.CreateCommand(
            """
            select profile_id, item_id, feedback, updated_at
            from cnx_profile_item_feedback
            where profile_id = @profile_id and item_id = @item_id
            """);
        command.Parameters.AddWithValue("profile_id", profileId);
        command.Parameters.AddWithValue("item_id", itemId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
            ? ReadItemFeedback(reader)
            : null;
    }

    public async Task<IReadOnlyList<ItemFeedbackRow>> GetLikedItemFeedbacksAsync(
        Guid profileId,
        int limit,
        CancellationToken cancellationToken)
    {
        using var timer = CustomNetflixMetrics.MeasurePostgreSqlOperation("get_liked_item_feedback");
        var rows = new List<ItemFeedbackRow>();
        await using var command = _dataSource.CreateCommand(
            """
            select profile_id, item_id, feedback, updated_at
            from cnx_profile_item_feedback
            where profile_id = @profile_id and feedback = 'like'
            order by updated_at desc
            limit @limit
            """);
        command.Parameters.AddWithValue("profile_id", profileId);
        command.Parameters.AddWithValue(
            "limit",
            Math.Clamp(limit, 1, CustomNetflixFeedbackPolicy.RecommendationFeedbackLimit));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            rows.Add(ReadItemFeedback(reader));
        }

        return rows;
    }

    public async Task<IReadOnlyList<ItemFeedbackRow>> GetItemFeedbacksForItemsAsync(
        Guid profileId,
        IReadOnlyList<Guid> itemIds,
        CancellationToken cancellationToken)
    {
        if (itemIds.Count == 0)
        {
            return Array.Empty<ItemFeedbackRow>();
        }

        using var timer = CustomNetflixMetrics.MeasurePostgreSqlOperation("get_item_feedback_batch");
        var rows = new List<ItemFeedbackRow>();
        await using var command = _dataSource.CreateCommand(
            """
            select profile_id, item_id, feedback, updated_at
            from cnx_profile_item_feedback
            where profile_id = @profile_id and item_id = any(@item_ids)
            """);
        command.Parameters.AddWithValue("profile_id", profileId);
        command.Parameters.AddWithValue(
            "item_ids",
            NpgsqlDbType.Array | NpgsqlDbType.Uuid,
            itemIds.Distinct().ToArray());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            rows.Add(ReadItemFeedback(reader));
        }

        return rows;
    }

    public async Task<ItemFeedbackRow> UpsertItemFeedbackAsync(
        Guid profileId,
        Guid itemId,
        string feedback,
        CancellationToken cancellationToken)
    {
        using var timer = CustomNetflixMetrics.MeasurePostgreSqlOperation("upsert_item_feedback");
        await using var command = _dataSource.CreateCommand(
            """
            insert into cnx_profile_item_feedback (profile_id, item_id, feedback)
            values (@profile_id, @item_id, @feedback)
            on conflict (profile_id, item_id) do update set
                feedback = excluded.feedback,
                updated_at = now()
            returning profile_id, item_id, feedback, updated_at
            """);
        command.Parameters.AddWithValue("profile_id", profileId);
        command.Parameters.AddWithValue("item_id", itemId);
        command.Parameters.AddWithValue("feedback", CustomNetflixFeedbackPolicy.Normalize(feedback));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("The item feedback update did not return a row.");
        }

        return ReadItemFeedback(reader);
    }

    public async Task<bool> DeleteItemFeedbackAsync(Guid profileId, Guid itemId, CancellationToken cancellationToken)
    {
        using var timer = CustomNetflixMetrics.MeasurePostgreSqlOperation("delete_item_feedback");
        await using var command = _dataSource.CreateCommand(
            "delete from cnx_profile_item_feedback where profile_id = @profile_id and item_id = @item_id");
        command.Parameters.AddWithValue("profile_id", profileId);
        command.Parameters.AddWithValue("item_id", itemId);
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) > 0;
    }

    public async Task<WatchProgressRow> SetPlayedAsync(Guid profileId, Guid jellyfinUserId, Guid itemId, bool played, string itemType, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        WatchProgressRow progress;
        await using (var command = new NpgsqlCommand(
            """
            insert into cnx_watch_progress (
                profile_id,
                item_id,
                position_seconds,
                duration_seconds,
                percent_viewed,
                completed,
                play_count,
                last_played_at)
            values (
                @profile_id,
                @item_id,
                0,
                0,
                case when @played then 100 else 0 end,
                @played,
                case when @played then 1 else 0 end,
                now())
            on conflict (profile_id, item_id) do update set
                position_seconds = case when @played then greatest(cnx_watch_progress.position_seconds, cnx_watch_progress.duration_seconds) else 0 end,
                percent_viewed = case when @played then 100 else 0 end,
                completed = @played,
                play_count = case when @played and not cnx_watch_progress.completed then cnx_watch_progress.play_count + 1 else cnx_watch_progress.play_count end,
                last_played_at = now(),
                updated_at = now()
            returning
                profile_id,
                item_id,
                media_source_id,
                position_seconds,
                duration_seconds,
                percent_viewed,
                completed,
                play_count,
                last_played_at
            """,
            connection,
            transaction))
        {
            command.Parameters.AddWithValue("profile_id", profileId);
            command.Parameters.AddWithValue("item_id", itemId);
            command.Parameters.AddWithValue("played", played);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException("The watch progress update did not return a row.");
            }

            progress = ReadProgress(reader);
        }

        var watchEvent = new WatchEventRow(
            Guid.NewGuid(),
            profileId,
            jellyfinUserId,
            itemId,
            itemType,
            played ? "mark_played" : "mark_unplayed",
            progress.PositionSeconds,
            progress.DurationSeconds,
            null,
            null);
        await InsertWatchEventAsync(connection, transaction, watchEvent, cancellationToken).ConfigureAwait(false);
        await UpsertWatchHistoryAsync(connection, transaction, progress, cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return progress;
    }

    public async Task<bool> HideFromContinueWatchingAsync(Guid profileId, Guid itemId, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            """
            insert into cnx_profile_hidden_items (profile_id, item_id, reason)
            values (@profile_id, @item_id, 'continue_watching')
            on conflict (profile_id, item_id, reason) do nothing
            """);
        command.Parameters.AddWithValue("profile_id", profileId);
        command.Parameters.AddWithValue("item_id", itemId);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<AutoplayStateRow> TrackAutoplayAsync(
        Guid profileId,
        Guid currentItemId,
        bool completed,
        CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            """
            insert into cnx_playback_preferences (
                profile_id,
                autoplay_consecutive_count,
                autoplay_last_item_id,
                still_watching_required)
            values (
                @profile_id,
                case when @completed then 1 else 0 end,
                case when @completed then @current_item_id else null end,
                false)
            on conflict (profile_id) do update set
                autoplay_consecutive_count = case
                    when cnx_playback_preferences.still_watching_required
                        then cnx_playback_preferences.autoplay_consecutive_count
                    when @completed
                        and cnx_playback_preferences.autoplay_last_item_id is distinct from @current_item_id
                        then least(cnx_playback_preferences.autoplay_consecutive_count + 1, 3)
                    else cnx_playback_preferences.autoplay_consecutive_count
                end,
                autoplay_last_item_id = case
                    when @completed then @current_item_id
                    else cnx_playback_preferences.autoplay_last_item_id
                end,
                still_watching_required = cnx_playback_preferences.still_watching_required
                    or (
                        @completed
                        and cnx_playback_preferences.autoplay_last_item_id is distinct from @current_item_id
                        and cnx_playback_preferences.autoplay_consecutive_count + 1 >= 3
                    ),
                updated_at = now()
            returning
                profile_id,
                autoplay_consecutive_count,
                autoplay_last_item_id,
                still_watching_required,
                still_watching_confirmed_at
            """);
        command.Parameters.AddWithValue("profile_id", profileId);
        command.Parameters.AddWithValue("current_item_id", currentItemId);
        command.Parameters.AddWithValue("completed", completed);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("The autoplay state update did not return a row.");
        }

        Guid? lastItemId = await reader.IsDBNullAsync(2, cancellationToken).ConfigureAwait(false)
            ? null
            : reader.GetGuid(2);
        DateTime? confirmedAt = await reader.IsDBNullAsync(4, cancellationToken).ConfigureAwait(false)
            ? null
            : reader.GetDateTime(4);
        return new AutoplayStateRow(
            reader.GetGuid(0),
            reader.GetInt32(1),
            lastItemId,
            reader.GetBoolean(3),
            confirmedAt);
    }

    public async Task<DateTime> ConfirmStillWatchingAsync(Guid profileId, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            """
            insert into cnx_playback_preferences (
                profile_id,
                autoplay_consecutive_count,
                still_watching_required,
                still_watching_confirmed_at)
            values (@profile_id, 0, false, now())
            on conflict (profile_id) do update set
                autoplay_consecutive_count = 0,
                still_watching_required = false,
                still_watching_confirmed_at = now(),
                updated_at = now()
            returning still_watching_confirmed_at
            """);
        command.Parameters.AddWithValue("profile_id", profileId);
        return (DateTime)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The still-watching confirmation did not return a timestamp."));
    }

    public async Task<IReadOnlyList<CustomMediaSegmentRow>> GetManualMediaSegmentsAsync(Guid itemId, IReadOnlyList<string>? segmentTypes, CancellationToken cancellationToken)
    {
        var rows = new List<CustomMediaSegmentRow>();
        await using var command = _dataSource.CreateCommand(
            """
            select
                id,
                item_id,
                segment_type,
                start_seconds,
                end_seconds,
                source,
                updated_at
            from cnx_media_segments
            where item_id = @item_id
                and source = @source
                and (@filter_types = false or segment_type = any(@segment_types))
            order by start_seconds asc
            """);
        command.Parameters.AddWithValue("item_id", itemId);
        command.Parameters.AddWithValue("source", CustomNetflixManualSegmentPolicy.ManualSource);
        command.Parameters.AddWithValue("filter_types", segmentTypes is { Count: > 0 });
        command.Parameters.AddWithValue("segment_types", segmentTypes?.ToArray() ?? Array.Empty<string>());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            rows.Add(new CustomMediaSegmentRow(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetString(2),
                reader.GetDouble(3),
                reader.GetDouble(4),
                reader.GetString(5),
                reader.GetDateTime(6)));
        }

        return rows;
    }

    public async Task ReplaceManualMediaSegmentsAsync(Guid itemId, IReadOnlyList<CustomMediaSegmentRow> segments, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await using (var deleteCommand = new NpgsqlCommand(
            "delete from cnx_media_segments where item_id = @item_id and source = @source",
            connection,
            transaction))
        {
            deleteCommand.Parameters.AddWithValue("item_id", itemId);
            deleteCommand.Parameters.AddWithValue("source", CustomNetflixManualSegmentPolicy.ManualSource);
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        foreach (var segment in segments)
        {
            await using var insertCommand = new NpgsqlCommand(
                """
                insert into cnx_media_segments (
                    id,
                    item_id,
                    segment_type,
                    start_seconds,
                    end_seconds,
                    source,
                    updated_at)
                values (
                    @id,
                    @item_id,
                    @segment_type,
                    @start_seconds,
                    @end_seconds,
                    @source,
                    @updated_at)
                """,
                connection,
                transaction);
            insertCommand.Parameters.AddWithValue("id", segment.Id);
            insertCommand.Parameters.AddWithValue("item_id", segment.ItemId);
            insertCommand.Parameters.AddWithValue("segment_type", segment.SegmentType);
            insertCommand.Parameters.AddWithValue("start_seconds", segment.StartSeconds);
            insertCommand.Parameters.AddWithValue("end_seconds", segment.EndSeconds);
            insertCommand.Parameters.AddWithValue("source", segment.Source);
            insertCommand.Parameters.AddWithValue("updated_at", segment.UpdatedAt);
            await insertCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteManualMediaSegmentsAsync(Guid itemId, IReadOnlyList<string>? segmentTypes, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            """
            delete from cnx_media_segments
            where item_id = @item_id
                and source = @source
                and (@filter_types = false or segment_type = any(@segment_types))
            """);
        command.Parameters.AddWithValue("item_id", itemId);
        command.Parameters.AddWithValue("source", CustomNetflixManualSegmentPolicy.ManualSource);
        command.Parameters.AddWithValue("filter_types", segmentTypes is { Count: > 0 });
        command.Parameters.AddWithValue("segment_types", segmentTypes?.ToArray() ?? Array.Empty<string>());
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<RankedItemRow>> GetTrendingItemsAsync(int limit, CancellationToken cancellationToken)
        => GetRankedItemsAsync(
            """
            with per_profile as (
                select
                    item_id,
                    profile_id,
                    max(case event_type
                        when 'complete' then 5.0
                        when 'mark_played' then 4.0
                        when 'progress' then 1.0
                        when 'pause' then 0.5
                        else 1.0
                    end) as profile_score,
                    count(*) as event_count,
                    max(created_at) as last_event_at
                from cnx_watch_events
                where created_at >= now() - interval '7 days'
                group by item_id, profile_id
            ),
            per_profile_capped as (
                select
                    item_id,
                    (
                        profile_score
                        + least(event_count * 0.08, 0.8)
                    ) * power(0.5, extract(epoch from (now() - last_event_at)) / (86400.0 * 2.0)) as profile_score
                from per_profile
            ),
            scored as (
                select
                    item_id,
                    sum(profile_score) + (count(*) * 2.0) as score
                from per_profile_capped
                group by item_id
            )
            select
                item_id,
                score,
                cast(row_number() over (order by score desc) as int) as rank
            from scored
            order by score desc
            limit @limit
            """,
            limit,
            cancellationToken);

    public Task<IReadOnlyList<RankedItemRow>> GetTopTenItemsAsync(int limit, CancellationToken cancellationToken)
        => GetRankedItemsAsync(
            """
            with per_profile as (
                select
                    item_id,
                    profile_id,
                    max(case event_type
                        when 'complete' then 8.0
                        when 'mark_played' then 7.0
                        when 'progress' then 1.0
                        else 0.5
                    end) as profile_score,
                    count(*) as event_count,
                    max(created_at) as last_event_at
                from cnx_watch_events
                where created_at >= now() - interval '30 days'
                group by item_id, profile_id
            ),
            per_profile_capped as (
                select
                    item_id,
                    (
                        profile_score
                        + least(event_count * 0.04, 0.6)
                    ) * power(0.5, extract(epoch from (now() - last_event_at)) / (86400.0 * 7.0)) as profile_score
                from per_profile
            ),
            scored as (
                select
                    item_id,
                    sum(profile_score) + (count(*) * 3.0) as score
                from per_profile_capped
                group by item_id
            )
            select
                item_id,
                score,
                cast(row_number() over (order by score desc) as int) as rank
            from scored
            order by score desc
            limit @limit
            """,
            Math.Min(limit, 10),
            cancellationToken);

    public async Task<RankingSnapshotRow?> GetRankingSnapshotAsync(string rankingId, int limit, DateTime utcNow, CancellationToken cancellationToken)
    {
        using var timer = CustomNetflixMetrics.MeasurePostgreSqlOperation("get_ranking_snapshot");
        await using var command = _dataSource.CreateCommand(
            """
            select
                ranking_id,
                items_json,
                generated_at,
                expires_at
            from cnx_ranking_snapshots
            where ranking_id = @ranking_id
                and expires_at > @utc_now
            """);
        command.Parameters.AddWithValue("ranking_id", rankingId);
        command.Parameters.AddWithValue("utc_now", utcNow);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var items = JsonSerializer.Deserialize<List<RankingSnapshotItem>>(reader.GetString(1)) ?? [];
        return new RankingSnapshotRow(
            reader.GetString(0),
            items
                .OrderBy(item => item.Rank)
                .Take(Math.Clamp(limit, 1, 100))
                .Select(item => new RankedItemRow(item.ItemId, item.Score, item.Rank))
                .ToArray(),
            reader.GetDateTime(2),
            reader.GetDateTime(3));
    }

    public async Task SaveRankingSnapshotAsync(string rankingId, IReadOnlyList<RankedItemRow> items, DateTime generatedAt, DateTime expiresAt, CancellationToken cancellationToken)
    {
        using var timer = CustomNetflixMetrics.MeasurePostgreSqlOperation("save_ranking_snapshot");
        var snapshotItems = items
            .OrderBy(item => item.Rank)
            .Select(item => new RankingSnapshotItem(item.ItemId, item.Score, item.Rank))
            .ToArray();
        await using var command = _dataSource.CreateCommand(
            """
            insert into cnx_ranking_snapshots (
                ranking_id,
                generated_at,
                expires_at,
                items_json)
            values (
                @ranking_id,
                @generated_at,
                @expires_at,
                @items_json)
            on conflict (ranking_id) do update set
                generated_at = excluded.generated_at,
                expires_at = excluded.expires_at,
                items_json = excluded.items_json,
                updated_at = now()
            """);
        command.Parameters.AddWithValue("ranking_id", rankingId);
        command.Parameters.AddWithValue("generated_at", generatedAt);
        command.Parameters.AddWithValue("expires_at", expiresAt);
        command.Parameters.Add(new NpgsqlParameter("items_json", NpgsqlDbType.Jsonb)
        {
            Value = JsonSerializer.Serialize(snapshotItems)
        });
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<HomeSnapshotRow?> GetHomeSnapshotAsync(Guid profileId, string snapshotKey, DateTime utcNow, CancellationToken cancellationToken)
    {
        using var timer = CustomNetflixMetrics.MeasurePostgreSqlOperation("get_home_snapshot");
        await using var command = _dataSource.CreateCommand(
            """
            select
                profile_id,
                snapshot_key,
                response_json,
                generated_at,
                expires_at
            from cnx_home_row_snapshots
            where profile_id = @profile_id
                and snapshot_key = @snapshot_key
                and expires_at > @utc_now
            """);
        command.Parameters.AddWithValue("profile_id", profileId);
        command.Parameters.AddWithValue("snapshot_key", snapshotKey);
        command.Parameters.AddWithValue("utc_now", utcNow);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
            ? new HomeSnapshotRow(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetDateTime(3),
                reader.GetDateTime(4))
            : null;
    }

    public async Task SaveHomeSnapshotAsync(Guid profileId, string snapshotKey, string payloadJson, DateTime generatedAt, DateTime expiresAt, CancellationToken cancellationToken)
    {
        using var timer = CustomNetflixMetrics.MeasurePostgreSqlOperation("save_home_snapshot");
        await using var command = _dataSource.CreateCommand(
            """
            insert into cnx_home_row_snapshots (
                profile_id,
                snapshot_key,
                generated_at,
                expires_at,
                response_json)
            values (
                @profile_id,
                @snapshot_key,
                @generated_at,
                @expires_at,
                @response_json)
            on conflict (profile_id, snapshot_key) do update set
                generated_at = excluded.generated_at,
                expires_at = excluded.expires_at,
                response_json = excluded.response_json,
                updated_at = now()
            """);
        command.Parameters.AddWithValue("profile_id", profileId);
        command.Parameters.AddWithValue("snapshot_key", snapshotKey);
        command.Parameters.AddWithValue("generated_at", generatedAt);
        command.Parameters.AddWithValue("expires_at", expiresAt);
        command.Parameters.Add(new NpgsqlParameter("response_json", NpgsqlDbType.Jsonb)
        {
            Value = payloadJson
        });
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteHomeSnapshotsAsync(Guid profileId, CancellationToken cancellationToken)
    {
        using var timer = CustomNetflixMetrics.MeasurePostgreSqlOperation("delete_home_snapshots");
        await using var command = _dataSource.CreateCommand(
            "delete from cnx_home_row_snapshots where profile_id = @profile_id");
        command.Parameters.AddWithValue("profile_id", profileId);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<RankedItemRow>> GetRankedItemsAsync(string sql, int limit, CancellationToken cancellationToken)
    {
        using var timer = CustomNetflixMetrics.MeasurePostgreSqlOperation("get_ranked_items");
        var rows = new List<RankedItemRow>();
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("limit", Math.Clamp(limit, 1, 100));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            rows.Add(new RankedItemRow(
                reader.GetGuid(0),
                reader.GetDouble(1),
                reader.GetInt32(2)));
        }

        return rows;
    }

    private static async Task InsertWatchEventAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        WatchEventRow watchEvent,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            insert into cnx_watch_events (
                id,
                profile_id,
                jellyfin_user_id,
                item_id,
                item_type,
                event_type,
                position_seconds,
                duration_seconds,
                play_session_id,
                client_name)
            values (
                @id,
                @profile_id,
                @jellyfin_user_id,
                @item_id,
                @item_type,
                @event_type,
                @position_seconds,
                @duration_seconds,
                @play_session_id,
                @client_name)
            on conflict (id) do nothing
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("id", watchEvent.Id);
        command.Parameters.AddWithValue("profile_id", watchEvent.ProfileId);
        command.Parameters.AddWithValue("jellyfin_user_id", watchEvent.JellyfinUserId);
        command.Parameters.AddWithValue("item_id", watchEvent.ItemId);
        command.Parameters.AddWithValue("item_type", watchEvent.ItemType);
        command.Parameters.AddWithValue("event_type", watchEvent.EventType);
        command.Parameters.AddWithValue("position_seconds", watchEvent.PositionSeconds);
        command.Parameters.AddWithValue("duration_seconds", watchEvent.DurationSeconds);
        command.Parameters.AddWithValue("play_session_id", (object?)watchEvent.PlaySessionId ?? DBNull.Value);
        command.Parameters.AddWithValue("client_name", (object?)watchEvent.ClientName ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<WatchProgressRow?> UpsertProgressCoreAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        WatchProgressRow progress,
        CancellationToken cancellationToken)
    {
        await using var progressCommand = new NpgsqlCommand(
            """
            insert into cnx_watch_progress (
                profile_id,
                item_id,
                media_source_id,
                position_seconds,
                duration_seconds,
                percent_viewed,
                completed,
                play_count,
                last_played_at)
            values (
                @profile_id,
                @item_id,
                @media_source_id,
                @position_seconds,
                @duration_seconds,
                @percent_viewed,
                @completed,
                case when @completed then 1 else 0 end,
                @last_played_at)
            on conflict (profile_id, item_id) do update set
                media_source_id = excluded.media_source_id,
                position_seconds = excluded.position_seconds,
                duration_seconds = excluded.duration_seconds,
                percent_viewed = excluded.percent_viewed,
                completed = excluded.completed,
                play_count = cnx_watch_progress.play_count + case when @completed and not cnx_watch_progress.completed then 1 else 0 end,
                last_played_at = excluded.last_played_at,
                updated_at = now()
            where cnx_watch_progress.last_played_at <= excluded.last_played_at
            returning
                profile_id,
                item_id,
                media_source_id,
                position_seconds,
                duration_seconds,
                percent_viewed,
                completed,
                play_count,
                last_played_at
            """,
            connection,
            transaction);
        AddProgressParameters(progressCommand, progress);

        await using var reader = await progressCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? ReadProgress(reader) : null;
    }

    private static async Task<Guid?> EnsureDefaultProfileAfterDeleteAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid jellyfinUserId,
        CancellationToken cancellationToken)
    {
        await using var selectCommand = new NpgsqlCommand(
            """
            select id, is_default
            from cnx_profiles
            where jellyfin_user_id = @jellyfin_user_id
                and deleted_at is null
            order by is_default desc, created_at asc
            limit 1
            """,
            connection,
            transaction);
        selectCommand.Parameters.AddWithValue("jellyfin_user_id", jellyfinUserId);

        Guid? replacementProfileId = null;
        var isDefault = false;
        await using (var reader = await selectCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                replacementProfileId = reader.GetGuid(0);
                isDefault = reader.GetBoolean(1);
            }
        }

        if (replacementProfileId.HasValue && !isDefault)
        {
            await using var promoteCommand = new NpgsqlCommand(
                "update cnx_profiles set is_default = true, updated_at = now() where id = @profile_id",
                connection,
                transaction);
            promoteCommand.Parameters.AddWithValue("profile_id", replacementProfileId.Value);
            await promoteCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        return replacementProfileId;
    }

    private static async Task RepairActiveProfileAfterDeleteAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid jellyfinUserId,
        Guid deletedProfileId,
        Guid? replacementProfileId,
        CancellationToken cancellationToken)
    {
        if (replacementProfileId.HasValue)
        {
            await using var updateCommand = new NpgsqlCommand(
                """
                update cnx_active_profiles
                set
                    profile_id = @replacement_profile_id,
                    updated_at = now()
                where jellyfin_user_id = @jellyfin_user_id
                    and profile_id = @deleted_profile_id
                """,
                connection,
                transaction);
            updateCommand.Parameters.AddWithValue("jellyfin_user_id", jellyfinUserId);
            updateCommand.Parameters.AddWithValue("deleted_profile_id", deletedProfileId);
            updateCommand.Parameters.AddWithValue("replacement_profile_id", replacementProfileId.Value);
            await updateCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        await using var deleteCommand = new NpgsqlCommand(
            """
            delete from cnx_active_profiles
            where jellyfin_user_id = @jellyfin_user_id
                and profile_id = @deleted_profile_id
            """,
            connection,
            transaction);
        deleteCommand.Parameters.AddWithValue("jellyfin_user_id", jellyfinUserId);
        deleteCommand.Parameters.AddWithValue("deleted_profile_id", deletedProfileId);
        await deleteCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task UpsertWatchHistoryAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, WatchProgressRow progress, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            insert into cnx_watch_history (
                profile_id,
                item_id,
                first_played_at,
                last_played_at,
                completed_at,
                play_count)
            values (
                @profile_id,
                @item_id,
                @last_played_at,
                @last_played_at,
                case when @completed then @last_played_at else null end,
                @play_count)
            on conflict (profile_id, item_id) do update set
                last_played_at = excluded.last_played_at,
                completed_at = case when @completed then excluded.last_played_at else cnx_watch_history.completed_at end,
                play_count = greatest(cnx_watch_history.play_count, excluded.play_count)
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("profile_id", progress.ProfileId);
        command.Parameters.AddWithValue("item_id", progress.ItemId);
        command.Parameters.AddWithValue("last_played_at", progress.LastPlayedAt);
        command.Parameters.AddWithValue("completed", progress.Completed);
        command.Parameters.AddWithValue("play_count", progress.PlayCount);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void AddProgressParameters(NpgsqlCommand command, WatchProgressRow progress)
    {
        command.Parameters.AddWithValue("profile_id", progress.ProfileId);
        command.Parameters.AddWithValue("item_id", progress.ItemId);
        command.Parameters.AddWithValue("media_source_id", (object?)progress.MediaSourceId ?? DBNull.Value);
        command.Parameters.AddWithValue("position_seconds", progress.PositionSeconds);
        command.Parameters.AddWithValue("duration_seconds", progress.DurationSeconds);
        command.Parameters.AddWithValue("percent_viewed", progress.PercentViewed);
        command.Parameters.AddWithValue("completed", progress.Completed);
        command.Parameters.AddWithValue("last_played_at", progress.LastPlayedAt);
    }

    private static ProfileRow ReadProfile(NpgsqlDataReader reader)
    {
        var profileId = reader.GetGuid(0);
        return new ProfileRow(
            profileId,
            reader.GetGuid(1),
            reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.GetBoolean(4),
            reader.GetBoolean(5),
            reader.GetDateTime(6),
            reader.GetDateTime(7),
            new ProfileSettingsRow(
                profileId,
                reader.GetBoolean(8),
                reader.GetInt32(9),
                reader.GetBoolean(10),
                reader.GetBoolean(11)),
            new PlaybackPreferencesRow(
                profileId,
                reader.GetBoolean(12),
                reader.GetBoolean(13),
                reader.GetBoolean(14),
                reader.GetBoolean(15),
                reader.GetBoolean(16),
                reader.IsDBNull(17) ? null : reader.GetInt32(17),
                reader.IsDBNull(18) ? null : reader.GetString(18),
                reader.IsDBNull(19) ? null : reader.GetString(19),
                reader.GetBoolean(20),
                reader.GetBoolean(21),
                reader.GetBoolean(22),
                reader.GetBoolean(23)));
    }

    private static WatchProgressRow ReadProgress(NpgsqlDataReader reader)
        => new(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.IsDBNull(2) ? null : reader.GetGuid(2),
            reader.GetDouble(3),
            reader.GetDouble(4),
            reader.GetDouble(5),
            reader.GetBoolean(6),
            reader.GetInt32(7),
            reader.GetDateTime(8));

    private static WatchHistoryRow ReadWatchHistory(NpgsqlDataReader reader)
        => new(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetDateTime(2),
            reader.GetDateTime(3),
            reader.IsDBNull(4) ? null : reader.GetDateTime(4),
            reader.GetInt32(5));

    private static ItemFeedbackRow ReadItemFeedback(NpgsqlDataReader reader)
        => new(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetString(2),
            reader.GetDateTime(3));

    private sealed record RankingSnapshotItem(Guid ItemId, double Score, int Rank);
}
