using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Model.Dlna;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Jellyfin.Profiles.Tests
{
    public class DlnaTests
    {
        /// <summary>
        /// Test simulates how the settings of a dlna server client can be enhanced if playTo is active.
        /// </summary>
        [Fact]
        public void Simulation_Dlna_Server_Enhanced_By_Dlna_PlayTo()
        {
            var manager = ProfileTestingHelper.CreateProfileManager(true);

            // Simulate the DLNA Server Client connection via DLNA.
            var headers = new HeaderDictionary
            {
                { "ModelDescription", "ab" }
            };

            var dlnaServerClient = manager.GetOrCreateProfile(headers, IPAddress.Parse("10.10.10.10"));
            // Should have been assigned the default client, as no match found.
            Assert.True(string.Equals(dlnaServerClient.Name, "Default Profile", StringComparison.Ordinal));

            // Simulate the device is then discovered by DLNA PlayTo.
            var enhancedProfile = manager.GetOrCreateProfile(
                new DeviceDetails
                {
                    Address = "10.10.10.10",
                    FriendlyName = "Hisense TV"
                },
                null);

            Assert.True(enhancedProfile.Id == dlnaServerClient.Id);
        }

        /// <summary>
        /// Test simulates how the settings of a dlna server client can be enhanced if playTo is active.
        /// </summary>
        [Fact]
        public void Simulation_Dlna_PlayTo_Then_Dlna_Server()
        {
            var manager = ProfileTestingHelper.CreateProfileManager(true);

            // Simulate the dlna PlayTo callback.
            var playToClient = manager.GetOrCreateProfile(
                new DeviceDetails
                {
                    Address = "10.10.10.10",
                    FriendlyName = "Hisense TV"
                });

            // Should have been assigned the default client, as no match found.
            Assert.True(string.Equals(playToClient.Name, "Hisense TV", StringComparison.Ordinal));

            // Simulate the DLNA Server Client connection via DLNA.
            var headers = new HeaderDictionary
            {
                { "ModelDescription", "1bc" }
            };
            var dlnaServerClient = manager.GetOrCreateProfile(headers, IPAddress.Parse("10.10.10.10"));

            Assert.True(playToClient.Id == dlnaServerClient.Id);
        }
    }
}
