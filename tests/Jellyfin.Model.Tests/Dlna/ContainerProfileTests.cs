using MediaBrowser.Model.Dlna;
using Xunit;

namespace Jellyfin.Model.Tests.Dlna
{
    public class ContainerProfileTests
    {
        private readonly ContainerProfile _emptyContainerProfile = new ContainerProfile();

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("mp4")]
        public void ContainsContainer_EmptyContainerProfile_True(string? containers)
        {
            Assert.True(_emptyContainerProfile.ContainsContainer(containers));
        }
    }
}
