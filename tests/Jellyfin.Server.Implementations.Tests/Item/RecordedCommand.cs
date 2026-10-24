namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// A command captured by <see cref="QueryPlanRecorder"/>.
/// </summary>
/// <param name="Sql">The command text.</param>
/// <param name="Parameters">The bound parameters.</param>
public sealed record RecordedCommand(string Sql, (string Name, object? Value)[] Parameters);
