using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <inheritdoc />
    public partial class DetachUserDataInsteadOfDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RetentionDate",
                table: "UserData",
                type: "TEXT",
                nullable: true);

            migrationBuilder.InsertData(
                table: "BaseItems",
                columns: new[] { "Id", "Album", "AlbumArtists", "Artists", "Audio", "ChannelId", "CleanName", "CommunityRating", "CriticRating", "CustomRating", "Data", "DateCreated", "DateLastMediaAdded", "DateLastRefreshed", "DateLastSaved", "DateModified", "EndDate", "EpisodeTitle", "ExternalId", "ExternalSeriesId", "ExternalServiceId", "ExtraIds", "ExtraType", "ForcedSortName", "Genres", "Height", "IndexNumber", "InheritedParentalRatingSubValue", "InheritedParentalRatingValue", "IsFolder", "IsInMixedFolder", "IsLocked", "IsMovie", "IsRepeat", "IsSeries", "IsVirtualItem", "LUFS", "MediaType", "Name", "NormalizationGain", "OfficialRating", "OriginalTitle", "Overview", "OwnerId", "ParentId", "ParentIndexNumber", "Path", "PreferredMetadataCountryCode", "PreferredMetadataLanguage", "PremiereDate", "PresentationUniqueKey", "PrimaryVersionId", "ProductionLocations", "ProductionYear", "RunTimeTicks", "SeasonId", "SeasonName", "SeriesId", "SeriesName", "SeriesPresentationUniqueKey", "ShowId", "Size", "SortName", "StartDate", "Studios", "Tagline", "Tags", "TopParentId", "TotalBitrate", "Type", "UnratedType", "Width" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, false, false, false, false, false, false, false, null, null, "This is a placeholder item for UserData that has been detacted from its original item", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "PLACEHOLDER", null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RetentionDate",
                table: "UserData");

            migrationBuilder.DeleteData(
                table: "BaseItems",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"));
        }
    }
}
