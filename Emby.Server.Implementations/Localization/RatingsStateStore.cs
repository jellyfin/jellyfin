using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;

namespace Emby.Server.Implementations.Localization;

/// <summary>
/// Computes the current <see cref="RatingsState"/> and keeps the one the stored parental rating scores were last computed for.
/// </summary>
public class RatingsStateStore
{
    private readonly IServerConfigurationManager _configurationManager;
    private readonly ILibraryManager _libraryManager;
    private readonly Func<string> _ratingsDataVersion;

    /// <summary>
    /// Initializes a new instance of the <see cref="RatingsStateStore"/> class.
    /// </summary>
    /// <param name="configurationManager">The server configuration manager.</param>
    /// <param name="libraryManager">The library manager.</param>
    public RatingsStateStore(
        IServerConfigurationManager configurationManager,
        ILibraryManager libraryManager)
        : this(configurationManager, libraryManager, () => LocalizationManager.RatingsDataVersion)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RatingsStateStore"/> class with a custom source of the ratings data version.
    /// </summary>
    /// <param name="configurationManager">The server configuration manager.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="ratingsDataVersion">Provides the current ratings data version.</param>
    internal RatingsStateStore(
        IServerConfigurationManager configurationManager,
        ILibraryManager libraryManager,
        Func<string> ratingsDataVersion)
    {
        _configurationManager = configurationManager;
        _libraryManager = libraryManager;
        _ratingsDataVersion = ratingsDataVersion;
    }

    /// <summary>
    /// Computes the state of the rating data and the metadata countries as they are right now.
    /// </summary>
    /// <returns>The current <see cref="RatingsState"/>.</returns>
    public RatingsState ComputeCurrent()
        => new(_ratingsDataVersion(), ComputeCountryFingerprint());

    /// <summary>
    /// Gets the state the stored scores were last computed for.
    /// </summary>
    /// <returns>The stored <see cref="RatingsState"/>, with empty members when no recompute completed yet.</returns>
    public RatingsState GetStored()
    {
        var data = _configurationManager.GetConfiguration<RatingsStateData>(RatingsStateConfigurationFactory.Key);
        return new RatingsState(data.RatingsDataVersion, data.CountryFingerprint);
    }

    /// <summary>
    /// Stores the state the stored scores were computed for.
    /// </summary>
    /// <param name="state">The state to store.</param>
    public void Save(RatingsState state)
    {
        _configurationManager.SaveConfiguration(
            RatingsStateConfigurationFactory.Key,
            new RatingsStateData
            {
                RatingsDataVersion = state.RatingsDataVersion,
                CountryFingerprint = state.CountryFingerprint
            });
    }

    /// <summary>
    /// Determines whether the stored scores need to be recomputed.
    /// </summary>
    /// <returns><c>true</c> if nothing was stored yet or the stored state differs from the current one.</returns>
    public bool IsRecomputeNeeded()
    {
        // An empty stored state, as on a first start, never equals the hashes of the current one.
        return GetStored() != ComputeCurrent();
    }

    // Only the inputs that decide which country a rating is looked up in count: the server country and the
    // libraries that set a country of their own. A library without one follows the server country, so adding
    // or removing it cannot change any score.
    private string ComputeCountryFingerprint()
    {
        var builder = new StringBuilder();
        builder.Append("server=").Append(Normalize(_configurationManager.Configuration.MetadataCountryCode));

        var libraries = _libraryManager.GetVirtualFolders()
            .Select(f => (Key: f.ItemId ?? f.Name ?? string.Empty, Country: Normalize(f.LibraryOptions?.MetadataCountryCode)))
            .Where(f => f.Country.Length > 0)
            .OrderBy(f => f.Key, StringComparer.Ordinal);

        foreach (var (key, country) in libraries)
        {
            builder.Append('\n').Append(key).Append('=').Append(country);
        }

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    private static string Normalize(string? countryCode)
        => countryCode?.Trim().ToUpper(CultureInfo.InvariantCulture) ?? string.Empty;
}
