#!/bin/sh
embypath=${2:-/var/lib/emby-server}
jellyfinpath=${3:-/var/lib/jellyfin}
db=${1:-${jellyfinpath}/data/library.db}
sqlite3 ${db} << SQL
UPDATE Chapters2 
SET ImagePath=REPLACE(ImagePath, '${embypath}', '${jellyfinpath}');
UPDATE TypedBaseItems 
SET Path=REPLACE(Path, '${embypath}', '${jellyfinpath}');
UPDATE TypedBaseItems 
SET data=REPLACE(data, '${embypath}', '${jellyfinpath}');
SQL
