using MediaBrowser.Model.Entities;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Implementations.Security
{
    public static class MBRegistration
    {

        private static MBLicenseFile _licenseFile;
        private const string MBValidateUrl = "http://mb3admin.com/admin/service/registration/validate";

        private static IApplicationPaths _appPaths;

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

        public static void Init(IApplicationPaths appPaths)
        {
            // Ugly alert (static init)

            _appPaths = appPaths;
        }

        public static async Task<MBRegistrationRecord> GetRegistrationStatus(IHttpClient httpClient, IJsonSerializer jsonSerializer, string feature, string mb2Equivalent = null)
        {
            var mac = GetMacAddress();
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

        /// <summary>
        /// Returns MAC Address from first Network Card in Computer
        /// </summary>
        /// <returns>[string] MAC Address</returns>
        public static string GetMacAddress()
        {
            var mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            var moc = mc.GetInstances();
            var macAddress = String.Empty;
            foreach (ManagementObject mo in moc)
            {
                if (macAddress == String.Empty)  // only return MAC Address from first card
                {
                    try
                    {
                        if ((bool)mo["IPEnabled"]) macAddress = mo["MacAddress"].ToString();
                    }
                    catch
                    {
                        mo.Dispose();
                        return "";
                    }
                }
                mo.Dispose();
            }

            return macAddress.Replace(":", "");
        }
    }

    class RegRecord
    {
        public string featId { get; set; }
        public bool registered { get; set; }
        public DateTime expDate { get; set; }
    }
}
