; Shipped analyzer releases
; https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 1.0

### New Rules
Rule ID | Category | Severity | Notes
--------|----------|----------|-------
JF0001  | Usage    | Warning  | Async-created IAsyncDisposable objects should use 'await using'

## Release 1.1

### New Rules
Rule ID | Category    | Severity | Notes
--------|-------------|----------|-------
JF0002  | Portability | Error    | Raw SQL and checks for one database engine belong in the database providers
JF0003  | Portability | Error    | An entry of the database ban names no member of its type
