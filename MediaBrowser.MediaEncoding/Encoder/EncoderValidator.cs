using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using MediaBrowser.Model.Diagnostics;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.MediaEncoding.Encoder
{
    public class FFmpegVersion
    {
        private int _version;
        private int _multi => 100;
        private const int _unknown = 0;
        private const int _experimental = -1;

        public FFmpegVersion(int p1)
        {
            _version = p1;
        }

        public FFmpegVersion(string p1)
        {
            var match = Regex.Match(p1, @"(?<major>\d+)\.(?<minor>\d+)");

            if (match.Groups["major"].Success && match.Groups["minor"].Success)
            {
                int major = int.Parse(match.Groups["major"].Value);
                int minor = int.Parse(match.Groups["minor"].Value);
                _version = (major * _multi) + minor;
            }
        }

        public override string ToString()
        {
            switch (_version)
            {
                case _unknown:
                    return "Unknown";
                case _experimental:
                    return "Experimental";
                default:
                    string major = Convert.ToString(_version / _multi);
                    string minor = Convert.ToString(_version % _multi);
                    return string.Concat(major, ".", minor);
            }
        }

        public bool Unknown()
        {
            return _version == _unknown;
        }

        public int Version()
        {
            return _version;
        }

        public bool Experimental()
        {
            return _version == _experimental;
        }

        public bool Below(FFmpegVersion checkAgainst)
        {
            return (_version > 0) && (_version < checkAgainst._version);
        }

        public bool Suitable(FFmpegVersion checkAgainst)
        {
            return (_version > 0) && (_version >= checkAgainst._version);
        }
    }

    public class EncoderValidator
    {
        private readonly ILogger _logger;
        private readonly IProcessFactory _processFactory;

        public EncoderValidator(ILogger logger, IProcessFactory processFactory)
        {
            _logger = logger;
            _processFactory = processFactory;
        }

        public (IEnumerable<string> decoders, IEnumerable<string> encoders) Validate(string encoderPath)
        {
            _logger.LogInformation("Validating media encoder at {EncoderPath}", encoderPath);

            var decoders = GetCodecs(encoderPath, Codec.Decoder);
            var encoders = GetCodecs(encoderPath, Codec.Encoder);

            _logger.LogInformation("Encoder validation complete");

            return (decoders, encoders);
        }

        public bool ValidateVersion(string encoderAppPath, bool logOutput)
        {
            string output = null;
            try
            {
                output = GetProcessOutput(encoderAppPath, "-version");
            }
            catch (Exception ex)
            {
                if (logOutput)
                {
                    _logger.LogError(ex, "Error validating encoder");
                }
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                return false;
            }

            _logger.LogDebug("ffmpeg output: {Output}", output);

            if (output.IndexOf("Libav developers", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return false;
            }

            // The minimum FFmpeg version required to run jellyfin successfully
            FFmpegVersion required = new FFmpegVersion("4.0");

            // Work out what the version under test is
            FFmpegVersion underTest = GetFFmpegVersion(output);

            if (logOutput)
            {
                if (underTest.Unknown())
                {
                    _logger.LogWarning("FFmpeg validation: Unknown version");
                }
                else if (underTest.Below(required))
                {
                    _logger.LogWarning("FFmpeg validation: Found version {0} which is below the minimum recommended of {1}",
                        underTest.ToString(), required.ToString());
                }
                else if (underTest.Experimental())
                {
                    _logger.LogWarning("FFmpeg validation: Unknown version: {0}?", underTest.ToString());
                }
                else
                {
                    _logger.LogInformation("FFmpeg validation: Detected version {0}", underTest.ToString());
                }
            }

            return underTest.Suitable(required);
        }

        /// <summary>
        /// Using the output from "ffmpeg -version" work out the FFmpeg version.
        /// For pre-built binaries the first line should contain a string like "ffmpeg version x.y", which is easy
        /// to parse.  If this is not available, then we try to match known library versions to FFmpeg versions.
        /// If that fails then we use one of the main libraries to determine if it's new/older than the latest
        /// we have stored.
        /// </summary>
        /// <param name="output"></param>
        /// <returns></returns>
        static private FFmpegVersion GetFFmpegVersion(string output)
        {
            // For pre-built binaries the FFmpeg version should be mentioned at the very start of the output
            var match = Regex.Match(output, @"ffmpeg version (\d+\.\d+)");

            if (match.Success)
            {
                return new FFmpegVersion(match.Groups[1].Value);
            }
            else
            {
                // Try and use the individual library versions to determine a FFmpeg version
                // This lookup table is to be maintained with the following command line:
                // $ ./ffmpeg.exe -version | perl -ne ' print "$1=$2.$3," if /^(lib\w+)\s+(\d+)\.\s*(\d+)/'
                ReadOnlyDictionary<FFmpegVersion, string> lut = new ReadOnlyDictionary<FFmpegVersion, string>
                    (new Dictionary<FFmpegVersion, string>
                    {
                        { new FFmpegVersion("4.1"), "libavutil=56.22,libavcodec=58.35,libavformat=58.20,libavdevice=58.5,libavfilter=7.40,libswscale=5.3,libswresample=3.3,libpostproc=55.3," },
                        { new FFmpegVersion("4.0"), "libavutil=56.14,libavcodec=58.18,libavformat=58.12,libavdevice=58.3,libavfilter=7.16,libswscale=5.1,libswresample=3.1,libpostproc=55.1," },
                        { new FFmpegVersion("3.4"), "libavutil=55.78,libavcodec=57.107,libavformat=57.83,libavdevice=57.10,libavfilter=6.107,libswscale=4.8,libswresample=2.9,libpostproc=54.7," },
                        { new FFmpegVersion("3.3"), "libavutil=55.58,libavcodec=57.89,libavformat=57.71,libavdevice=57.6,libavfilter=6.82,libswscale=4.6,libswresample=2.7,libpostproc=54.5," },
                        { new FFmpegVersion("3.2"), "libavutil=55.34,libavcodec=57.64,libavformat=57.56,libavdevice=57.1,libavfilter=6.65,libswscale=4.2,libswresample=2.3,libpostproc=54.1," },
                        { new FFmpegVersion("2.8"), "libavutil=54.31,libavcodec=56.60,libavformat=56.40,libavdevice=56.4,libavfilter=5.40,libswscale=3.1,libswresample=1.2,libpostproc=53.3," }
                    });

                // Create a reduced version string and lookup key from dictionary
                var reducedVersion = GetVersionString(output);
                var found = lut.FirstOrDefault(x => x.Value == reducedVersion).Key;

                if (found != null)
                {
                    return found;
                }
                else
                {
                    // Unknown version.  Test the main libavcoder version in the candidate with the
                    // latest from the dictionary.  If candidate is greater than dictionary chances are
                    // the user if running HEAD/master ffmpeg build (which is probably ok)
                    var firstElement = lut.FirstOrDefault();

                    var reqVer = Regex.Match(firstElement.Value, @"libavcodec=(\d+\.\d+)");
                    var gotVer = Regex.Match(reducedVersion, @"libavcodec=(\d+\.\d+)");

                    if (reqVer.Success && gotVer.Success)
                    {
                        var req = new FFmpegVersion(reqVer.Groups[1].Value);
                        var got = new FFmpegVersion(gotVer.Groups[1].Value);

                        // The library versions are not comparable with the FFmpeg version so must check
                        // candidate (got) against value from dictionary (req).  Return Experimental if suitable
                        if( got.Suitable(req) )
                        {
                            return new FFmpegVersion(-1);

                        }
                    }
                }
            }

            // Default to return Unknown
            return new FFmpegVersion(0);
        }

        /// <summary>
        /// Grabs the library names and major.minor version numbers from the 'ffmpeg -version' output
        /// and condenses them on to one line.  Output format is "name1=major.minor,name2=major.minor,etc."
        /// </summary>
        /// <param name="output"></param>
        /// <returns></returns>
        static private string GetVersionString(string output)
        {
            string pattern = @"((?<name>lib\w+)\s+(?<major>\d+)\.\s*(?<minor>\d+))";
            RegexOptions options = RegexOptions.Multiline;

            string rc = null;

            foreach (Match m in Regex.Matches(output, pattern, options))
            {
                rc += string.Concat(m.Groups["name"], '=', m.Groups["major"], '.', m.Groups["minor"], ',');
            }

            return rc;
        }

        private static readonly string[] requiredDecoders = new[]
            {
                "mpeg2video",
                "h264_qsv",
                "hevc_qsv",
                "mpeg2_qsv",
                "vc1_qsv",
                "h264_cuvid",
                "hevc_cuvid",
                "dts",
                "ac3",
                "aac",
                "mp3",
                "h264",
                "hevc"
            };

        private static readonly string[] requiredEncoders = new[]
            {
                "libx264",
                "libx265",
                "mpeg4",
                "msmpeg4",
                "libvpx",
                "libvpx-vp9",
                "aac",
                "libmp3lame",
                "libopus",
                "libvorbis",
                "srt",
                "h264_nvenc",
                "hevc_nvenc",
                "h264_qsv",
                "hevc_qsv",
                "h264_omx",
                "hevc_omx",
                "h264_vaapi",
                "hevc_vaapi",
                "ac3"
            };

        private enum Codec
        {
            Encoder,
            Decoder
        }

        private IEnumerable<string> GetCodecs(string encoderAppPath, Codec codec)
        {
            string codecstr = codec == Codec.Encoder ? "encoders" : "decoders";
            string output = null;
            try
            {
                output = GetProcessOutput(encoderAppPath, "-" + codecstr);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting available {Codec}", codecstr);
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                return Enumerable.Empty<string>();
            }

            var required = codec == Codec.Encoder ? requiredEncoders : requiredDecoders;

            var found = Regex
                .Matches(output, @"^\s\S{6}\s(?<codec>[\w|-]+)\s+.+$", RegexOptions.Multiline)
                .Cast<Match>()
                .Select(x => x.Groups["codec"].Value)
                .Where(x => required.Contains(x));

            _logger.LogInformation("Available {Codec}: {Codecs}", codecstr, found);

            return found;
        }

        private string GetProcessOutput(string path, string arguments)
        {
            IProcess process = _processFactory.Create(new ProcessOptions
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                FileName = path,
                Arguments = arguments,
                IsHidden = true,
                ErrorDialog = false,
                RedirectStandardOutput = true,
                // ffmpeg uses stderr to log info, don't show this
                RedirectStandardError = true
            });

            _logger.LogDebug("Running {Path} {Arguments}", path, arguments);

            using (process)
            {
                process.Start();

                try
                {
                    return process.StandardOutput.ReadToEnd();
                }
                catch
                {
                    _logger.LogWarning("Killing process {Path} {Arguments}", path, arguments);

                    // Hate having to do this
                    try
                    {
                        process.Kill();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error killing process");
                    }

                    throw;
                }
            }
        }
    }
}
