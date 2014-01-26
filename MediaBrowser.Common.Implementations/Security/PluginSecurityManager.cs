using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Security;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Implementations.Security
{
    /// <summary>
    /// Class PluginSecurityManager
    /// </summary>
    public class PluginSecurityManager : ISecurityManager
    {
        /// <summary>
        /// The _is MB supporter
        /// </summary>
        private bool? _isMbSupporter;
        /// <summary>
        /// The _is MB supporter initialized
        /// </summary>
        private bool _isMbSupporterInitialized;
        /// <summary>
        /// The _is MB supporter sync lock
        /// </summary>
        private object _isMbSupporterSyncLock = new object();

        /// <summary>
        /// Gets a value indicating whether this instance is MB supporter.
        /// </summary>
        /// <value><c>true</c> if this instance is MB supporter; otherwise, <c>false</c>.</value>
        public bool IsMBSupporter
        {
            get
            {
                LazyInitializer.EnsureInitialized(ref _isMbSupporter, ref _isMbSupporterInitialized, ref _isMbSupporterSyncLock, () => GetRegistrationStatus("MBSupporter", null, _appHost.ApplicationVersion.ToString()).Result.IsRegistered);
                return _isMbSupporter.Value;
            }
        }

        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IApplicationHost _appHost;
        private readonly IApplicationPaths _applciationPaths;
        private readonly INetworkManager _networkManager;

        private IEnumerable<IRequiresRegistration> _registeredEntities;
        protected IEnumerable<IRequiresRegistration> RegisteredEntities
        {
            get
            {
                return _registeredEntities ?? (_registeredEntities = _appHost.GetExports<IRequiresRegistration>());
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginSecurityManager" /> class.
        /// </summary>
        public PluginSecurityManager(IApplicationHost appHost, IHttpClient httpClient, IJsonSerializer jsonSerializer,
            IApplicationPaths appPaths, INetworkManager networkManager, ILogManager logManager)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException("httpClient");
            }

            _applciationPaths = appPaths;
            _networkManager = networkManager;
            _appHost = appHost;
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            MBRegistration.Init(_applciationPaths, _networkManager, logManager);
        }

        /// <summary>
        /// Load all registration info for all entities that require registration
        /// </summary>
        /// <returns></returns>
        public async Task LoadAllRegistrationInfo()
        {
            var tasks = new List<Task>();

            ResetSupporterInfo();
            tasks.AddRange(RegisteredEntities.Select(i => i.LoadRegistrationInfoAsync()));
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Gets the registration status.
        /// This overload supports existing plug-ins.
        /// </summary>
        /// <param name="feature">The feature.</param>
        /// <param name="mb2Equivalent">The MB2 equivalent.</param>
        /// <returns>Task{MBRegistrationRecord}.</returns>
        public async Task<MBRegistrationRecord> GetRegistrationStatus(string feature, string mb2Equivalent = null)
        {
            return await MBRegistration.GetRegistrationStatus(_httpClient, _jsonSerializer, feature, mb2Equivalent).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the registration status.
        /// </summary>
        /// <param name="feature">The feature.</param>
        /// <param name="mb2Equivalent">The MB2 equivalent.</param>
        /// <param name="version">The version of this feature</param>
        /// <returns>Task{MBRegistrationRecord}.</returns>
        public async Task<MBRegistrationRecord> GetRegistrationStatus(string feature, string mb2Equivalent, string version)
        {
            return await MBRegistration.GetRegistrationStatus(_httpClient, _jsonSerializer, feature, mb2Equivalent, version).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets or sets the supporter key.
        /// </summary>
        /// <value>The supporter key.</value>
        public string SupporterKey
        {
            get
            {
                return MBRegistration.SupporterKey;
            }
            set
            {
                if (value != MBRegistration.SupporterKey)
                {
                    MBRegistration.SupporterKey = value;
                    // re-load registration info
                    Task.Run(() => LoadAllRegistrationInfo());
                }
            }
        }

        /// <summary>
        /// Gets or sets the legacy key.
        /// </summary>
        /// <value>The legacy key.</value>
        public string LegacyKey
        {
            get
            {
                return MBRegistration.LegacyKey;
            }
            set
            {
                if (value != MBRegistration.LegacyKey)
                {
                    MBRegistration.LegacyKey = value;
                    // re-load registration info
                    Task.Run(() => LoadAllRegistrationInfo());
                }
            }
        }

        /// <summary>
        /// Resets the supporter info.
        /// </summary>
        private void ResetSupporterInfo()
        {
            _isMbSupporter = null;
            _isMbSupporterInitialized = false;
        }
    }
}
