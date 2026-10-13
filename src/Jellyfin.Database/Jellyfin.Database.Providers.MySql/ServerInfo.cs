using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Jellyfin.Database.Providers.MySql;

/// <summary>
/// The two kinds of server this provider talks to.
/// </summary>
internal enum ServerFlavor
{
    /// <summary>
    /// Oracle MySQL, or a server that reports itself as such.
    /// </summary>
    MySql,

    /// <summary>
    /// MariaDB.
    /// </summary>
    MariaDb
}

/// <summary>
/// The kind and the version of a database server, and the oldest version of each kind that is supported.
/// </summary>
/// <remarks>
/// MySQL 8.4 is the oldest long-term release that is still maintained. MariaDB 11.4.7 is the first 11.4 release that
/// knows the <c>utf8mb4_0900_bin</c> collation, which every text column uses, and the one that makes it compare
/// without padding as MySQL does.
/// </remarks>
internal sealed partial class ServerInfo
{
    private static readonly Version _mySqlFloor = new(8, 4);
    private static readonly Version _mariaDbFloor = new(11, 4, 7);

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerInfo"/> class.
    /// </summary>
    /// <param name="flavor">The kind of the server.</param>
    /// <param name="version">The version of the server.</param>
    public ServerInfo(ServerFlavor flavor, Version version)
    {
        Flavor = flavor;
        Version = version;
    }

    /// <summary>
    /// Gets the kind of the server.
    /// </summary>
    public ServerFlavor Flavor { get; }

    /// <summary>
    /// Gets the version of the server. A version without a patch level has a <see cref="Version.Build"/> of -1.
    /// </summary>
    public Version Version { get; }

    /// <summary>
    /// Gets a value indicating whether the version is at least the oldest supported one of its kind. A version without a
    /// patch level gets the benefit of the doubt, and is checked again once a connection reports the exact version.
    /// </summary>
    public bool MeetsFloor
    {
        get
        {
            var floor = Flavor == ServerFlavor.MariaDb ? _mariaDbFloor : _mySqlFloor;
            var patch = Version.Build < 0 ? int.MaxValue : Version.Build;
            return (Version.Major, Version.Minor, patch).CompareTo((floor.Major, floor.Minor, Math.Max(floor.Build, 0))) >= 0;
        }
    }

    /// <summary>
    /// Gets the text that says why the server is too old, and which version is needed.
    /// </summary>
    public string UnsupportedMessage => Flavor == ServerFlavor.MariaDb
        ? string.Create(
            CultureInfo.InvariantCulture,
            $"MariaDB {_mariaDbFloor} or later is required, but the server is version {Version}. 11.4.7 is the first 11.4 release with the utf8mb4_0900_bin collation that Jellyfin uses.")
        : string.Create(
            CultureInfo.InvariantCulture,
            $"MySQL {_mySqlFloor} or later is required, but the server is version {Version}.");

    /// <summary>
    /// Builds the text that says that the version a server reported could not be read, and how to name the server instead.
    /// </summary>
    /// <param name="reported">The version string of the connection.</param>
    /// <returns>The message.</returns>
    public static string UnreadableMessage(string? reported)
    {
        return $"The version that the database server reported, '{reported}', could not be read, so it cannot be checked "
            + "against the oldest supported version. Name the server in the server-version option of database.xml, "
            + "for example 8.4-mysql or 11.4-mariadb.";
    }

    /// <summary>
    /// Reads the version string a server reports on connecting, for example <c>8.4.11</c> or <c>11.4.13-MariaDB-ubu2404</c>.
    /// </summary>
    /// <param name="reported">The version string of the connection.</param>
    /// <param name="info">The server, when the string could be read.</param>
    /// <returns><c>true</c> if the string could be read; otherwise, <c>false</c>.</returns>
    public static bool TryParseReported(string? reported, out ServerInfo? info)
    {
        info = null;
        if (reported is null || ReportedPattern().Match(reported) is not { Success: true } match
            || !Version.TryParse(match.Groups["version"].Value, out var version))
        {
            return false;
        }

        var flavor = match.Groups["suffix"].Value.Contains("mariadb", StringComparison.OrdinalIgnoreCase)
            ? ServerFlavor.MariaDb
            : ServerFlavor.MySql;
        info = new ServerInfo(flavor, version);
        return true;
    }

    /// <summary>
    /// Reads a version as it is written in <c>database.xml</c>, a version followed by <c>-mysql</c> or <c>-mariadb</c>,
    /// for example <c>8.4-mysql</c> or <c>11.4.13-mariadb</c>.
    /// </summary>
    /// <param name="configured">The configured text.</param>
    /// <param name="info">The server, when the text could be read.</param>
    /// <returns><c>true</c> if the text could be read; otherwise, <c>false</c>.</returns>
    public static bool TryParseConfigured(string? configured, out ServerInfo? info)
    {
        info = null;
        if (configured is null || ConfiguredPattern().Match(configured.Trim()) is not { Success: true } match
            || !Version.TryParse(match.Groups["version"].Value, out var version))
        {
            return false;
        }

        var flavor = match.Groups["flavor"].Value.Equals("mariadb", StringComparison.OrdinalIgnoreCase)
            ? ServerFlavor.MariaDb
            : ServerFlavor.MySql;
        info = new ServerInfo(flavor, version);
        return true;
    }

    // MariaDB servers before 11.0 report a replication compatibility prefix.
    [GeneratedRegex(@"^(?:5\.5\.5-)?(?<version>\d+\.\d+(?:\.\d+)?)(?<suffix>.*)$", RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex ReportedPattern();

    [GeneratedRegex(@"^(?<version>\d+\.\d+(?:\.\d+)?)-(?<flavor>mysql|mariadb)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex ConfiguredPattern();
}
