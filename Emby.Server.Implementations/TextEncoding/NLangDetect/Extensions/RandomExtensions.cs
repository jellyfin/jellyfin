using System;

namespace NLangDetect.Core.Extensions
{
  public static class RandomExtensions
  {
    private const double _Epsilon = 2.22044604925031E-15;

    private static readonly object _mutex = new object();

    private static double _nextNextGaussian;
    private static bool _hasNextNextGaussian;

    /// <summary>
    /// Returns the next pseudorandom, Gaussian ("normally") distributed double value with mean 0.0 and standard deviation 1.0 from this random number generator's sequence.
    /// The general contract of nextGaussian is that one double value, chosen from (approximately) the usual normal distribution with mean 0.0 and standard deviation 1.0, is pseudorandomly generated and returned.
    /// </summary>
    /// <remarks>
    /// Taken from: http://download.oracle.com/javase/6/docs/api/java/util/Random.html (nextGaussian())
    /// </remarks>
    public static double NextGaussian(this Random random)
    {
      lock (_mutex)
      {
        if (_hasNextNextGaussian)
        {
          _hasNextNextGaussian = false;

          return _nextNextGaussian;
        }

        double v1, v2, s;

        do
        {
          v1 = 2.0 * random.NextDouble() - 1.0; // between -1.0 and 1.0
          v2 = 2.0 * random.NextDouble() - 1.0; // between -1.0 and 1.0
          s = v1 * v1 + v2 * v2;
        }
        while (s >= 1.0 || Math.Abs(s - 0.0) < _Epsilon);

        double multiplier = Math.Sqrt(-2.0 * Math.Log(s) / s);

        _nextNextGaussian = v2 * multiplier;
        _hasNextNextGaussian = true;

        return v1 * multiplier;
      }
    }
  }
}
