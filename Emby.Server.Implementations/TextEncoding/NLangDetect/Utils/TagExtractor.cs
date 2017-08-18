using System.Text;

namespace NLangDetect.Core.Utils
{
  public class TagExtractor
  {
    // TODO IMM HI: do the really need to be internal?
    internal string Target;
    internal int Threshold;
    internal StringBuilder StringBuilder;
    internal string Tag;

    #region Constructor(s)

    public TagExtractor(string tag, int threshold)
    {
      Target = tag;
      Threshold = threshold;
      Count = 0;
      Clear();
    }

    #endregion

    #region Public methods

    public void Clear()
    {
      StringBuilder = new StringBuilder();
      Tag = null;
    }

    public void SetTag(string tag)
    {
      Tag = tag;
    }

    public void Add(string line)
    {
      if (Tag == Target && line != null)
      {
        StringBuilder.Append(line);
      }
    }

    public void CloseTag(LangProfile profile)
    {
      if (profile != null && Tag == Target && StringBuilder.Length > Threshold)
      {
        var gram = new NGram();

        for (int i = 0; i < StringBuilder.Length; i++)
        {
          gram.AddChar(StringBuilder[i]);

          for (int n = 1; n <= NGram.GramsCount; n++)
          {
            profile.Add(gram.Get(n));
          }
        }

        Count++;
      }

      Clear();
    }

    #endregion

    #region Properties

    public int Count { get; private set; }

    #endregion
  }
}
