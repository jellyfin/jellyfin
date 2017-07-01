using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NLangDetect.Core.Extensions;
using NLangDetect.Core.Utils;

namespace NLangDetect.Core
{
    public class Detector
    {
        private const double _AlphaDefault = 0.5;
        private const double _AlphaWidth = 0.05;

        private const int _IterationLimit = 1000;
        private const double _ProbThreshold = 0.1;
        private const double _ConvThreshold = 0.99999;
        private const int _BaseFreq = 10000;

        private static readonly Regex _UrlRegex = new Regex("https?://[-_.?&~;+=/#0-9A-Za-z]+", RegexOptions.Compiled);
        private static readonly Regex _MailRegex = new Regex("[-_.0-9A-Za-z]+@[-_0-9A-Za-z]+[-_.0-9A-Za-z]+", RegexOptions.Compiled);

        private readonly Dictionary<string, ProbVector> _wordLangProbMap;
        private readonly List<string> _langlist;

        private StringBuilder _text;
        private double[] _langprob;

        private double _alpha = _AlphaDefault;
        private const int _trialsCount = 7;
        private int _maxTextLength = 10000;
        private double[] _priorMap;
        private int? _seed;

        #region Constructor(s)

        public Detector(DetectorFactory factory)
        {
            _wordLangProbMap = factory.WordLangProbMap;
            _langlist = factory.Langlist;
            _text = new StringBuilder();
            _seed = factory.Seed;
        }

        #endregion

        #region Public methods

        public void SetAlpha(double alpha)
        {
            _alpha = alpha;
        }

        public void SetPriorMap(Dictionary<string, double> priorMap)
        {
            _priorMap = new double[_langlist.Count];

            double sump = 0;

            for (int i = 0; i < _priorMap.Length; i++)
            {
                string lang = _langlist[i];

                if (priorMap.ContainsKey(lang))
                {
                    double p = priorMap[lang];

                    if (p < 0)
                    {
                        throw new NLangDetectException("Prior probability must be non-negative.", ErrorCode.InitParamError);
                    }

                    _priorMap[i] = p;
                    sump += p;
                }
            }

            if (sump <= 0)
            {
                throw new NLangDetectException("More one of prior probability must be non-zero.", ErrorCode.InitParamError);
            }

            for (int i = 0; i < _priorMap.Length; i++)
            {
                _priorMap[i] /= sump;
            }
        }

        public void SetMaxTextLength(int max_text_length)
        {
            _maxTextLength = max_text_length;
        }

        // TODO IMM HI: TextReader?
        public void Append(StreamReader streamReader)
        {
            var buf = new char[_maxTextLength / 2];

            while (_text.Length < _maxTextLength && !streamReader.EndOfStream)
            {
                int length = streamReader.Read(buf, 0, buf.Length);

                Append(new string(buf, 0, length));
            }
        }

        public void Append(string text)
        {
            text = _UrlRegex.Replace(text, " ");
            text = _MailRegex.Replace(text, " ");

            char pre = '\0';

            for (int i = 0; i < text.Length && i < _maxTextLength; i++)
            {
                char c = NGram.Normalize(text[i]);

                if (c != ' ' || pre != ' ')
                {
                    _text.Append(c);
                }

                pre = c;
            }
        }

        private void CleanText()
        {
            int latinCount = 0, nonLatinCount = 0;

            for (int i = 0; i < _text.Length; i++)
            {
                char c = _text[i];

                if (c <= 'z' && c >= 'A')
                {
                    latinCount++;
                }
                else if (c >= '\u0300' && c.GetUnicodeBlock() != UnicodeBlock.LatinExtendedAdditional)
                {
                    nonLatinCount++;
                }
            }

            if (latinCount * 2 < nonLatinCount)
            {
                var textWithoutLatin = new StringBuilder();

                for (int i = 0; i < _text.Length; i++)
                {
                    char c = _text[i];

                    if (c > 'z' || c < 'A')
                    {
                        textWithoutLatin.Append(c);
                    }
                }

                _text = textWithoutLatin;
            }
        }

        public string Detect()
        {
            List<Language> probabilities = GetProbabilities();

            return
              probabilities.Count > 0
                ? probabilities[0].Name
                : null;
        }

        public List<Language> GetProbabilities()
        {
            if (_langprob == null)
            {
                DetectBlock();
            }

            List<Language> list = SortProbability(_langprob);

            return list;
        }

        #endregion

        #region Private helper methods

        private static double NormalizeProb(double[] probs)
        {
            double maxp = 0, sump = 0;

            sump += probs.Sum();

            for (int i = 0; i < probs.Length; i++)
            {
                double p = probs[i] / sump;

                if (maxp < p)
                {
                    maxp = p;
                }

                probs[i] = p;
            }

            return maxp;
        }

        private static string UnicodeEncode(string word)
        {
            var resultSb = new StringBuilder();

            foreach (char ch in word)
            {
                if (ch >= '\u0080')
                {
                    string st = string.Format("{0:x}", 0x10000 + ch);

                    while (st.Length < 4)
                    {
                        st = "0" + st;
                    }

                    resultSb
                      .Append("\\u")
                      .Append(st.SubSequence(1, 5));
                }
                else
                {
                    resultSb.Append(ch);
                }
            }

            return resultSb.ToString();
        }

        private void DetectBlock()
        {
            CleanText();

            List<string> ngrams = ExtractNGrams();

            if (ngrams.Count == 0)
            {
                throw new NLangDetectException("no features in text", ErrorCode.CantDetectError);
            }

            _langprob = new double[_langlist.Count];

            Random rand = (_seed.HasValue ? new Random(_seed.Value) : new Random());

            for (int t = 0; t < _trialsCount; t++)
            {
                double[] prob = InitProbability();

                // TODO IMM HI: verify it works
                double alpha = _alpha + rand.NextGaussian() * _AlphaWidth;

                for (int i = 0; ; i++)
                {
                    int r = rand.Next(ngrams.Count);

                    UpdateLangProb(prob, ngrams[r], alpha);

                    if (i % 5 == 0)
                    {
                        if (NormalizeProb(prob) > _ConvThreshold || i >= _IterationLimit)
                        {
                            break;
                        }
                    }
                }

                for (int j = 0; j < _langprob.Length; j++)
                {
                    _langprob[j] += prob[j] / _trialsCount;
                }
            }
        }

        private double[] InitProbability()
        {
            var prob = new double[_langlist.Count];

            if (_priorMap != null)
            {
                for (int i = 0; i < prob.Length; i++)
                {
                    prob[i] = _priorMap[i];
                }
            }
            else
            {
                for (int i = 0; i < prob.Length; i++)
                {
                    prob[i] = 1.0 / _langlist.Count;
                }
            }
            return prob;
        }

        private List<string> ExtractNGrams()
        {
            var list = new List<string>();
            NGram ngram = new NGram();

            for (int i = 0; i < _text.Length; i++)
            {
                ngram.AddChar(_text[i]);

                for (int n = 1; n <= NGram.GramsCount; n++)
                {
                    string w = ngram.Get(n);

                    if (w != null && _wordLangProbMap.ContainsKey(w))
                    {
                        list.Add(w);
                    }
                }
            }

            return list;
        }

        private void UpdateLangProb(double[] prob, string word, double alpha)
        {
            if (word == null || !_wordLangProbMap.ContainsKey(word))
            {
                return;
            }

            ProbVector langProbMap = _wordLangProbMap[word];
            double weight = alpha / _BaseFreq;

            for (int i = 0; i < prob.Length; i++)
            {
                prob[i] *= weight + langProbMap[i];
            }
        }

        private List<Language> SortProbability(double[] prob)
        {
            var list = new List<Language>();

            for (int j = 0; j < prob.Length; j++)
            {
                double p = prob[j];

                if (p > _ProbThreshold)
                {
                    for (int i = 0; i <= list.Count; i++)
                    {
                        if (i == list.Count || list[i].Probability < p)
                        {
                            list.Insert(i, new Language(_langlist[j], p));

                            break;
                        }
                    }
                }
            }

            return list;
        }

        #endregion
    }
}
