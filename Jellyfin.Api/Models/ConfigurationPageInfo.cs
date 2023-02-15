using System;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Api.Models;

/// <summary>
/// The configuration page info.
/// </summary>
public class ConfigurationPageInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationPageInfo"/> class.
    /// </summary>
    /// <param name="plugin">Instance of <see cref="IPlugin"/> interface.</param>
    /// <param name="page">Instance of <see cref="PluginPageInfo"/> interface.</param>
    public ConfigurationPageInfo(IPlugin? plugin, PluginPageInfo page)
    {
        Name = page.Name;
        EnableInMainMenu = page.EnableInMainMenu;
        MenuSection = page.MenuSection;
        MenuIcon = page.MenuIcon;
        DisplayName = string.IsNullOrWhiteSpace(page.DisplayName) ? plugin?.Name : page.DisplayName;
        PluginId = plugin?.Id;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationPageInfo"/> class.
    /// </summary>
    public ConfigurationPageInfo()
    {
        Name = string.Empty;
    }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    /// <value>The name.</value>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the configurations page is enabled in the main menu.
    /// </summary>
    public bool EnableInMainMenu { get; set; }

    /// <summary>
    /// Gets or sets the menu section.
    /// </summary>
    public string? MenuSection { get; set; }

    /// <summary>
    /// Gets or sets the menu icon.
    /// </summary>
    public string? MenuIcon { get; set; }

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the plugin id.
    /// </summary>
    /// <value>The plugin id.</value>
    public Guid? PluginId { get; set; }
}
