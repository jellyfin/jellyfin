namespace MediaBrowser.Model.Configuration;

/// <summary>
/// Open Telemetry Configuration.
/// </summary>
public class OtelConfiguration
{
    /// <summary>
    /// Gets or Sets a value indicating whether to enable otlp.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or Sets the endpoint to exporter to.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the meters to listen to.
    /// </summary>
    public string Meters { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the activity sources to listen to.
    /// </summary>
    public string ActivitySources { get; set; } = string.Empty;
}
