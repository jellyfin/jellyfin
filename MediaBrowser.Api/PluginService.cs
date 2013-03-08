using MediaBrowser.Common;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Security;
using MediaBrowser.Controller.Updates;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Server.Implementations.HttpServer;
using ServiceStack.ServiceHost;
using ServiceStack.Text.Controller;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class Plugins
    /// </summary>
    [Route("/Plugins", "GET")]
    [ServiceStack.ServiceHost.Api(("Gets a list of currently installed plugins"))]
    public class GetPlugins : IReturn<List<PluginInfo>>
    {
    }

    /// <summary>
    /// Class GetPluginAssembly
    /// </summary>
    [Route("/Plugins/{Id}/Assembly", "GET")]
    [ServiceStack.ServiceHost.Api(("Gets a plugin assembly file"))]
    public class GetPluginAssembly
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Plugin Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public Guid Id { get; set; }
    }

    /// <summary>
    /// Class UninstallPlugin
    /// </summary>
    [Route("/Plugins/{Id}", "DELETE")]
    [ServiceStack.ServiceHost.Api(("Uninstalls a plugin"))]
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
    [ServiceStack.ServiceHost.Api(("Gets a plugin's configuration"))]
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
    [ServiceStack.ServiceHost.Api(("Updates a plugin's configuration"))]
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
    /// Class GetPluginConfigurationFile
    /// </summary>
    [Route("/Plugins/{Id}/ConfigurationFile", "GET")]
    [ServiceStack.ServiceHost.Api(("Gets a plugin's configuration file, in plain text"))]
    public class GetPluginConfigurationFile
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Plugin Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public Guid Id { get; set; }
    }

    /// <summary>
    /// Class GetPluginSecurityInfo
    /// </summary>
    [Route("/Plugins/SecurityInfo", "GET")]
    [ServiceStack.ServiceHost.Api(("Gets plugin registration information"))]
    public class GetPluginSecurityInfo : IReturn<PluginSecurityInfo>
    {
    }

    /// <summary>
    /// Class UpdatePluginSecurityInfo
    /// </summary>
    [Route("/Plugins/SecurityInfo", "POST")]
    [ServiceStack.ServiceHost.Api(("Updates plugin registration information"))]
    public class UpdatePluginSecurityInfo : PluginSecurityInfo, IReturnVoid
    {
    }

    /// <summary>
    /// Class PluginsService
    /// </summary>
    public class PluginService : BaseRestService
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
        public object Get(GetPlugins request)
        {
            var result = _appHost.Plugins.OrderBy(p => p.Name).Select(p => p.GetPluginInfo()).ToList();
            
            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetPluginAssembly request)
        {
            var plugin = _appHost.Plugins.First(p => p.Id == request.Id);

            return ToStaticFileResult(plugin.AssemblyFilePath);
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
        public object Get(GetPluginConfigurationFile request)
        {
            var plugin = _appHost.Plugins.First(p => p.Id == request.Id);

            return ToStaticFileResult(plugin.ConfigurationFilePath);
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

            return ToOptimizedResult(result);
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
