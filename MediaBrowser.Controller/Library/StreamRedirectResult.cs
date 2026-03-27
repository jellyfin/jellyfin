namespace MediaBrowser.Controller.Library;

/// <summary>
/// The result returned by an <see cref="IStreamRedirectProvider"/>.
/// </summary>
/// <param name="RedirectUrl">The URL to redirect the client to.</param>
/// <param name="PingForwardUrl">
/// Optional URL to forward play-session heartbeat pings to.
/// Required when the redirect target runs a transcoding session that needs
/// liveness signals to stay alive. When set, the consumer server will forward
/// <c>POST /Sessions/Playing/Ping</c> calls to this URL for the duration of
/// the play session.
/// </param>
public record StreamRedirectResult(string RedirectUrl, string? PingForwardUrl = null);
