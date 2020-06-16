using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using Microsoft.Extensions.Logging;

namespace Common.Networking
{
    /// <summary>
    /// States of access to the internet.
    /// </summary>
    public enum InternetState
    {
        /// <summary>
        /// Internet access is up.
        /// </summary>
        Up = -1,

        /// <summary>
        /// Internet access is down.
        /// </summary>
        Down = 0,

        /// <summary>
        /// Internet access hasn't been checked yet.
        /// </summary>
        Unchecked = 1,

        /// <summary>
        /// Internet access errored.
        /// </summary>
        Error = 2,

        /// <summary>
        /// Redirect didn't work.
        /// </summary>
        RedirectFailure = 3,
    }

    /// <summary>
    /// Singleton that periodically checks the state of the internet.
    /// </summary>
    public class InternetChecker : IDisposable
    {
        /// <summary>
        /// Singleton accessor.
        /// </summary>
#pragma warning disable SA1401 // Fields should be private : Singleton Implementation.
        public static readonly InternetChecker Instance = new InternetChecker();
#pragma warning restore SA1401 // Fields should be private

        /// <summary>
        /// Timer object.
        /// </summary>
        private Timer _timer = null!;

        /// <summary>
        /// How long to wait between failures in ms.
        /// </summary>
        private int _retryDelay = 1000;

        /// <summary>
        /// How many times to try on failure.
        /// </summary>
        private int _retryCount = 3;

        /// <summary>
        /// Frequency of the check.
        /// </summary>
        private int _every = -1;

        /// <summary>
        /// External ip address resolver site.
        /// The external IP address is deemed to be the first valid IP in the response.
        /// </summary>
        private string _externalResolver = string.Empty;

        /// <summary>
        /// External redirector site.
        /// Provide a site which retreives data from an address passed to it as a parameter, and returns the content.
        /// It's up to the user to provide the site.
        /// eg. https://myjellyfintester.sitea.com?check= .
        /// </summary>
        private string _externalRedirector = string.Empty;

        /// <summary>
        /// External address of Jellyfin server. eg. http://myjellyfin.com/
        /// If no DNS is defined then https://[ip]/ can be used for dynamic allocations.
        /// </summary>
        private string _externalAddress = string.Empty;

        /// <summary>
        /// Set if disposed.
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// Logger.
        /// </summary>
        private ILogger<InternetChecker> _logger = null!;

        /// <summary>
        /// Required for access to configuration.
        /// </summary>
        private IServerConfigurationManager _config = null!;

        /// <summary>
        /// Initializes a new instance of the <see cref="InternetChecker"/> class.
        /// Constructor for InternetChecker.
        /// </summary>
        public InternetChecker()
        {
        }

        /// <summary>
        /// Event that gets called every time the state changes.
        /// </summary>
        public event EventHandler<InternetState>? StateChange;

        /// <summary>
        /// Gets the current internet state.
        /// </summary>
        public InternetState Status { get; private set; } = InternetState.Unchecked;

        /// <summary>
        /// Gets the external IP address of this connection.
        /// </summary>
        public string? ExternalIPAddress { get; private set; } = string.Empty;

        /// <summary>
        ///  Initialises the singleton.
        /// </summary>
        /// <param name="logger">ILogger instance.</param>
        /// <param name="config">IServerConfigurationManager instance.</param>
        public void Initialise(ILogger<InternetChecker> logger, IServerConfigurationManager config)
        {
            _logger = logger;
            _config = config;
            _config.ConfigurationUpdated += ConfigChanged;

            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            _externalResolver = "http://bot.whatismyipaddress.com"; ////_config.Configuration.ExternalResolver;
            // another alternative is http://checkip.dyndns.org

            _externalAddress = string.Empty; //// _config.Configuration.ExternalAddress;
            _externalRedirector = string.Empty; //// _config.Configuration.ExternalRedirector;

            _retryCount = 300; //// _config.Configuration.RetryCount;
            _retryDelay = 1000; //// _config.Configuration.RetryDelay;
            int check = 10; //// _config.Configuration.CheckEvery;  in minutes

            if (_every == -1)
            {
                _timer = new Timer(CheckInternetAccess, null, TimeSpan.Zero, TimeSpan.FromMinutes(check));
                _every = check;
            }
            else if (check != _every)
            {
                _timer.Change(TimeSpan.FromMinutes(check), TimeSpan.FromMinutes(check));
                _every = check;
            }
        }

        private void ConfigChanged(object sender, EventArgs args)
        {
            LoadConfiguration();
        }

        /// <summary>
        /// Checks the internet state.
        /// </summary>
        /// <param name="state">Stae of the timer.</param>
        private void CheckInternetAccess(object state)
        {
            _ = CheckAccess(0);
        }

        private async Task CheckAccess(int retry = 0)
        {
            InternetState laststate = Status;
            Status = await TryInternetAccess().ConfigureAwait(false);

            if (Status != InternetState.Up && retry < _retryCount)
            {
                if (Status != laststate && laststate != InternetState.Unchecked)
                {
                    StateChange?.Invoke(this, InternetState.Down);
                }

                await Task.Delay(_retryDelay).ConfigureAwait(false);
                _ = CheckAccess(retry + 1);
            }
            else
            {
                if (Status != laststate && laststate != InternetState.Unchecked)
                {
                    StateChange?.Invoke(this, Status);
                }
            }
        }

        private async Task<InternetState> TryInternetAccess()
        {
            if (string.IsNullOrEmpty(_externalResolver))
            {
                return InternetState.Unchecked;
            }

            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(_externalResolver).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    response?.Dispose();

                    Regex ip = new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
                    MatchCollection result = ip.Matches(content);

                    if (result.Count > 0 && IPAddress.TryParse(result[0].Value, out IPAddress _))
                    {
                        ExternalIPAddress = result[0].Value;
                    }
                    else
                    {
                        _logger?.LogWarning("External website returned unknown response {0}", content);
                        ExternalIPAddress = null;
                        return InternetState.Error;
                    }

                    if (!string.IsNullOrEmpty(_externalRedirector))
                    {
                        // Use our ping back address.
                        string addr = _externalRedirector + _externalAddress.Replace("[ip]", ExternalIPAddress.ToString(), StringComparison.OrdinalIgnoreCase);
                        addr = addr.TrimEnd('/') + "/system/ping";

                        try
                        {
                            response = await client.GetAsync(addr).ConfigureAwait(false);
                            response.EnsureSuccessStatusCode();
                            content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                            response?.Dispose();

                            if (string.Equals(content, FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location).ProductName, StringComparison.CurrentCulture))
                            {
                                // Rocking and rolling!
                                return InternetState.Up;
                            }

                            // If we got this far and didn't get the correct response, it's got to be because of the redirect.
                            _logger?.LogError("Redirection failed with {0}", content);
                            return InternetState.RedirectFailure;
                        }
                        catch (HttpRequestException ex)
                        {
                            _logger?.LogError(ex, "Connection to redirection site failed.");
                            return InternetState.Down;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error connecting to external site.");
                return InternetState.Error;
            }

            _logger?.LogInformation("Internet access is up. {0}", ExternalIPAddress);
            return InternetState.Up;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (_disposed)
            {
                return;
            }

            _timer?.Dispose();
            _disposed = true;
        }
    }
}
