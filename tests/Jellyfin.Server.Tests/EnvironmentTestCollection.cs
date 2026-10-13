using System;

namespace Jellyfin.Server.Tests;

/// <summary>
/// Names the xunit collection that serialises the tests changing process wide environment variables.
/// </summary>
internal static class EnvironmentTestCollection
{
    public const string Name = "Process environment variables";
}
