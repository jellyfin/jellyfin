using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Jellyfin.Server.Tests
{
    public class ProgramTests
    {
        private static readonly SemaphoreSlim _consoleLock = new(1, 1);

        [Theory]
        [InlineData("--help")]
        [InlineData("--version")]
        public async Task Main_WhenBuiltInOptionFollowsParsedOption_DisplaysOutput(string builtInOption)
        {
            await _consoleLock.WaitAsync(TestContext.Current.CancellationToken);
            try
            {
                TextWriter originalOut = Console.Out;
                TextWriter originalError = Console.Error;
                int originalExitCode = Environment.ExitCode;

                using var outputWriter = new StringWriter(CultureInfo.InvariantCulture);
                using var errorWriter = new StringWriter(CultureInfo.InvariantCulture);
                string output;
                string error;
                int exitCode;

                try
                {
                    Environment.ExitCode = 1;
                    Console.SetOut(outputWriter);
                    Console.SetError(errorWriter);

                    await Program.Main(new[] { "--ffmpeg", Path.Combine(Path.GetTempPath(), "ffmpeg"), builtInOption });

                    output = outputWriter.ToString();
                    error = errorWriter.ToString();
                    exitCode = Environment.ExitCode;
                }
                finally
                {
                    Console.SetOut(originalOut);
                    Console.SetError(originalError);
                    Environment.ExitCode = originalExitCode;
                }

                Assert.Equal(0, exitCode);
                Assert.NotEmpty(output);
                Assert.Empty(error);
            }
            finally
            {
                _consoleLock.Release();
            }
        }
    }
}
