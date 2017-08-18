using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using NLangDetect.Core.Utils;
using MediaBrowser.Model.Serialization;
using System.Linq;

namespace NLangDetect.Core
{
    public class DetectorFactory
    {
        public Dictionary<string, ProbVector> WordLangProbMap;
        public List<string> Langlist;

        private static readonly DetectorFactory _instance = new DetectorFactory();

        #region Constructor(s)

        private DetectorFactory()
        {
            WordLangProbMap = new Dictionary<string, ProbVector>();
            Langlist = new List<string>();
        }

        #endregion

        #region Public methods

        public static void LoadProfiles(IJsonSerializer json)
        {
            var assembly = typeof(DetectorFactory).Assembly;
            var names = assembly.GetManifestResourceNames()
                      .Where(i => i.IndexOf("NLangDetect.Profiles", StringComparison.Ordinal) != -1)
                      .ToList();

            var index = 0;

            foreach (var name in names)
            {
                using (var stream = assembly.GetManifestResourceStream(name))
                {
                    var langProfile = (LangProfile)json.DeserializeFromStream(stream, typeof(LangProfile));

                    AddProfile(langProfile, index);
                }

                index++;
            }
        }

        public static Detector Create()
        {
            return CreateDetector();
        }

        public static Detector Create(double alpha)
        {
            Detector detector = CreateDetector();

            detector.SetAlpha(alpha);

            return detector;
        }

        public static void SetSeed(int? seed)
        {
            _instance.Seed = seed;
        }

        #endregion

        #region Internal methods

        internal static void AddProfile(LangProfile profile, int index)
        {
            var lang = profile.name;

            if (_instance.Langlist.Contains(lang))
            {
                throw new NLangDetectException("duplicate the same language profile", ErrorCode.DuplicateLangError);
            }

            _instance.Langlist.Add(lang);

            foreach (string word in profile.freq.Keys)
            {
                if (!_instance.WordLangProbMap.ContainsKey(word))
                {
                    _instance.WordLangProbMap.Add(word, new ProbVector());
                }

                double prob = (double)profile.freq[word] / profile.n_words[word.Length - 1];

                _instance.WordLangProbMap[word][index] = prob;
            }
        }

        internal static void Clear()
        {
            _instance.Langlist.Clear();
            _instance.WordLangProbMap.Clear();
        }

        #endregion

        #region Private helper methods

        private static Detector CreateDetector()
        {
            if (_instance.Langlist.Count == 0)
            {
                throw new NLangDetectException("need to load profiles", ErrorCode.NeedLoadProfileError);
            }

            return new Detector(_instance);
        }

        #endregion

        #region Properties

        public int? Seed { get; private set; }

        #endregion
    }
}
