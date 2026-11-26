using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Providers.People;
using Xunit;

namespace Jellyfin.Providers.Tests.People;

public class PersonMetadataServiceTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MergeData_ExistingPerson_KeepsName(bool replaceData)
    {
        var source = new MetadataResult<Person> { Item = new Person { Name = "Regina Hall", Overview = "from the provider" } };
        var target = new MetadataResult<Person> { Item = new Person { Name = "Kathleen Banovich" } };

        new TestPersonMetadataService().MergeData(source, target, [], replaceData, false);

        Assert.Equal("Kathleen Banovich", target.Item.Name);
        Assert.Equal("from the provider", target.Item.Overview);
    }

    [Fact]
    public void MergeData_NewPerson_TakesName()
    {
        var source = new MetadataResult<Person> { Item = new Person { Name = "Regina Hall" } };
        var target = new MetadataResult<Person> { Item = new Person() };

        new TestPersonMetadataService().MergeData(source, target, [], false, false);

        Assert.Equal("Regina Hall", target.Item.Name);
    }

    private sealed class TestPersonMetadataService : PersonMetadataService
    {
        public TestPersonMetadataService()
            : base(null!, null!, null!, null!, null!, null!, null!)
        {
        }

        public new void MergeData(
            MetadataResult<Person> source,
            MetadataResult<Person> target,
            MetadataField[] lockedFields,
            bool replaceData,
            bool mergeMetadataSettings)
            => base.MergeData(source, target, lockedFields, replaceData, mergeMetadataSettings);
    }
}
