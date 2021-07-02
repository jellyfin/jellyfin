using System;
using System.Net;
using Jellyfin.DeviceProfiles;
using MediaBrowser.Model.Dlna;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Jellyfin.Profiles.Tests
{
    public class ProfileTemplateMatchingTests
    {
        /// <summary>
        /// Matches a device profile based on the general information provided, creating a dynamic profile.
        /// </summary>
        [Fact]
        public void General_Profile_Template_Matching()
        {
            var manager = ProfileTestingHelper.CreateProfileManager(true);

            var device = new DeviceIdentification()
            {
                FriendlyName = "Hisense (Other TV's are available)",
                ModelNumber = "123",
                ModelDescription = "1bc"
            };

            // Match device. Should return the general Hisense profile.
            var match = manager.GetOrCreateProfile(device, IPAddress.Parse("10.10.10.10"));
            Assert.StartsWith(ProfileTestingHelper.GeneralProfile, match.Name, StringComparison.Ordinal);

            // Ensure the correct profile type is returned.
            Assert.True(match.ProfileType == DeviceProfileType.Profile);
        }

        /// <summary>
        /// Matches a device profile based on the specific profile template (has ip address), creating a dynamic profile.
        /// </summary>
        [Fact]
        public void Specific_Profile_Template_Matching()
        {
            var manager = ProfileTestingHelper.CreateProfileManager(true);

            var device = new DeviceIdentification()
            {
                Address = "192.168.0.1",
                FriendlyName = "Hisense (Other TV's are available)",
                ModelNumber = "123",
                ModelDescription = "1bc"
            };

            // Match by address
            var match = manager.GetOrCreateProfile(device, IPAddress.Parse("192.168.0.1"));
            Assert.StartsWith(ProfileTestingHelper.SpecificProfile, match.Name, StringComparison.Ordinal);

            // Ensure a profile is returned and not a template.
            Assert.True(match.ProfileType == DeviceProfileType.Profile);
        }

        /// <summary>
        /// Matches with the exact profile IP address, not the specific one.
        /// </summary>
        [Fact]
        public void Exact_Profile_Template_Matching()
        {
            var manager = ProfileTestingHelper.CreateProfileManager(true);

            // Add a device template.
            manager.AddProfile(
                new DeviceProfile
                {
                    Name = ProfileTestingHelper.ExactProfile,
                    Identification = new ()
                    {
                        Address = "192.168.0.1",
                        FriendlyName = "Hisense (Other TV's are available)",
                        ModelNumber = "123",
                        ModelDescription = "1bc"
                    },
                    MaxStreamingBitrate = 1000
                },
                false);

            // Create a matching device.
            var device = new DeviceIdentification()
            {
                Address = "192.168.0.1",
                FriendlyName = "Hisense (Other TV's are available)",
                ModelNumber = "123",
                ModelDescription = "1bc"
            };

            var match = manager.GetOrCreateProfile(device);
            Assert.StartsWith(ProfileTestingHelper.ExactProfile, match.Name, StringComparison.Ordinal);

            // Ensure a profile is returned and not a template.
            Assert.True(match.ProfileType == DeviceProfileType.Profile);
        }

        [Fact]
        public void No_Match()
        {
            var manager = ProfileTestingHelper.CreateProfileManager(true);

            var device = new DeviceIdentification()
            {
                FriendlyName = "Panasonic",
                ModelNumber = "123",
                ModelDescription = "1bc"
            };

            // No match found, so the Generic Profile should be returned, named as Panasonic.
            var match = manager.GetOrCreateProfile(device);
            Assert.True(string.Equals(match.Name, "Panasonic", StringComparison.Ordinal));
        }

        /// <summary>
        /// Test simulates how the settings of a dlna server client can be enhanced if playTo is active.
        /// </summary>
        [Fact]
        public void Check_Default_Profile()
        {
            var manager = ProfileTestingHelper.CreateProfileManager(true);

            var dlnaServerClient = manager.GetOrCreateProfile(new HeaderDictionary(), IPAddress.Parse("10.10.10.10"));

            // Should have been assigned the default client, as no match found.
            Assert.StartsWith("Default Profile", dlnaServerClient.Name, StringComparison.Ordinal);
        }

        /// <summary>
        /// Priority of multiple profiles.
        /// </summary>
        /// <param name="property">A header property.</param>
        /// <param name="value">A header value.</param>
        /// <param name="property2">Optional second header.</param>
        /// <param name="value2">Optional second value.</param>
        /// <param name="match">The profile's matching name.</param>
        [InlineData("FriendlyName", "Hisense", null, null, "Profile 2")]
        [InlineData("FriendlyName", "Hisense TV", null, null, "Profile 1")]
        [InlineData("FriendlyName", "Hisense Television", null, null, "Profile 1")]
        [InlineData("FriendlyName", "Hisense", "ModelNumber", "123", "Profile 3")]
        [Theory]
        public void Ensure_Weighting_In_Header_Selection(string property, string value, string? property2, string? value2, string match)
        {
            var manager = ProfileTestingHelper.CreateProfileManager(false);
            AddTestData(manager);

            // client to match.
            var headers = new HeaderDictionary
            {
                { property, value }
            };

            if (!string.IsNullOrEmpty(property2))
            {
                headers[property2] = value2;
            }

            var profile = manager.GetProfile(headers, IPAddress.Parse("10.10.10.9"), null);
            Assert.True(string.Equals(profile.Name, match, StringComparison.Ordinal));
        }

        private static void AddTestData(IDeviceProfileManager manager)
        {
            manager.AddProfile(
                new DeviceProfile
                {
                    Name = "Profile 1",
                    Identification = new ()
                    {
                        FriendlyName = "Hisense*",
                        Headers = new[]
                        {
                            new HttpHeaderInfo
                            {
                                Name = "FriendlyName",
                                Value = "Hisense*",
                                Match = HeaderMatchType.Regex
                            }
                        }
                    },
                },
                false);

            manager.AddProfile(
                new DeviceProfile
                {
                    Name = "Profile 2",
                    Identification = new ()
                    {
                        FriendlyName = "Hisense",
                        Headers = new[]
                        {
                            new HttpHeaderInfo
                            {
                                Name = "FriendlyName",
                                Value = "Hisense",
                                Match = HeaderMatchType.Equals
                            }
                        }
                    }
                },
                false);

            manager.AddProfile(
                new DeviceProfile
                {
                    Name = "Profile 3",
                    Identification = new ()
                    {
                        FriendlyName = "Hisense",
                        ModelNumber = "123",
                        Headers = new[]
                        {
                            new HttpHeaderInfo
                            {
                                Name = "FriendlyName",
                                Value = "Hisense",
                                Match = HeaderMatchType.Substring
                            },

                            new HttpHeaderInfo
                            {
                                Name = "ModelNumber",
                                Value = "123",
                                Match = HeaderMatchType.Equals
                            }
                        }
                    },
                },
                false);
        }
    }
}
