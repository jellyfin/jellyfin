# PostgreSQL Database Provider Implementation - Master Plan

## Project Overview

This document outlines the complete implementation plan for a **fully-featured PostgreSQL database provider** for Jellyfin. The current implementation is incomplete with several critical gaps that must be addressed.

## Current State Assessment

### ✅ Completed
- Basic PostgreSQL provider class structure
- NpgsqlConnectionInterceptor skeleton
- Project configuration and NuGet packages
- Interface implementation (IJellyfinDatabaseProvider)

### ❌ Critical Gaps
1. **NO MIGRATIONS** - Zero PostgreSQL migration files (SQLite has 30+)
2. **NotImplementedException** in 4 critical methods:
   - MigrationBackupFast()
   - RestoreBackupFast()
   - DeleteBackup()
   - PurgeDatabase()
3. Missing NpgsqlConnectionStringBuilder import
4. No PostgreSQL-specific conventions
5. No maintenance task implementation
6. No testing infrastructure

## Implementation Phases

### Phase 1: Core Infrastructure (Week 1-2)
**Priority**: CRITICAL
**Estimated Effort**: 40-60 hours

Fix fundamental issues preventing the provider from functioning:
- Connection string builder fixes
- Backup/restore implementation using pg_dump/pg_restore
- Database purge implementation
- Error handling and logging

**Deliverables**:
- Functional PostgresDatabaseProvider
- Working backup/restore system
- Comprehensive error handling
- Unit tests for core methods

---

### Phase 2: Migration System (Week 3-5)
**Priority**: CRITICAL
**Estimated Effort**: 80-120 hours

Port all SQLite migrations to PostgreSQL:
- 30+ migration files to convert
- Data type mapping (TEXT→VARCHAR, INTEGER→INT, BLOB→BYTEA)
- Index and constraint adaptations
- Foreign key relationship preservation

**Deliverables**:
- Complete PostgreSQL migration set
- Migration testing framework
- Rollback capability
- Migration generation workflow

---

### Phase 3: PostgreSQL Optimizations (Week 6-7)
**Priority**: HIGH
**Estimated Effort**: 40-60 hours

Implement PostgreSQL-specific features:
- Model conventions and configurations
- VACUUM and ANALYZE scheduling
- Connection pooling optimization
- Performance indexes
- Query optimization

**Deliverables**:
- Optimized database performance
- Maintenance task scheduler
- Connection pool configuration
- Performance benchmarks

---

### Phase 4: Testing & Validation (Week 8-9)
**Priority**: HIGH
**Estimated Effort**: 60-80 hours

Comprehensive testing suite:
- Unit tests for all provider methods
- Integration tests with real PostgreSQL
- Migration testing (fresh + upgrade scenarios)
- Performance benchmarking vs SQLite
- Load testing

**Deliverables**:
- Complete test suite (90%+ coverage)
- CI/CD integration
- Performance comparison reports
- Bug-free provider

---

### Phase 5: Documentation (Week 10)
**Priority**: MEDIUM
**Estimated Effort**: 20-30 hours

User and developer documentation:
- Configuration guide
- Migration guide from SQLite
- Troubleshooting documentation
- Performance tuning guide
- API documentation

**Deliverables**:
- Complete user documentation
- Developer guides
- Troubleshooting playbook
- Migration scripts/tools

---

## Database Schema Overview

### Core Tables (11 major tables)
1. **BaseItems** - 80+ columns, core media library
2. **Users** - User accounts and authentication
3. **Groups** - User groups and permissions
4. **ActivityLog** - System activity tracking
5. **DisplayPreferences** - UI preferences
6. **Devices** - Client device tracking
7. **TrickplayInfo** - Video preview thumbnails
8. **MediaSegments** - Intro/outro detection
9. **KeyframeData** - Video keyframe information
10. **Peoples** - Actors, directors, etc.
11. **ItemValues** - Genres, tags, studios

### Relationship Tables (7 mapping tables)
- UserData (Users ↔ BaseItems)
- PeopleBaseItemMap (Peoples ↔ BaseItems)
- ItemValuesMap (ItemValues ↔ BaseItems)
- AncestorIds (BaseItems hierarchy)
- BaseItemProviders (External IDs)
- Permissions, Preferences (User settings)

### Complex Features
- Cascading deletes
- Composite primary keys
- Full-text search requirements
- JSON/JSONB storage needs
- Large binary data (BLOB→BYTEA)

---

## Technical Considerations

### PostgreSQL vs SQLite Differences

| Aspect | SQLite | PostgreSQL | Impact |
|--------|--------|------------|--------|
| Data Types | TEXT, INTEGER, REAL, BLOB | VARCHAR, INT, NUMERIC, BYTEA | HIGH - All migrations affected |
| Case Sensitivity | Case-insensitive by default | Case-sensitive | MEDIUM - Collation needed |
| Boolean Type | INTEGER (0/1) | BOOLEAN | MEDIUM - Type conversion |
| Auto-increment | AUTOINCREMENT | SERIAL/IDENTITY | LOW - EF Core handles |
| Transactions | Limited concurrency | Full MVCC | LOW - Better performance |
| Full-text Search | FTS5 | tsvector/tsquery | HIGH - Different syntax |

### Migration Challenges

1. **Data Type Mapping**
   - TEXT → VARCHAR(unlimited) or TEXT
   - INTEGER → INT, BIGINT
   - REAL → NUMERIC or DOUBLE PRECISION
   - BLOB → BYTEA

2. **Index Strategy**
   - SQLite uses simple indexes
   - PostgreSQL supports partial, GiST, GIN indexes
   - Need to optimize for PostgreSQL strengths

3. **Collation Handling**
   - SQLite: NOCASE collation
   - PostgreSQL: COLLATE "en-US-x-icu" or similar
   - Critical for text searches

4. **Foreign Key Constraints**
   - SQLite: ON DELETE CASCADE
   - PostgreSQL: Same but with different performance characteristics

---

## Risk Assessment

### High Risk Areas
1. **Data Migration** - Converting existing SQLite databases
2. **Performance** - Ensuring PostgreSQL matches/exceeds SQLite
3. **Backup/Restore** - pg_dump integration complexity
4. **Testing** - Comprehensive testing required for production

### Mitigation Strategies
1. Extensive testing with real-world data
2. Performance benchmarking at each phase
3. Rollback procedures for failed migrations
4. Beta testing period before production release

---

## Success Criteria

### Functional Requirements
- ✅ All IJellyfinDatabaseProvider methods implemented
- ✅ All 30+ migrations ported and tested
- ✅ Backup/restore functionality working
- ✅ Database purge functionality working
- ✅ Fresh installation works
- ✅ SQLite→PostgreSQL migration works

### Performance Requirements
- ✅ Query performance ≥ SQLite performance
- ✅ Startup time < 5 seconds
- ✅ Migration time < 30 minutes (100K items)
- ✅ Backup time < 10 minutes (100K items)

### Quality Requirements
- ✅ 90%+ code coverage
- ✅ Zero critical bugs
- ✅ All integration tests passing
- ✅ Documentation complete

---

## Timeline Summary

| Phase | Duration | Effort | Dependencies |
|-------|----------|--------|--------------|
| Phase 1: Infrastructure | 2 weeks | 40-60h | None |
| Phase 2: Migrations | 3 weeks | 80-120h | Phase 1 |
| Phase 3: Optimization | 2 weeks | 40-60h | Phase 2 |
| Phase 4: Testing | 2 weeks | 60-80h | Phase 3 |
| Phase 5: Documentation | 1 week | 20-30h | Phase 4 |
| **TOTAL** | **10 weeks** | **240-350h** | Sequential |

---

## Getting Started

1. **Review Phase 1 Documentation** - `/context/phase-1-infrastructure/`
2. **Set up PostgreSQL Development Environment**
   - Install PostgreSQL 14+ locally
   - Install pgAdmin or similar tool
   - Configure connection strings
3. **Run Current Tests** - Establish baseline
4. **Begin Phase 1 Implementation** - Follow checklist

---

## Phase Directory Structure

```
/context/
├── MASTER-PLAN.md (this file)
├── phase-1-infrastructure/
│   ├── README.md
│   ├── connection-string-builder.md
│   ├── backup-restore-implementation.md
│   ├── purge-database-implementation.md
│   ├── error-handling-logging.md
│   └── checklist.md
├── phase-2-migrations/
│   ├── README.md
│   ├── data-type-mapping.md
│   ├── migration-porting-guide.md
│   ├── migration-list.md
│   ├── testing-strategy.md
│   └── checklist.md
├── phase-3-optimization/
│   ├── README.md
│   ├── conventions-configuration.md
│   ├── maintenance-tasks.md
│   ├── connection-pooling.md
│   ├── indexing-strategy.md
│   └── checklist.md
├── phase-4-testing/
│   ├── README.md
│   ├── unit-tests.md
│   ├── integration-tests.md
│   ├── migration-tests.md
│   ├── performance-tests.md
│   └── checklist.md
└── phase-5-documentation/
    ├── README.md
    ├── configuration-guide.md
    ├── migration-from-sqlite.md
    ├── troubleshooting.md
    ├── performance-tuning.md
    └── checklist.md
```

---

## Questions & Support

For questions during implementation:
1. Refer to phase-specific README files
2. Check Jellyfin documentation at https://jellyfin.org/docs
3. Review PostgreSQL documentation at https://www.postgresql.org/docs/
4. Consult Entity Framework Core documentation for migration specifics

---

**Last Updated**: November 2, 2025
**Status**: Ready to Begin Phase 1
**Next Action**: Review Phase 1 Infrastructure documentation
