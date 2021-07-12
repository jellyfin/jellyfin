using Xunit;

namespace Jellyfin.Profiles.Tests
{
    public class ProfileNameTest
    {
        [InlineData("ABC", null, "ABC1")]
        [InlineData("ABC1", null, "ABC2")]
        [InlineData("ABC9", null, "ABC10")]
        [InlineData("ABC9", "ABC10", "ABC11")]
        [InlineData("ABC99", null, "ABC100")]
        [Theory]
        public void Check_Unique_Names(string addName, string? addName2, string unique)
        {
            var manager = ProfileTestingHelper.CreateProfileManager(false);
            manager.AddProfile(
                new MediaBrowser.Model.Dlna.DeviceProfile
                {
                    Name = addName
                },
                false);

            if (addName2 != null)
            {
                manager.AddProfile(
                    new MediaBrowser.Model.Dlna.DeviceProfile
                    {
                        Name = addName2
                    },
                    false);
            }

            addName = manager.GetUniqueProfileName(addName);

            Assert.True(string.Equals(addName, unique, System.StringComparison.Ordinal));
        }
    }
}
