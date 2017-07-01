using System.Globalization;

namespace NLangDetect.Core
{
  // TODO IMM HI: name??
  public class Language
  {
    #region Constructor(s)

    public Language(string name, double probability)
    {
      Name = name;
      Probability = probability;
    }

    #endregion

    #region Object overrides

    public override string ToString()
    {
      if (Name == null)
      {
        return "";
      }

      return
        string.Format(
          CultureInfo.InvariantCulture.NumberFormat,
          "{0}:{1:0.000000}",
          Name,
          Probability);
    }

    #endregion

    #region Properties

    public string Name { get; set; }

    public double Probability { get; set; }

    #endregion
  }
}
