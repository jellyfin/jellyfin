using System;

namespace MediaBrowser.Model.Extensions
{
    /// <summary>
    /// Defines the <see cref="ContainerExtensions"/> class.
    /// </summary>
    public static class ContainerExtensions
    {
        /// <summary>
        /// Compares two containers, returning true if an item in <paramref name="inputContainer"/> does not exist
        /// in <paramref name="profileContainers"/>.
        /// </summary>
        /// <param name="profileContainers">The comma-delimited string being searched.
        /// If the parameter begins with the - character, the operation is reversed.</param>
        /// <param name="inputContainer">The comma-delimited string being matched.</param>
        /// <returns>The result of the operation.</returns>
        public static bool ContainsContainer(this string? profileContainers, string? inputContainer)
        {
            var isNegativeList = string.IsNullOrEmpty(inputContainer);
            if (profileContainers != null && profileContainers.StartsWith('-'))
            {
                isNegativeList = true;
                profileContainers = profileContainers[1..];
            }

            return ContainsContainer(profileContainers, isNegativeList, inputContainer);
        }

        /// <summary>
        /// Compares two containers, returning true if an item in <paramref name="inputContainer"/> does not exist
        /// in <paramref name="profileContainers"/>.
        /// </summary>
        /// <param name="profileContainers">The comma-delimited string being searched.
        /// If the parameter begins with the - character, the operation is reversed.</param>
        /// <param name="inputContainer">The comma-delimited string being matched.</param>
        /// <returns>The result of the operation.</returns>
        public static bool ContainsContainer(this string? profileContainers, ReadOnlySpan<char> inputContainer)
        {
            var isNegativeList = inputContainer.IsEmpty;
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
        /// <param name="profileContainers">The comma-delimited string being searched.</param>
        /// <param name="isNegativeList">The boolean result to return if a match is not found.</param>
        /// <param name="inputContainer">The comma-delimited string being matched.</param>
        /// <returns>The result of the operation.</returns>
        public static bool ContainsContainer(this string? profileContainers, bool isNegativeList, string? inputContainer)
        {
            if (string.IsNullOrEmpty(profileContainers))
            {
                // Empty profiles always support all containers/codecs.
                return true;
            }

            if (string.IsNullOrEmpty(inputContainer))
            {
                return isNegativeList;
            }

            var allInputContainers = inputContainer.SpanSplit(',');
            var allProfileContainers = profileContainers.SpanSplit(',');
            foreach (var container in allInputContainers)
            {
                foreach (var profile in allProfileContainers)
                {
                    if (MemoryExtensions.Equals(profile, container, StringComparison.OrdinalIgnoreCase))
                    {
                        return !isNegativeList;
                    }
                }
            }

            return isNegativeList;
        }

        /// <summary>
        /// Compares two containers, returning <paramref name="isNegativeList"/> if an item in <paramref name="inputContainer"/>
        /// does not exist in <paramref name="profileContainers"/>.
        /// </summary>
        /// <param name="profileContainers">The comma-delimited string being searched.</param>
        /// <param name="isNegativeList">The boolean result to return if a match is not found.</param>
        /// <param name="inputContainer">The comma-delimited string being matched.</param>
        /// <returns>The result of the operation.</returns>
        public static bool ContainsContainer(this string? profileContainers, bool isNegativeList, ReadOnlySpan<char> inputContainer)
        {
            if (string.IsNullOrEmpty(profileContainers))
            {
                // Empty profiles always support all containers/codecs.
                return true;
            }

            if (inputContainer == null)
            {
                return isNegativeList;
            }

            var allInputContainers = inputContainer.Split(',');
            foreach (var container in allInputContainers)
            {
                var allProfileContainers = profileContainers.SpanSplit(',');
                foreach (var profile in allProfileContainers)
                {
                    if (MemoryExtensions.Equals(profile, container, StringComparison.OrdinalIgnoreCase))
                    {
                        return !isNegativeList;
                    }
                }
            }

            return isNegativeList;
        }
    }
}
