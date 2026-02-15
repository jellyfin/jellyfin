using System;
using System.Collections.Generic;
using Jellyfin.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using SharpFuzz;

namespace Jellyfin.Api.Fuzz
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            switch (args[0])
            {
                case "UrlDecodeQueryFeature": Run(UrlDecodeQueryFeature); return;
                default: throw new ArgumentException($"Unknown fuzzing function: {args[0]}");
            }
        }

        private static void Run(Action<string> action) => Fuzzer.OutOfProcess.Run(action);

        private static void UrlDecodeQueryFeature(string data)
        {
            var dict = new Dictionary<string, StringValues>
            {
                { data, StringValues.Empty }
            };
            _ = new UrlDecodeQueryFeature(new QueryFeature(new QueryCollection(dict)));
        }
    }
}
