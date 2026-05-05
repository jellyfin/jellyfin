using System;
using Microsoft.Extensions.Options;

namespace MediaBrowser.Common.Configuration;

/// <summary>
/// Extends <see cref="IOptions{TOptions}"/> with the ability to persist changes back to the
/// underlying JSON configuration file at runtime.
/// </summary>
/// <typeparam name="T">The options type.</typeparam>
public interface IWritableOptions<out T> : IOptions<T>
    where T : class, new()
{
    /// <summary>
    /// Applies <paramref name="applyChanges"/> to the current options value, persists the
    /// result to disk, and triggers an <see cref="IOptionsMonitor{TOptions}"/> change
    /// notification so that all consumers receive the updated value without a restart.
    /// </summary>
    /// <param name="applyChanges">A delegate that mutates the current options snapshot.</param>
    void Update(Action<T> applyChanges);
}
