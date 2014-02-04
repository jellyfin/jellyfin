using MediaBrowser.Common;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Security;
using MediaBrowser.Common.Updates;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ServiceStack.Text.Controller;
using ServiceStack.Web;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class Plugins
    /// </summary>
    [Route("/Plugins", "GET")]
    [Api(("Gets a list of currently installed plugins"))]
    public class GetPlugins : IReturn<List<PluginInfo>>
    {
    }

    /// <summary>
    /// Class UninstallPlugin
    /// </summary>
    [Route("/Plugins/{Id}", "DELETE")]
    [Api(("Uninstalls a plugin"))]
    public class UninstallPlugin : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Plugin Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public Guid Id { get; set; }
    }

    /// <summary>
    /// Class GetPluginConfiguration
    /// </summary>
    [Route("/Plugins/{Id}/Configuration", "GET")]
    [Api(("Gets a plugin's configuration"))]
    public class GetPluginConfiguration
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Plugin Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public Guid Id { get; set; }
    }

    /// <summary>
    /// Class UpdatePluginConfiguration
    /// </summary>
    [Route("/Plugins/{Id}/Configuration", "POST")]
    [Api(("Updates a plugin's configuration"))]
    public class UpdatePluginConfiguration : IRequiresRequestStream, IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Plugin Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public Guid Id { get; set; }

        /// <summary>
        /// The raw Http Request Input Stream
        /// </summary>
        /// <value>The request stream.</value>
        public Stream RequestStream { get; set; }
    }

    /// <summary>
    /// Class GetPluginSecurityInfo
    /// </summary>
    [Route("/Plugins/SecurityInfo", "GET")]
    [Api(("Gets plugin registration information"))]
    public class GetPluginSecurityInfo : IReturn<PluginSecurityInfo>
    {
    }

    /// <summary>
    /// Class UpdatePluginSecurityInfo
    /// </summary>
    [Route("/Plugins/SecurityInfo", "POST")]
    [Api("Updates plugin registration information")]
    public class UpdatePluginSecurityInfo : PluginSecurityInfo, IReturnVoid
    {
    }

    [Route("/Plugins/RegistrationRecords/{Name}", "GET")]
    [Api("Gets registration status for a feature")]
    public class GetRegistrationStatus
    {
        [ApiMember(Name = "Name", Description = "Feature Name", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Name { get; set; }

        [ApiMember(Name = "Mb2Equivalent", Description = "Optional. The equivalent feature name in MB2", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Mb2Equivalent { get; set; }
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

        private readonly ISecurityManager _securityManager;

        private readonly IInstallationManager _installationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginService" /> class.
        /// </summary>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="appHost">The app host.</param>
        /// <param name="securityManager">The security manager.</param>
        /// <param name="installationManager">The installation manager.</param>
        /// <exception cref="System.ArgumentNullException">jsonSerializer</exception>
        public PluginService(IJsonSerializer jsonSerializer, IApplicationHost appHost, ISecurityManager securityManager, IInstallationManager installationManager)
            : base()
        {
            if (jsonSerializer == null)
            {
                throw new ArgumentNullException("jsonSerializer");
            }

            _appHost = appHost;
            _securityManager = securityManager;
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
            var result = _securityManager.GetRegistrationStatus(request.Name, request.Mb2Equivalent).Result;

            return ToOptimizedResult(result);
        }
        
        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetPlugins request)
        {
            var result = _appHost.Plugins.OrderBy(p => p.Name).Select(p => p.GetPluginInfo()).ToList();

            return ToOptimizedSerializedResultUsingCache(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetPluginConfiguration request)
        {
            var plugin = _appHost.Plugins.First(p => p.Id == request.Id);

            var dateModified = plugin.ConfigurationDateLastModified;

            var cacheKey = (plugin.Version.ToString() + dateModified.Ticks).GetMD5();

            return ToOptimizedResultUsingCache(cacheKey, dateModified, null, () => plugin.Configuration);
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
                IsMBSupporter = _securityManager.IsMBSupporter,
                SupporterKey = _securityManager.SupporterKey,
                LegacyKey = _securityManager.LegacyKey
            };

            return ToOptimizedSerializedResultUsingCache(result);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(UpdatePluginSecurityInfo request)
        {
            var info = request;

            _securityManager.SupporterKey = info.SupporterKey;
            _securityManager.LegacyKey = info.LegacyKey;
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(UpdatePluginConfiguration request)
        {
            // We need to parse this manually because we told service stack not to with IRequiresRequestStream
            // https://code.google.com/p/servicestack/source/browse/trunk/Common/ServiceStack.Text/ServiceStack.Text/Controller/PathInfo.cs
            var pathInfo = PathInfo.Parse(Request.PathInfo);
            var id = new Guid(pathInfo.GetArgumentValue<string>(1));

            var plugin = _appHost.Plugins.First(p => p.Id == id);

            var configuration = _jsonSerializer.DeserializeFromStream(request.RequestStream, plugin.ConfigurationType) as BasePluginConfiguration;

            plugin.UpdateConfiguration(configuration);
        }

        /// <summary>
        /// Deletes the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Delete(UninstallPlugin request)
        {
            var plugin = _appHost.Plugins.First(p => p.Id == request.Id);

            _installationManager.UninstallPlugin(plugin);
        }
    }
}
