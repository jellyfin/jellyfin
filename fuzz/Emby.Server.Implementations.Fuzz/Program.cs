using System;
using Emby.Server.Implementations.Library;
using SharpFuzz;

namespace Emby.Server.Implementations.Fuzz
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            switch (args[0])
            {
                case "PathExtensions.TryReplaceSubPath": Run(PathExtensions_TryReplaceSubPath); return;
                default: throw new ArgumentException($"Unknown fuzzing function: {args[0]}");
            }
        }

        private static void Run(Action<string> action) => Fuzzer.OutOfProcess.Run(action);

        private static void PathExtensions_TryReplaceSubPath(string data)
        {
            // Stupid, but it worked
            var parts = data.Split(':');
            if (parts.Length != 3)
            {
                return;
            }

            _ = PathExtensions.TryReplaceSubPath(parts[0], parts[1], parts[2], out _);
        }
    }
}
