using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Implementations.Security
{
    public static class MBRegistration
    {

        private static MBLicenseFile _licenseFile;
        private const string MBValidateUrl = Constants.Constants.MbAdminUrl + "service/registration/validate";

        private static IApplicationPaths _appPaths;
        private static INetworkManager _networkManager;
        private static ILogger _logger;
        private static IApplicationHost _applicationHost;

        private static MBLicenseFile LicenseFile
        {
            get { return _licenseFile ?? (_licenseFile = new MBLicenseFile(_appPaths)); }
        }

        public static string SupporterKey
        {
            get { return LicenseFile.RegKey; }
            set { LicenseFile.RegKey = value; LicenseFile.Save(); }
        }

        public static string LegacyKey
        {
            get { return LicenseFile.LegacyKey; }
            set { LicenseFile.LegacyKey = value; LicenseFile.Save(); }
        }

        public static void Init(IApplicationPaths appPaths, INetworkManager networkManager, ILogManager logManager, IApplicationHost appHost)
        {
            // Ugly alert (static init)

            _appPaths = appPaths;
            _networkManager = networkManager;
            _logger = logManager.GetLogger("SecurityManager");
            _applicationHost = appHost;
        }

        public static async Task<MBRegistrationRecord> GetRegistrationStatus(IHttpClient httpClient, IJsonSerializer jsonSerializer, string feature, string mb2Equivalent = null, string version = null)
        {
            //check the reg file first to alleviate strain on the MB admin server - must actually check in every 30 days tho
            var reg = new RegRecord { registered = LicenseFile.LastChecked(feature) > DateTime.UtcNow.AddDays(-30) };

            if (!reg.registered)
            {
                var mac = _networkManager.GetMacAddress();
                var data = new Dictionary<string, string>
                {
                    { "feature", feature }, 
                    { "key", SupporterKey }, 
                    { "mac", mac }, 
                    { "mb2equiv", mb2Equivalent }, 
                    { "legacykey", LegacyKey }, 
                    { "ver", version }, 
                    { "platform", Environment.OSVersion.VersionString }, 
                    { "isservice", _applicationHost.IsRunningAsService.ToString().ToLower() }
                };

                try
                {
                    using (var json = await httpClient.Post(MBValidateUrl, data, CancellationToken.None).ConfigureAwait(false))
                    {
                        reg = jsonSerializer.DeserializeFromStream<RegRecord>(json);
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

            return new MBRegistrationRecord { IsRegistered = reg.registered, ExpirationDate = reg.expDate, RegChecked = true };
        }
    }

    class RegRecord
    {
        public string featId { get; set; }
        public bool registered { get; set; }
        public DateTime expDate { get; set; }
    }
}
