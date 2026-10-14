using System.Collections.Generic;
using Xunit;

namespace Jellyfin.Database.Testing;

/// <summary>
/// Assertions that the assertion library does not offer in the form the database tests need.
/// </summary>
public static class AssertHelpers
{
    /// <summary>
    /// Compares two lists of names as one string each, so that a failure reports both lists in full. The assertion
    /// library reports only the first differing element of two collections.
    /// </summary>
    /// <param name="expected">The names the test expects.</param>
    /// <param name="actual">The names the query returned.</param>
    public static void AssertSameNames(IEnumerable<string> expected, IEnumerable<string> actual)
        => Assert.Equal(string.Join(", ", expected), string.Join(", ", actual));
}
