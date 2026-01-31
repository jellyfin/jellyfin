using System;

namespace Emby.Server.Implementations.Library;

/// <summary>
/// Record that holds the cached ignore rules of a .ignore file.
/// </summary>
public record DotIgnoreFile(string Path, DateTime ChangedDate, Ignore.Ignore? IgnoreRules);
