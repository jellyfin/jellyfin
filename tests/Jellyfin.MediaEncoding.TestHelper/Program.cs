using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace Jellyfin.MediaEncoding.TestHelper;

/// <summary>
/// A deliberately stalled ffprobe stand-in for process ownership tests.
/// </summary>
internal static class Program
{
    private static async Task Main(string[] args)
    {
        var input = args[Array.IndexOf(args, "-i") + 1];
        if (input.StartsWith("file:", StringComparison.Ordinal))
        {
            input = input["file:".Length..];
        }

        var folder = Path.GetDirectoryName(input)!;
        await File.WriteAllTextAsync(Path.Combine(folder, "pid"), Environment.ProcessId.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
        if (Path.GetFileName(input) == "invalid")
        {
            while (!File.Exists(Path.Combine(folder, "release")))
            {
                await Task.Delay(20).ConfigureAwait(false);
            }

            Console.Write("{}");
            return;
        }

        await Task.Delay(TimeSpan.FromMinutes(1)).ConfigureAwait(false);
    }
}
