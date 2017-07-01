using System;
using MediaBrowser.Model.Serialization;

namespace NLangDetect.Core
{
    // TODO IMM HI: change to non-static class
    // TODO IMM HI: hide other, unnecassary classes via internal?
    public static class LanguageDetector
    {
        private const double _DefaultAlpha = 0.5;

        #region Public methods

        public static void Initialize(IJsonSerializer json)
        {
            DetectorFactory.LoadProfiles(json);
        }

        public static void Release()
        {
            DetectorFactory.Clear();
        }

        public static string DetectLanguage(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) { throw new ArgumentException("Argument can't be null nor empty.", "plainText"); }

            Detector detector = DetectorFactory.Create(_DefaultAlpha);

            detector.Append(plainText);

            return detector.Detect();
        }

        #endregion
    }
}
