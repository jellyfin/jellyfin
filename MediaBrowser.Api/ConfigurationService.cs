using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class GetConfiguration
    /// </summary>
    [Route("/System/Configuration", "GET", Summary = "Gets application configuration")]
    [Authenticated]
    public class GetConfiguration : IReturn<ServerConfiguration>
    {

    }

    [Route("/System/Configuration/{Key}", "GET", Summary = "Gets a named configuration")]
    [Authenticated(AllowBeforeStartupWizard = true)]
    public class GetNamedConfiguration
    {
        [ApiMember(Name = "Key", Description = "Key", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Key { get; set; }
    }

    /// <summary>
    /// Class UpdateConfiguration
    /// </summary>
    [Route("/System/Configuration", "POST", Summary = "Updates application configuration")]
    [Authenticated(Roles = "Admin")]
    public class UpdateConfiguration : ServerConfiguration, IReturnVoid
    {
    }

    [Route("/System/Configuration/{Key}", "POST", Summary = "Updates named configuration")]
    [Authenticated(Roles = "Admin")]
    public class UpdateNamedConfiguration : IReturnVoid, IRequiresRequestStream
    {
        [ApiMember(Name = "Key", Description = "Key", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Key { get; set; }

        public Stream RequestStream { get; set; }
    }

    [Route("/System/Configuration/MetadataOptions/Default", "GET", Summary = "Gets a default MetadataOptions object")]
    [Authenticated(Roles = "Admin")]
    public class GetDefaultMetadataOptions : IReturn<MetadataOptions>
    {

    }

    [Route("/System/MediaEncoder/Path", "POST", Summary = "Updates the path to the media encoder")]
    [Authenticated(Roles = "Admin", AllowBeforeStartupWizard = true)]
    public class UpdateMediaEncoderPath : IReturnVoid
    {
        [ApiMember(Name = "Path", Description = "Path", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Path { get; set; }
        [ApiMember(Name = "PathType", Description = "PathType", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string PathType { get; set; }
    }

    public class ConfigurationService : BaseApiService
    {
        /// <summary>
        /// The _json serializer
        /// </summary>
        private readonly IJsonSerializer _jsonSerializer;

        /// <summary>
        /// The _configuration manager
        /// </summary>
        private readonly IServerConfigurationManager _configurationManager;

        private readonly IMediaEncoder _mediaEncoder;

        public ConfigurationService(
            ILogger<ConfigurationService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            IJsonSerializer jsonSerializer,
            IServerConfigurationManager configurationManager,
            IMediaEncoder mediaEncoder)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _jsonSerializer = jsonSerializer;
            _configurationManager = configurationManager;
            _mediaEncoder = mediaEncoder;
        }

        public void Post(UpdateMediaEncoderPath request)
        {
            _mediaEncoder.UpdateEncoderPath(request.Path, request.PathType);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetConfiguration request)
        {
            return ToOptimizedResult(_configurationManager.Configuration);
        }

        public object Get(GetNamedConfiguration request)
        {
            var result = _configurationManager.GetConfiguration(request.Key);

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Posts the specified configuraiton.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(UpdateConfiguration request)
        {
            // Silly, but we need to serialize and deserialize or the XmlSerializer will write the xml with an element name of UpdateConfiguration
            var json = _jsonSerializer.SerializeToString(request);

            var config = _jsonSerializer.DeserializeFromString<ServerConfiguration>(json);

            _configurationManager.ReplaceConfiguration(config);
        }

        public async Task Post(UpdateNamedConfiguration request)
        {
            var key = GetPathValue(2).ToString();

            var configurationType = _configurationManager.GetConfigurationType(key);
            var configuration = await _jsonSerializer.DeserializeFromStreamAsync(request.RequestStream, configurationType).ConfigureAwait(false);

            _configurationManager.SaveConfiguration(key, configuration);
        }

        public object Get(GetDefaultMetadataOptions request)
        {
            return ToOptimizedResult(new MetadataOptions());
        }
    }
}
