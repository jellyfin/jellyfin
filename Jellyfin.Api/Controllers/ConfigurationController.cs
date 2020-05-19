#nullable enable

using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Models.ConfigurationDtos;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// Configuration Controller.
    /// </summary>
    [Route("System")]
    [Authorize]
    public class ConfigurationController : BaseJellyfinApiController
    {
        private readonly IServerConfigurationManager _configurationManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IJsonSerializer _jsonSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationController"/> class.
        /// </summary>
        /// <param name="configurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="mediaEncoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
        /// <param name="jsonSerializer">Instance of the <see cref="IJsonSerializer"/> interface.</param>
        public ConfigurationController(
            IServerConfigurationManager configurationManager,
            IMediaEncoder mediaEncoder,
            IJsonSerializer jsonSerializer)
        {
            _configurationManager = configurationManager;
            _mediaEncoder = mediaEncoder;
            _jsonSerializer = jsonSerializer;
        }

        /// <summary>
        /// Gets application configuration.
        /// </summary>
        /// <response code="200">Application configuration returned.</response>
        /// <returns>Application configuration.</returns>
        [HttpGet("Configuration")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<ServerConfiguration> GetConfiguration()
        {
            return _configurationManager.Configuration;
        }

        /// <summary>
        /// Updates application configuration.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <response code="200">Configuration updated.</response>
        /// <returns>Update status.</returns>
        [HttpPost("Configuration")]
        [Authorize(Policy = Policies.RequiresElevation)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult UpdateConfiguration([FromBody, BindRequired] ServerConfiguration configuration)
        {
            _configurationManager.ReplaceConfiguration(configuration);
            return Ok();
        }

        /// <summary>
        /// Gets a named configuration.
        /// </summary>
        /// <param name="key">Configuration key.</param>
        /// <response code="200">Configuration returned.</response>
        /// <returns>Configuration.</returns>
        [HttpGet("Configuration/{Key}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<object> GetNamedConfiguration([FromRoute] string key)
        {
            return _configurationManager.GetConfiguration(key);
        }

        /// <summary>
        /// Updates named configuration.
        /// </summary>
        /// <param name="key">Configuration key.</param>
        /// <response code="200">Named configuration updated.</response>
        /// <returns>Update status.</returns>
        [HttpPost("Configuration/{Key}")]
        [Authorize(Policy = Policies.RequiresElevation)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> UpdateNamedConfiguration([FromRoute] string key)
        {
            var configurationType = _configurationManager.GetConfigurationType(key);
            /*
            // TODO switch to System.Text.Json when https://github.com/dotnet/runtime/issues/30255 is fixed.
            var configuration = await JsonSerializer.DeserializeAsync(Request.Body, configurationType);
            */

            var configuration = await _jsonSerializer.DeserializeFromStreamAsync(Request.Body, configurationType)
                .ConfigureAwait(false);
            _configurationManager.SaveConfiguration(key, configuration);
            return Ok();
        }

        /// <summary>
        /// Gets a default MetadataOptions object.
        /// </summary>
        /// <response code="200">Metadata options returned.</response>
        /// <returns>Default MetadataOptions.</returns>
        [HttpGet("Configuration/MetadataOptions/Default")]
        [Authorize(Policy = Policies.RequiresElevation)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<MetadataOptions> GetDefaultMetadataOptions()
        {
            return new MetadataOptions();
        }

        /// <summary>
        /// Updates the path to the media encoder.
        /// </summary>
        /// <param name="mediaEncoderPath">Media encoder path form body.</param>
        /// <response code="200">Media encoder path updated.</response>
        /// <returns>Status.</returns>
        [HttpPost("MediaEncoder/Path")]
        [Authorize(Policy = Policies.FirstTimeSetupOrElevated)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult UpdateMediaEncoderPath([FromForm, BindRequired] MediaEncoderPathDto mediaEncoderPath)
        {
            _mediaEncoder.UpdateEncoderPath(mediaEncoderPath.Path, mediaEncoderPath.PathType);
            return Ok();
        }
    }
}
