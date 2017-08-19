using System;
using System.IO.Compression;
using System.Xml;
using NLangDetect.Core.Utils;
using System.IO;

namespace NLangDetect.Core
{
  // TODO IMM HI: xml reader not tested
  public static class GenProfile
  {
    #region Public methods

    public static LangProfile load(string lang, string file)
    {
      LangProfile profile = new LangProfile(lang);
      TagExtractor tagextractor = new TagExtractor("abstract", 100);
      Stream inputStream = null;

      try
      {
        inputStream = File.OpenRead(file);

        string extension = Path.GetExtension(file) ?? "";

        if (extension.ToUpper() == ".GZ")
        {
          inputStream = new GZipStream(inputStream, CompressionMode.Decompress);
        }

        using (XmlReader xmlReader = XmlReader.Create(inputStream))
        {
          while (xmlReader.Read())
          {
            switch (xmlReader.NodeType)
            {
              case XmlNodeType.Element:
                tagextractor.SetTag(xmlReader.Name);
                break;

              case XmlNodeType.Text:
                tagextractor.Add(xmlReader.Value);
                break;

              case XmlNodeType.EndElement:
                tagextractor.CloseTag(profile);
                break;
            }
          }
        }
      }
      finally
      {
        if (inputStream != null)
        {
          inputStream.Close();
        }
      }

      Console.WriteLine(lang + ": " + tagextractor.Count);

      return profile;
    }

    #endregion
  }
}
