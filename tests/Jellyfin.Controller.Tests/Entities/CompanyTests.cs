using System.Linq;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using Xunit;

namespace Jellyfin.Controller.Tests.Entities;

public class CompanyTests
{
    [Theory]
    [InlineData("HBO", "hbo")]
    [InlineData("H.B.O", "H B O")]
    [InlineData("Studio Ghibli", "Stüdio  Ghibli")]
    public void GetCompanyId_NamesThatCleanToTheSameValue_ShareAnId(string name, string otherName)
    {
        Assert.Equal(Company.GetCompanyId(name), Company.GetCompanyId(otherName));
    }

    [Fact]
    public void GetCompanyId_OneNameCreditedTwice_IsOneCompany()
    {
        var item = new Movie();
        item.AddCompany("HBO", CompanyKind.Studio);
        item.AddCompany("HBO", CompanyKind.Network);

        // Two credits, one company: reclassifying does not move it off the by-name item that
        // carries its artwork and user data.
        Assert.Equal(2, item.Companies.Length);
        Assert.Single(item.Companies.Select(e => Company.GetCompanyId(e.Name)).Distinct());
    }

    [Fact]
    public void GetCompanyId_DifferentNames_DifferById()
    {
        Assert.NotEqual(Company.GetCompanyId("HBO"), Company.GetCompanyId("AMC"));
    }

    [Fact]
    public void AddCompany_SameNameDifferentKind_KeepsBoth()
    {
        var item = new Movie();

        item.AddCompany("HBO", CompanyKind.Studio);
        item.AddCompany("HBO", CompanyKind.Network);
        item.AddCompany("hbo", CompanyKind.Network);

        Assert.Equal(2, item.Companies.Length);
        Assert.Equal(["HBO"], item.GetCompanyNames(CompanyKind.Studio));
        Assert.Equal(["HBO"], item.GetCompanyNames(CompanyKind.Network));
    }

    [Fact]
    public void SetCompanies_ReplacesOneKindAndLeavesTheOthers()
    {
        var item = new Movie();
        item.AddCompany("HBO", CompanyKind.Network);
        item.AddCompany("Warner Bros.", CompanyKind.Studio);

        item.SetCompanies(["AMC"], CompanyKind.Network);

        Assert.Equal(["Warner Bros."], item.GetCompanyNames(CompanyKind.Studio));
        Assert.Equal(["AMC"], item.GetCompanyNames(CompanyKind.Network));
    }
}
