using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Emby.Naming.Common;

namespace Emby.Naming.TV
{
    internal static class EpisodePathParserRustInterop
    {
        private const string LibraryName = "jellyfin_naming";

        private static readonly Lazy<NativeMethods> _nativeMethods = new(LoadNativeMethods);

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr ParseJsonDelegate([MarshalAs(UnmanagedType.LPUTF8Str)] string input);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void FreeStringDelegate(IntPtr value);

        internal static EpisodePathParserResult Parse(
            NamingOptions options,
            string path,
            bool isDirectory,
            bool? isNamed,
            bool? isOptimistic,
            bool? supportsAbsoluteNumbers,
            bool fillExtendedInfo)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(path);

            var request = new ParseRequest(
                path,
                isDirectory,
                isNamed,
                isOptimistic,
                supportsAbsoluteNumbers,
                fillExtendedInfo,
                ToExpressionDtos(options.EpisodeExpressions),
                ToExpressionDtos(options.MultipleEpisodeExpressions));

            string requestJson = JsonSerializer.Serialize(request, _jsonOptions);
            IntPtr responsePointer = _nativeMethods.Value.ParseJson(requestJson);

            if (responsePointer == IntPtr.Zero)
            {
                throw new InvalidOperationException("Rust episode path parser returned a null response.");
            }

            try
            {
                string? responseJson = Marshal.PtrToStringUTF8(responsePointer);
                if (string.IsNullOrEmpty(responseJson))
                {
                    throw new InvalidOperationException("Rust episode path parser returned an empty response.");
                }

                var result = JsonSerializer.Deserialize<RustEpisodePathParserResult>(responseJson, _jsonOptions)
                    ?? throw new InvalidOperationException("Rust episode path parser returned an invalid response.");

                if (!string.IsNullOrEmpty(result.Error))
                {
                    throw new InvalidOperationException($"Rust episode path parser failed: {result.Error}");
                }

                return result;
            }
            finally
            {
                _nativeMethods.Value.FreeString(responsePointer);
            }
        }

        private static EpisodeExpressionDto[] ToExpressionDtos(IReadOnlyCollection<EpisodeExpression> expressions)
        {
            var result = new EpisodeExpressionDto[expressions.Count];
            int index = 0;

            foreach (var expression in expressions)
            {
                result[index] = new EpisodeExpressionDto(
                    expression.Expression,
                    expression.IsByDate,
                    expression.IsOptimistic,
                    expression.IsNamed,
                    expression.SupportsAbsoluteEpisodeNumbers,
                    expression.DateTimeFormats);
                index++;
            }

            return result;
        }

        private static NativeMethods LoadNativeMethods()
        {
            IntPtr libraryHandle = LoadLibrary();
            IntPtr parseJson = NativeLibrary.GetExport(libraryHandle, "jellyfin_episode_path_parse_json");
            IntPtr freeString = NativeLibrary.GetExport(libraryHandle, "jellyfin_free_string");

            return new NativeMethods(
                Marshal.GetDelegateForFunctionPointer<ParseJsonDelegate>(parseJson),
                Marshal.GetDelegateForFunctionPointer<FreeStringDelegate>(freeString));
        }

        private static IntPtr LoadLibrary()
        {
            if (NativeLibrary.TryLoad(LibraryName, typeof(EpisodePathParserRustInterop).Assembly, null, out IntPtr handle))
            {
                return handle;
            }

            string baseDirectory = AppContext.BaseDirectory;
            foreach (string candidate in GetLibraryCandidates(baseDirectory))
            {
                if (File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out handle))
                {
                    return handle;
                }
            }

            throw new DllNotFoundException($"Unable to load the Rust episode path parser native library from '{baseDirectory}'.");
        }

        private static IEnumerable<string> GetLibraryCandidates(string baseDirectory)
        {
            if (OperatingSystem.IsWindows())
            {
                yield return Path.Combine(baseDirectory, $"{LibraryName}.dll");
            }
            else if (OperatingSystem.IsMacOS())
            {
                yield return Path.Combine(baseDirectory, $"lib{LibraryName}.dylib");
            }
            else
            {
                yield return Path.Combine(baseDirectory, $"lib{LibraryName}.so");
            }
        }

        private sealed record NativeMethods(ParseJsonDelegate ParseJson, FreeStringDelegate FreeString);

        private sealed record ParseRequest(
            string Path,
            bool IsDirectory,
            bool? IsNamed,
            bool? IsOptimistic,
            bool? SupportsAbsoluteNumbers,
            bool FillExtendedInfo,
            EpisodeExpressionDto[] EpisodeExpressions,
            EpisodeExpressionDto[] MultipleEpisodeExpressions);

        private sealed record EpisodeExpressionDto(
            string Expression,
            bool IsByDate,
            bool IsOptimistic,
            bool IsNamed,
            bool SupportsAbsoluteEpisodeNumbers,
            string[] DateTimeFormats);

        private sealed class RustEpisodePathParserResult : EpisodePathParserResult
        {
            public string? Error { get; set; }
        }
    }
}
