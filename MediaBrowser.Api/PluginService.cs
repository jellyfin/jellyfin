using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.Updates;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class Plugins
    /// </summary>
    [Route("/Plugins", "GET", Summary = "Gets a list of currently installed plugins")]
    [Authenticated]
    public class GetPlugins : IReturn<PluginInfo[]>
    {
        public bool? IsAppStoreEnabled { get; set; }
    }

    /// <summary>
    /// Class UninstallPlugin
    /// </summary>
    [Route("/Plugins/{Id}", "DELETE", Summary = "Uninstalls a plugin")]
    [Authenticated(Roles = "Admin")]
    public class UninstallPlugin : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Plugin Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Id { get; set; }
    }

    /// <summary>
    /// Class GetPluginConfiguration
    /// </summary>
    [Route("/Plugins/{Id}/Configuration", "GET", Summary = "Gets a plugin's configuration")]
    [Authenticated]
    public class GetPluginConfiguration
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Plugin Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    /// <summary>
    /// Class UpdatePluginConfiguration
    /// </summary>
    [Route("/Plugins/{Id}/Configuration", "POST", Summary = "Updates a plugin's configuration")]
    [Authenticated]
    public class UpdatePluginConfiguration : IRequiresRequestStream, IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Plugin Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }

        /// <summary>
        /// The raw Http Request Input Stream
        /// </summary>
        /// <value>The request stream.</value>
        public Stream RequestStream { get; set; }
    }

    //TODO Once we have proper apps and plugins and decide to break compatibility with paid plugins,
    // delete all these registration endpoints. They are only kept for compatibility.
    [Route("/Registrations/{Name}", "GET", Summary = "Gets registration status for a feature", IsHidden = true)]
    [Authenticated]
    public class GetRegistration : IReturn<RegistrationInfo>
    {
        [ApiMember(Name = "Name", Description = "Feature Name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }
    }

    /// <summary>
    /// Class GetPluginSecurityInfo
    /// </summary>
    [Route("/Plugins/SecurityInfo", "GET", Summary = "Gets plugin registration information", IsHidden = true)]
    [Authenticated]
    public class GetPluginSecurityInfo : IReturn<PluginSecurityInfo>
    {
    }

    /// <summary>
    /// Class UpdatePluginSecurityInfo
    /// </summary>
    [Route("/Plugins/SecurityInfo", "POST", Summary = "Updates plugin registration information", IsHidden = true)]
    [Authenticated(Roles = "Admin")]
    public class UpdatePluginSecurityInfo : PluginSecurityInfo, IReturnVoid
    {
    }

    [Route("/Plugins/RegistrationRecords/{Name}", "GET", Summary = "Gets registration status for a feature", IsHidden = true)]
    [Authenticated]
    public class GetRegistrationStatus
    {
        [ApiMember(Name = "Name", Description = "Feature Name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }
    }

    // TODO these two classes are only kept for compability with paid plugins and should be removed
    public class RegistrationInfo
    {
        public string Name { get; set; }
        public DateTime ExpirationDate { get; set; }
        public bool IsTrial { get; set; }
        public bool IsRegistered { get; set; }
    }

    public class MBRegistrationRecord
    {
        public DateTime ExpirationDate { get; set; }
        public bool IsRegistered { get; set; }
        public bool RegChecked { get; set; }
        public bool RegError { get; set; }
        public bool TrialVersion { get; set; }
        public bool IsValid { get; set; }
    }

    public class PluginSecurityInfo
    {
        public string SupporterKey { get; set; }
        public bool IsMBSupporter { get; set; }
    }
    /// <summary>
    /// Class PluginsService
    /// </summary>
    public class PluginService : BaseApiService
    {
        /// <summary>
        /// The _json serializer
        /// </summary>
        private readonly IJsonSerializer _jsonSerializer;

        /// <summary>
        /// The _app host
        /// </summary>
        private readonly IApplicationHost _appHost;
        private readonly IInstallationManager _installationManager;

        public PluginService(
            ILogger<PluginService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            IJsonSerializer jsonSerializer,
            IApplicationHost appHost,
            IInstallationManager installationManager)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _appHost = appHost;
            _installationManager = installationManager;
            _jsonSerializer = jsonSerializer;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetRegistrationStatus request)
        {
            var record = new MBRegistrationRecord
            {
                IsRegistered = true,
                RegChecked = true,
                TrialVersion = false,
                IsValid = true,
                RegError = false
            };

            return ToOptimizedResult(record);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetPlugins request)
        {
            var result = _appHost.Plugins.OrderBy(p => p.Name).Select(p => p.GetPluginInfo()).ToArray();
            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetPluginConfiguration request)
        {
            var guid = new Guid(request.Id);
            var plugin = _appHost.Plugins.First(p => p.Id == guid) as IHasPluginConfiguration;

            return ToOptimizedResult(plugin.Configuration);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetPluginSecurityInfo request)
        {
            var result = new PluginSecurityInfo
            {
                IsMBSupporter = true,
                SupporterKey = "IAmTotallyLegit"
            };

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public Task Post(UpdatePluginSecurityInfo request)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public async Task Post(UpdatePluginConfiguration request)
        {
            // We need to parse this manually because we told service stack not to with IRequiresRequestStream
            // https://code.google.com/p/servicestack/source/browse/trunk/Common/ServiceStack.Text/ServiceStack.Text/Controller/PathInfo.cs
            var id = Guid.Parse(GetPathValue(1));

            if (!(_appHost.Plugins.First(p => p.Id == id) is IHasPluginConfiguration plugin))
            {
                throw new FileNotFoundException();
            }

            var configuration = (await _jsonSerializer.DeserializeFromStreamAsync(request.RequestStream, plugin.ConfigurationType).ConfigureAwait(false)) as BasePluginConfiguration;

            plugin.UpdateConfiguration(configuration);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(UninstallPlugin request)
        {
            var guid = new Guid(request.Id);
            var plugin = _appHost.Plugins.First(p => p.Id == guid);

            _installationManager.UninstallPlugin(plugin);
        }
    }
}
