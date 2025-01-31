using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Emby.Naming.Video;

/// <summary>
/// Regex based rule for file stacking (eg. disc1, disc2).
/// </summary>
public class FileStackRule
{
    private readonly Regex _tokenRegex;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileStackRule"/> class.
    /// </summary>
    /// <param name="token">Token.</param>
    /// <param name="isNumerical">Whether the file stack rule uses numerical or alphabetical numbering.</param>
    public FileStackRule(string token, bool isNumerical)
    {
        _tokenRegex = new Regex(token, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        IsNumerical = isNumerical;
    }

    /// <summary>
    /// Gets a value indicating whether the rule uses numerical or alphabetical numbering.
    /// </summary>
    public bool IsNumerical { get; }

    /// <summary>
    /// Match the input against the rule regex.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <param name="result">The part type and number or <c>null</c>.</param>
    /// <returns>A value indicating whether the input matched the rule.</returns>
    public bool Match(string input, [NotNullWhen(true)] out (string StackName, string PartType, string PartNumber)? result)
    {
        result = null;
        var match = _tokenRegex.Match(input);
        if (!match.Success)
        {
            return false;
        }

        var partType = match.Groups["parttype"].Success ? match.Groups["parttype"].Value : "unknown";
        result = (match.Groups["filename"].Value, partType, match.Groups["number"].Value);
        return true;
    }
}
