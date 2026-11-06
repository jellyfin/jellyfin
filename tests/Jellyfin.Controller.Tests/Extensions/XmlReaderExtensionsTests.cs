using System.IO;
using System.Xml;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Extensions;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Controller.Tests.Extensions;

public class XmlReaderExtensionsTests
{
    [Fact]
    public void GetPersonFromXmlNode_CreditWithProviderIds_KeepsThem()
    {
        // Kodi writes these next to the name, and they identify the person where the spelling does not.
        var person = ReadPerson(
            """
            <actor>
                <name>Zoe Saldana</name>
                <role>Neytiri</role>
                <tmdbid>1234</tmdbid>
                <imdbid>nm0757855</imdbid>
            </actor>
            """);

        Assert.NotNull(person);
        Assert.Equal("Zoe Saldana", person.Name);
        Assert.Equal("Neytiri", person.Role);
        Assert.Equal("1234", person.GetProviderId(MetadataProvider.Tmdb));
        Assert.Equal("nm0757855", person.GetProviderId(MetadataProvider.Imdb));
    }

    [Fact]
    public void GetPersonFromXmlNode_CreditWithoutProviderIds_HasNone()
    {
        var person = ReadPerson("<actor><name>Zoe Saldana</name></actor>");

        Assert.NotNull(person);
        Assert.Empty(person.ProviderIds);
    }

    [Fact]
    public void GetPersonFromXmlNode_CreditWithAnEmptyProviderId_SkipsIt()
    {
        var person = ReadPerson("<actor><name>Zoe Saldana</name><tmdbid></tmdbid></actor>");

        Assert.NotNull(person);
        Assert.Empty(person.ProviderIds);
    }

    private static PersonInfo? ReadPerson(string xml)
    {
        using var reader = XmlReader.Create(new StringReader(xml));
        reader.MoveToContent();

        return reader.GetPersonFromXmlNode();
    }
}
