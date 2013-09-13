using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Entities;
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
        private const string MBValidateUrl = "http://mb3admin.com/admin/service/registration/validate";

        private static IApplicationPaths _appPaths;
        private static INetworkManager _networkManager;

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

        public static void Init(IApplicationPaths appPaths, INetworkManager networkManager)
        {
            // Ugly alert (static init)

            _appPaths = appPaths;
            _networkManager = networkManager;
        }

        public static async Task<MBRegistrationRecord> GetRegistrationStatus(IHttpClient httpClient, IJsonSerializer jsonSerializer, string feature, string mb2Equivalent = null)
        {
            var mac = _networkManager.GetMacAddress();
            var data = new Dictionary<string, string> {{"feature", feature}, {"key",SupporterKey}, {"mac",mac}, {"mb2equiv",mb2Equivalent}, {"legacykey", LegacyKey} };

            var reg = new RegRecord();
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

            }
            catch (Exception)
            {
                //if we have trouble obtaining from web - allow it if we've validated in the past 30 days
                reg.registered = LicenseFile.LastChecked(feature) > DateTime.UtcNow.AddDays(-30);
            }

            return new MBRegistrationRecord {IsRegistered = reg.registered, ExpirationDate = reg.expDate, RegChecked = true};
        }
    }

    class RegRecord
    {
        public string featId { get; set; }
        public bool registered { get; set; }
        public DateTime expDate { get; set; }
    }
}
