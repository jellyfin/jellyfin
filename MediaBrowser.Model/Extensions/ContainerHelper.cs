using System;
using System.Collections.Generic;
using Jellyfin.Extensions;

namespace MediaBrowser.Model.Extensions;

/// <summary>
/// Defines the <see cref="ContainerHelper"/> class.
/// </summary>
public static class ContainerHelper
{
    /// <summary>
    /// Compares two containers, returning true if an item in <paramref name="inputContainer"/> exists
    /// in <paramref name="profileContainers"/>.
    /// </summary>
    /// <param name="profileContainers">The comma-delimited string being searched.
    /// If the parameter begins with the <c>-</c> character, the operation is reversed.
    /// If the parameter is empty or null, all containers in <paramref name="inputContainer"/> will be accepted.</param>
    /// <param name="inputContainer">The comma-delimited string being matched.</param>
    /// <returns>The result of the operation.</returns>
    public static bool ContainsContainer(string? profileContainers, string? inputContainer)
    {
        var isNegativeList = false;
        if (profileContainers != null && profileContainers.StartsWith('-'))
        {
            isNegativeList = true;
            profileContainers = profileContainers[1..];
        }

        return ContainsContainer(profileContainers, isNegativeList, inputContainer);
    }

    /// <summary>
    /// Compares two containers, returning true if an item in <paramref name="inputContainer"/> exists
    /// in <paramref name="profileContainers"/>.
    /// </summary>
    /// <param name="profileContainers">The comma-delimited string being searched.
    /// If the parameter begins with the <c>-</c> character, the operation is reversed.
    /// If the parameter is empty or null, all containers in <paramref name="inputContainer"/> will be accepted.</param>
    /// <param name="inputContainer">The comma-delimited string being matched.</param>
    /// <returns>The result of the operation.</returns>
    public static bool ContainsContainer(string? profileContainers, ReadOnlySpan<char> inputContainer)
    {
        var isNegativeList = false;
        if (profileContainers != null && profileContainers.StartsWith('-'))
        {
            isNegativeList = true;
            profileContainers = profileContainers[1..];
        }

        return ContainsContainer(profileContainers, isNegativeList, inputContainer);
    }

    /// <summary>
    /// Compares two containers, returning <paramref name="isNegativeList"/> if an item in <paramref name="inputContainer"/>
    /// does not exist in <paramref name="profileContainers"/>.
    /// </summary>
    /// <param name="profileContainers">The comma-delimited string being searched.
    /// If the parameter is empty or null, all containers in <paramref name="inputContainer"/> will be accepted.</param>
    /// <param name="isNegativeList">The boolean result to return if a match is not found.</param>
    /// <param name="inputContainer">The comma-delimited string being matched.</param>
    /// <returns>The result of the operation.</returns>
    public static bool ContainsContainer(string? profileContainers, bool isNegativeList, string? inputContainer)
    {
        if (string.IsNullOrEmpty(inputContainer))
        {
            return isNegativeList;
        }

        return ContainsContainer(profileContainers, isNegativeList, inputContainer.AsSpan());
    }

    /// <summary>
    /// Compares two containers, returning <paramref name="isNegativeList"/> if an item in <paramref name="inputContainer"/>
    /// does not exist in <paramref name="profileContainers"/>.
    /// </summary>
    /// <param name="profileContainers">The comma-delimited string being searched.
    /// If the parameter is empty or null, all containers in <paramref name="inputContainer"/> will be accepted.</param>
    /// <param name="isNegativeList">The boolean result to return if a match is not found.</param>
    /// <param name="inputContainer">The comma-delimited string being matched.</param>
    /// <returns>The result of the operation.</returns>
    public static bool ContainsContainer(string? profileContainers, bool isNegativeList, ReadOnlySpan<char> inputContainer)
    {
        if (string.IsNullOrEmpty(profileContainers))
        {
            // Empty profiles always support all containers/codecs.
            return true;
        }

        var allInputContainers = inputContainer.Split(',');
        var allProfileContainers = profileContainers.SpanSplit(',');
        foreach (var container in allInputContainers)
        {
            if (!container.IsEmpty)
            {
                foreach (var profile in allProfileContainers)
                {
                    if (!profile.IsEmpty && container.Equals(profile, StringComparison.OrdinalIgnoreCase))
                    {
                        return !isNegativeList;
                    }
                }
            }
        }

        return isNegativeList;
    }

    /// <summary>
    /// Compares two containers, returning <paramref name="isNegativeList"/> if an item in <paramref name="inputContainer"/>
    /// does not exist in <paramref name="profileContainers"/>.
    /// </summary>
    /// <param name="profileContainers">The profile containers being matched searched.
    /// If the parameter is empty or null, all containers in <paramref name="inputContainer"/> will be accepted.</param>
    /// <param name="isNegativeList">The boolean result to return if a match is not found.</param>
    /// <param name="inputContainer">The comma-delimited string being matched.</param>
    /// <returns>The result of the operation.</returns>
    public static bool ContainsContainer(IReadOnlyList<string>? profileContainers, bool isNegativeList, string inputContainer)
    {
        if (profileContainers is null)
        {
            // Empty profiles always support all containers/codecs.
            return true;
        }

        var allInputContainers = Split(inputContainer);
        foreach (var container in allInputContainers)
        {
            foreach (var profile in profileContainers)
            {
                if (string.Equals(profile, container, StringComparison.OrdinalIgnoreCase))
                {
                    return !isNegativeList;
                }
            }
        }

        return isNegativeList;
    }

    /// <summary>
    /// Splits and input string.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>The result of the operation.</returns>
    public static string[] Split(string? input)
    {
        return input?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [];
    }
}
