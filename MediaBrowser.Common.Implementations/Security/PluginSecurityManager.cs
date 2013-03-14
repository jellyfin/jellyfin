using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Security;
using MediaBrowser.Model.Serialization;
using Mediabrowser.Model.Entities;
using Mediabrowser.PluginSecurity;
using MediaBrowser.Common.Net;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

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
        private bool? _isMBSupporter;
        /// <summary>
        /// The _is MB supporter initialized
        /// </summary>
        private bool _isMBSupporterInitialized;
        /// <summary>
        /// The _is MB supporter sync lock
        /// </summary>
        private object _isMBSupporterSyncLock = new object();

        /// <summary>
        /// Gets a value indicating whether this instance is MB supporter.
        /// </summary>
        /// <value><c>true</c> if this instance is MB supporter; otherwise, <c>false</c>.</value>
        public bool IsMBSupporter
        {
            get
            {
                LazyInitializer.EnsureInitialized(ref _isMBSupporter, ref _isMBSupporterInitialized, ref _isMBSupporterSyncLock, () => GetRegistrationStatus("MBSupporter").Result.IsRegistered);
                return _isMBSupporter.Value;
            }
        }

        private IHttpClient _httpClient;
        private IJsonSerializer _jsonSerializer;
        private IApplicationHost _appHost;
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
        public PluginSecurityManager(IApplicationHost appHost, IHttpClient httpClient, IJsonSerializer jsonSerializer, IApplicationPaths appPaths)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException("httpClient");
            }

            _appHost = appHost;
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            MBRegistration.Init(appPaths);
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
        /// </summary>
        /// <param name="feature">The feature.</param>
        /// <param name="mb2Equivalent">The MB2 equivalent.</param>
        /// <returns>Task{MBRegistrationRecord}.</returns>
        public async Task<MBRegistrationRecord> GetRegistrationStatus(string feature, string mb2Equivalent = null)
        {
            return await MBRegistration.GetRegistrationStatus(_httpClient, _jsonSerializer, feature, mb2Equivalent).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets or sets the supporter key.
        /// </summary>
        /// <value>The supporter key.</value>
        public string SupporterKey
        {
            get { return MBRegistration.SupporterKey; }
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
            get { return MBRegistration.LegacyKey; }
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
            _isMBSupporter = null;
            _isMBSupporterInitialized = false;
        }
    }
}
