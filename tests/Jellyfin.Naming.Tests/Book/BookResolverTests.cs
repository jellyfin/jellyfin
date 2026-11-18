using Emby.Naming.Book;
using Xunit;

namespace Jellyfin.Naming.Tests.Book;

public class BookResolverTests
{
    [Theory]
    // seriesName (seriesYear?) #index (of count?) (year?)
    [InlineData("Sherlock Holmes (1887) #1 (of 4) (1887)", null, "Sherlock Holmes", 1, 1887)]
    [InlineData("Sherlock Holmes #2", null, "Sherlock Holmes", 2, null)]
    [InlineData("Sherlock Holmes (1887) #1", null, "Sherlock Holmes", 1, null)]
    [InlineData("Sherlock Holmes #2 (1890)", null, "Sherlock Holmes", 2, 1890)]
    // name (seriesName, #index) (year?)
    [InlineData("A Study in Scarlet (Sherlock Holmes, #1) (1887)", "A Study in Scarlet", "Sherlock Holmes", 1, 1887)]
    [InlineData("The Adventures of Sherlock Holmes (Sherlock Holmes, #5)", "The Adventures of Sherlock Holmes", "Sherlock Holmes", 5, null)]
    // name (year)
    [InlineData("The Sign of the Four (1890)", "The Sign of the Four", null, null, 1890)]
    [InlineData("The Valley of Fear (1915)", "The Valley of Fear", null, null, 1915)]
    // index - name (year?)
    [InlineData("2 - The Sign of the Four (1890)", "The Sign of the Four", null, 2, 1890)]
    [InlineData("4 - The Valley of Fear", "The Valley of Fear", null, 4, null)]
    // parse entire string as book name
    [InlineData("A Study in Scarlet", "A Study in Scarlet", null, null, null)]
    [InlineData("The Adventures of Sherlock Holmes", "The Adventures of Sherlock Holmes", null, null, null)]
    // leading zeros on index number
    [InlineData("00 - Dracula's Guest (1914)", "Dracula's Guest", null, 0, 1914)]
    [InlineData("01 - Dracula (1897)", "Dracula", null, 1, 1897)]
    // basic decimal support for prequels and novellas
    [InlineData("2.0 - Twenty Thousand Leagues Under the Sea", "Twenty Thousand Leagues Under the Sea", null, 2, null)]
    // TODO decide how to process non-zero decimals
    [InlineData("2.1 - The Blockade Runners", "2.1 - The Blockade Runners", null, null, null)]
    public void Resolve_Books(string input, string? name, string? series, int? index, int? year)
    {
        var result = BookFileNameParser.Parse(input);

        Assert.Equal(name, result.Name);
        Assert.Equal(series, result.SeriesName);
        Assert.Equal(index, result.Index);
        Assert.Equal(year, result.Year);
    }

    [Theory]
    // name volume? chapter? (year?)
    [InlineData("Captain Marvel Adventures v01 (1941)", "Captain Marvel Adventures v01", null, null, 1, 1941)]
    [InlineData("Captain Marvel Adventures c120", "Captain Marvel Adventures c120", null, 120, null, null)]
    [InlineData("Captain Marvel Adventures v01 c120", "Captain Marvel Adventures v01 c120", null, 120, 1, null)]
    public void Resolve_Comics(string input, string? name, string? series, int? chapter, int? volume, int? year)
    {
        var result = BookFileNameParser.Parse(input);

        Assert.Equal(name, result.Name);
        Assert.Equal(series, result.SeriesName);
        Assert.Equal(chapter, result.Index);
        Assert.Equal(volume, result.ParentIndex);
        Assert.Equal(year, result.Year);
    }
}
