using System;
using System.Collections.Generic;

namespace NLangDetect.Core
{
  public class ProbVector
  {
    private readonly Dictionary<int, double> _dict = new Dictionary<int, double>();

    public double this[int key]
    {
      get
      {
        double value;

        return _dict.TryGetValue(key, out value) ? value : 0.0;
      }

      set
      {
        if (Math.Abs(value) < double.Epsilon)
        {
          if (_dict.ContainsKey(key))
          {
            _dict.Remove(key);
          }

          return;
        }

        _dict[key] = value;
      }
    }
  }
}
