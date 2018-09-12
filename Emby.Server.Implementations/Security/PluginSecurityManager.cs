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

        public async Task<bool> IsSupporter()
        {
            var result = await GetRegistrationStatusInternal("MBSupporter", false, _appHost.ApplicationVersion.ToString(), CancellationToken.None).ConfigureAwait(false);

            return result.IsRegistered;
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
        /// Gets the registration status.
        /// This overload supports existing plug-ins.
        /// </summary>
        public Task<MBRegistrationRecord> GetRegistrationStatus(string feature)
        {
            return GetRegistrationStatusInternal(feature, false, null, CancellationToken.None);
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
                throw new Exception("Please call UpdateSupporterKey");
            }
        }

        public async Task UpdateSupporterKey(string newValue)
        {
            if (newValue != null)
            {
                newValue = newValue.Trim();
            }

            if (!string.Equals(newValue, LicenseFile.RegKey, StringComparison.Ordinal))
            {
                LicenseFile.RegKey = newValue;
                LicenseFile.Save();

                // Reset this
                await GetRegistrationStatusInternal("MBSupporter", true, _appHost.ApplicationVersion.ToString(), CancellationToken.None).ConfigureAwait(false);
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
                    var reg = await _jsonSerializer.DeserializeFromStreamAsync<RegRecord>(response.Content).ConfigureAwait(false);

                    if (reg == null)
                    {
                        var msg = "Result from appstore registration was null.";
                        _logger.Error(msg);
                        throw new ArgumentException(msg);
                    }
                    if (!String.IsNullOrEmpty(reg.key))
                    {
                        await UpdateSupporterKey(reg.key).ConfigureAwait(false);
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

        private SemaphoreSlim _regCheckLock = new SemaphoreSlim(1, 1);

        private async Task<MBRegistrationRecord> GetRegistrationStatusInternal(string feature, bool forceCallToServer, string version, CancellationToken cancellationToken)
        {
            await _regCheckLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var regInfo = LicenseFile.GetRegInfo(feature);
                var lastChecked = regInfo == null ? DateTime.MinValue : regInfo.LastChecked;
                var expDate = regInfo == null ? DateTime.MinValue : regInfo.ExpirationDate;

                var maxCacheDays = 14;
                var nextCheckDate = new[] { expDate, lastChecked.AddDays(maxCacheDays) }.Min();

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

                var key = SupporterKey;

                if (!forceCallToServer && string.IsNullOrWhiteSpace(key))
                {
                    return new MBRegistrationRecord();
                }

                var success = reg.registered;

                if (!(lastChecked > DateTime.UtcNow.AddDays(-1)) || (!reg.registered))
                {
                    var data = new Dictionary<string, string>
                {
                    { "feature", feature },
                    { "key", key },
                    { "mac", _appHost.SystemId },
                    { "systemid", _appHost.SystemId },
                    { "ver", version },
                    { "platform", _appHost.OperatingSystemDisplayName }
                };

                    try
                    {
                        var options = new HttpRequestOptions
                        {
                            Url = MBValidateUrl,

                            // Seeing block length errors
                            EnableHttpCompression = false,
                            BufferContent = false,
                            CancellationToken = cancellationToken
                        };

                        options.SetPostData(data);

                        using (var response = (await _httpClient.Post(options).ConfigureAwait(false)))
                        {
                            using (var json = response.Content)
                            {
                                reg = await _jsonSerializer.DeserializeFromStreamAsync<RegRecord>(json).ConfigureAwait(false);
                                success = true;
                            }
                        }

                        if (reg.registered)
                        {
                            _logger.Info("Registered for feature {0}", feature);
                            LicenseFile.AddRegCheck(feature, reg.expDate);
                        }
                        else
                        {
                            _logger.Info("Not registered for feature {0}", feature);
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
            finally
            {
                _regCheckLock.Release();
            }
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
    }
}