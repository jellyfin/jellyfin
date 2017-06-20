using System;
using System.Text;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Logging;
using UniversalDetector;
using NLangDetect.Core;
using MediaBrowser.Model.Serialization;

namespace Emby.Common.Implementations.TextEncoding
{
    public class TextEncoding : ITextEncoding
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;
        private IJsonSerializer _json;

        public TextEncoding(IFileSystem fileSystem, ILogger logger, IJsonSerializer json)
        {
            _fileSystem = fileSystem;
            _logger = logger;
            _json = json;
        }

        public Encoding GetASCIIEncoding()
        {
            return Encoding.ASCII;
        }

        private Encoding GetInitialEncoding(byte[] buffer)
        {
            if (buffer[0] == 0xef && buffer[1] == 0xbb && buffer[2] == 0xbf)
                return Encoding.UTF8;
            if (buffer[0] == 0xfe && buffer[1] == 0xff)
                return Encoding.Unicode;
            if (buffer[0] == 0 && buffer[1] == 0 && buffer[2] == 0xfe && buffer[3] == 0xff)
                return Encoding.UTF32;
            if (buffer[0] == 0x2b && buffer[1] == 0x2f && buffer[2] == 0x76)
                return Encoding.UTF7;

            var result = new TextEncodingDetect().DetectEncoding(buffer, buffer.Length);

            switch (result)
            {
                case TextEncodingDetect.CharacterEncoding.Ansi:
                    return Encoding.ASCII;
                case TextEncodingDetect.CharacterEncoding.Ascii:
                    return Encoding.ASCII;
                case TextEncodingDetect.CharacterEncoding.Utf16BeBom:
                    return Encoding.UTF32;
                case TextEncodingDetect.CharacterEncoding.Utf16BeNoBom:
                    return Encoding.UTF32;
                case TextEncodingDetect.CharacterEncoding.Utf16LeBom:
                    return Encoding.UTF32;
                case TextEncodingDetect.CharacterEncoding.Utf16LeNoBom:
                    return Encoding.UTF32;
                case TextEncodingDetect.CharacterEncoding.Utf8Bom:
                    return Encoding.UTF8;
                case TextEncodingDetect.CharacterEncoding.Utf8Nobom:
                    return Encoding.UTF8;
                default:
                    return null;
            }
        }

        private bool _langDetectInitialized;
        public string GetDetectedEncodingName(byte[] bytes, string language, bool enableLanguageDetection)
        {
            var encoding = GetInitialEncoding(bytes);

            if (encoding != null && encoding.Equals(Encoding.UTF8))
            {
                return "utf-8";
            }

            if (string.IsNullOrWhiteSpace(language) && enableLanguageDetection)
            {
                if (!_langDetectInitialized)
                {
                    _langDetectInitialized = true;
                    LanguageDetector.Initialize(_json);
                }

                language = DetectLanguage(bytes);

                if (!string.IsNullOrWhiteSpace(language))
                {
                    _logger.Debug("Text language detected as {0}", language);
                }
            }

            var charset = DetectCharset(bytes, language);

            if (!string.IsNullOrWhiteSpace(charset))
            {
                if (string.Equals(charset, "utf-8", StringComparison.OrdinalIgnoreCase))
                {
                    return "utf-8";
                }

                if (!string.Equals(charset, "windows-1252", StringComparison.OrdinalIgnoreCase))
                {
                    return charset;
                }
            }

            if (!string.IsNullOrWhiteSpace(language))
            {
                return GetFileCharacterSetFromLanguage(language);
            }

            return null;
        }

        private string DetectLanguage(byte[] bytes)
        {
            try
            {
                return LanguageDetector.DetectLanguage(Encoding.UTF8.GetString(bytes));
            }
            catch (NLangDetectException ex)
            {
            }

            try
            {
                return LanguageDetector.DetectLanguage(Encoding.ASCII.GetString(bytes));
            }
            catch (NLangDetectException ex)
            {
            }

            try
            {
                return LanguageDetector.DetectLanguage(Encoding.Unicode.GetString(bytes));
            }
            catch (NLangDetectException ex)
            {
            }

            return null;
        }

        public Encoding GetEncodingFromCharset(string charset)
        {
            if (string.IsNullOrWhiteSpace(charset))
            {
                throw new ArgumentNullException("charset");
            }

            _logger.Debug("Getting encoding object for character set: {0}", charset);

            try
            {
                return Encoding.GetEncoding(charset);
            }
            catch (ArgumentException)
            {
                charset = charset.Replace("-", string.Empty);
                _logger.Debug("Getting encoding object for character set: {0}", charset);

                return Encoding.GetEncoding(charset);
            }
        }

        public Encoding GetDetectedEncoding(byte[] bytes, string language, bool enableLanguageDetection)
        {
            var charset = GetDetectedEncodingName(bytes, language, enableLanguageDetection);

            return GetEncodingFromCharset(charset);
        }

        private string GetFileCharacterSetFromLanguage(string language)
        {
            // https://developer.xamarin.com/api/type/System.Text.Encoding/

            switch (language.ToLower())
            {
                case "hun":
                    return "windows-1252";
                case "pol":
                case "cze":
                case "ces":
                case "slo":
                case "srp":
                case "hrv":
                case "rum":
                case "ron":
                case "rup":
                    return "windows-1250";
                // albanian
                case "alb":
                case "sqi":
                    return "windows-1250";
                // slovak
                case "slk":
                case "slv":
                    return "windows-1250";
                case "ara":
                    return "windows-1256";
                case "heb":
                    return "windows-1255";
                case "grc":
                    return "windows-1253";
                // greek
                case "gre":
                case "ell":
                    return "windows-1253";
                case "crh":
                case "ota":
                case "tur":
                    return "windows-1254";
                // bulgarian
                case "bul":
                case "bgr":
                    return "windows-1251";
                case "rus":
                    return "windows-1251";
                case "vie":
                    return "windows-1258";
                case "kor":
                    return "cp949";
                default:
                    return "windows-1252";
            }
        }

        private string DetectCharset(byte[] bytes, string language)
        {
            var detector = new CharsetDetector();
            detector.Feed(bytes, 0, bytes.Length);
            detector.DataEnd();

            var charset = detector.Charset;

            // This is often incorrectly indetected. If this happens, try to use other techniques instead
            if (string.Equals("x-mac-cyrillic", charset, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(language))
                {
                    return null;
                }
            }

            return charset;
        }
    }
}
