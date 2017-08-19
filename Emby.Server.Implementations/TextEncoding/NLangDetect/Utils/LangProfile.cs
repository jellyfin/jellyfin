using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NLangDetect.Core.Utils
{
    public class LangProfile
    {
        private const int MinimumFreq = 2;
        private const int LessFreqRatio = 100000;

        public string name { get; set; }

        public Dictionary<string, int> freq { get; set; }
        public int[] n_words { get; set; }

        #region Constructor(s)

        public LangProfile()
        {
            freq = new Dictionary<string, int>();
            n_words = new int[NGram.GramsCount];
        }

        public LangProfile(string name)
        {
            this.name = name;
            freq = new Dictionary<string, int>();
            n_words = new int[NGram.GramsCount];
        }

        #endregion

        #region Public methods

        public void Add(string gram)
        {
            if (name == null || gram == null) return; // Illegal
            int len = gram.Length;
            if (len < 1 || len > NGram.GramsCount) return; // Illegal

            n_words[len - 1]++;

            if (freq.ContainsKey(gram))
            {
                freq[gram] = freq[gram] + 1;
            }
            else
            {
                freq.Add(gram, 1);
            }
        }

        public void OmitLessFreq()
        {
            if (name == null) return; // Illegal
            int threshold = n_words[0] / LessFreqRatio;
            if (threshold < MinimumFreq) threshold = MinimumFreq;

            ICollection<string> keys = freq.Keys;
            int roman = 0;
            // TODO IMM HI: move up?
            Regex regex1 = new Regex("^[A-Za-z]$", RegexOptions.Compiled);
            List<string> keysToRemove = new List<string>();

            foreach (string key in keys)
            {
                int count = freq[key];

                if (count <= threshold)
                {
                    n_words[key.Length - 1] -= count;
                    keysToRemove.Add(key);
                }
                else
                {
                    if (regex1.IsMatch(key))
                    {
                        roman += count;
                    }
                }
            }

            foreach (string keyToRemove in keysToRemove)
            {
                freq.Remove(keyToRemove);
            }

            // roman check
            keysToRemove = new List<string>();

            if (roman < n_words[0] / 3)
            {
                ICollection<string> keys2 = freq.Keys;

                // TODO IMM HI: move up?
                Regex regex2 = new Regex(".*[A-Za-z].*", RegexOptions.Compiled);

                foreach (string key in keys2)
                {
                    int count = freq[key];

                    if (regex2.IsMatch(key))
                    {
                        n_words[key.Length - 1] -= count;
                        keysToRemove.Add(key);
                    }
                }

                foreach (string keyToRemove in keysToRemove)
                {
                    freq.Remove(keyToRemove);
                }
            }
        }

        #endregion
    }
}
