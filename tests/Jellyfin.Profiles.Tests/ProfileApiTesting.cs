using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Model.Dlna;
using Xunit;

namespace Jellyfin.Profiles.Tests
{
    public class ProfileApiTesting
    {
        [Fact]
        public void Delete_User_Template()
        {
            var pm = ProfileTestingHelper.CreateProfileManager(true);

            var userProfile = pm.Profiles.First(p => string.Equals(p.Name, ProfileTestingHelper.GeneralProfile, StringComparison.Ordinal));

            // Attempt to delete a system profile.
            pm.DeleteProfile(userProfile.Id);

            // profile should no longer exist.
            Assert.True(pm.GetProfile(userProfile.Id) == null);
        }

        [Fact]
        public void Delete_System_Template()
        {
            var pm = ProfileTestingHelper.CreateProfileManager(false);

            // create a dummy system template.
            var systemTemplate = new DeviceProfile()
            {
                ProfileType = DeviceProfileType.SystemTemplate
            };
            pm.AddProfile(systemTemplate);

            // Attempt to delete a system profile.
            try
            {
                pm.DeleteProfile(systemTemplate.Id);

                // The code should NEVER hit this.
                Assert.True(false);
            }
            catch (ArgumentException)
            {
                // do nothing.
            }
            catch (Exception)
            {
                // The code should NEVER hit this.
                Assert.True(false);
            }

            // profile should no longer exist.
            Assert.True(pm.GetProfile(systemTemplate.Id) != null);
        }

        [Fact]
        public void Update_UserTemplate()
        {
            var pm = ProfileTestingHelper.CreateProfileManager(true);
            var userProfile = pm.Profiles.First(p => !p.Id.Equals(Guid.Empty));

            // Simulate a name change.
            var updatedProfile = new DeviceProfile(userProfile);
            updatedProfile.Name = "I changed the name.";
            updatedProfile.ProfileType = DeviceProfileType.Profile;

            Assert.True(userProfile.Id != updatedProfile.Id);

            // Update the profile.
            pm.UpdateProfile(userProfile.Id, updatedProfile, false);

            var newProfile = pm.GetProfile(userProfile.Id);
            Assert.NotNull(newProfile);

            Assert.Equal("I changed the name.", newProfile!.Name);
        }

        [Fact]
        public void Update_SystemTemplate()
        {
            var pm = ProfileTestingHelper.CreateProfileManager(false);
            var systemTemplate = new DeviceProfile()
            {
                ProfileType = DeviceProfileType.SystemTemplate,
                Name = "Template"
            };
            pm.AddProfile(systemTemplate);

            // Simulate a name change.
            var updatedProfile = new DeviceProfile(systemTemplate);
            updatedProfile.Name = "I changed the name.";

            Assert.True(systemTemplate.Id != updatedProfile.Id);

            // Update the profile.
            pm.UpdateProfile(systemTemplate.Id, updatedProfile, false);

            systemTemplate = pm.GetProfile(systemTemplate.Id);
            Assert.NotNull(systemTemplate);

            // Template should still exist.
            Assert.Equal("Template", systemTemplate!.Name);

            // But there should be another user template created.
            Assert.Contains(pm.Profiles, p => string.Equals("I changed the name.", p.Name, StringComparison.Ordinal));
        }
    }
}
