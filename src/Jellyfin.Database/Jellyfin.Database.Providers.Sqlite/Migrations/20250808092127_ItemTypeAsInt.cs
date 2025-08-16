using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <inheritdoc />
    public partial class ItemTypeAsInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BaseItems_Id_Type_IsFolder_IsVirtualItem",
                table: "BaseItems");

            migrationBuilder.DropIndex(
                name: "IX_BaseItems_Type_SeriesPresentationUniqueKey_IsFolder_IsVirtualItem",
                table: "BaseItems");

            migrationBuilder.DropIndex(
                name: "IX_BaseItems_Type_SeriesPresentationUniqueKey_PresentationUniqueKey_SortName",
                table: "BaseItems");

            migrationBuilder.DropIndex(
                name: "IX_BaseItems_Type_TopParentId_Id",
                table: "BaseItems");

            migrationBuilder.DropIndex(
                name: "IX_BaseItems_Type_TopParentId_IsVirtualItem_PresentationUniqueKey_DateCreated",
                table: "BaseItems");

            migrationBuilder.DropIndex(
                name: "IX_BaseItems_Type_TopParentId_PresentationUniqueKey",
                table: "BaseItems");

            migrationBuilder.DropIndex(
                name: "IX_BaseItems_Type_TopParentId_StartDate",
                table: "BaseItems");

            migrationBuilder.AddColumn<int>(
                name: "ItemType",
                table: "BaseItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: -1);

            migrationBuilder.CreateTable(
                name: "BaseItemKinds",
                columns: table => new
                {
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    TypeName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseItemKinds", x => x.Kind);
                });

            migrationBuilder.InsertData(
                table: "BaseItemKinds",
                columns: new[] { "Kind", "Description", "TypeName" },
                values: new object[,]
                {
                    { -1, "Default if no class found", "PLACEHOLDER" },
                    { 0, "Aggregate Folder", "MediaBrowser.Controller.Entities.AggregateFolder" },
                    { 1, "Audio File", "MediaBrowser.Controller.Entities.Audio.Audio" },
                    { 2, "Audio Book", "MediaBrowser.Controller.Entities.AudioBook" },
                    { 3, "Plugin Folder", "MediaBrowser.Controller.Entities.BasePluginFolder" },
                    { 4, "Book", "MediaBrowser.Controller.Entities.Book" },
                    { 5, "Box Set", "MediaBrowser.Controller.Entities.Movies.BoxSet" },
                    { 6, "Channel", "MediaBrowser.Controller.Channels.Channel" },
                    { 7, "Channel Folder Item (virtual class)", "?.ChannelFolderItem" },
                    { 8, "Collection Folder", "MediaBrowser.Controller.Entities.CollectionFolder" },
                    { 9, "TV Episode", "MediaBrowser.Controller.Entities.TV.Episode" },
                    { 10, "Folder", "MediaBrowser.Controller.Entities.Folder" },
                    { 11, "Genre", "MediaBrowser.Controller.Entities.Genre" },
                    { 12, "Live TV Channel", "MediaBrowser.Controller.LiveTv.LiveTvChannel" },
                    { 13, "Live TV Program", "MediaBrowser.Controller.LiveTv.LiveTvProgram" },
                    { 14, "Manual Playlists Folder", "Emby.Server.Implementations.Playlists.PlaylistsFolder" },
                    { 15, "Movie", "MediaBrowser.Controller.Entities.Movies.Movie" },
                    { 16, "Music Album", "MediaBrowser.Controller.Entities.Audio.MusicAlbum" },
                    { 17, "Music Artist", "MediaBrowser.Controller.Entities.Audio.MusicArtist" },
                    { 18, "Music Genre", "MediaBrowser.Controller.Entities.Audio.MusicGenre" },
                    { 19, "Music Video", "MediaBrowser.Controller.Entities.MusicVideo" },
                    { 20, "Person", "MediaBrowser.Controller.Entities.Person" },
                    { 21, "Photo", "MediaBrowser.Controller.Entities.Photo" },
                    { 22, "Photo Album", "MediaBrowser.Controller.Entities.PhotoAlbum" },
                    { 23, "Playlist", "MediaBrowser.Controller.Playlists.Playlist" },
                    { 24, "Recording (obsolete?)", "?.Recording" },
                    { 25, "TV Season", "MediaBrowser.Controller.Entities.TV.Season" },
                    { 26, "TV Series", "MediaBrowser.Controller.Entities.TV.Series" },
                    { 27, "Studio", "MediaBrowser.Controller.Entities.Studio" },
                    { 28, "Trailer", "MediaBrowser.Controller.Entities.Trailer" },
                    { 29, "User Root Folder", "MediaBrowser.Controller.Entities.UserRootFolder" },
                    { 30, "User View", "MediaBrowser.Controller.Entities.UserView" },
                    { 31, "Video", "MediaBrowser.Controller.Entities.Video" },
                    { 32, "Year", "MediaBrowser.Controller.Entities.Year" }
                });

            migrationBuilder.UpdateData(
                table: "BaseItems",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "ItemType", "Name" },
                values: new object[] { -1, "This is a placeholder item for UserData that has been detected from its original item" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_ItemType_SeriesPresentationUniqueKey_IsFolder_IsVirtualItem",
                table: "BaseItems",
                columns: new[] { "ItemType", "SeriesPresentationUniqueKey", "IsFolder", "IsVirtualItem" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_ItemType_SeriesPresentationUniqueKey_PresentationUniqueKey_SortName",
                table: "BaseItems",
                columns: new[] { "ItemType", "SeriesPresentationUniqueKey", "PresentationUniqueKey", "SortName" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_ItemType_TopParentId",
                table: "BaseItems",
                columns: new[] { "ItemType", "TopParentId" });

            migrationBuilder.AddForeignKey(
                name: "FK_BaseItems_BaseItemKinds_ItemType",
                table: "BaseItems",
                column: "ItemType",
                principalTable: "BaseItemKinds",
                principalColumn: "Kind");

            // with TvChannel -> LiveTvChannel, Program -> LiveTvProgram
            migrationBuilder.Sql(@"
                UPDATE BaseItems
                SET ItemType = CASE 
                    WHEN Type = 'MediaBrowser.Controller.LiveTv.TvChannel' THEN 12
                    WHEN Type = 'MediaBrowser.Controller.LiveTv.Program' THEN 13
                    ELSE COALESCE((
                        SELECT Kind
                        FROM BaseItemKinds
                        WHERE BaseItemKinds.TypeName = BaseItems.Type
                    ), -1)
                END;
            ");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "BaseItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // this one and orhers below WILL NOT work in downgrade case.
            // EF refers to:
            // "SQLite does not support this migration operation ('DropForeignKeyOperation/DropColumnOperation').
            // See https://go.microsoft.com/fwlink/?LinkId=723262 for more information and examples."
            // in 'up' it is ok, seems rebuild is applied but in 'down' ef sucks
            migrationBuilder.DropForeignKey(
                name: "FK_BaseItems_BaseItemKinds_ItemType",
                table: "BaseItems");

            migrationBuilder.DropTable(
                name: "BaseItemKinds");

            migrationBuilder.DropIndex(
                name: "IX_BaseItems_ItemType_SeriesPresentationUniqueKey_IsFolder_IsVirtualItem",
                table: "BaseItems");

            migrationBuilder.DropIndex(
                name: "IX_BaseItems_ItemType_SeriesPresentationUniqueKey_PresentationUniqueKey_SortName",
                table: "BaseItems");

            migrationBuilder.DropIndex(
                name: "IX_BaseItems_ItemType_TopParentId",
                table: "BaseItems");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "BaseItems",
                type: "TEXT",
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.Sql(@"
                UPDATE BaseItems
                SET Type = COALESCE((
                    SELECT TypeName
                    FROM BaseItemKinds
                    WHERE BaseItemKinds.Kind = BaseItems.ItemType
                ), '?');");

            migrationBuilder.DropColumn(
                name: "ItemType",
                table: "BaseItems");

            migrationBuilder.UpdateData(
                table: "BaseItems",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "Name", "Type" },
                values: new object[] { "This is a placeholder item for UserData that has been detacted from its original item", "PLACEHOLDER" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Id_Type_IsFolder_IsVirtualItem",
                table: "BaseItems",
                columns: new[] { "Id", "Type", "IsFolder", "IsVirtualItem" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_SeriesPresentationUniqueKey_IsFolder_IsVirtualItem",
                table: "BaseItems",
                columns: new[] { "Type", "SeriesPresentationUniqueKey", "IsFolder", "IsVirtualItem" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_SeriesPresentationUniqueKey_PresentationUniqueKey_SortName",
                table: "BaseItems",
                columns: new[] { "Type", "SeriesPresentationUniqueKey", "PresentationUniqueKey", "SortName" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_TopParentId_Id",
                table: "BaseItems",
                columns: new[] { "Type", "TopParentId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_TopParentId_IsVirtualItem_PresentationUniqueKey_DateCreated",
                table: "BaseItems",
                columns: new[] { "Type", "TopParentId", "IsVirtualItem", "PresentationUniqueKey", "DateCreated" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_TopParentId_PresentationUniqueKey",
                table: "BaseItems",
                columns: new[] { "Type", "TopParentId", "PresentationUniqueKey" });

            migrationBuilder.CreateIndex(
                name: "IX_BaseItems_Type_TopParentId_StartDate",
                table: "BaseItems",
                columns: new[] { "Type", "TopParentId", "StartDate" });
        }
    }
}
