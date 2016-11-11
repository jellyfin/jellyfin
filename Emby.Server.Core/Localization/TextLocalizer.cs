using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Emby.Server.Implementations.Localization;

namespace Emby.Server.Core.Localization
{
    public class TextLocalizer : ITextLocalizer
    {
        public string RemoveDiacritics(string text)
        {
            return String.Concat(
                text.Normalize(NormalizationForm.FormD)
                .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) !=
                                              UnicodeCategory.NonSpacingMark)
              ).Normalize(NormalizationForm.FormC);
        }

        public string NormalizeFormKD(string text)
        {
            return text.Normalize(NormalizationForm.FormKD);
        }
    }
}
