using System;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.ServerSetupApp;

/// <summary>
/// Defines a topic for the Startup UI.
/// </summary>
public class StartupLogTopic
{
    /// <summary>
    /// Gets or Sets the LogLevel.
    /// </summary>
    public LogLevel LogLevel { get; set; }

    /// <summary>
    /// Gets or Sets the descriptor for the topic.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Gets or sets the time the topic was created.
    /// </summary>
    public DateTimeOffset DateOfCreation { get; set; }

    /// <summary>
    /// Gets the child items of this topic.
    /// </summary>
    public Collection<StartupLogTopic> Children { get; } = [];
}
