# Rules Verification Against Jellyfin Contribution Guidelines

This document verifies that the Cursor rules align with official Jellyfin contribution guidelines and codebase standards.

## Sources Verified

✅ **Official Contribution Guidelines**
- https://jellyfin.org/docs/general/contributing/development
- Fork, feature-branch, and PR workflow documented
- CONTRIBUTORS.md requirement included
- PR guidelines and review process covered

✅ **Codebase Configuration Files**
- `.editorconfig` - Code formatting and naming conventions
- `stylecop.json` - StyleCop analyzer settings
- `Directory.Build.props` - Build configuration (warnings as errors, analyzers)
- `Directory.Packages.props` - Package version management
- `BannedSymbols.txt` - Forbidden APIs

✅ **Actual Project Files**
- `.csproj` files - Target framework (net10.0)
- Source code examples - Naming, namespace, and pattern verification
- Test structure - xUnit patterns and conventions

## Key Verification Points

### ✅ Project Information
- **Framework**: .NET 10.0 (net10.0) - Verified from `.csproj` files
- **Tech Stack**: ASP.NET Core, EF Core, Serilog, Prometheus - Correct
- **Architecture**: DI-based, interface abstractions in MediaBrowser.Controller - Correct

### ✅ Naming Conventions (.editorconfig lines 87-106)
- **Instance fields**: `_camelCase` ✅
- **Static fields**: `_camelCase` ✅  
- **Constants**: `PascalCase` ✅
- **Parameters/locals**: `camelCase` ✅
- **Public members**: `PascalCase` ✅

### ✅ Code Style (.editorconfig)
- **Indentation**: 4 spaces ✅ (line 11)
- **Line endings**: LF ✅ (line 15)
- **Charset**: UTF-8 ✅ (line 12)
- **Trailing whitespace**: Remove ✅ (line 13)
- **Final newline**: Required ✅ (line 14)
- **Brace style**: Allman (new line) ✅ (line 169)

### ✅ C# Features
- **Nullable reference types**: Enabled project-wide ✅ (Directory.Build.props line 5)
- **File-scoped namespaces**: Used throughout codebase ✅
- **var keyword**: Preferred for built-in types ✅ (.editorconfig line 140-142)

### ✅ Banned Symbols (BannedSymbols.txt)
- ❌ `Task.Result` - Documented ✅
- ❌ `Guid.op_Equality` - Documented ✅
- ❌ `Guid.op_Inequality` - Documented ✅
- ❌ `Guid.Equals(object)` - Documented ✅

### ✅ Analyzer Rules (.editorconfig lines 199-543)
- String comparison requirements (CA1307, CA1309, CA1310) - Documented ✅
- Async/await patterns (CA1849) - Documented ✅
- Disposal patterns (CA1001, CA1063, IDISP*) - Documented ✅
- CancellationToken forwarding (CA2016) - Documented ✅
- Warnings as errors - Documented ✅

### ✅ Testing Patterns
- **Framework**: xUnit - Correct ✅
- **Mocking**: Moq - Correct ✅
- **Fixture**: AutoFixture - Correct ✅
- **Naming**: `MethodName_Scenario_ExpectedBehavior` - Verified from actual tests ✅
- **Integration tests**: `JellyfinApplicationFactory` pattern - Verified ✅

### ✅ Contribution Workflow
- Fork and feature branch workflow - Documented ✅
- Target branch: `master` - Correct ✅
- PR review requirement: 2 team members - Documented ✅
- Imperative mood in commits - Documented ✅
- CONTRIBUTORS.md requirement - Documented ✅
- Issue referencing with keywords - Documented ✅

### ✅ Architecture Patterns
- **Interfaces**: MediaBrowser.Controller - Correct ✅
- **Implementations**: Emby.Server.Implementations / src/Jellyfin.* - Correct ✅
- **API Controllers**: Jellyfin.Api/Controllers - Correct ✅
- **Base controller**: BaseJellyfinApiController - Verified ✅
- **DI patterns**: Constructor injection - Standard throughout ✅

### ✅ Database Conventions
- **ORM**: Entity Framework Core - Correct ✅
- **Provider**: SQLite - Correct ✅
- **Pattern**: Repository pattern - Verified ✅
- **Migrations**: Timestamped format in src/Jellyfin.Database/.../Migrations/ - Verified ✅

### ✅ Logging Conventions
- **Framework**: Serilog - Correct ✅
- **Injection**: `ILogger<T>` - Standard pattern ✅
- **Structured logging**: Named placeholders required - Per best practices ✅
- **No sensitive data**: Documented ✅

## Discrepancies or Notes

### Documentation vs. Codebase
- **README states .NET 9 SDK**, but actual `.csproj` files use `net10.0`
  - **Resolution**: Rules reflect actual codebase (net10.0) with note about README
  - **Reasoning**: Codebase is authoritative; documentation may lag

### Style Guide Coverage
- No C#-specific style guide published on jellyfin.org (only JavaScript exists)
- **Resolution**: Rules derived directly from `.editorconfig`, `stylecop.json`, and analyzer configuration
- **Reasoning**: These configuration files are authoritative and enforced at build time

## Conclusion

✅ **All rules verified and aligned with:**
1. Official contribution guidelines
2. Enforced analyzer rules
3. Project configuration files
4. Actual codebase patterns

The rules accurately represent Jellyfin's coding standards and contribution process as of January 2026.
