# Jellyfin Cursor Rules

This directory contains Cursor AI rules that provide context and coding standards for the Jellyfin project. These rules help AI assistants understand the codebase structure, conventions, and best practices.

**Note:** These rules are based on the actual codebase configuration (`.editorconfig`, `stylecop.json`, `Directory.Build.props`) and official [Jellyfin contribution guidelines](https://jellyfin.org/docs/general/contributing/development).

## Rule Files

### Always Applied

- **`project-overview.mdc`** - Core project information, tech stack, architecture overview, and contribution workflow

### Context-Specific Rules

Applied automatically when working with specific file types:

- **`csharp-standards.mdc`** - C# coding standards, naming conventions, and formatting rules
  - Applies to: `**/*.cs`

- **`controller-patterns.mdc`** - API controller patterns and conventions
  - Applies to: `**/Controllers/**/*.cs`

- **`testing-patterns.mdc`** - Test structure and patterns
  - Applies to: `tests/**/*.cs`

- **`database-conventions.mdc`** - Entity Framework and database patterns
  - Applies to: `src/Jellyfin.Database/**/*.cs`

- **`dependency-injection.mdc`** - DI patterns and service registration
  - Applies to: `**/*.cs`

- **`logging-conventions.mdc`** - Serilog logging patterns
  - Applies to: `**/*.cs`

- **`error-handling.mdc`** - Exception handling and error patterns
  - Applies to: `**/*.cs`

## Key Conventions Summary

### Naming
- Instance fields: `_camelCase`
- Static fields: `_camelCase`
- Constants: `PascalCase`
- Parameters/locals: `camelCase`
- Public members: `PascalCase`

### Code Style
- File-scoped namespaces (C# 10+)
- Nullable reference types enabled
- 4 spaces indentation
- LF line endings
- Allman brace style

### Critical Rules
- ❌ Never use `Task.Result` (use `await`)
- ❌ Never use `Guid` equality operators (use `.Equals()`)
- ✅ Always specify `StringComparison`
- ✅ Always pass `CancellationToken` to async methods
- ✅ Warnings are treated as errors

### Architecture
- Interfaces in `MediaBrowser.Controller`
- Implementations in `Emby.Server.Implementations` or `src/Jellyfin.*`
- API controllers in `Jellyfin.Api/Controllers`
- Use dependency injection for all services
- Repository pattern for data access

## Development Workflow

1. **Fork and Clone**: Fork the repository, clone it locally, add upstream remote
2. **Create Feature Branch**: `git checkout -b my-feature master`
3. **Make Changes**: Implement your changes following the coding standards
4. **Build**: `dotnet build` (warnings are treated as errors)
5. **Test**: `dotnet test` to run all tests
6. **Rebase**: Rebase against upstream/master before submitting
7. **Submit PR**: Push to your fork and create PR against upstream `master`
8. **Add Yourself**: First-time contributors add yourself to `CONTRIBUTORS.md`

### Running the Server

```bash
dotnet run --project Jellyfin.Server --webdir /path/to/jellyfin-web/dist
```

Or set the `JELLYFIN_WEB_DIR` environment variable to the web client dist folder.

## PR Guidelines

- Use imperative mood in titles (e.g., "Add feature", not "Added feature")
- Keep titles short, descriptive, with proper capitalization, no punctuation
- Reference issues with `fixes`, `closes`, or `addresses` keywords
- Mark unfinished PRs as "draft"
- Squash junk commits but keep logical commits separate
- Requires 2 team member reviews before merging

## Additional Resources

- [Jellyfin Documentation](https://jellyfin.org/docs/)
- [Contributing Guide](https://jellyfin.org/docs/general/contributing/development)
- [How to Write a Git Commit Message](https://chris.beams.io/posts/git-commit/)
- Target framework: `.NET 10.0` (net10.0)
- API docs when running: `http://localhost:8096/api-docs/swagger/index.html`
