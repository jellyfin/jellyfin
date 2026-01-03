# Fix Corrupted DateTime Data in Jellyfin SQLite Database

This guide helps troubleshoot and fix corrupted datetime values in the Jellyfin SQLite database that cause API crashes with errors like:

```
System.FormatException: String '2025-89-11 18:11:29.5154458' was not recognized as a valid DateTime
```

## Find Corrupted Records

Run this SQL query against your `jellyfin.db` file to identify all corrupted datetime records:

```sql
-- Find all BaseItems with invalid datetime values
SELECT
    Id,
    Name,
    Path,
    Type,
    DateCreated,
    DateModified,
    PremiereDate,
    StartDate,
    EndDate,
    DateLastMediaAdded,
    DateLastRefreshed,
    DateLastSaved
FROM BaseItems
WHERE
    -- Invalid DateCreated
    (DateCreated IS NOT NULL AND (
        CAST(SUBSTR(DateCreated, 6, 2) AS INTEGER) < 1 OR
        CAST(SUBSTR(DateCreated, 6, 2) AS INTEGER) > 12 OR
        CAST(SUBSTR(DateCreated, 9, 2) AS INTEGER) < 1 OR
        CAST(SUBSTR(DateCreated, 9, 2) AS INTEGER) > 31 OR
        CAST(SUBSTR(DateCreated, 1, 4) AS INTEGER) < 1900 OR
        CAST(SUBSTR(DateCreated, 1, 4) AS INTEGER) > 2100
    ))
    -- Invalid DateModified
    OR (DateModified IS NOT NULL AND (
        CAST(SUBSTR(DateModified, 6, 2) AS INTEGER) < 1 OR
        CAST(SUBSTR(DateModified, 6, 2) AS INTEGER) > 12 OR
        CAST(SUBSTR(DateModified, 9, 2) AS INTEGER) < 1 OR
        CAST(SUBSTR(DateModified, 9, 2) AS INTEGER) > 31 OR
        CAST(SUBSTR(DateModified, 1, 4) AS INTEGER) < 1900 OR
        CAST(SUBSTR(DateModified, 1, 4) AS INTEGER) > 2100
    ))
    -- Invalid PremiereDate
    OR (PremiereDate IS NOT NULL AND (
        CAST(SUBSTR(PremiereDate, 6, 2) AS INTEGER) < 1 OR
        CAST(SUBSTR(PremiereDate, 6, 2) AS INTEGER) > 12 OR
        CAST(SUBSTR(PremiereDate, 9, 2) AS INTEGER) < 1 OR
        CAST(SUBSTR(PremiereDate, 9, 2) AS INTEGER) > 31 OR
        CAST(SUBSTR(PremiereDate, 1, 4) AS INTEGER) < 1900 OR
        CAST(SUBSTR(PremiereDate, 1, 4) AS INTEGER) > 2100
    ))
    -- Invalid StartDate
    OR (StartDate IS NOT NULL AND (
        CAST(SUBSTR(StartDate, 6, 2) AS INTEGER) < 1 OR
        CAST(SUBSTR(StartDate, 6, 2) AS INTEGER) > 12 OR
        CAST(SUBSTR(StartDate, 9, 2) AS INTEGER) < 1 OR
        CAST(SUBSTR(StartDate, 9, 2) AS INTEGER) > 31 OR
        CAST(SUBSTR(StartDate, 1, 4) AS INTEGER) < 1900 OR
        CAST(SUBSTR(StartDate, 1, 4) AS INTEGER) > 2100
    ))
    -- Invalid EndDate
    OR (EndDate IS NOT NULL AND (
        CAST(SUBSTR(EndDate, 6, 2) AS INTEGER) < 1 OR
        CAST(SUBSTR(EndDate, 6, 2) AS INTEGER) > 12 OR
        CAST(SUBSTR(EndDate, 9, 2) AS INTEGER) < 1 OR
        CAST(SUBSTR(EndDate, 9, 2) AS INTEGER) > 31 OR
        CAST(SUBSTR(EndDate, 1, 4) AS INTEGER) < 1900 OR
        CAST(SUBSTR(EndDate, 1, 4) AS INTEGER) > 2100
    ))
    -- Invalid DateLastMediaAdded
    OR (DateLastMediaAdded IS NOT NULL AND (
        CAST(SUBSTR(DateLastMediaAdded, 6, 2) AS INTEGER) < 1 OR
        CAST(SUBSTR(DateLastMediaAdded, 6, 2) AS INTEGER) > 12 OR
        CAST(SUBSTR(DateLastMediaAdded, 9, 2) AS INTEGER) < 1 OR
        CAST(SUBSTR(DateLastMediaAdded, 9, 2) AS INTEGER) > 31 OR
        CAST(SUBSTR(DateLastMediaAdded, 1, 4) AS INTEGER) < 1900 OR
        CAST(SUBSTR(DateLastMediaAdded, 1, 4) AS INTEGER) > 2100
    ))
    -- Invalid DateLastRefreshed
    OR (DateLastRefreshed IS NOT NULL AND (
        CAST(SUBSTR(DateLastRefreshed, 6, 2) AS INTEGER) < 1 OR
        CAST(SUBSTR(DateLastRefreshed, 6, 2) AS INTEGER) > 12 OR
        CAST(SUBSTR(DateLastRefreshed, 9, 2) AS INTEGER) < 1 OR
        CAST(SUBSTR(DateLastRefreshed, 9, 2) AS INTEGER) > 31 OR
        CAST(SUBSTR(DateLastRefreshed, 1, 4) AS INTEGER) < 1900 OR
        CAST(SUBSTR(DateLastRefreshed, 1, 4) AS INTEGER) > 2100
    ))
    -- Invalid DateLastSaved
    OR (DateLastSaved IS NOT NULL AND (
        CAST(SUBSTR(DateLastSaved, 6, 2) AS INTEGER) < 1 OR
        CAST(SUBSTR(DateLastSaved, 6, 2) AS INTEGER) > 12 OR
        CAST(SUBSTR(DateLastSaved, 9, 2) AS INTEGER) < 1 OR
        CAST(SUBSTR(DateLastSaved, 9, 2) AS INTEGER) > 31 OR
        CAST(SUBSTR(DateLastSaved, 1, 4) AS INTEGER) < 1900 OR
        CAST(SUBSTR(DateLastSaved, 1, 4) AS INTEGER) > 2100
    ));
```

## Fix Corrupted Records

Run these UPDATE statements to set invalid datetime values to NULL. This allows Jellyfin to re-scan the items and populate correct values:

```sql
-- Fix DateCreated
UPDATE BaseItems SET DateCreated = NULL
WHERE DateCreated IS NOT NULL AND (
    CAST(SUBSTR(DateCreated, 6, 2) AS INTEGER) < 1 OR
    CAST(SUBSTR(DateCreated, 6, 2) AS INTEGER) > 12 OR
    CAST(SUBSTR(DateCreated, 9, 2) AS INTEGER) < 1 OR
    CAST(SUBSTR(DateCreated, 9, 2) AS INTEGER) > 31 OR
    CAST(SUBSTR(DateCreated, 1, 4) AS INTEGER) < 1900 OR
    CAST(SUBSTR(DateCreated, 1, 4) AS INTEGER) > 2100
);

-- Fix DateModified
UPDATE BaseItems SET DateModified = NULL
WHERE DateModified IS NOT NULL AND (
    CAST(SUBSTR(DateModified, 6, 2) AS INTEGER) < 1 OR
    CAST(SUBSTR(DateModified, 6, 2) AS INTEGER) > 12 OR
    CAST(SUBSTR(DateModified, 9, 2) AS INTEGER) < 1 OR
    CAST(SUBSTR(DateModified, 9, 2) AS INTEGER) > 31 OR
    CAST(SUBSTR(DateModified, 1, 4) AS INTEGER) < 1900 OR
    CAST(SUBSTR(DateModified, 1, 4) AS INTEGER) > 2100
);

-- Fix PremiereDate
UPDATE BaseItems SET PremiereDate = NULL
WHERE PremiereDate IS NOT NULL AND (
    CAST(SUBSTR(PremiereDate, 6, 2) AS INTEGER) < 1 OR
    CAST(SUBSTR(PremiereDate, 6, 2) AS INTEGER) > 12 OR
    CAST(SUBSTR(PremiereDate, 9, 2) AS INTEGER) < 1 OR
    CAST(SUBSTR(PremiereDate, 9, 2) AS INTEGER) > 31 OR
    CAST(SUBSTR(PremiereDate, 1, 4) AS INTEGER) < 1900 OR
    CAST(SUBSTR(PremiereDate, 1, 4) AS INTEGER) > 2100
);

-- Fix StartDate
UPDATE BaseItems SET StartDate = NULL
WHERE StartDate IS NOT NULL AND (
    CAST(SUBSTR(StartDate, 6, 2) AS INTEGER) < 1 OR
    CAST(SUBSTR(StartDate, 6, 2) AS INTEGER) > 12 OR
    CAST(SUBSTR(StartDate, 9, 2) AS INTEGER) < 1 OR
    CAST(SUBSTR(StartDate, 9, 2) AS INTEGER) > 31 OR
    CAST(SUBSTR(StartDate, 1, 4) AS INTEGER) < 1900 OR
    CAST(SUBSTR(StartDate, 1, 4) AS INTEGER) > 2100
);

-- Fix EndDate
UPDATE BaseItems SET EndDate = NULL
WHERE EndDate IS NOT NULL AND (
    CAST(SUBSTR(EndDate, 6, 2) AS INTEGER) < 1 OR
    CAST(SUBSTR(EndDate, 6, 2) AS INTEGER) > 12 OR
    CAST(SUBSTR(EndDate, 9, 2) AS INTEGER) < 1 OR
    CAST(SUBSTR(EndDate, 9, 2) AS INTEGER) > 31 OR
    CAST(SUBSTR(EndDate, 1, 4) AS INTEGER) < 1900 OR
    CAST(SUBSTR(EndDate, 1, 4) AS INTEGER) > 2100
);

-- Fix DateLastMediaAdded
UPDATE BaseItems SET DateLastMediaAdded = NULL
WHERE DateLastMediaAdded IS NOT NULL AND (
    CAST(SUBSTR(DateLastMediaAdded, 6, 2) AS INTEGER) < 1 OR
    CAST(SUBSTR(DateLastMediaAdded, 6, 2) AS INTEGER) > 12 OR
    CAST(SUBSTR(DateLastMediaAdded, 9, 2) AS INTEGER) < 1 OR
    CAST(SUBSTR(DateLastMediaAdded, 9, 2) AS INTEGER) > 31 OR
    CAST(SUBSTR(DateLastMediaAdded, 1, 4) AS INTEGER) < 1900 OR
    CAST(SUBSTR(DateLastMediaAdded, 1, 4) AS INTEGER) > 2100
);

-- Fix DateLastRefreshed
UPDATE BaseItems SET DateLastRefreshed = NULL
WHERE DateLastRefreshed IS NOT NULL AND (
    CAST(SUBSTR(DateLastRefreshed, 6, 2) AS INTEGER) < 1 OR
    CAST(SUBSTR(DateLastRefreshed, 6, 2) AS INTEGER) > 12 OR
    CAST(SUBSTR(DateLastRefreshed, 9, 2) AS INTEGER) < 1 OR
    CAST(SUBSTR(DateLastRefreshed, 9, 2) AS INTEGER) > 31 OR
    CAST(SUBSTR(DateLastRefreshed, 1, 4) AS INTEGER) < 1900 OR
    CAST(SUBSTR(DateLastRefreshed, 1, 4) AS INTEGER) > 2100
);

-- Fix DateLastSaved
UPDATE BaseItems SET DateLastSaved = NULL
WHERE DateLastSaved IS NOT NULL AND (
    CAST(SUBSTR(DateLastSaved, 6, 2) AS INTEGER) < 1 OR
    CAST(SUBSTR(DateLastSaved, 6, 2) AS INTEGER) > 12 OR
    CAST(SUBSTR(DateLastSaved, 9, 2) AS INTEGER) < 1 OR
    CAST(SUBSTR(DateLastSaved, 9, 2) AS INTEGER) > 31 OR
    CAST(SUBSTR(DateLastSaved, 1, 4) AS INTEGER) < 1900 OR
    CAST(SUBSTR(DateLastSaved, 1, 4) AS INTEGER) > 2100
);
```

## After Fixing

1. Restart Jellyfin
2. Run a library scan to repopulate the corrected datetime values
3. Verify the `/Items/Latest` endpoint works correctly
