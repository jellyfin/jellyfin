using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using Xunit;

namespace Jellyfin.Controller.Tests.Entities;

public class CompanyInfoTests
{
    [Fact]
    public void Pack_NoCompanies_ReturnsNull()
    {
        Assert.Null(CompanyInfo.Pack(null));
        Assert.Null(CompanyInfo.Pack([]));
    }

    [Fact]
    public void Pack_DropsBlankNames()
    {
        Assert.Null(CompanyInfo.Pack([new CompanyInfo { Name = "  ", Type = CompanyKind.Studio }]));
    }

    [Fact]
    public void Pack_DuplicateNameAndKind_DropsTheDuplicate()
    {
        var packed = CompanyInfo.Pack(
        [
            new CompanyInfo { Name = "HBO", Type = CompanyKind.Network },
            new CompanyInfo { Name = "hbo", Type = CompanyKind.Network }
        ]);

        Assert.Equal("Network:HBO", packed);
    }

    [Fact]
    public void Pack_SameNameDifferentKinds_KeepsBoth()
    {
        var packed = CompanyInfo.Pack(
        [
            new CompanyInfo { Name = "HBO", Type = CompanyKind.Studio },
            new CompanyInfo { Name = "HBO", Type = CompanyKind.Network }
        ]);

        Assert.Equal("Studio:HBO|Network:HBO", packed);
    }

    [Theory]
    [InlineData("Warner Bros.", CompanyKind.Studio)]
    [InlineData("Amazon Prime Video", CompanyKind.Network)]
    // Only the first separator is the kind, so a name is free to contain more of them.
    [InlineData("Marvel: Studios", CompanyKind.Publisher)]
    public void Unpack_RoundTripsPack(string name, CompanyKind type)
    {
        var companies = CompanyInfo.Unpack(CompanyInfo.Pack([new CompanyInfo { Name = name, Type = type }]));

        var company = Assert.Single(companies);
        Assert.Equal(name, company.Name);
        Assert.Equal(type, company.Type);
    }

    [Fact]
    public void Unpack_NothingPacked_ReturnsEmpty()
    {
        Assert.Empty(CompanyInfo.Unpack(null));
        Assert.Empty(CompanyInfo.Unpack(string.Empty));
    }

    [Theory]
    [InlineData("Studio:")]
    [InlineData(":HBO")]
    [InlineData("HBO")]
    [InlineData("Broadcaster:HBO")]
    public void Unpack_UnreadableEntry_IsDropped(string packed)
    {
        Assert.Empty(CompanyInfo.Unpack(packed));
    }

    [Fact]
    public void Unpack_UnreadableEntry_KeepsTheReadableOnes()
    {
        var companies = CompanyInfo.Unpack("Studio:A|nonsense|Network:B");

        Assert.Equal(2, companies.Length);
        Assert.Equal("A", companies[0].Name);
        Assert.Equal("B", companies[1].Name);
    }
}
