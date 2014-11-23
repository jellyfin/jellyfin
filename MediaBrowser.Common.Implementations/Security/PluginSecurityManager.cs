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
        private const string MBValidateUrl = Constants.Constants.MbAdminUrl + "service/registration/validate";

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
                LazyInitializer.EnsureInitialized(ref _isMbSupporter, ref _isMbSupporterInitialized, ref _isMbSupporterSyncLock, () => GetSupporterRegistrationStatus().Result.IsRegistered);
                return _isMbSupporter.Value;
            }
        }

        private MBLicenseFile _licenseFile;
        private MBLicenseFile LicenseFile
        {
            get { return _licenseFile ?? (_licenseFile = new MBLicenseFile(_appPaths)); }
        }

        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IApplicationHost _appHost;
        private readonly ILogger _logger;
        private readonly INetworkManager _networkManager;
        private readonly IApplicationPaths _appPaths;

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

            _appHost = appHost;
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            _networkManager = networkManager;
            _appPaths = appPaths;
            _logger = logManager.GetLogger("SecurityManager");
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
        public Task<MBRegistrationRecord> GetRegistrationStatus(string feature, string mb2Equivalent = null)
        {
            return GetRegistrationStatusInternal(feature, mb2Equivalent);
        }

        /// <summary>
        /// Gets the registration status.
        /// </summary>
        /// <param name="feature">The feature.</param>
        /// <param name="mb2Equivalent">The MB2 equivalent.</param>
        /// <param name="version">The version of this feature</param>
        /// <returns>Task{MBRegistrationRecord}.</returns>
        public Task<MBRegistrationRecord> GetRegistrationStatus(string feature, string mb2Equivalent, string version)
        {
            return GetRegistrationStatusInternal(feature, mb2Equivalent, version);
        }

        private Task<MBRegistrationRecord> GetSupporterRegistrationStatus()
        {
            return GetRegistrationStatusInternal("MBSupporter", null, _appHost.ApplicationVersion.ToString());
        }

        /// <summary>
        /// Gets or sets the supporter key.
        /// </summary>
        /// <value>The supporter key.</value>
        public string SupporterKey
        {
            get
            {
                return LicenseFile.RegKey;
            }
            set
            {
                if (value != LicenseFile.RegKey)
                {
                    LicenseFile.RegKey = value;
                    LicenseFile.Save();

                    // re-load registration info
                    Task.Run(() => LoadAllRegistrationInfo());
                }
            }
        }

        public async Task<SupporterInfo> GetSupporterInfo()
        {
            var key = SupporterKey;

            if (string.IsNullOrWhiteSpace(key))
            {
                return new SupporterInfo();
            }

            var url = Constants.Constants.MbAdminUrl + "/service/supporter/retrieve?key=" + key;

            using (var stream = await _httpClient.Get(url, CancellationToken.None).ConfigureAwait(false))
            {
                var response = _jsonSerializer.DeserializeFromStream<SuppporterInfoResponse>(stream);

                var info = new SupporterInfo
                {
                    Email = response.email,
                    PlanType = response.planType,
                    SupporterKey = response.supporterKey,
                    ExpirationDate = string.IsNullOrWhiteSpace(response.expDate) ? (DateTime?)null : DateTime.Parse(response.expDate),
                    RegistrationDate = DateTime.Parse(response.regDate),
                    IsActiveSupporter = IsMBSupporter
                };

                info.IsExpiredSupporter = info.ExpirationDate.HasValue && info.ExpirationDate < DateTime.UtcNow && !string.IsNullOrWhiteSpace(info.SupporterKey);

                return info;
            }
        }

        private async Task<MBRegistrationRecord> GetRegistrationStatusInternal(string feature,
            string mb2Equivalent = null,
            string version = null)
        {
            var lastChecked = LicenseFile.LastChecked(feature);

            //check the reg file first to alleviate strain on the MB admin server - must actually check in every 30 days tho
            var reg = new RegRecord
            {
                // Cache the result for up to a week
                registered = lastChecked > DateTime.UtcNow.AddDays(-7)
            };

            var success = reg.registered;

            if (!(lastChecked > DateTime.UtcNow.AddDays(-1)))
            {
                var mac = _networkManager.GetMacAddress();
                var data = new Dictionary<string, string>
                {
                    { "feature", feature }, 
                    { "key", SupporterKey }, 
                    { "mac", mac }, 
                    { "systemid", _appHost.SystemId }, 
                    { "mb2equiv", mb2Equivalent }, 
                    { "ver", version }, 
                    { "platform", _appHost.OperatingSystemDisplayName }, 
                    { "isservice", _appHost.IsRunningAsService.ToString().ToLower() }
                };

                try
                {
                    using (var json = await _httpClient.Post(MBValidateUrl, data, CancellationToken.None).ConfigureAwait(false))
                    {
                        reg = _jsonSerializer.DeserializeFromStream<RegRecord>(json);
                        success = true;
                    }

                    if (reg.registered)
                    {
                        LicenseFile.AddRegCheck(feature);
                    }
                    else
                    {
                        LicenseFile.RemoveRegCheck(feature);
                    }

                }
                catch (Exception e)
                {
                    _logger.ErrorException("Error checking registration status of {0}", e, feature);
                }
            }

            var record = new MBRegistrationRecord
            {
                IsRegistered = reg.registered,
                ExpirationDate = reg.expDate,
                RegChecked = true,
                RegError = !success
            };

            record.TrialVersion = IsInTrial(reg.expDate, record.RegChecked, record.IsRegistered);
            record.IsValid = !record.RegChecked || (record.IsRegistered || record.TrialVersion);

            return record;
        }

        private bool IsInTrial(DateTime expirationDate, bool regChecked, bool isRegistered)
        {
            //don't set this until we've successfully obtained exp date
            if (!regChecked)
            {
                return false;
            }

            var isInTrial = expirationDate > DateTime.UtcNow;

            return (isInTrial && !isRegistered);
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
