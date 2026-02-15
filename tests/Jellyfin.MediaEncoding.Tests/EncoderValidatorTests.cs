using System;
using MediaBrowser.MediaEncoding.Encoder;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.MediaEncoding.Tests
{
    public class EncoderValidatorTests
    {
        private readonly EncoderValidator _encoderValidator = new EncoderValidator(new NullLogger<EncoderValidatorTests>(), "ffmpeg");

        [Theory]
        [ClassData(typeof(GetFFmpegVersionTestData))]
        public void GetFFmpegVersionTest(string versionOutput, Version? version)
        {
            Assert.Equal(version, _encoderValidator.GetFFmpegVersionInternal(versionOutput));
        }

        [Theory]
        [InlineData(EncoderValidatorTestsData.FFmpegV701Output, true)]
        [InlineData(EncoderValidatorTestsData.FFmpegV611Output, true)]
        [InlineData(EncoderValidatorTestsData.FFmpegV60Output, true)]
        [InlineData(EncoderValidatorTestsData.FFmpegV512Output, true)]
        [InlineData(EncoderValidatorTestsData.FFmpegV44Output, true)]
        [InlineData(EncoderValidatorTestsData.FFmpegV432Output, false)]
        [InlineData(EncoderValidatorTestsData.FFmpegGitUnknownOutput2, true)]
        [InlineData(EncoderValidatorTestsData.FFmpegGitUnknownOutput, false)]
        public void ValidateVersionInternalTest(string versionOutput, bool valid)
        {
            Assert.Equal(valid, _encoderValidator.ValidateVersionInternal(versionOutput));
        }

        private sealed class GetFFmpegVersionTestData : TheoryData<string, Version?>
        {
            public GetFFmpegVersionTestData()
            {
                Add(EncoderValidatorTestsData.FFmpegV701Output, new Version(7, 0, 1));
                Add(EncoderValidatorTestsData.FFmpegV611Output, new Version(6, 1, 1));
                Add(EncoderValidatorTestsData.FFmpegV60Output, new Version(6, 0));
                Add(EncoderValidatorTestsData.FFmpegV512Output, new Version(5, 1, 2));
                Add(EncoderValidatorTestsData.FFmpegV44Output, new Version(4, 4));
                Add(EncoderValidatorTestsData.FFmpegV432Output, new Version(4, 3, 2));
                Add(EncoderValidatorTestsData.FFmpegGitUnknownOutput2, new Version(4, 4));
                Add(EncoderValidatorTestsData.FFmpegGitUnknownOutput, null);
            }
        }
    }
}
