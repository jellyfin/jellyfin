using System;
using System.Collections;
using System.Collections.Generic;
using MediaBrowser.MediaEncoding.Encoder;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.MediaEncoding.Tests
{
    public class EncoderValidatorTests
    {
        [Theory]
        [ClassData(typeof(GetFFmpegVersionTestData))]
        public void GetFFmpegVersionTest(string versionOutput, Version? version)
        {
            var val = new EncoderValidator(new NullLogger<EncoderValidatorTests>());
            Assert.Equal(version, val.GetFFmpegVersion(versionOutput));
        }

        [Theory]
        [InlineData(EncoderValidatorTestsData.FFmpegV431Output, true)]
        [InlineData(EncoderValidatorTestsData.FFmpegV43Output, true)]
        [InlineData(EncoderValidatorTestsData.FFmpegV421Output, true)]
        [InlineData(EncoderValidatorTestsData.FFmpegV42Output, true)]
        [InlineData(EncoderValidatorTestsData.FFmpegV414Output, true)]
        [InlineData(EncoderValidatorTestsData.FFmpegV404Output, true)]
        [InlineData(EncoderValidatorTestsData.FFmpegGitUnknownOutput2, true)]
        [InlineData(EncoderValidatorTestsData.FFmpegGitUnknownOutput, false)]
        public void ValidateVersionInternalTest(string versionOutput, bool valid)
        {
            var val = new EncoderValidator(new NullLogger<EncoderValidatorTests>());
            Assert.Equal(valid, val.ValidateVersionInternal(versionOutput));
        }

        private class GetFFmpegVersionTestData : IEnumerable<object?[]>
        {
            public IEnumerator<object?[]> GetEnumerator()
            {
                yield return new object?[] { EncoderValidatorTestsData.FFmpegV431Output, new Version(4, 3, 1) };
                yield return new object?[] { EncoderValidatorTestsData.FFmpegV43Output, new Version(4, 3) };
                yield return new object?[] { EncoderValidatorTestsData.FFmpegV421Output, new Version(4, 2, 1) };
                yield return new object?[] { EncoderValidatorTestsData.FFmpegV42Output, new Version(4, 2) };
                yield return new object?[] { EncoderValidatorTestsData.FFmpegV414Output, new Version(4, 1, 4) };
                yield return new object?[] { EncoderValidatorTestsData.FFmpegV404Output, new Version(4, 0, 4) };
                yield return new object?[] { EncoderValidatorTestsData.FFmpegGitUnknownOutput2, new Version(4, 0) };
                yield return new object?[] { EncoderValidatorTestsData.FFmpegGitUnknownOutput, null };
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
