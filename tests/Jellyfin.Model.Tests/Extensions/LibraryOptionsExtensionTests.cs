using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Extensions;
using Xunit;

namespace Jellyfin.Model.Tests.Extensions
{
    public class LibraryOptionsExtensionTests
    {
        [Fact]
        public void GetCustomTagDelimiters_MultiCharacterEntry_SplitsIntoIndividualCharacters()
        {
            var options = new LibraryOptions { CustomTagDelimiters = [",&"] };

            var delimiters = options.GetCustomTagDelimiters();

            Assert.Contains(',', delimiters);
            Assert.Contains('&', delimiters);
        }

        [Fact]
        public void GetCustomTagDelimiters_SingleCharacterEntries_AreKept()
        {
            var options = new LibraryOptions { CustomTagDelimiters = ["/", "|", ";", "\\"] };

            var delimiters = options.GetCustomTagDelimiters();

            Assert.Contains('/', delimiters);
            Assert.Contains('|', delimiters);
            Assert.Contains(';', delimiters);
            Assert.Contains('\\', delimiters);
        }
    }
}
