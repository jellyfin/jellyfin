using System;
using System.Diagnostics;
using System.IO;
using MediaBrowser.Controller.Extensions;

namespace Jellyfin.MediaEncoding.Tests.FFProcessing;

/// <summary>
/// Builds the media these tests run against, so the repository ships no binary fixtures.
/// Everything is synthesized from lavfi sources plus <c>-attach</c>.
/// </summary>
public sealed class SynthesizedMediaFixture : IDisposable
{
    private const string DefaultEncoder = "/usr/share/jellyfin-ffmpeg/ffmpeg";

    // xunit builds the class fixture for a test class even when every test in that class is
    // Explicit and will not run, so the constructor has to stay inert: synthesizing here would
    // fail the whole class on a machine without ffmpeg, which is what Explicit exists to avoid.
    // Deferring to first property access keeps the hard failure, but only for a run that asked
    // for these tests.
    private readonly Lazy<SynthesizedMedia> _media = new(Synthesize);

    public string EncoderPath => _media.Value.EncoderPath;

    public string ProberPath => _media.Value.ProberPath;

    public string Root => _media.Value.Root;

    public string AttachmentName => _media.Value.AttachmentName;

    public string VideoWithAttachment => _media.Value.VideoWithAttachment;

    public string SubtitlesWithAttachment => _media.Value.SubtitlesWithAttachment;

    public string VideoWithoutAttachment => _media.Value.VideoWithoutAttachment;

    public string Mp4 => _media.Value.Mp4;

    /// <summary>
    /// Gets the environment variable the server itself honours for the encoder path, which is what
    /// <c>--ffmpeg</c> sets. Configuration keys are matched case-insensitively, so the
    /// conventional upper-case spelling works too.
    /// </summary>
    private static string EnvironmentVariable => "JELLYFIN_" + ConfigurationExtensions.FfmpegPathKey;

    public void Dispose()
    {
        // Nothing was synthesized if no test touched the fixture, which is the normal case.
        if (!_media.IsValueCreated)
        {
            return;
        }

        try
        {
            Directory.Delete(_media.Value.Root, true);
        }
        catch (IOException)
        {
            // A leftover temp directory is not worth failing a test run over.
        }
    }

    private static SynthesizedMedia Synthesize()
    {
        var encoderPath = ResolveEncoderPath();

        // The server never configures ffprobe separately; MediaEncoder derives it from the encoder
        // path, so do the same here.
        var proberPath = Path.Combine(
            Path.GetDirectoryName(encoderPath) ?? string.Empty,
            "ffprobe" + Path.GetExtension(encoderPath));

        // These tests are opt-in, so a missing binary is a hard failure rather than a silent skip:
        // a run that was explicitly asked for must not report green without having run.
        if (!File.Exists(encoderPath) || !File.Exists(proberPath))
        {
            throw new InvalidOperationException(
                $"ffmpeg not found at '{encoderPath}' / '{proberPath}'. Point {EnvironmentVariable} "
                + "at an ffmpeg binary, or install jellyfin-ffmpeg.");
        }

        var root = Directory.CreateTempSubdirectory("jf-ffmpeg-tests-").FullName;

        var font = Path.Combine(root, "TestFont.ttf");
        File.WriteAllText(font, "not really a font, but ffmpeg only copies the bytes");

        var srt = Path.Combine(root, "s.srt");
        File.WriteAllText(srt, "1\n00:00:00,000 --> 00:00:02,000\nhello\n\n");

        const string Attach = "-metadata:s:t mimetype=application/x-truetype-font";

        // Video + audio + one attachment.
        var videoWithAttachment = Path.Combine(root, "av_with_attachment.mkv");
        Run(encoderPath, $"-y -f lavfi -i testsrc=d=1:s=64x64 -f lavfi -i sine=d=1 -attach \"{font}\" {Attach} "
            + $"-c:v libx264 -c:a aac -shortest \"{videoWithAttachment}\"");

        // Subtitles + one attachment, no audio or video. This is the shape Jellyfin writes when it
        // round-trips VobSub.
        var subtitlesWithAttachment = Path.Combine(root, "subs_only.mks");
        Run(encoderPath, $"-y -i \"{srt}\" -attach \"{font}\" {Attach} -c:s srt -f matroska \"{subtitlesWithAttachment}\"");

        // No attachments at all.
        var videoWithoutAttachment = Path.Combine(root, "no_attachment.mkv");
        Run(encoderPath, $"-y -f lavfi -i testsrc=d=1:s=64x64 -c:v libx264 \"{videoWithoutAttachment}\"");

        // MP4 carries no attachment streams, so it exercises the same empty-map path.
        var mp4 = Path.Combine(root, "video.mp4");
        Run(encoderPath, $"-y -f lavfi -i testsrc=d=1:s=64x64 -f lavfi -i sine=d=1 -c:v libx264 -c:a aac -shortest \"{mp4}\"");

        return new SynthesizedMedia(
            encoderPath,
            proberPath,
            root,
            Path.GetFileName(font),
            videoWithAttachment,
            subtitlesWithAttachment,
            videoWithoutAttachment,
            mp4);
    }

    private static string ResolveEncoderPath()
    {
        var configured = Environment.GetEnvironmentVariable(EnvironmentVariable)
                         ?? Environment.GetEnvironmentVariable(EnvironmentVariable.ToUpperInvariant());

        return string.IsNullOrWhiteSpace(configured) ? DefaultEncoder : configured;
    }

    private static void Run(string encoderPath, string arguments)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = encoderPath,
            Arguments = "-hide_banner -loglevel error -nostdin " + arguments,
            UseShellExecute = false,
            RedirectStandardError = true
        })!;

        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Fixture setup failed ({process.ExitCode}): {stderr}");
        }
    }

    private sealed record SynthesizedMedia(
        string EncoderPath,
        string ProberPath,
        string Root,
        string AttachmentName,
        string VideoWithAttachment,
        string SubtitlesWithAttachment,
        string VideoWithoutAttachment,
        string Mp4);
}
