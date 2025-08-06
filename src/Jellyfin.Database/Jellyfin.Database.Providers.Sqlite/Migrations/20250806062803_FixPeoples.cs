using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jellyfin.Server.Implementations.Migrations
{
    /// <summary>
    /// Clean up duplicate People records and enforce unique constraints.
    /// </summary>
    public partial class FixPeoples : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Migrate Conductor and Composer names to Artists column before deletion
            migrationBuilder.Sql(@"
                UPDATE BaseItems
                SET Artists = (
                    CASE
                        WHEN Artists IS NULL OR Artists = '' THEN
                            -- No existing artists, just add the new ones
                            (SELECT GROUP_CONCAT(p.Name, '|')
                             FROM (SELECT DISTINCT p.Name
                                   FROM PeopleBaseItemMap pbim
                                   JOIN Peoples p ON pbim.PeopleId = p.Id
                                   WHERE pbim.ItemId = BaseItems.Id
                                     AND p.PersonType IN ('Conductor', 'Composer')
                                   ORDER BY CASE p.PersonType WHEN 'Conductor' THEN 1 WHEN 'Composer' THEN 2 END) p)
                        ELSE
                            -- Merge with existing artists, avoiding duplicates
                            (SELECT CASE
                                WHEN new_names.names IS NULL THEN BaseItems.Artists
                                ELSE BaseItems.Artists || '|' || new_names.names
                            END
                            FROM (
                                SELECT GROUP_CONCAT(p.Name, '|') as names
                                FROM (SELECT DISTINCT p.Name
                                      FROM PeopleBaseItemMap pbim
                                      JOIN Peoples p ON pbim.PeopleId = p.Id
                                      WHERE pbim.ItemId = BaseItems.Id
                                        AND p.PersonType IN ('Conductor', 'Composer')
                                        AND ('|' || BaseItems.Artists || '|') NOT LIKE ('%|' || p.Name || '|%')
                                      ORDER BY CASE p.PersonType WHEN 'Conductor' THEN 1 WHEN 'Composer' THEN 2 END) p
                            ) new_names)
                    END
                )
                WHERE Id IN (
                    SELECT DISTINCT pbim.ItemId
                    FROM PeopleBaseItemMap pbim
                    JOIN Peoples p ON pbim.PeopleId = p.Id
                    WHERE p.PersonType IN ('Conductor', 'Composer')
                );
            ");

            // Step 2: Delete People with removed PersonType values (Artist, AlbumArtist, Composer, Conductor)
            // These are no longer valid PersonKind enum values as of commit 62ff759f
            migrationBuilder.Sql(@"
                DELETE FROM Peoples
                WHERE PersonType IN ('Artist', 'AlbumArtist', 'Composer', 'Conductor');
            ");

            // Step 3: Update any NULL PersonType values to "Unknown" (default from PersonKind enum)
            migrationBuilder.Sql(@"
                UPDATE Peoples
                SET PersonType = 'Unknown'
                WHERE PersonType IS NULL;
            ");

            // Step 4: Remove PeopleBaseItemMap records that reference deleted People records
            migrationBuilder.Sql(@"
                DELETE FROM PeopleBaseItemMap
                WHERE PeopleId NOT IN (SELECT Id FROM Peoples);
            ");

            // Step 4b: Consolidate remaining PeopleBaseItemMap to point to canonical People records
            // For each (Name, PersonType) combination, use the People record with MIN(Id)
            migrationBuilder.Sql(@"
                UPDATE PeopleBaseItemMap
                SET PeopleId = (
                    SELECT MIN(p.Id)
                    FROM Peoples p
                    WHERE p.Name = (SELECT Name FROM Peoples WHERE Id = PeopleBaseItemMap.PeopleId)
                      AND p.PersonType = (SELECT PersonType FROM Peoples WHERE Id = PeopleBaseItemMap.PeopleId)
                )
                WHERE PeopleId NOT IN (
                    SELECT MIN(Id)
                    FROM Peoples
                    GROUP BY Name, PersonType
                );
            ");

            // Step 5: Clean up duplicate PeopleBaseItemMap records after consolidation
            migrationBuilder.Sql(@"
                DELETE FROM PeopleBaseItemMap
                WHERE rowid NOT IN (
                    SELECT MIN(rowid)
                    FROM PeopleBaseItemMap
                    GROUP BY ItemId, PeopleId
                );
            ");

            // Step 6: Delete orphaned People records
            migrationBuilder.Sql(@"
                DELETE FROM Peoples
                WHERE Id NOT IN (
                    SELECT DISTINCT PeopleId
                    FROM PeopleBaseItemMap
                );
            ");

            // Step 7: Drop the old IX_Peoples_Name index (will be superseded by unique index)
            migrationBuilder.DropIndex(
                name: "IX_Peoples_Name",
                table: "Peoples");

            // Step 8: Make PersonType column NOT NULL (SQLite doesn't support ALTER COLUMN directly)
            // We need to recreate the table
            migrationBuilder.Sql(@"
                -- Create temporary table with NOT NULL constraint
                CREATE TABLE Peoples_temp (
                    Id TEXT NOT NULL,
                    Name TEXT NOT NULL,
                    PersonType TEXT NOT NULL DEFAULT 'Unknown',
                    CONSTRAINT PK_Peoples PRIMARY KEY (Id)
                );
            ");

            migrationBuilder.Sql(@"
                -- Copy data from old table
                INSERT INTO Peoples_temp (Id, Name, PersonType)
                SELECT Id, Name, COALESCE(PersonType, 'Unknown')
                FROM Peoples;
            ");

            migrationBuilder.Sql(@"
                -- Drop old table and recreate foreign key constraints
                PRAGMA foreign_keys=off;
                DROP TABLE Peoples;
                ALTER TABLE Peoples_temp RENAME TO Peoples;
                PRAGMA foreign_keys=on;
            ");

            // Step 9: Delete orphaned Person BaseItems that don't have corresponding Peoples records
            migrationBuilder.Sql(@"
                DELETE FROM BaseItems
                WHERE Type = 'MediaBrowser.Controller.Entities.Person'
                  AND Name NOT IN (SELECT DISTINCT Name FROM Peoples);
            ");

            // Step 10: Create unique index on Name, PersonType (replaces the old IX_Peoples_Name)
            migrationBuilder.CreateIndex(
                name: "IX_Peoples_Name_PersonType",
                table: "Peoples",
                columns: new[] { "Name", "PersonType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the unique index
            migrationBuilder.DropIndex(
                name: "IX_Peoples_Name_PersonType",
                table: "Peoples");

            // Recreate the old IX_Peoples_Name index
            migrationBuilder.CreateIndex(
                name: "IX_Peoples_Name",
                table: "Peoples",
                column: "Name");

            // Note: We cannot easily reverse the data cleanup or make PersonType nullable again
            // in SQLite without recreating the table again. This is a destructive migration.
        }
    }
}
