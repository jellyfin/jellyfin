using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ICU4N.Globalization;
using ICU4N.Impl;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace Emby.Server.Implementations.Dto
{
    /// <summary>
    /// Contains methods to help with determining if Name or OriginalTitle should be PrimaryDisplayName.
    /// </summary>
    public static class PreferOriginalTitleHelper
    {
        private static readonly IReadOnlyDictionary<int, int[]> _hybridScriptsLookup = new Dictionary<int, int[]>
        {
            { UScript.HanWithBopomofo, [UScript.Han, UScript.Bopomofo] },
            // not (yet?) included in ICU4N library: { UScript.Hntl, [UScript.TraditionalHan, UScript.Latin] },
            { UScript.KatakanaOrHiragana, [UScript.Katakana, UScript.Hiragana] },
            { UScript.Japanese, [UScript.Katakana, UScript.Hiragana, UScript.Han] },
            { UScript.Korean, [UScript.Han, UScript.Hangul] },
        };

        /// <summary>
        /// Gets physical folder ids which are direct descendants of collection folders. These folders are TopParents for other items.
        /// </summary>
        /// <param name="collectionFolders">List of source collection folders.</param>
        /// <param name="libraryIds">List of collection folder ids.</param>
        /// <returns>Physical folder ids.</returns>
        public static IReadOnlySet<Guid> GetPhysicalFolderIdsFromFolders(IEnumerable<BaseItem> collectionFolders, IReadOnlySet<Guid> libraryIds)
        {
            ArgumentNullException.ThrowIfNull(collectionFolders);
            ArgumentNullException.ThrowIfNull(libraryIds);

            HashSet<Guid>? result = null;
            foreach (var item in collectionFolders)
            {
                if (item is CollectionFolder cf && libraryIds.Contains(item.Id))
                {
                    if (result is null)
                    {
                        result = [.. cf.PhysicalFolderIds];
                    }
                    else
                    {
                        result.UnionWith(cf.PhysicalFolderIds);
                    }
                }
            }

            if (result is null)
            {
                return FrozenSet<Guid>.Empty;
            }
            else
            {
                return result;
            }
        }

        /// <summary>
        /// Gets physical folder ids which are direct descendants of collection folders. These folders are TopParents for other items.
        /// </summary>
        /// <param name="libraryManager">Library Manager instance.</param>
        /// <param name="libraryIds">Optional list of collection folder ids.</param>
        /// <returns>Physical folder ids.</returns>
        public static IReadOnlySet<Guid> GetPhysicalFolderIdsFromLibraryIds(ILibraryManager libraryManager, IReadOnlySet<Guid> libraryIds)
        {
            ArgumentNullException.ThrowIfNull(libraryIds);

            if (libraryIds.Count == 0)
            {
                return FrozenSet<Guid>.Empty;
            }

            ArgumentNullException.ThrowIfNull(libraryManager);

            return GetPhysicalFolderIdsFromFolders(libraryManager.GetUserRootFolder().Children, libraryIds);
        }

        /// <summary>
        /// Method for determening if text's language and script match any language (ISO 639-1), script (ISO 15924) or lang-script pair
        /// from inclusion list and do not match any from the exclusion list. If codes are individual, omitted code always match.
        /// E.g.: "en-Latn,Cyrl". Match if English language and Latin script or Cyrillic script with any language.
        /// </summary>
        /// <param name="text">Input text.</param>
        /// <param name="textLang">Predetermined text language in 2 letter (ISO 639-1) code.</param>
        /// <param name="langScriptCodesInclude">Comma delimited inclusion list of concatenated with dash or individual language (ISO 639-1) and script (ISO 15924) codes.</param>
        /// <param name="langScriptCodesExclude">Comma delimited exclusion list of concatenated with dash or individual language (ISO 639-1) and script (ISO 15924) codes. Exclusion list has priority over inclusion list.</param>
        /// <returns>True if text matches any language, script or language-script pair from the list.</returns>
        /// <exception cref="InvalidFormatException">Script (ISO 15924) code is invalid.</exception>
        public static bool IsPrefered(string text, string? textLang, IReadOnlyList<string> langScriptCodesInclude, IReadOnlyList<string> langScriptCodesExclude)
        {
            return !string.IsNullOrEmpty(text)
                && (langScriptCodesExclude.Count == 0 || !MatchLangScript(text, textLang, langScriptCodesExclude))
                && (langScriptCodesInclude.Count == 0 || MatchLangScript(text, textLang, langScriptCodesInclude));
        }

        private static bool MatchLangScript(string text, string? textLang, IReadOnlyList<string> langScriptCodes)
        {
            if (langScriptCodes.Count == 0)
            {
                return true;
            }

            foreach (var langScriptCode in langScriptCodes)
            {
                var codeSpan = langScriptCode.AsSpan();

                // parse for language code
                var lang = langScriptCode.Length switch
                {
                    2 => codeSpan,
                    7 => codeSpan.LeftPart('-'),
                    _ => [],
                };

                // if language doesn't match go next
                if (!lang.IsEmpty && (string.IsNullOrEmpty(textLang) || !lang.SequenceEqual(textLang)))
                {
                    continue;
                }

                // parse for script code
                var scriptAbbreviationSpan = langScriptCode.Length switch
                {
                    4 => codeSpan,
                    7 => codeSpan.RightPart('-'),
                    _ => [],
                };

                // if script matches the last condition is met
                if (scriptAbbreviationSpan.IsEmpty)
                {
                    return true;
                }

                var scriptAbbreviation = codeSpan.Length == 4 ? langScriptCode : scriptAbbreviationSpan.ToString();
                var script = UScript.GetCodeFromName(scriptAbbreviation);
                if (script == UScript.InvalidCode)
                {
                    throw new InvalidFormatException($"'{scriptAbbreviation}' is invalid ISO 15924 script code.");
                }

                if (_hybridScriptsLookup.TryGetValue(script, out var scripts))
                {
                    return IsScript(text, scripts);
                }
                else
                {
                    return IsScript(text, script);
                }
            }

            return false;
        }

        private static bool IsScript(string text, int script)
        {
            // check if there is any char not belonging to any script
            foreach (var c in text)
            {
                if (!UScript.HasScript(c, script))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsScript(string text, int[] scripts)
        {
            // check if there is any char not belonging to any script
            foreach (var c in text)
            {
                if (!IsScript(c, scripts))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsScript(char c, int[] scripts)
        {
            foreach (var script in scripts)
            {
                if (UScript.HasScript(c, script))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
