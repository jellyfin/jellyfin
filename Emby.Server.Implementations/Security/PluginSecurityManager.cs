using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Security;
using MediaBrowser.Controller;
using MediaBrowser.Model.Cryptography;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;

namespace Emby.Server.Implementations.Security
{
    /// <summary>
    /// Class PluginSecurityManager
    /// </summary>
    public class PluginSecurityManager : ISecurityManager
    {
        private const string MBValidateUrl = "https://mb3admin.com/admin/service/registration/validate";
        private const string AppstoreRegUrl = /*MbAdmin.HttpsUrl*/ "https://mb3admin.com/admin/service/appstore/register";

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
            get { return _licenseFile ?? (_licenseFile = new MBLicenseFile(_appPaths, _fileSystem, _cryptographyProvider)); }
        }

        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IServerApplicationHost _appHost;
        private readonly ILogger _logger;
        private readonly IApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;
        private readonly ICryptoProvider _cryptographyProvider;

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
        public PluginSecurityManager(IServerApplicationHost appHost, IHttpClient httpClient, IJsonSerializer jsonSerializer,
            IApplicationPaths appPaths, ILogManager logManager, IFileSystem fileSystem, ICryptoProvider cryptographyProvider)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException("httpClient");
            }

            _appHost = appHost;
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            _appPaths = appPaths;
            _fileSystem = fileSystem;
            _cryptographyProvider = cryptographyProvider;
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
                var newValue = value;
                if (newValue != null)
                {
                    newValue = newValue.Trim();
                }

                if (newValue != LicenseFile.RegKey)
                {
                    LicenseFile.RegKey = newValue;
                    LicenseFile.Save();

                    // re-load registration info
                    Task.Run(() => LoadAllRegistrationInfo());
                }
            }
        }

        /// <summary>
        /// Register an app store sale with our back-end.  It will validate the transaction with the store
        /// and then register the proper feature and then fill in the supporter key on success.
        /// </summary>
        /// <param name="parameters">Json parameters to send to admin server</param>
        public async Task RegisterAppStoreSale(string parameters)
        {
            var options = new HttpRequestOptions()
            {
                Url = AppstoreRegUrl,
                CancellationToken = CancellationToken.None,
                BufferContent = false
            };
            options.RequestHeaders.Add("X-Emby-Token", _appHost.SystemId);
            options.RequestContent = parameters;
            options.RequestContentType = "application/json";

            try
            {
                using (var response = await _httpClient.Post(options).ConfigureAwait(false))
                {
                    var reg = _jsonSerializer.DeserializeFromStream<RegRecord>(response.Content);

                    if (reg == null)
                    {
                        var msg = "Result from appstore registration was null.";
                        _logger.Error(msg);
                        throw new ArgumentException(msg);
                    }
                    if (!String.IsNullOrEmpty(reg.key))
                    {
                        SupporterKey = reg.key;
                    }
                }

            }
            catch (ArgumentException)
            {
                SaveAppStoreInfo(parameters);
                throw;
            }
            catch (HttpException e)
            {
                _logger.ErrorException("Error registering appstore purchase {0}", e, parameters ?? "NO PARMS SENT");

                if (e.StatusCode.HasValue && e.StatusCode.Value == HttpStatusCode.PaymentRequired)
                {
                    throw new PaymentRequiredException();
                }
                throw new Exception("Error registering store sale");
            }
            catch (Exception e)
            {
                _logger.ErrorException("Error registering appstore purchase {0}", e, parameters ?? "NO PARMS SENT");
                SaveAppStoreInfo(parameters);
                //TODO - could create a re-try routine on start-up if this file is there.  For now we can handle manually.
                throw new Exception("Error registering store sale");
            }

        }

        private void SaveAppStoreInfo(string info)
        {
            // Save all transaction information to a file

            try
            {
                _fileSystem.WriteAllText(Path.Combine(_appPaths.ProgramDataPath, "apptrans-error.txt"), info);
            }
            catch (IOException)
            {

            }
        }

        private async Task<MBRegistrationRecord> GetRegistrationStatusInternal(string feature,
            string mb2Equivalent = null,
            string version = null)
        {
            var regInfo = LicenseFile.GetRegInfo(feature);
            var lastChecked = regInfo == null ? DateTime.MinValue : regInfo.LastChecked;
            var expDate = regInfo == null ? DateTime.MinValue : regInfo.ExpirationDate;

            var maxCacheDays = 14;
            var nextCheckDate = new [] { expDate, lastChecked.AddDays(maxCacheDays) }.Min();

            if (nextCheckDate > DateTime.UtcNow.AddDays(maxCacheDays))
            {
                nextCheckDate = DateTime.MinValue;
            }

            //check the reg file first to alleviate strain on the MB admin server - must actually check in every 30 days tho
            var reg = new RegRecord
            {
                // Cache the result for up to a week
                registered = regInfo != null && nextCheckDate >= DateTime.UtcNow && expDate >= DateTime.UtcNow,
                expDate = expDate
            };

            var success = reg.registered;

            if (!(lastChecked > DateTime.UtcNow.AddDays(-1)) || !reg.registered)
            {
                var data = new Dictionary<string, string>
                {
                    { "feature", feature }, 
                    { "key", SupporterKey }, 
                    { "mac", _appHost.SystemId }, 
                    { "systemid", _appHost.SystemId }, 
                    { "mb2equiv", mb2Equivalent }, 
                    { "ver", version }, 
                    { "platform", _appHost.OperatingSystemDisplayName }, 
                    { "isservice", _appHost.IsRunningAsService.ToString().ToLower() }
                };

                try
                {
                    var options = new HttpRequestOptions
                    {
                        Url = MBValidateUrl,

                        // Seeing block length errors
                        EnableHttpCompression = false,
                        BufferContent = false
                    };

                    options.SetPostData(data);

                    using (var json = (await _httpClient.Post(options).ConfigureAwait(false)).Content)
                    {
                        reg = _jsonSerializer.DeserializeFromStream<RegRecord>(json);
                        success = true;
                    }

                    if (reg.registered)
                    {
                        LicenseFile.AddRegCheck(feature, reg.expDate);
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
            record.IsValid = !record.RegChecked || record.IsRegistered || record.TrialVersion;

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

            return isInTrial && !isRegistered;
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