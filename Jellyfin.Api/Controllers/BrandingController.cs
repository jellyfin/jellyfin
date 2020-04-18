using Jellyfin.Api.Models.Branding;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// The branding controller.
    /// </summary>
    public class BrandingController : BaseJellyfinApiController
    {
        private IServerConfigurationManager _serverConfigurationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="BrandingController"/> class.
        /// </summary>
        /// <param name="serverConfigurationManager">Instance of ServerConfigurationManager.</param>
        public BrandingController(IServerConfigurationManager serverConfigurationManager)
        {
            this._serverConfigurationManager = serverConfigurationManager;
        }

        /// <summary>
        /// Endpoint for getting a servers branding settings.
        /// </summary>
        /// <returns>Branding settings of the server.</returns>
        [HttpGet("Configuration")]
        public BrandingDto GetBrandingOptions()
        {
            return this._serverConfigurationManager.GetConfiguration<BrandingDto>("branding");
        }

        /// <summary>
        /// Endpoint for getting a servers branding css.
        /// </summary>
        /// <returns>String representation of the server.</returns>
        [HttpGet("Css")]
        [HttpGet("Css.css")]
        public string GetBrandingCss()
        {
            var result = this._serverConfigurationManager.GetConfiguration<BrandingDto>("branding");
            return result.CustomCss ?? string.Empty;
        }
    }
}
