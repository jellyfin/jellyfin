# Phase 1: Core Infrastructure Implementation

## Overview

Phase 1 focuses on fixing the fundamental issues in the PostgreSQL provider that prevent it from functioning. This phase addresses the 4 `NotImplementedException` methods and establishes the core infrastructure needed for all subsequent phases.

**Priority**: CRITICAL
**Estimated Effort**: 40-60 hours
**Duration**: 2 weeks
**Dependencies**: None

## Objectives

1. Fix connection string builder implementation
2. Implement backup/restore functionality using pg_dump/pg_restore
3. Implement database purge functionality
4. Add comprehensive error handling and logging
5. Create unit tests for all provider methods

## Current Issues

### 1. Missing NpgsqlConnectionStringBuilder Import
**File**: `PostgresDatabaseProvider.cs`
**Issue**: The code references `NpgsqlConnectionStringBuilder` but doesn't import it
**Impact**: Build failure or runtime error

### 2. NotImplementedException - MigrationBackupFast()
**Method**: `Task<string> MigrationBackupFast(CancellationToken cancellationToken)`
**Required**: Implement PostgreSQL database backup using pg_dump
**Return**: Unique backup identifier (key)

### 3. NotImplementedException - RestoreBackupFast()
**Method**: `Task RestoreBackupFast(string key, CancellationToken cancellationToken)`
**Required**: Restore PostgreSQL database from backup using pg_restore
**Input**: Backup key from MigrationBackupFast()

### 4. NotImplementedException - DeleteBackup()
**Method**: `Task DeleteBackup(string key)`
**Required**: Clean up backup files
**Input**: Backup key to delete

### 5. NotImplementedException - PurgeDatabase()
**Method**: `Task PurgeDatabase(JellyfinDbContext dbContext, IEnumerable<string>? tableNames)`
**Required**: Clear specified tables (or all tables) from database
**Note**: Must handle PostgreSQL syntax differences from SQLite

## Implementation Strategy

### Connection String Builder Fix

**Priority**: HIGH
**Complexity**: LOW
**Effort**: 1-2 hours

The current code has a compilation issue with `NpgsqlConnectionStringBuilder`. Need to:

1. Add proper using statement: `using Npgsql;`
2. Verify connection string properties are correct
3. Test with various connection configurations
4. Handle SSL/TLS options properly

**See**: `connection-string-builder.md` for detailed implementation

---

### Backup/Restore Implementation

**Priority**: CRITICAL
**Complexity**: HIGH
**Effort**: 20-30 hours

PostgreSQL backup/restore requires external tool integration:

- **pg_dump**: Command-line tool to export database
- **pg_restore**: Command-line tool to import database
- **Process execution**: Need to run external processes from .NET
- **Error handling**: Capture and handle pg_dump/pg_restore errors
- **Connection management**: Ensure no active connections during restore

**Challenges**:
1. Finding pg_dump/pg_restore executable paths
2. Building correct command-line arguments
3. Handling large databases (streaming vs. file-based)
4. Cross-platform compatibility (Windows, Linux, macOS)
5. Permission issues

**See**: `backup-restore-implementation.md` for detailed implementation

---

### Database Purge Implementation

**Priority**: HIGH
**Complexity**: MEDIUM
**Effort**: 8-12 hours

PostgreSQL syntax differs from SQLite for table operations:

- **TRUNCATE vs DELETE**: PostgreSQL prefers TRUNCATE for performance
- **CASCADE handling**: Must handle foreign key constraints
- **Transaction management**: Ensure atomic operations
- **Schema awareness**: PostgreSQL has schemas (public by default)

**Challenges**:
1. Foreign key constraint ordering
2. Performance with large tables
3. Transaction rollback on failure
4. Sequence reset after truncate

**See**: `purge-database-implementation.md` for detailed implementation

---

### Error Handling & Logging

**Priority**: HIGH
**Complexity**: MEDIUM
**Effort**: 8-12 hours

Comprehensive error handling for:

- Connection failures
- Permission errors
- Backup/restore failures
- SQL execution errors
- Configuration errors

**Requirements**:
1. Structured logging with context
2. User-friendly error messages
3. Debug-level detailed logging
4. Performance metrics logging
5. Error recovery strategies

**See**: `error-handling-logging.md` for detailed implementation

---

## File Structure

```
phase-1-infrastructure/
├── README.md (this file)
├── connection-string-builder.md
├── backup-restore-implementation.md
├── purge-database-implementation.md
├── error-handling-logging.md
└── checklist.md
```

## Success Criteria

### Functional Requirements
- ✅ PostgresDatabaseProvider compiles without errors
- ✅ Connection string builder works with all configuration options
- ✅ Backup creates valid PostgreSQL dump files
- ✅ Restore successfully restores from backup
- ✅ Delete backup removes backup files
- ✅ Purge database clears specified tables
- ✅ All methods have proper error handling

### Quality Requirements
- ✅ Unit tests for all public methods
- ✅ Integration tests with real PostgreSQL
- ✅ Code coverage > 80%
- ✅ No unhandled exceptions
- ✅ Proper logging at all levels

### Performance Requirements
- ✅ Backup completes in < 10 minutes for 100K items
- ✅ Restore completes in < 15 minutes for 100K items
- ✅ Purge completes in < 5 minutes for all tables
- ✅ Connection establishment < 2 seconds

## Testing Strategy

### Unit Tests
1. Connection string building
2. Backup key generation
3. Error handling paths
4. Configuration parsing
5. Mock pg_dump/pg_restore execution

### Integration Tests
1. Backup with real PostgreSQL
2. Restore from backup
3. Purge operations
4. Connection failures
5. Permission errors

### Manual Tests
1. Cross-platform testing (Windows, Linux, macOS)
2. Large database scenarios
3. Network failure scenarios
4. Concurrent operation testing

## Dependencies

### NuGet Packages (Already Referenced)
- `Npgsql.EntityFrameworkCore.PostgreSQL` - PostgreSQL EF Core provider
- `Microsoft.EntityFrameworkCore.Relational` - EF Core base
- `Microsoft.Extensions.Logging` - Logging abstractions

### System Requirements
- PostgreSQL 12+ installed
- pg_dump and pg_restore accessible in PATH
- Write permissions to backup directory
- PostgreSQL connection with backup privileges

## Risks & Mitigation

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| pg_dump not in PATH | HIGH | MEDIUM | Implement pg_dump discovery logic |
| Permission errors | HIGH | MEDIUM | Clear error messages, documentation |
| Large backup files | MEDIUM | HIGH | Implement compression, cleanup |
| Restore conflicts | HIGH | LOW | Pre-restore validation, rollback |
| Cross-platform issues | MEDIUM | MEDIUM | Extensive platform testing |

## Next Steps

1. **Review all Phase 1 documents**
2. **Set up development environment**:
   - Install PostgreSQL 14+
   - Configure test database
   - Install pgAdmin or similar tool
3. **Start with connection string builder** (quickest win)
4. **Implement backup/restore** (most complex)
5. **Add purge functionality**
6. **Implement error handling**
7. **Write tests**
8. **Run integration tests**
9. **Complete checklist**

## Timeline

| Task | Days | Owner |
|------|------|-------|
| Connection string fix | 0.5 | TBD |
| Backup implementation | 3-4 | TBD |
| Restore implementation | 2-3 | TBD |
| Delete backup | 0.5 | TBD |
| Purge database | 1-2 | TBD |
| Error handling | 1-2 | TBD |
| Unit tests | 2-3 | TBD |
| Integration tests | 1-2 | TBD |
| **TOTAL** | **11-17 days** | |

## Questions?

- Check specific implementation docs for detailed guidance
- Review SQLite provider for reference implementation patterns
- Consult PostgreSQL documentation for backup/restore details
- Test frequently with real PostgreSQL instance

---

**Status**: Ready to Begin
**Next**: Review `connection-string-builder.md`
