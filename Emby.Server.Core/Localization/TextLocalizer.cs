using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Emby.Server.Implementations.Localization;

namespace Emby.Server.Core.Localization
{
    public class TextLocalizer : ITextLocalizer
    {
        public string RemoveDiacritics(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            var chars = Normalize(text, NormalizationForm.FormD)
                .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark);

            return Normalize(String.Concat(chars), NormalizationForm.FormC);
        }

        private static string Normalize(string text, NormalizationForm form, bool stripStringOnFailure = true)
        {
            if (stripStringOnFailure)
            {
                try
                {
                    return text.Normalize(form);
                }
                catch (ArgumentException)
                {
                    // will throw if input contains invalid unicode chars
                    // https://mnaoumov.wordpress.com/2014/06/14/stripping-invalid-characters-from-utf-16-strings/   
                    text = StripInvalidUnicodeCharacters(text);
                    return Normalize(text, form, false);
                }
            }

            try
            {
                return text.Normalize(form);
            }
            catch (ArgumentException)
            {
                // if it still fails, return the original text
                return text;
            }
        }

        private static string StripInvalidUnicodeCharacters(string str)
        {
            var invalidCharactersRegex = new Regex("([\ud800-\udbff](?![\udc00-\udfff]))|((?<![\ud800-\udbff])[\udc00-\udfff])");
            return invalidCharactersRegex.Replace(str, "");
        }

        public string NormalizeFormKD(string text)
        {
            return text.Normalize(NormalizationForm.FormKD);
        }
    }
}
