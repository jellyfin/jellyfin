using System.Collections.Generic;
using System.Linq;
using Prometheus;

namespace Jellyfin.Server.Implementations.Metrics
{
    internal static class MetricsSnapshot
    {
        private const char LabelSeparator = '\x1f';

        public static void SetSnapshot(Gauge gauge, Dictionary<string, double> snapshot, HashSet<string> previousKeys)
        {
            foreach (var (key, value) in snapshot)
            {
                gauge.WithLabels(Split(key)).Set(value);
            }

            foreach (var key in previousKeys.Where(key => !snapshot.ContainsKey(key)))
            {
                gauge.WithLabels(Split(key)).Set(0);
            }

            previousKeys.Clear();
            previousKeys.UnionWith(snapshot.Keys);
        }

        public static string Sanitize(string? value)
            => string.IsNullOrEmpty(value) ? "unknown" : value.Replace(LabelSeparator, '_');

        public static string Join(params string[] values)
            => string.Join(LabelSeparator, values);

        private static string[] Split(string key)
            => key.Split(LabelSeparator);
    }
}
